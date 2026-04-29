using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Room Hazard Tuning Profile", fileName = "RoomHazardTuningProfile")]
    public sealed class RoomHazardTuningProfileDefinition : HollowDefinition
    {
        public const string DefaultResourcePath = "Hollow/Combat/RoomHazardTuningProfile_M45";

        [SerializeField] private int spikeDamage = 1;
        [SerializeField] private float spikeCooldownSeconds = 0.85f;
        [SerializeField] private int barrelHealth = 1;
        [SerializeField] private int explosiveBarrelDamage = 2;
        [SerializeField] private int explosiveBarrelPlayerDamage = 1;
        [SerializeField] private float explosionRadiusMeters = 1.8f;
        [SerializeField] private float bossExplosionDamageMultiplier = 0.5f;
        [SerializeField] private int standardBarrelCoinDropChancePercent = 12;
        [SerializeField] private int standardBarrelCoinDropAmount = 1;

        public int SpikeDamage => Mathf.Max(1, spikeDamage);
        public float SpikeCooldownSeconds => Mathf.Max(0.05f, spikeCooldownSeconds);
        public int BarrelHealth => Mathf.Max(1, barrelHealth);
        public int ExplosiveBarrelDamage => Mathf.Max(1, explosiveBarrelDamage);
        public int ExplosiveBarrelPlayerDamage => Mathf.Max(1, explosiveBarrelPlayerDamage);
        public float ExplosionRadiusMeters => Mathf.Max(0.25f, explosionRadiusMeters);
        public float BossExplosionDamageMultiplier => Mathf.Clamp01(bossExplosionDamageMultiplier);
        public int StandardBarrelCoinDropChancePercent => Mathf.Clamp(standardBarrelCoinDropChancePercent, 0, 100);
        public int StandardBarrelCoinDropAmount => Mathf.Max(0, standardBarrelCoinDropAmount);

        public void ConfigureM45Defaults()
        {
            spikeDamage = 1;
            spikeCooldownSeconds = 0.85f;
            barrelHealth = 1;
            explosiveBarrelDamage = 2;
            explosiveBarrelPlayerDamage = 1;
            explosionRadiusMeters = 1.8f;
            bossExplosionDamageMultiplier = 0.5f;
            standardBarrelCoinDropChancePercent = 12;
            standardBarrelCoinDropAmount = 1;
        }

        public static RoomHazardTuningProfileDefinition CreateRuntimeDefault()
        {
            return CreateInstance<RoomHazardTuningProfileDefinition>();
        }

        public static RoomHazardTuningProfileDefinition Resolve(RoomHazardTuningProfileDefinition configured)
        {
            if (configured != null)
            {
                return configured;
            }

            var resource = Resources.Load<RoomHazardTuningProfileDefinition>(DefaultResourcePath);
            return resource != null ? resource : CreateRuntimeDefault();
        }
    }
}
