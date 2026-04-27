using Hollow.Persistence;

namespace Hollow.Rewards
{
    public sealed class RunEquipmentSlots
    {
        public string MeleeWeaponId { get; private set; } = "starter_blade";

        public string RangedWeaponId { get; private set; } = "starter_bolt";

        public string ActiveItemId { get; private set; } = string.Empty;

        public string ConsumableCardId { get; private set; } = string.Empty;

        public void EquipMeleeWeapon(string weaponId)
        {
            MeleeWeaponId = string.IsNullOrWhiteSpace(weaponId) ? "starter_blade" : weaponId;
        }

        public void EquipRangedWeapon(string weaponId)
        {
            RangedWeaponId = string.IsNullOrWhiteSpace(weaponId) ? "starter_bolt" : weaponId;
        }

        public void EquipActiveItem(string itemId)
        {
            ActiveItemId = itemId ?? string.Empty;
        }

        public void EquipConsumableCard(string cardId)
        {
            ConsumableCardId = cardId ?? string.Empty;
        }

        public RunEquipmentSlotsSaveState ToSaveState()
        {
            return new RunEquipmentSlotsSaveState
            {
                meleeWeaponId = MeleeWeaponId,
                rangedWeaponId = RangedWeaponId,
                activeItemId = ActiveItemId,
                consumableCardId = ConsumableCardId
            };
        }

        public static RunEquipmentSlots FromSaveState(RunEquipmentSlotsSaveState saveState)
        {
            var slots = new RunEquipmentSlots();
            if (saveState == null)
            {
                return slots;
            }

            slots.EquipMeleeWeapon(saveState.meleeWeaponId);
            slots.EquipRangedWeapon(saveState.rangedWeaponId);
            slots.EquipActiveItem(saveState.activeItemId);
            slots.EquipConsumableCard(saveState.consumableCardId);
            return slots;
        }
    }
}
