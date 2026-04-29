using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Persistence;
using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class PlayerRunBuild
    {
        private readonly List<PlayerStatModifier> modifiers = new();

        public PlayerRunBuild()
        {
            BaseStats = PlayerBaseStats.Default;
            CurrentStamina = BaseStats.MaxStamina;
        }

        public string SelectedCharacterId { get; private set; } = "balanced";

        public PlayerBaseStats BaseStats { get; private set; }

        public float CurrentStamina { get; private set; }

        public RunCurrencyWallet Wallet { get; private set; } = new();

        public RunEquipmentSlots Equipment { get; private set; } = new();

        public RunInventoryState Inventory { get; private set; } = new();

        public IReadOnlyList<PlayerStatModifier> Modifiers => modifiers;

        public PlayerDerivedStats DerivedStats
        {
            get
            {
                var cooldownMultiplier = BaseStats.AttackCooldownMultiplier;
                var maxHealth = BaseStats.MaxHealth;
                var speed = BaseStats.SpeedMetersPerSecond;
                var strength = BaseStats.Strength;
                var maxStamina = BaseStats.MaxStamina;
                var staminaRegen = BaseStats.StaminaRegenPerSecond;
                var defense = BaseStats.Defense;
                var meleeDamage = BaseStats.MeleeDamageBonus;
                var rangedDamage = BaseStats.RangedDamageBonus;
                var meleeRange = BaseStats.MeleeRangeBonusMeters;
                var rangedRange = BaseStats.RangedRangeBonusMeters;

                foreach (var modifier in modifiers)
                {
                    maxHealth += modifier.maxHealth;
                    speed += modifier.speed;
                    strength += modifier.strength;
                    maxStamina += modifier.maxStamina;
                    staminaRegen += modifier.staminaRegen;
                    defense += modifier.defense;
                    meleeDamage += modifier.meleeDamage;
                    rangedDamage += modifier.rangedDamage;
                    meleeRange += modifier.meleeRangeBonusMeters;
                    rangedRange += modifier.rangedRangeBonusMeters;
                    if (modifier.attackCooldownMultiplier > 0f)
                    {
                        cooldownMultiplier *= modifier.attackCooldownMultiplier;
                    }
                }

                return new PlayerDerivedStats(
                    maxHealth,
                    speed,
                    strength,
                    maxStamina,
                    staminaRegen,
                    defense,
                    meleeDamage,
                    rangedDamage,
                    cooldownMultiplier,
                    meleeRange,
                    rangedRange);
            }
        }

        public void ConfigureCharacter(string characterId, PlayerBaseStats baseStats)
        {
            SelectedCharacterId = string.IsNullOrWhiteSpace(characterId) ? "balanced" : characterId;
            BaseStats = baseStats.IsConfigured ? baseStats : PlayerBaseStats.Default;
            CurrentStamina = Mathf.Clamp(CurrentStamina <= 0f ? BaseStats.MaxStamina : CurrentStamina, 0f, DerivedStats.MaxStamina);
        }

        public void ConfigureCharacter(CharacterDefinition character)
        {
            if (character == null)
            {
                ConfigureCharacter("balanced", PlayerBaseStats.Default);
                Equipment.EquipMeleeWeapon("starter_blade");
                Equipment.EquipRangedWeapon("starter_bolt");
                Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
                CurrentStamina = DerivedStats.MaxStamina;
                return;
            }

            ConfigureCharacter(character.CharacterId, character.BaseStats);
            Equipment.EquipMeleeWeapon(character.StarterMeleeWeaponId);
            Equipment.EquipRangedWeapon(character.StarterRangedWeaponId);
            Equipment.SetActiveWeaponSlot(WeaponSlot.Ranged);
            ApplyPassiveSkill(character.PassiveSkill);
            if (!string.IsNullOrWhiteSpace(character.StarterPassiveRewardId))
            {
                Inventory.AddPassiveItem(character.StarterPassiveRewardId);
            }

            CurrentStamina = DerivedStats.MaxStamina;
        }

        public void ApplyPassiveSkill(CharacterPassiveSkillDefinition passiveSkill)
        {
            if (passiveSkill == null || passiveSkill.StatModifier.IsEmpty)
            {
                return;
            }

            var sourceId = string.IsNullOrWhiteSpace(passiveSkill.SkillId) ? "character_passive" : $"character:{passiveSkill.SkillId}";
            AddModifier(PlayerStatModifier.FromCharacterStatModifier(sourceId, passiveSkill.StatModifier));
        }

        public void AddModifier(PlayerStatModifier modifier)
        {
            if (!modifier.IsEmpty)
            {
                modifiers.Add(modifier);
                CurrentStamina = Mathf.Min(CurrentStamina, DerivedStats.MaxStamina);
            }
        }

        public bool SpendStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (CurrentStamina + 0.001f < amount)
            {
                return false;
            }

            CurrentStamina -= amount;
            return true;
        }

        public void RegenerateStamina(float deltaTime)
        {
            CurrentStamina = Mathf.Min(DerivedStats.MaxStamina, CurrentStamina + Mathf.Max(0f, deltaTime) * DerivedStats.StaminaRegenPerSecond);
        }

        public PlayerRunBuildSaveState ToSaveState()
        {
            return new PlayerRunBuildSaveState
            {
                selectedCharacterId = SelectedCharacterId,
                currentStamina = CurrentStamina,
                baseMaxHealth = BaseStats.MaxHealth,
                baseSpeed = BaseStats.SpeedMetersPerSecond,
                baseStrength = BaseStats.Strength,
                baseMaxStamina = BaseStats.MaxStamina,
                baseStaminaRegen = BaseStats.StaminaRegenPerSecond,
                baseDefense = BaseStats.Defense,
                baseMeleeDamageBonus = BaseStats.MeleeDamageBonus,
                baseRangedDamageBonus = BaseStats.RangedDamageBonus,
                baseAttackCooldownMultiplier = BaseStats.AttackCooldownMultiplier,
                baseMeleeRangeBonusMeters = BaseStats.MeleeRangeBonusMeters,
                baseRangedRangeBonusMeters = BaseStats.RangedRangeBonusMeters,
                wallet = Wallet.ToSaveState(),
                equipment = Equipment.ToSaveState(),
                inventory = Inventory.ToSaveState(),
                modifiers = modifiers.Select(modifier => modifier.ToSaveState()).ToList()
            };
        }

        public static PlayerRunBuild FromSaveState(PlayerRunBuildSaveState saveState)
        {
            var build = new PlayerRunBuild();
            if (saveState == null)
            {
                return build;
            }

            build.SelectedCharacterId = string.IsNullOrWhiteSpace(saveState.selectedCharacterId) ? "balanced" : saveState.selectedCharacterId;
            build.BaseStats = new PlayerBaseStats(
                saveState.baseMaxHealth <= 0 ? PlayerBaseStats.Default.MaxHealth : saveState.baseMaxHealth,
                saveState.baseSpeed <= 0f ? PlayerBaseStats.Default.SpeedMetersPerSecond : saveState.baseSpeed,
                saveState.baseStrength,
                saveState.baseMaxStamina <= 0f ? PlayerBaseStats.Default.MaxStamina : saveState.baseMaxStamina,
                saveState.baseStaminaRegen <= 0f ? PlayerBaseStats.Default.StaminaRegenPerSecond : saveState.baseStaminaRegen,
                saveState.baseDefense,
                saveState.baseMeleeDamageBonus,
                saveState.baseRangedDamageBonus,
                saveState.baseAttackCooldownMultiplier <= 0f ? 1f : saveState.baseAttackCooldownMultiplier,
                saveState.baseMeleeRangeBonusMeters,
                saveState.baseRangedRangeBonusMeters);
            build.Wallet = RunCurrencyWallet.FromSaveState(saveState.wallet);
            build.Equipment = RunEquipmentSlots.FromSaveState(saveState.equipment);
            build.Inventory = RunInventoryState.FromSaveState(saveState.inventory);
            foreach (var modifier in saveState.modifiers ?? new List<PlayerStatModifierSaveState>())
            {
                build.AddModifier(PlayerStatModifier.FromSaveState(modifier));
            }

            build.CurrentStamina = Mathf.Clamp(saveState.currentStamina <= 0f ? build.DerivedStats.MaxStamina : saveState.currentStamina, 0f, build.DerivedStats.MaxStamina);
            return build;
        }

        public static PlayerRunBuild FromLegacy(PlayerRunStats stats, RunEconomy economy)
        {
            var build = new PlayerRunBuild();
            if (economy != null)
            {
                build.Wallet.AddSouls(economy.RunSouls);
                build.Wallet.AddCoins(economy.RunCoins);
            }

            if (stats != null)
            {
                build.AddModifier(stats.ToStatModifier("legacy_player_run_stats"));
            }

            return build;
        }
    }
}
