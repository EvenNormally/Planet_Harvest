using System;
using System.Collections.Generic;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Progression
{
    public readonly struct MaterialRequirement
    {
        public readonly MaterialId Material;
        public readonly int CubicMetres;

        public MaterialRequirement(MaterialId material, int cubicMetres)
        {
            if (!HarvestInventory.IsResource(material)) throw new ArgumentOutOfRangeException(nameof(material));
            if (cubicMetres < 1) throw new ArgumentOutOfRangeException(nameof(cubicMetres));
            Material = material;
            CubicMetres = cubicMetres;
        }

        public long Volume => CubicMetres * HarvestInventory.CubicMetre;
    }

    /// <summary>One elevator order: minimum deliveries of one or more materials plus a one-time bonus.</summary>
    public sealed class ContractSpec
    {
        private readonly MaterialRequirement[] requirements;
        public string Id { get; }
        public string Title { get; }
        public string Act { get; }
        public int BonusCredits { get; }
        public IReadOnlyList<MaterialRequirement> Requirements => requirements;
        public long RequiredVolume { get; }

        public ContractSpec(string id, string title, string act, int bonusCredits, params MaterialRequirement[] requirements)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Auftrag ohne Kennung.", nameof(id));
            if (bonusCredits < 0) throw new ArgumentOutOfRangeException(nameof(bonusCredits));
            if (requirements == null || requirements.Length == 0)
                throw new ArgumentException("Auftrag ohne Mindestlieferung.", nameof(requirements));
            for (int i = 0; i < requirements.Length; i++)
            for (int j = i + 1; j < requirements.Length; j++)
                if (requirements[i].Material == requirements[j].Material)
                    throw new ArgumentException($"Material {requirements[i].Material} doppelt in Auftrag {id}.");
            Id = id;
            Title = string.IsNullOrWhiteSpace(title) ? id : title;
            Act = act ?? string.Empty;
            BonusCredits = bonusCredits;
            this.requirements = (MaterialRequirement[])requirements.Clone();
            foreach (var requirement in this.requirements) RequiredVolume += requirement.Volume;
        }
    }

    /// <summary>
    /// Validated, immutable combination of the deliverable materials and the ordered elevator contracts.
    /// Pure data without Unity object references so rules can be tested without assets.
    /// </summary>
    public sealed class FreightProgram
    {
        private readonly MaterialSpec[] materials;
        private readonly ContractSpec[] contracts;
        private readonly int[] slotToIndex = new int[HarvestInventory.MaterialSlots];
        public IReadOnlyList<MaterialSpec> Materials => materials;
        public IReadOnlyList<ContractSpec> Contracts => contracts;

        public FreightProgram(IEnumerable<MaterialSpec> materials, IEnumerable<ContractSpec> contracts)
        {
            this.materials = new List<MaterialSpec>(materials ?? throw new ArgumentNullException(nameof(materials))).ToArray();
            this.contracts = new List<ContractSpec>(contracts ?? throw new ArgumentNullException(nameof(contracts))).ToArray();
            if (this.materials.Length == 0) throw new ArgumentException("Keine Materialien.", nameof(materials));
            for (int i = 0; i < slotToIndex.Length; i++) slotToIndex[i] = -1;
            for (int i = 0; i < this.materials.Length; i++)
            {
                int slot = (int)this.materials[i].Id;
                if (slotToIndex[slot] >= 0) throw new ArgumentException($"Material {this.materials[i].Id} doppelt definiert.");
                slotToIndex[slot] = i;
            }
            var ids = new HashSet<string>();
            foreach (var contract in this.contracts)
            {
                if (contract == null) throw new ArgumentException("Leerer Auftrag.", nameof(contracts));
                if (!ids.Add(contract.Id)) throw new ArgumentException($"Auftrag {contract.Id} doppelt.");
                foreach (var requirement in contract.Requirements)
                    if (!Contains(requirement.Material))
                        throw new ArgumentException($"Auftrag {contract.Id} verlangt nicht lieferbares Material {requirement.Material}.");
            }
        }

        public bool Contains(MaterialId material) => IndexOf(material) >= 0;

        /// <summary>Position of the material in <see cref="Materials"/>, or -1.</summary>
        public int IndexOf(MaterialId material) =>
            HarvestInventory.IsResource(material) ? slotToIndex[(int)material] : -1;

        public MaterialSpec Material(MaterialId material)
        {
            int index = IndexOf(material);
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(material));
            return materials[index];
        }
    }
}
