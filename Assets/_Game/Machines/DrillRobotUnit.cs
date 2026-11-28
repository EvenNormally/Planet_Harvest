using System;
using System.Collections.Generic;
using PlanetHarvest.Art;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Machines
{
    /// <summary>
    /// Scene side of the drill robot: placement, work-area preview, walking and drilling animation, status light
    /// and unloading into a nearby beam hopper. The host simulates; a guest only shows the replicated state.
    /// </summary>
    public sealed class DrillRobotUnit : MonoBehaviour
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly Color Orange = new Color(.98f, .6f, .16f), Graphite = new Color(.17f, .24f, .27f),
            Mint = new Color(.38f, .84f, .68f), Violet = new Color(.62f, .38f, .95f);
        private readonly List<Vector3Int> cut = new List<Vector3Int>(128);
        private readonly List<GameObject> outline = new List<GameObject>();
        private readonly Transform[] legs = new Transform[4];
        private VoxelPlanet planet;
        private EarthFreight wallet;
        private BeamNetwork beams;
        private Predicate<Vector3Int> foundation;
        private Func<Vector3, bool> nearBase;
        private Material finish;
        private MaterialPropertyBlock tint;
        private Transform body, drillHead;
        private Renderer lamp;
        private BoxCollider hitBox;
        private int shownRevision = -1, outlinedArea = -1, areaRevision;
        private float walk;
        private ModelLibrary models;
        private MiningDebris dust;
        private int dustStrokes;

        public RobotSettings Settings { get; private set; }
        public DrillRobot Robot { get; private set; }
        public bool Replica { get; set; }
        public bool Unlocked => wallet.CompletedContracts >= Settings.UnlockAfterContracts;

        public void Initialize(VoxelPlanet planet, EarthFreight wallet, BeamNetwork beams, RobotSettings settings,
            Predicate<Vector3Int> foundation, Func<Vector3, bool> nearBase, ModelLibrary models = null)
        {
            this.models = models;
            this.planet = planet;
            this.wallet = wallet;
            this.beams = beams;
            this.foundation = foundation;
            this.nearBase = nearBase;
            Settings = settings ? settings : throw new MissingReferenceException("Bohrroboter-Einstellungen fehlen.");
            Robot = new DrillRobot(settings.HopperCubicMetres, settings.StrokeSeconds, foundation);
            finish = FieldProps.Finish();
            tint = new MaterialPropertyBlock();
            BuildBody();
            dust = new GameObject("Roboterstaub").AddComponent<MiningDebris>();
            dust.transform.SetParent(transform, false);
            dust.Prepare(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        }

        public RobotWorkArea AreaAt(Vector3Int centre) => new RobotWorkArea(centre, Settings.Width, Settings.Length, Settings.Depth);

        /// <summary>Null when the robot may be placed at this ground point, else the reason.</summary>
        public string CheckPlace(Vector3 worldPoint)
        {
            if (!Unlocked) return "Bohrroboter nach dem Landevertrag";
            if (Robot.Placed) return "Es gibt schon einen Bohrroboter";
            if (nearBase(worldPoint)) return "Schutzzone um Lift und Werkbank";
            return null;
        }

        public bool TryPlace(Vector3 worldPoint, out string reason)
        {
            reason = CheckPlace(worldPoint);
            if (reason != null) return false;
            if (!wallet.Spend(Settings.Price)) { reason = $"{Settings.Price} Credits nötig"; return false; }
            Vector3 local = planet.transform.InverseTransformPoint(worldPoint);
            Robot.Place(AreaAt(Vector3Int.FloorToInt(local - local.normalized * .3f)));
            areaRevision++;
            return true;
        }

        public bool TryRemove()
        {
            if (!Robot.TryRemove()) return false;
            wallet.Refund(Settings.Price);
            areaRevision++;
            return true;
        }

        /// <summary>E on the robot: hand its load to the player first, otherwise start or stop it.</summary>
        public string Use(HarvestInventory pack)
        {
            long moved = 0;
            for (int m = 1; m < HarvestInventory.MaterialSlots; m++)
                moved += Robot.Hopper.TransferTo(pack, (MaterialId)m);
            if (moved > 0) return $"{moved / HarvestInventory.CubicMetre} m³ übernommen";
            Robot.Toggle();
            return Robot.State == RobotState.Stopped ? "Bohrroboter gestoppt" : "Bohrroboter läuft";
        }

        public RobotSnapshot Capture() => Robot.Capture();

        public void Restore(RobotSnapshot state)
        {
            bool hadArea = Robot.Placed;
            var centre = hadArea ? Robot.Area.Centre : default;
            Robot.Restore(state, AreaAt);
            if (hadArea != Robot.Placed || (Robot.Placed && centre != Robot.Area.Centre)) areaRevision++;
        }

        private int Cut(Vector3Int contact, Vector3 axis, HarvestInventory hopper, Predicate<Vector3Int> blocked)
        {
            DrillStroke.Cut(planet, hopper, contact, axis, DrillProfile.Disc, Settings.CutScale, blocked, cut);
            return cut.Count;
        }

        private void Update()
        {
            if (Robot == null) return;
            if (!Replica)
            {
                Robot.Advance(Time.deltaTime, Cut);
                Unload();
            }
            Animate();
        }

        private void Unload()
        {
            if (!Robot.Placed || Robot.Hopper.Amount == 0 || beams == null || !beams.HasHopper) return;
            if (Vector3.Distance(body.position, beams.Orb(0)) > Settings.UnloadRange) return;
            for (int m = 1; m < HarvestInventory.MaterialSlots; m++)
                Robot.Hopper.TransferTo(beams.Chain.Hopper, (MaterialId)m);
        }

        private void BuildBody()
        {
            body = new GameObject("Bohrroboter").transform;
            body.SetParent(transform, false);
            hitBox = body.gameObject.AddComponent<BoxCollider>();
            hitBox.center = Vector3.up * .75f;
            hitBox.size = new Vector3(1.4f, 1.3f, 1.6f);
            body.gameObject.AddComponent<DrillRobotMarker>().Unit = this;
            var model = models ? models.Spawn(GameModel.DrillRobot, body, finish) : null;
            if (model)
            {
                lamp = ModelLibrary.Find(model, "Statusleuchte").GetComponent<Renderer>();
                drillHead = ModelLibrary.Find(model, "Bohrkopf");
                for (int i = 0; i < legs.Length; i++) legs[i] = ModelLibrary.Find(model, $"Bein{i}");
                body.gameObject.SetActive(false);
                return;
            }
            FieldProps.Part(body, "Rumpf", PrimitiveType.Cube, Vector3.up * .9f, new Vector3(1.1f, .6f, 1.3f), Orange, finish);
            FieldProps.Part(body, "Kuppel", PrimitiveType.Sphere, new Vector3(0, 1.25f, -.15f), new Vector3(.7f, .45f, .7f), Mint, finish);
            FieldProps.Part(body, "Antenne", PrimitiveType.Cylinder, new Vector3(.3f, 1.6f, -.3f), new Vector3(.04f, .25f, .04f), Graphite, finish);
            lamp = FieldProps.Part(body, "Statusleuchte", PrimitiveType.Sphere, new Vector3(.3f, 1.88f, -.3f), Vector3.one * .16f,
                Mint, finish).GetComponent<Renderer>();
            for (int i = 0; i < legs.Length; i++)
            {
                var hip = new GameObject("Bein").transform;
                hip.SetParent(body, false);
                hip.localPosition = new Vector3(i % 2 == 0 ? -.55f : .55f, .7f, i < 2 ? .45f : -.45f);
                FieldProps.Part(hip, "Bein", PrimitiveType.Cube, Vector3.down * .35f, new Vector3(.14f, .7f, .14f), Graphite, finish);
                legs[i] = hip;
            }
            drillHead = new GameObject("Bohrkopf").transform;
            drillHead.SetParent(body, false);
            drillHead.localPosition = new Vector3(0, .45f, .5f);
            FieldProps.Part(drillHead, "Bohrscheibe", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.8f, .05f, .8f), Violet, finish);
            for (int i = 0; i < 4; i++)
            {
                var tooth = FieldProps.Part(drillHead, "Zahn", PrimitiveType.Cube, Quaternion.Euler(0, i * 90, 0) * Vector3.forward * .38f,
                    new Vector3(.12f, .12f, .12f), Color.gray, finish);
                tooth.transform.localRotation = Quaternion.Euler(0, i * 90, 0);
            }
            body.gameObject.SetActive(false);
        }

        private void BuildOutline()
        {
            outlinedArea = areaRevision;
            foreach (var edge in outline) Destroy(edge);
            outline.Clear();
            if (!Robot.Placed) return;
            var area = Robot.Area;
            float w = area.Width * .5f, l = area.Length * .5f, top = RobotWorkArea.Above + .5f, bottom = -area.Depth;
            var corners = new List<Vector3>();
            foreach (float h in new[] { top, bottom })
            foreach (var (a, b) in new[] { (-w, -l), (w, -l), (w, l), (-w, l) })
                corners.Add(planet.transform.TransformPoint(area.Point(a, b, h)));
            for (int i = 0; i < 4; i++)
            {
                outline.Add(Edge(corners[i], corners[(i + 1) % 4]));
                outline.Add(Edge(corners[4 + i], corners[4 + (i + 1) % 4]));
                outline.Add(Edge(corners[i], corners[4 + i]));
            }
        }

        private GameObject Edge(Vector3 from, Vector3 to)
        {
            var edge = FieldProps.Part(transform, "Arbeitsbereich", PrimitiveType.Cube, Vector3.zero, Vector3.one, Mint, finish);
            edge.transform.position = (from + to) * .5f;
            edge.transform.rotation = Quaternion.LookRotation(to - from, (from - planet.transform.position).normalized);
            edge.transform.localScale = new Vector3(.05f, .05f, Vector3.Distance(from, to));
            return edge;
        }

        private void Animate()
        {
            if (outlinedArea != areaRevision) BuildOutline();
            body.gameObject.SetActive(Robot.Placed);
            if (!Robot.Placed) return;
            var area = Robot.Area;
            Vector3 contact = (Vector3)Robot.CurrentContact + Vector3.one * .5f;
            Vector3 target = planet.transform.TransformPoint(contact + area.Up * 1.1f);
            Vector3 up = (target - planet.transform.position).normalized;
            if (shownRevision < 0) body.position = target;
            float step = Time.deltaTime * 2.5f;
            bool moving = Vector3.Distance(body.position, target) > .05f;
            body.position = Vector3.MoveTowards(body.position, target, step);
            body.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(planet.transform.TransformDirection(area.Forward), up), up);
            shownRevision = Robot.Revision;
            bool working = Robot.State == RobotState.Working;
            if (moving || working) walk += Time.deltaTime * (moving ? 9 : 3);
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(walk + (i % 3 == 0 ? 0 : Mathf.PI)) * 25, 0, 0);
            if (working) drillHead.Rotate(0, Time.deltaTime * 720, 0, Space.Self);
            if (Robot.Strokes != dustStrokes)
            {
                dustStrokes = Robot.Strokes;
                Vector3 hit = planet.transform.TransformPoint(contact);
                dust.Burst(hit, up, up, MaterialId.Stone);
            }
            Color color = Robot.State switch
            {
                RobotState.Working => Mint,
                RobotState.Full => new Color(1f, .72f, .2f),
                RobotState.Finished => new Color(.35f, .55f, 1f),
                _ => new Color(.55f, .55f, .55f)
            };
            tint.SetColor(TintId, color);
            lamp.SetPropertyBlock(tint);
            foreach (var edge in outline) edge.SetActive(Robot.State != RobotState.Finished);
        }

        private void OnDestroy()
        {
            if (finish) Destroy(finish);
        }
    }
}
