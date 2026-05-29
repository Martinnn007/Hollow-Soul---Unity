using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone129ShipSoulLoopGreyboxReport
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
        public Milestone129ShipSoulLoopGreyboxCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone129ShipSoulLoopGreyboxCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone129ShipSoulLoopGreyboxAssetGenerator
    {
        public const string LockId = "m129_ship_soul_loop_greybox_v1";
        public const string Title = "M129 Ship-Soul Loop Greybox";
        public const string DocsPath = "Docs/Milestone129ShipSoulLoopGreybox.md";
        public const string M128ReportPath = "output/reports/m128_soul_biomass_economy_design.md";
        public const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        public const string SpaceshipTerminalPath = "Assets/_Hollow/Scripts/Hollow.World/SpaceshipTerminal.cs";
        public const string ShipUpgradeCatalogPath = "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ShipUpgradeCatalogDefinition.cs";
        public const string NextBranchChoicePath = "Assets/_Hollow/Scripts/Hollow.Branches/NextBranchChoice.cs";
        public const string SpaceshipTestsPath = "Assets/_Hollow/Tests/EditMode/SpaceshipMetaHubTests.cs";
        public const string ReportMarkdownPath = "output/reports/m129_ship_soul_loop_greybox.md";
        public const string ReportJsonPath = "output/reports/m129_ship_soul_loop_greybox.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            M128ReportPath,
            BranchSessionControllerPath,
            SpaceshipTerminalPath,
            ShipUpgradeCatalogPath,
            NextBranchChoicePath,
            SpaceshipTestsPath
        };

        public static readonly string[] RequiredReportPaths =
        {
            ReportMarkdownPath,
            ReportJsonPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 129 Ship-Soul Loop Greybox")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            File.WriteAllText(DocsPath, BuildDocsMarkdown());

            SpaceshipHubSceneGenerator.EnsureShipUpgradeCatalog();

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

        public static Milestone129ShipSoulLoopGreyboxReport BuildReport()
        {
            var checks = new List<Milestone129ShipSoulLoopGreyboxCheck>();
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
            AddTestChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone129ShipSoulLoopGreyboxReport
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

        public static string ToMarkdown(Milestone129ShipSoulLoopGreyboxReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M129 Ship-Soul Loop Greybox Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Ship role: clean roguelite hub; Portal Engine launches normal runs.");
            builder.AppendLine("- Soul rule: world-loop souls bank only on final `Return to Ship`; branch hubs keep souls at risk.");
            builder.AppendLine("- Runtime copy: ship/module surfaces use `Banked Souls`; run HUD does not use `Unbanked Souls` in M129.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone129ShipSoulLoopGreyboxCheck>())
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
            builder.AppendLine("M130 may begin after M129 is reviewed and accepted.");
            return builder.ToString();
        }

        private static string BuildDocsMarkdown()
        {
            return @"# M129: Ship-Soul Loop Greybox

M129 is a runtime greybox and lock-artifact milestone. It makes the ship the beta loop's practical center without rebuilding the whole hub.

## Decisions

- The ship is a clean roguelite hub first: readable, useful, and fast to navigate.
- The Departures launcher is reframed as the `Portal Engine`.
- The Portal Engine is immediately usable once a valid profile is selected.
- The Technology Lab presents existing soul upgrades as ship tech modules.
- Ship module costs use `Banked Souls` copy.
- A persistent `Ship Log` panel explains the loop in plain operational language.
- Normal world-loop souls stay at risk through inter-branch hubs.
- The final world-loop endpoint is `Return to Ship`; this banks souls and returns to ship arrival.
- Normal-run death returns to ship arrival with zero souls banked.
- Arrivals quarantine remains the post-run reset beat.

## Runtime Copy

- Portal launch terminal: `Portal Engine`
- Ship log panel: `Ship Log`
- Ship log message: `Portal Engine online. Banked Souls are safe aboard ship. Souls collected during a run bank only after final return.`
- Final endpoint: `Return to Ship`
- Module names: `Vitals Module`, `Stamina Module`, `Reactor Module`, `Combat Module`

## Deferrals

- No save schema changes.
- No reward or economy schema changes.
- No biomass runtime behavior.
- No Black Orb or generic-resource runtime behavior.
- No runtime run-HUD rename to `Unbanked Souls` in M129.

## Acceptance

- The player can start at the ship, use the Portal Engine, enter a normal run, move through branch hubs without banking, and bank souls only through final Return to Ship.
- The player can spend Banked Souls on ship tech modules.
- The ship includes a visible Ship Log surface that explains the banking rule.
- Menus remain fallback access, while the ship communicates the beta loop.
";
        }

        private static void AddDocsChecks(List<Milestone129ShipSoulLoopGreyboxCheck> checks)
        {
            if (!File.Exists(DocsPath))
            {
                AddCheck(checks, "docs:m129", "Documentation", false, $"Missing `{DocsPath}`.");
                return;
            }

            var markdown = File.ReadAllText(DocsPath);
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:ship-loop-decisions",
                "Documentation",
                new[]
                {
                    "clean roguelite hub",
                    "Portal Engine",
                    "Technology Lab",
                    "ship tech modules",
                    "Banked Souls",
                    "Ship Log",
                    "Return to Ship",
                    "Normal-run death returns to ship arrival"
                });
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:deferrals",
                "Documentation",
                new[]
                {
                    "No save schema changes",
                    "No reward or economy schema changes",
                    "No biomass runtime behavior",
                    "No Black Orb or generic-resource runtime behavior",
                    "No runtime run-HUD rename to `Unbanked Souls`"
                });
        }

        private static void AddRuntimeChecks(List<Milestone129ShipSoulLoopGreyboxCheck> checks)
        {
            AddCheck(
                checks,
                "runtime:portal-engine-copy",
                "Runtime",
                BranchSessionController.PortalEngineDisplayName == "Portal Engine",
                $"Portal Engine display is `{BranchSessionController.PortalEngineDisplayName}`.");

            AddContainsAllCheck(
                checks,
                BranchSessionController.ShipLogMessage,
                "runtime:ship-log-message",
                "Runtime",
                new[] { "Portal Engine online", "Banked Souls are safe", "bank only after final return" });

            var terminalSource = ReadFile(SpaceshipTerminalPath);
            AddContainsAllCheck(
                checks,
                terminalSource,
                "runtime:ship-log-terminal-kind",
                "Runtime",
                new[] { "ShipLog = 5" });

            var branchSource = ReadFile(BranchSessionControllerPath);
            AddContainsAllCheck(
                checks,
                branchSource,
                "runtime:ship-terminals",
                "Runtime",
                new[]
                {
                    "SpaceshipTerminalKind.ShipLog",
                    "TryReadShipLog",
                    "PortalEngineDisplayName",
                    "Banked Souls",
                    "installed."
                });
            AddContainsAllCheck(
                checks,
                branchSource,
                "runtime:final-only-banking",
                "Runtime",
                new[]
                {
                    "if (IsM20Branch())",
                    "EnterInterBranchHub();",
                    "if (choice.Kind == HubPortalKind.FinalExtraction)",
                    "CompleteActiveRunIfPersistent()",
                    "ReturnToSpaceshipOrProfileMenu(SpaceshipArrivalReason.NormalSuccess, soulsBanked)"
                });
            AddCheck(
                checks,
                "runtime:no-unbanked-hud-copy",
                "Runtime",
                branchSource.IndexOf("Unbanked Souls", StringComparison.OrdinalIgnoreCase) < 0,
                "Branch runtime copy does not use `Unbanked Souls` in M129.");

            var catalog = ShipUpgradeCatalogDefinition.CreateRuntimeDefault();
            var moduleNames = new[]
            {
                ("reinforced_vitals", "Vitals Module"),
                ("stamina_cell", "Stamina Module"),
                ("reactor_tuning", "Reactor Module"),
                ("combat_calibration", "Combat Module")
            };
            var missingModules = moduleNames
                .Where(row => !catalog.TryGetUpgrade(row.Item1, out var upgrade) || upgrade.DisplayName != row.Item2)
                .Select(row => row.Item2)
                .ToArray();
            AddCheck(
                checks,
                "runtime:module-names",
                "Runtime",
                missingModules.Length == 0,
                missingModules.Length == 0 ? "Default ship upgrades use module names." : $"Missing module names: {string.Join(", ", missingModules)}.");

            AddCheck(
                checks,
                "runtime:return-to-ship-choice",
                "Runtime",
                NextBranchChoice.CreateFinalExtraction(12901, 3).DisplayName == "Return to Ship" &&
                ReadFile(BranchSessionControllerPath).Contains("return \"Return to Ship\";"),
                "Final extraction choice and display override use `Return to Ship`.");
        }

        private static void AddTestChecks(List<Milestone129ShipSoulLoopGreyboxCheck> checks)
        {
            var tests = ReadFile(SpaceshipTestsPath);
            AddContainsAllCheck(
                checks,
                tests,
                "tests:runtime-coverage",
                "Tests",
                new[]
                {
                    "PortalEngineDisplayName",
                    "MainHallShipLogPanelReportsShipSoulRule",
                    "WorldLoopBranchReturnEntersInterBranchHubWithoutBankingSouls",
                    "WorldLoopFinalReturnToShipBanksSoulsAndRoutesToArrival",
                    "Vitals Module\\n5 Banked Souls"
                });
        }

        private static void AddDependencyChecks(List<Milestone129ShipSoulLoopGreyboxCheck> checks)
        {
            var m128 = ReadFile(M128ReportPath);
            AddContainsAllCheck(
                checks,
                m128,
                "dependency:m128-pass",
                "Dependency",
                new[]
                {
                    "# M128 Soul + Biomass Economy Design Pass Report",
                    "- Result: PASSED",
                    "M129 Ship-Soul Loop Greybox"
                });
        }

        private static string ReadFile(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static void AddContainsAllCheck(
            List<Milestone129ShipSoulLoopGreyboxCheck> checks,
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
            List<Milestone129ShipSoulLoopGreyboxCheck> checks,
            string id,
            string category,
            bool passed,
            string detail)
        {
            checks.Add(new Milestone129ShipSoulLoopGreyboxCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }
    }
}
