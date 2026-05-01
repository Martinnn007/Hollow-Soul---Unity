using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string weaponId;
        [SerializeField] private string displayName;
        [SerializeField] private WeaponSlot slot;
        [SerializeField] private WeaponCategory category;
        [SerializeField] private EquipmentLoadClass loadClass = EquipmentLoadClass.Light;
        [SerializeField] private BuildTag[] tags = System.Array.Empty<BuildTag>();
        [SerializeField] private WeaponAttackDefinition lightAttack;
        [SerializeField] private WeaponAttackDefinition heavyAttack;

        public string WeaponId => weaponId;

        public string DisplayName => displayName;

        public WeaponSlot Slot => slot;

        public WeaponCategory Category => category;

        public EquipmentLoadClass LoadClass => loadClass;

        public IReadOnlyList<BuildTag> Tags => tags;

        public WeaponAttackDefinition LightAttack => lightAttack.Damage <= 0 ? WeaponAttackDefinition.DefaultLight(slot) : lightAttack;

        public WeaponAttackDefinition HeavyAttack => heavyAttack.Damage <= 0 ? WeaponAttackDefinition.DefaultHeavy(slot) : heavyAttack;

        public void Configure(
            string nextWeaponId,
            string nextDisplayName,
            WeaponSlot nextSlot,
            WeaponCategory nextCategory,
            IEnumerable<BuildTag> nextTags = null,
            WeaponAttackDefinition? nextLightAttack = null,
            WeaponAttackDefinition? nextHeavyAttack = null,
            EquipmentLoadClass nextLoadClass = EquipmentLoadClass.Light)
        {
            weaponId = nextWeaponId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            slot = nextSlot;
            category = nextCategory;
            loadClass = nextLoadClass;
            tags = (nextTags ?? Enumerable.Empty<BuildTag>())
                .Where(tag => tag != BuildTag.None)
                .Distinct()
                .ToArray();
            lightAttack = nextLightAttack ?? WeaponAttackDefinition.DefaultLight(slot);
            heavyAttack = nextHeavyAttack ?? WeaponAttackDefinition.DefaultHeavy(slot);
        }
    }
}
