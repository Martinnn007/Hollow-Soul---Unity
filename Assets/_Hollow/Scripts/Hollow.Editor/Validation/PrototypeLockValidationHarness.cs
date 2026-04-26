using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Persistence;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class PrototypeLockValidationHarness
    {
        public static ContentValidationReport ValidateAll(
            PrototypeLockChecklistDefinition checklist,
            PerformanceBudgetDefinition performanceBudget,
            BuildHandoffDefinition buildHandoff)
        {
            var report = new ContentValidationReport();
            ValidateChecklist(checklist, report);
            ValidatePerformanceBudgets(performanceBudget, report);
            ValidateBuildHandoff(buildHandoff, report);
            ValidateAddressables(report);
            ValidateSaveLoadCoverage(report);
            MergeContentValidation(report, ContentImportValidator.ValidateAll());
            return report;
        }

        private static void ValidateChecklist(PrototypeLockChecklistDefinition checklist, ContentValidationReport report)
        {
            if (checklist == null)
            {
                report.AddFailure("Prototype lock checklist asset is missing.");
                return;
            }

            if (checklist.Items.Length < 10)
            {
                report.AddFailure("Prototype lock checklist must contain QA, performance, save/load, content, and build handoff coverage.");
            }

            foreach (var item in checklist.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Group))
                {
                    report.AddFailure("Prototype lock checklist contains an item with missing id, title, or group.");
                }

                if (item.Required && item.Status != PrototypeLockStatus.Passed)
                {
                    report.AddFailure($"Required prototype lock checklist item is not passed: {item.Id}");
                }

                if (item.Status == PrototypeLockStatus.Deferred && string.IsNullOrWhiteSpace(item.Notes))
                {
                    report.AddFailure($"Deferred prototype lock checklist item needs notes: {item.Id}");
                }
            }

            if (!checklist.RequiredItemsSatisfied)
            {
                report.AddFailure("Prototype lock checklist has required items that are not satisfied.");
            }
        }

        private static void ValidatePerformanceBudgets(PerformanceBudgetDefinition performanceBudget, ContentValidationReport report)
        {
            if (performanceBudget == null)
            {
                report.AddFailure("Performance budget asset is missing.");
                return;
            }

            ValidatePerformanceBudget(performanceBudget, PlatformPresentationMode.WindowsStandard3D, Milestone10AssetGenerator.WindowsProfilePath, report);
            ValidatePerformanceBudget(performanceBudget, PlatformPresentationMode.VisionOSBoundedTabletop, Milestone10AssetGenerator.BoundedProfilePath, report);
            ValidatePerformanceBudget(performanceBudget, PlatformPresentationMode.VisionOSImmersive, Milestone10AssetGenerator.ImmersiveProfilePath, report);
        }

        private static void ValidatePerformanceBudget(
            PerformanceBudgetDefinition performanceBudget,
            PlatformPresentationMode mode,
            string profilePath,
            ContentValidationReport report)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(profilePath);
            if (profile == null)
            {
                report.AddFailure($"Missing M10 platform polish profile for M11 budget validation: {profilePath}");
                return;
            }

            if (!performanceBudget.TryGetBudget(mode, out var budget))
            {
                report.AddFailure($"Performance budget is missing platform mode {mode}.");
                return;
            }

            if (profile.TargetFrameRate < budget.MinimumTargetFrameRate)
            {
                report.AddFailure($"{mode} target frame rate {profile.TargetFrameRate} is below M11 minimum {budget.MinimumTargetFrameRate}.");
            }

            if (profile.RenderScale > budget.MaximumRenderScale + 0.0001f)
            {
                report.AddFailure($"{mode} render scale {profile.RenderScale} exceeds M11 maximum {budget.MaximumRenderScale}.");
            }

            var requiredFrameTime = 1000f / Mathf.Max(1, budget.MinimumTargetFrameRate);
            if (budget.MaximumFrameTimeMs + 0.05f < requiredFrameTime)
            {
                report.AddFailure($"{mode} frame-time budget is impossible for {budget.MinimumTargetFrameRate} FPS.");
            }

            if (budget.MaximumVisibleEnemies <= 0 || budget.MaximumProjectiles <= 0 || budget.MaximumDrawCalls <= 0)
            {
                report.AddFailure($"{mode} performance budget must include positive entity and draw-call budgets.");
            }
        }

        private static void ValidateBuildHandoff(BuildHandoffDefinition buildHandoff, ContentValidationReport report)
        {
            if (buildHandoff == null)
            {
                report.AddFailure("Build handoff asset is missing.");
                return;
            }

            if (buildHandoff.LastVerifiedMilestone != "M11" || string.IsNullOrWhiteSpace(buildHandoff.PrototypeVersion))
            {
                report.AddFailure("Build handoff must identify M11 as the last verified milestone.");
            }

            if (buildHandoff.ValidationCommands.Length < 3 || buildHandoff.HandoffNotes.Length < 3)
            {
                report.AddFailure("Build handoff must include validation commands and practical handoff notes.");
            }

            var enabledBuildScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToHashSet();

            foreach (var scenePath in Milestone11AssetGenerator.RequiredBuildScenes)
            {
                if (!File.Exists(scenePath))
                {
                    report.AddFailure($"M11 required scene is missing: {scenePath}");
                }

                if (!buildHandoff.RequiredScenes.Contains(scenePath))
                {
                    report.AddFailure($"Build handoff is missing required scene: {scenePath}");
                }

                if (!enabledBuildScenes.Contains(scenePath))
                {
                    report.AddFailure($"Build settings are missing enabled scene: {scenePath}");
                }
            }
        }

        private static void ValidateAddressables(ContentValidationReport report)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                report.AddFailure("M11 requires Addressables settings.");
                return;
            }

            if (!settings.GetLabels().Contains(Milestone11AssetGenerator.PrototypeLockAddressableLabel))
            {
                report.AddFailure($"Missing Addressables label {Milestone11AssetGenerator.PrototypeLockAddressableLabel}.");
            }

            AssertAddressable(settings, Milestone11AssetGenerator.ChecklistPath, report);
            AssertAddressable(settings, Milestone11AssetGenerator.PerformanceBudgetPath, report);
            AssertAddressable(settings, Milestone11AssetGenerator.BuildHandoffPath, report);
        }

        private static void AssertAddressable(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings, string path, ContentValidationReport report)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = string.IsNullOrWhiteSpace(guid) ? null : settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null)
            {
                report.AddFailure($"Prototype lock asset is not addressable: {path}");
                return;
            }

            if (!entry.labels.Contains(Milestone11AssetGenerator.PrototypeLockAddressableLabel) || !entry.labels.Contains("hollow.data"))
            {
                report.AddFailure($"Prototype lock asset is missing required Addressables labels: {path}");
            }
        }

        private static void ValidateSaveLoadCoverage(ContentValidationReport report)
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m11_validator", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                var profile = store.CreateOrLoadProfile(slotId, "Prototype Lock");
                var started = store.MarkRunStarted(slotId);
                if (started.TotalRuns != 1)
                {
                    report.AddFailure("Save/load coverage failed: New Run did not increment total runs.");
                }

                store.SaveActiveRun(slotId, CreateCoverageSnapshot("east", 5));
                if (!store.TryLoadActiveRun(slotId, out var loaded) || loaded.currentRoomId != "east" || loaded.economy.runSouls != 40 || loaded.rooms.Count < 2)
                {
                    report.AddFailure("Save/load coverage failed: active run checkpoint did not round-trip.");
                }

                store.CompleteActiveRun(slotId, new RunCompletionSummary { soulsToBank = 40, rewardsClaimed = 4 });
                var completed = store.LoadSlotSummaries()[profile.SlotIndex];
                if (completed.HasActiveRun || completed.BankedSouls != 40 || completed.CompletedRuns != 1)
                {
                    report.AddFailure("Save/load coverage failed: completion did not bank souls and clear active run.");
                }

                store.MarkRunStarted(slotId);
                store.SaveActiveRun(slotId, CreateCoverageSnapshot("south", 1));
                store.ClearActiveRun(slotId);
                var afterDeath = store.LoadSlotSummaries()[profile.SlotIndex];
                if (afterDeath.HasActiveRun || afterDeath.BankedSouls != 40 || afterDeath.CompletedRuns != 1)
                {
                    report.AddFailure("Save/load coverage failed: death clear mutated banked meta progression.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        private static RunSaveSnapshot CreateCoverageSnapshot(string currentRoomId, int health)
        {
            return new RunSaveSnapshot
            {
                runId = "m11-prototype-lock",
                branchId = "m7_five_room_cross",
                currentRoomId = currentRoomId,
                platformKind = PlatformPresentationMode.WindowsStandard3D.ToString(),
                playerCurrentHealth = health,
                rooms = new()
                {
                    new BranchRoomSaveState { roomId = "origin", coordinateX = 0, coordinateZ = 0, isVisited = true, isCleared = true, rewardState = RoomRewardState.Unavailable.ToString() },
                    new BranchRoomSaveState { roomId = currentRoomId, coordinateX = 1, coordinateZ = 0, isVisited = true, isCleared = true, rewardState = RoomRewardState.Claimed.ToString() }
                },
                economy = new RunEconomySaveState
                {
                    runSouls = 40,
                    collectedRewards = new()
                    {
                        new RunRewardSaveState { roomId = currentRoomId, rewardId = "ember_charm", displayName = "Ember Charm", rewardKind = RewardKind.PassiveItem.ToString(), souls = 10 }
                    }
                },
                playerStats = new PlayerRunStatsSaveState
                {
                    maxHealthBonus = 1,
                    moveSpeedBonus = 0.5f,
                    shotCooldownMultiplier = 0.9f,
                    projectileDamageBonus = 1
                }
            };
        }

        private static void MergeContentValidation(ContentValidationReport target, ContentValidationReport source)
        {
            foreach (var failure in source.Failures)
            {
                target.AddFailure($"Content validation: {failure}");
            }

            foreach (var warning in source.Warnings)
            {
                target.AddWarning($"Content validation: {warning}");
            }
        }
    }
}
