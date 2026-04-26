using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string spawnKind = "spawnEnemyNormal";
        [SerializeField] private string displayName = "Normal Chaser";
        [SerializeField] private EnemyArchetypeId archetypeId = EnemyArchetypeId.Normal;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Grounded;
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float speedMetersPerSecond = 1.5f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactCooldownSeconds = 1f;
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private Color color = new(0.85f, 0.16f, 0.14f, 1f);

        public string SpawnKind => spawnKind;

        public string DisplayName => displayName;

        public EnemyArchetypeId ArchetypeId => archetypeId;

        public EnemyMovementMode MovementMode => movementMode;

        public int MaxHealth => maxHealth;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public int ContactDamage => contactDamage;

        public float ContactCooldownSeconds => contactCooldownSeconds;

        public float RadiusMeters => radiusMeters;

        public Color Color => color;

        public void Configure(
            string nextSpawnKind,
            string nextDisplayName,
            EnemyArchetypeId nextArchetypeId,
            EnemyMovementMode nextMovementMode,
            int nextMaxHealth,
            float nextSpeedMetersPerSecond,
            int nextContactDamage,
            float nextContactCooldownSeconds,
            float nextRadiusMeters,
            Color nextColor)
        {
            spawnKind = nextSpawnKind;
            displayName = nextDisplayName;
            archetypeId = nextArchetypeId;
            movementMode = nextMovementMode;
            maxHealth = Mathf.Max(1, nextMaxHealth);
            speedMetersPerSecond = Mathf.Max(0f, nextSpeedMetersPerSecond);
            contactDamage = Mathf.Max(0, nextContactDamage);
            contactCooldownSeconds = Mathf.Max(0.01f, nextContactCooldownSeconds);
            radiusMeters = Mathf.Max(0.01f, nextRadiusMeters);
            color = nextColor;
        }

        public static EnemyDefinition CreateRuntime(
            string spawnKind,
            string displayName,
            EnemyArchetypeId archetypeId,
            EnemyMovementMode movementMode,
            int maxHealth,
            float speedMetersPerSecond,
            int contactDamage,
            Color color)
        {
            var definition = CreateInstance<EnemyDefinition>();
            definition.Configure(spawnKind, displayName, archetypeId, movementMode, maxHealth, speedMetersPerSecond, contactDamage, 1f, 0.32f, color);
            return definition;
        }

        public static EnemyDefinition CreateRuntimeNormal()
        {
            return CreateRuntime("spawnEnemyNormal", "Normal Chaser", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 3, 1.5f, 1, new Color(0.85f, 0.16f, 0.14f, 1f));
        }
    }
}
