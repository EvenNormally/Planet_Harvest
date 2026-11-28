using System;
using System.IO;
using UnityEngine;

namespace PlanetHarvest.Core
{
    public enum GameLanguage
    {
        German = 0,
        English = 1
    }

    /// <summary>Player preferences, stored as JSON next to the save games. Independent from expedition saves.</summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const float MinUiScale = .75f, MaxUiScale = 1.5f;
        public float masterVolume = .8f;
        /// <summary>Index into QualitySettings.names; -1 keeps the project default.</summary>
        public int quality = -1;
        public float uiScale = 1;
        public GameLanguage language = GameLanguage.German;
        /// <summary>Input System binding overrides (SaveBindingOverridesAsJson).</summary>
        public string bindings = string.Empty;

        /// <summary>Brings every value into its valid range; returns this for chaining.</summary>
        public GameSettings Sanitize(int qualityLevels)
        {
            masterVolume = float.IsNaN(masterVolume) ? .8f : Mathf.Clamp01(masterVolume);
            uiScale = float.IsNaN(uiScale) ? 1 : Mathf.Clamp(uiScale, MinUiScale, MaxUiScale);
            if (quality < -1 || quality >= qualityLevels) quality = -1;
            if (!Enum.IsDefined(typeof(GameLanguage), language)) language = GameLanguage.German;
            bindings ??= string.Empty;
            return this;
        }

        public static GameSettings Load(string path, int qualityLevels)
        {
            try
            {
                if (File.Exists(path))
                    return (JsonUtility.FromJson<GameSettings>(File.ReadAllText(path)) ?? new GameSettings()).Sanitize(qualityLevels);
            }
            catch (Exception) { /* A damaged settings file falls back to defaults. */ }
            return new GameSettings().Sanitize(qualityLevels);
        }

        public void Save(string path)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(this, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
