using System;
using System.Collections.Generic;

namespace Hollow.Persistence
{
    [Serializable]
    public sealed class RunSaveSnapshot
    {
        public string runId = string.Empty;
        public string branchId = "m7_five_room_cross";
        public int branchSeed;
        public string currentRoomId = "origin";
        public string platformKind = string.Empty;
        public int playerCurrentHealth = 6;
        public int branchDepth;
        public int currentBranchSeed;
        public int runSeed;
        public int worldIndex = 1;
        public string worldPhase = "Legacy";
        public string activeHubPortalId = string.Empty;
        public int hubShopRefreshIndex;
        public string bossKeyState = "None";
        public string bossKeyRoomId = string.Empty;
        public string secretRoomId = string.Empty;
        public bool bossDoorUnlocked;
        public long savedAtUtcTicks;
        public List<BranchRoomSaveState> rooms = new();
        public List<RunRewardSaveState> proceduralRewardPlan = new();
        public List<RoomEncounterSaveState> encounterPlan = new();
        public HubShopStateSaveState interBranchHub = new();
        public RunEconomySaveState economy = new();
        public PlayerRunStatsSaveState playerStats = new();
        public PlayerRunBuildSaveState runBuild = new();
    }

    [Serializable]
    public sealed class BranchRoomSaveState
    {
        public string roomId = string.Empty;
        public int coordinateX;
        public int coordinateZ;
        public bool isVisited;
        public bool isCleared;
        public string rewardState = "None";
    }

    [Serializable]
    public sealed class RunRewardSaveState
    {
        public string roomId = string.Empty;
        public string rewardId = string.Empty;
        public string displayName = string.Empty;
        public string rewardKind = string.Empty;
        public int souls;
        public int coins;
        public List<RunRewardEffectSaveState> effects = new();
    }

    [Serializable]
    public sealed class RunRewardEffectSaveState
    {
        public string kind = string.Empty;
        public int intValue;
        public float floatValue;
    }

    [Serializable]
    public sealed class RoomEncounterSaveState
    {
        public string roomId = string.Empty;
        public string encounterId = string.Empty;
        public List<string> enemySpawnKinds = new();
    }

    [Serializable]
    public sealed class RunEconomySaveState
    {
        public int runSouls;
        public int runCoins;
        public List<RunRewardSaveState> collectedRewards = new();
    }

    [Serializable]
    public sealed class HubShopStateSaveState
    {
        public bool isActive;
        public int runSeed;
        public int worldIndex = 1;
        public int shopRefreshIndex;
        public bool isNextWorldPortalAvailable;
        public bool isFinalExtractionPortalAvailable;
        public List<HubShopOfferSaveState> offers = new();
        public List<NextBranchChoiceSaveState> nextChoices = new();
    }

    [Serializable]
    public sealed class HubShopOfferSaveState
    {
        public string offerId = string.Empty;
        public string displayName = string.Empty;
        public int price;
        public string priceCurrency = "Souls";
        public int healAmount;
        public bool isPurchased;
        public RunRewardSaveState reward = new();
    }

    [Serializable]
    public sealed class NextBranchChoiceSaveState
    {
        public string choiceId = string.Empty;
        public string displayName = string.Empty;
        public int seed;
        public int index;
        public int worldIndex = 1;
        public int slotIndex;
        public string kind = "Branch";
        public string state = "Open";
    }

    [Serializable]
    public sealed class PlayerRunStatsSaveState
    {
        public int maxHealthBonus;
        public float moveSpeedBonus;
        public float shotCooldownMultiplier = 1f;
        public int projectileDamageBonus;
        public int strengthBonus;
        public float maxStaminaBonus;
        public float staminaRegenBonus;
        public int defenseBonus;
        public int meleeDamageBonus;
        public int rangedDamageBonus;
    }

    [Serializable]
    public sealed class RunCurrencyWalletSaveState
    {
        public int runSouls;
        public int runCoins;
    }

    [Serializable]
    public sealed class RunEquipmentSlotsSaveState
    {
        public string meleeWeaponId = "starter_blade";
        public string rangedWeaponId = "starter_bolt";
        public string activeWeaponSlot = "Ranged";
        public string activeItemId = string.Empty;
        public int activeItemCharges;
        public string consumableCardId = string.Empty;
        public string armorId = string.Empty;
    }

    [Serializable]
    public sealed class RunInventoryStateSaveState
    {
        public List<string> passiveItemIds = new();
        public List<string> passiveCardIds = new();
    }

    [Serializable]
    public sealed class PlayerStatModifierSaveState
    {
        public string sourceId = string.Empty;
        public int maxHealth;
        public float speed;
        public int strength;
        public float maxStamina;
        public float staminaRegen;
        public int defense;
        public int meleeDamage;
        public int rangedDamage;
        public float attackCooldownMultiplier;
    }

    [Serializable]
    public sealed class PlayerRunBuildSaveState
    {
        public string selectedCharacterId = "balanced";
        public float currentStamina;
        public int baseMaxHealth = 6;
        public float baseSpeed = 4f;
        public int baseStrength = 1;
        public float baseMaxStamina = 100f;
        public float baseStaminaRegen = 18f;
        public int baseDefense;
        public int baseMeleeDamageBonus;
        public int baseRangedDamageBonus;
        public float baseAttackCooldownMultiplier = 1f;
        public RunCurrencyWalletSaveState wallet = new();
        public RunEquipmentSlotsSaveState equipment = new();
        public RunInventoryStateSaveState inventory = new();
        public List<PlayerStatModifierSaveState> modifiers = new();
    }

    [Serializable]
    public sealed class RunCompletionSummary
    {
        public int soulsToBank;
        public int rewardsClaimed;
    }
}
