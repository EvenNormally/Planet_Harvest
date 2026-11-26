using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetHarvest.UI
{
    [RequireComponent(typeof(EarthMining))]
    public sealed class EarthHud : MonoBehaviour
    {
        private static readonly Color32 Ink = new Color32(24,43,49,245);
        private static readonly Color32 Slate = new Color32(52,76,79,248);
        private static readonly Color32 Paper = new Color32(246,244,222,255);
        private static readonly Color32 Muted = new Color32(186,204,200,255);
        private static readonly Color32 Mint = new Color32(128,229,190,255);
        private static readonly MaterialId[] Materials = { MaterialId.None, MaterialId.Soil, MaterialId.Stone, MaterialId.IronOre };
        private static readonly string[] Names = { "Feldhacke", "Erde", "Stein", "Eisenerz" };
        private readonly Image[] outlines = new Image[4];
        private readonly TextMeshProUGUI[] quantities = new TextMeshProUGUI[4];
        private EarthMining mining;
        private TMP_FontAsset font;
        private TextMeshProUGUI level, title, instruction, progress, soil, stone, flight, hint;
        private Image bar;
        private float nextRefresh;
        private (int slot, MaterialId target, MaterialId bin, long soil, long stone, long iron, long loadedSoil,
            long loadedStone, int levels, MaterialId flying, bool returning) last;
        private bool initialized;

        private void Start()
        {
            mining = GetComponent<EarthMining>();
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var canvas = new GameObject("Feldanzeige", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720);
            scaler.matchWidthOrHeight = .5f;
            var root = (RectTransform)canvas.transform;

            var order = Panel(root, "Lieferauftrag", new Vector2(0,1), new Vector2(22,-20), new Vector2(300,126), Ink);
            Box(order, 0,0,4,126,Mint);
            level = Label(order, 17,9,265,17,12,Mint,true);
            title = Label(order, 16,28,267,33,27,Paper,true);
            instruction = Label(order, 17,65,270,23,16,Paper);
            Box(order, 17,102,168,6,Slate);
            bar = Box(order, 17,102,168,6,Mint);
            progress = Label(order, 190,91,94,28,15,Muted,true,TextAlignmentOptions.MidlineRight);

            var dock = Panel(root,"Orbitallift", Vector2.one,new Vector2(-22,-20),new Vector2(202,100),Ink);
            Label(dock,14,8,174,19,13,Mint,true).text = "SPACE ELEVATOR";
            soil = Label(dock,14,32,174,17,15,Paper);
            stone = Label(dock,14,52,174,17,15,Paper);
            flight = Label(dock,14,75,174,18,13,Muted);

            var bottom = new Vector2(.5f,0);
            var belt = Panel(root,"Ausrüstung",bottom,new Vector2(0,14),new Vector2(362,79),Ink);
            for (int i = 0; i < 4; i++)
            {
                int x = 5 + i * 89;
                outlines[i] = Box(belt,x,5,85,69,Slate);
                var tile = Box(belt,x+2,7,81,65,Ink).rectTransform;
                Label(tile,5,2,17,17,12,Muted,true).text = (i+1).ToString();
                var glyph = Rect(tile,"Symbol",new Vector2(0,1),new Vector2(22,-5),new Vector2(34,34))
                    .gameObject.AddComponent<EquipmentGlyph>();
                glyph.Slot = i;
                glyph.raycastTarget = false;
                quantities[i] = Label(tile,49,2,27,16,13,Paper,true,TextAlignmentOptions.MidlineRight);
                Label(tile,0,41,81,21,14,Paper,true,TextAlignmentOptions.Center).text = Names[i];
            }
            var help = Panel(root,"Aktion",bottom,new Vector2(0,100),new Vector2(368,31),Ink);
            hint = Label(help,4,1,360,28,15,Paper,false,TextAlignmentOptions.Center);
            var centre = new Vector2(.5f,.5f);
            Panel(root,"Visierkontur",centre,Vector2.zero,new Vector2(6,6),Ink);
            Panel(root,"Visier",centre,Vector2.zero,new Vector2(3,3),Paper);
        }

        private void LateUpdate()
        {
            if (mining?.Elevator == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .08f;
            var cargo = mining.Elevator.Freight;
            var pack = mining.Inventory;
            const long unit = HarvestInventory.CubicMetre;
            var state = (mining.Equipment.Slot, mining.Target, mining.Bin ? mining.Bin.Material : MaterialId.None,
                pack.AmountOf(MaterialId.Soil), pack.AmountOf(MaterialId.Stone), pack.AmountOf(MaterialId.IronOre),
                cargo.Container(MaterialId.Soil).Amount, cargo.Container(MaterialId.Stone).Amount,
                cargo.CompletedLevels, cargo.Flying, cargo.Returning);
            if (initialized && last.Equals(state)) return;
            last = state;
            initialized = true;
            bool first = last.levels == 0;
            bool complete = last.levels == 2;
            var required = first ? MaterialId.Soil : MaterialId.Stone;
            int goal = first ? EarthFreight.SoilGoal : EarthFreight.StoneGoal;
            long ready = System.Math.Min(goal,(pack.AmountOf(required) + cargo.Container(required).Amount) / unit);
            level.text = complete ? "AUFTRÄGE ABGESCHLOSSEN" : first ? "LIEFERAUFTRAG    01 / 02" : "LIEFERAUFTRAG    02 / 02";
            title.text = complete ? "Freier Abbau" : first ? "Bodenprobe" : "Gesteinslieferung";
            instruction.text = complete ? "Der Elevator nimmt weiter Fracht an." : $"{goal} m³ {required.Name()} in den Orbit liefern.";
            bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,complete ? 168 : 168f * ready / goal);
            progress.text = complete ? "2 / 2" : $"{ready} / {goal} m³";
            soil.text = $"Erde     {last.loadedSoil / unit} / {EarthFreight.SoilGoal} m³";
            stone.text = $"Stein     {last.loadedStone / unit} / {EarthFreight.StoneGoal} m³";
            flight.text = last.flying == MaterialId.None ? "Voll beladen = Abflug" :
                last.flying.Name() + (last.returning ? " · Rückkehr" : " · Aufstieg");
            for (int i = 0; i < outlines.Length; i++)
            {
                outlines[i].color = i == last.slot ? Mint : Slate;
                if (i > 0) quantities[i].text = (pack.AmountOf(Materials[i]) / unit).ToString();
            }
            if (last.bin != MaterialId.None)
                hint.text = cargo.Container(last.bin).Free == 0 ? "Voll beladen · wartet auf den Lift"
                    : pack.AmountOf(last.bin) > 0 ? $"E   {last.bin.Name()} einladen" : $"{last.bin.Name()}container · kein passender Vorrat";
            else if (last.target != MaterialId.None)
                hint.text = last.slot == 0 ? $"LMB   {last.target.Name()} abbauen" : "1   Feldhacke ausrüsten";
            else hint.text = last.slot == 0 ? "1–4  /  Mausrad    Ausrüstung wechseln" : Names[last.slot] + "   ·   1 für die Feldhacke";
        }

        private static RectTransform Rect(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Panel(RectTransform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {
            var rect = Rect(parent,name,anchor,pos,size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Image Box(RectTransform p,int x,int y,int w,int h,Color tint) =>
            Panel(p,"Fläche",Vector2.up,new Vector2(x,-y),new Vector2(w,h),tint).GetComponent<Image>();

        private TextMeshProUGUI Label(RectTransform p,int x,int y,int w,int h,int size,Color tint,
            bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var label = Rect(p,"Text",Vector2.up,new Vector2(x,-y),new Vector2(w,h)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = tint;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
