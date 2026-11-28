using System;
using UnityEngine;

namespace PlanetHarvest.Player
{
    /// <summary>Configuration asset for the three tool upgrade axes. Step 0 is the starting equipment.</summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Werkzeug-Upgrades")]
    public sealed class ToolUpgradeSet : ScriptableObject
    {
        [Serializable]
        private sealed class Step
        {
            [SerializeField, Min(0)] private int price;
            [SerializeField, Min(0.01f)] private float value = 1;
            public UpgradeStep ToStep() => new UpgradeStep(price, value);
        }

        [SerializeField, Tooltip("Faktor auf die Schnittgröße")] private Step[] radius = Array.Empty<Step>();
        [SerializeField, Tooltip("Faktor auf die Schlagfrequenz")] private Step[] speed = Array.Empty<Step>();
        [SerializeField, Tooltip("Rucksackvolumen in m³")] private Step[] capacity = Array.Empty<Step>();

        public UpgradeCatalog CreateCatalog() => new UpgradeCatalog(Convert(radius), Convert(speed), Convert(capacity));

        private static UpgradeStep[] Convert(Step[] source)
        {
            var steps = new UpgradeStep[source?.Length ?? 0];
            for (int i = 0; i < steps.Length; i++) steps[i] = source[i].ToStep();
            return steps;
        }
    }
}
