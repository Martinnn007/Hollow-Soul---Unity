using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    public enum ArtPassPrefabSafetyStatus
    {
        Ready,
        NeedsScaleFix,
        MissingRenderer,
        MissingMaterial,
        MissingBinding,
        UnsafePrefab
    }

    public enum ArtPassProductionReadinessStatus
    {
        Ready,
        PrototypeFallback,
        NeedsAssetWork,
        MissingBinding,
        UnsafePrefab
    }

    public enum ArtPassBindingStatus
    {
        Bound,
        Missing,
        PrototypeFallback,
        Unsafe
    }

    public enum InspectionSpawnMode
    {
        StaticDisplay,
        FrozenRuntime,
        LiveRuntime
    }

    [Serializable]
    public sealed class ArtPassPrefabCalibrationRecord
    {
        public string role = string.Empty;
        public string displayName = string.Empty;
        public string group = string.Empty;
        public string prefabPath = string.Empty;
        public ArtPassPrefabSafetyStatus safetyStatus = ArtPassPrefabSafetyStatus.MissingBinding;
        public ArtPassProductionReadinessStatus readinessStatus = ArtPassProductionReadinessStatus.MissingBinding;
        public ArtPassBindingStatus bindingStatus = ArtPassBindingStatus.Missing;
        public Vector3 rootScale = Vector3.one;
        public Vector3 rendererBoundsSize = Vector3.zero;
        public Vector3 rendererLocalCenter = Vector3.zero;
        public float rendererBottomLocalY;
        public bool hasRenderer;
        public bool hasRootMarker;
        public int materialCount;
        public string[] warnings = Array.Empty<string>();
        public string[] errors = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ArtPassPrefabCalibrationReport
    {
        public string generatedAtUtc = string.Empty;
        public string catalogPath = string.Empty;
        public string artPassRoot = string.Empty;
        public int totalRoles;
        public int readyCount;
        public int needsScaleFixCount;
        public int missingRendererCount;
        public int missingMaterialCount;
        public int missingBindingCount;
        public int unsafePrefabCount;
        public ArtPassPrefabCalibrationRecord[] records = Array.Empty<ArtPassPrefabCalibrationRecord>();

        public bool HasBlockingSafetyFailures => missingBindingCount > 0 || unsafePrefabCount > 0;

        public void Recalculate()
        {
            records ??= Array.Empty<ArtPassPrefabCalibrationRecord>();
            totalRoles = records.Length;
            readyCount = Count(ArtPassPrefabSafetyStatus.Ready);
            needsScaleFixCount = Count(ArtPassPrefabSafetyStatus.NeedsScaleFix);
            missingRendererCount = Count(ArtPassPrefabSafetyStatus.MissingRenderer);
            missingMaterialCount = Count(ArtPassPrefabSafetyStatus.MissingMaterial);
            missingBindingCount = Count(ArtPassPrefabSafetyStatus.MissingBinding);
            unsafePrefabCount = Count(ArtPassPrefabSafetyStatus.UnsafePrefab);
        }

        private int Count(ArtPassPrefabSafetyStatus status)
        {
            var count = 0;
            foreach (var record in records)
            {
                if (record != null && record.safetyStatus == status)
                {
                    count++;
                }
            }

            return count;
        }
    }

    [Serializable]
    public sealed class DeveloperInspectionEntry
    {
        public string group = string.Empty;
        public string entityId = string.Empty;
        public string displayName = string.Empty;
        public string prefabRole = string.Empty;
        public string labRoom = string.Empty;
        public ArtPassBindingStatus bindingStatus = ArtPassBindingStatus.Missing;
        public InspectionSpawnMode spawnMode = InspectionSpawnMode.StaticDisplay;
        public string notes = string.Empty;
    }

    [Serializable]
    public sealed class DeveloperInspectionCoverageReport
    {
        public string generatedAtUtc = string.Empty;
        public int totalEntries;
        public int boundEntries;
        public int missingEntries;
        public DeveloperInspectionEntry[] entries = Array.Empty<DeveloperInspectionEntry>();

        public void Recalculate()
        {
            entries ??= Array.Empty<DeveloperInspectionEntry>();
            totalEntries = entries.Length;
            boundEntries = 0;
            missingEntries = 0;
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.bindingStatus == ArtPassBindingStatus.Missing)
                {
                    missingEntries++;
                }
                else
                {
                    boundEntries++;
                }
            }
        }
    }

    [CreateAssetMenu(menuName = "Hollow/Beta/Beta Content Lock", fileName = "BetaContentLock")]
    public sealed class BetaContentLockDefinition : ScriptableObject
    {
        [SerializeField] private string lockId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string[] characterIds = Array.Empty<string>();
        [SerializeField] private string[] weaponIds = Array.Empty<string>();
        [SerializeField] private string[] rewardPoolIds = Array.Empty<string>();
        [SerializeField] private string[] roomPoolIds = Array.Empty<string>();
        [SerializeField] private string[] bossIds = Array.Empty<string>();
        [SerializeField] private string[] challengeIds = Array.Empty<string>();
        [SerializeField] private string[] allowedPrototypeNotes = Array.Empty<string>();

        public string LockId => lockId;

        public string DisplayName => displayName;

        public string[] CharacterIds => characterIds;

        public string[] WeaponIds => weaponIds;

        public string[] RewardPoolIds => rewardPoolIds;

        public string[] RoomPoolIds => roomPoolIds;

        public string[] BossIds => bossIds;

        public string[] ChallengeIds => challengeIds;

        public string[] AllowedPrototypeNotes => allowedPrototypeNotes;

        public void Configure(
            string nextLockId,
            string nextDisplayName,
            string[] nextCharacterIds,
            string[] nextWeaponIds,
            string[] nextRewardPoolIds,
            string[] nextRoomPoolIds,
            string[] nextBossIds,
            string[] nextChallengeIds,
            string[] nextAllowedPrototypeNotes)
        {
            lockId = nextLockId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            characterIds = nextCharacterIds ?? Array.Empty<string>();
            weaponIds = nextWeaponIds ?? Array.Empty<string>();
            rewardPoolIds = nextRewardPoolIds ?? Array.Empty<string>();
            roomPoolIds = nextRoomPoolIds ?? Array.Empty<string>();
            bossIds = nextBossIds ?? Array.Empty<string>();
            challengeIds = nextChallengeIds ?? Array.Empty<string>();
            allowedPrototypeNotes = nextAllowedPrototypeNotes ?? Array.Empty<string>();
        }
    }

    [CreateAssetMenu(menuName = "Hollow/Beta/Beta QA Checklist", fileName = "BetaQaChecklist")]
    public sealed class BetaQaChecklistDefinition : ScriptableObject
    {
        [SerializeField] private string checklistId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string[] smokeRoutes = Array.Empty<string>();
        [SerializeField] private string[] platformRoutes = Array.Empty<string>();
        [SerializeField] private string[] manualChecks = Array.Empty<string>();

        public string ChecklistId => checklistId;

        public string DisplayName => displayName;

        public string[] SmokeRoutes => smokeRoutes;

        public string[] PlatformRoutes => platformRoutes;

        public string[] ManualChecks => manualChecks;

        public void Configure(
            string nextChecklistId,
            string nextDisplayName,
            string[] nextSmokeRoutes,
            string[] nextPlatformRoutes,
            string[] nextManualChecks)
        {
            checklistId = nextChecklistId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            smokeRoutes = nextSmokeRoutes ?? Array.Empty<string>();
            platformRoutes = nextPlatformRoutes ?? Array.Empty<string>();
            manualChecks = nextManualChecks ?? Array.Empty<string>();
        }
    }

    [Serializable]
    public sealed class BetaLockReport
    {
        public string generatedAtUtc = string.Empty;
        public string lockId = string.Empty;
        public bool readyForBeta;
        public BetaLockCheckResult[] checks = Array.Empty<BetaLockCheckResult>();

        public void Recalculate()
        {
            checks ??= Array.Empty<BetaLockCheckResult>();
            readyForBeta = true;
            foreach (var check in checks)
            {
                if (check != null && !check.passed)
                {
                    readyForBeta = false;
                    break;
                }
            }
        }
    }

    [Serializable]
    public sealed class BetaLockCheckResult
    {
        public string id = string.Empty;
        public string status = string.Empty;
        public string details = string.Empty;
        public string remediation = string.Empty;
        public bool passed;

        public static BetaLockCheckResult Passed(string id, string details)
        {
            return new BetaLockCheckResult
            {
                id = id ?? string.Empty,
                status = "Passed",
                details = details ?? string.Empty,
                remediation = string.Empty,
                passed = true
            };
        }

        public static BetaLockCheckResult Failed(string id, string details, string remediation)
        {
            return new BetaLockCheckResult
            {
                id = id ?? string.Empty,
                status = "Failed",
                details = details ?? string.Empty,
                remediation = remediation ?? string.Empty,
                passed = false
            };
        }

        public static BetaLockCheckResult BlockedByEnvironment(string id, string details, string remediation)
        {
            return new BetaLockCheckResult
            {
                id = id ?? string.Empty,
                status = "BlockedByEnvironment",
                details = details ?? string.Empty,
                remediation = remediation ?? string.Empty,
                passed = true
            };
        }
    }
}
