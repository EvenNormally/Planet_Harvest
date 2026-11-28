using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EarthCargoBin : MonoBehaviour
    {
        [SerializeField] private MaterialId material;
        public MaterialId Material => material;
        private Vector3 rest;
        private BoxCollider hitBox;

        private void Awake()
        {
            rest = transform.localPosition;
            hitBox = GetComponent<BoxCollider>();
        }

        internal void ConfigureOre(MaterialSpec spec, Vector3 position)
        {
            material = spec.Id;
            rest = transform.localPosition = position;
            name = spec.Name + "container";
            var label = GetComponentInChildren<TMPro.TextMeshPro>();
            label.text = spec.Name.ToUpperInvariant();
            var face = transform.Find("Frachtkörper").GetComponent<Renderer>();
            var colors = new MaterialPropertyBlock();
            colors.SetColor("_BaseColor", spec.ContainerColor);
            face.SetPropertyBlock(colors);
        }

        private Transform contents, cargoHeap;
        private float lastFill = -1;

        internal void BuildGauge(Material hardware, ContainerKind kind, Art.ModelLibrary models = null)
        {
            // The material-specific model replaces the generic body; the fill gauge below stays data driven.
            var model = models ? models.SpawnContainer(kind, transform, hardware) : null;
            if (model)
            {
                var body = transform.Find("Frachtkörper");
                if (body && body.TryGetComponent(out Renderer shell)) shell.enabled = false;
            }
            var baseColor = material == MaterialId.Soil ? new Color(.46f,.28f,.13f) :
                material == MaterialId.Stone ? new Color(.58f,.61f,.65f) : new Color(.78f,.55f,.36f);
            Art.FieldProps.Part(transform, "Schauglas", PrimitiveType.Cube, new Vector3(0,-.37f,-.70f),
                new Vector3(1.16f,.29f,.045f), new Color(.09f,.15f,.18f), hardware);
            contents = Art.FieldProps.Part(transform, "Ladestand", PrimitiveType.Cube, Vector3.zero,
                Vector3.one, baseColor, hardware).transform;
            bool rock = kind == ContainerKind.Barrel;
            var cradle = rock ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            Art.FieldProps.Part(transform, rock ? "Fassring" : "Presswanne", cradle, new Vector3(0,.73f,0),
                rock ? new Vector3(1.24f,.08f,1.24f) : new Vector3(1.43f,.12f,1.27f),
                new Color(.24f,.33f,.36f), hardware);
            if (kind == ContainerKind.GlassSphere)
                Art.FieldProps.Part(transform, "Glaskugel", PrimitiveType.Sphere, new Vector3(0,1.15f,0), Vector3.one * 1.15f,
                    new Color(.7f,.88f,1f), hardware);
            else if (kind == ContainerKind.OrganicTank)
                Art.FieldProps.Part(transform, "Organischer Glastank", PrimitiveType.Cylinder, new Vector3(0,1.15f,0),
                    new Vector3(1.05f,.45f,1.05f), new Color(.55f,.85f,.45f), hardware);
            cargoHeap = Art.FieldProps.Part(transform, rock ? "Steinhaufen" : "Pressladung",
                rock ? PrimitiveType.Sphere : PrimitiveType.Cube, Vector3.zero, Vector3.one, baseColor, hardware).transform;
            SetFill(0);
        }

        internal void SetFill(float fill)
        {
            if (fill == lastFill) return;
            lastFill = fill;
            contents.gameObject.SetActive(fill > 0);
            cargoHeap.gameObject.SetActive(fill > 0);
            cargoHeap.localScale = new Vector3(1.12f,.62f * fill,1.04f);
            cargoHeap.localPosition = new Vector3(0,.79f + .31f * fill,0);
            contents.localScale = new Vector3(1.08f,.24f * fill,.03f);
            contents.localPosition = new Vector3(0,-.49f + .12f * fill,-.74f);
        }

        internal void SetHeight(float metres, bool atDock)
        {
            transform.localPosition = rest + Vector3.up * metres;
            hitBox.enabled = atDock;
        }
    }
}
