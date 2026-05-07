using System;

namespace Hollow.Combat
{
    public readonly struct EnemyAiToolBakeOffOption
    {
        public EnemyAiToolBakeOffOption(
            string name,
            string role,
            string sourceUrl,
            bool requiresPurchase,
            int integrationRisk,
            string expectedBenefit,
            string adoptionGate)
        {
            Name = name ?? string.Empty;
            Role = role ?? string.Empty;
            SourceUrl = sourceUrl ?? string.Empty;
            RequiresPurchase = requiresPurchase;
            IntegrationRisk = integrationRisk;
            ExpectedBenefit = expectedBenefit ?? string.Empty;
            AdoptionGate = adoptionGate ?? string.Empty;
        }

        public string Name { get; }

        public string Role { get; }

        public string SourceUrl { get; }

        public bool RequiresPurchase { get; }

        public int IntegrationRisk { get; }

        public string ExpectedBenefit { get; }

        public string AdoptionGate { get; }
    }

    public static class EnemyAiToolBakeOffEvaluation
    {
        public const string HollowSourceOfTruth = "Hollow enemy/action/spacing/behavior data";

        public static readonly EnemyAiToolBakeOffOption[] Options =
        {
            new(
                "Current Custom RoomGridAStar",
                "baseline navigation backend",
                "local",
                false,
                2,
                "Keeps deterministic room-JSON navigation and all current debug hooks.",
                "Must stop rock scraping, hold stable frame time, and feed tactical slots."),
            new(
                "Unity AI Navigation",
                "built-in NavMesh candidate",
                "https://docs.unity.cn/6000.2/Documentation/Manual/com.unity.ai.navigation.html",
                false,
                3,
                "Could provide mature NavMesh surfaces, runtime baking, dynamic obstacles, and links.",
                "Only adopt if runtime/generated-room baking is deterministic and cheaper than custom corridors."),
            new(
                "Unity Behavior",
                "official Unity behavior-tree graph/runtime candidate",
                "https://docs.unity3d.com/ja/current/Manual/com.unity.behavior.html",
                false,
                3,
                "Official graph authoring, reusable subgraphs, C# integration, and play-mode debugging for high-level AI decisions.",
                "Adopt enemy-by-enemy only if graphs can output Hollow commands without replacing combat data, attack windows, or pressure budgets."),
            new(
                "A* Pathfinding Project Pro",
                "paid navigation/local-avoidance candidate",
                "https://assetstore.unity.com/packages/tools/behavior-ai/a-pathfinding-project-pro-87744",
                true,
                3,
                "Strong candidate for polished grid/navmesh pathing, local avoidance, and designer diagnostics.",
                "Only adopt if it clearly beats custom A* in obstacle feel and 20-40 enemy performance."),
            new(
                "Behavior Designer Pro 3",
                "paid behavior-authoring candidate",
                "https://assetstore.unity.com/packages/tools/visual-scripting/behavior-designer-pro-3-dots-powered-behavior-trees-368344",
                true,
                4,
                "Potential visual behavior-tree authoring acceleration for designers.",
                "Only adopt if it mirrors Hollow data cleanly without replacing our source-of-truth assets.")
        };

        public static EnemyAiToolBakeOffOption Resolve(string name)
        {
            for (var index = 0; index < Options.Length; index++)
            {
                if (string.Equals(Options[index].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return Options[index];
                }
            }

            return default;
        }
    }
}
