using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone133NpcSpecialEncounterPrototypeSetReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public int totalChecks;
        public int passedChecks;
        public string[] evidencePaths;
        public string[] failures;
        public Milestone133NpcSpecialEncounterPrototypeSetCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone133NpcSpecialEncounterPrototypeSetCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator
    {
        public const string LockId = "m133_npc_special_encounter_prototype_set_v1";
        public const string Title = "M133 NPC/Special Encounter Prototype Set";
        public const string DocsPath = "Docs/Milestone133NpcSpecialEncounterPrototypeSet.md";
        public const string M132ReportPath = "output/reports/m132_biome_world_selection_lock.md";
        public const string BranchGeneratorPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchGenerator.cs";
        public const string BranchFeaturePlanPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchFeaturePlan.cs";
        public const string BranchRoomRolePath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchRoomRole.cs";
        public const string BranchSessionContentPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionContent.cs";
        public const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        public const string SpecialEncounterKindPath = "Assets/_Hollow/Scripts/Hollow.Branches/SpecialEncounterKind.cs";
        public const string SpecialEncounterResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/SpecialEncounterResolver.cs";
        public const string ProceduralRewardResolverPath = "Assets/_Hollow/Scripts/Hollow.Branches/ProceduralRewardResolver.cs";
        public const string RoomCombatControllerPath = "Assets/_Hollow/Scripts/Hollow.Combat/RoomCombatController.cs";
        public const string EnemyCatalogPath = "Assets/_Hollow/Scripts/Hollow.Combat/EnemyCatalog.cs";
        public const string BranchMiniMapControllerPath = "Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs";
        public const string RoomDesignerProjectPath = "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerProject.cs";
        public const string SoulEaterRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/special_soul_eater_single_1x1.hollowruntime.json";
        public const string EscapistRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/special_escapist_single_1x1.hollowruntime.json";
        public const string M133TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone133NpcSpecialEncounterPrototypeSetTests.cs";
        public const string ReportMarkdownPath = "output/reports/m133_npc_special_encounter_prototype_set.md";
        public const string ReportJsonPath = "output/reports/m133_npc_special_encounter_prototype_set.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M132ReportPath,
            BranchGeneratorPath,
            BranchFeaturePlanPath,
            BranchRoomRolePath,
            BranchSessionContentPath,
            BranchSessionControllerPath,
            SpecialEncounterKindPath,
            SpecialEncounterResolverPath,
            ProceduralRewardResolverPath,
            RoomCombatControllerPath,
            EnemyCatalogPath,
            BranchMiniMapControllerPath,
            RoomDesignerProjectPath,
            SoulEaterRoomPath,
            EscapistRoomPath,
            M133TestsPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 133 NPC Special Encounter Prototype Set")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            File.WriteAllText(DocsPath, BuildDocsMarkdown());

            var report = BuildReport();
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(ReportMarkdownPath, ToMarkdown(report));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"{Title} generation {report.result}: {report.passedChecks}/{report.totalChecks} checks passed. Report: {ReportMarkdownPath}";
            if (report.passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static Milestone133NpcSpecialEncounterPrototypeSetReport BuildReport()
        {
            var checks = new List<Milestone133NpcSpecialEncounterPrototypeSetCheck>();
            foreach (var path in RequiredEvidencePaths)
            {
                AddCheck(
                    checks,
                    $"evidence:{Path.GetFileName(path)}",
                    "Evidence",
                    File.Exists(path),
                    File.Exists(path) ? $"Found `{path}`." : $"Missing `{path}`.");
            }

            AddDocsChecks(checks);
            AddRuntimeChecks(checks);
            AddEncounterChecks(checks);
            AddTemplateChecks(checks);
            AddHudAndDesignerChecks(checks);
            AddTestChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone133NpcSpecialEncounterPrototypeSetReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                totalChecks = checks.Count,
                passedChecks = checks.Count(check => check.passed),
                evidencePaths = RequiredEvidencePaths.ToArray(),
                failures = failures,
                checks = checks.ToArray()
            };
        }

        public static string ToMarkdown(Milestone133NpcSpecialEncounterPrototypeSetReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M133 NPC/Special Encounter Prototype Set Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Special room rule: normal world-loop branches roll a deterministic 15% optional terminal Special Encounter leaf.");
            builder.AppendLine("- Encounter set: Soul Eater offer or Escapist hunt, selected 50/50 by seed.");
            builder.AppendLine("- Reward rule: Soul Eater sells one rare reward for 10 Souls; Escapist kill spawns a Golden Chest; timeout has no penalty or reward.");
            builder.AppendLine();
            builder.AppendLine("## Evidence");
            builder.AppendLine();
            foreach (var path in report.evidencePaths ?? Array.Empty<string>())
            {
                builder.AppendLine($"- `{path}`");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            foreach (var check in report.checks ?? Array.Empty<Milestone133NpcSpecialEncounterPrototypeSetCheck>())
            {
                builder.AppendLine($"- [{(check.passed ? "PASS" : "FAIL")}] `{check.id}` ({check.category}) - {check.detail}");
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            builder.AppendLine();
            if (report.failures == null || report.failures.Length == 0)
            {
                builder.AppendLine("None.");
            }
            else
            {
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("## Next Gate");
            builder.AppendLine();
            builder.AppendLine("M134 may build on these optional special encounter prototypes after M133 is reviewed and accepted.");
            return builder.ToString();
        }

        public static string BuildDocsMarkdown()
        {
            return
                "# M133: NPC/Special Encounter Prototype Set\n\n" +
                "## Summary\n" +
                "M133 adds two rare optional special encounter prototypes: `Soul Eater` and `Escapist`. These are terminal branch leaves, never required for boss access, and never boss-key eligible. The milestone stays practical: one clear interaction, one clear outcome, and no quest system.\n\n" +
                "## Branch Rule\n" +
                "- Normal world-loop branches roll a deterministic `15%` chance to add one optional terminal `SpecialEncounter` room.\n" +
                "- The rule applies to prologue and hub-selected normal branches.\n" +
                "- The rule does not apply to spaceship, Developer Lab, or challenge-only scaffolds.\n" +
                "- Special rooms are leaves, not boss-path critical, and do not replace boss, treasure, secret, wave, or corrupted rooms.\n" +
                "- The encounter kind is seeded `50/50`: `SoulEater` or `Escapist`.\n\n" +
                "## Soul Eater\n" +
                "- Non-hostile NPC/shop prototype.\n" +
                "- Uses current-run `Souls` copy only. Do not use `Unbanked Souls` or `Banked Souls` in this runtime interaction.\n" +
                "- Offers one seeded rare build reward for `10 Souls`.\n" +
                "- If the player has fewer than 10 Souls, show `Need 10 Souls` feedback.\n" +
                "- A successful purchase spends current-run souls and grants the reward through the existing reward application and reveal systems.\n" +
                "- The room does not lock doors and does not require combat.\n\n" +
                "## Escapist\n" +
                "- Timed escape hunt room.\n" +
                "- Doors lock on entry and a `20s` timer starts.\n" +
                "- One luminous wisp-style Escapist target appears.\n" +
                "- Kill before escape: clear the room, unlock doors, and spawn an existing `Golden Chest`.\n" +
                "- Timer expires: the Escapist leaves, the room clears, doors unlock, and no reward is granted.\n" +
                "- There is no penalty on failure.\n\n" +
                "## Presentation\n" +
                "- Templates: `special_soul_eater_single_1x1` and `special_escapist_single_1x1`.\n" +
                "- Special rooms inherit the active branch biome.\n" +
                "- Room Designer can preview and edit both templates.\n" +
                "- Minimap/HUD exposes `Special Encounter`, with room labels `Soul Eater` and `Escapist`.\n" +
                "- Escapist combat status shows the active timer.\n\n" +
                "## Deferrals\n" +
                "Mimic, Drunk NPC, companions, full NPC quest chains, deeper special encounter systems, biomass, Black Orb, and new chest kinds are deferred.\n\n" +
                "## Interfaces\n" +
                "- Adds `BranchRoomRole.SpecialEncounter`.\n" +
                "- Adds `SpecialEncounterKind.SoulEater` and `SpecialEncounterKind.Escapist`.\n" +
                "- Adds `SpecialEncounterResolver.SpecialEncounterRollPercent = 15`.\n" +
                "- No profile save schema changes.\n" +
                "- No reward schema, economy schema, chest-kind, companion, quest, biomass, or Black Orb changes.\n";
        }

        private static void AddDocsChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            var docs = Read(DocsPath);
            RequireAll(checks, "docs:m133-decisions", "Documentation", docs, new[]
            {
                "deterministic `15%`",
                "`50/50`: `SoulEater` or `Escapist`",
                "Uses current-run `Souls` copy only",
                "`Need 10 Souls`",
                "`20s` timer",
                "`Golden Chest`",
                "Mimic, Drunk NPC, companions",
                "No profile save schema changes"
            });
        }

        private static void AddRuntimeChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            var roles = Read(BranchRoomRolePath);
            var kinds = Read(SpecialEncounterKindPath);
            var resolver = Read(SpecialEncounterResolverPath);
            var generator = Read(BranchGeneratorPath);
            var featurePlan = Read(BranchFeaturePlanPath);
            var content = Read(BranchSessionContentPath);

            RequireAll(checks, "runtime:special-public-surface", "Runtime", roles + kinds + resolver + content, new[]
            {
                "SpecialEncounter = 8",
                "SpecialEncounterRollPercent = 15",
                "SoulEater = 1",
                "Escapist = 2",
                "special_soul_eater_single_1x1",
                "special_escapist_single_1x1",
                "SpecialSoulEaterRoomAsset",
                "SpecialEscapistRoomAsset"
            });
            RequireAll(checks, "runtime:special-terminal-policy", "Runtime", generator, new[]
            {
                "enableSpecialEncounterLeaf",
                "ShouldRollSpecialEncounterLeaf",
                "TryPlaceEndpointRecord",
                "BranchRoomRole.SpecialEncounter",
                "must be a terminal leaf",
                "must not attach to boss, secret, treasure, corrupted, or wave endpoints"
            });
            RequireAll(checks, "runtime:special-not-boss-key", "Runtime", featurePlan, new[]
            {
                "room.Role == BranchRoomRole.Combat"
            });
        }

        private static void AddEncounterChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            var session = Read(BranchSessionControllerPath);
            var combat = Read(RoomCombatControllerPath);
            var rewards = Read(ProceduralRewardResolverPath);
            var enemies = Read(EnemyCatalogPath);
            var resolver = Read(SpecialEncounterResolverPath);

            RequireAll(checks, "encounter:soul-eater-offer", "Runtime", session + SpecialEncounterResolverPath, new[]
            {
                "TryUseSoulEaterOffer",
                "Need {SpecialEncounterResolver.SoulEaterSoulPrice} Souls",
                "SpendSouls",
                "ResolveSoulEaterOffer",
                "Soul Eater: {grant.DisplayName} gained"
            });
            RequireAll(checks, "encounter:escapist-timer", "Runtime", session + combat + enemies + resolver, new[]
            {
                "TickEscapistEncounter",
                "EscapistTimerSeconds = 20f",
                "ForceClearRoomWithoutReward",
                "spawnEnemyEscapist",
                "SetRuntimeStatusOverride",
                "Escapist escaped. No reward."
            });
            RequireAll(checks, "encounter:escapist-golden-chest", "Rewards", rewards, new[]
            {
                "BranchRoomRole.SpecialEncounter",
                "SpecialEncounterKind.Escapist",
                "GoldenChestGrant"
            });
        }

        private static void AddTemplateChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            AddTemplateCheck(checks, "template:soul-eater", SoulEaterRoomPath, SpecialEncounterResolver.SoulEaterRoomAssetId, "spawnEnemySoulEater");
            AddTemplateCheck(checks, "template:escapist", EscapistRoomPath, SpecialEncounterResolver.EscapistRoomAssetId, SpecialEncounterResolver.EscapistSpawnKind);
        }

        private static void AddTemplateCheck(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks, string id, string path, string expectedId, string expectedSpawnKind)
        {
            if (!File.Exists(path))
            {
                AddCheck(checks, id, "Templates", false, $"Missing `{path}`.");
                return;
            }

            if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(path), out var asset, out var error))
            {
                AddCheck(checks, id, "Templates", false, $"Import failed: {error}");
                return;
            }

            var valid = asset.Id == expectedId &&
                        asset.EnemySpawns.Any(spawn => spawn.kind == expectedSpawnKind) &&
                        RuntimeRoomValidator.Validate(asset).IsValid;
            AddCheck(
                checks,
                id,
                "Templates",
                valid,
                valid
                    ? $"`{expectedId}` imports, validates, and includes `{expectedSpawnKind}`."
                    : $"`{expectedId}` template is missing expected id, spawn, or runtime validity.");
        }

        private static void AddHudAndDesignerChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            var minimap = Read(BranchMiniMapControllerPath);
            var designer = Read(RoomDesignerProjectPath);
            var session = Read(BranchSessionControllerPath);

            RequireAll(checks, "presentation:hud-minimap", "Presentation", minimap + session, new[]
            {
                "BranchRoomRole.SpecialEncounter",
                "SpecialEncounterResolver.DisplayNameForAssetId",
                "\"S\"",
                "Escapist {Mathf.CeilToInt"
            });
            RequireAll(checks, "presentation:room-designer", "Room Designer", designer, new[]
            {
                "EnemyEscapist",
                "spawnEnemyEscapist",
                "EnemyKinds"
            });
        }

        private static void AddTestChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            var tests = Read(M133TestsPath);
            RequireAll(checks, "tests:m133-lock-tests", "Tests", tests, new[]
            {
                "SpecialEncounterPolicyIsRareTerminalAndSeeded",
                "SoulEaterOfferUsesRunSoulsAndCuratedReward",
                "EscapistRewardsGoldenChestOnlyOnSuccessPolicy",
                "SpecialTemplatesImportAndInheritBranchBiome",
                "GeneratedReportsArePresentPassingAndUseM133LockId",
                "ValidatorReportsGeneratedStateValid"
            });
        }

        private static void AddDependencyChecks(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks)
        {
            var m132Report = Read(M132ReportPath);
            AddCheck(
                checks,
                "dependency:m132-passing-report",
                "Dependency",
                m132Report.Contains("- Result: PASSED") && m132Report.Contains(Milestone132BiomeWorldSelectionLockAssetGenerator.LockId),
                "M132 passing report exists and includes the M132 lock id.");
        }

        private static void RequireAll(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks, string id, string category, string content, IEnumerable<string> needles)
        {
            var missing = needles.Where(needle => !content.Contains(needle)).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? "Required lock strings found." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone133NpcSpecialEncounterPrototypeSetCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone133NpcSpecialEncounterPrototypeSetCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }

        private static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
    }
}
