using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone56AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone56ArtPassWrapperCalibrationAssetIntakeQA.md";
        public const string ReportJsonPath = "output/reports/m56_artpass_prefab_calibration.json";
        public const string ReportMarkdownPath = "output/reports/m56_artpass_prefab_calibration.md";
        public const string PdfPath = "output/pdf/Hollow_M56_ArtPass_Wrapper_Calibration_Asset_Intake_QA.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 56 Assets")]
        public static void Generate()
        {
            Milestone55AssetGenerator.Generate();
            var report = BetaStabilizationReportBuilder.BuildArtPassCalibrationReport();
            BetaStabilizationMilestoneWriter.WriteArtPassCalibration(DocsPath, ReportJsonPath, ReportMarkdownPath, PdfPath, report);
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 56 ArtPass calibration report for {report.totalRoles} roles.");
        }
    }

    public static class Milestone57AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone57DeveloperLabCoverageLock.md";
        public const string ReportJsonPath = "output/reports/m57_developer_lab_coverage.json";
        public const string ReportMarkdownPath = "output/reports/m57_developer_lab_coverage.md";
        public const string PdfPath = "output/pdf/Hollow_M57_Developer_Lab_Coverage_Lock.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 57 Assets")]
        public static void Generate()
        {
            Milestone56AssetGenerator.Generate();
            var calibration = BetaStabilizationReportBuilder.BuildArtPassCalibrationReport();
            var report = BetaStabilizationReportBuilder.BuildDeveloperInspectionCoverageReport(calibration);
            BetaStabilizationMilestoneWriter.WriteDeveloperCoverage(DocsPath, ReportJsonPath, ReportMarkdownPath, PdfPath, report);
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 57 Developer Lab coverage report with {report.totalEntries} entries.");
        }
    }

    public static class Milestone58AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone58BetaRewardEconomyChestBalance.md";
        public const string ReportPath = "output/reports/m58_beta_reward_economy_chest_balance.md";
        public const string PdfPath = "output/pdf/Hollow_M58_Beta_Reward_Economy_Chest_Balance.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 58 Assets")]
        public static void Generate()
        {
            Milestone57AssetGenerator.Generate();
            var lines = BetaStabilizationMilestoneWriter.RewardEconomyLines();
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 58 reward economy/chest balance report.");
        }
    }

    public static class Milestone59AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone59CombatInputControllerReliability.md";
        public const string ReportPath = "output/reports/m59_combat_input_controller_reliability.md";
        public const string PdfPath = "output/pdf/Hollow_M59_Combat_Input_Controller_Reliability.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 59 Assets")]
        public static void Generate()
        {
            Milestone58AssetGenerator.Generate();
            var lines = BetaStabilizationMilestoneWriter.InputReliabilityLines();
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 59 input reliability report.");
        }
    }

    public static class Milestone60AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone60BossPolishBossLabV2.md";
        public const string ReportPath = "output/reports/m60_boss_polish_boss_lab_v2.md";
        public const string PdfPath = "output/pdf/Hollow_M60_Boss_Polish_Boss_Lab_V2.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 60 Assets")]
        public static void Generate()
        {
            Milestone59AssetGenerator.Generate();
            var lines = BetaStabilizationMilestoneWriter.BossPolishLines();
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 60 boss polish report.");
        }
    }

    public static class Milestone61AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone61RoomPoolQualityApprovalWorkflow.md";
        public const string ReportPath = "output/reports/m61_room_pool_quality_approval_workflow.md";
        public const string PdfPath = "output/pdf/Hollow_M61_Room_Pool_Quality_Approval_Workflow.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 61 Assets")]
        public static void Generate()
        {
            Milestone60AssetGenerator.Generate();
            var lines = BetaStabilizationMilestoneWriter.RoomWorkflowLines();
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 61 room approval workflow report.");
        }
    }

    public static class Milestone62AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone62RunReadabilityBetaHudCleanup.md";
        public const string ReportPath = "output/reports/m62_run_readability_beta_hud_cleanup.md";
        public const string PdfPath = "output/pdf/Hollow_M62_Run_Readability_Beta_HUD_Cleanup.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 62 Assets")]
        public static void Generate()
        {
            Milestone61AssetGenerator.Generate();
            var lines = BetaStabilizationMilestoneWriter.HudCleanupLines();
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 62 HUD cleanup report.");
        }
    }

    public static class Milestone63AssetGenerator
    {
        public const string DataDirectory = "Assets/_Hollow/Data/Beta";
        public const string LockPath = DataDirectory + "/BetaContentLock_M63.asset";
        public const string DocsPath = "Docs/Milestone63BetaContentSelectionLock.md";
        public const string ReportPath = "output/reports/m63_beta_content_selection_lock.md";
        public const string PdfPath = "output/pdf/Hollow_M63_Beta_Content_Selection_Lock.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 63 Assets")]
        public static void Generate()
        {
            Milestone62AssetGenerator.Generate();
            Directory.CreateDirectory(DataDirectory);
            var definition = AssetDatabase.LoadAssetAtPath<BetaContentLockDefinition>(LockPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BetaContentLockDefinition>();
                AssetDatabase.CreateAsset(definition, LockPath);
            }

            definition.Configure(
                "m63_beta_content_selection_lock_v1",
                "M63 Beta Content Selection Lock",
                new[] { "balanced", "heavy" },
                new[] { "starter_blade", "starter_pistol", "starter_bolt", "skeletal_sword", "bone_pistol", "dragon_fang", "dragon_pistol" },
                new[] { "m52_standard_sparse_rewards", "m54_treasure_projectile_passive_rewards", "m54_boss_projectile_passive_rewards" },
                new[] { "M13 macro fixtures", "M36 approved rooms", "M48 approved rooms", "M53 boss arenas", "M55 developer lab rooms" },
                new[] { "stone_warden", "splinter_saint", "gravel_maw", "cartouche_widow", "iron_reliquary", "mirror_husk", "ash_comet", "choir_of_teeth", "rust_bishop", "hollow_star_larva" },
                new[] { "blade_trial", "glass_runner", "stone_oath", "macro_maze", "splitter_swarm", "merchants_debt" },
                new[] { "Prototype ArtPass prefabs are allowed only when M56 marks them safe and visible.", "Gameplay remains data/controller authoritative; visual prefabs never own gameplay." });
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            var lines = BetaStabilizationMilestoneWriter.BetaContentLines(definition);
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 63 beta content selection lock.");
        }
    }

    public static class Milestone64AssetGenerator
    {
        public const string QaChecklistPath = Milestone63AssetGenerator.DataDirectory + "/BetaQaChecklist_M64.asset";
        public const string DocsPath = "Docs/Milestone64VerticalSliceBetaLockGate.md";
        public const string ReportJsonPath = "output/reports/m64_vertical_slice_beta_lock_gate.json";
        public const string ReportMarkdownPath = "output/reports/m64_vertical_slice_beta_lock_gate.md";
        public const string PdfPath = "output/pdf/Hollow_M64_Vertical_Slice_Beta_Lock_Gate.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 64 Assets")]
        public static void Generate()
        {
            Milestone63AssetGenerator.Generate();
            var checklist = AssetDatabase.LoadAssetAtPath<BetaQaChecklistDefinition>(QaChecklistPath);
            if (checklist == null)
            {
                checklist = ScriptableObject.CreateInstance<BetaQaChecklistDefinition>();
                AssetDatabase.CreateAsset(checklist, QaChecklistPath);
            }

            checklist.Configure(
                "m64_beta_qa_checklist_v1",
                "M64 Vertical Slice Beta QA Checklist",
                new[] { "New Run", "Continue", "Challenge", "Developer Lab", "Boss Lab", "Room Designer", "Pause/Controls", "Shop/Chest/Boss Clear" },
                new[] { "Windows development build", "VisionOS bounded readiness", "VisionOS immersive readiness" },
                new[] { "HUD stays outside WorldPresentationRoot.", "ArtPass prefabs stay visual-only.", "Save/Continue restores seed, build, room, chest, shop, and portal state.", "Debug overlays are hidden by default." });
            EditorUtility.SetDirty(checklist);
            AssetDatabase.SaveAssets();

            var contentLock = AssetDatabase.LoadAssetAtPath<BetaContentLockDefinition>(Milestone63AssetGenerator.LockPath);
            var report = BetaStabilizationReportBuilder.BuildBetaLockReport(contentLock, checklist);
            BetaStabilizationMilestoneWriter.WriteBetaLock(DocsPath, ReportJsonPath, ReportMarkdownPath, PdfPath, report);
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 64 beta lock report. Ready: {report.readyForBeta}");
        }
    }

    public static class Milestone65AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone65BetaHandoffBuildQaPack.md";
        public const string ReportPath = "output/reports/m65_beta_handoff_build_qa_pack.md";
        public const string PdfPath = "output/pdf/Hollow_M65_Beta_Handoff_Build_QA_Pack.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 65 Assets")]
        public static void Generate()
        {
            Milestone64AssetGenerator.Generate();
            var lines = BetaStabilizationMilestoneWriter.BetaHandoffLines();
            BetaStabilizationMilestoneWriter.WriteSimple(DocsPath, ReportPath, PdfPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 65 beta handoff QA pack.");
        }
    }

    internal static class BetaStabilizationMilestoneWriter
    {
        public static void WriteArtPassCalibration(string docsPath, string jsonPath, string markdownPath, string pdfPath, ArtPassPrefabCalibrationReport report)
        {
            EnsureOutputDirectories(jsonPath, markdownPath, pdfPath, docsPath);
            report.Recalculate();
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, prettyPrint: true));
            var markdown = ArtPassCalibrationMarkdown(report);
            File.WriteAllText(markdownPath, markdown);
            File.WriteAllText(docsPath, markdown);
            HollowSimplePdfWriter.Write(pdfPath, MarkdownToPdfLines(markdown));
        }

        public static void WriteDeveloperCoverage(string docsPath, string jsonPath, string markdownPath, string pdfPath, DeveloperInspectionCoverageReport report)
        {
            EnsureOutputDirectories(jsonPath, markdownPath, pdfPath, docsPath);
            report.Recalculate();
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, prettyPrint: true));
            var markdown = DeveloperCoverageMarkdown(report);
            File.WriteAllText(markdownPath, markdown);
            File.WriteAllText(docsPath, markdown);
            HollowSimplePdfWriter.Write(pdfPath, MarkdownToPdfLines(markdown));
        }

        public static void WriteBetaLock(string docsPath, string jsonPath, string markdownPath, string pdfPath, BetaLockReport report)
        {
            EnsureOutputDirectories(jsonPath, markdownPath, pdfPath, docsPath);
            report.Recalculate();
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, prettyPrint: true));
            var markdown = BetaLockMarkdown(report);
            File.WriteAllText(markdownPath, markdown);
            File.WriteAllText(docsPath, markdown);
            HollowSimplePdfWriter.Write(pdfPath, MarkdownToPdfLines(markdown));
        }

        public static void WriteSimple(string docsPath, string reportPath, string pdfPath, IReadOnlyList<string> lines)
        {
            EnsureOutputDirectories(reportPath, pdfPath, docsPath);
            var markdown = string.Join("\n", lines ?? Array.Empty<string>()) + "\n";
            File.WriteAllText(docsPath, markdown);
            File.WriteAllText(reportPath, markdown);
            HollowSimplePdfWriter.Write(pdfPath, MarkdownToPdfLines(markdown));
        }

        public static IReadOnlyList<string> RewardEconomyLines()
        {
            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.BossRewardPoolPath);
            return new[]
            {
                "# M58: Beta Reward Economy + Chest Balance Pass",
                "",
                "M58 records the beta reward-economy target around the 3 HP baseline.",
                "",
                $"Standard pool: `{standard?.PoolId ?? "missing"}` ({standard?.Rewards.Count ?? 0} entries).",
                $"Treasure pool: `{treasure?.PoolId ?? "missing"}` ({treasure?.Rewards.Count ?? 0} entries).",
                $"Boss pool: `{boss?.PoolId ?? "missing"}` ({boss?.Rewards.Count ?? 0} entries).",
                "",
                "- Ordinary rooms should resolve to coins, HP refill, Normal/Golden Chest, or no reward.",
                "- Item/gear rewards remain restricted to treasure rooms, boss rewards, and shops.",
                "- Chest contents must clearly report coins, HP, or card outcome through pickup reveal UI.",
                "- Balance target: sparse rewards, readable economy pacing, no item spam."
            };
        }

        public static IReadOnlyList<string> InputReliabilityLines()
        {
            var findings = Directory.GetFiles("Assets/_Hollow/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("/Hollow.Editor/", StringComparison.Ordinal))
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, index) => new { path, line, index })
                    .Where(item => item.line.Contains("UnityEngine.Input", StringComparison.Ordinal) ||
                                   item.line.Contains("Input.Get", StringComparison.Ordinal)))
                .Select(item => $"- `{item.path}:{item.index + 1}` {item.line.Trim()}")
                .ToArray();
            return new[]
            {
                "# M59: Combat Input + Controller Reliability Pass",
                "",
                "M59 locks gameplay input around the Unity Input System and DualShock 5 reference mapping.",
                "",
                "## Legacy Input Scan",
                findings.Length == 0 ? "- No non-editor legacy `UnityEngine.Input` usages found." : string.Join("\n", findings),
                "",
                "## Locked Gameplay Controls",
                "- Keyboard: WASD move, arrows aim, J light, K heavy, E interact, Tab swap, Q active, F card, Shift guard, Escape pause.",
                "- DualShock 5: left stick move, right stick aim, Cross interact, L1 swap, R1 light, R2 heavy, Triangle active, Square card, L2 guard, Options pause.",
                "- Debug UI should use visible dev buttons/toggles where function keys are unreliable."
            };
        }

        public static IReadOnlyList<string> BossPolishLines()
        {
            var bossCatalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            var bossRows = bossCatalog != null
                ? bossCatalog.Bosses.Select(boss => $"- `{boss.BossId}`: {boss.DisplayName}, HP {boss.MaxHealth}, band {boss.WorldBand}.").ToArray()
                : new[] { "- Boss catalog missing; regenerate M53." };
            return new[]
            {
                "# M60: Boss Polish + Boss Lab V2",
                "",
                "M60 keeps the 10-boss roster but deep-polishes the beta subset first.",
                "",
                "## Beta Polish Priority",
                "- Stone Warden",
                "- Splinter Saint",
                "- Gravel Maw",
                "- Cartouche Widow",
                "",
                "## Boss Roster",
                string.Join("\n", bossRows),
                "",
                "## Acceptance",
                "- Boss windups are readable.",
                "- Boss Lab can inspect frozen/live boss behavior.",
                "- Boss death and reward clarity are visible before beta lock."
            };
        }

        public static IReadOnlyList<string> RoomWorkflowLines()
        {
            var approvedCount = Directory.Exists(Milestone16AssetGenerator.ApprovedRoomDirectory)
                ? Directory.GetFiles(Milestone16AssetGenerator.ApprovedRoomDirectory, "*.hollowruntime.json", SearchOption.TopDirectoryOnly).Length
                : 0;
            var curatedCount = Directory.Exists(CuratedRoomDesignerDraftGenerator.CuratedDraftDirectory)
                ? Directory.GetFiles(CuratedRoomDesignerDraftGenerator.CuratedDraftDirectory, "*.roomdesigner.json", SearchOption.TopDirectoryOnly).Length
                : 0;
            return new[]
            {
                "# M61: Room Pool Quality + Room Designer Approval Workflow",
                "",
                $"Approved runtime room files: {approvedCount}.",
                $"Curated Room Designer draft files: {curatedCount}.",
                "",
                "## Approval Pipeline",
                "- Draft: editable profile/local Room Designer copy.",
                "- Reviewed: exported runtime JSON has passed validation and manual smoke.",
                "- Approved Runtime: copied into DesignerApproved and included in generated curated drafts.",
                "",
                "## Acceptance",
                "- Edited rooms promote without hand-editing runtime JSON.",
                "- Safe starts, doors, hazards, chests, enemy anchors, boss endpoints, and ArtPass Scene Mode are validated."
            };
        }

        public static IReadOnlyList<string> HudCleanupLines()
        {
            return new[]
            {
                "# M62: Run Readability + Beta HUD Cleanup",
                "",
                "M62 defines the beta HUD source-of-truth layout.",
                "",
                "- Left: player build sidebar with HP, stamina, coins, souls, weapons, armor, active/card, and active set.",
                "- Top-right: graphical minimap only.",
                "- Top-center: world/branch framing and boss bar when applicable.",
                "- Center-right: pickup reveal cards and short toasts.",
                "- Bottom/right dev controls: debug buttons/toggles only in editor/development builds.",
                "",
                "Acceptance: no debug room/enemy/director text overlaps the build sidebar by default."
            };
        }

        public static IReadOnlyList<string> BetaContentLines(BetaContentLockDefinition definition)
        {
            return new[]
            {
                "# M63: Beta Content Selection Lock",
                "",
                $"Lock: `{definition.LockId}` - {definition.DisplayName}.",
                "",
                $"Characters: `{string.Join("`, `", definition.CharacterIds)}`.",
                $"Weapons: `{string.Join("`, `", definition.WeaponIds)}`.",
                $"Reward pools: `{string.Join("`, `", definition.RewardPoolIds)}`.",
                $"Room pools: `{string.Join("`, `", definition.RoomPoolIds)}`.",
                $"Bosses: `{string.Join("`, `", definition.BossIds)}`.",
                $"Challenges: `{string.Join("`, `", definition.ChallengeIds)}`.",
                "",
                "Allowed prototype notes:",
                string.Join("\n", definition.AllowedPrototypeNotes.Select(note => $"- {note}"))
            };
        }

        public static IReadOnlyList<string> BetaHandoffLines()
        {
            return new[]
            {
                "# M65: Beta Handoff Build + QA Pack",
                "",
                "M65 packages the beta handoff instructions for external/manual QA.",
                "",
                "## Required Routes",
                "- New Run",
                "- Continue",
                "- Challenge",
                "- Developer Lab",
                "- Room Designer",
                "- Boss Lab",
                "- Shop, chest, boss clear, and next-world portal",
                "",
                "## Handoff Artifacts",
                "- Windows development build when the local build module is available.",
                "- VisionOS bounded/immersive readiness notes.",
                "- Controls sheet.",
                "- Known issues list.",
                "- Rafal ArtPass checklist.",
                "",
                "Environment-blocked builds must be reported explicitly, never silently skipped."
            };
        }

        private static string ArtPassCalibrationMarkdown(ArtPassPrefabCalibrationReport report)
        {
            return "# M56: ArtPass Wrapper Calibration + Asset Intake QA\n\n" +
                $"Generated: {report.generatedAtUtc}\n\n" +
                $"Total roles: {report.totalRoles}\n" +
                $"Ready: {report.readyCount}\n" +
                $"Needs scale/pivot fix: {report.needsScaleFixCount}\n" +
                $"Missing renderer: {report.missingRendererCount}\n" +
                $"Missing material: {report.missingMaterialCount}\n" +
                $"Missing binding: {report.missingBindingCount}\n" +
                $"Unsafe prefab: {report.unsafePrefabCount}\n\n" +
                "## Wrapper Standard\n\n" +
                "- AP_* prefab root is scale 1,1,1 and positioned at origin.\n" +
                "- Rendered visual is centered on X/Z and sits on y=0.\n" +
                "- Prefab is visual-only: no gameplay colliders or gameplay scripts.\n" +
                "- Catalog binding resolves the same prefab used by gameplay and Room Designer Scene Mode.\n\n" +
                "## Targets\n\n" +
                "| Group | Role | Safety | Readiness | Prefab | Notes |\n" +
                "| --- | --- | --- | --- | --- | --- |\n" +
                string.Join("\n", report.records.Select(record =>
                    $"| {record.group} | {record.displayName} | {record.safetyStatus} | {record.readinessStatus} | `{record.prefabPath}` | {FormatNotes(record.warnings, record.errors)} |")) +
                "\n";
        }

        private static string DeveloperCoverageMarkdown(DeveloperInspectionCoverageReport report)
        {
            return "# M57: Developer Lab Coverage Lock\n\n" +
                $"Generated: {report.generatedAtUtc}\n\n" +
                $"Total inspection entries: {report.totalEntries}\n" +
                $"Bound/safe entries: {report.boundEntries}\n" +
                $"Missing entries: {report.missingEntries}\n\n" +
                "## Coverage\n\n" +
                "| Group | Entity | Role | Lab Room | Binding | Spawn Mode |\n" +
                "| --- | --- | --- | --- | --- | --- |\n" +
                string.Join("\n", report.entries.Select(entry =>
                    $"| {entry.group} | {entry.displayName} | `{entry.prefabRole}` | {entry.labRoom} | {entry.bindingStatus} | {entry.spawnMode} |")) +
                "\n";
        }

        private static string BetaLockMarkdown(BetaLockReport report)
        {
            return "# M64: Vertical Slice Beta Lock Gate\n\n" +
                $"Generated: {report.generatedAtUtc}\n\n" +
                $"Lock ID: `{report.lockId}`\n" +
                $"Ready for beta: {(report.readyForBeta ? "Yes" : "No")}\n\n" +
                "| Check | Status | Details | Remediation |\n" +
                "| --- | --- | --- | --- |\n" +
                string.Join("\n", report.checks.Select(check =>
                    $"| {check.id} | {check.status} | {EscapeMarkdown(check.details)} | {EscapeMarkdown(check.remediation)} |")) +
                "\n";
        }

        private static string FormatNotes(string[] warnings, string[] errors)
        {
            var notes = (errors ?? Array.Empty<string>())
                .Concat(warnings ?? Array.Empty<string>())
                .Where(note => !string.IsNullOrWhiteSpace(note))
                .Select(EscapeMarkdown)
                .ToArray();
            return notes.Length == 0 ? "OK" : string.Join("<br>", notes);
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
        }

        private static IReadOnlyList<string> MarkdownToPdfLines(string markdown)
        {
            return (markdown ?? string.Empty)
                .Split('\n')
                .Select(line => line.Replace("#", string.Empty).Replace("|", "  ").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Take(260)
                .ToArray();
        }

        private static void EnsureOutputDirectories(params string[] paths)
        {
            foreach (var path in paths)
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
    }
}
