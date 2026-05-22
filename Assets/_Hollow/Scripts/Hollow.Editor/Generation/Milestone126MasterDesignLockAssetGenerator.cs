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
    public sealed class Milestone126MasterDesignLockReport
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
        public Milestone126MasterDesignLockCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone126MasterDesignLockCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone126MasterDesignLockAssetGenerator
    {
        public const string LockId = "m126_master_design_lock_v1";
        public const string Title = "M126 Master Design Lock";
        public const string MasterDesignPath = "Docs/HollowSoul_MasterDesignAndBetaRoadmap.md";
        public const string EnglishPdfPath = "output/pdf/HollowSoul_MasterDesignAndBetaRoadmap.pdf";
        public const string PolishPdfPath = "output/pdf/HollowSoul_MasterDesignAndBetaRoadmap_PL.pdf";
        public const string ReportMarkdownPath = "output/reports/m126_master_design_lock.md";
        public const string ReportJsonPath = "output/reports/m126_master_design_lock.json";

        private const long MinimumPdfSizeBytes = 8192;

        public static readonly string[] RequiredEvidencePaths =
        {
            MasterDesignPath,
            EnglishPdfPath,
            PolishPdfPath
        };

        public static readonly string[] RequiredReportPaths =
        {
            ReportMarkdownPath,
            ReportJsonPath
        };

        public static readonly string[] RequiredSectors =
        {
            "### 5.1 Core Fantasy",
            "### 5.2 Narrative And World Premise",
            "### 5.3 Player Controls",
            "### 5.4 HUD And UI",
            "### 5.5 Combat",
            "### 5.6 Currencies",
            "### 5.7 Items, Cards, And Rewards",
            "### 5.8 Chests And Optional Risk",
            "### 5.9 Rooms, Levels, Hubs, And Worlds",
            "### 5.10 Biomes",
            "### 5.11 NPCs And Companions",
            "### 5.12 Enemies And Bosses",
            "### 5.13 Challenges And Achievements",
            "### 5.14 Art, Pipeline, And Production Roles"
        };

        public static readonly string[] RequiredRubricFields =
        {
            "Current Status",
            "Beta Value",
            "Full-Game Value",
            "Cost",
            "Risk",
            "Recommendation",
            "Target"
        };

        public static readonly string[] RequiredRecommendationCategories =
        {
            "Beta Core",
            "Beta Update",
            "Post-Beta Backlog",
            "Prototype Later",
            "Cut/Defer"
        };

        public static readonly string[] RequiredSourceReferences =
        {
            "Docs/HollowSoul_GameDesignFoundation_GDD_V1.md",
            "Docs/HollowSoul_6_12_Month_Roadmap_Team_Capacity_Plan.md",
            "Docs/Milestone58BetaRewardEconomyChestBalance.md",
            "Docs/Milestone63BetaContentSelectionLock.md",
            "Docs/Milestone64VerticalSliceBetaLockGate.md",
            "M69-M78",
            "WholeGameAuditRunner.cs"
        };

        private static readonly string[] RequiredScopeSections =
        {
            "## 7. Beta Scope Decision",
            "### Beta Core",
            "### Beta Exclusions",
            "## 10. Full-Vision Backlog",
            "## 11. Cut Or Deferred Without Active Design",
            "## 12. Review Checklist Before Runtime Work",
            "## 13. Source Coverage"
        };

        private static readonly string[] PlaceholderIdeas =
        {
            "Placeholder identity \"Agent Tampon I / Federation XYZ\"",
            "Placeholder X item slots",
            "Placeholder world slots X",
            "Important NPC category",
            "Support NPC category",
            "Boss placeholder list X"
        };

        private static readonly string[] RuntimeGuardPhrases =
        {
            "It is not an implementation changelist",
            "before runtime changes",
            "Does the task strengthen the Ship-Soul Loop?"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 126 Master Design Lock")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
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

        public static Milestone126MasterDesignLockReport BuildReport()
        {
            var checks = new List<Milestone126MasterDesignLockCheck>();
            foreach (var path in RequiredEvidencePaths)
            {
                AddCheck(
                    checks,
                    $"evidence:{Path.GetFileName(path)}",
                    "Evidence",
                    File.Exists(path),
                    File.Exists(path) ? $"Found `{path}`." : $"Missing `{path}`.");
            }

            AddPdfCheck(checks, EnglishPdfPath, "English PDF");
            AddPdfCheck(checks, PolishPdfPath, "Polish PDF");
            AddMasterDocumentChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone126MasterDesignLockReport
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

        public static string ToMarkdown(Milestone126MasterDesignLockReport report)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# M126 Master Design Lock Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Runtime scope: no gameplay, prefab, scene, balance, or content-system changes are part of M126.");
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
            foreach (var check in report.checks ?? Array.Empty<Milestone126MasterDesignLockCheck>())
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
            builder.AppendLine("M127 HUD + Run Readability Pass may begin only after this lock is reviewed and accepted.");
            return builder.ToString();
        }

        private static void AddMasterDocumentChecks(List<Milestone126MasterDesignLockCheck> checks)
        {
            if (!File.Exists(MasterDesignPath))
            {
                AddCheck(checks, "master-doc:readable", "Master Document", false, $"Missing `{MasterDesignPath}`.");
                return;
            }

            var markdown = File.ReadAllText(MasterDesignPath);
            AddCheck(checks, "master-doc:readable", "Master Document", markdown.Length > 4096, $"{markdown.Length} characters read.");
            AddContainsAllCheck(checks, markdown, "master-doc:sectors", "Design Sectors", RequiredSectors);
            AddContainsAllCheck(checks, markdown, "master-doc:rubric", "Idea Evaluation", RequiredRubricFields);
            AddContainsAllCheck(checks, markdown, "master-doc:recommendations", "Idea Evaluation", RequiredRecommendationCategories);
            AddContainsAllCheck(checks, markdown, "master-doc:scope", "Beta Scope", RequiredScopeSections);
            AddContainsAllCheck(checks, markdown, "master-doc:sources", "Cross References", RequiredSourceReferences);
            AddContainsAllCheck(checks, markdown, "master-doc:runtime-guard", "Runtime Guard", RuntimeGuardPhrases);
            AddMilestoneRoadmapCheck(checks, markdown);
            AddPlaceholderClassificationCheck(checks, markdown);
        }

        private static void AddPdfCheck(List<Milestone126MasterDesignLockCheck> checks, string path, string label)
        {
            if (!File.Exists(path))
            {
                AddCheck(checks, $"pdf:{label}", "PDF Evidence", false, $"Missing `{path}`.");
                return;
            }

            var bytes = File.ReadAllBytes(path);
            var headerValid = bytes.Length >= 5 &&
                              bytes[0] == '%' &&
                              bytes[1] == 'P' &&
                              bytes[2] == 'D' &&
                              bytes[3] == 'F' &&
                              bytes[4] == '-';
            var ascii = Encoding.ASCII.GetString(bytes);
            var pageMarkers = CountOccurrences(ascii, "/Type /Page") + CountOccurrences(ascii, "/Page ");
            var passed = headerValid && bytes.LongLength >= MinimumPdfSizeBytes && pageMarkers > 0;
            AddCheck(
                checks,
                $"pdf:{label}",
                "PDF Evidence",
                passed,
                $"{label} `{path}` size={bytes.LongLength} bytes, header={(headerValid ? "valid" : "invalid")}, page markers={pageMarkers}.");
        }

        private static void AddContainsAllCheck(
            List<Milestone126MasterDesignLockCheck> checks,
            string text,
            string id,
            string category,
            IReadOnlyList<string> required)
        {
            var missing = required
                .Where(value => text.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
                .ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? $"Found {required.Count} required entries." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddMilestoneRoadmapCheck(List<Milestone126MasterDesignLockCheck> checks, string markdown)
        {
            var missing = Enumerable.Range(126, 10)
                .Select(milestone => $"### M{milestone}:")
                .Where(marker => markdown.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0)
                .ToArray();
            AddCheck(
                checks,
                "master-doc:m126-m135-roadmap",
                "Milestone Roadmap",
                missing.Length == 0,
                missing.Length == 0 ? "Found M126-M135 roadmap entries." : $"Missing: {string.Join(", ", missing)}.");
        }

        private static void AddPlaceholderClassificationCheck(List<Milestone126MasterDesignLockCheck> checks, string markdown)
        {
            var lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var missing = new List<string>();
            var unclassified = new List<string>();

            foreach (var idea in PlaceholderIdeas)
            {
                var line = lines.FirstOrDefault(candidate => candidate.IndexOf(idea, StringComparison.OrdinalIgnoreCase) >= 0);
                if (string.IsNullOrWhiteSpace(line))
                {
                    missing.Add(idea);
                    continue;
                }

                if (line.IndexOf("Cut/Defer", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    unclassified.Add(idea);
                }
            }

            var passed = missing.Count == 0 && unclassified.Count == 0;
            var detail = passed
                ? "All placeholder ideas are explicitly cut/deferred rather than active implementation tasks."
                : $"Missing: {string.Join(", ", missing)}. Not cut/deferred: {string.Join(", ", unclassified)}.";
            AddCheck(checks, "master-doc:placeholder-classification", "Scope Control", passed, detail);
        }

        private static void AddCheck(
            List<Milestone126MasterDesignLockCheck> checks,
            string id,
            string category,
            bool passed,
            string detail)
        {
            checks.Add(new Milestone126MasterDesignLockCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var index = 0;
            while (index < text.Length)
            {
                var found = text.IndexOf(value, index, StringComparison.Ordinal);
                if (found < 0)
                {
                    return count;
                }

                count++;
                index = found + value.Length;
            }

            return count;
        }
    }
}
