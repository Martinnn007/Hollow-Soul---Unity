using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class VerticalSliceContentValidator
    {
        private const string SampleRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        public static VerticalSliceLockReport ValidateLock(VerticalSliceLockDefinition definition)
        {
            var report = CreateReport(definition);
            if (definition == null)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("lock-definition", "M25 vertical slice lock definition is missing.", "Run Hollow/Generation/Generate Milestone 25 Assets."));
                report.Recalculate();
                return report;
            }

            ValidateDefinition(definition, report);
            ValidateBranch(definition, report);
            ValidatePresentation(definition, report);
            ValidatePlatformChecklist(definition, report);
            ValidatePriorMilestones(definition, report);
            report.manualChecklist.AddRange(ManualChecklist());
            report.Recalculate();
            return report;
        }

        private static VerticalSliceLockReport CreateReport(VerticalSliceLockDefinition definition)
        {
            return new VerticalSliceLockReport
            {
                reportId = $"vertical-slice-lock-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                lockName = definition != null ? definition.LockName : "M25 Vertical Slice Content Lock",
                branchIdentity = definition != null ? definition.BranchIdentity : string.Empty,
                lockedSeed = definition != null ? definition.LockedSeed : 0,
                unityVersion = Application.unityVersion,
                gitBranch = BuildManifestWriter.ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = BuildManifestWriter.ReadGitValue("rev-parse --short HEAD"),
                reportRoot = definition != null ? definition.ReportRoot : "output/reports",
                pdfOutputPath = definition != null ? definition.PdfOutputPath : "output/pdf/Hollow_M25_Vertical_Slice_Content_Lock.pdf"
            };
        }

        private static void ValidateDefinition(VerticalSliceLockDefinition definition, VerticalSliceLockReport report)
        {
            var failures = new List<string>();
            if (definition.BranchIdentity != BranchGenerator.BranchFeaturesId)
            {
                failures.Add($"Expected branch identity {BranchGenerator.BranchFeaturesId}, got {definition.BranchIdentity}.");
            }

            if (definition.LockedSeed != BranchGenerator.DefaultSeededMacroSeed)
            {
                failures.Add($"Expected locked seed {BranchGenerator.DefaultSeededMacroSeed}, got {definition.LockedSeed}.");
            }

            if (definition.BranchGenerationSettings == null)
            {
                failures.Add("Missing BranchGenerationSettingsDefinition reference.");
            }

            if (definition.RoomTemplateCatalog == null)
            {
                failures.Add("Missing BranchRoomTemplateCatalogDefinition reference.");
            }

            if (definition.PresentationCatalog == null)
            {
                failures.Add("Missing PresentationContentCatalog reference.");
            }

            if (definition.PlatformQaProfile == null)
            {
                failures.Add("Missing PlatformBuildQaProfileDefinition reference.");
            }

            if (definition.RequiredShopOfferCount != 3 || definition.RequiredNextBranchPortalCount != 3)
            {
                failures.Add("M25 lock must require three shop offers and three next-branch portals.");
            }

            if (failures.Count == 0)
            {
                report.checks.Add(VerticalSliceCheckResult.Passed("lock-definition", "M25 lock asset pins branch identity, seed, catalogs, platform QA, and slice counts."));
            }
            else
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("lock-definition", string.Join("; ", failures), "Regenerate the M25 lock asset."));
            }
        }

        private static void ValidateBranch(VerticalSliceLockDefinition definition, VerticalSliceLockReport report)
        {
            if (definition.RoomTemplateCatalog == null || definition.BranchGenerationSettings == null)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", "Cannot validate branch content without room catalog and generation settings."));
                return;
            }

            var sampleAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(SampleRoomPath);
            var sampleError = "sample room asset missing";
            if (sampleAsset == null || !HollowRuntimeV2Importer.TryImport(sampleAsset.text, out var sampleRoom, out sampleError))
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", $"Sample room import failed: {sampleError}", "Regenerate M3-M16 room assets."));
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, definition.RoomTemplateCatalog, definition.LockedSeed, out var contentError);
            report.fixtureRoomCount = content.FixtureRoomPool.Count;
            report.approvedRoomCount = content.ApprovedRoomPool.Count;
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", contentError, "Fix invalid branch room templates or duplicate approved room IDs."));
                return;
            }

            if (!content.HasMacroFixturePool)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", "M25 requires all five M13 macro fixture room templates.", "Regenerate M13-M16 assets."));
                return;
            }

            if (!definition.AllowEmptyApprovedRoomPool && content.ApprovedRoomPool.Count == 0)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", "Approved room pool is empty but the lock does not allow it."));
                return;
            }

            try
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, definition.BranchGenerationSettings, definition.LockedSeed);
                report.roomCount = graph.RoomCount;
                report.connectionCount = graph.Connections.Count;
                var featurePlan = BranchFeaturePlan.Create(graph);
                var branchFailures = ValidateGraph(definition, graph, featurePlan);
                if (branchFailures.Count == 0)
                {
                    report.checks.Add(VerticalSliceCheckResult.Passed(
                        "branch-content",
                        $"Locked branch generated with {graph.RoomCount} rooms, {graph.Connections.Count} directional connections, {content.FixtureRoomPool.Count} fixtures, and {content.ApprovedRoomPool.Count} approved rooms.",
                        $"Boss key source: {featurePlan.BossKeyRoomId}; secret: {featurePlan.SecretRoomId}; boss: {featurePlan.BossRoomId}."));
                }
                else
                {
                    report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", string.Join("; ", branchFailures), "Adjust the locked seed or branch content catalog."));
                }
            }
            catch (Exception exception)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("branch-content", exception.Message, "Regenerate branch room templates and validate M20 branch generation."));
            }

            var hub = InterBranchHubState.Create(definition.LockedSeed, 0, null);
            report.shopOfferCount = hub.ShopOffers.Count;
            report.nextBranchPortalCount = hub.NextBranchChoices.Count;
            if (hub.ShopOffers.Count == definition.RequiredShopOfferCount &&
                hub.NextBranchChoices.Count == definition.RequiredNextBranchPortalCount)
            {
                report.checks.Add(VerticalSliceCheckResult.Passed("hub-shop-portals", "Inter-branch hub exposes three shop offers and three seeded next-branch portal choices."));
            }
            else
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("hub-shop-portals", $"Expected {definition.RequiredShopOfferCount} shop offers and {definition.RequiredNextBranchPortalCount} next portals, got {hub.ShopOffers.Count} and {hub.NextBranchChoices.Count}."));
            }
        }

        private static List<string> ValidateGraph(VerticalSliceLockDefinition definition, BranchFloorGraph graph, BranchFeaturePlan featurePlan)
        {
            var failures = new List<string>();
            if (graph.BranchId != definition.BranchIdentity)
            {
                failures.Add($"Graph branch ID {graph.BranchId} does not match lock {definition.BranchIdentity}.");
            }

            if (graph.Seed != definition.LockedSeed)
            {
                failures.Add($"Graph seed {graph.Seed} does not match lock seed {definition.LockedSeed}.");
            }

            foreach (var role in definition.RequiredRoomRoles)
            {
                if (!Enum.TryParse<BranchRoomRole>(role, out var parsedRole) || !graph.Rooms.Any(room => room.Role == parsedRole))
                {
                    failures.Add($"Locked branch is missing required room role {role}.");
                }
            }

            if (!featurePlan.HasBossKeyRoom)
            {
                failures.Add("Feature plan did not select a boss-key source room.");
            }

            if (string.IsNullOrWhiteSpace(featurePlan.SecretRoomId))
            {
                failures.Add("Feature plan did not select a visible secret room.");
            }

            if (string.IsNullOrWhiteSpace(featurePlan.BossRoomId))
            {
                failures.Add("Feature plan did not select a boss room.");
            }

            if (!graph.Connections.Any(connection => connection.LockKind == BranchConnectionLockKind.BossKey))
            {
                failures.Add("No boss-key locked connection found.");
            }

            if (graph.Connections.Any(connection => !connection.HasExplicitPorts))
            {
                failures.Add("Vertical slice branch must use explicit port-to-port traversal on every connection.");
            }

            var expectedOccupiedCells = graph.Rooms.Sum(room => room.Footprint?.OccupiedCellCount ?? 0);
            if (expectedOccupiedCells == 0 || graph.OccupancyMap.OwnerByCell.Count != expectedOccupiedCells)
            {
                failures.Add("Branch occupancy map does not match placed macro-room footprints.");
            }

            return failures;
        }

        private static void ValidatePresentation(VerticalSliceLockDefinition definition, VerticalSliceLockReport report)
        {
            var catalog = definition.PresentationCatalog;
            if (catalog == null)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("artpass-lock", "Presentation catalog is missing."));
                return;
            }

            var failures = new List<string>();
            foreach (var role in definition.RequiredPrefabRoles)
            {
                if (!catalog.TryGetPrefab(role, out var prefab) || prefab == null)
                {
                    failures.Add($"Missing ArtPass prefab binding for {role}.");
                    continue;
                }

                var marker = prefab.GetComponent<PresentationVisualMarker>();
                var path = AssetDatabase.GetAssetPath(prefab);
                if (marker == null || marker.Role != role || marker.IsFallback)
                {
                    failures.Add($"Prefab binding for {role} must have non-fallback PresentationVisualMarker.");
                }

                if (definition.RequireArtPassPrefabs && !path.StartsWith(Milestone23AssetGenerator.ArtPassRoot, StringComparison.Ordinal))
                {
                    failures.Add($"Prefab binding for {role} must point under {Milestone23AssetGenerator.ArtPassRoot}.");
                }
            }

            foreach (var cue in definition.RequiredVfxCues)
            {
                if (!catalog.TryGetVfxCue(cue, out var cueDefinition) || cueDefinition == null || cueDefinition.Prefab == null)
                {
                    failures.Add($"Missing required VFX cue prefab for {cue}.");
                }
            }

            foreach (var cue in definition.RequiredAudioCues)
            {
                if (!catalog.TryGetAudioCue(cue, out var cueDefinition) || cueDefinition == null || cueDefinition.Clip == null)
                {
                    failures.Add($"Missing required audio cue clip for {cue}.");
                }
            }

            var artPassReport = ArtPassContentValidator.ValidateAll();
            failures.AddRange(artPassReport.Failures);
            if (failures.Count == 0)
            {
                report.checks.Add(VerticalSliceCheckResult.Passed("artpass-lock", $"Required ArtPass roles/cues resolved without prototype fallback. Warnings: {artPassReport.Warnings.Count}."));
            }
            else
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("artpass-lock", string.Join("; ", failures), "Regenerate M23 ArtPass assets and repair catalog bindings."));
            }
        }

        private static void ValidatePlatformChecklist(VerticalSliceLockDefinition definition, VerticalSliceLockReport report)
        {
            var failures = new List<string>();
            if (!definition.RequireEqualPlatformChecklist)
            {
                failures.Add("M25 requires equal checklist coverage for Windows, Vision Pro bounded, and Vision Pro immersive.");
            }

            foreach (var target in new[] { "Windows", "VisionOSBounded", "VisionOSImmersive" })
            {
                if (!definition.PlatformChecklistTargets.Contains(target))
                {
                    failures.Add($"Missing platform checklist target {target}.");
                }
            }

            var profile = definition.PlatformQaProfile;
            if (profile == null)
            {
                failures.Add("Platform QA profile reference is missing.");
            }
            else
            {
                foreach (var scene in Milestone11AssetGenerator.RequiredBuildScenes)
                {
                    if (!profile.RequiredScenes.Contains(scene))
                    {
                        failures.Add($"Platform QA profile is missing required scene {scene}.");
                    }
                }
            }

            if (failures.Count == 0)
            {
                report.checks.Add(VerticalSliceCheckResult.Passed("platform-checklist", "Windows, Vision Pro bounded, and Vision Pro immersive have equal vertical-slice checklist coverage."));
            }
            else
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("platform-checklist", string.Join("; ", failures), "Regenerate M24/M25 QA profiles."));
            }
        }

        private static void ValidatePriorMilestones(VerticalSliceLockDefinition definition, VerticalSliceLockReport report)
        {
            if (definition.PlatformQaProfile == null)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("m0-m24-audit", "Platform QA profile is missing; cannot run prior milestone audit."));
                return;
            }

            var validationTypes = Milestone24AssetGenerator.ValidationTypes
                .Concat(new[] { "Hollow.Editor.Validation.Milestone24Validator" })
                .Distinct()
                .ToArray();
            var auditProfile = ScriptableObject.CreateInstance<BuildAutomationProfileDefinition>();
            try
            {
                auditProfile.Configure(
                    definition.LockName,
                    definition.PlatformQaProfile.BuildRoot,
                    definition.ReportRoot,
                    definition.PlatformQaProfile.WindowsBuildFolderName,
                    definition.PlatformQaProfile.WindowsExecutableName,
                    "latest_m25_dependency_audit.json",
                    "latest_m25_dependency_audit.md",
                    "latest_m25_dependency_manifest.json",
                    nextRequirePassingAuditBeforeBuild: true,
                    definition.PlatformQaProfile.RequiredScenes,
                    validationTypes);
                var audit = PrototypeAuditRunner.RunFullAudit(auditProfile, writeReports: false);
                report.checks.Add(audit.Passed
                    ? VerticalSliceCheckResult.Passed("m0-m24-audit", $"M0-M24 validators passed: {audit.passedChecks}/{audit.totalChecks}.")
                    : VerticalSliceCheckResult.Failed("m0-m24-audit", $"M0-M24 validators failed: {audit.failedChecks}/{audit.totalChecks}.", string.Join("; ", audit.entries.Where(entry => !entry.passed).Select(entry => entry.id))));
            }
            catch (Exception exception)
            {
                report.checks.Add(VerticalSliceCheckResult.Failed("m0-m24-audit", exception.Message, "Run M24 validation directly and repair reported failures."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(auditProfile);
            }
        }

        private static IEnumerable<string> ManualChecklist()
        {
            return new[]
            {
                "Windows: start New Run with the locked seed, clear combat rooms, collect rewards, unlock the boss door, defeat boss, enter hub, buy one shop card, and inspect all three next-branch portals.",
                "Windows: quit after a checkpoint and Continue to confirm room/reward/key/shop/hub state restores.",
                "Vision Pro bounded: repeat route smoke with tabletop scale 0.1, HUD/minimap unscaled, readable door/shop/portal cards, and no ArtPass visual collider takeover.",
                "Vision Pro immersive: repeat route smoke at full world scale, verify comfort posture/readability, boss/projectile clarity, and next-branch portal placement.",
                "All platforms: confirm transient designer/sample sessions remain excluded from run saves and profile mutation."
            };
        }
    }
}
