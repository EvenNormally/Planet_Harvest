using UnityEngine;

namespace PlanetHarvest.Mining
{
    /// <summary>
    /// Unchangeable description of one mineable material. The <see cref="MaterialId"/> is the stable key
    /// used by saves and the network; its numeric value must never change.
    /// </summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Materialdefinition")]
    public sealed class MaterialDefinition : ScriptableObject
    {
        [SerializeField] private MaterialId id = MaterialId.Stone;
        [SerializeField] private string displayName = "Stein";
        [SerializeField, Min(1)] private float densityKgPerCubicMetre = 2600;
        [SerializeField, Range(0, MaterialSpec.MaxCreditsPerCubicMetre)] private float creditsPerCubicMetre = 1;
        [SerializeField, Min(1), Tooltip("Füllvolumen des Elevator-Containers in m³")] private int containerCubicMetres = 10;
        [SerializeField] private ContainerKind container = ContainerKind.Barrel;
        [SerializeField] private Color containerColor = Color.gray;

        public MaterialId Id => id;

        public MaterialSpec ToSpec() => new MaterialSpec(id, displayName, densityKgPerCubicMetre,
            creditsPerCubicMetre, containerCubicMetres, container, containerColor);
    }
}
