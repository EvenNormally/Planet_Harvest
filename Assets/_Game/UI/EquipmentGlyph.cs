using UnityEngine;
using UnityEngine.UI;

namespace PlanetHarvest.UI
{
    // Flat, two-tone silhouettes stay legible at hotbar size.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EquipmentGlyph : MaskableGraphic
    {
        public int Slot { get; set; }
        private static readonly Color32 Light = new Color32(241,235,204,255);
        private static readonly Color32 Mint = new Color32(109,220,186,255);
        private static readonly Color32 Bark = new Color32(178,102,58,255);
        private static readonly Color32 Clay = new Color32(228,157,89,255);
        private static readonly Color32 Blue = new Color32(122,153,179,255);
        private static readonly Color32 Silver = new Color32(188,213,219,255);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Slot == 0)
            {
                Face(mesh, Mint, -15,-14, -11,-17, 9,10, 5,13);
                Face(mesh, Light, -11,12, 1,15, 12,9, 16,-1, 8,5, 0,10);
                Face(mesh, Bark, 1,11, 5,15, 9,11, 6,7);
                return;
            }
            var baseColor = Slot == 2 ? Blue : Bark;
            Face(mesh, baseColor, -16,-8, -11,6, 0,12, 12,7, 17,-5, 7,-12, -6,-13);
            Face(mesh, Slot == 2 ? Silver : Clay, -11,6, 0,12, 12,7, 3,3, -5,1);
            if (Slot == 1) Face(mesh, Mint, -11,6, 0,12, 12,7, 4,5, -3,6, -7,4);
            if (Slot == 3)
            {
                Face(mesh, Clay, -10,-5, -5,1, 0,0, -5,-7);
                Face(mesh, Light, 4,-5, 9,2, 13,1, 8,-7);
            }
        }

        private void Face(VertexHelper mesh, Color32 tint, params float[] xy)
        {
            var rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) / 36;
            int first = mesh.currentVertCount;
            for (int i = 0; i < xy.Length; i += 2)
                mesh.AddVert(rect.center + new Vector2(xy[i], xy[i + 1]) * scale, tint, Vector2.zero);
            for (int i = 1; i < xy.Length / 2 - 1; i++) mesh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
