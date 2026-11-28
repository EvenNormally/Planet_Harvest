using System.Collections.Generic;
using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Planets;
using PlanetHarvest.Player;
using PlanetHarvest.Voxel;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetHarvest.Mining
{
    [DefaultExecutionOrder(-10), RequireComponent(typeof(RadialPlayer))]
    public sealed class EarthMining : MonoBehaviour
    {
        public const float InteractReach = 5f;
        public const float BuildReach = 7f;

        [SerializeField] private VoxelPlanet planet;
        [SerializeField] private Camera view;
        [SerializeField] private FieldEquipment equipmentPrefab;
        [SerializeField] private EarthElevator elevatorPrefab;
        private RadialPlayer player;
        private UI.EarthHud hud;
        private MiningDebris debris;
        private Audio.ExpeditionAudio feedback;
        private EarthLandscape landscape;
        private float nextStrike;
        public FieldEquipment Equipment { get; private set; }
        public EarthElevator Elevator { get; private set; }
        public ToolStation Workshop { get; private set; }
        public BeamNetwork Beams { get; private set; }
        public Machines.DrillRobotUnit Robot { get; private set; }
        public Machines.DrillRobotUnit AimedRobot { get; private set; }
        public PumpStation Pump { get; private set; }
        public bool AtPump { get; private set; }
        public bool AimingAtPlant { get; private set; }
        public EarthLandscape Landscape => landscape;
        private ExpeditionRules rules;
        public ExpeditionRules Rules => rules ??= new ExpeditionRules(Elevator.Freight.Program, Equipment.UpgradeCatalog,
            Elevator.BeamSettings, Elevator.RobotSettings, Elevator.WaterSettings, landscape.IsPlant);
        public bool AtHopper { get; private set; }
        /// <summary>Feedback of the last build attempt for the HUD.</summary>
        public string BuildMessage { get; private set; } = string.Empty;
        public bool AtWorkshop { get; private set; }
        public FieldMenu Menu { get; private set; }
        public EarthCargoBin Bin { get; private set; }
        public const long PackCapacity = 24 * HarvestInventory.CubicMetre;
        public HarvestInventory Inventory { get; } = new HarvestInventory(PackCapacity);
        public DrillProfile Profile { get; internal set; }
        public ExpeditionCrew Crew { get; private set; }
        public VoxelPlanet Planet => planet;
        internal Camera View => view;
        private readonly List<Vector3Int> cut = new List<Vector3Int>(64);
        internal List<Vector3Int> Cut(HarvestInventory pack, MiningTool tool, DrillProfile profile,
            Vector3Int cell, Vector3 forward, float radiusMultiplier)
        {
            float scale = (tool == MiningTool.Pick ? 1 : tool == MiningTool.Drill ? 1.45f : 1.2f) * radiusMultiplier;
            DrillStroke.Cut(planet, pack, cell, planet.transform.InverseTransformDirection(forward),
                profile, scale, IsFoundation, cut);
            return cut;
        }

        internal bool IsFoundation(Vector3Int cell)
        {
            Vector3 p = planet.transform.TransformPoint((Vector3)cell + Vector3.one * .5f);
            Vector3 dock = Elevator.transform.InverseTransformPoint(p);
            Vector3 shop = Workshop.transform.InverseTransformPoint(p);
            return (dock.x > -3.5f && dock.x < 12.4f && Mathf.Abs(dock.z) < 2 && Mathf.Abs(dock.y) < 2) ||
                (Mathf.Abs(shop.x) < 1.4f && Mathf.Abs(shop.z) < 1 && Mathf.Abs(shop.y) < 1.5f);
        }
        public MaterialId Target { get; private set; }

        private void Awake() => player = GetComponent<RadialPlayer>();

        private void Start()
        {
            hud = GetComponent<UI.EarthHud>();
            Equipment = Instantiate(equipmentPrefab, view.transform, false);
            Equipment.Select(0, Inventory);
            Inventory.Resize(Equipment.Upgrades.Capacity);
            Elevator = Instantiate(elevatorPrefab, planet.transform.parent);
            Elevator.Place(planet);
            Workshop = new GameObject("Werkzeugstation").AddComponent<ToolStation>();
            Workshop.transform.SetParent(planet.transform.parent, false);
            Vector3 direction = (planet.Earth.SpawnDirection * planet.Earth.Radius + new Vector3(-5,0,5)).normalized;
            Vector3 up = planet.transform.TransformDirection(direction);
            Vector3 foot = planet.transform.TransformPoint(direction * planet.Earth.SurfaceRadius(direction));
            if (Physics.Raycast(foot + up*7, -up, out var ground, 14) && ground.collider.GetComponentInParent<VoxelPlanet>() == planet)
                foot = ground.point;
            Workshop.transform.SetPositionAndRotation(foot, Quaternion.LookRotation(Vector3.ProjectOnPlane(planet.transform.forward,up),up));
            Workshop.Assemble(Elevator.Models);
            Beams = new GameObject("Traktorstrahl").AddComponent<BeamNetwork>();
            Beams.transform.SetParent(planet.transform.parent, false);
            Beams.Initialize(planet, Elevator, Elevator.BeamSettings, IsFoundation, Elevator.Models);
            Robot = new GameObject("Maschinen").AddComponent<Machines.DrillRobotUnit>();
            Robot.transform.SetParent(planet.transform.parent, false);
            Robot.Initialize(planet, Elevator.Freight, Beams, Elevator.RobotSettings, IsFoundation, NearBase, Elevator.Models);
            landscape = new GameObject("Landschaft").AddComponent<EarthLandscape>();
            landscape.transform.SetParent(planet.transform.parent, false);
            landscape.Populate(planet, view);
            Pump = new GameObject("Pumpstation").AddComponent<PumpStation>();
            Pump.transform.SetParent(planet.transform.parent, false);
            Pump.Initialize(planet, Elevator.Freight, Beams, Elevator.WaterSettings, landscape.LakeSurface, Elevator.Models);
            debris = new GameObject("Abbausplitter").AddComponent<MiningDebris>();
            debris.transform.SetParent(transform, false);
            debris.Prepare(Equipment.GetComponentInChildren<MeshFilter>().sharedMesh);
            Crew = gameObject.AddComponent<ExpeditionCrew>();
            Crew.Initialize(this);
            feedback = gameObject.AddComponent<Audio.ExpeditionAudio>();
            feedback.Initialize(this);
        }

        private void OnDestroy()
        {
            if (Workshop) Destroy(Workshop.gameObject);
            if (Elevator) Destroy(Elevator.gameObject);
            if (Beams) Destroy(Beams.gameObject);
            if (Robot) Destroy(Robot.gameObject);
            if (Pump) Destroy(Pump.gameObject);
            if (landscape) Destroy(landscape.gameObject);
        }

        public void CloseMenu() => SetMenu(FieldMenu.Closed);

        private void SetMenu(FieldMenu menu)
        {
            Menu = menu;
            player.SetMenuOpen(menu != FieldMenu.Closed);
            Equipment.gameObject.SetActive(menu == FieldMenu.Closed);
        }

        public bool ChooseTool(MiningTool tool)
        {
            if (Menu == FieldMenu.Closed) return false;
            if (Crew.IsGuest) { Crew.Request("equip", (int)tool); return true; }
            if (!Equipment.Tools.Select(tool, Elevator.Freight, Menu == FieldMenu.Workshop)) return false;
            Equipment.Select(0, Inventory);
            return true;
        }

        /// <summary>Buys the next level of an upgrade axis at the workbench (host decides for guests).</summary>
        private void BuildBeam()
        {
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out var ground, BuildReach, ~0,
                QueryTriggerInteraction.Ignore) || ground.collider.GetComponentInParent<VoxelPlanet>() != planet)
            {
                BuildMessage = "Mast braucht festen Boden";
                return;
            }
            if (Crew.IsGuest) { Crew.Request("build", 0); BuildMessage = "Bauauftrag an den Host"; return; }
            BuildMessage = Beams.TryBuild(ground.point, Elevator.Freight, out var reason)
                ? (Beams.MastCount == 0 ? "Sammeltrichter gebaut" : $"Mast {Beams.MastCount} gebaut") : reason;
        }

        /// <summary>True for points inside the protected zone around elevator and workbench.</summary>
        internal bool NearBase(Vector3 world) =>
            Vector3.Distance(world, Elevator.transform.position) < Elevator.RobotSettings.ProtectedRadius ||
            Vector3.Distance(world, Workshop.transform.position) < Elevator.RobotSettings.ProtectedRadius;

        private long BiomassVolume => Elevator.WaterSettings.BiomassPerPlant * HarvestInventory.CubicMetre;

        /// <summary>Takes a plant for good and books its biomass. Host side for both crew members.</summary>
        public bool HarvestPlant(HarvestInventory pack, Vector3Int plant)
        {
            if (pack.Free < BiomassVolume || !landscape.Harvest(plant)) return false;
            pack.TryAdd(MaterialId.Biomass, BiomassVolume);
            return true;
        }

        private void PlaceRobot()
        {
            if (Crew.IsGuest) { Crew.Request(Robot.Robot.Placed ? "robot_remove" : "robot_place", 0); return; }
            if (Robot.Robot.Placed)
            {
                BuildMessage = Robot.TryRemove() ? "Bohrroboter abgebaut" : "Erst Robotertrichter leeren";
                return;
            }
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out var ground, BuildReach, ~0,
                QueryTriggerInteraction.Ignore) || ground.collider.GetComponentInParent<VoxelPlanet>() != planet)
            {
                BuildMessage = "Roboter braucht festen Boden";
                return;
            }
            BuildMessage = Robot.TryPlace(ground.point, out var reason) ? "Bohrroboter platziert · E startet" : reason;
        }

        private void RemoveBeam()
        {
            if (Crew.IsGuest) { Crew.Request("unbuild", 0); return; }
            BuildMessage = Beams.TryRemoveLast(Elevator.Freight) ? "Letztes Teil abgebaut" : "Abbau erst bei leerem Abschnitt";
        }

        public bool ChooseUpgrade(UpgradeAxis axis)
        {
            if (Menu != FieldMenu.Workshop) return false;
            if (Crew.IsGuest) { Crew.Request("upgrade", (int)axis); return true; }
            return Equipment.Upgrades.TryBuy(axis, Elevator.Freight, true, Inventory);
        }

        private void Update()
        {
            var keys = Keyboard.current;
            var pad = Gamepad.current;
            bool inventoryPressed = keys?.tabKey.wasPressedThisFrame == true || keys?.iKey.wasPressedThisFrame == true || pad?.selectButton.wasPressedThisFrame == true;
            Target = MaterialId.None;
            Bin = null;
            AtWorkshop = false;
            AtHopper = false;
            AimedRobot = null;
            AtPump = false;
            AimingAtPlant = false;
            if (Menu != FieldMenu.Closed)
            {
                if (hud && hud.EditingAddress && keys?.escapeKey.wasPressedThisFrame != true) return;
                if (inventoryPressed || keys?.escapeKey.wasPressedThisFrame == true || pad?.buttonEast.wasPressedThisFrame == true)
                    CloseMenu();
                else if (keys != null)
                {
                    if (keys.digit1Key.wasPressedThisFrame) ChooseTool(MiningTool.Pick);
                    if (keys.digit2Key.wasPressedThisFrame) ChooseTool(MiningTool.ProspectingPick);
                    if (keys.digit3Key.wasPressedThisFrame) ChooseTool(MiningTool.Drill);
                    if (keys.digit4Key.wasPressedThisFrame) ChooseUpgrade(UpgradeAxis.Radius);
                    if (keys.digit5Key.wasPressedThisFrame) ChooseUpgrade(UpgradeAxis.Speed);
                    if (keys.digit6Key.wasPressedThisFrame) ChooseUpgrade(UpgradeAxis.Capacity);
                }
                return;
            }
            if (!player.Captured) return;
            if (inventoryPressed) { SetMenu(FieldMenu.Inventory); return; }
            if (keys?.rKey.wasPressedThisFrame == true || pad?.dpad.right.wasPressedThisFrame == true)
                Profile = (DrillProfile)(((int)Profile + 1) % 3);
            Equipment.ReadSelection(Inventory);
            if (keys?.bKey.wasPressedThisFrame == true || pad?.dpad.up.wasPressedThisFrame == true) BuildBeam();
            if (keys?.nKey.wasPressedThisFrame == true || pad?.dpad.down.wasPressedThisFrame == true) RemoveBeam();
            if (keys?.jKey.wasPressedThisFrame == true || pad?.dpad.left.wasPressedThisFrame == true) PlaceRobot();
            var tool = MiningTools.Spec(Equipment.Tools.Active);
            float reach = Equipment.Slot == 0 ? tool.Reach : InteractReach;
            bool hitSomething = Physics.Raycast(view.transform.position, view.transform.forward, out var hit,
                reach, ~0, QueryTriggerInteraction.Ignore);
            if (Equipment.Slot == 0 && landscape.TryFindPlant(view.transform.position, view.transform.forward,
                hitSomething ? hit.distance + .3f : reach, out var plant))
            {
                AimingAtPlant = true;
                bool chop = tool.RepeatSeconds > 0 ? player.MineHeld && Time.time >= nextStrike : player.MinePressed;
                if (chop && Inventory.Free >= BiomassVolume)
                {
                    nextStrike = Time.time + Equipment.Upgrades.RepeatSeconds(tool.RepeatSeconds);
                    if (Crew.IsGuest) Crew.Request("plant", 0, plant);
                    else HarvestPlant(Inventory, plant);
                    Equipment.Strike();
                }
                return;
            }
            if (!hitSomething) return;
            if (hit.collider.TryGetComponent(out PumpStation pump))
            {
                if (hit.distance > InteractReach) return;
                AtPump = true;
                if (player.InteractPressed)
                {
                    if (Crew.IsGuest) Crew.Request("pump", 0);
                    else BuildMessage = pump.Use(Inventory);
                    Equipment.Select(Equipment.Slot, Inventory);
                }
                return;
            }
            if (hit.collider.TryGetComponent(out ToolStation station))
            {
                AtWorkshop = station == Workshop && hit.distance <= InteractReach;
                if (AtWorkshop && player.InteractPressed) SetMenu(FieldMenu.Workshop);
                return;
            }
            if (hit.collider.TryGetComponent(out Machines.DrillRobotMarker marker))
            {
                if (hit.distance > InteractReach) return;
                AimedRobot = marker.Unit;
                if (player.InteractPressed)
                {
                    if (Crew.IsGuest) Crew.Request("robot", 0);
                    else BuildMessage = marker.Unit.Use(Inventory);
                    Equipment.Select(Equipment.Slot, Inventory);
                }
                return;
            }
            if (hit.collider.TryGetComponent(out BeamHopper _))
            {
                if (hit.distance > InteractReach) return;
                AtHopper = true;
                if (player.InteractPressed)
                {
                    if (Crew.IsGuest) Crew.Request("hopper", 0);
                    else Beams.LoadHopper(Inventory);
                    Equipment.Select(Equipment.Slot, Inventory);
                }
                return;
            }
            if (hit.collider.TryGetComponent(out EarthCargoBin bin))
            {
                if (hit.distance > InteractReach) return;
                Bin = bin;
                if (player.InteractPressed || (player.MinePressed && Equipment.HeldMaterial == bin.Material))
                {
                    if (Crew.IsGuest) Crew.Request("load", (int)bin.Material);
                    else if (Elevator.Freight.Load(Inventory, bin.Material) == 0) Elevator.Freight.Dispatch(bin.Material);
                    Equipment.Select(Equipment.Slot, Inventory);
                }
                return;
            }
            if (hit.collider.GetComponentInParent<VoxelPlanet>() != planet) return;
            var cell = planet.CellAt(hit);
            Target = planet.Data.MaterialAt(cell);
            bool strike = tool.RepeatSeconds > 0 ? player.MineHeld && Time.time >= nextStrike : player.MinePressed;
            if (strike && Equipment.Slot == 0 && Target != MaterialId.None && Inventory.Free >= HarvestInventory.CubicMetre)
            {
                nextStrike = Time.time + Equipment.Upgrades.RepeatSeconds(tool.RepeatSeconds);
                if (Crew.IsGuest) Crew.Request("cut", 0, cell);
                else if (Cut(Inventory, Equipment.Tools.Active, Profile, cell, view.transform.forward,
                    Equipment.Upgrades.RadiusMultiplier).Count == 0) return;
                Equipment.Strike();
                feedback.Contact(Target);
                debris.Burst(hit.point, hit.normal, (hit.point - planet.transform.position).normalized, Target);
            }
        }
    }
}
