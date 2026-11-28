using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.Player;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
        private static readonly string[] Names = { "Handbohrer", "Erde", "Stein", "Eisenerz" };
        private readonly Image[] outlines = new Image[4];
        private readonly TextMeshProUGUI[] quantities = new TextMeshProUGUI[4];
        private EarthMining mining;
        private TMP_FontAsset font;
        private TextMeshProUGUI level, title, instruction, progress, soil, stone, flight, hint;
        private Image bar;
        private TextMeshProUGUI telemetry, crewStatus, crewButton;
        private TMP_InputField address;
        private TextMeshProUGUI beltTool, menuTitle, wallet, holdings, footer;
        private readonly TextMeshProUGUI[] toolLabels = new TextMeshProUGUI[MiningTools.Count];
        private readonly Button[] toolButtons = new Button[MiningTools.Count];
        private readonly TextMeshProUGUI[] upgradeLabels = new TextMeshProUGUI[UpgradeCatalog.AxisCount];
        private readonly Button[] upgradeButtons = new Button[UpgradeCatalog.AxisCount];
        private RectTransform modal, gameplay;
        private FieldMenu displayedMenu;
        private EventSystem events;
        private float nextRefresh;
        private (int slot, MaterialId target, MaterialId bin, bool shop, int inventory, int freight,
            MiningTool active, FieldMenu menu, int upgrades, int machine) last;
        public bool EditingAddress => address && address.isFocused;
        private bool initialized, showPerformance;
        private int measuredFrames;
        private float sampleTime, fps;

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current?.f3Key.wasPressedThisFrame == true) showPerformance = !showPerformance;
            sampleTime += Time.unscaledDeltaTime;
            measuredFrames++;
            if (sampleTime < .5f) return;
            fps = measuredFrames / sampleTime;
            sampleTime = 0;
            measuredFrames = 0;
        }

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
            var screen = (RectTransform)canvas.transform;
            canvas.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)new GameObject("Spielanzeige", typeof(RectTransform)).transform;
            root.SetParent(screen, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            gameplay = root;

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
                var caption = Label(tile,0,41,81,21,14,Paper,true,TextAlignmentOptions.Center);
                caption.text = Names[i];
                if (i == 0) beltTool = caption;
            }
            var help = Panel(root,"Aktion",bottom,new Vector2(0,100),new Vector2(368,31),Ink);
            hint = Label(help,4,1,360,28,15,Paper,false,TextAlignmentOptions.Center);
            var centre = new Vector2(.5f,.5f);
            Panel(root,"Visierkontur",centre,Vector2.zero,new Vector2(6,6),Ink);
            Panel(root,"Visier",centre,Vector2.zero,new Vector2(3,3),Paper);
            var pocket = Panel(root, "Inventartaste", Vector2.zero, new Vector2(22,20), new Vector2(300,116), Ink);
            Label(pocket,10,4,250,23,14,Paper,true).text = "Tab  Inventar & Crew     R  Bohrform";
            telemetry = Label(pocket,10,30,284,80,13,Muted,false,TextAlignmentOptions.TopLeft);
            BuildMenu(screen);
        }

        private void BuildMenu(RectTransform root)
        {
            events = EventSystem.current;
            if (!events)
            {
                var host = new GameObject("Menü-Eingabe",typeof(EventSystem),typeof(InputSystemUIInputModule));
                host.transform.SetParent(root,false);
                events = host.GetComponent<EventSystem>();
            }
            modal = Panel(root,"Feldmenü",new Vector2(.5f,.5f),Vector2.zero,new Vector2(590,652),Ink);
            Box(modal,0,0,590,4,Mint);
            menuTitle = Label(modal,22,16,320,42,30,Paper,true);
            wallet = Label(modal,346,21,218,28,18,Mint,true,TextAlignmentOptions.MidlineRight);
            Label(modal,22,78,183,23,14,Mint,true).text = "ROHSTOFFE";
            Label(modal,221,78,340,23,14,Mint,true).text = "WERKZEUGE";
            holdings = Label(modal,22,110,188,170,17,Paper,false,TextAlignmentOptions.TopLeft);
            for (int i = 0; i < MiningTools.Count; i++)
            {
                var tool = (MiningTool)i;
                var row = Box(modal,221,107+i*59,347,52,Slate);
                toolButtons[i] = MenuButton(row, () => mining.ChooseTool(tool));
                toolLabels[i] = Label(row.rectTransform,10,3,330,46,15,Paper);
            }
            Label(modal,221,292,340,23,14,Mint,true).text = "UPGRADES";
            for (int i = 0; i < upgradeLabels.Length; i++)
            {
                var axis = (UpgradeAxis)i;
                var row = Box(modal,221,320+i*44,347,40,Slate);
                upgradeButtons[i] = MenuButton(row, () => mining.ChooseUpgrade(axis));
                upgradeLabels[i] = Label(row.rectTransform,10,2,330,36,13,Paper);
            }
            footer = Label(modal,22,450,548,28,15,Muted);
            var close = Box(modal,22,492,160,32,Slate);
            MenuButton(close, () => mining.CloseMenu());
            Label(close.rectTransform,5,1,150,30,15,Paper,true,TextAlignmentOptions.Center).text = "Schließen";
            Label(modal,209,494,360,26,14,Muted,false,TextAlignmentOptions.MidlineRight).text = "1–3  Werkzeug   ·   4–6  Upgrade   ·   Esc  Zurück";
            crewStatus = Label(modal,22,531,548,21,14,Mint);
            address = AddressField(modal);
            var hostButton = Box(modal,22,562,150,33,Slate);
            MenuButton(hostButton, () =>
            {
                if (mining.Crew.Hosting || mining.Crew.IsGuest) mining.Crew.StopSession();
                else mining.Crew.Host();
            });
            crewButton = Label(hostButton.rectTransform,4,1,142,31,14,Paper,true,TextAlignmentOptions.Center);
            var join = Box(modal,414,562,154,33,Slate);
            MenuButton(join, () => mining.Crew.Join(address.text));
            Label(join.rectTransform,4,1,146,31,14,Paper,true,TextAlignmentOptions.Center).text = "IP beitreten";
            Label(modal,22,605,548,31,14,Muted).text = "F5  Speichern     F9  Laden     ·     Spielstand liegt beim Host";
            modal.gameObject.SetActive(false);
        }

        private TMP_InputField AddressField(RectTransform panel)
        {
            var field = Box(panel,181,562,224,33,Slate);
            field.raycastTarget = true;
            var input = field.gameObject.AddComponent<TMP_InputField>();
            var value = Label(field.rectTransform,8,0,208,33,16,Paper);
            input.textViewport = field.rectTransform;
            input.textComponent = value;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.characterLimit = 45;
            input.text = "127.0.0.1";
            return input;
        }

        private static Button MenuButton(Image image, UnityEngine.Events.UnityAction action)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.selectedColor = new Color(.68f,1,.82f);
            colors.disabledColor = new Color(.70f,.70f,.70f);
            button.colors = colors;
            button.onClick.AddListener(action);
            return button;
        }

        private void LateUpdate()
        {
            if (mining?.Elevator == null) return;
            bool menuChanged = displayedMenu != mining.Menu;
            if (!menuChanged && Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .08f;
            var cargo = mining.Elevator.Freight;
            var pack = mining.Inventory;
            const long unit = HarvestInventory.CubicMetre;
            long carried = pack.Amount + (mining.Crew.Paired ? mining.Crew.PartnerLoad : 0);
            var planet = PlanetProgress.Measure(mining.Planet.Data.InitialCount, cargo, carried);
            telemetry.text = $"Rucksack   {pack.Amount / unit} / {pack.Capacity / unit} m³   ·   {DrillStroke.Name(mining.Profile)}\nIm Flug {planet.InFlight / unit} m³   ·   am Lift {planet.AtElevator / unit} m³\n{BeamStatus()}\n{mining.Crew.Status}";
            if (!string.IsNullOrEmpty(mining.BuildMessage)) telemetry.text += $"   ·   {mining.BuildMessage}";
            soil.text = $"Planet im Orbit   {planet.OrbitFraction * 100:F4} %";
            if (showPerformance) telemetry.text += $"   ·   {fps:F0} FPS";
            crewStatus.text = mining.Crew.Paired ? $"{mining.Crew.Status}    ·    Partner trägt {mining.Crew.PartnerLoad/unit} m³" : mining.Crew.Status;
            crewButton.text = mining.Crew.Hosting || mining.Crew.IsGuest ? "Trennen" : "Crew hosten";
            var state = (mining.Equipment.Slot, mining.Target, mining.Bin ? mining.Bin.Material : MaterialId.None,
                mining.AtWorkshop, pack.Revision, cargo.Revision, mining.Equipment.Tools.Active, mining.Menu,
                mining.Equipment.Upgrades.Revision,
                mining.AimedRobot ? mining.AimedRobot.Robot.Revision : mining.AtHopper ? -2 - mining.Beams.Chain.Revision
                    : mining.AtPump ? -100000 - mining.Pump.Pump.Revision : mining.AimingAtPlant ? -3 : -1);
            if (initialized && last.Equals(state)) return;
            last = state;
            initialized = true;
            displayedMenu = mining.Menu;
            bool open = displayedMenu != FieldMenu.Closed;
            gameplay.gameObject.SetActive(!open);
            modal.gameObject.SetActive(open);
            if (open)
            {
                bool shop = displayedMenu == FieldMenu.Workshop;
                menuTitle.text = shop ? "Werkzeugstation" : "Inventar";
                wallet.text = $"{cargo.Credits} Credits";
                var stock = new System.Text.StringBuilder();
                foreach (var resource in cargo.Program.Materials)
                    stock.AppendLine($"{resource.Name}   {pack.AmountOf(resource.Id)/unit} m³");
                holdings.text = stock.ToString();
                for (int i = 0; i < toolLabels.Length; i++)
                {
                    var tool = (MiningTool)i;
                    var spec = MiningTools.Spec(tool);
                    bool owned = mining.Equipment.Tools.Owns(tool);
                    string action = tool == last.active ? "Ausgerüstet" : owned ? "Ausrüsten" : shop ? $"{spec.Price} Credits" : "Werkzeugstation";
                    toolLabels[i].text = $"{i+1}   <b>{spec.Name}</b>    ·    {action}\n      {spec.Description}";
                    toolButtons[i].interactable = owned || (shop && cargo.Credits >= spec.Price);
                }
                var upgrades = mining.Equipment.Upgrades;
                for (int i = 0; i < upgradeLabels.Length; i++)
                {
                    var axis = (UpgradeAxis)i;
                    int level = upgrades.Level(axis), max = upgrades.Catalog.MaxLevel(axis);
                    string next = upgrades.IsMaxed(axis) ? "Höchste Stufe erreicht"
                        : $"Nächste Stufe {Describe(axis, upgrades.Catalog.Step(axis, level + 1).Value)}   ·   " +
                          (shop ? $"{upgrades.NextPrice(axis)} Credits" : "an der Werkbank");
                    upgradeLabels[i].text = $"{i+4}   <b>{upgrades.Catalog.Name(axis)}</b>    Stufe {level}/{max}  ·  {Describe(axis, upgrades.Catalog.Step(axis, level).Value)}\n      {next}";
                    upgradeButtons[i].interactable = shop && !upgrades.IsMaxed(axis) && cargo.Credits >= upgrades.NextPrice(axis);
                }
                footer.text = shop ? "Credits erhältst du, sobald eine volle Ladung den Orbit erreicht." : "Neue Werkzeuge gibt es an der Werkbank links vom Startplatz.";
                if (menuChanged) events.SetSelectedGameObject(toolButtons[(int)last.active].gameObject);
                return;
            }
            if (menuChanged) events.SetSelectedGameObject(null);
            var contracts = cargo.Contracts;
            var contract = contracts.Current;
            level.text = contract == null ? "AUFTRÄGE ABGESCHLOSSEN"
                : $"AUFTRAG {contracts.Completed + 1} / {contracts.Count}   ·   {contract.Act.ToUpperInvariant()}";
            title.text = contract == null ? "Freier Abbau" : contract.Title;
            instruction.text = contract == null ? "Weitere Lieferungen bringen Credits." : Requirements(cargo, contract);
            float share = contract == null ? 1 : (float)contracts.CurrentProgress / contract.RequiredVolume;
            bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 168f * share);
            progress.text = contract == null ? $"{contracts.Count} / {contracts.Count}"
                : $"{contracts.CurrentProgress / unit} / {contract.RequiredVolume / unit} m³";
            stone.text = $"Guthaben    {cargo.Credits} Credits";
            flight.text = cargo.Flying == MaterialId.None ? "Voll beladen = Abflug" :
                cargo.Flying.Name() + (cargo.Returning ? " · Rückkehr" : " · Aufstieg");
            beltTool.text = MiningTools.Spec(last.active).Name;
            for (int i = 0; i < outlines.Length; i++)
            {
                outlines[i].color = i == last.slot ? Mint : Slate;
                if (i > 0) quantities[i].text = (pack.AmountOf(Materials[i]) / unit).ToString();
            }
            if (last.shop) hint.text = "E   Werkzeugstation öffnen";
            else if (mining.AimingAtPlant)
                hint.text = pack.Free < unit ? "Rucksack voll" : "LMB   Pflanze ernten (Biomasse)";
            else if (mining.AtPump)
            {
                var pump = mining.Pump.Pump;
                hint.text = !pump.Built ? $"E   Pumpe bauen ({mining.Pump.Settings.PumpPrice} Credits)"
                    : pump.Tank.Amount > 0 ? $"E   {pump.Tank.Amount / unit} m³ Wasser übernehmen"
                    : pump.State == Logistics.PumpState.Dry ? "Der See ist leer" : "E   Pumpe starten/stoppen";
            }
            else if (mining.AimedRobot)
            {
                var robot = mining.AimedRobot.Robot;
                string phase = robot.State switch
                {
                    Machines.RobotState.Working => "arbeitet", Machines.RobotState.Full => "Trichter voll",
                    Machines.RobotState.Finished => "Bereich fertig", _ => "gestoppt"
                };
                hint.text = robot.Hopper.Amount > 0
                    ? $"E   {robot.Hopper.Amount / unit} m³ übernehmen  ·  {phase} {robot.Progress * 100:F0} %"
                    : $"E   Start/Stopp  ·  {phase} {robot.Progress * 100:F0} %  ·  J abbauen";
            }
            else if (mining.AtHopper) hint.text = mining.Beams.Chain.Hopper.Free < unit ? "Trichter voll · Strahl arbeitet" : "E   Sammeltrichter befüllen";
            else if (last.bin != MaterialId.None)
                hint.text = cargo.Container(last.bin).Free == 0 ? "Voll beladen · wartet auf den Lift"
                    : pack.AmountOf(last.bin) > 0 ? $"E   {last.bin.Name()} einladen" : cargo.Container(last.bin).Amount > 0 ? "E   Teilladung versenden" : $"{last.bin.Name()}container · leer";
            else if (pack.Free < unit) hint.text = "Rucksack voll · am Lift mit E entladen";
            else if (last.target != MaterialId.None)
                hint.text = last.slot == 0 ? $"LMB   {last.target.Name()} abbauen" : "1   Werkzeug ausrüsten";
            else hint.text = "LMB  Bohren    ·    R  Form wechseln    ·    E  Verladen";
        }

        private string BeamStatus()
        {
            var beams = mining.Beams;
            if (beams == null) return string.Empty;
            if (!beams.Unlocked) return "Traktorstrahl nach dem Landevertrag";
            if (!beams.HasHopper) return $"B  Sammeltrichter bauen ({beams.NextPrice} Cr)";
            string link = beams.MastCount == 0 ? "ohne Mast" : beams.Connected ? "am Lift" : "nicht verbunden";
            string jam = beams.Chain.Blocked ? "  ·  Rückstau" : string.Empty;
            return $"Strahl {beams.MastCount} Masten, {link}, {beams.Chain.LumpCount} m³{jam}   ·   B/N";
        }

        private static string Describe(UpgradeAxis axis, float value) =>
            axis == UpgradeAxis.Capacity ? $"{value:0} m³" : $"×{value:0.##}";

        /// <summary>Compact per-material progress such as "Erde 12/20 · Stein 3/12 m³".</summary>
        private static string Requirements(EarthFreight cargo, ContractSpec contract)
        {
            const long unit = HarvestInventory.CubicMetre;
            var text = new System.Text.StringBuilder();
            foreach (var requirement in contract.Requirements)
            {
                if (text.Length > 0) text.Append("  ·  ");
                text.Append(cargo.Program.Material(requirement.Material).Name).Append(' ')
                    .Append(cargo.Contracts.Progress(requirement.Material) / unit).Append('/').Append(requirement.CubicMetres);
            }
            return text.Append(" m³").ToString();
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
