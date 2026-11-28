using System;
using PlanetHarvest.Logistics;
using PlanetHarvest.Machines;
using PlanetHarvest.Player;
using PlanetHarvest.Progression;
using UnityEngine;

namespace PlanetHarvest.Core
{
    /// <summary>The configuration a saved or received expedition state is checked against.</summary>
    public sealed class ExpeditionRules
    {
        public FreightProgram Program { get; }
        public UpgradeCatalog Upgrades { get; }
        public BeamSettings Beams { get; }
        public RobotSettings Robots { get; }
        public WaterSettings Water { get; }
        /// <summary>True for keys of plants that exist in this world.</summary>
        public Func<Vector3Int, bool> IsPlant { get; }

        public ExpeditionRules(FreightProgram program, UpgradeCatalog upgrades, BeamSettings beams, RobotSettings robots,
            WaterSettings water, Func<Vector3Int, bool> isPlant)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            Beams = beams ? beams : throw new ArgumentNullException(nameof(beams));
            Robots = robots ? robots : throw new ArgumentNullException(nameof(robots));
            Water = water ? water : throw new ArgumentNullException(nameof(water));
            IsPlant = isPlant ?? (_ => false);
        }
    }
}
