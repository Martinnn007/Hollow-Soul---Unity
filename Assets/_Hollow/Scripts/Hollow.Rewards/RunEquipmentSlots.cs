using Hollow.Persistence;
using Hollow.Data.Definitions;

namespace Hollow.Rewards
{
    public sealed class RunEquipmentSlots
    {
        public string MeleeWeaponId { get; private set; } = "starter_blade";

        public string RangedWeaponId { get; private set; } = "starter_bolt";

        public WeaponSlot ActiveWeaponSlot { get; private set; } = WeaponSlot.Ranged;

        public string ActiveItemId { get; private set; } = string.Empty;

        public int ActiveItemCharges { get; private set; }

        public string ConsumableCardId { get; private set; } = string.Empty;

        public string ArmorId { get; private set; } = string.Empty;

        public void EquipMeleeWeapon(string weaponId)
        {
            MeleeWeaponId = string.IsNullOrWhiteSpace(weaponId) ? "starter_blade" : weaponId;
        }

        public void EquipRangedWeapon(string weaponId)
        {
            RangedWeaponId = string.IsNullOrWhiteSpace(weaponId) ? "starter_bolt" : weaponId;
        }

        public void SetActiveWeaponSlot(WeaponSlot slot)
        {
            ActiveWeaponSlot = slot;
        }

        public void EquipActiveItem(string itemId)
        {
            ActiveItemId = itemId ?? string.Empty;
        }

        public void SetActiveItemCharges(int charges)
        {
            ActiveItemCharges = System.Math.Max(0, charges);
        }

        public bool SpendActiveItemCharge()
        {
            if (ActiveItemCharges <= 0)
            {
                return false;
            }

            ActiveItemCharges--;
            return true;
        }

        public void RechargeActiveItem(int amount, int maxCharges)
        {
            if (string.IsNullOrWhiteSpace(ActiveItemId) || maxCharges <= 0 || amount <= 0)
            {
                return;
            }

            ActiveItemCharges = System.Math.Min(maxCharges, ActiveItemCharges + amount);
        }

        public void EquipConsumableCard(string cardId)
        {
            ConsumableCardId = cardId ?? string.Empty;
        }

        public void EquipArmor(string armorId)
        {
            ArmorId = armorId ?? string.Empty;
        }

        public RunEquipmentSlotsSaveState ToSaveState()
        {
            return new RunEquipmentSlotsSaveState
            {
                meleeWeaponId = MeleeWeaponId,
                rangedWeaponId = RangedWeaponId,
                activeWeaponSlot = ActiveWeaponSlot.ToString(),
                activeItemId = ActiveItemId,
                activeItemCharges = ActiveItemCharges,
                consumableCardId = ConsumableCardId,
                armorId = ArmorId
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
            if (System.Enum.TryParse(saveState.activeWeaponSlot, out WeaponSlot parsedSlot))
            {
                slots.SetActiveWeaponSlot(parsedSlot);
            }

            slots.EquipActiveItem(saveState.activeItemId);
            slots.SetActiveItemCharges(saveState.activeItemCharges);
            slots.EquipConsumableCard(saveState.consumableCardId);
            slots.EquipArmor(saveState.armorId);
            return slots;
        }
    }
}
