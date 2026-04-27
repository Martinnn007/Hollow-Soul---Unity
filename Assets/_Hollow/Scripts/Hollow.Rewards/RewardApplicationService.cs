using Hollow.Data.Definitions;

namespace Hollow.Rewards
{
    public static class RewardApplicationService
    {
        public static RewardApplicationResult Apply(
            RewardGrant grant,
            RunEconomy economy,
            PlayerRunStats stats,
            PlayerRunBuild build,
            WeaponCatalogDefinition weaponCatalog,
            UsableItemCatalogDefinition usableCatalog)
        {
            if (grant.IsEmpty || economy == null || build == null)
            {
                return new RewardApplicationResult(false, 0, "No reward");
            }

            if (!economy.ApplyReward(grant))
            {
                return new RewardApplicationResult(false, 0, "Already claimed");
            }

            var healAmount = stats?.ApplyReward(grant) ?? 0;
            switch (grant.RewardKind)
            {
                case RewardKind.PassiveItem:
                    build.Inventory.AddPassiveItem(grant.RewardId);
                    break;
                case RewardKind.Card:
                case RewardKind.PassiveCard:
                    build.Inventory.AddPassiveCard(grant.RewardId);
                    break;
                case RewardKind.ActiveItem:
                    build.Equipment.EquipActiveItem(grant.RewardId);
                    build.Equipment.SetActiveItemCharges(MaxChargesFor(grant.RewardId, usableCatalog));
                    break;
                case RewardKind.ConsumableCard:
                    build.Equipment.EquipConsumableCard(grant.RewardId);
                    break;
                case RewardKind.Weapon:
                    ApplyWeapon(grant.RewardId, build, weaponCatalog);
                    break;
                case RewardKind.Armor:
                    build.Equipment.EquipArmor(grant.RewardId);
                    break;
                case RewardKind.Heal:
                    healAmount += grant.Effects != null && grant.Effects.Count > 0
                        ? 0
                        : System.Math.Max(1, grant.Souls);
                    break;
            }

            return new RewardApplicationResult(true, healAmount, MessageFor(grant));
        }

        public static void RechargeActiveItem(PlayerRunBuild build, UsableItemCatalogDefinition usableCatalog, int amount = 1)
        {
            if (build == null || string.IsNullOrWhiteSpace(build.Equipment.ActiveItemId))
            {
                return;
            }

            build.Equipment.RechargeActiveItem(amount, MaxChargesFor(build.Equipment.ActiveItemId, usableCatalog));
        }

        private static int MaxChargesFor(string itemId, UsableItemCatalogDefinition usableCatalog)
        {
            return usableCatalog != null && usableCatalog.TryGet(itemId, out var item)
                ? item.MaxCharges
                : 3;
        }

        private static void ApplyWeapon(string weaponId, PlayerRunBuild build, WeaponCatalogDefinition weaponCatalog)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return;
            }

            if (weaponCatalog != null && weaponCatalog.TryGetWeapon(weaponId, out var weapon))
            {
                if (weapon.Slot == WeaponSlot.Melee)
                {
                    build.Equipment.EquipMeleeWeapon(weapon.WeaponId);
                    build.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);
                }
                else
                {
                    build.Equipment.EquipRangedWeapon(weapon.WeaponId);
                    build.Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
                }

                return;
            }

            if (weaponId.Contains("blade") || weaponId.Contains("cleaver") || weaponId.Contains("sword"))
            {
                build.Equipment.EquipMeleeWeapon(weaponId);
                build.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);
            }
            else
            {
                build.Equipment.EquipRangedWeapon(weaponId);
                build.Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
            }
        }

        private static string MessageFor(RewardGrant grant)
        {
            if (grant.Souls > 0 && grant.Coins > 0)
            {
                return $"Received: {grant.DisplayName} (+{grant.Souls} souls, +{grant.Coins} coins)";
            }

            if (grant.Coins > 0)
            {
                return $"Received: {grant.DisplayName} (+{grant.Coins} coins)";
            }

            return grant.Souls > 0
                ? $"Received: {grant.DisplayName} (+{grant.Souls} souls)"
                : $"Received: {grant.DisplayName}";
        }
    }
}
