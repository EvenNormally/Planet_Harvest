using UnityEngine;

namespace PlanetHarvest.Art
{
    // The owner supplies one shared material for all pieces of a model.
    internal static class FieldProps
    {
        private static readonly int Tint = Shader.PropertyToID("_Tint");
        public static Material Finish() => new Material(Resources.Load<Shader>("FieldFinish"));

        public static GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 position,
            Vector3 scale, Color color, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            var props = new MaterialPropertyBlock();
            props.SetColor(Tint, color);
            renderer.SetPropertyBlock(props);
            return part;
        }
    }
}
