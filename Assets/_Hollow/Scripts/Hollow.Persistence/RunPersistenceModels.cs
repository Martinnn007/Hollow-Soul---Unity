using System;
using System.Collections.Generic;

namespace Hollow.Persistence
{
    [Serializable]
    public sealed class RunSaveSnapshot
    {
        public string runId = string.Empty;
        public string challengeId = string.Empty;
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
        public string activeBiomeId = "hollow_threshold";
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
        public List<DroppedReplacementPickupSaveState> droppedReplacementPickups = new();
        public List<RunRoomHazardStateSave> roomHazardStates = new();
        public List<RunChestStateSave> roomChestStates = new();
        public List<RunCoinPickupSaveState> looseCoinPickups = new();
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
    public sealed class DroppedReplacementPickupSaveState
    {
        public string pickupId = string.Empty;
        public string roomId = string.Empty;
        public string rewardKind = string.Empty;
        public string rewardId = string.Empty;
        public string displayName = string.Empty;
        public int activeItemCharges;
        public float localX;
        public float localY;
        public float localZ;
    }

    [Serializable]
    public sealed class RunRoomHazardStateSave
    {
        public string roomId = string.Empty;
        public string objectId = string.Empty;
        public string objectKind = string.Empty;
        public bool isDestroyed;
        public int coinDropAmount;
        public bool coinCollected;
        public float localX;
        public float localY;
        public float localZ;
    }

    [Serializable]
    public sealed class RunChestStateSave
    {
        public string roomId = string.Empty;
        public string chestId = string.Empty;
        public string kind = "Normal";
        public string state = "Unopened";
        public bool contentsClaimed;
        public string contentRewardId = string.Empty;
        public string contentDisplayName = string.Empty;
        public string contentRewardKind = string.Empty;
        public int contentSouls;
        public int contentCoins;
        public List<RunRewardEffectSaveState> contentEffects = new();
        public float localX;
        public float localY;
        public float localZ;
    }

    [Serializable]
    public sealed class RunCoinPickupSaveState
    {
        public string roomId = string.Empty;
        public string pickupId = string.Empty;
        public string denomination = "Copper";
        public int value = 1;
        public bool isCollected;
        public float localX;
        public float localY;
        public float localZ;
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
        public int maxStacks = 1;
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
        public List<int> enemyIntelligenceLevels = new();
        public List<string> enemyDispositions = new();
        public int worldIndex;
        public int difficultyBand;
        public int directorPressure;
        public string bossId = string.Empty;
        public string bossArenaId = string.Empty;
        public int bossWorldBand;
        public string bossPhaseState = string.Empty;
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
        public float meleeRangeBonusMeters;
        public float rangedRangeBonusMeters;
        public int stabilityBonus;
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
        public string rangedWeaponId = "starter_pistol";
        public string activeWeaponSlot = "Melee";
        public string activeItemId = string.Empty;
        public int activeItemCharges;
        public string consumableCardId = string.Empty;
        public string armorId = string.Empty;
        public string shieldId = "starter_buckler";
    }

    [Serializable]
    public sealed class RunInventoryStateSaveState
    {
        public List<string> passiveItemIds = new();
        public List<PassiveItemStackSaveState> passiveItemStacks = new();
        public List<string> passiveCardIds = new();
    }

    [Serializable]
    public sealed class PassiveItemStackSaveState
    {
        public string itemId = string.Empty;
        public int count = 1;
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
        public int stability;
        public float attackCooldownMultiplier;
        public float meleeRangeBonusMeters;
        public float rangedRangeBonusMeters;
    }

    [Serializable]
    public sealed class PlayerRunBuildSaveState
    {
        public string selectedCharacterId = "balanced";
        public float currentStamina;
        public int baseMaxHealth = 3;
        public float baseSpeed = 4f;
        public int baseStrength = 1;
        public float baseMaxStamina = 100f;
        public float baseStaminaRegen = 11f;
        public int baseDefense;
        public int baseMeleeDamageBonus;
        public int baseRangedDamageBonus;
        public int baseStability = 1;
        public float baseAttackCooldownMultiplier = 1f;
        public float baseMeleeRangeBonusMeters;
        public float baseRangedRangeBonusMeters;
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
