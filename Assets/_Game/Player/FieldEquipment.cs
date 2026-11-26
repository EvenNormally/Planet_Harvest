using PlanetHarvest.Mining;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetHarvest.Player
{
    public sealed class FieldEquipment : MonoBehaviour
    {
        [SerializeField] private GameObject[] items;
        private static readonly Key[] Keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        private static readonly MaterialId[] Materials = { MaterialId.None, MaterialId.Soil, MaterialId.Stone, MaterialId.IronOre };
        public int Slot { get; private set; }
        public MaterialId HeldMaterial => Materials[Slot];
        private float swing;
        private Quaternion rest;

        private void Awake() => rest = transform.localRotation;

        public void Select(int slot, HarvestInventory pack)
        {
            Slot = Mathf.Clamp(slot, 0, items.Length - 1);
            for (int i = 0; i < items.Length; i++)
                items[i].SetActive(i == Slot && (i == 0 || pack.AmountOf(Materials[i]) > 0));
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
            if (swing <= 0) return;
            swing = Mathf.Max(0, swing - Time.deltaTime);
            float arc = Mathf.Sin(Mathf.PI * swing / 0.28f);
            transform.localRotation = rest * Quaternion.Euler(28 * arc, -12 * arc, 4 * arc);
        }
    }
}
