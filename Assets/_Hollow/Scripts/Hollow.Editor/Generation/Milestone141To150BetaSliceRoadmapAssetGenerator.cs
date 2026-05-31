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
    public sealed class Milestone141To150BetaSliceRoadmapReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public string targetOutcome;
        public bool betaSliceCandidateByM150;
        public bool fullBetaByM150;
        public int plannedMilestoneCount;
        public string[] failures = Array.Empty<string>();
        public Milestone141To150BetaSliceMilestone[] milestones = Array.Empty<Milestone141To150BetaSliceMilestone>();

        public void Recalculate()
        {
            milestones ??= Array.Empty<Milestone141To150BetaSliceMilestone>();
            plannedMilestoneCount = milestones.Length;
            var failuresList = new List<string>();
            var expected = Enumerable.Range(141, 10).ToArray();
            var actual = milestones.Select(milestone => milestone != null ? milestone.milestone : 0).ToArray();
            if (!expected.SequenceEqual(actual))
            {
                failuresList.Add("Roadmap must define exactly M141-M150 in order.");
            }

            if (!betaSliceCandidateByM150)
            {
                failuresList.Add("M150 must target an internal beta-slice candidate.");
            }

            if (fullBetaByM150)
            {
                failuresList.Add("M150 must not claim full beta readiness.");
            }

            foreach (var milestone in milestones)
            {
                if (milestone == null)
                {
                    failuresList.Add("Roadmap contains a null milestone entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(milestone.title) ||
                    string.IsNullOrWhiteSpace(milestone.focus) ||
                    string.IsNullOrWhiteSpace(milestone.passGate) ||
                    milestone.work == null ||
                    milestone.work.Length == 0)
                {
                    failuresList.Add($"M{milestone.milestone} is missing title, focus, work, or pass gate.");
                }
            }

            failures = failuresList.ToArray();
        }
    }

    [Serializable]
    public sealed class Milestone141To150BetaSliceMilestone
    {
        public int milestone;
        public string title;
        public string focus;
        public string[] work = Array.Empty<string>();
        public string passGate;
        public string outcome;
        public string dependency;
    }

    public static class Milestone141To150BetaSliceRoadmapAssetGenerator
    {
        public const string LockId = "m141_m150_beta_slice_roadmap_v1";
        public const string Title = "M141-M150 Roadmap: Build-Real Stability To Beta Slice";
        public const string TargetOutcome = "M150 reaches Internal Beta Slice Candidate, not full beta.";
        public const string DocsPath = "Docs/Milestone141To150BetaSliceRoadmap.md";
        public const string ReportJsonPath = "output/reports/m141_m150_beta_slice_roadmap.json";
        public const string ReportMarkdownPath = "output/reports/m141_m150_beta_slice_roadmap.md";

        [MenuItem("Hollow/Generation/Generate Milestone 141-150 Beta Slice Roadmap")]
        public static void Generate()
        {
            var report = BuildReport();
            WriteReports(report);
            Debug.Log($"Generated M141-M150 beta slice roadmap. Report: {ReportMarkdownPath}");
        }

        public static Milestone141To150BetaSliceRoadmapReport BuildReport()
        {
            var report = new Milestone141To150BetaSliceRoadmapReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                targetOutcome = TargetOutcome,
                betaSliceCandidateByM150 = true,
                fullBetaByM150 = false,
                milestones = BuildMilestones()
            };
            report.Recalculate();
            return report;
        }

        public static bool ValidateReport(Milestone141To150BetaSliceRoadmapReport report, out string detail)
        {
            report?.Recalculate();
            if (report == null)
            {
                detail = "M141-M150 roadmap report is missing.";
                return false;
            }

            if (report.failures != null && report.failures.Length > 0)
            {
                detail = string.Join("; ", report.failures);
                return false;
            }

            var hasBuildReal = Contains(report, 141, "M140") && Contains(report, 141, "Windows");
            var hasPoolClosure = Contains(report, 142, "cold") && Contains(report, 142, "hard instantiates");
            var hasProjectile = Contains(report, 143, "projectile-heavy");
            var hasAiNav = Contains(report, 144, "AI") && Contains(report, 144, "Nav");
            var hasRecovery = Contains(report, 145, "save") && Contains(report, 145, "restore");
            var hasVisuals = Contains(report, 146, "visual") && Contains(report, 146, "render");
            var hasContentLock = Contains(report, 147, "content") && Contains(report, 147, "beta-slice");
            var hasBalance = Contains(report, 148, "balance") && Contains(report, 148, "feel");
            var hasQaGate = Contains(report, 149, "QA") && Contains(report, 149, "gate");
            var hasCandidate = Contains(report, 150, "Internal Beta Slice Candidate");

            if (!hasBuildReal ||
                !hasPoolClosure ||
                !hasProjectile ||
                !hasAiNav ||
                !hasRecovery ||
                !hasVisuals ||
                !hasContentLock ||
                !hasBalance ||
                !hasQaGate ||
                !hasCandidate)
            {
                detail = "Roadmap is missing one or more required beta-slice gates across M141-M150.";
                return false;
            }

            detail = "M141-M150 roadmap is ordered, decision-complete, and targets beta slice rather than full beta.";
            return true;
        }

        public static void WriteReports(Milestone141To150BetaSliceRoadmapReport report)
        {
            WriteText(ReportJsonPath, JsonUtility.ToJson(report, true));
            var markdown = ToMarkdown(report);
            WriteText(ReportMarkdownPath, markdown);
            WriteText(DocsPath, markdown);
        }

        public static string ToMarkdown(Milestone141To150BetaSliceRoadmapReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M141-M150 Roadmap: From Build-Real Stability To Beta Slice");
            builder.AppendLine();
            builder.AppendLine($"- Lock: `{report?.lockId ?? LockId}`");
            builder.AppendLine($"- Target outcome: {report?.targetOutcome ?? TargetOutcome}");
            builder.AppendLine($"- Beta slice by M150: `{(report?.betaSliceCandidateByM150 == true ? "yes" : "no")}`");
            builder.AppendLine($"- Full beta by M150: `{(report?.fullBetaByM150 == true ? "yes" : "no")}`");
            builder.AppendLine();
            builder.AppendLine("M150 is an internal beta-slice candidate, not a full beta promise. Full beta remains M151+ unless broader content, progression, UX, audio, settings, accessibility, crash reporting, and external-test readiness are also complete.");
            builder.AppendLine();

            foreach (var milestone in report?.milestones ?? Array.Empty<Milestone141To150BetaSliceMilestone>())
            {
                builder.AppendLine($"## M{milestone.milestone}: {milestone.title}");
                builder.AppendLine();
                builder.AppendLine($"Focus: {milestone.focus}");
                builder.AppendLine();
                builder.AppendLine("Work:");
                foreach (var item in milestone.work ?? Array.Empty<string>())
                {
                    builder.AppendLine($"- {item}");
                }

                builder.AppendLine();
                builder.AppendLine($"Pass gate: {milestone.passGate}");
                builder.AppendLine($"Outcome: {milestone.outcome}");
                builder.AppendLine($"Dependency: {milestone.dependency}");
                builder.AppendLine();
            }

            if (report?.failures != null && report.failures.Length > 0)
            {
                builder.AppendLine("## Roadmap Validation Failures");
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            return builder.ToString();
        }

        private static Milestone141To150BetaSliceMilestone[] BuildMilestones()
        {
            return new[]
            {
                M(141, "M140 Gate Closure And Truth Cleanup", "Make M140 trustworthy and passing on macOS, with Windows artifact flow ready.",
                    new[]
                    {
                        "Finish valid gameplay screenshots, render/FPS capture, missing-script failures, pool miss attribution, and M138/M139 integration.",
                        "Re-run macOS Apple Silicon development and release-smoke gates.",
                        "Document or import Windows player artifact requirements."
                    },
                    "macOS M140 passes except Windows environment/artifact status; no fake gameplay screenshots; no hidden player-log script warnings.",
                    "Build-real telemetry becomes the trusted source of performance and visual truth.",
                    "M140 implemented and rerunnable."),
                M(142, "Cold Miss And Pool Warm Closure", "Remove branch/reward/boss cold-cache misses and post-warmup hard instantiates.",
                    new[]
                    {
                        "Use M139/M140 miss-key reports to warm exact VFX, audio, projectile, pickup, reward, portal, and generated keys.",
                        "Extend branch-load preload coverage for reward rooms, boss rooms, special rooms, and return traversal.",
                        "Keep boss enemies unpooled unless a measured spike proves pooling is necessary."
                    },
                    "Normal traversal after branch load has 0 cold misses and 0 runtime hard instantiates, except documented boss/unpooled exceptions.",
                    "Branch traversal is seamless because runtime content is warm before gameplay reveal.",
                    "M141 truth reports expose exact miss keys."),
                M(143, "Projectile-Heavy Combat Performance Pass", "Fix the first real performance cliff.",
                    new[]
                    {
                        "Profile projectile-heavy stress separately from harness allocations.",
                        "Optimize projectile collision queries, lifetime/update paths, ranged fire cadence, pooled reset, and VFX/audio spam.",
                        "Add projectile counters for active projectiles, collision checks, hits, returns, pool misses, and projectile update ms."
                    },
                    "Projectile-heavy M138/M140 scenario returns to stable 60 FPS p95 in trusted player capture.",
                    "Projectile rooms become a known budgeted stress case instead of a frame cliff.",
                    "M142 pool warming removes false-positive projectile misses."),
                M(144, "AI/Nav Scale Finalization", "Make crowded fights stable without making enemies feel asleep.",
                    new[]
                    {
                        "Finalize central AI think budget and stagger policy.",
                        "Tune LOD degradation for offscreen, far, waiting, and add enemies.",
                        "Verify NavMesh solve budget, deferred path retry, avoidance tiers, and boss/add priority."
                    },
                    "30-enemy and boss-plus-adds scenarios have no synchronized AI/Nav spikes.",
                    "Crowded combat scales predictably while visible threats remain responsive.",
                    "M143 projectile pressure no longer hides AI/Nav cost."),
                M(145, "Save/Load, Branch Restore, And Failure Recovery", "Make the beta slice resilient.",
                    new[]
                    {
                        "Validate fresh run, continue run, snapshot restore, branch abandon/re-enter, boss room restore, reward room restore, and next-branch transition.",
                        "Add corrupted or old snapshot fallback behavior where needed.",
                        "Ensure loading screens, input locks, cache invalidation, and pool ownership recover cleanly after failure."
                    },
                    "No broken saves, no stuck loading/input state, no stale branch/pool state after restore.",
                    "The slice survives interruption, restore, and branch transitions without developer repair.",
                    "M141-M144 gates are stable enough to test restore behavior honestly."),
                M(146, "Visual Readability And Render Budget Polish", "Make the game look intentionally good under budget.",
                    new[]
                    {
                        "Lock render profiles for macOS, Windows, and dev.",
                        "Audit lighting after branch load, material first-use misses, shadows, projectile visibility, rewards, enemy silhouettes, HUD, and minimap contrast.",
                        "Add screenshot review sheets from M140 scenarios."
                    },
                    "Automated screenshots pass and manual visual review confirms combat, boss, rewards, rooms, HUD, and minimap read clearly.",
                    "The beta slice looks deliberate while staying inside the render budget.",
                    "M145 restore paths no longer create misleading visual states."),
                M(147, "Beta Slice Content Lock", "Define the exact playable beta-slice path.",
                    new[]
                    {
                        "Choose the beta-slice branch/floor: biome, room types, enemy families, boss, reward room, special room, hub return, and next-branch handoff.",
                        "Freeze content scope for the slice.",
                        "Add content-lock validation for missing prefabs, materials, NavMesh, catalog entries, and reward definitions."
                    },
                    "One deterministic beta-slice route is content complete and all required assets resolve in player builds.",
                    "The team has one scoped path to polish instead of an expanding target.",
                    "M146 confirms the selected content is visually readable."),
                M(148, "Balance And Feel Pass", "Make the slice fun, not just functional.",
                    new[]
                    {
                        "Tune enemy HP/damage, player damage, weapon cadence, projectile density, rewards, coin/soul economy, chest risk/reward, and boss difficulty.",
                        "Add deterministic balance smoke captures for normal, low-skill, and high-pressure routes.",
                        "Preserve performance budgets while tuning."
                    },
                    "Internal playtest checklist says the slice is readable, fair, paced, and worth replaying.",
                    "The slice has a coherent difficulty and reward arc.",
                    "M147 content scope is frozen."),
                M(149, "QA Automation And Bug Triage Gate", "Turn repeated checking into a routine.",
                    new[]
                    {
                        "Add one-click Beta Slice QA Gate chaining compile, EditMode, PlayMode smoke, M138, M139 smoke, M140 macOS, and report summary.",
                        "Create severity buckets: blocker, beta-slice blocker, polish, and later.",
                        "Add a latest-report dashboard linking failures to artifacts, screenshots, and logs."
                    },
                    "QA gate produces a clear pass/fail report with actionable failure reasons.",
                    "Every candidate build has one obvious go/no-go report.",
                    "M148 establishes what the QA gate must preserve."),
                M(150, "Internal Beta Slice Candidate", "Package a playable internal beta-slice build.",
                    new[]
                    {
                        "Build macOS and Windows candidate artifacts.",
                        "Include boot loading, branch loading, seamless traversal, boss room, reward room, save/continue, and return-to-hub.",
                        "Produce release notes, known issues, QA checklist, and performance report."
                    },
                    "Internal testers can play the slice from boot to boss/reward/return without developer intervention.",
                    "Internal Beta Slice Candidate is ready; full beta remains a later M151+ expansion target.",
                    "M149 QA gate is passing or has only accepted non-blocking known issues.")
            };
        }

        private static Milestone141To150BetaSliceMilestone M(
            int number,
            string title,
            string focus,
            string[] work,
            string passGate,
            string outcome,
            string dependency)
        {
            return new Milestone141To150BetaSliceMilestone
            {
                milestone = number,
                title = title,
                focus = focus,
                work = work ?? Array.Empty<string>(),
                passGate = passGate ?? string.Empty,
                outcome = outcome ?? string.Empty,
                dependency = dependency ?? string.Empty
            };
        }

        private static bool Contains(Milestone141To150BetaSliceRoadmapReport report, int milestoneNumber, string text)
        {
            var milestone = report.milestones.FirstOrDefault(item => item != null && item.milestone == milestoneNumber);
            if (milestone == null)
            {
                return false;
            }

            var haystack = $"{milestone.title} {milestone.focus} {string.Join(" ", milestone.work ?? Array.Empty<string>())} {milestone.passGate} {milestone.outcome} {milestone.dependency}";
            return haystack.IndexOf(text ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void WriteText(string path, string text)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, text ?? string.Empty);
        }
    }
}
