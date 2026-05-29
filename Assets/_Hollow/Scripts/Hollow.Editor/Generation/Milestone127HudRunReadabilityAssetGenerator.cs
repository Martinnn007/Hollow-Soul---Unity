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
    public sealed class Milestone127HudRunReadabilityReport
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
        public Milestone127HudRunReadabilityCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone127HudRunReadabilityCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone127HudRunReadabilityAssetGenerator
    {
        public const string LockId = "m127_hud_run_readability_v1";
        public const string Title = "M127 HUD + Run Readability Pass";
        public const string DocsPath = "Docs/Milestone127HudRunReadabilityPass.md";
        public const string ReportMarkdownPath = "output/reports/m127_hud_run_readability.md";
        public const string ReportJsonPath = "output/reports/m127_hud_run_readability.json";

        public static readonly string[] RequiredEvidencePaths =
        {
            DocsPath,
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/RunLocationLabelFormatter.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/DebugSpawnMenuController.cs",
            "Assets/_Hollow/Scripts/Hollow.Input/GameplayInputReader.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/PlayerBuildHudController.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/PickupRevealController.cs"
        };

        public static readonly string[] RequiredReportPaths =
        {
            ReportMarkdownPath,
            ReportJsonPath
        };

        [MenuItem("Hollow/Generation/Generate Milestone 127 HUD + Run Readability Pass")]
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

        public static Milestone127HudRunReadabilityReport BuildReport()
        {
            var checks = new List<Milestone127HudRunReadabilityCheck>();
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
            AddMiniMapChecks(checks);
            AddDebugSpawnChecks(checks);
            AddHudSurfaceChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone127HudRunReadabilityReport
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

        public static string ToMarkdown(Milestone127HudRunReadabilityReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M127 HUD + Run Readability Pass Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Enlarged minimap/map interaction: deferred beyond M127.");
            builder.AppendLine("- Runtime scope: HUD readability cleanup, debug affordance hiding, and lock validation only.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone127HudRunReadabilityCheck>())
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
            builder.AppendLine("M128 Soul + Biomass Economy Design Pass may begin after M127 is reviewed and accepted.");
            return builder.ToString();
        }

        private static string BuildDocsMarkdown()
        {
            return @"# M127: HUD + Run Readability Pass

M127 locks the beta HUD readability pass after M126's design lock.

## Decisions

- Enlarged minimap and map interaction are deferred beyond M127.
- The top-right minimap uses a plain dark rectangular background instead of the previous cosmic frame.
- The current location label sits below the minimap.
- The bottom-right Debug Spawn button is hidden from normal screen UI.
- `F10` opens the Developer Spawn Menu only in editor/development builds.

## Acceptance

- Health, souls, coins, optional key/boss-key, active item, consumable card, pickup reveal, and minimap surfaces remain present.
- Normal gameplay has no always-visible developer/debug spawn button.
- Developer/debug surfaces remain available only through explicit debug routes.
- Location labels resolve Spaceship, Developer Lab, inter-branch hubs, and world branch names.
";
        }

        private static void AddDocsChecks(List<Milestone127HudRunReadabilityCheck> checks)
        {
            if (!File.Exists(DocsPath))
            {
                AddCheck(checks, "docs:m127", "Documentation", false, $"Missing `{DocsPath}`.");
                return;
            }

            var markdown = File.ReadAllText(DocsPath);
            AddContainsAllCheck(
                checks,
                markdown,
                "docs:m127-decisions",
                "Documentation",
                new[]
                {
                    "Enlarged minimap and map interaction are deferred beyond M127",
                    "plain dark rectangular background",
                    "current location label sits below the minimap",
                    "`F10` opens the Developer Spawn Menu"
                });
        }

        private static void AddMiniMapChecks(List<Milestone127HudRunReadabilityCheck> checks)
        {
            const string path = "Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs";
            if (!File.Exists(path))
            {
                AddCheck(checks, "minimap:source", "Minimap", false, $"Missing `{path}`.");
                return;
            }

            var source = File.ReadAllText(path);
            AddCheck(checks, "minimap:plain-background", "Minimap", !source.Contains("CosmicMiniMapPanel") && source.Contains("image.sprite = null"), "Minimap no longer loads the cosmic frame sprite.");
            AddCheck(checks, "minimap:dark-rectangle", "Minimap", source.Contains("0.72f") && source.Contains("BranchMiniMap.MapPanel"), "Minimap panel uses a dark rectangle color.");
            AddCheck(checks, "minimap:location-label", "Minimap", source.Contains("BranchMiniMap.LocationLabel") && source.Contains("CreateLocationLabel"), "Location label is rendered below the minimap.");
        }

        private static void AddDebugSpawnChecks(List<Milestone127HudRunReadabilityCheck> checks)
        {
            const string debugPath = "Assets/_Hollow/Scripts/Hollow.Branches/DebugSpawnMenuController.cs";
            const string inputPath = "Assets/_Hollow/Scripts/Hollow.Input/GameplayInputReader.cs";
            if (!File.Exists(debugPath) || !File.Exists(inputPath))
            {
                AddCheck(checks, "debug-spawn:source", "Debug Spawn", false, "Debug spawn/input source files are missing.");
                return;
            }

            var debugSource = File.ReadAllText(debugPath);
            var inputSource = File.ReadAllText(inputPath);
            AddCheck(checks, "debug-spawn:no-button", "Debug Spawn", !debugSource.Contains("DrawToggleButton") && !debugSource.Contains("GUI.Button(rect"), "Bottom-right OnGUI toggle button is removed.");
            AddCheck(checks, "debug-spawn:f10", "Debug Spawn", debugSource.Contains("ReadDebugSpawnMenuTogglePressed") && inputSource.Contains("f10Key"), "F10 toggles the developer spawn menu.");
            AddCheck(checks, "debug-spawn:gated", "Debug Spawn", debugSource.Contains("Application.isEditor || Debug.isDebugBuild"), "Debug spawn remains editor/development gated.");
        }

        private static void AddHudSurfaceChecks(List<Milestone127HudRunReadabilityCheck> checks)
        {
            var playerHud = ReadFile("Assets/_Hollow/Scripts/Hollow.UI/Shell/PlayerBuildHudController.cs");
            var pickupReveal = ReadFile("Assets/_Hollow/Scripts/Hollow.UI/Shell/PickupRevealController.cs");
            AddCheck(checks, "hud:core-surfaces", "HUD", ContainsAll(playerHud, "RenderedHeartCount", "HasRenderedSoulsCounter", "HasRenderedCoinsCounter", "HasRenderedKeysCounter", "HasRenderedActiveItemIcon", "HasRenderedConsumableCardIcon"), "Player HUD exposes health, currency, key, active item, and card surfaces.");
            AddCheck(checks, "hud:pickup-reveal", "HUD", ContainsAll(pickupReveal, "PickupReveal.Panel", "RarityColor", "ToastText"), "Pickup reveal remains available.");
        }

        private static string ReadFile(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static bool ContainsAll(string text, params string[] required)
        {
            return required.All(value => text.Contains(value));
        }

        private static void AddContainsAllCheck(
            List<Milestone127HudRunReadabilityCheck> checks,
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
            List<Milestone127HudRunReadabilityCheck> checks,
            string id,
            string category,
            bool passed,
            string detail)
        {
            checks.Add(new Milestone127HudRunReadabilityCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }
    }
}
