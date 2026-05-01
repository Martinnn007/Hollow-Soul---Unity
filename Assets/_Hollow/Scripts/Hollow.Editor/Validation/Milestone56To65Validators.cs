using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using Hollow.Rewards;
using UnityEditor;
using UnityEngine;
using static Hollow.Editor.Validation.Milestone56To65ValidationShared;

namespace Hollow.Editor.Validation
{
    public static class Milestone56Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 56 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var report = BetaStabilizationReportBuilder.BuildArtPassCalibrationReport();
            if (report.totalRoles != Enum.GetValues(typeof(PresentationPrefabRole)).Length)
            {
                failures.Add("M56 ArtPass calibration report must cover every PresentationPrefabRole.");
            }

            if (report.records.Any(record => record == null || string.IsNullOrWhiteSpace(record.role)))
            {
                failures.Add("M56 ArtPass calibration report contains an empty role record.");
            }

            return Finish("Milestone 56", failures);
        }
    }

    public static class Milestone57Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 57 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var report = BetaStabilizationReportBuilder.BuildDeveloperInspectionCoverageReport();
            if (report.totalEntries < Enum.GetValues(typeof(PresentationPrefabRole)).Length)
            {
                failures.Add("M57 Developer Lab coverage must include every ArtPass prefab role.");
            }

            if (report.entries.All(entry => entry == null || !entry.labRoom.Contains("Developer Lab", StringComparison.Ordinal)))
            {
                failures.Add("M57 Developer Lab coverage entries must map to Developer Lab rooms.");
            }

            return Finish("Milestone 57", failures);
        }
    }

    public static class Milestone58Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 58 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.BossRewardPoolPath);
            if (standard == null)
            {
                failures.Add("M58 requires the sparse standard reward pool.");
            }

            if (treasure == null || boss == null)
            {
                failures.Add("M58 requires treasure and boss item-capable reward pools.");
            }

            if (standard != null && standard.Rewards.Any(IsBuildChangingReward))
            {
                failures.Add("M58 standard room rewards must not include build-changing items/gear/cards.");
            }

            return Finish("Milestone 58", failures);
        }

        private static bool IsBuildChangingReward(RewardDefinition reward)
        {
            return reward != null && reward.RewardKind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Weapon or RewardKind.Armor or RewardKind.Shield;
        }
    }

    public static class Milestone59Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 59 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var legacyInputHits = Directory.GetFiles("Assets/_Hollow/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("/Hollow.Editor/", StringComparison.Ordinal))
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, index) => new { path, line, index })
                    .Where(item => item.line.Contains("UnityEngine.Input", StringComparison.Ordinal) ||
                                   item.line.Contains("Input.Get", StringComparison.Ordinal)))
                .ToArray();
            foreach (var hit in legacyInputHits)
            {
                failures.Add($"M59 gameplay route still uses legacy input: {hit.path}:{hit.index + 1}");
            }

            return Finish("Milestone 59", failures);
        }
    }

    public static class Milestone60Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 60 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var bossCatalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            if (bossCatalog == null)
            {
                failures.Add("M60 requires the M53 boss catalog.");
            }
            else
            {
                if (bossCatalog.Bosses.Count != 10)
                {
                    failures.Add("M60 beta boss polish assumes exactly 10 M53 bosses.");
                }

                if (!bossCatalog.TryGetBoss("stone_warden", out _))
                {
                    failures.Add("M60 beta boss subset must include Stone Warden.");
                }
            }

            return Finish("Milestone 60", failures);
        }
    }

    public static class Milestone61Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 61 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            if (!Directory.Exists(Milestone16AssetGenerator.ApprovedRoomDirectory))
            {
                failures.Add("M61 approved runtime room directory is missing.");
            }

            if (!Directory.Exists(CuratedRoomDesignerDraftGenerator.CuratedDraftDirectory))
            {
                failures.Add("M61 curated Room Designer draft directory is missing.");
            }

            return Finish("Milestone 61", failures);
        }
    }

    public static class Milestone62Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 62 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            RequireFile("Assets/_Hollow/Scripts/Hollow.UI/Shell/PlayerBuildHudController.cs", failures);
            RequireFile("Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs", failures);
            RequireFile("Assets/_Hollow/Scripts/Hollow.UI/Shell/PickupRevealController.cs", failures);
            return Finish("Milestone 62", failures);
        }
    }

    public static class Milestone63Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 63 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var definition = AssetDatabase.LoadAssetAtPath<BetaContentLockDefinition>(Milestone63AssetGenerator.LockPath);
            if (definition == null)
            {
                failures.Add("M63 beta content lock asset is missing; run M63 generation.");
            }
            else
            {
                if (definition.CharacterIds.Length < 2)
                {
                    failures.Add("M63 beta content lock must include both starting characters.");
                }

                if (definition.BossIds.Length != 10)
                {
                    failures.Add("M63 beta content lock must include the 10-boss roster.");
                }
            }

            return Finish("Milestone 63", failures);
        }
    }

    public static class Milestone64Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 64 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            var contentLock = AssetDatabase.LoadAssetAtPath<BetaContentLockDefinition>(Milestone63AssetGenerator.LockPath);
            var qaChecklist = AssetDatabase.LoadAssetAtPath<BetaQaChecklistDefinition>(Milestone64AssetGenerator.QaChecklistPath);
            var report = BetaStabilizationReportBuilder.BuildBetaLockReport(contentLock, qaChecklist);
            if (report.checks.Length < 5)
            {
                failures.Add("M64 beta lock report must contain core beta gate checks.");
            }

            if (report.checks.Any(check => check == null || string.IsNullOrWhiteSpace(check.id)))
            {
                failures.Add("M64 beta lock report contains an empty check.");
            }

            return Finish("Milestone 64", failures);
        }
    }

    public static class Milestone65Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 65 Validation")]
        public static bool Validate()
        {
            var failures = new List<string>();
            RequireFile(Milestone65AssetGenerator.DocsPath, failures);
            RequireFile(Milestone65AssetGenerator.ReportPath, failures);
            RequireFile(Milestone65AssetGenerator.PdfPath, failures);
            return Finish("Milestone 65", failures);
        }
    }

    internal static class Milestone56To65ValidationShared
    {
        public static void RequireFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing required file: {path}");
            }
        }

        public static bool Finish(string label, List<string> failures)
        {
            if (failures.Count == 0)
            {
                Debug.Log($"{label} validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }
    }
}
