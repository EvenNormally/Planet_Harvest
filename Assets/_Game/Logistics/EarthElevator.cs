using PlanetHarvest.Mining;
using PlanetHarvest.Art;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    public sealed class EarthElevator : MonoBehaviour
    {
        [SerializeField] private EarthCargoBin soil;
        [SerializeField] private EarthCargoBin stone;
        [SerializeField] private ElevatorContractSet contracts;
        [SerializeField] private BeamSettings beamSettings;
        public BeamSettings BeamSettings => beamSettings;
        [SerializeField] private Machines.RobotSettings robotSettings;
        public Machines.RobotSettings RobotSettings => robotSettings;
        [SerializeField] private WaterSettings waterSettings;
        public WaterSettings WaterSettings => waterSettings;
        private EarthCargoBin[] bins;
        private Material hardware;
        private Transform[] extensions;
        private float assembled;
        public bool Replica { get; set; }
        public EarthFreight Freight { get; private set; }

        private void Awake()
        {
            if (contracts == null) throw new MissingReferenceException($"{name}: Elevator-Aufträge fehlen.");
            Freight = new EarthFreight(contracts.CreateProgram());
            extensions = new Transform[Freight.Contracts.Count];
        }

        public void Place(VoxelPlanet planet)
        {
            Vector3 spawn = planet.Earth.SpawnDirection;
            Vector3 local = (spawn * planet.Earth.SurfaceRadius(spawn) + new Vector3(6, 0, 9)).normalized;
            Vector3 up = planet.transform.TransformDirection(local);
            Vector3 origin = planet.transform.position + up * (planet.Earth.Extent + 4);
            Vector3 foot = planet.transform.TransformPoint(local * planet.Earth.SurfaceRadius(local));
            if (Physics.Raycast(origin, -up, out var hit, planet.Earth.Extent, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<VoxelPlanet>() == planet) foot = hit.point;
            transform.SetPositionAndRotation(foot, Quaternion.LookRotation(Vector3.ProjectOnPlane(planet.transform.forward, up), up));
            hardware = FieldProps.Finish();
            var program = Freight.Program;
            bins = new EarthCargoBin[program.Materials.Count];
            int extra = 0;
            for (int i = 0; i < bins.Length; i++)
            {
                var spec = program.Materials[i];
                // The prefab carries the two landing-contract bins; every further material gets a copy of the barrel.
                if (spec.Id == soil.Material) bins[i] = soil;
                else if (spec.Id == stone.Material) bins[i] = stone;
                else
                {
                    float x = 3.55f + extra++ * 1.8f;
                    bins[i] = Instantiate(stone, transform);
                    bins[i].ConfigureOre(spec, new Vector3(x,.85f,-.12f));
                    FieldProps.Part(transform, "Führungsschiene", PrimitiveType.Cube, new Vector3(x,33,.64f),
                        new Vector3(.07f,66,.07f), new Color(.29f,.39f,.4f), hardware);
                }
            }
            if (!program.Contains(soil.Material)) soil.gameObject.SetActive(false);
            if (!program.Contains(stone.Material)) stone.gameObject.SetActive(false);
            var deck = FieldProps.Part(transform, "Erzrampe", PrimitiveType.Cube, new Vector3(7.4f,-.19f,0),
                new Vector3(9.9f,.52f,2.7f), new Color(.18f,.26f,.29f), hardware);
            deck.AddComponent<BoxCollider>();
            for (int i = 0; i < bins.Length; i++) bins[i].BuildGauge(hardware, program.Materials[i].Container);
            BuildExtensions();
        }

        private void BuildExtensions()
        {
            string[] names = { "Wartungssteg", "Stützjoch", "Seilwinde", "Solarflügel", "Funkrelais" };
            Color frame = new Color(.22f,.32f,.36f), signal = new Color(.38f,.84f,.68f);
            for (int i = 0; i < extensions.Length; i++)
            {
                string label = names[i % names.Length];
                var module = new GameObject(label).transform;
                module.SetParent(transform, false);
                module.localPosition = new Vector3(-2.05f,5.9f+i*1.4f,.85f);
                int shape = i % names.Length;
                extensions[i] = module;
                FieldProps.Part(module,"Mastsegment",PrimitiveType.Cube,Vector3.up*.7f,new Vector3(.32f,1.4f,.36f),frame,hardware);
                FieldProps.Part(module,"Signalband",PrimitiveType.Cube,Vector3.up*1.28f,new Vector3(.38f,.13f,.42f),signal,hardware);
                if (shape < 2)
                {
                    var beam = FieldProps.Part(module,label,PrimitiveType.Cube,new Vector3(1.9f,.82f,0),
                        new Vector3(4.1f,.18f,.43f),frame,hardware);
                    beam.transform.localRotation = Quaternion.Euler(0,0,shape*12);
                }
                else if (shape == 2)
                {
                    var drum = FieldProps.Part(module,label,PrimitiveType.Cylinder,new Vector3(.43f,.8f,0),
                        new Vector3(.83f,.48f,.83f),signal,hardware);
                    drum.transform.localRotation = Quaternion.Euler(90,0,0);
                }
                else if (shape == 3)
                {
                    foreach (int side in new[] { -1,1 })
                        FieldProps.Part(module,label,PrimitiveType.Cube,new Vector3(side*1.22f,.87f,0),
                            new Vector3(2.35f,.11f,1.45f),new Color(.18f,.39f,.66f),hardware);
                }
                else
                {
                    var dish = FieldProps.Part(module,label,PrimitiveType.Sphere,Vector3.up*1.5f,
                        new Vector3(1.23f,.26f,1.23f),Color.white,hardware);
                    dish.transform.localRotation = Quaternion.Euler(-25,0,24);
                }
                module.gameObject.SetActive(false);
            }
        }

        private void AnimateConstruction()
        {
            if (assembled > Freight.CompletedContracts)
            {
                assembled = 0;
                foreach (var extension in extensions) extension.gameObject.SetActive(false);
            }
            if (assembled == Freight.CompletedContracts) return;
            float before = assembled;
            assembled = Mathf.MoveTowards(assembled,Freight.CompletedContracts,Time.deltaTime * 1.5f);
            for (int i = Mathf.FloorToInt(before); i < Mathf.CeilToInt(assembled); i++)
            {
                float amount = Mathf.SmoothStep(0,1,Mathf.Clamp01(assembled-i));
                extensions[i].gameObject.SetActive(true);
                extensions[i].localScale = new Vector3(1,amount,1);
            }
        }

        private void OnDestroy() => Destroy(hardware);

        private void Update()
        {
            if (!Replica) Freight.Advance(Time.deltaTime);
            AnimateConstruction();
            float fraction = Mathf.SmoothStep(0, 1, Freight.LegProgress);
            float height = 65 * (Freight.Returning ? 1 - fraction : fraction);
            if (bins != null) foreach (var bin in bins) Position(bin, height);
        }

        private void Position(EarthCargoBin bin, float height)
        {
            bool flying = Freight.Flying == bin.Material;
            bin.SetHeight(flying ? height : 0, !flying);
            var container = Freight.Container(bin.Material);
            bin.SetFill((float)container.Amount / container.Capacity);
        }
    }
}
