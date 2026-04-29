using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public sealed class ChallengeRunLoadout
    {
        [SerializeField] private string meleeWeaponId = string.Empty;
        [SerializeField] private string rangedWeaponId = string.Empty;
        [SerializeField] private string armorId = string.Empty;
        [SerializeField] private string activeItemId = string.Empty;
        [SerializeField] private string consumableCardId = string.Empty;

        public string MeleeWeaponId => meleeWeaponId ?? string.Empty;

        public string RangedWeaponId => rangedWeaponId ?? string.Empty;

        public string ArmorId => armorId ?? string.Empty;

        public string ActiveItemId => activeItemId ?? string.Empty;

        public string ConsumableCardId => consumableCardId ?? string.Empty;

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(MeleeWeaponId) &&
            string.IsNullOrWhiteSpace(RangedWeaponId) &&
            string.IsNullOrWhiteSpace(ArmorId) &&
            string.IsNullOrWhiteSpace(ActiveItemId) &&
            string.IsNullOrWhiteSpace(ConsumableCardId);

        public void Configure(
            string nextMeleeWeaponId,
            string nextRangedWeaponId,
            string nextArmorId,
            string nextActiveItemId,
            string nextConsumableCardId)
        {
            meleeWeaponId = nextMeleeWeaponId ?? string.Empty;
            rangedWeaponId = nextRangedWeaponId ?? string.Empty;
            armorId = nextArmorId ?? string.Empty;
            activeItemId = nextActiveItemId ?? string.Empty;
            consumableCardId = nextConsumableCardId ?? string.Empty;
        }

        public static ChallengeRunLoadout Create(
            string meleeWeaponId = "",
            string rangedWeaponId = "",
            string armorId = "",
            string activeItemId = "",
            string consumableCardId = "")
        {
            var loadout = new ChallengeRunLoadout();
            loadout.Configure(meleeWeaponId, rangedWeaponId, armorId, activeItemId, consumableCardId);
            return loadout;
        }
    }
}
