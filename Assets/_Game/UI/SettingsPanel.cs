using System.IO;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PlanetHarvest.UI
{
    /// <summary>
    /// F10 settings: volume, graphics quality, UI scale, language and rebinding of the interact action.
    /// Changes apply at once and are stored in settings.json beside the save games.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1280, 720);
        private EarthMining mining;
        private RadialPlayer player;
        private TMP_FontAsset font;
        private RectTransform panel;
        private TextMeshProUGUI title, volume, quality, scale, language, rebind, reset, close, hint;
        private TextMeshProUGUI volumeName, qualityName, scaleName, languageName;
        private InputActionRebindingExtensions.RebindingOperation rebinding;
        public GameSettings Settings { get; private set; }
        public string SettingsPath => Path.Combine(Application.isBatchMode ? Application.temporaryCachePath : Application.persistentDataPath, "settings.json");

        public void Initialize(EarthMining owner, RectTransform screen, TMP_FontAsset typeface)
        {
            mining = owner;
            player = owner.GetComponent<RadialPlayer>();
            font = typeface;
            Settings = GameSettings.Load(SettingsPath, QualitySettings.names.Length);
            Build(screen);
            Apply(false);
        }

        private void Build(RectTransform screen)
        {
            panel = UiKit.Panel(screen, "Einstellungen", new Vector2(520, 420), UiKit.Ink);
            UiKit.Box(panel, 0, 0, 520, 4, UiKit.Mint);
            title = UiKit.Label(panel, font, 22, 16, 470, 40, 28, UiKit.Paper, true);
            (volumeName, volume) = Row(60, () => Change(ref Settings.masterVolume, -.1f, 0, 1), () => Change(ref Settings.masterVolume, .1f, 0, 1));
            (qualityName, quality) = Row(110, () => StepQuality(-1), () => StepQuality(1));
            (scaleName, scale) = Row(160, () => Change(ref Settings.uiScale, -.125f, GameSettings.MinUiScale, GameSettings.MaxUiScale),
                () => Change(ref Settings.uiScale, .125f, GameSettings.MinUiScale, GameSettings.MaxUiScale));
            (languageName, language) = Row(210, ToggleLanguage, ToggleLanguage);
            rebind = UiKit.Button(panel, font, 22, 268, 230, 36, StartRebind).label;
            reset = UiKit.Button(panel, font, 268, 268, 230, 36, ResetBindings).label;
            close = UiKit.Button(panel, font, 22, 330, 476, 36, () => mining.CloseMenu()).label;
            hint = UiKit.Label(panel, font, 22, 378, 476, 30, 14, UiKit.Muted);
            panel.gameObject.SetActive(false);
        }

        private (TextMeshProUGUI name, TextMeshProUGUI value) Row(float y, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus)
        {
            var name = UiKit.Label(panel, font, 22, y, 200, 36, 17, UiKit.Paper);
            UiKit.Button(panel, font, 230, y, 44, 36, minus).label.text = "−";
            var value = UiKit.Label(panel, font, 280, y, 160, 36, 17, UiKit.Mint, true, TextAlignmentOptions.Center);
            UiKit.Button(panel, font, 446, y, 44, 36, plus).label.text = "+";
            return (name, value);
        }

        private void Change(ref float value, float step, float min, float max)
        {
            value = Mathf.Clamp(Mathf.Round((value + step) * 1000) / 1000, min, max);
            Apply(true);
        }

        private void StepQuality(int step)
        {
            int count = QualitySettings.names.Length;
            int current = Settings.quality < 0 ? QualitySettings.GetQualityLevel() : Settings.quality;
            Settings.quality = Mathf.Clamp(current + step, 0, count - 1);
            Apply(true);
        }

        public void ToggleLanguage()
        {
            Settings.language = Settings.language == GameLanguage.German ? GameLanguage.English : GameLanguage.German;
            Apply(true);
        }

        private void StartRebind()
        {
            var action = player.Actions?.FindAction("Player/Interact");
            if (action == null || rebinding != null) return;
            action.Disable();
            rebind.text = Localization.Text("settings.rebindWait");
            rebinding = action.PerformInteractiveRebinding(0)
                .WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(_ => FinishRebind(action)).OnCancel(_ => FinishRebind(action)).Start();
        }

        private void FinishRebind(InputAction action)
        {
            rebinding?.Dispose();
            rebinding = null;
            action.Enable();
            Settings.bindings = player.Actions.SaveBindingOverridesAsJson();
            Apply(true);
        }

        private void ResetBindings()
        {
            player.Actions?.RemoveAllBindingOverrides();
            Settings.bindings = string.Empty;
            Apply(true);
        }

        /// <summary>Applies all values to the running game and optionally stores them.</summary>
        public void Apply(bool save)
        {
            Settings.Sanitize(QualitySettings.names.Length);
            AudioListener.volume = Settings.masterVolume;
            if (Settings.quality >= 0 && Settings.quality != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(Settings.quality, true);
            Localization.Language = Settings.language;
            foreach (var scaler in mining.GetComponentsInChildren<CanvasScaler>(true))
                scaler.referenceResolution = ReferenceResolution / Settings.uiScale;
            if (player.Actions != null && !string.IsNullOrEmpty(Settings.bindings) && !save)
                player.Actions.LoadBindingOverridesFromJson(Settings.bindings);
            Refresh();
            if (!save) return;
            try { Settings.Save(SettingsPath); }
            catch (IOException) { /* Settings stay active for this session. */ }
        }

        private void Refresh()
        {
            if (!panel) return;
            title.text = Localization.Text("settings.title");
            volumeName.text = Localization.Text("settings.volume");
            volume.text = $"{Settings.masterVolume * 100:F0} %";
            qualityName.text = Localization.Text("settings.quality");
            quality.text = Settings.quality < 0 ? Localization.Text("settings.default") : QualitySettings.names[Settings.quality];
            scaleName.text = Localization.Text("settings.uiScale");
            scale.text = $"{Settings.uiScale * 100:F0} %";
            languageName.text = Localization.Text("settings.language");
            language.text = Localization.Text("settings.languageName");
            rebind.text = $"{Localization.Text("settings.rebind")} ({InteractBinding()})";
            reset.text = Localization.Text("settings.reset");
            close.text = Localization.Text("settings.close");
            hint.text = Localization.Text("settings.hint");
        }

        private string InteractBinding()
        {
            var action = player.Actions?.FindAction("Player/Interact");
            return action == null ? "?" : action.GetBindingDisplayString(0);
        }

        private void LateUpdate()
        {
            bool open = mining && mining.Menu == FieldMenu.Settings;
            if (panel.gameObject.activeSelf != open)
            {
                panel.gameObject.SetActive(open);
                if (open) Refresh();
            }
        }

        private void OnDestroy() => rebinding?.Dispose();
    }
}
