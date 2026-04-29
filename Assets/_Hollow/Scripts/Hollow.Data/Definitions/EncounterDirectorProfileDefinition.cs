using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Branches/Encounter Director Profile", fileName = "EncounterDirectorProfile")]
    public sealed class EncounterDirectorProfileDefinition : HollowDefinition
    {
        public const string DefaultResourcePath = "Hollow/Branches/EncounterDirectorProfile_M46";

        [SerializeField] private int maxNonBossEnemySpawns = 6;
        [SerializeField] private List<EncounterDirectorWorldConfig> worldConfigs = new();

        public int MaxNonBossEnemySpawns => Mathf.Max(1, maxNonBossEnemySpawns);
        public IReadOnlyList<EncounterDirectorWorldConfig> WorldConfigs => worldConfigs;

        public EncounterDirectorWorldConfig WorldConfigFor(int worldIndex)
        {
            var normalized = Mathf.Max(1, worldIndex);
            var exact = worldConfigs.FirstOrDefault(config => config != null && config.WorldIndex == normalized);
            if (exact != null)
            {
                return exact;
            }

            return worldConfigs
                       .Where(config => config != null)
                       .OrderBy(config => Mathf.Abs(config.WorldIndex - normalized))
                       .ThenBy(config => config.WorldIndex)
                       .FirstOrDefault() ?? CreateWorldConfig(1, 8, 0, 0, 0);
        }

        public void ConfigureM46Defaults()
        {
            maxNonBossEnemySpawns = 6;
            worldConfigs = new List<EncounterDirectorWorldConfig>
            {
                CreateWorldConfig(1, 8, 0, 0, 0),
                CreateWorldConfig(2, 10, 1, 2, 1),
                CreateWorldConfig(3, 12, 2, 3, 2)
            };
        }

        public static EncounterDirectorProfileDefinition CreateRuntimeDefault()
        {
            var profile = CreateInstance<EncounterDirectorProfileDefinition>();
            profile.ConfigureM46Defaults();
            return profile;
        }

        public static EncounterDirectorProfileDefinition Resolve(EncounterDirectorProfileDefinition configured)
        {
            if (configured != null)
            {
                return configured;
            }

            var resource = Resources.Load<EncounterDirectorProfileDefinition>(DefaultResourcePath);
            return resource != null ? resource : CreateRuntimeDefault();
        }

        private static EncounterDirectorWorldConfig CreateWorldConfig(int worldIndex, int targetRooms, int offset, int hardWeight, int veryHardWeight)
        {
            var config = new EncounterDirectorWorldConfig();
            config.Configure(worldIndex, targetRooms, offset, hardWeight, veryHardWeight);
            return config;
        }
    }
}
