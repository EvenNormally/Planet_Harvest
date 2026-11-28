using PlanetHarvest.Mining;
using PlanetHarvest.Art;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetHarvest.Player
{
    public sealed class FieldEquipment : MonoBehaviour
    {
        [SerializeField] private GameObject[] items;
        private static readonly Key[] Keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        private static readonly MaterialId[] Materials = { MaterialId.None, MaterialId.Soil, MaterialId.Stone, MaterialId.IronOre };
        public bool ShowingTool => Slot == 0 && models[(int)Tools.Active].activeSelf;
        public int Slot { get; private set; }
        public MaterialId HeldMaterial => Materials[Slot];
        public MiningTools Tools { get; } = new MiningTools();
        private readonly GameObject[] models = new GameObject[3];
        private readonly Transform[] spindles = new Transform[3];
        private Material finish;
        private float swing, draw;
        private Quaternion rest;
        private Vector3 restPosition;

        private void Awake()
        {
            rest = transform.localRotation;
            restPosition = transform.localPosition;
            finish = FieldProps.Finish();
            items[0].SetActive(false);
            var drill = new GameObject("Kernbohrer");
            drill.transform.SetParent(transform, false);
            models[2] = drill;
            Color mint = new Color(.25f,.77f,.64f), dark = new Color(.16f,.23f,.26f);
            FieldProps.Part(drill.transform, "Griff", PrimitiveType.Cube, new Vector3(0,-.08f,0),
                new Vector3(.063f,.20f,.085f), dark, finish);
            FieldProps.Part(drill.transform, "Motor", PrimitiveType.Cube, new Vector3(0,.07f,.02f),
                new Vector3(.15f,.145f,.24f), mint, finish);
            Color orange = new Color(.98f,.57f,.16f);
            FieldProps.Part(drill.transform, "Motorschutz", PrimitiveType.Cube, new Vector3(0,.13f,-.035f),
                new Vector3(.165f,.05f,.19f), orange, finish);
            FieldProps.Part(drill.transform, "Griffbügel", PrimitiveType.Cube, new Vector3(0,.19f,-.025f),
                new Vector3(.035f,.03f,.125f), dark, finish);
            for (int side = -1; side <= 1; side += 2)
            {
                FieldProps.Part(drill.transform, "Bügelstütze", PrimitiveType.Cube, new Vector3(0,.16f,-.025f + side*.06f),
                    new Vector3(.035f,.07f,.025f), dark, finish);
                FieldProps.Part(drill.transform, "Anzeige", PrimitiveType.Cube, new Vector3(side*.078f,.072f,-.025f),
                    new Vector3(.009f,.044f,.067f), new Color(.25f,1,.89f), finish);
            }
            var chuck = FieldProps.Part(drill.transform, "Bohrfutter", PrimitiveType.Cylinder, new Vector3(0,.07f,.14f),
                new Vector3(.127f,.033f,.127f), orange, finish);
            chuck.transform.localRotation = Quaternion.Euler(90,0,0);
            var spindle = new GameObject("Spindel").transform;
            spindle.SetParent(drill.transform, false);
            spindle.localPosition = new Vector3(0,.07f,.13f);
            for (int i = 0; i < 6; i++)
            {
                float width = .085f - i*.011f;
                var tooth = FieldProps.Part(spindle, "Schneide", PrimitiveType.Cube, Vector3.forward * (.02f+i*.031f),
                    new Vector3(width,width,.048f), i%2==0 ? Color.gray : dark, finish);
                tooth.transform.localRotation = Quaternion.Euler(0,0,i*28);
            }
            // Reuse the drill assembly for the starter and reach upgrade.
            models[0] = Instantiate(drill, transform);
            models[0].name = "Handbohrer";
            models[0].transform.localScale = Vector3.one * .82f;
            models[1] = Instantiate(drill, transform);
            models[1].name = "Langbohrer";
            models[1].transform.localScale = new Vector3(.92f,.92f,1.25f);
            models[1].SetActive(false);
            for (int i = 0; i < models.Length; i++) spindles[i] = models[i].transform.Find("Spindel");
            drill.SetActive(false);
        }

        private void OnDestroy() => Destroy(finish);

        public void Select(int slot, HarvestInventory pack)
        {
            Slot = Mathf.Clamp(slot, 0, items.Length - 1);
            draw = .16f;
            Refresh(pack);
        }

        public void Refresh(HarvestInventory pack)
        {
            for (int i = 1; i < items.Length; i++)
                items[i].SetActive(i == Slot && pack.AmountOf(Materials[i]) > 0);
            for (int i = 0; i < models.Length; i++)
                models[i].SetActive(Slot == 0 && i == (int)Tools.Active);
        }

        public void ReadSelection(HarvestInventory pack)
        {
            int next = Slot;
            var keyboard = Keyboard.current;
            for (int i = 0; i < Keys.Length; i++)
                if (keyboard != null && keyboard[Keys[i]].wasPressedThisFrame) next = i;
            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0;
            var pad = Gamepad.current;
            if (scroll < 0 || (pad != null && pad.rightShoulder.wasPressedThisFrame)) next++;
            if (scroll > 0 || (pad != null && pad.leftShoulder.wasPressedThisFrame)) next--;
            next = (next + items.Length) % items.Length;
            if (next != Slot) Select(next, pack);
        }

        public void Strike() => swing = 0.28f;

        private void LateUpdate()
        {
            swing = Mathf.Max(0, swing - Time.deltaTime);
            draw = Mathf.Max(0, draw - Time.deltaTime);
            float phase = 1 - swing / .28f;
            float arc = Mathf.Sin(Mathf.PI * Mathf.Pow(phase, .67f));
            float lift = Mathf.Sin(Mathf.PI * draw / .16f);
            bool drill = Slot == 0;
            if (drill && swing > 0) spindles[(int)Tools.Active].Rotate(0,0,Time.deltaTime * 1050);
            transform.localPosition = restPosition + new Vector3(0, -.075f*lift - .025f*arc, (drill ? .06f : .032f)*arc);
            transform.localRotation = rest * Quaternion.Euler((drill ? 4 : 38)*arc + 12*lift,-14*arc,7*arc);
        }
    }
}
