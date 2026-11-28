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
    public enum FieldMenu { Closed, Inventory, Workshop }

    [DefaultExecutionOrder(-10), RequireComponent(typeof(RadialPlayer))]
    public sealed class EarthMining : MonoBehaviour
    {
        [SerializeField] private VoxelMoon planet;
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
        public bool AtWorkshop { get; private set; }
        public FieldMenu Menu { get; private set; }
        public EarthCargoBin Bin { get; private set; }
        public const long PackCapacity = 24 * HarvestInventory.CubicMetre;
        public HarvestInventory Inventory { get; } = new HarvestInventory(PackCapacity);
        public DrillProfile Profile { get; internal set; }
        public ExpeditionCrew Crew { get; private set; }
        public VoxelMoon Planet => planet;
        internal Camera View => view;
        private readonly List<Vector3Int> cut = new List<Vector3Int>(64);
        internal List<Vector3Int> Cut(HarvestInventory pack, MiningTool tool, DrillProfile profile,
            Vector3Int cell, Vector3 forward)
        {
            float scale = tool == MiningTool.Pick ? 1 : tool == MiningTool.Drill ? 1.45f : 1.2f;
            DrillStroke.Cut(planet, pack, cell, planet.transform.InverseTransformDirection(forward),
                profile, scale, IsFoundation, cut);
            return cut;
        }

        private bool IsFoundation(Vector3Int cell)
        {
            Vector3 p = planet.transform.TransformPoint((Vector3)cell + Vector3.one * .5f);
            Vector3 dock = Elevator.transform.InverseTransformPoint(p);
            Vector3 shop = Workshop.transform.InverseTransformPoint(p);
            return (dock.x > -3.5f && dock.x < 8.6f && Mathf.Abs(dock.z) < 2 && Mathf.Abs(dock.y) < 2) ||
                (Mathf.Abs(shop.x) < 1.4f && Mathf.Abs(shop.z) < 1 && Mathf.Abs(shop.y) < 1.5f);
        }
        public MaterialId Target { get; private set; }

        private void Awake() => player = GetComponent<RadialPlayer>();

        private void Start()
        {
            hud = GetComponent<UI.EarthHud>();
            Equipment = Instantiate(equipmentPrefab, view.transform, false);
            Equipment.Select(0, Inventory);
            Elevator = Instantiate(elevatorPrefab, planet.transform.parent);
            Elevator.Place(planet);
            Workshop = new GameObject("Werkzeugstation").AddComponent<ToolStation>();
            Workshop.transform.SetParent(planet.transform.parent, false);
            Vector3 direction = (planet.Earth.SpawnDirection * planet.Earth.Radius + new Vector3(-5,0,5)).normalized;
            Vector3 up = planet.transform.TransformDirection(direction);
            Vector3 foot = planet.transform.TransformPoint(direction * planet.Earth.SurfaceRadius(direction));
            if (Physics.Raycast(foot + up*7, -up, out var ground, 14) && ground.collider.GetComponentInParent<VoxelMoon>() == planet)
                foot = ground.point;
            Workshop.transform.SetPositionAndRotation(foot, Quaternion.LookRotation(Vector3.ProjectOnPlane(planet.transform.forward,up),up));
            Workshop.Assemble();
            landscape = new GameObject("Landschaft").AddComponent<EarthLandscape>();
            landscape.transform.SetParent(planet.transform.parent, false);
            landscape.Populate(planet, view);
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

        private void Update()
        {
            var keys = Keyboard.current;
            var pad = Gamepad.current;
            bool inventoryPressed = keys?.tabKey.wasPressedThisFrame == true || keys?.iKey.wasPressedThisFrame == true || pad?.selectButton.wasPressedThisFrame == true;
            Target = MaterialId.None;
            Bin = null;
            AtWorkshop = false;
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
                }
                return;
            }
            if (!player.Captured) return;
            if (inventoryPressed) { SetMenu(FieldMenu.Inventory); return; }
            if (keys?.rKey.wasPressedThisFrame == true || pad?.dpad.right.wasPressedThisFrame == true)
                Profile = (DrillProfile)(((int)Profile + 1) % 3);
            Equipment.ReadSelection(Inventory);
            var tool = MiningTools.Spec(Equipment.Tools.Active);
            float reach = Equipment.Slot == 0 ? tool.Reach : HarvestInteraction.Reach;
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out var hit,
                reach, ~0, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider.TryGetComponent(out ToolStation station))
            {
                AtWorkshop = station == Workshop && hit.distance <= HarvestInteraction.Reach;
                if (AtWorkshop && player.InteractPressed) SetMenu(FieldMenu.Workshop);
                return;
            }
            if (hit.collider.TryGetComponent(out EarthCargoBin bin))
            {
                if (hit.distance > HarvestInteraction.Reach) return;
                Bin = bin;
                if (player.InteractPressed || (player.MinePressed && Equipment.HeldMaterial == bin.Material))
                {
                    if (Crew.IsGuest) Crew.Request("load", (int)bin.Material);
                    else if (Elevator.Freight.Load(Inventory, bin.Material) == 0) Elevator.Freight.Dispatch(bin.Material);
                    Equipment.Select(Equipment.Slot, Inventory);
                }
                return;
            }
            if (hit.collider.GetComponentInParent<VoxelMoon>() != planet) return;
            var cell = planet.CellAt(hit);
            Target = planet.Data.MaterialAt(cell);
            bool strike = tool.RepeatSeconds > 0 ? player.MineHeld && Time.time >= nextStrike : player.MinePressed;
            if (strike && Equipment.Slot == 0 && Target != MaterialId.None && Inventory.Free >= HarvestInventory.CubicMetre)
            {
                nextStrike = Time.time + tool.RepeatSeconds;
                if (Crew.IsGuest) Crew.Request("cut", 0, cell);
                else if (Cut(Inventory, Equipment.Tools.Active, Profile, cell, view.transform.forward).Count == 0) return;
                Equipment.Strike();
                feedback.Contact(Target);
                debris.Burst(hit.point, hit.normal, (hit.point - planet.transform.position).normalized, Target);
            }
        }
    }
}
