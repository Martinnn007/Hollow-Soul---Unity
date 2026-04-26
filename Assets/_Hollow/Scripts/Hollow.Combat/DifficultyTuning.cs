using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct DifficultyTuning
    {
        public DifficultyTuning(float healthMultiplier, float speedMultiplier, float contactDamageMultiplier)
        {
            HealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            SpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            ContactDamageMultiplier = Mathf.Max(0.01f, contactDamageMultiplier);
        }

        public float HealthMultiplier { get; }

        public float SpeedMultiplier { get; }

        public float ContactDamageMultiplier { get; }

        public int ApplyHealth(int baseHealth)
        {
            return Mathf.Max(1, Mathf.CeilToInt(baseHealth * HealthMultiplier));
        }

        public float ApplySpeed(float baseSpeed)
        {
            return Mathf.Max(0f, baseSpeed * SpeedMultiplier);
        }

        public int ApplyContactDamage(int baseDamage)
        {
            return Mathf.Max(0, Mathf.CeilToInt(baseDamage * ContactDamageMultiplier));
        }
    }
}
