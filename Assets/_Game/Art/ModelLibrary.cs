using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Art
{
    public enum GameModel
    {
        HandDrill, Workbench, TractorMast, BeamHopper, DrillRobot, Pump, AlienCrew, SpaceElevator
    }

    /// <summary>
    /// References to the Blender-generated models (Tools/Blender/build_models.py). Every system keeps a primitive
    /// fallback, so a missing model never breaks gameplay. Instances use the shared vertex-colour finish.
    /// </summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Modellbibliothek")]
    public sealed class ModelLibrary : ScriptableObject
    {
        [SerializeField] private GameObject handDrill, workbench, tractorMast, beamHopper, drillRobot, pump, alienCrew, spaceElevator;
        [SerializeField] private GameObject pressTub, barrel, oreHopper, magneticBin, prismBox, glassSphere, organicTank;

        public GameObject Prefab(GameModel model) => model switch
        {
            GameModel.HandDrill => handDrill, GameModel.Workbench => workbench, GameModel.TractorMast => tractorMast,
            GameModel.BeamHopper => beamHopper, GameModel.DrillRobot => drillRobot, GameModel.Pump => pump,
            GameModel.AlienCrew => alienCrew, GameModel.SpaceElevator => spaceElevator, _ => null
        };

        public GameObject Container(ContainerKind kind) => kind switch
        {
            ContainerKind.PressTub => pressTub, ContainerKind.Barrel => barrel, ContainerKind.OreHopper => oreHopper,
            ContainerKind.MagneticBin => magneticBin, ContainerKind.PrismBox => prismBox,
            ContainerKind.GlassSphere => glassSphere, ContainerKind.OrganicTank => organicTank, _ => null
        };

        /// <summary>Instantiates a model under a parent; null when the model is not assigned.</summary>
        public Transform Spawn(GameModel model, Transform parent, Material finish) => Spawn(Prefab(model), parent, finish);

        public Transform SpawnContainer(ContainerKind kind, Transform parent, Material finish) => Spawn(Container(kind), parent, finish);

        public static Transform Spawn(GameObject prefab, Transform parent, Material finish)
        {
            if (!prefab) return null;
            var instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Destroy(collider);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = finish;
            return instance.transform;
        }

        /// <summary>Finds a named part anywhere below a model root.</summary>
        public static Transform Find(Transform root, string name)
        {
            if (!root) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
