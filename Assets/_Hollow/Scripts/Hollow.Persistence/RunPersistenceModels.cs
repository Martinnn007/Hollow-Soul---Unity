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
        public long savedAtUtcTicks;
        public List<BranchRoomSaveState> rooms = new();
        public List<RunRewardSaveState> proceduralRewardPlan = new();
        public RunEconomySaveState economy = new();
        public PlayerRunStatsSaveState playerStats = new();
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
    public sealed class RunEconomySaveState
    {
        public int runSouls;
        public List<RunRewardSaveState> collectedRewards = new();
    }

    [Serializable]
    public sealed class PlayerRunStatsSaveState
    {
        public int maxHealthBonus;
        public float moveSpeedBonus;
        public float shotCooldownMultiplier = 1f;
        public int projectileDamageBonus;
    }

    [Serializable]
    public sealed class RunCompletionSummary
    {
        public int soulsToBank;
        public int rewardsClaimed;
    }
}
