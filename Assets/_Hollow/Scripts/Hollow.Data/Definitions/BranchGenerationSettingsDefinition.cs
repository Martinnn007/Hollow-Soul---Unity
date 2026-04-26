using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    public sealed class BranchGenerationSettingsDefinition : ScriptableObject
    {
        [SerializeField] private int defaultSeed = 15001;
        [SerializeField] private int targetRoomCount = 8;
        [SerializeField] private int maxPlacementAttempts = 250;
        [SerializeField] private bool allowLoops;
        [SerializeField] private bool enableBossLeaf = true;
        [SerializeField] private bool enableTreasureLeaf;
        [SerializeField] private List<string> allowedFixtureIds = new()
        {
            "combat_macro_single_1x1",
            "combat_macro_wide_2x1",
            "combat_macro_tall_1x2",
            "combat_macro_block_2x2",
            "combat_macro_l_3cell"
        };

        public int DefaultSeed => defaultSeed;

        public int TargetRoomCount => Mathf.Max(2, targetRoomCount);

        public int MaxPlacementAttempts => Mathf.Max(1, maxPlacementAttempts);

        public bool AllowLoops => allowLoops;

        public bool EnableBossLeaf => enableBossLeaf;

        public bool EnableTreasureLeaf => enableTreasureLeaf;

        public IReadOnlyList<string> AllowedFixtureIds => allowedFixtureIds;

        public void Configure(
            int nextDefaultSeed,
            int nextTargetRoomCount,
            int nextMaxPlacementAttempts,
            bool nextAllowLoops,
            bool nextEnableBossLeaf,
            IEnumerable<string> nextAllowedFixtureIds)
        {
            Configure(
                nextDefaultSeed,
                nextTargetRoomCount,
                nextMaxPlacementAttempts,
                nextAllowLoops,
                nextEnableBossLeaf,
                nextEnableTreasureLeaf: false,
                nextAllowedFixtureIds: nextAllowedFixtureIds);
        }

        public void Configure(
            int nextDefaultSeed,
            int nextTargetRoomCount,
            int nextMaxPlacementAttempts,
            bool nextAllowLoops,
            bool nextEnableBossLeaf,
            bool nextEnableTreasureLeaf,
            IEnumerable<string> nextAllowedFixtureIds)
        {
            defaultSeed = nextDefaultSeed == 0 ? 15001 : nextDefaultSeed;
            targetRoomCount = Mathf.Max(2, nextTargetRoomCount);
            maxPlacementAttempts = Mathf.Max(1, nextMaxPlacementAttempts);
            allowLoops = nextAllowLoops;
            enableBossLeaf = nextEnableBossLeaf;
            enableTreasureLeaf = nextEnableTreasureLeaf;
            allowedFixtureIds = nextAllowedFixtureIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList() ?? new List<string>();
        }

        public static BranchGenerationSettingsDefinition CreateRuntimeDefault()
        {
            var settings = CreateInstance<BranchGenerationSettingsDefinition>();
            settings.Configure(
                15001,
                8,
                250,
                nextAllowLoops: false,
                nextEnableBossLeaf: true,
                nextEnableTreasureLeaf: false,
                nextAllowedFixtureIds: new[]
                {
                    "combat_macro_single_1x1",
                    "combat_macro_wide_2x1",
                    "combat_macro_tall_1x2",
                    "combat_macro_block_2x2",
                    "combat_macro_l_3cell"
                });
            return settings;
        }
    }
}
