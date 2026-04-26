using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinition> definitions = new();
        [SerializeField] private EnemyDefinition fallbackDefinition;

        public IReadOnlyList<EnemyDefinition> Definitions => definitions;

        public EnemyDefinition FallbackDefinition => fallbackDefinition != null ? fallbackDefinition : definitions.FirstOrDefault();

        public void Configure(IEnumerable<EnemyDefinition> nextDefinitions, EnemyDefinition nextFallback)
        {
            definitions = nextDefinitions?.Where(definition => definition != null).ToList() ?? new List<EnemyDefinition>();
            fallbackDefinition = nextFallback != null ? nextFallback : definitions.FirstOrDefault();
        }

        public EnemyDefinition Resolve(string spawnKind)
        {
            var definition = definitions.FirstOrDefault(candidate => candidate != null && candidate.SpawnKind == spawnKind);
            return definition != null ? definition : FallbackDefinition;
        }

        public static EnemyCatalog CreateRuntimeDefault()
        {
            var normal = EnemyDefinition.CreateRuntimeNormal();
            var flying = EnemyDefinition.CreateRuntime("spawnEnemyFlying", "Flying Chaser", EnemyArchetypeId.Flying, EnemyMovementMode.Flying, 3, 1.8f, 1, new Color(0.25f, 0.65f, 1f, 1f));
            var fast = EnemyDefinition.CreateRuntime("spawnEnemyFast", "Fast Chaser", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 2, 2.4f, 1, new Color(1f, 0.66f, 0.18f, 1f));
            var heavy = EnemyDefinition.CreateRuntime("spawnEnemyHeavy", "Heavy Chaser", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 6, 0.9f, 2, new Color(0.62f, 0.22f, 0.82f, 1f));

            var catalog = CreateInstance<EnemyCatalog>();
            catalog.Configure(new[] { normal, flying, fast, heavy }, normal);
            return catalog;
        }
    }
}
