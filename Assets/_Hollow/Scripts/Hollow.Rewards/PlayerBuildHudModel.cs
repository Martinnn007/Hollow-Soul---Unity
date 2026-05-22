using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Rewards
{
    public readonly struct PlayerBuildHudModel
    {
        public PlayerBuildHudModel(
            string characterName,
            int currentHealth,
            int maxHealth,
            int defense,
            bool isGuarding,
            float speed,
            int strength,
            float currentStamina,
            float maxStamina,
            float staminaRegen,
            int meleeDamage,
            int rangedDamage,
            float meleeRangeBonusMeters,
            float rangedRangeBonusMeters,
            float cooldownMultiplier,
            int coins,
            int souls,
            string activeWeapon,
            string meleeWeapon,
            string rangedWeapon,
            string armor,
            string activeItem,
            string card,
            string activeSet)
            : this(
                characterName,
                currentHealth,
                maxHealth,
                defense,
                stability: 1,
                isGuarding,
                speed,
                strength,
                currentStamina,
                maxStamina,
                staminaRegen,
                meleeDamage,
                rangedDamage,
                meleeRangeBonusMeters,
                rangedRangeBonusMeters,
                cooldownMultiplier,
                coins,
                souls,
                0,
                false,
                0,
                0,
                0f,
                0,
                0,
                0f,
                0f,
                speed,
                0,
                "starter_blade",
                WeaponSlot.Melee,
                string.Empty,
                0,
                0,
                string.Empty,
                activeWeapon,
                meleeWeapon,
                rangedWeapon,
                armor,
                "Starter Buckler",
                PlayerEquipmentLoadState.Default,
                activeItem,
                card,
                activeSet)
        {
        }

        public PlayerBuildHudModel(
            string characterName,
            int currentHealth,
            int maxHealth,
            int defense,
            int stability,
            bool isGuarding,
            float speed,
            int strength,
            float currentStamina,
            float maxStamina,
            float staminaRegen,
            int meleeDamage,
            int rangedDamage,
            float meleeRangeBonusMeters,
            float rangedRangeBonusMeters,
            float cooldownMultiplier,
            int coins,
            int souls,
            string activeWeapon,
            string meleeWeapon,
            string rangedWeapon,
            string armor,
            string shield,
            PlayerEquipmentLoadState equipmentLoad,
            string activeItem,
            string card,
            string activeSet)
            : this(
                characterName,
                currentHealth,
                maxHealth,
                defense,
                stability,
                isGuarding,
                speed,
                strength,
                currentStamina,
                maxStamina,
                staminaRegen,
                meleeDamage,
                rangedDamage,
                meleeRangeBonusMeters,
                rangedRangeBonusMeters,
                cooldownMultiplier,
                coins,
                souls,
                0,
                false,
                0,
                0,
                0f,
                0,
                0,
                0f,
                0f,
                speed,
                0,
                "starter_blade",
                WeaponSlot.Melee,
                string.Empty,
                0,
                0,
                string.Empty,
                activeWeapon,
                meleeWeapon,
                rangedWeapon,
                armor,
                shield,
                equipmentLoad,
                activeItem,
                card,
                activeSet)
        {
        }

        public PlayerBuildHudModel(
            string characterName,
            int currentHealth,
            int maxHealth,
            int defense,
            int stability,
            bool isGuarding,
            float speed,
            int strength,
            float currentStamina,
            float maxStamina,
            float staminaRegen,
            int meleeDamage,
            int rangedDamage,
            float meleeRangeBonusMeters,
            float rangedRangeBonusMeters,
            float cooldownMultiplier,
            int coins,
            int souls,
            int keys,
            bool hasBossKey,
            int meleeLightDamage,
            int meleeHeavyDamage,
            float meleeLightAttacksPerSecond,
            int rangedLightDamage,
            int rangedHeavyDamage,
            float rangedLightAttacksPerSecond,
            float effectiveRangeMeters,
            float moveSpeedMetersPerSecond,
            int karma,
            string activeWeaponId,
            WeaponSlot activeWeaponSlot,
            string activeItemId,
            int activeItemCharges,
            int activeItemMaxCharges,
            string consumableCardId,
            string activeWeapon,
            string meleeWeapon,
            string rangedWeapon,
            string armor,
            string shield,
            PlayerEquipmentLoadState equipmentLoad,
            string activeItem,
            string card,
            string activeSet)
        {
            CharacterName = characterName ?? "Unknown";
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Defense = defense;
            Stability = stability;
            IsGuarding = isGuarding;
            Speed = speed;
            Strength = strength;
            CurrentStamina = currentStamina;
            MaxStamina = maxStamina;
            StaminaRegen = staminaRegen;
            MeleeDamage = meleeDamage;
            RangedDamage = rangedDamage;
            MeleeRangeBonusMeters = meleeRangeBonusMeters;
            RangedRangeBonusMeters = rangedRangeBonusMeters;
            CooldownMultiplier = cooldownMultiplier;
            Coins = coins;
            Souls = souls;
            Keys = keys;
            HasBossKey = hasBossKey;
            MeleeLightDamage = Mathf.Max(0, meleeLightDamage);
            MeleeHeavyDamage = Mathf.Max(0, meleeHeavyDamage);
            MeleeLightAttacksPerSecond = Mathf.Max(0f, meleeLightAttacksPerSecond);
            RangedLightDamage = Mathf.Max(0, rangedLightDamage);
            RangedHeavyDamage = Mathf.Max(0, rangedHeavyDamage);
            RangedLightAttacksPerSecond = Mathf.Max(0f, rangedLightAttacksPerSecond);
            EffectiveRangeMeters = Mathf.Max(0f, effectiveRangeMeters);
            MoveSpeedMetersPerSecond = Mathf.Max(0f, moveSpeedMetersPerSecond);
            Karma = Mathf.Clamp(karma, -3, 3);
            ActiveWeaponId = ResolveActiveWeaponId(activeWeaponId, activeWeaponSlot);
            ActiveWeaponSlot = activeWeaponSlot;
            ActiveItemId = NormalizeOptionalId(activeItemId);
            ActiveItemCharges = Mathf.Max(0, activeItemCharges);
            ActiveItemMaxCharges = Mathf.Max(0, activeItemMaxCharges);
            ConsumableCardId = NormalizeOptionalId(consumableCardId);
            ActiveWeapon = activeWeapon ?? "None";
            MeleeWeapon = meleeWeapon ?? "None";
            RangedWeapon = rangedWeapon ?? "None";
            Armor = armor ?? "None";
            Shield = shield ?? "Starter Buckler";
            EquipmentLoad = equipmentLoad.TotalLoadScore <= 0 ? PlayerEquipmentLoadState.Default : equipmentLoad;
            ActiveItem = activeItem ?? "None";
            Card = card ?? "None";
            ActiveSet = activeSet ?? "None";
        }

        public string CharacterName { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public int Defense { get; }
        public int Stability { get; }
        public bool IsGuarding { get; }
        public float Speed { get; }
        public int Strength { get; }
        public float CurrentStamina { get; }
        public float MaxStamina { get; }
        public float StaminaRegen { get; }
        public int MeleeDamage { get; }
        public int RangedDamage { get; }
        public float MeleeRangeBonusMeters { get; }
        public float RangedRangeBonusMeters { get; }
        public float CooldownMultiplier { get; }
        public int Coins { get; }
        public int Souls { get; }
        public int Keys { get; }
        public bool HasBossKey { get; }
        public int MeleeLightDamage { get; }
        public int MeleeHeavyDamage { get; }
        public float MeleeLightAttacksPerSecond { get; }
        public int RangedLightDamage { get; }
        public int RangedHeavyDamage { get; }
        public float RangedLightAttacksPerSecond { get; }
        public float EffectiveRangeMeters { get; }
        public float MoveSpeedMetersPerSecond { get; }
        public int Karma { get; }
        public string ActiveWeaponId { get; }
        public WeaponSlot ActiveWeaponSlot { get; }
        public string ActiveItemId { get; }
        public int ActiveItemCharges { get; }
        public int ActiveItemMaxCharges { get; }
        public string ConsumableCardId { get; }
        public string ActiveWeapon { get; }
        public string MeleeWeapon { get; }
        public string RangedWeapon { get; }
        public string Armor { get; }
        public string Shield { get; }
        public PlayerEquipmentLoadState EquipmentLoad { get; }
        public string ActiveItem { get; }
        public string Card { get; }
        public string ActiveSet { get; }

        public string BodyText =>
            $"BUILD\n" +
            $"Character: {CharacterName}\n" +
            $"HP: {CurrentHealth}/{MaxHealth}  DEF: {Defense}{(IsGuarding ? " Guard" : string.Empty)}\n" +
            $"Speed: {Speed:0.0}  STR: {Strength}\n" +
            $"Stamina: {CurrentStamina:0}/{MaxStamina:0} (+{StaminaRegen:0}/s)\n" +
            $"Melee: +{MeleeDamage}  Ranged: +{RangedDamage}\n" +
            $"Range: M +{MeleeRangeBonusMeters:0.##}m  R +{RangedRangeBonusMeters:0.##}m\n" +
            $"Stability: {Stability} (+{EquipmentLoad.ActiveGuardShieldStabilityBonus} guard)\n" +
            $"Load: {EquipmentLoad.Tier} ({EquipmentLoad.TotalLoadScore}/12)\n" +
            $"Cooldown: x{CooldownMultiplier:0.##}\n" +
            $"Coins: {Coins}  Souls: {Souls}  Keys: {(HasBossKey ? 1 : Keys)}\n" +
            $"Stats: M {MeleeLightDamage}/{MeleeHeavyDamage} @ {MeleeLightAttacksPerSecond:0.0}/s  R {RangedLightDamage}/{RangedHeavyDamage} @ {RangedLightAttacksPerSecond:0.0}/s  Range {EffectiveRangeMeters:0.0}m  Karma {FormatKarma(Karma)}\n\n" +
            $"Active: {ActiveWeapon}\n" +
            $"Melee: {MeleeWeapon}\n" +
            $"Ranged: {RangedWeapon}\n" +
            $"Armor: {Armor}\n" +
            $"Shield: {Shield}\n" +
            $"Item: {ActiveItem}\n" +
            $"Card: {Card}\n" +
            $"Set: {ActiveSet}";

        private static string FormatKarma(int karma)
        {
            return karma > 0 ? $"+{karma}" : karma.ToString();
        }

        private static string ResolveActiveWeaponId(string activeWeaponId, WeaponSlot activeWeaponSlot)
        {
            var normalized = WeaponIdAliases.Normalize(activeWeaponId);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            return activeWeaponSlot == WeaponSlot.Ranged ? WeaponIdAliases.StarterPistolId : "starter_blade";
        }

        private static string NormalizeOptionalId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }
}
