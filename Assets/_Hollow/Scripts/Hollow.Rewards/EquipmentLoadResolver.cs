using Hollow.Data.Definitions;

namespace Hollow.Rewards
{
    public static class EquipmentLoadResolver
    {
        public static PlayerEquipmentLoadState Resolve(
            PlayerRunBuild build,
            WeaponCatalogDefinition weapons,
            ArmorCatalogDefinition armors,
            ShieldCatalogDefinition shields)
        {
            if (build == null)
            {
                return PlayerEquipmentLoadState.Default;
            }

            var meleeClass = ResolveWeaponLoad(build.Equipment.MeleeWeaponId, WeaponSlot.Melee, weapons);
            var rangedClass = ResolveWeaponLoad(build.Equipment.RangedWeaponId, WeaponSlot.Ranged, weapons);
            var armorClass = ResolveArmorLoad(build.Equipment.ArmorId, armors);
            var shield = ResolveShield(build.Equipment.ShieldId, shields);
            var shieldClass = shield != null ? shield.LoadClass : EquipmentLoadClass.Light;
            var score = Score(meleeClass) + Score(rangedClass) + Score(armorClass) + Score(shieldClass);
            var tier = ResolveTier(score);
            var shieldName = shield != null ? shield.DisplayName : "Starter Buckler";

            return new PlayerEquipmentLoadState(
                score,
                tier,
                SpeedMultiplier(tier),
                AttackStaminaCostMultiplier(tier),
                GuardStaminaCostMultiplier(tier),
                StaminaRegenMultiplier(tier),
                ArmorStabilityBonus(armorClass),
                ActiveGuardShieldStabilityBonus(shieldClass),
                shieldName);
        }

        public static int Score(EquipmentLoadClass loadClass)
        {
            return loadClass switch
            {
                EquipmentLoadClass.Heavy => 3,
                EquipmentLoadClass.Medium => 2,
                _ => 1
            };
        }

        public static EquipmentLoadTier ResolveTier(int totalLoadScore)
        {
            if (totalLoadScore >= 10)
            {
                return EquipmentLoadTier.Heavy;
            }

            return totalLoadScore >= 7 ? EquipmentLoadTier.Medium : EquipmentLoadTier.Light;
        }

        private static EquipmentLoadClass ResolveWeaponLoad(string weaponId, WeaponSlot slot, WeaponCatalogDefinition weapons)
        {
            var weapon = weapons != null ? weapons.Resolve(weaponId, slot) : null;
            return weapon != null ? weapon.LoadClass : EquipmentLoadClass.Light;
        }

        private static EquipmentLoadClass ResolveArmorLoad(string armorId, ArmorCatalogDefinition armors)
        {
            return !string.IsNullOrWhiteSpace(armorId) && armors != null && armors.TryGetArmor(armorId, out var armor)
                ? armor.LoadClass
                : EquipmentLoadClass.Light;
        }

        private static ShieldDefinition ResolveShield(string shieldId, ShieldCatalogDefinition shields)
        {
            if (shields != null)
            {
                return shields.Resolve(shieldId);
            }

            return null;
        }

        private static float SpeedMultiplier(EquipmentLoadTier tier)
        {
            return tier switch
            {
                EquipmentLoadTier.Heavy => 0.80f,
                EquipmentLoadTier.Medium => 0.92f,
                _ => 1f
            };
        }

        private static float AttackStaminaCostMultiplier(EquipmentLoadTier tier)
        {
            return tier switch
            {
                EquipmentLoadTier.Heavy => 1.25f,
                EquipmentLoadTier.Medium => 1.10f,
                _ => 1f
            };
        }

        private static float GuardStaminaCostMultiplier(EquipmentLoadTier tier)
        {
            return AttackStaminaCostMultiplier(tier);
        }

        private static float StaminaRegenMultiplier(EquipmentLoadTier tier)
        {
            return tier == EquipmentLoadTier.Heavy ? 0.90f : 1f;
        }

        private static int ArmorStabilityBonus(EquipmentLoadClass loadClass)
        {
            return loadClass switch
            {
                EquipmentLoadClass.Heavy => 2,
                EquipmentLoadClass.Medium => 1,
                _ => 0
            };
        }

        private static int ActiveGuardShieldStabilityBonus(EquipmentLoadClass loadClass)
        {
            return loadClass switch
            {
                EquipmentLoadClass.Heavy => 3,
                EquipmentLoadClass.Medium => 2,
                _ => 1
            };
        }
    }
}
