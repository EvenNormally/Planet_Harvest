using PlanetHarvest.Art;
using TMPro;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    public sealed class ToolStation : MonoBehaviour
    {
        private Material hardware;
        private Transform lamp;
        private float glow;

        public void Assemble(ModelLibrary models = null)
        {
            hardware = FieldProps.Finish();
            Color graphite = new Color(.17f,.24f,.27f), mint = new Color(.32f,.85f,.68f);
            void Block(string label, Vector3 pos, Vector3 size, Color tint)
                => FieldProps.Part(transform, label, PrimitiveType.Cube, pos, size, tint, hardware);

            var model = models ? models.Spawn(GameModel.Workbench, transform, hardware) : null;
            lamp = ModelLibrary.Find(model, "Statusleuchte");
            if (!model) BuildPrimitives(Block, graphite, mint);
            var sign = new GameObject("Werkstatt-Schild").AddComponent<TextMeshPro>();
            sign.transform.SetParent(transform, false);
            sign.transform.localPosition = new Vector3(0,1.89f,.38f);
            sign.rectTransform.sizeDelta = new Vector2(2.2f,.55f);
            sign.text = "WERKZEUGE";
            sign.fontSize = 2.6f;
            sign.fontStyle = FontStyles.Bold;
            sign.alignment = TextAlignmentOptions.Center;
            sign.color = new Color(.88f,.97f,.83f);
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0,1.18f,0);
            box.size = new Vector3(2.55f,2.4f,1.65f);
        }

        // A gently pulsing status light makes the workbench read as a live device.
        private void Update()
        {
            if (!lamp) return;
            glow += Time.deltaTime * 2.4f;
            lamp.localScale = Vector3.one * (1 + .18f * Mathf.Sin(glow));
        }

        private static void BuildPrimitives(System.Action<string, Vector3, Vector3, Color> Block, Color graphite, Color mint)
        {
            Block("Sockel", new Vector3(0,.12f,0), new Vector3(2.6f,.34f,1.7f), graphite);
            Block("Werkbank", new Vector3(0,.67f,0), new Vector3(2.05f,1.12f,1.12f), new Color(.68f,.39f,.16f));
            Block("Arbeitsplatte", new Vector3(0,1.28f,0), new Vector3(2.45f,.16f,1.4f), graphite);
            Block("Rückwand", new Vector3(0,1.78f,.48f), new Vector3(2.26f,1.02f,.13f), graphite);
            Block("Lichtleiste", new Vector3(0,2.32f,.46f), new Vector3(2.3f,.06f,.19f), mint);
            Block("Bohrermotor", new Vector3(.36f,1.49f,-.14f), new Vector3(.48f,.25f,.3f), mint);
            Block("Bohrspindel", new Vector3(-.07f,1.49f,-.14f), new Vector3(.47f,.09f,.1f), Color.gray);
        }

        private void OnDestroy() => Destroy(hardware);
    }
}
