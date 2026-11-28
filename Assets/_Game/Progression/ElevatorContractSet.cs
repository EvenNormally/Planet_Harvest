using System;
using System.Collections.Generic;
using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Progression
{
    /// <summary>Configuration asset: deliverable materials and the ordered list of elevator contracts.</summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Elevator-Aufträge")]
    public sealed class ElevatorContractSet : ScriptableObject
    {
        [Serializable]
        private sealed class Requirement
        {
            [SerializeField] private MaterialId material = MaterialId.Stone;
            [SerializeField, Min(1)] private int cubicMetres = 1;
            public MaterialRequirement ToSpec() => new MaterialRequirement(material, cubicMetres);
        }

        [Serializable]
        private sealed class Contract
        {
            [SerializeField, Tooltip("Stabile Kennung, wird nicht angezeigt")] private string id;
            [SerializeField] private string title;
            [SerializeField] private string act;
            [SerializeField, Min(0)] private int bonusCredits;
            [SerializeField] private Requirement[] requirements = Array.Empty<Requirement>();

            public ContractSpec ToSpec()
            {
                var specs = new MaterialRequirement[requirements?.Length ?? 0];
                for (int i = 0; i < specs.Length; i++) specs[i] = requirements[i].ToSpec();
                return new ContractSpec(id, title, act, bonusCredits, specs);
            }
        }

        [SerializeField, Tooltip("Reihenfolge bestimmt die Container am Lift")] private MaterialDefinition[] materials = Array.Empty<MaterialDefinition>();
        [SerializeField] private Contract[] contracts = Array.Empty<Contract>();

        /// <summary>Builds the validated runtime program. Throws when the asset is inconsistent.</summary>
        public FreightProgram CreateProgram()
        {
            var materialSpecs = new List<MaterialSpec>();
            foreach (var definition in materials)
            {
                if (definition == null) throw new InvalidOperationException($"{name}: leerer Materialeintrag.");
                materialSpecs.Add(definition.ToSpec());
            }
            var contractSpecs = new List<ContractSpec>();
            foreach (var contract in contracts) contractSpecs.Add(contract.ToSpec());
            return new FreightProgram(materialSpecs, contractSpecs);
        }
    }
}
