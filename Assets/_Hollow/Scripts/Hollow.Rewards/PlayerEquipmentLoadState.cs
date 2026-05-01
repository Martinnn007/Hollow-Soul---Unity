namespace Hollow.Rewards
{
    public readonly struct PlayerEquipmentLoadState
    {
        public PlayerEquipmentLoadState(
            int totalLoadScore,
            EquipmentLoadTier tier,
            float speedMultiplier,
            float attackStaminaCostMultiplier,
            float guardStaminaCostMultiplier,
            float staminaRegenMultiplier,
            int armorStabilityBonus,
            int activeGuardShieldStabilityBonus,
            string shieldDisplayName)
        {
            TotalLoadScore = totalLoadScore;
            Tier = tier;
            SpeedMultiplier = speedMultiplier;
            AttackStaminaCostMultiplier = attackStaminaCostMultiplier;
            GuardStaminaCostMultiplier = guardStaminaCostMultiplier;
            StaminaRegenMultiplier = staminaRegenMultiplier;
            ArmorStabilityBonus = armorStabilityBonus;
            ActiveGuardShieldStabilityBonus = activeGuardShieldStabilityBonus;
            ShieldDisplayName = string.IsNullOrWhiteSpace(shieldDisplayName) ? "Starter Buckler" : shieldDisplayName;
        }

        public int TotalLoadScore { get; }

        public EquipmentLoadTier Tier { get; }

        public float SpeedMultiplier { get; }

        public float AttackStaminaCostMultiplier { get; }

        public float GuardStaminaCostMultiplier { get; }

        public float StaminaRegenMultiplier { get; }

        public int ArmorStabilityBonus { get; }

        public int ActiveGuardShieldStabilityBonus { get; }

        public string ShieldDisplayName { get; }

        public static PlayerEquipmentLoadState Default => new(
            totalLoadScore: 4,
            tier: EquipmentLoadTier.Light,
            speedMultiplier: 1f,
            attackStaminaCostMultiplier: 1f,
            guardStaminaCostMultiplier: 1f,
            staminaRegenMultiplier: 1f,
            armorStabilityBonus: 0,
            activeGuardShieldStabilityBonus: 1,
            shieldDisplayName: "Starter Buckler");
    }
}
