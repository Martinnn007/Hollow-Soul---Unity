using System;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Persistence;
using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class ReplacementPickup : MonoBehaviour, IPooledRuntimeObject
    {
        [SerializeField] private string pickupId;
        [SerializeField] private bool claimed;

        public string PickupId => pickupId;

        public void Configure(ReplacementPickupState state)
        {
            pickupId = state?.PickupId ?? string.Empty;
            claimed = false;
        }

        public bool Claim()
        {
            if (claimed)
            {
                return false;
            }

            claimed = true;
            return true;
        }

        public void OnRentFromPool()
        {
            claimed = false;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            pickupId = string.Empty;
            claimed = false;
        }
    }

    public sealed class ReplacementPickupState
    {
        public ReplacementPickupState(
            string pickupId,
            string roomId,
            RewardKind rewardKind,
            string rewardId,
            string displayName,
            int activeItemCharges,
            Vector3 localPosition)
        {
            PickupId = string.IsNullOrWhiteSpace(pickupId) ? Guid.NewGuid().ToString("N") : pickupId;
            RoomId = roomId ?? string.Empty;
            RewardKind = rewardKind;
            RewardId = rewardKind == RewardKind.Weapon
                ? WeaponIdAliases.Normalize(rewardId)
                : rewardId ?? string.Empty;
            DisplayName = rewardKind == RewardKind.Weapon
                ? WeaponIdAliases.NormalizeDisplayName(rewardId, displayName)
                : displayName ?? string.Empty;
            ActiveItemCharges = Math.Max(0, activeItemCharges);
            LocalPosition = localPosition;
        }

        public string PickupId { get; }

        public string RoomId { get; }

        public RewardKind RewardKind { get; }

        public string RewardId { get; }

        public string DisplayName { get; }

        public int ActiveItemCharges { get; }

        public Vector3 LocalPosition { get; }

        public RewardGrant ToGrant()
        {
            return new RewardGrant(PickupId, RewardId, DisplayName, RewardKind, 0, 0, Array.Empty<RewardEffect>());
        }

        public DroppedReplacementPickupSaveState ToSaveState()
        {
            return new DroppedReplacementPickupSaveState
            {
                pickupId = PickupId,
                roomId = RoomId,
                rewardKind = RewardKind.ToString(),
                rewardId = RewardId,
                displayName = DisplayName,
                activeItemCharges = ActiveItemCharges,
                localX = LocalPosition.x,
                localY = LocalPosition.y,
                localZ = LocalPosition.z
            };
        }

        public static ReplacementPickupState FromSaveState(DroppedReplacementPickupSaveState saveState)
        {
            if (saveState == null ||
                string.IsNullOrWhiteSpace(saveState.rewardId) ||
                !Enum.TryParse(saveState.rewardKind, out RewardKind kind))
            {
                return null;
            }

            return new ReplacementPickupState(
                saveState.pickupId,
                saveState.roomId,
                kind,
                saveState.rewardId,
                saveState.displayName,
                saveState.activeItemCharges,
                new Vector3(saveState.localX, saveState.localY, saveState.localZ));
        }
    }

    public static class RewardReplacementDetector
    {
        public static ReplacementPickupState CaptureBeforeApply(
            RewardGrant incomingGrant,
            PlayerRunBuild build,
            WeaponCatalogDefinition weaponCatalog,
            ArmorCatalogDefinition armorCatalog,
            UsableItemCatalogDefinition usableCatalog,
            Vector3 localPosition)
        {
            return CaptureBeforeApply(incomingGrant, build, weaponCatalog, armorCatalog, null, usableCatalog, localPosition);
        }

        public static ReplacementPickupState CaptureBeforeApply(
            RewardGrant incomingGrant,
            PlayerRunBuild build,
            WeaponCatalogDefinition weaponCatalog,
            ArmorCatalogDefinition armorCatalog,
            ShieldCatalogDefinition shieldCatalog,
            UsableItemCatalogDefinition usableCatalog,
            Vector3 localPosition)
        {
            if (build == null || incomingGrant.IsEmpty)
            {
                return null;
            }

            switch (incomingGrant.RewardKind)
            {
                case RewardKind.Weapon:
                    return CaptureWeapon(incomingGrant, build, weaponCatalog, localPosition);
                case RewardKind.Armor:
                    return CaptureArmor(incomingGrant, build, armorCatalog, localPosition);
                case RewardKind.Shield:
                    return CaptureShield(incomingGrant, build, shieldCatalog, localPosition);
                case RewardKind.ActiveItem:
                    return CaptureActiveItem(incomingGrant, build, usableCatalog, localPosition);
                case RewardKind.ConsumableCard:
                    return CaptureConsumableCard(incomingGrant, build, usableCatalog, localPosition);
                default:
                    return null;
            }
        }

        private static ReplacementPickupState CaptureWeapon(RewardGrant incomingGrant, PlayerRunBuild build, WeaponCatalogDefinition catalog, Vector3 localPosition)
        {
            var slot = WeaponSlot.Ranged;
            var incomingWeaponId = WeaponIdAliases.Normalize(incomingGrant.RewardId);
            if (catalog != null && catalog.TryGetWeapon(incomingGrant.RewardId, out var incomingWeapon))
            {
                slot = incomingWeapon.Slot;
                incomingWeaponId = incomingWeapon.WeaponId;
            }
            else if (incomingGrant.RewardId.Contains("blade") || incomingGrant.RewardId.Contains("cleaver") || incomingGrant.RewardId.Contains("sword") || incomingGrant.RewardId.Contains("fang"))
            {
                slot = WeaponSlot.Melee;
            }

            var oldId = slot == WeaponSlot.Melee ? build.Equipment.MeleeWeaponId : build.Equipment.RangedWeaponId;
            if (string.IsNullOrWhiteSpace(oldId) || oldId == incomingWeaponId)
            {
                return null;
            }

            var oldName = catalog != null && catalog.TryGetWeapon(oldId, out var oldWeapon) ? oldWeapon.DisplayName : oldId;
            return new ReplacementPickupState(string.Empty, incomingGrant.RoomId, RewardKind.Weapon, oldId, oldName, 0, localPosition);
        }

        private static ReplacementPickupState CaptureArmor(RewardGrant incomingGrant, PlayerRunBuild build, ArmorCatalogDefinition catalog, Vector3 localPosition)
        {
            var oldId = build.Equipment.ArmorId;
            if (string.IsNullOrWhiteSpace(oldId) || oldId == incomingGrant.RewardId)
            {
                return null;
            }

            var oldName = catalog != null && catalog.TryGetArmor(oldId, out var oldArmor) ? oldArmor.DisplayName : oldId;
            return new ReplacementPickupState(string.Empty, incomingGrant.RoomId, RewardKind.Armor, oldId, oldName, 0, localPosition);
        }

        private static ReplacementPickupState CaptureShield(RewardGrant incomingGrant, PlayerRunBuild build, ShieldCatalogDefinition catalog, Vector3 localPosition)
        {
            var oldId = build.Equipment.ShieldId;
            if (string.IsNullOrWhiteSpace(oldId) || oldId == incomingGrant.RewardId)
            {
                return null;
            }

            var oldName = catalog != null && catalog.TryGetShield(oldId, out var oldShield) ? oldShield.DisplayName : oldId;
            return new ReplacementPickupState(string.Empty, incomingGrant.RoomId, RewardKind.Shield, oldId, oldName, 0, localPosition);
        }

        private static ReplacementPickupState CaptureActiveItem(RewardGrant incomingGrant, PlayerRunBuild build, UsableItemCatalogDefinition catalog, Vector3 localPosition)
        {
            var oldId = build.Equipment.ActiveItemId;
            if (string.IsNullOrWhiteSpace(oldId) || oldId == incomingGrant.RewardId)
            {
                return null;
            }

            var oldName = catalog != null && catalog.TryGet(oldId, out var oldItem) ? oldItem.DisplayName : oldId;
            return new ReplacementPickupState(string.Empty, incomingGrant.RoomId, RewardKind.ActiveItem, oldId, oldName, build.Equipment.ActiveItemCharges, localPosition);
        }

        private static ReplacementPickupState CaptureConsumableCard(RewardGrant incomingGrant, PlayerRunBuild build, UsableItemCatalogDefinition catalog, Vector3 localPosition)
        {
            var oldId = build.Equipment.ConsumableCardId;
            if (string.IsNullOrWhiteSpace(oldId) || oldId == incomingGrant.RewardId)
            {
                return null;
            }

            var oldName = catalog != null && catalog.TryGet(oldId, out var oldCard) ? oldCard.DisplayName : oldId;
            return new ReplacementPickupState(string.Empty, incomingGrant.RoomId, RewardKind.ConsumableCard, oldId, oldName, 0, localPosition);
        }
    }
}
