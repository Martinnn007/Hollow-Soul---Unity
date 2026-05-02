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
            var flying = EnemyDefinition.CreateRuntime("spawnEnemyFlying", "Flying Chaser", EnemyArchetypeId.Flying, EnemyMovementMode.Flying, 3, 1.8f, 1, EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Prey, new Color(0.25f, 0.65f, 1f, 1f));
            var fast = EnemyDefinition.CreateRuntime("spawnEnemyFast", "Fast Chaser", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 2, 2.4f, 1, new Color(1f, 0.66f, 0.18f, 1f));
            var heavy = EnemyDefinition.CreateRuntime("spawnEnemyHeavy", "Heavy Chaser", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 6, 0.9f, 2, new Color(0.62f, 0.22f, 0.82f, 1f));
            var boss = EnemyDefinition.CreateRuntimeBoss();
            var charger = EnemyDefinition.CreateRuntime("spawnEnemyCharger", "Ash Charger", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 4, 1.2f, 1, new Color(1f, 0.34f, 0.12f, 1f));
            charger.Configure("spawnEnemyCharger", "Ash Charger", EnemyArchetypeId.Fast, EnemyBehaviorId.Charger, EnemyMovementMode.Grounded, 4, 1.2f, 1, 1f, 0.34f, 5.5f, 1.2f, 1, 5f, 5.5f, 2f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Predator, new Color(1f, 0.34f, 0.12f, 1f));
            var turret = EnemyDefinition.CreateRuntime("spawnEnemyTurret", "Bone Turret", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 4, 0f, 1, new Color(0.72f, 0.86f, 0.94f, 1f));
            turret.Configure("spawnEnemyTurret", "Bone Turret", EnemyArchetypeId.Heavy, EnemyBehaviorId.TurretShooter, EnemyMovementMode.Grounded, 4, 0f, 1, 1f, 0.34f, 8f, 1.35f, 1, 4.8f, 0f, 2f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, new Color(0.72f, 0.86f, 0.94f, 1f));
            var splitter = EnemyDefinition.CreateRuntime("spawnEnemySplitter", "Husk Splitter", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 5, 1.1f, 1, new Color(0.55f, 0.95f, 0.35f, 1f));
            splitter.Configure("spawnEnemySplitter", "Husk Splitter", EnemyArchetypeId.Normal, EnemyBehaviorId.Splitter, EnemyMovementMode.Grounded, 5, 1.1f, 1, 1f, 0.36f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 2, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator, new Color(0.55f, 0.95f, 0.35f, 1f));

            var catalog = CreateInstance<EnemyCatalog>();
            catalog.Configure(new[] { normal, flying, fast, heavy, boss, charger, turret, splitter }, normal);
            return catalog;
        }
    }
}
