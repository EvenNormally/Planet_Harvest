using System.Collections.Generic;
using PlanetHarvest.Core;

namespace PlanetHarvest.UI
{
    /// <summary>
    /// German/English texts for the settings, scanner and ending screens and the key hints. Missing keys fall back
    /// to German, then to the key itself, so a gap never shows an empty label.
    /// </summary>
    public static class Localization
    {
        public static GameLanguage Language { get; set; } = GameLanguage.German;

        private static readonly Dictionary<string, (string de, string en)> Texts = new Dictionary<string, (string, string)>
        {
            ["settings.title"] = ("Einstellungen", "Settings"),
            ["settings.volume"] = ("Lautstärke", "Volume"),
            ["settings.quality"] = ("Grafik", "Graphics"),
            ["settings.uiScale"] = ("UI-Skalierung", "UI scale"),
            ["settings.language"] = ("Sprache", "Language"),
            ["settings.languageName"] = ("Deutsch", "English"),
            ["settings.rebind"] = ("Interagieren neu belegen", "Rebind interact"),
            ["settings.rebindWait"] = ("Taste drücken …", "Press a key …"),
            ["settings.reset"] = ("Belegung zurücksetzen", "Reset bindings"),
            ["settings.close"] = ("Schließen", "Close"),
            ["settings.hint"] = ("F10 Einstellungen   ·   Esc zurück", "F10 settings   ·   Esc back"),
            ["settings.default"] = ("Standard", "Default"),
            ["hud.keys"] = ("Tab  Inventar & Crew     R  Bohrform", "Tab  Inventory & crew     R  drill shape"),
            ["scanner.locked"] = ("Restscanner nach allen Aufträgen", "Residue scanner after all contracts"),
            ["scanner.none"] = ("Restscanner: kein Material in Reichweite", "Residue scanner: no material in range"),
            ["scanner.found"] = ("Restscanner: {0} in {1:F0} m", "Residue scanner: {0} at {1:F0} m"),
            ["ending.title"] = ("Planet geerntet!", "Planet harvested!"),
            ["ending.subtitle"] = ("Der letzte Brocken ist im Orbit. Die Firma ist … fast zufrieden.",
                "The last chunk is in orbit. The company is … almost pleased."),
            ["ending.orbit"] = ("Im Orbit", "In orbit"),
            ["ending.credits"] = ("Credits", "Credits"),
            ["ending.contracts"] = ("Aufträge", "Contracts"),
            ["ending.time"] = ("Spielzeit", "Play time"),
            ["ending.continue"] = ("Esc  weiter abbauen", "Esc  keep mining"),
            ["ending.crew"] = ("Crew: die Alien-Abbaucrew · Ein Spiel des Planet-Harvest-Teams",
                "Crew: the alien mining crew · A game by the Planet Harvest team"),
        };

        public static string Text(string key)
        {
            if (!Texts.TryGetValue(key, out var entry)) return key;
            return Language == GameLanguage.English && !string.IsNullOrEmpty(entry.en) ? entry.en : entry.de;
        }

        public static string Format(string key, params object[] values) => string.Format(Text(key), values);

        /// <summary>For tests: every key has both languages.</summary>
        public static IEnumerable<KeyValuePair<string, (string de, string en)>> All => Texts;
    }
}
