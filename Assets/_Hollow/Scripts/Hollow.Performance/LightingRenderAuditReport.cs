using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Performance
{
    public static class LightingRenderAuditMode
    {
        public const string Smoke = "smoke";
        public const string Full = "full";
    }

    [Serializable]
    public sealed class LightingRenderAuditOptions
    {
        public string mode = LightingRenderAuditMode.Smoke;
        public string outputRoot = LightingRenderAuditReportGenerator.DefaultReportDirectory;
        public string platformId = M140BuildRealReportGenerator.CurrentRuntimePlatformId();
        public string[] scenarioIds = LightingRenderAuditScenarioPolicy.SmokeScenarioIds;
        public bool autoExit;
        public bool writeReports = true;
        public int targetFrameRate = 60;

        public string ResolvedOutputRoot => string.IsNullOrWhiteSpace(outputRoot) ? LightingRenderAuditReportGenerator.DefaultReportDirectory : outputRoot;

        public string JsonReportPath => Path.Combine(ResolvedOutputRoot, LightingRenderAuditReportGenerator.DefaultJsonFileName);

        public string MarkdownReportPath => Path.Combine(ResolvedOutputRoot, LightingRenderAuditReportGenerator.DefaultMarkdownFileName);

        public bool IsFull => string.Equals(mode, LightingRenderAuditMode.Full, StringComparison.OrdinalIgnoreCase);

        public static LightingRenderAuditOptions Smoke(string outputRoot = null)
        {
            return new LightingRenderAuditOptions
            {
                mode = LightingRenderAuditMode.Smoke,
                outputRoot = string.IsNullOrWhiteSpace(outputRoot) ? LightingRenderAuditReportGenerator.DefaultReportDirectory : outputRoot,
                platformId = M140BuildRealReportGenerator.CurrentRuntimePlatformId(),
                scenarioIds = LightingRenderAuditScenarioPolicy.SmokeScenarioIds,
                autoExit = false,
                writeReports = true,
                targetFrameRate = 60
            };
        }

        public static LightingRenderAuditOptions Full(string outputRoot = null)
        {
            return new LightingRenderAuditOptions
            {
                mode = LightingRenderAuditMode.Full,
                outputRoot = string.IsNullOrWhiteSpace(outputRoot) ? LightingRenderAuditReportGenerator.DefaultReportDirectory : outputRoot,
                platformId = M140BuildRealReportGenerator.CurrentRuntimePlatformId(),
                scenarioIds = LightingRenderAuditScenarioPolicy.AllScenarioIds,
                autoExit = false,
                writeReports = true,
                targetFrameRate = 60
            };
        }

        public static bool TryParse(string[] args, out LightingRenderAuditOptions options)
        {
            options = Smoke();
            if (args == null || !args.Any(arg => string.Equals(arg, "--hollow-lighting-audit", StringComparison.Ordinal)))
            {
                return false;
            }

            options.autoExit = args.Any(arg => string.Equals(arg, "--hollow-lighting-audit-auto-exit", StringComparison.Ordinal));
            string scenarioList = null;
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index] ?? string.Empty;
                if (TryReadValue(arg, "--hollow-lighting-audit-output", args, ref index, out var output))
                {
                    options.outputRoot = output;
                }
                else if (TryReadValue(arg, "--hollow-lighting-audit-platform", args, ref index, out var platform))
                {
                    options.platformId = platform;
                }
                else if (TryReadValue(arg, "--hollow-lighting-audit-mode", args, ref index, out var mode))
                {
                    options.mode = string.Equals(mode, LightingRenderAuditMode.Full, StringComparison.OrdinalIgnoreCase)
                        ? LightingRenderAuditMode.Full
                        : LightingRenderAuditMode.Smoke;
                }
                else if (TryReadValue(arg, "--hollow-lighting-audit-scenarios", args, ref index, out var scenarios))
                {
                    scenarioList = scenarios;
                }
                else if (TryReadValue(arg, "--hollow-lighting-audit-fps-cap", args, ref index, out var fpsText) &&
                         int.TryParse(fpsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fps))
                {
                    options.targetFrameRate = Mathf.Max(1, fps);
                }
            }

            options.scenarioIds = ParseScenarioList(scenarioList, options.IsFull);
            if (options.scenarioIds == null || options.scenarioIds.Length == 0)
            {
                options.scenarioIds = options.IsFull ? LightingRenderAuditScenarioPolicy.AllScenarioIds : LightingRenderAuditScenarioPolicy.SmokeScenarioIds;
            }

            return true;
        }

        private static string[] ParseScenarioList(string value, bool fullMode)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "default", StringComparison.OrdinalIgnoreCase))
            {
                return fullMode ? LightingRenderAuditScenarioPolicy.AllScenarioIds : LightingRenderAuditScenarioPolicy.SmokeScenarioIds;
            }

            if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase) || string.Equals(value, LightingRenderAuditMode.Full, StringComparison.OrdinalIgnoreCase))
            {
                return LightingRenderAuditScenarioPolicy.AllScenarioIds;
            }

            if (string.Equals(value, LightingRenderAuditMode.Smoke, StringComparison.OrdinalIgnoreCase))
            {
                return LightingRenderAuditScenarioPolicy.SmokeScenarioIds;
            }

            return value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static bool TryReadValue(string arg, string key, string[] args, ref int index, out string value)
        {
            value = string.Empty;
            if (arg.StartsWith(key + "=", StringComparison.Ordinal))
            {
                value = arg.Substring(key.Length + 1).Trim('"');
                return true;
            }

            if (string.Equals(arg, key, StringComparison.Ordinal) && index + 1 < args.Length)
            {
                index++;
                value = (args[index] ?? string.Empty).Trim('"');
                return true;
            }

            return false;
        }
    }

    [Serializable]
    public sealed class LightingRenderAuditScenarioDefinition
    {
        public string id;
        public string displayName;
        public string biomeId;
        public int propLightCount;
        public int dynamicEffectLightCount;
        public int enemyCount;
        public int projectileCount;
        public int vfxCount;
        public bool transitionSweep;
        public bool overBudgetLights;
    }

    public static class LightingRenderAuditScenarioPolicy
    {
        public const string HollowThresholdBaseline = "biome_hollow_threshold_baseline";
        public const string VerdantProps = "biome_verdant_props";
        public const string AshenProjectiles = "biome_ashen_projectiles";
        public const string SunkenManyLights = "biome_sunken_many_lights";
        public const string RustManyEnemies = "biome_rust_many_enemies";
        public const string TransitionSweep = "biome_transition_sweep";

        public static readonly string[] SmokeScenarioIds =
        {
            HollowThresholdBaseline,
            VerdantProps,
            TransitionSweep
        };

        public static readonly string[] AllScenarioIds =
        {
            HollowThresholdBaseline,
            VerdantProps,
            AshenProjectiles,
            SunkenManyLights,
            RustManyEnemies,
            TransitionSweep
        };

        public static readonly LightingRenderAuditScenarioDefinition[] Manifest =
        {
            Scenario(HollowThresholdBaseline, "Hollow Threshold Baseline", RoomBiomeIds.HollowThreshold, propLights: 1, effects: 0, enemies: 0, projectiles: 0, vfx: 0),
            Scenario(VerdantProps, "Verdant Props Near Budget", RoomBiomeIds.VerdantRuins, propLights: 5, effects: 1, enemies: 0, projectiles: 0, vfx: 2),
            Scenario(AshenProjectiles, "Ashen Projectile Pressure", RoomBiomeIds.CorruptedAshenShrine, propLights: 2, effects: 3, enemies: 6, projectiles: 18, vfx: 8),
            Scenario(SunkenManyLights, "Sunken Many Lights Budget", RoomBiomeIds.SunkenCartouche, propLights: 9, effects: 5, enemies: 0, projectiles: 0, vfx: 4, overBudget: true),
            Scenario(RustManyEnemies, "Rust Choir Enemy Crowd", RoomBiomeIds.RustChoir, propLights: 4, effects: 2, enemies: 20, projectiles: 4, vfx: 4),
            Scenario(TransitionSweep, "Biome Transition Sweep", RoomBiomeIds.HollowThreshold, propLights: 3, effects: 1, enemies: 2, projectiles: 2, vfx: 2, transition: true)
        };

        public static LightingRenderAuditScenarioDefinition Find(string id)
        {
            return Manifest.FirstOrDefault(scenario => string.Equals(scenario.id, id, StringComparison.Ordinal));
        }

        private static LightingRenderAuditScenarioDefinition Scenario(
            string id,
            string displayName,
            string biomeId,
            int propLights,
            int effects,
            int enemies,
            int projectiles,
            int vfx,
            bool transition = false,
            bool overBudget = false)
        {
            return new LightingRenderAuditScenarioDefinition
            {
                id = id,
                displayName = displayName,
                biomeId = biomeId,
                propLightCount = propLights,
                dynamicEffectLightCount = effects,
                enemyCount = enemies,
                projectileCount = projectiles,
                vfxCount = vfx,
                transitionSweep = transition,
                overBudgetLights = overBudget
            };
        }
    }

    [Serializable]
    public sealed class LightingRenderAuditFrameSample
    {
        public int sampleIndex;
        public float elapsedSeconds;
        public double frameTimeMs;
        public double mainThreadMs;
        public double renderThreadMs;
        public double gpuFrameMs;
        public double gcAllocatedKb;
        public int activeLights;
        public int activeLocalLights;
        public int shadowedLights;
        public int activePropLights;
        public int activeDynamicEffectLights;
        public int activeEnemies;
        public int activeProjectiles;
        public int activeVfx;
        public int activeCameras;
        public int activeRenderers;
        public int activeParticleSystems;
    }

    [Serializable]
    public sealed class LightingRenderAuditScenarioSummary
    {
        public string scenarioId;
        public string displayName;
        public string biomeId;
        public string profileId;
        public bool passed;
        public string[] failures = Array.Empty<string>();
        public string screenshotPath;
        public M140VisualValidationSummary visual;
        public int sampleCount;
        public double frameP50Ms;
        public double frameP95Ms;
        public double frameMaxMs;
        public double mainThreadP95Ms;
        public double renderThreadP95Ms;
        public double gpuFrameP95Ms;
        public double gcAllocatedAverageKb;
        public int peakActiveLights;
        public int peakActiveLocalLights;
        public int peakShadowedLights;
        public int peakPropLights;
        public int peakDynamicEffectLights;
        public int peakEnemies;
        public int peakProjectiles;
        public int peakVfx;
        public int peakRenderers;
        public int peakParticleSystems;
        public int maxActiveLocalLights;
        public int maxShadowedLocalLights;
        public int maxPropLights;
        public int maxDynamicEffectLights;
        public string cameraBackgroundColor;
        public string ambientSkyColor;
        public string fogColor;
        public bool fogEnabled;
        public string note;
        public LightingRenderAuditFrameSample[] samples = Array.Empty<LightingRenderAuditFrameSample>();
    }

    [Serializable]
    public sealed class LightingRenderAuditReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public string mode;
        public string platformId;
        public string result;
        public bool passed;
        public string artifactRoot;
        public int scenarioCount;
        public string[] failures = Array.Empty<string>();
        public LightingRenderAuditScenarioSummary[] scenarios = Array.Empty<LightingRenderAuditScenarioSummary>();
    }

    public static class LightingRenderAuditReportGenerator
    {
        public const string LockId = "LightingRenderAudit";
        public const string Title = "Lighting Render Audit";
        public const string DefaultReportDirectory = "output/reports/lighting_render_audit";
        public const string DefaultJsonFileName = "lighting_render_audit.json";
        public const string DefaultMarkdownFileName = "lighting_render_audit.md";

        public static LightingRenderAuditReport BuildReport(LightingRenderAuditOptions options, IEnumerable<LightingRenderAuditScenarioSummary> scenarioSummaries)
        {
            options ??= LightingRenderAuditOptions.Smoke();
            var summaries = scenarioSummaries?.Where(summary => summary != null).ToArray() ?? Array.Empty<LightingRenderAuditScenarioSummary>();
            var failures = new List<string>();
            var required = options.IsFull ? LightingRenderAuditScenarioPolicy.AllScenarioIds : options.scenarioIds ?? LightingRenderAuditScenarioPolicy.SmokeScenarioIds;
            foreach (var scenarioId in required ?? Array.Empty<string>())
            {
                if (!summaries.Any(summary => string.Equals(summary.scenarioId, scenarioId, StringComparison.Ordinal)))
                {
                    failures.Add($"Missing lighting audit scenario `{scenarioId}`.");
                }
            }

            foreach (var summary in summaries)
            {
                var scenarioFailures = ValidateScenario(summary).ToArray();
                summary.failures = scenarioFailures;
                summary.passed = scenarioFailures.Length == 0;
                foreach (var failure in scenarioFailures)
                {
                    failures.Add($"{summary.scenarioId}: {failure}");
                }
            }

            var passed = failures.Count == 0;
            return new LightingRenderAuditReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                mode = options.mode ?? LightingRenderAuditMode.Smoke,
                platformId = options.platformId ?? string.Empty,
                result = passed ? M140GateResult.Passed : M140GateResult.Failed,
                passed = passed,
                artifactRoot = options.ResolvedOutputRoot,
                scenarioCount = summaries.Length,
                failures = failures.ToArray(),
                scenarios = summaries
            };
        }

        public static void WriteReport(LightingRenderAuditReport report, string jsonPath = null, string markdownPath = null)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            jsonPath = string.IsNullOrWhiteSpace(jsonPath) ? Path.Combine(DefaultReportDirectory, DefaultJsonFileName) : jsonPath;
            markdownPath = string.IsNullOrWhiteSpace(markdownPath) ? Path.Combine(DefaultReportDirectory, DefaultMarkdownFileName) : markdownPath;
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath) ?? DefaultReportDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(markdownPath) ?? DefaultReportDirectory);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(markdownPath, ToMarkdown(report));
        }

        public static string ToMarkdown(LightingRenderAuditReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Lighting Render Audit");
            builder.AppendLine();
            builder.AppendLine($"- Result: `{report?.result ?? M140GateResult.Failed}`");
            builder.AppendLine($"- Mode: `{report?.mode ?? LightingRenderAuditMode.Smoke}`");
            builder.AppendLine($"- Platform: `{report?.platformId ?? string.Empty}`");
            builder.AppendLine($"- Generated: `{report?.generatedAtUtc ?? string.Empty}`");
            builder.AppendLine($"- Scenarios: `{report?.scenarioCount ?? 0}`");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Biome | Frame p95 | GPU p95 | Local lights | Shadowed | Props | Effects | Enemies | Projectiles | Renderers | Screenshot | Result |");
            builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |");
            foreach (var scenario in report?.scenarios ?? Array.Empty<LightingRenderAuditScenarioSummary>())
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "| `{0}` | `{1}` | {2:0.00}ms | {3:0.00}ms | {4}/{5} | {6}/{7} | {8}/{9} | {10}/{11} | {12} | {13} | {14} | `{15}` | {16} |",
                    scenario.scenarioId,
                    scenario.biomeId,
                    scenario.frameP95Ms,
                    scenario.gpuFrameP95Ms,
                    scenario.peakActiveLocalLights,
                    scenario.maxActiveLocalLights,
                    scenario.peakShadowedLights,
                    scenario.maxShadowedLocalLights,
                    scenario.peakPropLights,
                    scenario.maxPropLights,
                    scenario.peakDynamicEffectLights,
                    scenario.maxDynamicEffectLights,
                    scenario.peakEnemies,
                    scenario.peakProjectiles,
                    scenario.peakRenderers,
                    scenario.screenshotPath,
                    scenario.passed ? "PASS" : "FAIL"));
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            var failures = report?.failures ?? Array.Empty<string>();
            builder.AppendLine(failures.Length == 0 ? "- None" : string.Join("\n", failures.Select(failure => "- " + failure)));
            builder.AppendLine();
            builder.AppendLine("## Scenario Notes");
            foreach (var scenario in report?.scenarios ?? Array.Empty<LightingRenderAuditScenarioSummary>())
            {
                builder.AppendLine($"- `{scenario.scenarioId}`: {scenario.note ?? string.Empty}");
            }

            return builder.ToString();
        }

        public static IEnumerable<string> ValidateScenario(LightingRenderAuditScenarioSummary summary)
        {
            if (summary == null)
            {
                yield return "Scenario summary is missing.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(summary.biomeId) || string.IsNullOrWhiteSpace(summary.profileId))
            {
                yield return "Biome/profile evidence is missing.";
            }

            if (summary.sampleCount <= 0)
            {
                yield return "No frame samples were recorded.";
            }

            if (summary.maxActiveLocalLights > 0 && summary.peakActiveLocalLights > summary.maxActiveLocalLights)
            {
                yield return $"Local light budget exceeded {summary.peakActiveLocalLights}/{summary.maxActiveLocalLights}.";
            }

            if (summary.maxShadowedLocalLights >= 0 && summary.peakShadowedLights > summary.maxShadowedLocalLights)
            {
                yield return $"Shadowed light budget exceeded {summary.peakShadowedLights}/{summary.maxShadowedLocalLights}.";
            }

            if (summary.maxPropLights >= 0 && summary.peakPropLights > summary.maxPropLights)
            {
                yield return $"Prop light budget exceeded {summary.peakPropLights}/{summary.maxPropLights}.";
            }

            if (summary.maxDynamicEffectLights >= 0 && summary.peakDynamicEffectLights > summary.maxDynamicEffectLights)
            {
                yield return $"Dynamic effect light budget exceeded {summary.peakDynamicEffectLights}/{summary.maxDynamicEffectLights}.";
            }

            if (summary.visual == null || !summary.visual.exists || !summary.visual.loadable)
            {
                yield return "Screenshot evidence is missing or not loadable.";
            }
            else if (!summary.visual.passed)
            {
                foreach (var failure in summary.visual.failures ?? Array.Empty<string>())
                {
                    yield return "Screenshot validation: " + failure;
                }
            }
        }

        public static double Percentile(IEnumerable<double> values, double percentile)
        {
            var sorted = values?.Where(value => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value)).OrderBy(value => value).ToArray() ?? Array.Empty<double>();
            if (sorted.Length == 0)
            {
                return 0d;
            }

            var index = Mathf.Clamp((int)Math.Ceiling(percentile * sorted.Length) - 1, 0, sorted.Length - 1);
            return sorted[index];
        }

        public static string FormatColor(Color color)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", Mathf.RoundToInt(color.r * 255f), Mathf.RoundToInt(color.g * 255f), Mathf.RoundToInt(color.b * 255f));
        }
    }
}
