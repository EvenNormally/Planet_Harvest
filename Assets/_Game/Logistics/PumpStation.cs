using PlanetHarvest.Art;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using TMPro;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    /// <summary>
    /// Pump site at the mountain lake. Bought and operated with E; the lake surface sinks with the remaining
    /// volume. Unloads into a nearby beam hopper, otherwise the player takes the water. Host simulates.
    /// </summary>
    public sealed class PumpStation : MonoBehaviour
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly Color Graphite = new Color(.17f, .24f, .27f), Mint = new Color(.38f, .84f, .68f),
            Water = new Color(.33f, .62f, .95f), Amber = new Color(1f, .72f, .2f);
        private EarthFreight wallet;
        private BeamNetwork beams;
        private Transform lake, waterColumn, pumpHead, built;
        private Vector3 lakeScale, lakePosition;
        private Renderer lamp;
        private TextMeshPro sign;
        private Material finish;
        private MaterialPropertyBlock tint;
        private float bob, splash;
        private ModelLibrary models;
        private Mining.MiningDebris drops;

        public WaterSettings Settings { get; private set; }
        public WaterPump Pump { get; private set; }
        public bool Replica { get; set; }
        public bool Unlocked => wallet.CompletedContracts >= Settings.UnlockAfterContracts;

        public void Initialize(VoxelPlanet planet, EarthFreight wallet, BeamNetwork beams, WaterSettings settings, Transform lakeSurface,
            ModelLibrary models = null)
        {
            this.models = models;
            this.wallet = wallet;
            this.beams = beams;
            Settings = settings ? settings : throw new MissingReferenceException("Wassereinstellungen fehlen.");
            Pump = new WaterPump(new WaterReservoir(settings.LakeCubicMetres), settings.TankCubicMetres, settings.PumpSeconds);
            lake = lakeSurface;
            if (lake) { lakeScale = lake.localScale; lakePosition = lake.localPosition; }
            finish = FieldProps.Finish();
            tint = new MaterialPropertyBlock();
            Place(planet);
            Assemble();
            drops = new GameObject("Spritzwasser").AddComponent<Mining.MiningDebris>();
            drops.transform.SetParent(transform, false);
            drops.Prepare(Resources.GetBuiltinResource<Mesh>("Sphere.fbx"));
        }

        private void Place(VoxelPlanet planet)
        {
            var field = planet.Earth;
            Vector3 lakeUp = Planets.EarthPlanetField.LakeDirection;
            Vector3 tangent = Vector3.Cross(lakeUp, Vector3.forward).normalized;
            Vector3 direction = (lakeUp * field.Radius + tangent * 9.5f).normalized;
            Vector3 up = planet.transform.TransformDirection(direction);
            Vector3 foot = planet.transform.TransformPoint(direction * field.SurfaceRadius(direction));
            if (Physics.Raycast(foot + up * 8, -up, out var hit, 16, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<VoxelPlanet>() == planet) foot = hit.point;
            Vector3 toLake = Vector3.ProjectOnPlane(planet.transform.TransformDirection(lakeUp) - up, up);
            transform.SetPositionAndRotation(foot, Quaternion.LookRotation(toLake.sqrMagnitude > 1e-4f ? toLake : Vector3.Cross(up, Vector3.right), up));
        }

        private void Assemble()
        {
            FieldProps.Part(transform, "Pumpenplatte", PrimitiveType.Cube, Vector3.up * .08f, new Vector3(2.2f, .16f, 2.2f), Graphite, finish);
            sign = new GameObject("Pumpenschild").AddComponent<TextMeshPro>();
            sign.transform.SetParent(transform, false);
            sign.transform.localPosition = new Vector3(0, 2.6f, -.6f);
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            sign.rectTransform.sizeDelta = new Vector2(3, .6f);
            sign.fontSize = 2.4f;
            sign.alignment = TextAlignmentOptions.Center;
            sign.color = new Color(.88f, .97f, .83f);
            built = new GameObject("Pumpe").transform;
            built.SetParent(transform, false);
            var model = models ? models.Spawn(GameModel.Pump, built, finish) : null;
            if (model)
            {
                FieldProps.Part(built, "Glastank", PrimitiveType.Sphere, new Vector3(-.95f, 1.1f, 0), Vector3.one * 1.1f, new Color(.75f, .9f, 1f, .35f), finish);
                waterColumn = FieldProps.Part(built, "Tankwasser", PrimitiveType.Sphere, new Vector3(-.95f, .7f, 0), Vector3.one * .1f, Water, finish).transform;
                pumpHead = ModelLibrary.Find(model, "Pumpenkopf");
                lamp = ModelLibrary.Find(model, "Statusleuchte").GetComponent<Renderer>();
                var hitBox = gameObject.AddComponent<BoxCollider>();
                hitBox.center = Vector3.up * 1;
                hitBox.size = new Vector3(2.2f, 2, 2.2f);
                return;
            }
            FieldProps.Part(built, "Gehäuse", PrimitiveType.Cylinder, Vector3.up * .7f, new Vector3(.9f, .55f, .9f), new Color(.98f, .6f, .16f), finish);
            pumpHead = FieldProps.Part(built, "Pumpenkopf", PrimitiveType.Cube, Vector3.up * 1.4f, new Vector3(.5f, .3f, .5f), Mint, finish).transform;
            var hose = FieldProps.Part(built, "Saugrüssel", PrimitiveType.Cylinder, new Vector3(0, .5f, 1.6f), new Vector3(.18f, 1.2f, .18f), new Color(.62f, .38f, .95f), finish);
            hose.transform.localRotation = Quaternion.Euler(70, 0, 0);
            FieldProps.Part(built, "Glastank", PrimitiveType.Sphere, new Vector3(-.95f, 1.1f, 0), Vector3.one * 1.1f, new Color(.75f, .9f, 1f, .35f), finish);
            waterColumn = FieldProps.Part(built, "Tankwasser", PrimitiveType.Sphere, new Vector3(-.95f, .7f, 0), Vector3.one * .1f, Water, finish).transform;
            lamp = FieldProps.Part(built, "Statusleuchte", PrimitiveType.Sphere, new Vector3(.45f, 1.7f, 0), Vector3.one * .18f, Mint, finish).GetComponent<Renderer>();
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.up * 1;
            box.size = new Vector3(2.2f, 2, 2.2f);
        }

        /// <summary>E at the pump: build it, hand over the tank, or start/stop.</summary>
        public string Use(HarvestInventory pack)
        {
            if (!Pump.Built)
            {
                if (!Unlocked) return "Pumpe nach dem Landevertrag";
                if (!wallet.Spend(Settings.PumpPrice)) return $"Pumpe kostet {Settings.PumpPrice} Credits";
                Pump.Build();
                return "Pumpe gebaut · E startet";
            }
            long moved = Pump.Tank.TransferTo(pack, MaterialId.Water);
            if (moved > 0) return $"{moved / HarvestInventory.CubicMetre} m³ Wasser übernommen";
            Pump.Toggle();
            return Pump.State == PumpState.Pumping ? "Pumpe läuft" : Pump.State == PumpState.Dry ? "See ist leer" : "Pumpe gestoppt";
        }

        private void Update()
        {
            if (Pump == null) return;
            if (!Replica)
            {
                Pump.Advance(Time.deltaTime);
                if (Pump.Tank.Amount > 0 && beams != null && beams.HasHopper &&
                    Vector3.Distance(transform.position, beams.Orb(0)) <= Settings.UnloadRange)
                    Pump.Tank.TransferTo(beams.Chain.Hopper, MaterialId.Water);
            }
            Animate();
        }

        private void Animate()
        {
            built.gameObject.SetActive(Pump.Built);
            sign.text = Pump.Built ? $"WASSER {Pump.Reservoir.Remaining / HarvestInventory.CubicMetre} m³" : $"PUMPE · {Settings.PumpPrice} Cr";
            float fill = (float)Pump.Tank.Amount / Pump.Tank.Capacity;
            waterColumn.localScale = Vector3.one * Mathf.Lerp(.1f, 1.02f, fill);
            waterColumn.localPosition = new Vector3(-.95f, Mathf.Lerp(.6f, 1.1f, fill), 0);
            if (Pump.State == PumpState.Pumping)
            {
                bob += Time.deltaTime * 8;
                pumpHead.localPosition = Vector3.up * (1.4f + Mathf.Abs(Mathf.Sin(bob)) * .15f);
                splash -= Time.deltaTime;
                if (splash <= 0)
                {
                    splash = .45f;
                    Vector3 nozzle = transform.TransformPoint(new Vector3(0, .1f, 2.3f));
                    drops.Burst(nozzle, transform.up, transform.up, MaterialId.Water);
                }
            }
            tint.SetColor(TintId, Pump.State switch
            {
                PumpState.Pumping => Mint, PumpState.Full => Amber, PumpState.Dry => new Color(.9f, .3f, .3f),
                _ => new Color(.55f, .55f, .55f)
            });
            lamp.SetPropertyBlock(tint);
            if (!lake) return;
            float level = (float)Pump.Reservoir.Fill;
            lake.gameObject.SetActive(level > 0);
            // The lake sinks towards the planet centre and shrinks as it is drained.
            lake.localScale = lakeScale * Mathf.Lerp(.965f, 1, level);
            lake.localPosition = lakePosition;
        }

        private void OnDestroy()
        {
            if (finish) Destroy(finish);
        }
    }
}
