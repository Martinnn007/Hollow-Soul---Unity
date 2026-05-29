using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone128SoulBiomassEconomyDesignReport
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
        public Milestone128SoulBiomassEconomyDesignCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone128SoulBiomassEconomyDesignCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone128SoulBiomassEconomyDesignAssetGenerator
    {
        public const string LockId = "m128_soul_biomass_economy_design_v1";
        public const string Title = "M128 Soul + Biomass Economy Design Pass";
        public const string DocsPath = "Docs/Milestone128SoulBiomassEconomyDesignPass.md";
        public const string MasterDesignPath = "Docs/HollowSoul_MasterDesignAndBetaRoadmap.md";
        public const string FoundationGddPath = "Docs/HollowSoul_GameDesignFoundation_GDD_V1.md";
        public const string M127ReportPath = "output/reports/m127_hud_run_readability.md";
        public const string ReportMarkdownPath = "output/reports/m128_soul_biomass_economy_design.md";
        public const string ReportJsonPath = "output/reports/m128_soul_biomass_economy_design.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            MasterDesignPath,
            FoundationGddPath,
            M127ReportPath
        };

        public static readonly string[] RequiredReportPaths =
        {
            ReportMarkdownPath,
            ReportJsonPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 128 Soul + Biomass Economy Design Pass")]
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

        public static Milestone128SoulBiomassEconomyDesignReport BuildReport()
        {
            var checks = new List<Milestone128SoulBiomassEconomyDesignCheck>();
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
            AddRoadmapChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone128SoulBiomassEconomyDesignReport
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

        public static string ToMarkdown(Milestone128SoulBiomassEconomyDesignReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M128 Soul + Biomass Economy Design Pass Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Runtime scope: design artifact only; no gameplay, HUD, save, reward, economy, or schema changes.");
            builder.AppendLine("- Canonical future run label: `Unbanked Souls`.");
            builder.AppendLine("- Biomass runtime: deferred beyond M128.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone128SoulBiomassEconomyDesignCheck>())
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
            builder.AppendLine("M129 Ship-Soul Loop Greybox may begin after M128 is reviewed and accepted.");
            return builder.ToString();
        }

        private static string BuildDocsMarkdown()
        {
            return @"# M128: Soul + Biomass Economy Design Pass

M128 is a design-artifact milestone. It clarifies economy roles before any new currency systems are added.

## Decisions

- Souls collected during a run are canonically **Unbanked Souls** for future player-facing copy.
- Unbanked Souls are risky fuel and meta progression: they are valuable during the run, lost on death, and secured only through extraction or banking.
- Banked souls should primarily repair or reopen Derelict Sanctuary ship systems, modules, access, context, and routes.
- Existing soul-based stat upgrades are placeholder scaffolding until the ship-soul loop is replaced or reframed by stronger ship-system unlocks.
- Coins are safe, practical run-local shop money. They should not compete with souls as a long-term progression currency.
- Biomass is future organic crafting material gathered from world salvage such as alien flora, nests, corpses, and biome matter.
- Biomass is not a runtime currency in M128.

## Deferrals

- No biomass HUD.
- No biomass pickups.
- No biomass save data.
- No biomass reward, economy, or shop schema.
- No Black Orb or generic resource economy.
- No soul-ammo experiment.
- No runtime HUD rename from souls to Unbanked Souls in M128.

## Copy Direction

Use plain action-RPG language. Copy should be clear and readable before it becomes poetic.

Recommended future UI terms:

- Current run wallet: `Unbanked Souls`
- Secured profile wallet: `Banked Souls`
- Practical shop wallet: `Coins`
- Future crafting salvage: `Biomass`

## Acceptance

- Souls do not read as duplicate coins.
- The player can understand that continuing a run risks unbanked souls.
- Coins remain run-local shop money.
- Biomass is explicitly locked as future organic crafting/world-salvage material, with no M128 runtime implementation.
- M129 or later may implement ship-system soul spend and apply the Unbanked Souls label in runtime UI.
";
        }

        private static void AddDocsChecks(List<Milestone128SoulBiomassEconomyDesignCheck> checks)
        {
            if (!File.Exists(DocsPath))
            {
                AddCheck(checks, "docs:m128", "Documentation", false, $"Missing `{DocsPath}`.");
                return;
            }

            var markdown = File.ReadAllText(DocsPath);
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:runtime-scope",
                "Documentation",
                new[]
                {
                    "design-artifact milestone",
                    "before any new currency systems are added",
                    "Biomass is not a runtime currency in M128"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:soul-role",
                "Documentation",
                new[]
                {
                    "Unbanked Souls",
                    "lost on death",
                    "secured only through extraction or banking",
                    "repair or reopen Derelict Sanctuary ship systems"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:coin-role",
                "Documentation",
                new[]
                {
                    "Coins are safe",
                    "run-local shop money",
                    "should not compete with souls"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:biomass-role",
                "Documentation",
                new[]
                {
                    "future organic crafting material",
                    "world salvage",
                    "flora",
                    "nests",
                    "corpses",
                    "biome matter"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:deferrals",
                "Documentation",
                new[]
                {
                    "No biomass HUD",
                    "No biomass pickups",
                    "No biomass save data",
                    "No Black Orb or generic resource economy",
                    "No soul-ammo experiment",
                    "No runtime HUD rename"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:ship-upgrade-policy",
                "Documentation",
                new[]
                {
                    "placeholder scaffolding",
                    "ship-system unlocks"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:copy-direction",
                "Documentation",
                new[]
                {
                    "plain action-RPG language",
                    "clear and readable"
                });
        }

        private static void AddRoadmapChecks(List<Milestone128SoulBiomassEconomyDesignCheck> checks)
        {
            var roadmap = ReadFile(MasterDesignPath);
            AddContainsAllCheck(
                checks,
                roadmap,
                "roadmap:m128-present",
                "Roadmap",
                new[]
                {
                    "### M128: Soul + Biomass Economy Design Pass",
                    "Souls: run pickup, bank/extract, death risk, ship use",
                    "Coins: run/shop role",
                    "Black Orb and generic resources deferred"
                });

            var foundation = ReadFile(FoundationGddPath);
            AddContainsAllCheck(
                checks,
                foundation,
                "foundation:soul-pillar",
                "Roadmap",
                new[]
                {
                    "Souls are necessary fuel",
                    "repair technology",
                    "soul rewards should feel valuable"
                });
        }

        private static void AddDependencyChecks(List<Milestone128SoulBiomassEconomyDesignCheck> checks)
        {
            var m127 = ReadFile(M127ReportPath);
            AddContainsAllCheck(
                checks,
                m127,
                "dependency:m127-pass",
                "Dependency",
                new[]
                {
                    "# M127 HUD + Run Readability Pass Report",
                    "- Result: PASSED",
                    "M128 Soul + Biomass Economy Design Pass"
                });
        }

        private static string ReadFile(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static void AddContainsAllCheck(
            List<Milestone128SoulBiomassEconomyDesignCheck> checks,
            string text,
            string id,
            string category,
            IReadOnlyList<string> required)
        {
            var missing = required.Where(value => text.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? $"Found {required.Count} required entries." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(
            List<Milestone128SoulBiomassEconomyDesignCheck> checks,
            string id,
            string category,
            bool passed,
            string detail)
        {
            checks.Add(new Milestone128SoulBiomassEconomyDesignCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }
    }
}
