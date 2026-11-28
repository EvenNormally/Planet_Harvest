using PlanetHarvest.Progression;

namespace PlanetHarvest.Player
{
    public enum MiningTool { Pick, ProspectingPick, Drill }

    public readonly struct ToolSpec
    {
        public readonly string Name, Description;
        public readonly int Price;
        public readonly float Reach, RepeatSeconds;

        public ToolSpec(string name, string description, int price, float reach, float repeatSeconds)
        {
            Name = name;
            Description = description;
            Price = price;
            Reach = reach;
            RepeatSeconds = repeatSeconds;
        }
    }

    public sealed class MiningTools
    {
        private static readonly ToolSpec[] Catalog =
        {
            new ToolSpec("Handbohrer", "3 Bohrformen · 5 m Reichweite", 0, 5, .65f),
            new ToolSpec("Langbohrer", "Breiterer Schnitt · 8 m Reichweite", 20, 8, .46f),
            new ToolSpec("Kernbohrer", "Dauerabbau · 6 m", 50, 6, 0.24f)
        };
        private readonly bool[] owned = { true, false, false };
        public MiningTool Active { get; private set; }
        public static int Count => Catalog.Length;
        public static ToolSpec Spec(MiningTool tool) => Catalog[(int)tool];
        public bool Owns(MiningTool tool) => owned[(int)tool];

        public int Ownership => 1 | (owned[1] ? 2 : 0) | (owned[2] ? 4 : 0);

        public void Restore(int ownership, MiningTool active)
        {
            for (int i = 0; i < owned.Length; i++) owned[i] = (ownership & (1 << i)) != 0;
            Active = active;
        }

        public bool Select(MiningTool tool, EarthFreight wallet, bool atShop)
        {
            if (!Owns(tool))
            {
                if (!atShop || !wallet.Spend(Spec(tool).Price)) return false;
                owned[(int)tool] = true;
            }
            Active = tool;
            return true;
        }
    }
}
