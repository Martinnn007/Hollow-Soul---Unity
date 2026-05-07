using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Weapon Catalog")]
    public sealed class WeaponCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<WeaponDefinition> weapons = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<WeaponDefinition> Weapons => weapons;

        public void Configure(string nextCatalogId, IEnumerable<WeaponDefinition> nextWeapons)
        {
            catalogId = nextCatalogId ?? string.Empty;
            weapons = (nextWeapons ?? Enumerable.Empty<WeaponDefinition>())
                .Where(weapon => weapon != null && !string.IsNullOrWhiteSpace(weapon.WeaponId))
                .GroupBy(weapon => weapon.WeaponId)
                .Select(group => group.First())
                .OrderBy(weapon => weapon.WeaponId)
                .ToList();
        }

        public bool TryGetWeapon(string weaponId, out WeaponDefinition weapon)
        {
            weapon = weapons.FirstOrDefault(candidate => candidate != null && candidate.WeaponId == weaponId);
            return weapon != null;
        }

        public WeaponDefinition Resolve(string weaponId, WeaponSlot fallbackSlot)
        {
            if (TryGetWeapon(weaponId, out var weapon) && weapon.Slot == fallbackSlot)
            {
                return weapon;
            }

            var starterId = fallbackSlot == WeaponSlot.Melee ? "starter_blade" : "starter_bow";
            return TryGetWeapon(starterId, out var starter) && starter.Slot == fallbackSlot ? starter : null;
        }

        public IReadOnlyList<WeaponDefinition> WeaponsForSlot(WeaponSlot slot)
        {
            return weapons.Where(weapon => weapon != null && weapon.Slot == slot).ToArray();
        }
    }
}
