using System;
using System.Collections.Generic;
using PlanetHarvest.Art;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    [Serializable]
    public sealed class BeamNetworkSnapshot
    {
        /// <summary>Planet-local foot positions; index 0 is the hopper, the rest are masts.</summary>
        public Vector3[] nodes = Array.Empty<Vector3>();
        public BeamChainSnapshot chain;
    }

    /// <summary>Feeds lumps arriving at the head of the chain into the matching elevator container.</summary>
    public sealed class ElevatorBeamSink : IBeamSink
    {
        private readonly EarthFreight freight;
        public ElevatorBeamSink(EarthFreight freight) => this.freight = freight;

        public long Accept(HarvestInventory source, MaterialId material, long volume) =>
            freight.Program.Contains(material) ? freight.Load(source, material, volume) : 0;
    }

    /// <summary>
    /// One tractor-beam chain per expedition: a hopper, a row of antigrav masts and, once the last mast is in
    /// range, the elevator intake. The host simulates; a guest only shows the replicated state.
    /// </summary>
    public sealed class BeamNetwork : MonoBehaviour
    {
        private const float OrbHeight = 3.2f, HopperHeight = 1.25f;
        private static readonly Color Violet = new Color(.62f, .38f, .95f), Mast = new Color(.22f, .32f, .36f),
            Mint = new Color(.38f, .84f, .68f), Amber = new Color(1f, .72f, .2f);
        private readonly List<Vector3> nodes = new List<Vector3>();
        private readonly List<GameObject> props = new List<GameObject>();
        private readonly List<Transform> lumps = new List<Transform>();
        private MaterialPropertyBlock tint;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private VoxelPlanet planet;
        private EarthElevator elevator;
        private Predicate<Vector3Int> foundation;
        private IBeamSink sink;
        private Material finish;
        private Transform intakeBeam;
        private Renderer status;
        private int builtLayout = -1;
        private ModelLibrary models;
        private readonly List<Transform> rings = new List<Transform>();
        private readonly List<Transform> cores = new List<Transform>();
        private readonly List<Transform> beamsShown = new List<Transform>();

        public BeamSettings Settings { get; private set; }
        public BeamChain Chain { get; private set; }
        public bool Replica { get; set; }
        public int MastCount => Math.Max(0, nodes.Count - 1);
        public bool HasHopper => nodes.Count > 0;
        public int LayoutRevision { get; private set; }
        public Vector3 IntakePoint => elevator.transform.position + elevator.transform.up * 1.6f;
        public bool Connected => MastCount > 0 && Vector3.Distance(Orb(nodes.Count - 1), IntakePoint) <= Settings.Range;

        public void Initialize(VoxelPlanet planet, EarthElevator elevator, BeamSettings settings, Predicate<Vector3Int> foundation,
            ModelLibrary models = null)
        {
            this.models = models;
            this.planet = planet;
            this.elevator = elevator;
            this.foundation = foundation;
            Settings = settings ? settings : throw new MissingReferenceException("Traktorstrahl-Einstellungen fehlen.");
            Chain = new BeamChain(settings.HopperCubicMetres, settings.SlotsPerSegment, settings.StepSeconds);
            sink = new ElevatorBeamSink(elevator.Freight);
            finish = FieldProps.Finish();
            tint = new MaterialPropertyBlock();
        }

        public bool Unlocked => elevator.Freight.CompletedContracts >= Settings.UnlockAfterContracts;
        public int NextPrice => HasHopper ? Settings.MastPrice : Settings.HopperPrice;

        /// <summary>World position of a node's beam emitter.</summary>
        public Vector3 Orb(int node)
        {
            Vector3 foot = planet.transform.TransformPoint(nodes[node]);
            return foot + Up(foot) * (node == 0 ? HopperHeight : OrbHeight);
        }

        private Vector3 Up(Vector3 world) => (world - planet.transform.position).normalized;

        /// <summary>Checks a build spot without spending. Returns null when allowed, else the reason.</summary>
        public string CheckBuild(Vector3 worldPoint)
        {
            if (!Unlocked) return "Freischaltung nach dem Landevertrag";
            if (MastCount >= Settings.MaximumMasts) return "Maximale Mastzahl erreicht";
            Vector3 local = planet.transform.InverseTransformPoint(worldPoint);
            if (foundation(Vector3Int.FloorToInt(local - local.normalized * .3f))) return "Geschützte Zone";
            if (HasHopper)
            {
                Vector3 foot = planet.transform.TransformPoint(nodes[nodes.Count - 1]);
                float distance = Vector3.Distance(foot, worldPoint);
                if (distance > Settings.Range) return $"Zu weit vom letzten Mast ({distance:F0} m)";
                if (distance < Settings.MinimumSpacing) return "Zu nah am letzten Mast";
            }
            return null;
        }

        public bool TryBuild(Vector3 worldPoint, EarthFreight wallet, out string reason)
        {
            reason = CheckBuild(worldPoint);
            if (reason != null) return false;
            if (!wallet.Spend(NextPrice)) { reason = $"{NextPrice} Credits nötig"; return false; }
            if (HasHopper) Chain.AddSegment();
            nodes.Add(planet.transform.InverseTransformPoint(worldPoint));
            LayoutRevision++;
            return true;
        }

        /// <summary>Removes the last mast (or the empty hopper) and refunds its price.</summary>
        public bool TryRemoveLast(EarthFreight wallet)
        {
            if (MastCount > 0)
            {
                if (!Chain.TryRemoveSegment()) return false;
                wallet.Refund(Settings.MastPrice);
            }
            else if (HasHopper && Chain.Hopper.Amount == 0) wallet.Refund(Settings.HopperPrice);
            else return false;
            nodes.RemoveAt(nodes.Count - 1);
            LayoutRevision++;
            return true;
        }

        /// <summary>Moves deliverable material from a backpack into the hopper.</summary>
        public long LoadHopper(HarvestInventory pack)
        {
            if (!HasHopper) return 0;
            long moved = 0;
            foreach (var material in elevator.Freight.Program.Materials)
                moved += pack.TransferTo(Chain.Hopper, material.Id);
            return moved;
        }

        public BeamNetworkSnapshot Capture() => new BeamNetworkSnapshot { nodes = nodes.ToArray(), chain = Chain.Capture() };

        public static void Validate(BeamNetworkSnapshot state, BeamSettings settings, float planetExtent)
        {
            if (state == null) return;
            if (state.nodes == null || state.nodes.Length > settings.MaximumMasts + 1)
                throw new System.IO.InvalidDataException("Ungültige Mastkette.");
            foreach (var node in state.nodes)
                if (!(node.sqrMagnitude < planetExtent * planetExtent * 4)) throw new System.IO.InvalidDataException("Ungültiger Mast.");
            BeamChain.Validate(state.chain, settings.HopperCubicMetres * BeamChain.Lump, settings.SlotsPerSegment, settings.StepSeconds);
            if (state.chain.segments != Math.Max(0, state.nodes.Length - 1) ||
                (state.nodes.Length == 0 && Array.Exists(state.chain.hopper, amount => amount != 0)))
                throw new System.IO.InvalidDataException("Mastkette und Strahl passen nicht.");
        }

        public void Restore(BeamNetworkSnapshot state)
        {
            state ??= new BeamNetworkSnapshot();
            state.chain ??= new BeamChainSnapshot { slots = Array.Empty<MaterialId>(), hopper = new long[HarvestInventory.MaterialSlots] };
            Chain.Restore(state.chain);
            bool same = state.nodes.Length == nodes.Count;
            for (int i = 0; same && i < nodes.Count; i++) same = nodes[i] == state.nodes[i];
            if (same) return;
            nodes.Clear();
            nodes.AddRange(state.nodes);
            LayoutRevision++;
        }

        private void Update()
        {
            if (Chain == null) return;
            if (!Replica) Chain.Advance(Time.deltaTime, Connected ? sink : null);
            if (builtLayout != LayoutRevision) BuildProps();
            Render();
        }

        private void BuildProps()
        {
            builtLayout = LayoutRevision;
            foreach (var prop in props) Destroy(prop);
            props.Clear();
            rings.Clear();
            cores.Clear();
            beamsShown.Clear();
            status = null;
            intakeBeam = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                Vector3 foot = planet.transform.TransformPoint(nodes[i]);
                Vector3 up = Up(foot);
                var root = new GameObject(i == 0 ? "Sammeltrichter" : $"Traktormast {i}").transform;
                root.SetParent(transform, false);
                root.SetPositionAndRotation(foot, Quaternion.FromToRotation(Vector3.up, up));
                props.Add(root.gameObject);
                var model = models ? models.Spawn(i == 0 ? GameModel.BeamHopper : GameModel.TractorMast, root, finish) : null;
                if (model)
                {
                    AddIfFound(cores, ModelLibrary.Find(model, "Strahlkern"));
                    AddIfFound(rings, ModelLibrary.Find(model, "Ring"));
                }
                if (i == 0)
                {
                    if (model) status = ModelLibrary.Find(model, "Statusleuchte")?.GetComponent<Renderer>();
                    else
                    {
                    FieldProps.Part(root, "Trichtersockel", PrimitiveType.Cylinder, Vector3.up * .3f, new Vector3(1.4f, .3f, 1.4f), Mast, finish);
                    FieldProps.Part(root, "Trichter", PrimitiveType.Cylinder, Vector3.up * .85f, new Vector3(1.7f, .25f, 1.7f), Amber, finish);
                    FieldProps.Part(root, "Strahlkern", PrimitiveType.Sphere, Vector3.up * HopperHeight, Vector3.one * .45f, Violet, finish);
                    }
                    var box = root.gameObject.AddComponent<BoxCollider>();
                    box.center = Vector3.up * .6f;
                    box.size = new Vector3(1.7f, 1.2f, 1.7f);
                    root.gameObject.AddComponent<BeamHopper>();
                    if (!status)
                        status = FieldProps.Part(root, "Statusleuchte", PrimitiveType.Sphere, new Vector3(.75f, .7f, 0),
                            Vector3.one * .22f, Mint, finish).GetComponent<Renderer>();
                    continue;
                }
                var beam = Beam($"Strahl {i}", Orb(i - 1), Orb(i));
                props.Add(beam.gameObject);
                beamsShown.Add(beam);
                if (model) continue;
                FieldProps.Part(root, "Fuß", PrimitiveType.Cylinder, Vector3.up * .1f, new Vector3(.7f, .1f, .7f), Mast, finish);
                FieldProps.Part(root, "Mast", PrimitiveType.Cylinder, Vector3.up * 1.5f, new Vector3(.16f, 1.5f, .16f), Mast, finish);
                FieldProps.Part(root, "Ring", PrimitiveType.Cylinder, Vector3.up * (OrbHeight - .3f), new Vector3(.6f, .04f, .6f), Mint, finish);
                FieldProps.Part(root, "Strahlkern", PrimitiveType.Sphere, Vector3.up * OrbHeight, Vector3.one * .38f, Violet, finish);
            }
            if (MastCount > 0)
            {
                intakeBeam = Beam("Lift-Einspeisung", Orb(nodes.Count - 1), IntakePoint);
                props.Add(intakeBeam.gameObject);
                beamsShown.Add(intakeBeam);
            }
        }

        private Transform Beam(string label, Vector3 from, Vector3 to)
        {
            var beam = FieldProps.Part(transform, label, PrimitiveType.Cylinder, Vector3.zero, Vector3.one, Violet, finish).transform;
            beam.position = (from + to) * .5f;
            beam.rotation = Quaternion.FromToRotation(Vector3.up, to - from);
            beam.localScale = new Vector3(.07f, Vector3.Distance(from, to) * .5f, .07f);
            return beam;
        }

        private void Render()
        {
            int count = Chain.SlotCount;
            while (lumps.Count < count)
            {
                var lump = FieldProps.Part(transform, "Materialklumpen", PrimitiveType.Cube, Vector3.zero, Vector3.one * .42f, Color.white, finish).transform;
                lumps.Add(lump);
            }
            float t = Mathf.SmoothStep(0, 1, Chain.StepProgress);
            for (int i = 0; i < lumps.Count; i++)
            {
                bool visible = i < count && Chain.Slot(i) != MaterialId.None;
                lumps[i].gameObject.SetActive(visible);
                if (!visible) continue;
                float slot = i + 1 - (Chain.MovedIn(i) ? 1 - t : 0);
                Vector3 position = SlotPosition(slot);
                float wobble = Mathf.Sin(Time.time * 5 + i * 1.7f);
                lumps[i].position = position + Up(position) * (.08f * wobble);
                lumps[i].rotation = Quaternion.Euler(Time.time * 40 + i * 33, Time.time * 25 + i * 71, 0);
                var spec = elevator.Freight.Program.Contains(Chain.Slot(i)) ? elevator.Freight.Program.Material(Chain.Slot(i)).ContainerColor : Color.gray;
                tint.SetColor(TintId, spec);
                lumps[i].GetComponent<Renderer>().SetPropertyBlock(tint);
            }
            if (status)
            {
                tint.SetColor(TintId, Chain.Blocked || (MastCount > 0 && !Connected) ? Amber : Mint);
                status.SetPropertyBlock(tint);
            }
            if (intakeBeam) intakeBeam.gameObject.SetActive(Connected);
            // Living beam: rings spin, cores breathe, beams shimmer in width.
            float time = Time.time;
            foreach (var ring in rings) ring.Rotate(0, 90 * Time.deltaTime, 0, Space.Self);
            for (int i = 0; i < cores.Count; i++) cores[i].localScale = Vector3.one * (1 + .12f * Mathf.Sin(time * 4 + i));
            for (int i = 0; i < beamsShown.Count; i++)
            {
                var scale = beamsShown[i].localScale;
                float width = .07f * (1 + .35f * Mathf.Sin(time * 9 + i * 1.3f));
                beamsShown[i].localScale = new Vector3(width, scale.y, width);
            }
        }

        private static void AddIfFound(List<Transform> list, Transform item)
        {
            if (item) list.Add(item);
        }

        /// <summary>Continuous slot coordinate (0 = hopper emitter, k·S = mast k) to a world position.</summary>
        private Vector3 SlotPosition(float slot)
        {
            int perSegment = Chain.SlotsPerSegment;
            int segment = Mathf.Clamp(Mathf.FloorToInt(slot / perSegment), 0, Math.Max(0, MastCount - 1));
            float along = Mathf.Clamp01((slot - segment * perSegment) / perSegment);
            return Vector3.Lerp(Orb(segment), Orb(segment + 1), along);
        }

        private void OnDestroy()
        {
            if (finish) Destroy(finish);
        }
    }
}
