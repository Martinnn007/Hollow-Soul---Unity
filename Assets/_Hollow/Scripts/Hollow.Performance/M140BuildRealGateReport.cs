using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Core.Diagnostics;
using Hollow.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Performance
{
    public static class M140BuildKind
    {
        public const string Development = "development";
        public const string ReleaseSmoke = "release-smoke";
    }

    public static class M140GateResult
    {
        public const string Passed = "Passed";
        public const string Failed = "Failed";
        public const string BlockedByEnvironment = "BlockedByEnvironment";
    }

    [Serializable]
    public sealed class M140BuiltPlayerCaptureOptions
    {
        public string outputRoot = M140BuildRealReportGenerator.DefaultReportDirectory;
        public string platformId = M140BuildRealReportGenerator.CurrentRuntimePlatformId();
        public string buildKind = M140BuildKind.Development;
        public string[] scenarioIds = M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds;
        public bool autoExit;
        public bool writeReports = true;
        public bool enforceTiming = true;
        public bool releaseSmoke;
        public int targetFrameRate = M140BuildRealReportGenerator.TargetFrameRate;

        public bool IsDevelopment => string.Equals(buildKind, M140BuildKind.Development, StringComparison.OrdinalIgnoreCase) && !releaseSmoke;

        public string JsonReportPath => Path.Combine(ResolvedOutputRoot, M140BuildRealReportGenerator.DefaultJsonFileName);

        public string MarkdownReportPath => Path.Combine(ResolvedOutputRoot, M140BuildRealReportGenerator.DefaultMarkdownFileName);

        public string ResolvedOutputRoot => string.IsNullOrWhiteSpace(outputRoot) ? M140BuildRealReportGenerator.DefaultReportDirectory : outputRoot;

        public static M140BuiltPlayerCaptureOptions Development(string outputRoot = null, string[] scenarios = null)
        {
            return new M140BuiltPlayerCaptureOptions
            {
                outputRoot = string.IsNullOrWhiteSpace(outputRoot) ? M140BuildRealReportGenerator.DefaultReportDirectory : outputRoot,
                platformId = M140BuildRealReportGenerator.CurrentRuntimePlatformId(),
                buildKind = M140BuildKind.Development,
                scenarioIds = scenarios ?? M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds,
                autoExit = false,
                writeReports = true,
                enforceTiming = true,
                releaseSmoke = false,
                targetFrameRate = M140BuildRealReportGenerator.TargetFrameRate
            };
        }

        public static M140BuiltPlayerCaptureOptions ReleaseSmoke(string outputRoot = null, string[] scenarios = null)
        {
            return new M140BuiltPlayerCaptureOptions
            {
                outputRoot = string.IsNullOrWhiteSpace(outputRoot) ? M140BuildRealReportGenerator.DefaultReportDirectory : outputRoot,
                platformId = M140BuildRealReportGenerator.CurrentRuntimePlatformId(),
                buildKind = M140BuildKind.ReleaseSmoke,
                scenarioIds = scenarios ?? M140BuildRealReportGenerator.RequiredReleaseSmokeScenarioIds,
                autoExit = false,
                writeReports = true,
                enforceTiming = false,
                releaseSmoke = true,
                targetFrameRate = M140BuildRealReportGenerator.TargetFrameRate
            };
        }

        public static bool TryParse(string[] args, out M140BuiltPlayerCaptureOptions options)
        {
            options = Development();
            if (args == null || !args.Any(arg => string.Equals(arg, "--hollow-m140-capture", StringComparison.Ordinal)))
            {
                return false;
            }

            options.autoExit = args.Any(arg => string.Equals(arg, "--hollow-m140-auto-exit", StringComparison.Ordinal));
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index] ?? string.Empty;
                if (TryReadValue(arg, "--hollow-m140-output", args, ref index, out var output))
                {
                    options.outputRoot = output;
                }
                else if (TryReadValue(arg, "--hollow-m140-platform", args, ref index, out var platform))
                {
                    options.platformId = platform;
                }
                else if (TryReadValue(arg, "--hollow-m140-build-kind", args, ref index, out var buildKind))
                {
                    options.buildKind = buildKind;
                    options.releaseSmoke = string.Equals(buildKind, M140BuildKind.ReleaseSmoke, StringComparison.OrdinalIgnoreCase);
                }
                else if (TryReadValue(arg, "--hollow-m140-scenarios", args, ref index, out var scenarios))
                {
                    options.scenarioIds = ParseScenarioList(scenarios, options.releaseSmoke);
                }
                else if (TryReadValue(arg, "--hollow-m140-fps-cap", args, ref index, out var fpsText) &&
                         int.TryParse(fpsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fps))
                {
                    options.targetFrameRate = Mathf.Max(1, fps);
                }
                else if (string.Equals(arg, "--hollow-m140-release-smoke", StringComparison.Ordinal))
                {
                    options.releaseSmoke = true;
                    options.buildKind = M140BuildKind.ReleaseSmoke;
                    if (options.scenarioIds == M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds)
                    {
                        options.scenarioIds = M140BuildRealReportGenerator.RequiredReleaseSmokeScenarioIds;
                    }
                }
            }

            options.scenarioIds = options.scenarioIds == null || options.scenarioIds.Length == 0
                ? ParseScenarioList("all", options.releaseSmoke)
                : options.scenarioIds;
            return true;
        }

        private static string[] ParseScenarioList(string value, bool releaseSmoke)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
            {
                return releaseSmoke
                    ? M140BuildRealReportGenerator.RequiredReleaseSmokeScenarioIds
                    : M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds;
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
    public sealed class M140VisualValidationSummary
    {
        public string screenshotPath;
        public bool exists;
        public bool loadable;
        public int width;
        public int height;
        public float averageLuminance;
        public float darkPixelRatio;
        public float brightPixelRatio;
        public float hotPinkPixelRatio;
        public bool passed;
        public string[] failures = Array.Empty<string>();
    }

    [Serializable]
    public sealed class M140PlayerLogValidationSummary
    {
        public string logPath;
        public bool exists;
        public int errorCount;
        public int shaderIssueCount;
        public int materialIssueCount;
        public int addressablesIssueCount;
        public int exceptionCount;
        public int missingScriptWarningCount;
        public int startupMissingScriptWarningCount;
        public int scenarioMissingScriptWarningCount;
        public int runtimeNavMeshFallbackWarningCount;
        public int startupRuntimeNavMeshFallbackWarningCount;
        public int scenarioRuntimeNavMeshFallbackWarningCount;
        public string[] issueLines = Array.Empty<string>();
        public string[] missingScriptWarningLines = Array.Empty<string>();
        public string[] missingScriptWarningContextLines = Array.Empty<string>();
        public string[] runtimeNavMeshFallbackWarningLines = Array.Empty<string>();
        public string[] runtimeNavMeshFallbackWarningContextLines = Array.Empty<string>();
        public bool passed;
        public string[] failures = Array.Empty<string>();
    }

    [Serializable]
    public sealed class M140RenderRuntimeSnapshot
    {
        public string unityVersion;
        public string platform;
        public string operatingSystem;
        public string processorType;
        public string graphicsDeviceName;
        public string graphicsDeviceType;
        public int targetFrameRate;
        public int vSyncCount;
        public string renderPipelineAssetName;
        public string shaderWarmupPolicy;
        public int shaderWarmAttempts;
        public int shaderWarmCollections;
        public int shaderWarmSuccesses;
        public int shaderWarmMisses;
        public float shaderWarmMaxMilliseconds;

        public static M140RenderRuntimeSnapshot Capture(M136PerformanceOperationSnapshot operations)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.name
                : QualitySettings.renderPipeline != null
                    ? QualitySettings.renderPipeline.name
                    : "Built-in/none";
            var processorType = SystemInfo.processorType;
            try
            {
                processorType = string.IsNullOrWhiteSpace(processorType)
                    ? System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
                    : $"{processorType} {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}";
            }
            catch
            {
                // Some Unity profiles may not expose RuntimeInformation; SystemInfo still gives us the device string.
            }

            return new M140RenderRuntimeSnapshot
            {
                unityVersion = Application.unityVersion,
                platform = M140BuildRealReportGenerator.CurrentRuntimePlatformId(),
                operatingSystem = SystemInfo.operatingSystem,
                processorType = processorType,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                targetFrameRate = Application.targetFrameRate,
                vSyncCount = QualitySettings.vSyncCount,
                renderPipelineAssetName = pipeline,
                shaderWarmupPolicy = "curated ShaderVariantCollection only",
                shaderWarmAttempts = operations.BootPreloadShaderWarmAttempts,
                shaderWarmCollections = operations.BootPreloadShaderWarmCollections,
                shaderWarmSuccesses = operations.BootPreloadShaderWarmSuccesses,
                shaderWarmMisses = operations.BootPreloadShaderWarmMisses,
                shaderWarmMaxMilliseconds = operations.BootPreloadShaderWarmMaxMilliseconds
            };
        }
    }

    [Serializable]
    public sealed class M140ScenarioSummary
    {
        public string scenarioId;
        public string displayName;
        public string platformId;
        public string buildKind;
        public bool developmentCapture;
        public bool timingAuthoritative;
        public string frameCadenceConfidence;
        public int rawSampleCount;
        public double frameP50Ms;
        public double frameP95Ms;
        public double frameMaxMs;
        public bool cpuWorkMetricSupported;
        public double cpuWorkP95Ms;
        public double cpuWorkMaxMs;
        public long gcMaxBytes;
        public int peakActiveEnemies;
        public int peakProjectiles;
        public int peakRenderers;
        public bool observedBranchSession;
        public bool observedCombatController;
        public bool observedBoss;
        public int runtimeNavMeshFallbacks;
        public int stressHarnessNavMeshBakes;
        public int normalTraversalColdCacheMissesAfterLoad;
        public int branchLiveRoomsBuilt;
        public int branchLiveRoomCacheHits;
        public int branchLiveRoomCacheMisses;
        public int normalTraversalRoomRebuildCalls;
        public int normalTraversalWarmCalls;
        public int hibernatedRoomActiveObjectLeaks;
        public int transitionCurtainMaxFramesAfterReady;
        public float transitionCurtainMaxVisibleMilliseconds;
        public int stagedRoomVisibleRendererFrames;
        public int normalTraversalRevealFrames;
        public int roomReadyBeforeRevealMaxFrames;
        public int poolWarmVisibleObjects;
        public int poolWarmRootActiveErrors;
        public int poolWarmActiveLeaks;
        public int presentationFallbackVisuals;
        public int roomEntryVfxBeforeReveal;
        public string cpuStageSummary;
        public string tacticalDirectorSummary;
        public string cacheMissAttributionSummary;
        public string preloadBuildCacheMissAttributionSummary;
        public string postLoadCacheMissAttributionSummary;
        public int projectileActivePeak;
        public int projectileSpawns;
        public int projectileReturns;
        public int projectileCollisionChecks;
        public int projectilePoolMisses;
        public int projectileHardInstantiates;
        public float projectileUpdateMaxMilliseconds;
        public int bootLoadingStarts;
        public int bootLoadingCompletions;
        public int branchLoadingCompletions;
        public int bossLoadingStarts;
        public int bossLoadingCompletions;
        public int shaderMaterialFirstUseMissesAfterLoad;
        public int staleEnemyStateFailures;
        public int staleRuntimePoolStateFailures;
        public int poolActiveLeaks;
        public int enemyPoolHardInstantiatesAfterWarmup;
        public int runtimePoolHardInstantiatesAfterWarmup;
        public int aiThinkFull;
        public int aiThinkReduced;
        public int aiThinkBackground;
        public int aiCommandReuses;
        public int aiScorerCalls;
        public int aiBehaviorGraphTicks;
        public int navPathRequests;
        public int navPathSolves;
        public int navPathDeferred;
        public float navPathMaxSolveMilliseconds;
        public int tacticalCrowdReservationSkips;
        public int tacticalCrowdCachedIntentReuses;
        public int tacticalCrowdSupportReservationBudgetUses;
        public int tacticalCrowdActiveThreatLimitMax;
        public int tacticalCrowdScorerSkips;
        public bool m138GatePassed;
        public bool m139GatePassed;
        public M140VisualValidationSummary visual;
        public bool passed;
        public string[] failures = Array.Empty<string>();
        public string note;
    }

    [Serializable]
    public sealed class M140BuildRealReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public string platformId;
        public string buildKind;
        public string result;
        public bool passed;
        public bool environmentBlocked;
        public int scenarioCount;
        public string artifactRoot;
        public string playerLogPath;
        public M140RenderRuntimeSnapshot renderRuntime;
        public M140PlayerLogValidationSummary playerLog;
        public M140ScenarioSummary[] scenarios = Array.Empty<M140ScenarioSummary>();
        public string[] failures = Array.Empty<string>();
    }

    public static class M140VisualScreenshotValidator
    {
        public const float MinimumAverageLuminance = 0.10f;
        public const float MaximumDarkPixelRatio = 0.92f;
        public const float MinimumScenarioBrightPixelRatio = 0.002f;
        public const float MaximumHotPinkPixelRatio = 0.03f;

        public static M140VisualValidationSummary Validate(string screenshotPath)
        {
            return Validate(screenshotPath, string.Empty);
        }

        public static M140VisualValidationSummary Validate(string screenshotPath, string scenarioId)
        {
            var summary = new M140VisualValidationSummary
            {
                screenshotPath = screenshotPath ?? string.Empty,
                exists = !string.IsNullOrWhiteSpace(screenshotPath) && File.Exists(screenshotPath)
            };
            var failures = new List<string>();
            if (!summary.exists)
            {
                failures.Add("Screenshot artifact is missing.");
                summary.failures = failures.ToArray();
                summary.passed = false;
                return summary;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                var bytes = File.ReadAllBytes(screenshotPath);
                summary.loadable = ImageConversion.LoadImage(texture, bytes, markNonReadable: false);
                if (!summary.loadable)
                {
                    failures.Add("Screenshot could not be decoded.");
                }
                else
                {
                    summary.width = texture.width;
                    summary.height = texture.height;
                    Analyze(texture, summary);
                    var strictBrightness = string.IsNullOrWhiteSpace(scenarioId);
                    if (strictBrightness && summary.averageLuminance < MinimumAverageLuminance)
                    {
                        failures.Add($"Screenshot average luminance {summary.averageLuminance:0.000} is below {MinimumAverageLuminance:0.00}.");
                    }

                    if (strictBrightness && summary.darkPixelRatio > MaximumDarkPixelRatio)
                    {
                        failures.Add($"Screenshot dark-pixel ratio {summary.darkPixelRatio:P1} exceeds {MaximumDarkPixelRatio:P0}.");
                    }

                    if (!strictBrightness &&
                        summary.averageLuminance < 0.015f &&
                        summary.brightPixelRatio < MinimumScenarioBrightPixelRatio)
                    {
                        failures.Add("Screenshot appears blank or fully black for the scenario.");
                    }

                    if (summary.hotPinkPixelRatio > MaximumHotPinkPixelRatio)
                    {
                        failures.Add($"Screenshot has hot-pink material-like pixels {summary.hotPinkPixelRatio:P1} above {MaximumHotPinkPixelRatio:P0}.");
                    }
                }
            }
            catch (Exception exception)
            {
                failures.Add($"Screenshot validation failed: {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(texture);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            summary.failures = failures.ToArray();
            summary.passed = failures.Count == 0;
            return summary;
        }

        private static void Analyze(Texture2D texture, M140VisualValidationSummary summary)
        {
            var total = 0;
            var dark = 0;
            var bright = 0;
            var hotPink = 0;
            var luminanceSum = 0f;
            var stepX = Mathf.Max(1, texture.width / 64);
            var stepY = Mathf.Max(1, texture.height / 64);
            for (var y = 0; y < texture.height; y += stepY)
            {
                for (var x = 0; x < texture.width; x += stepX)
                {
                    var color = texture.GetPixel(x, y);
                    var luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
                    luminanceSum += luminance;
                    total++;
                    if (luminance < 0.08f)
                    {
                        dark++;
                    }

                    if (luminance > 0.32f)
                    {
                        bright++;
                    }

                    if (color.r > 0.82f && color.g < 0.28f && color.b > 0.82f)
                    {
                        hotPink++;
                    }
                }
            }

            summary.averageLuminance = total <= 0 ? 0f : luminanceSum / total;
            summary.darkPixelRatio = total <= 0 ? 1f : dark / (float)total;
            summary.brightPixelRatio = total <= 0 ? 0f : bright / (float)total;
            summary.hotPinkPixelRatio = total <= 0 ? 0f : hotPink / (float)total;
        }
    }

    public static class M140PlayerLogValidator
    {
        private static readonly string[] ExceptionTokens =
        {
            "Exception:",
            "NullReferenceException",
            "MissingReferenceException",
            "ArgumentException",
            "InvalidOperationException"
        };

        private static readonly string[] ShaderTokens =
        {
            "Shader error",
            "shader is not supported",
            "missing shader",
            "failed to create shader"
        };

        private static readonly string[] MaterialTokens =
        {
            "pink material",
            "magenta material",
            "material could not",
            "failed to load material"
        };

        private static readonly string[] AddressablesTokens =
        {
            "Addressables Exception",
            "InvalidKeyException",
            "Unable to load asset",
            "could not be loaded"
        };

        private static readonly string[] MissingScriptTokens =
        {
            "The referenced script",
            "referenced script on this Behaviour",
            "Behaviour is missing"
        };

        private static readonly string[] RuntimeNavMeshFallbackTokens =
        {
            "dev-only runtime Unity NavMesh fallback",
            "runtime Unity NavMesh fallback",
            "NavMesh.DevRuntime"
        };

        public static M140PlayerLogValidationSummary Validate(string logPath)
        {
            var summary = new M140PlayerLogValidationSummary
            {
                logPath = logPath ?? string.Empty,
                exists = !string.IsNullOrWhiteSpace(logPath) && File.Exists(logPath)
            };
            if (!summary.exists)
            {
                summary.passed = true;
                return summary;
            }

            var text = File.ReadAllText(logPath);
            summary.exceptionCount = CountTokens(text, ExceptionTokens);
            summary.shaderIssueCount = CountTokens(text, ShaderTokens);
            summary.materialIssueCount = CountTokens(text, MaterialTokens);
            summary.addressablesIssueCount = CountTokens(text, AddressablesTokens);
            summary.missingScriptWarningCount = CountMatchingLines(text, MissingScriptTokens);
            summary.startupMissingScriptWarningCount = CountMatchingLinesBeforeFirstScenario(text, MissingScriptTokens);
            summary.scenarioMissingScriptWarningCount = Mathf.Max(
                0,
                summary.missingScriptWarningCount - summary.startupMissingScriptWarningCount);
            summary.runtimeNavMeshFallbackWarningCount = CountMatchingLines(text, RuntimeNavMeshFallbackTokens);
            summary.startupRuntimeNavMeshFallbackWarningCount = CountMatchingLinesBeforeFirstScenario(text, RuntimeNavMeshFallbackTokens);
            summary.scenarioRuntimeNavMeshFallbackWarningCount = Mathf.Max(
                0,
                summary.runtimeNavMeshFallbackWarningCount - summary.startupRuntimeNavMeshFallbackWarningCount);
            summary.issueLines = MatchingLines(text, ExceptionTokens, ShaderTokens, MaterialTokens, AddressablesTokens, MissingScriptTokens, RuntimeNavMeshFallbackTokens);
            summary.missingScriptWarningLines = MatchingLines(text, MissingScriptTokens);
            summary.missingScriptWarningContextLines = MatchingLineContexts(text, MissingScriptTokens);
            summary.runtimeNavMeshFallbackWarningLines = MatchingLines(text, RuntimeNavMeshFallbackTokens);
            summary.runtimeNavMeshFallbackWarningContextLines = MatchingLineContexts(text, RuntimeNavMeshFallbackTokens);
            summary.errorCount = summary.exceptionCount +
                summary.shaderIssueCount +
                summary.materialIssueCount +
                summary.addressablesIssueCount +
                summary.scenarioMissingScriptWarningCount +
                summary.scenarioRuntimeNavMeshFallbackWarningCount;
            var failures = new List<string>();
            if (summary.exceptionCount > 0)
            {
                failures.Add($"Player log contains {summary.exceptionCount} exception-like entries.");
            }

            if (summary.shaderIssueCount > 0)
            {
                failures.Add($"Player log contains {summary.shaderIssueCount} shader issue entries.");
            }

            if (summary.materialIssueCount > 0)
            {
                failures.Add($"Player log contains {summary.materialIssueCount} material issue entries.");
            }

            if (summary.addressablesIssueCount > 0)
            {
                failures.Add($"Player log contains {summary.addressablesIssueCount} Addressables/content issue entries.");
            }

            if (summary.scenarioMissingScriptWarningCount > 0)
            {
                failures.Add($"Player log contains {summary.scenarioMissingScriptWarningCount} missing-script warning entries during M140 scenarios.");
            }

            if (summary.scenarioRuntimeNavMeshFallbackWarningCount > 0)
            {
                failures.Add($"Player log contains {summary.scenarioRuntimeNavMeshFallbackWarningCount} runtime NavMesh fallback warning entries during M140 scenarios.");
            }

            summary.failures = failures.ToArray();
            summary.passed = failures.Count == 0;
            return summary;
        }

        private static int CountMatchingLinesBeforeFirstScenario(string text, params string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null || tokens.Length == 0)
            {
                return 0;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var count = 0;
            foreach (var line in lines)
            {
                if (line.IndexOf("[M140] ScenarioStart", StringComparison.Ordinal) >= 0)
                {
                    break;
                }

                foreach (var token in tokens)
                {
                    if (!string.IsNullOrWhiteSpace(token) &&
                        line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private static string[] MatchingLines(string text, params string[][] tokenGroups)
        {
            if (string.IsNullOrEmpty(text) || tokenGroups == null)
            {
                return Array.Empty<string>();
            }

            const int maxLines = 24;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var matches = new List<string>(Math.Min(lines.Length, maxLines));
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var matched = false;
                foreach (var group in tokenGroups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    foreach (var token in group)
                    {
                        if (!string.IsNullOrEmpty(token) &&
                            line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        break;
                    }
                }

                if (!matched)
                {
                    continue;
                }

                matches.Add(line.Length <= 240 ? line : line.Substring(0, 240));
                if (matches.Count >= maxLines)
                {
                    break;
                }
            }

            return matches.ToArray();
        }

        private static string[] MatchingLineContexts(string text, params string[][] tokenGroups)
        {
            if (string.IsNullOrEmpty(text) || tokenGroups == null)
            {
                return Array.Empty<string>();
            }

            const int maxContexts = 8;
            const int contextBefore = 3;
            const int contextAfter = 2;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var contexts = new List<string>(maxContexts);
            for (var index = 0; index < lines.Length && contexts.Count < maxContexts; index++)
            {
                var line = lines[index];
                if (string.IsNullOrWhiteSpace(line) || !LineMatches(line, tokenGroups))
                {
                    continue;
                }

                var start = Mathf.Max(0, index - contextBefore);
                var end = Mathf.Min(lines.Length - 1, index + contextAfter);
                var builder = new StringBuilder(512);
                builder.Append("lines ").Append(start + 1).Append('-').Append(end + 1).Append(": ");
                for (var cursor = start; cursor <= end; cursor++)
                {
                    var sample = lines[cursor];
                    if (string.IsNullOrWhiteSpace(sample))
                    {
                        continue;
                    }

                    if (builder.Length > 540)
                    {
                        builder.Append(" ...");
                        break;
                    }

                    builder.Append(cursor == index ? " >> " : " | ");
                    builder.Append(TrimLine(sample, 180));
                }

                contexts.Add(builder.ToString());
            }

            return contexts.ToArray();
        }

        private static bool LineMatches(string line, string[][] tokenGroups)
        {
            foreach (var group in tokenGroups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var token in group)
                {
                    if (!string.IsNullOrEmpty(token) &&
                        line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string TrimLine(string line, int maxLength)
        {
            if (string.IsNullOrEmpty(line) || line.Length <= maxLength)
            {
                return line ?? string.Empty;
            }

            return line.Substring(0, Mathf.Max(0, maxLength));
        }

        private static int CountTokens(string text, string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var token in tokens)
            {
                var index = 0;
                while ((index = text.IndexOf(token, index, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    count++;
                    index += token.Length;
                }
            }

            return count;
        }

        private static int CountMatchingLines(string text, string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null)
            {
                return 0;
            }

            var count = 0;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                foreach (var token in tokens)
                {
                    if (!string.IsNullOrEmpty(token) &&
                        line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }
    }

    public static class M140BuildRealReportGenerator
    {
        public const string LockId = "M140";
        public const string Title = "M140 Build-Real Performance And Visual Release Gate";
        public const string DefaultReportDirectory = "output/reports/m140/player";
        public const string DefaultJsonFileName = "m140_build_real_gate.json";
        public const string DefaultMarkdownFileName = "m140_build_real_gate.md";
        public const int TargetFrameRate = 60;
        public const double FrameP95BudgetMs = 16.7d;
        public const double FrameP95BorderlineJitterMs = 17.0d;
        public const double FrameP95HardFailMs = 17.25d;
        public const double FrameP95CleanCadenceJitterCeilingMs = 18.0d;
        public const double CpuWorkP95BudgetMs = 16.7d;
        public const double CpuStageCleanMaxMs = 6.0d;
        public const double MaxFrameBudgetMs = 50d;

        public static readonly string[] RequiredDevelopmentScenarioIds =
        {
            "boot_loading_screen",
            "branch_entry_loading",
            "normal_traversal",
            "return_to_previous_room",
            "reward_room",
            "enemy_stress_30",
            "projectile_heavy_room",
            "boss_plus_adds",
            "boss_entry",
            "long_run_smoke",
            "next_branch_entry"
        };

        public static readonly string[] RequiredReleaseSmokeScenarioIds =
        {
            "boot_loading_screen",
            "branch_entry_loading",
            "normal_traversal"
        };

        public static string CurrentRuntimePlatformId()
        {
            return Application.platform switch
            {
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => "macos-apple-silicon",
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "windows-x64",
                _ => Application.platform.ToString()
            };
        }

        public static M140ScenarioSummary BuildBootScenarioSummary(
            string platformId,
            string buildKind,
            M136PerformanceOperationSnapshot operations,
            M140VisualValidationSummary visual,
            bool enforceTiming)
        {
            var summary = BaseScenario("boot_loading_screen", "Boot Loading Screen", platformId, buildKind, visual);
            summary.bootLoadingStarts = operations.BootLoadingStarts;
            summary.bootLoadingCompletions = operations.BootLoadingCompletions;
            summary.runtimeNavMeshFallbacks = operations.RuntimeNavMeshFallbacks;
            summary.branchLiveRoomsBuilt = operations.BranchLiveRoomsBuilt;
            summary.branchLiveRoomCacheHits = operations.BranchLiveRoomCacheHits;
            summary.branchLiveRoomCacheMisses = operations.BranchLiveRoomCacheMisses;
            summary.normalTraversalRoomRebuildCalls = operations.NormalTraversalRoomRebuildCalls;
            summary.normalTraversalWarmCalls = operations.NormalTraversalWarmCalls;
            summary.hibernatedRoomActiveObjectLeaks = operations.HibernatedRoomActiveObjectLeaks;
            summary.transitionCurtainMaxFramesAfterReady = operations.TransitionCurtainMaxFramesAfterReady;
            summary.transitionCurtainMaxVisibleMilliseconds = operations.TransitionCurtainMaxVisibleMilliseconds;
            summary.stagedRoomVisibleRendererFrames = operations.StagedRoomVisibleRendererFrames;
            summary.normalTraversalRevealFrames = operations.NormalTraversalRevealFrames;
            summary.roomReadyBeforeRevealMaxFrames = operations.RoomReadyBeforeRevealMaxFrames;
            summary.poolWarmVisibleObjects = operations.PoolWarmVisibleObjects;
            summary.poolWarmRootActiveErrors = operations.PoolWarmRootActiveErrors;
            summary.poolWarmActiveLeaks = operations.PoolWarmActiveLeaks;
            summary.presentationFallbackVisuals = operations.PresentationFallbackVisuals;
            summary.roomEntryVfxBeforeReveal = operations.RoomEntryVfxBeforeReveal;
            summary.cpuStageSummary = operations.CpuStageSummary;
            summary.tacticalDirectorSummary = operations.TacticalDirectorSummary;
            summary.cacheMissAttributionSummary = operations.CacheMissAttributionSummary;
            summary.preloadBuildCacheMissAttributionSummary = BuildAttributionSummary(
                operations.CacheMissAttributionRows,
                row => !IsPostLoadTraversalAttribution(row));
            summary.postLoadCacheMissAttributionSummary = BuildAttributionSummary(
                operations.CacheMissAttributionRows,
                IsPostLoadTraversalAttribution);
            summary.note = "Boot counters are captured from runtime operation telemetry.";
            EvaluateScenario(summary, enforceTiming);
            return summary;
        }

        public static M140ScenarioSummary FromM136Result(
            string scenarioId,
            string displayName,
            string platformId,
            string buildKind,
            M136PerformanceScenarioResult result,
            M140VisualValidationSummary visual,
            bool enforceTiming)
        {
            var summary = BaseScenario(scenarioId, displayName, platformId, buildKind, visual);
            if (result == null)
            {
                summary.failures = new[] { "Missing M136 scenario result." };
                summary.passed = false;
                return summary;
            }

            var frameMetric = Metric(result, "frame_time_ms");
            var mainThreadMetric = Metric(result, "main_thread_ms");
            var gcMetric = Metric(result, "gc_allocated_bytes");
            summary.timingAuthoritative = string.Equals(result.samplingSource, M136FrameCadencePolicy.RuntimeUpdateSamplingSource, StringComparison.Ordinal) &&
                string.Equals(result.frameCadenceConfidence, M136FrameCadencePolicy.Trusted, StringComparison.Ordinal) &&
                frameMetric.supported &&
                frameMetric.sampleCount > 0;
            summary.frameCadenceConfidence = result.frameCadenceConfidence ?? string.Empty;
            summary.rawSampleCount = result.rawSampleCount;
            summary.frameP50Ms = frameMetric.p50;
            summary.frameP95Ms = frameMetric.p95;
            summary.frameMaxMs = frameMetric.max;
            summary.cpuWorkMetricSupported = IsIndependentCpuWorkMetric(frameMetric, mainThreadMetric);
            summary.cpuWorkP95Ms = mainThreadMetric.p95;
            summary.cpuWorkMaxMs = mainThreadMetric.max;
            if (!summary.cpuWorkMetricSupported && mainThreadMetric.supported && mainThreadMetric.sampleCount > 0)
            {
                summary.note = AppendNote(summary.note, "Main-thread recorder matched frame cadence; CPU truth uses named stage maxima.");
            }
            summary.gcMaxBytes = gcMetric.supported ? (long)Math.Round(gcMetric.max) : 0;

            var objects = result.objectCounts ?? new M136LiveObjectCountSummary();
            summary.peakActiveEnemies = objects.peakEnemies;
            summary.peakProjectiles = objects.peakProjectiles;
            summary.peakRenderers = objects.peakRenderers;
            summary.observedBranchSession = objects.observedBranchSession;
            summary.observedCombatController = objects.observedCombatController;
            summary.observedBoss = objects.observedBoss;

            var operations = result.operations ?? new M136RuntimeOperationSummary();
            summary.runtimeNavMeshFallbacks = operations.runtimeNavMeshFallbacks;
            summary.stressHarnessNavMeshBakes = operations.stressHarnessNavMeshBakes;
            summary.aiThinkFull = operations.aiThinkFull;
            summary.aiThinkReduced = operations.aiThinkReduced;
            summary.aiThinkBackground = operations.aiThinkBackground;
            summary.aiCommandReuses = operations.aiCommandReuses;
            summary.aiScorerCalls = operations.aiScorerCalls;
            summary.aiBehaviorGraphTicks = operations.aiBehaviorGraphTicks;
            summary.navPathRequests = operations.navPathRequests;
            summary.navPathSolves = operations.navPathSolves;
            summary.navPathDeferred = operations.navPathDeferred;
            summary.navPathMaxSolveMilliseconds = operations.navPathMaxSolveMilliseconds;
            summary.tacticalCrowdReservationSkips = operations.tacticalCrowdReservationSkips;
            summary.tacticalCrowdCachedIntentReuses = operations.tacticalCrowdCachedIntentReuses;
            summary.tacticalCrowdSupportReservationBudgetUses = operations.tacticalCrowdSupportReservationBudgetUses;
            summary.tacticalCrowdActiveThreatLimitMax = operations.tacticalCrowdActiveThreatLimitMax;
            summary.tacticalCrowdScorerSkips = operations.tacticalCrowdScorerSkips;
            summary.normalTraversalColdCacheMissesAfterLoad = operations.traversalColdCacheMisses;
            summary.branchLiveRoomsBuilt = operations.branchLiveRoomsBuilt;
            summary.branchLiveRoomCacheHits = operations.branchLiveRoomCacheHits;
            summary.branchLiveRoomCacheMisses = operations.branchLiveRoomCacheMisses;
            summary.normalTraversalRoomRebuildCalls = operations.normalTraversalRoomRebuildCalls;
            summary.normalTraversalWarmCalls = operations.normalTraversalWarmCalls;
            summary.hibernatedRoomActiveObjectLeaks = operations.hibernatedRoomActiveObjectLeaks;
            summary.transitionCurtainMaxFramesAfterReady = operations.transitionCurtainMaxFramesAfterReady;
            summary.transitionCurtainMaxVisibleMilliseconds = operations.transitionCurtainMaxVisibleMilliseconds;
            summary.stagedRoomVisibleRendererFrames = operations.stagedRoomVisibleRendererFrames;
            summary.normalTraversalRevealFrames = operations.normalTraversalRevealFrames;
            summary.roomReadyBeforeRevealMaxFrames = operations.roomReadyBeforeRevealMaxFrames;
            summary.poolWarmVisibleObjects = operations.poolWarmVisibleObjects;
            summary.poolWarmRootActiveErrors = operations.poolWarmRootActiveErrors;
            summary.poolWarmActiveLeaks = operations.poolWarmActiveLeaks;
            summary.presentationFallbackVisuals = operations.presentationFallbackVisuals;
            summary.roomEntryVfxBeforeReveal = operations.roomEntryVfxBeforeReveal;
            summary.cpuStageSummary = operations.cpuStageSummary;
            summary.tacticalDirectorSummary = operations.tacticalDirectorSummary;
            summary.cacheMissAttributionSummary = operations.cacheMissAttributionSummary;
            summary.preloadBuildCacheMissAttributionSummary = BuildAttributionSummary(
                operations.cacheMissAttributionRows,
                row => !IsPostLoadTraversalAttribution(row));
            summary.postLoadCacheMissAttributionSummary = BuildAttributionSummary(
                operations.cacheMissAttributionRows,
                IsPostLoadTraversalAttribution);
            summary.projectileActivePeak = operations.projectileActivePeak;
            summary.projectileSpawns = operations.projectileSpawns;
            summary.projectileReturns = operations.projectileReturns;
            summary.projectileCollisionChecks = operations.projectileCollisionChecks;
            summary.projectilePoolMisses = operations.projectilePoolMisses;
            summary.projectileHardInstantiates = operations.projectileHardInstantiates;
            summary.projectileUpdateMaxMilliseconds = operations.projectileUpdateMaxMilliseconds;
            summary.bootLoadingStarts = operations.bootLoadingStarts;
            summary.bootLoadingCompletions = operations.bootLoadingCompletions;
            summary.branchLoadingCompletions = operations.branchLoadingCompletions;
            summary.bossLoadingStarts = operations.bossLoadingStarts;
            summary.bossLoadingCompletions = operations.bossLoadingCompletions;
            summary.note = result.note ?? string.Empty;
            EvaluateScenario(summary, enforceTiming);
            return summary;
        }

        public static M140ScenarioSummary FromM138Summary(
            M138CombatScaleStressScenarioSummary stress,
            string platformId,
            string buildKind,
            M140VisualValidationSummary visual,
            bool enforceTiming,
            M136LiveObjectCountSnapshot objectEvidence = null)
        {
            var summary = BaseScenario(stress?.scenarioId, stress?.displayName, platformId, buildKind, visual);
            if (stress == null)
            {
                summary.failures = new[] { "Missing M138 stress summary." };
                summary.passed = false;
                return summary;
            }

            summary.timingAuthoritative = stress.timingAuthoritative;
            summary.frameCadenceConfidence = stress.frameCadenceConfidence ?? string.Empty;
            summary.rawSampleCount = stress.rawSampleCount;
            summary.frameP50Ms = stress.frameP50Ms;
            summary.frameP95Ms = stress.frameP95Ms;
            summary.frameMaxMs = stress.frameMaxMs;
            summary.cpuWorkMetricSupported = stress.cpuWorkMetricSupported;
            summary.cpuWorkP95Ms = stress.cpuWorkP95Ms;
            summary.cpuWorkMaxMs = stress.cpuWorkMaxMs;
            summary.gcMaxBytes = stress.gcMaxBytes;
            summary.peakActiveEnemies = stress.peakActiveEnemies;
            summary.peakProjectiles = stress.peakProjectiles;
            summary.projectileActivePeak = stress.projectileActivePeak;
            summary.projectileSpawns = stress.projectileSpawns;
            summary.projectileReturns = stress.projectileReturns;
            summary.projectileCollisionChecks = stress.projectileCollisionChecks;
            summary.projectilePoolMisses = stress.projectilePoolMisses;
            summary.projectileHardInstantiates = stress.projectileHardInstantiates;
            summary.projectileUpdateMaxMilliseconds = stress.projectileUpdateMaxMilliseconds;
            summary.observedCombatController = true;
            summary.observedBoss = stress.observedBoss;
            summary.runtimeNavMeshFallbacks = stress.runtimeNavMeshFallbacks;
            summary.stressHarnessNavMeshBakes = stress.stressHarnessNavMeshBakes;
            summary.aiThinkFull = stress.aiThinkFull;
            summary.aiThinkReduced = stress.aiThinkReduced;
            summary.aiThinkBackground = stress.aiThinkBackground;
            summary.aiCommandReuses = stress.aiCommandReuses;
            summary.aiScorerCalls = stress.aiScorerCalls;
            summary.aiBehaviorGraphTicks = stress.aiBehaviorGraphTicks;
            summary.navPathRequests = stress.navPathRequests;
            summary.navPathSolves = stress.navPathSolves;
            summary.navPathDeferred = stress.navPathDeferred;
            summary.navPathMaxSolveMilliseconds = stress.navPathMaxSolveMilliseconds;
            summary.tacticalCrowdReservationSkips = stress.tacticalCrowdReservationSkips;
            summary.tacticalCrowdCachedIntentReuses = stress.tacticalCrowdCachedIntentReuses;
            summary.tacticalCrowdSupportReservationBudgetUses = stress.tacticalCrowdSupportReservationBudgetUses;
            summary.tacticalCrowdActiveThreatLimitMax = stress.tacticalCrowdActiveThreatLimitMax;
            summary.tacticalCrowdScorerSkips = stress.tacticalCrowdScorerSkips;
            summary.cpuStageSummary = stress.cpuStageSummary;
            summary.tacticalDirectorSummary = stress.tacticalDirectorSummary;
            summary.note = stress.note ?? "M138 stress scenario reused by M140 built-player gate.";
            ApplyObjectEvidence(summary, objectEvidence);
            EvaluateScenario(summary, enforceTiming);
            var acceptedNestedTimingFailure = CanAcceptNestedM138TimingOnlyFailure(stress, summary, enforceTiming);
            summary.m138GatePassed = stress.passed || acceptedNestedTimingFailure;
            if (acceptedNestedTimingFailure)
            {
                summary.note = AppendNote(
                    summary.note,
                    "Nested M138 frame-p95-only failure accepted by M140 clean frame-cap jitter rule.");
            }
            else if (!stress.passed)
            {
                AppendFailure(summary, $"M138 scenario gate failed: {string.Join("; ", stress.failures ?? Array.Empty<string>())}");
            }

            if (string.Equals(stress.scenarioId, "enemy_stress_30", StringComparison.Ordinal) && stress.peakActiveEnemies < 30)
            {
                AppendFailure(summary, "M140 primary combat gate did not observe 30 active enemies.");
            }

            summary.passed = (summary.failures ?? Array.Empty<string>()).Length == 0;
            return summary;
        }

        public static M140ScenarioSummary FromM139Report(
            M139LongRunSoakReport soakReport,
            string platformId,
            string buildKind,
            M140VisualValidationSummary visual,
            M136LiveObjectCountSnapshot objectEvidence = null)
        {
            var summary = BaseScenario("long_run_smoke", "Long-Run Smoke", platformId, buildKind, visual);
            if (soakReport == null)
            {
                summary.failures = new[] { "Missing M139 long-run smoke report." };
                summary.passed = false;
                return summary;
            }

            var combinedFailures = new List<string>();
            foreach (var scenario in soakReport.scenarios ?? Array.Empty<M139LongRunSoakScenarioSummary>())
            {
                summary.normalTraversalColdCacheMissesAfterLoad += scenario.normalTraversalColdCacheMissesAfterLoad;
                summary.runtimeNavMeshFallbacks += scenario.runtimeNavMeshFallbacks;
                summary.shaderMaterialFirstUseMissesAfterLoad += scenario.shaderMaterialFirstUseMissesAfterLoad;
                summary.staleEnemyStateFailures += scenario.staleEnemyStateFailures;
                summary.staleRuntimePoolStateFailures += scenario.staleRuntimePoolStateFailures;
                summary.poolActiveLeaks += scenario.poolActiveLeaks;
                summary.enemyPoolHardInstantiatesAfterWarmup += scenario.enemyPoolHardInstantiatesAfterWarmup;
                summary.runtimePoolHardInstantiatesAfterWarmup += scenario.runtimePoolHardInstantiatesAfterWarmup;
                summary.stagedRoomVisibleRendererFrames += scenario.stagedRoomVisibleRendererFrames;
                summary.poolWarmVisibleObjects += scenario.poolWarmVisibleObjects;
                summary.poolWarmRootActiveErrors += scenario.poolWarmRootActiveErrors;
                summary.poolWarmActiveLeaks += scenario.poolWarmActiveLeaks;
                summary.presentationFallbackVisuals += scenario.presentationFallbackVisuals;
                summary.roomEntryVfxBeforeReveal += scenario.roomEntryVfxBeforeReveal;
                summary.frameP95Ms = Math.Max(summary.frameP95Ms, scenario.frameP95Ms);
                summary.frameMaxMs = Math.Max(summary.frameMaxMs, scenario.frameMaxMs);
                summary.cacheMissAttributionSummary = CombineAttribution(
                    summary.cacheMissAttributionSummary,
                    scenario.scenarioId,
                    scenario.cacheMissAttributionSummary);
                summary.preloadBuildCacheMissAttributionSummary = CombineAttribution(
                    summary.preloadBuildCacheMissAttributionSummary,
                    scenario.scenarioId,
                    scenario.preloadBuildCacheMissAttributionSummary);
                summary.postLoadCacheMissAttributionSummary = CombineAttribution(
                    summary.postLoadCacheMissAttributionSummary,
                    scenario.scenarioId,
                    scenario.postLoadCacheMissAttributionSummary);
                if (!scenario.passed)
                {
                    combinedFailures.Add($"{scenario.scenarioId}: {string.Join("; ", scenario.failures ?? Array.Empty<string>())}");
                }
            }

            summary.m139GatePassed = soakReport.passed;
            summary.note = "M139 CI smoke reused by M140 built-player gate.";
            ApplyObjectEvidence(summary, objectEvidence);
            EvaluateScenario(summary, enforceTiming: false);
            if (!soakReport.passed)
            {
                AppendFailure(summary, $"M139 long-run smoke failed: {string.Join("; ", combinedFailures)}");
            }

            summary.passed = (summary.failures ?? Array.Empty<string>()).Length == 0;
            return summary;
        }

        public static M140ScenarioSummary FromM139ScenarioSummary(
            string scenarioId,
            string displayName,
            M139LongRunSoakScenarioSummary soak,
            string platformId,
            string buildKind,
            M140VisualValidationSummary visual,
            M136LiveObjectCountSnapshot objectEvidence = null)
        {
            var summary = BaseScenario(scenarioId, displayName, platformId, buildKind, visual);
            if (soak == null)
            {
                summary.failures = new[] { "Missing M139 branch-backed scenario summary." };
                summary.passed = false;
                return summary;
            }

            summary.frameP95Ms = soak.frameP95Ms;
            summary.frameMaxMs = soak.frameMaxMs;
            summary.normalTraversalColdCacheMissesAfterLoad = soak.normalTraversalColdCacheMissesAfterLoad;
            summary.runtimeNavMeshFallbacks = soak.runtimeNavMeshFallbacks;
            summary.branchLoadingCompletions = soak.branchLoadsCompleted;
            summary.bossLoadingCompletions = soak.bossLoadsCompleted;
            summary.shaderMaterialFirstUseMissesAfterLoad = soak.shaderMaterialFirstUseMissesAfterLoad;
            summary.staleEnemyStateFailures = soak.staleEnemyStateFailures;
            summary.staleRuntimePoolStateFailures = soak.staleRuntimePoolStateFailures;
            summary.poolActiveLeaks = soak.poolActiveLeaks;
            summary.enemyPoolHardInstantiatesAfterWarmup = soak.enemyPoolHardInstantiatesAfterWarmup;
            summary.runtimePoolHardInstantiatesAfterWarmup = soak.runtimePoolHardInstantiatesAfterWarmup;
            summary.stagedRoomVisibleRendererFrames = soak.stagedRoomVisibleRendererFrames;
            summary.poolWarmVisibleObjects = soak.poolWarmVisibleObjects;
            summary.poolWarmRootActiveErrors = soak.poolWarmRootActiveErrors;
            summary.poolWarmActiveLeaks = soak.poolWarmActiveLeaks;
            summary.presentationFallbackVisuals = soak.presentationFallbackVisuals;
            summary.roomEntryVfxBeforeReveal = soak.roomEntryVfxBeforeReveal;
            summary.cacheMissAttributionSummary = soak.cacheMissAttributionSummary;
            summary.preloadBuildCacheMissAttributionSummary = soak.preloadBuildCacheMissAttributionSummary;
            summary.postLoadCacheMissAttributionSummary = soak.postLoadCacheMissAttributionSummary;
            summary.m139GatePassed = soak.passed;
            summary.note = $"Branch-backed M139 smoke slice `{soak.scenarioId}` reused by M140.";
            ApplyObjectEvidence(summary, objectEvidence);
            EvaluateScenario(summary, enforceTiming: false);
            if (!soak.passed)
            {
                AppendFailure(summary, $"M139 branch-backed slice failed: {string.Join("; ", soak.failures ?? Array.Empty<string>())}");
            }

            summary.passed = (summary.failures ?? Array.Empty<string>()).Length == 0;
            return summary;
        }

        public static M140BuildRealReport BuildReport(
            string platformId,
            string buildKind,
            string artifactRoot,
            IEnumerable<M140ScenarioSummary> scenarioSummaries,
            M140RenderRuntimeSnapshot renderRuntime,
            M140PlayerLogValidationSummary playerLog,
            bool environmentBlocked = false,
            string environmentBlockReason = "")
        {
            var summaries = scenarioSummaries?.Where(summary => summary != null).ToArray() ?? Array.Empty<M140ScenarioSummary>();
            var required = string.Equals(buildKind, M140BuildKind.ReleaseSmoke, StringComparison.OrdinalIgnoreCase)
                ? RequiredReleaseSmokeScenarioIds
                : RequiredDevelopmentScenarioIds;
            var failures = new List<string>();

            foreach (var id in required)
            {
                if (!summaries.Any(summary => string.Equals(summary.scenarioId, id, StringComparison.Ordinal)))
                {
                    failures.Add($"Missing M140 built-player scenario `{id}`.");
                }
            }

            foreach (var scenario in summaries)
            {
                if (!scenario.passed)
                {
                    failures.Add($"{scenario.scenarioId}: {string.Join("; ", scenario.failures ?? Array.Empty<string>())}");
                }
            }

            if (playerLog != null && !playerLog.passed)
            {
                failures.Add($"Player log failed validation: {string.Join("; ", playerLog.failures ?? Array.Empty<string>())}");
            }

            if (renderRuntime == null)
            {
                failures.Add("Render runtime snapshot is missing.");
            }
            else
            {
                if (renderRuntime.targetFrameRate != TargetFrameRate)
                {
                    failures.Add($"M140 runtime target FPS is {renderRuntime.targetFrameRate}; expected {TargetFrameRate} while capture override is active.");
                }

                if (renderRuntime.vSyncCount != 0)
                {
                    failures.Add($"M140 runtime vSyncCount is {renderRuntime.vSyncCount}; expected 0 for deterministic capture FPS policy.");
                }
            }

            if (string.Equals(platformId, "macos-apple-silicon", StringComparison.Ordinal) &&
                renderRuntime != null &&
                !LooksLikeAppleSilicon(renderRuntime.processorType, renderRuntime.operatingSystem))
            {
                failures.Add($"macOS M140 player did not report Apple silicon/ARM runtime. Processor=`{renderRuntime.processorType}`, OS=`{renderRuntime.operatingSystem}`.");
            }

            if (environmentBlocked && !string.IsNullOrWhiteSpace(environmentBlockReason))
            {
                failures.Add(environmentBlockReason);
            }

            var result = environmentBlocked
                ? M140GateResult.BlockedByEnvironment
                : failures.Count == 0 ? M140GateResult.Passed : M140GateResult.Failed;
            return new M140BuildRealReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                platformId = platformId ?? string.Empty,
                buildKind = buildKind ?? string.Empty,
                result = result,
                passed = string.Equals(result, M140GateResult.Passed, StringComparison.Ordinal),
                environmentBlocked = environmentBlocked,
                scenarioCount = summaries.Length,
                artifactRoot = artifactRoot ?? string.Empty,
                playerLogPath = playerLog?.logPath ?? string.Empty,
                renderRuntime = renderRuntime,
                playerLog = playerLog,
                scenarios = summaries,
                failures = failures.ToArray()
            };
        }

        public static void WriteReport(M140BuildRealReport report, string jsonPath = null, string markdownPath = null)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var resolvedJson = string.IsNullOrWhiteSpace(jsonPath) ? Path.Combine(DefaultReportDirectory, DefaultJsonFileName) : jsonPath;
            var resolvedMarkdown = string.IsNullOrWhiteSpace(markdownPath) ? Path.Combine(DefaultReportDirectory, DefaultMarkdownFileName) : markdownPath;
            CreateParentDirectory(resolvedJson);
            CreateParentDirectory(resolvedMarkdown);
            File.WriteAllText(resolvedJson, JsonUtility.ToJson(report, true));
            File.WriteAllText(resolvedMarkdown, ToMarkdown(report));
        }

        public static string ToMarkdown(M140BuildRealReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M140 Build-Real Performance And Visual Release Gate");
            builder.AppendLine();
            builder.AppendLine($"- Status: `{report?.result ?? M140GateResult.Failed}`");
            builder.AppendLine($"- Platform: `{report?.platformId ?? string.Empty}`");
            builder.AppendLine($"- Build kind: `{report?.buildKind ?? string.Empty}`");
            builder.AppendLine($"- Scenarios: {report?.scenarioCount ?? 0}");
            builder.AppendLine($"- Artifact root: `{report?.artifactRoot ?? string.Empty}`");
            if (report?.renderRuntime != null)
            {
                builder.AppendLine($"- Runtime: `{report.renderRuntime.operatingSystem}` / `{report.renderRuntime.processorType}` / `{report.renderRuntime.graphicsDeviceName}`");
                builder.AppendLine($"- Rendering: `{report.renderRuntime.renderPipelineAssetName}`, target FPS {report.renderRuntime.targetFrameRate}, vSync {report.renderRuntime.vSyncCount}");
                builder.AppendLine($"- Shader warmup: {report.renderRuntime.shaderWarmSuccesses}/{report.renderRuntime.shaderWarmAttempts} successes, misses {report.renderRuntime.shaderWarmMisses}");
            }

            if (report?.playerLog != null)
            {
                builder.AppendLine($"- Player log: `{report.playerLog.logPath}` exists {report.playerLog.exists}, issues {report.playerLog.errorCount}, missing scripts {report.playerLog.missingScriptWarningCount} (startup {report.playerLog.startupMissingScriptWarningCount}, scenario {report.playerLog.scenarioMissingScriptWarningCount}), runtime nav fallbacks {report.playerLog.runtimeNavMeshFallbackWarningCount} (startup {report.playerLog.startupRuntimeNavMeshFallbackWarningCount}, scenario {report.playerLog.scenarioRuntimeNavMeshFallbackWarningCount})");
            }

            builder.AppendLine();
            if (report?.failures != null && report.failures.Length > 0)
            {
                builder.AppendLine("## Failures");
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }

                builder.AppendLine();
            }

            if (report?.playerLog?.issueLines != null && report.playerLog.issueLines.Length > 0)
            {
                builder.AppendLine("## Player Log Issue Lines");
                foreach (var line in report.playerLog.issueLines)
                {
                    builder.AppendLine($"- `{line}`");
                }

                builder.AppendLine();
            }

            if (report?.playerLog?.missingScriptWarningContextLines != null &&
                report.playerLog.missingScriptWarningContextLines.Length > 0)
            {
                builder.AppendLine("## Player Log Missing Script Context");
                foreach (var line in report.playerLog.missingScriptWarningContextLines)
                {
                    builder.AppendLine($"- `{line}`");
                }

                builder.AppendLine();
            }

            builder.AppendLine("## Scenarios");
            foreach (var scenario in report?.scenarios ?? Array.Empty<M140ScenarioSummary>())
            {
                builder.AppendLine($"### {scenario.displayName}");
                builder.AppendLine($"- Status: {(scenario.passed ? "PASS" : "FAIL")}");
                builder.AppendLine($"- Frame p95/max: {scenario.frameP95Ms:0.00} / {scenario.frameMaxMs:0.00} ms ({scenario.frameCadenceConfidence})");
                if (scenario.cpuWorkMetricSupported)
                {
                    builder.AppendLine($"- CPU work p95/max: {scenario.cpuWorkP95Ms:0.00} / {scenario.cpuWorkMaxMs:0.00} ms");
                }

                builder.AppendLine($"- Objects: enemies {scenario.peakActiveEnemies}, projectiles {scenario.peakProjectiles}, renderers {scenario.peakRenderers}, boss {scenario.observedBoss}");
                builder.AppendLine($"- Gates: nav fallback {scenario.runtimeNavMeshFallbacks}, stress harness bakes {scenario.stressHarnessNavMeshBakes}, cold misses {scenario.normalTraversalColdCacheMissesAfterLoad}, live rooms {scenario.branchLiveRoomsBuilt}, live hits/misses {scenario.branchLiveRoomCacheHits}/{scenario.branchLiveRoomCacheMisses}, rebuilds/warms {scenario.normalTraversalRoomRebuildCalls}/{scenario.normalTraversalWarmCalls}, curtain after-ready {scenario.transitionCurtainMaxFramesAfterReady}, boss loads {scenario.bossLoadingStarts}/{scenario.bossLoadingCompletions}, shader misses {scenario.shaderMaterialFirstUseMissesAfterLoad}");
                builder.AppendLine($"- Visual artifacts: staged visible frames {scenario.stagedRoomVisibleRendererFrames}, warm visible {scenario.poolWarmVisibleObjects}, warm leaks {scenario.poolWarmActiveLeaks}, fallback visuals {scenario.presentationFallbackVisuals}, pre-reveal VFX {scenario.roomEntryVfxBeforeReveal}");
                if (!string.IsNullOrWhiteSpace(scenario.postLoadCacheMissAttributionSummary))
                {
                    builder.AppendLine($"- Post-load traversal miss attribution: {scenario.postLoadCacheMissAttributionSummary}");
                }

                if (!scenario.passed && !string.IsNullOrWhiteSpace(scenario.preloadBuildCacheMissAttributionSummary))
                {
                    builder.AppendLine($"- Preload/build attribution: {TrimAttributionForMarkdown(scenario.preloadBuildCacheMissAttributionSummary)}");
                }

                if (scenario.projectileActivePeak > 0 || scenario.projectileSpawns > 0 || scenario.projectileCollisionChecks > 0)
                {
                    builder.AppendLine($"- Projectile counters: peak {scenario.projectileActivePeak}, spawns {scenario.projectileSpawns}, returns {scenario.projectileReturns}, collision checks {scenario.projectileCollisionChecks}, pool misses {scenario.projectilePoolMisses}, hard instantiates {scenario.projectileHardInstantiates}, update max {scenario.projectileUpdateMaxMilliseconds:0.###} ms");
                }

                if (scenario.aiThinkFull > 0 || scenario.aiThinkReduced > 0 || scenario.aiThinkBackground > 0 || scenario.navPathRequests > 0)
                {
                    builder.AppendLine($"- AI/Nav counters: think F/R/B {scenario.aiThinkFull}/{scenario.aiThinkReduced}/{scenario.aiThinkBackground}, reuse {scenario.aiCommandReuses}, scorer {scenario.aiScorerCalls}, graph {scenario.aiBehaviorGraphTicks}, paths req/solve/def {scenario.navPathRequests}/{scenario.navPathSolves}/{scenario.navPathDeferred}, max solve {scenario.navPathMaxSolveMilliseconds:0.###} ms");
                }

                if (scenario.tacticalCrowdReservationSkips > 0 ||
                    scenario.tacticalCrowdCachedIntentReuses > 0 ||
                    scenario.tacticalCrowdSupportReservationBudgetUses > 0 ||
                    scenario.tacticalCrowdActiveThreatLimitMax > 0 ||
                    scenario.tacticalCrowdScorerSkips > 0)
                {
                    builder.AppendLine($"- Crowd tactical LOD: reservation skips {scenario.tacticalCrowdReservationSkips}, cached intents {scenario.tacticalCrowdCachedIntentReuses}, support budget uses {scenario.tacticalCrowdSupportReservationBudgetUses}, active threat cap {scenario.tacticalCrowdActiveThreatLimitMax}, scorer skips {scenario.tacticalCrowdScorerSkips}");
                }

                if (!string.IsNullOrWhiteSpace(scenario.cpuStageSummary))
                {
                    builder.AppendLine($"- CPU stages: {scenario.cpuStageSummary}");
                }

                if (!string.IsNullOrWhiteSpace(scenario.tacticalDirectorSummary))
                {
                    builder.AppendLine($"- Tactical director: {scenario.tacticalDirectorSummary}");
                }

                if (scenario.visual != null)
                {
                    builder.AppendLine($"- Screenshot: `{scenario.visual.screenshotPath}` luminance {scenario.visual.averageLuminance:0.000}, dark {scenario.visual.darkPixelRatio:P1}, bright {scenario.visual.brightPixelRatio:P1}, hot-pink {scenario.visual.hotPinkPixelRatio:P1}");
                }

                foreach (var failure in scenario.failures ?? Array.Empty<string>())
                {
                    builder.AppendLine($"- Failure: {failure}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string TrimAttributionForMarkdown(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            const int maxLength = 900;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + " ...";
        }

        public static bool LooksLikeAppleSilicon(string processorType, string operatingSystem)
        {
            var combined = $"{processorType} {operatingSystem}".ToLowerInvariant();
            return combined.Contains("apple") || combined.Contains("arm64") || combined.Contains("aarch64");
        }

        private static M140ScenarioSummary BaseScenario(string id, string displayName, string platformId, string buildKind, M140VisualValidationSummary visual)
        {
            return new M140ScenarioSummary
            {
                scenarioId = id ?? string.Empty,
                displayName = string.IsNullOrWhiteSpace(displayName) ? id ?? string.Empty : displayName,
                platformId = platformId ?? string.Empty,
                buildKind = buildKind ?? string.Empty,
                developmentCapture = string.Equals(buildKind, M140BuildKind.Development, StringComparison.OrdinalIgnoreCase),
                visual = visual
            };
        }

        private static void EvaluateScenario(M140ScenarioSummary summary, bool enforceTiming)
        {
            var failures = new List<string>();
            if (summary.visual != null && !summary.visual.passed)
            {
                failures.Add($"Screenshot visual validation failed: {string.Join("; ", summary.visual.failures ?? Array.Empty<string>())}");
            }

            if (summary.runtimeNavMeshFallbacks != 0)
            {
                failures.Add($"Runtime NavMesh fallback count {summary.runtimeNavMeshFallbacks}; expected 0.");
            }

            if (summary.normalTraversalColdCacheMissesAfterLoad != 0)
            {
                failures.Add($"Normal traversal cold-cache misses after branch load {summary.normalTraversalColdCacheMissesAfterLoad}; expected 0.");
            }

            if (summary.normalTraversalRoomRebuildCalls != 0 || summary.normalTraversalWarmCalls != 0)
            {
                failures.Add($"Normal traversal rebuild/warm calls {summary.normalTraversalRoomRebuildCalls}/{summary.normalTraversalWarmCalls}; expected 0 with branch live room cache.");
            }

            if (summary.hibernatedRoomActiveObjectLeaks != 0)
            {
                failures.Add($"Hibernated room active object leaks {summary.hibernatedRoomActiveObjectLeaks}; expected 0.");
            }

            if (summary.transitionCurtainMaxFramesAfterReady != 0 || summary.transitionCurtainMaxVisibleMilliseconds > 0.01f)
            {
                failures.Add($"Transition curtain visible during gameplay: after-ready frames {summary.transitionCurtainMaxFramesAfterReady}, visible {summary.transitionCurtainMaxVisibleMilliseconds:0.0} ms.");
            }

            if (summary.stagedRoomVisibleRendererFrames != 0)
            {
                failures.Add($"Staged room content rendered before reveal for {summary.stagedRoomVisibleRendererFrames} frame(s); expected 0.");
            }

            if (summary.poolWarmVisibleObjects != 0 || summary.poolWarmRootActiveErrors != 0 || summary.poolWarmActiveLeaks != 0)
            {
                failures.Add($"Pool warm visibility violations visible={summary.poolWarmVisibleObjects}, rootActive={summary.poolWarmRootActiveErrors}, leaks={summary.poolWarmActiveLeaks}; expected 0.");
            }

            if (summary.roomEntryVfxBeforeReveal != 0)
            {
                failures.Add($"Room-entry VFX/audio spawned before reveal {summary.roomEntryVfxBeforeReveal}; expected 0.");
            }

            if (summary.shaderMaterialFirstUseMissesAfterLoad != 0)
            {
                failures.Add($"Shader/material first-use misses after branch load {summary.shaderMaterialFirstUseMissesAfterLoad}; expected 0.");
            }

            if (summary.staleEnemyStateFailures != 0 || summary.staleRuntimePoolStateFailures != 0)
            {
                failures.Add($"Stale pooled state failures enemies={summary.staleEnemyStateFailures}, runtime={summary.staleRuntimePoolStateFailures}; expected 0.");
            }

            if (summary.poolActiveLeaks != 0)
            {
                failures.Add($"Pool active leaks {summary.poolActiveLeaks}; expected 0.");
            }

            if (summary.enemyPoolHardInstantiatesAfterWarmup != 0 || summary.runtimePoolHardInstantiatesAfterWarmup != 0)
            {
                failures.Add($"Post-warmup hard instantiates enemy/runtime {summary.enemyPoolHardInstantiatesAfterWarmup}/{summary.runtimePoolHardInstantiatesAfterWarmup}; expected 0.");
            }

            if (RequiresGameplayEvidence(summary.scenarioId))
            {
                if (summary.presentationFallbackVisuals != 0)
                {
                    failures.Add($"Gameplay presentation fallback primitive visuals {summary.presentationFallbackVisuals}; expected 0.");
                }

                if (summary.peakRenderers <= 0)
                {
                    failures.Add("Gameplay scenario captured no active renderers; screenshot was likely taken after cleanup or outside gameplay.");
                }

                if (!summary.observedCombatController)
                {
                    failures.Add("Gameplay scenario did not observe a RoomCombatController before screenshot capture.");
                }

                if (RequiresBranchEvidence(summary.scenarioId) && !summary.observedBranchSession)
                {
                    failures.Add("Branch-backed gameplay scenario did not observe a BranchSessionController before screenshot capture.");
                }
            }

            if (string.Equals(summary.scenarioId, "projectile_heavy_room", StringComparison.Ordinal) &&
                Math.Max(summary.peakProjectiles, summary.projectileActivePeak) < M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles)
            {
                failures.Add($"Projectile-heavy scenario observed {Math.Max(summary.peakProjectiles, summary.projectileActivePeak)} projectiles; expected at least {M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles}.");
            }

            if (string.Equals(summary.scenarioId, "boss_entry", StringComparison.Ordinal) &&
                !summary.observedBoss &&
                summary.bossLoadingStarts <= 0 &&
                summary.bossLoadingCompletions <= 0)
            {
                failures.Add("Boss-entry scenario did not observe boss runtime, boss loading, or boss activation evidence.");
            }

            if (enforceTiming && summary.timingAuthoritative && summary.frameP95Ms > FrameP95BudgetMs)
            {
                if (CanAcceptBorderlineFramePacing(summary))
                {
                    summary.note = AppendNote(
                        summary.note,
                        $"Accepted borderline frame-cap cadence jitter: frame p95 {summary.frameP95Ms:0.00} ms, deterministic gates clean.");
                }
                else
                {
                    failures.Add(
                        $"Frame p95 {summary.frameP95Ms:0.00} ms exceeds {FrameP95BudgetMs:0.0} ms. Borderline jitter acceptance requires p95 <= {FrameP95CleanCadenceJitterCeilingMs:0.00} ms, max <= {MaxFrameBudgetMs:0} ms, clean deterministic gates, and low CPU work/stage maxima.");
                }
            }

            if (enforceTiming && summary.timingAuthoritative && summary.frameMaxMs > MaxFrameBudgetMs)
            {
                failures.Add($"Frame max {summary.frameMaxMs:0.00} ms exceeds {MaxFrameBudgetMs:0} ms.");
            }

            summary.failures = failures.ToArray();
            summary.passed = failures.Count == 0;
        }

        public static bool RequiresGameplayEvidence(string scenarioId)
        {
            return !string.Equals(scenarioId, "boot_loading_screen", StringComparison.Ordinal);
        }

        private static bool RequiresBranchEvidence(string scenarioId)
        {
            return scenarioId is "branch_entry_loading" or
                "normal_traversal" or
                "return_to_previous_room" or
                "reward_room" or
                "boss_entry" or
                "long_run_smoke" or
                "next_branch_entry";
        }

        private static bool CanAcceptBorderlineFramePacing(M140ScenarioSummary summary)
        {
            if (summary == null ||
                summary.frameP95Ms > FrameP95CleanCadenceJitterCeilingMs ||
                summary.frameMaxMs > MaxFrameBudgetMs ||
                summary.gcMaxBytes > 1024)
            {
                return false;
            }

            if (summary.runtimeNavMeshFallbacks != 0 ||
                summary.normalTraversalColdCacheMissesAfterLoad != 0 ||
                summary.normalTraversalRoomRebuildCalls != 0 ||
                summary.normalTraversalWarmCalls != 0 ||
                summary.hibernatedRoomActiveObjectLeaks != 0 ||
                summary.transitionCurtainMaxFramesAfterReady != 0 ||
                summary.stagedRoomVisibleRendererFrames != 0 ||
                summary.poolWarmVisibleObjects != 0 ||
                summary.poolWarmRootActiveErrors != 0 ||
                summary.poolWarmActiveLeaks != 0 ||
                summary.presentationFallbackVisuals != 0 ||
                summary.roomEntryVfxBeforeReveal != 0 ||
                summary.shaderMaterialFirstUseMissesAfterLoad != 0 ||
                summary.staleEnemyStateFailures != 0 ||
                summary.staleRuntimePoolStateFailures != 0 ||
                summary.poolActiveLeaks != 0 ||
                summary.enemyPoolHardInstantiatesAfterWarmup != 0 ||
                summary.runtimePoolHardInstantiatesAfterWarmup != 0 ||
                summary.projectilePoolMisses != 0 ||
                summary.projectileHardInstantiates != 0)
            {
                return false;
            }

            if (summary.cpuWorkMetricSupported)
            {
                return summary.cpuWorkP95Ms <= CpuWorkP95BudgetMs;
            }

            return CpuStageSummaryLooksClean(summary.cpuStageSummary);
        }

        private static bool CanAcceptNestedM138TimingOnlyFailure(
            M138CombatScaleStressScenarioSummary stress,
            M140ScenarioSummary summary,
            bool enforceTiming)
        {
            if (!enforceTiming || stress == null || summary == null || stress.passed)
            {
                return false;
            }

            var stressFailures = stress.failures ?? Array.Empty<string>();
            if (stressFailures.Length == 0 ||
                !stressFailures.All(IsM138FrameP95OnlyFailure))
            {
                return false;
            }

            if ((summary.failures ?? Array.Empty<string>()).Length != 0)
            {
                return false;
            }

            return CanAcceptBorderlineFramePacing(summary);
        }

        private static bool IsM138FrameP95OnlyFailure(string failure)
        {
            return !string.IsNullOrWhiteSpace(failure) &&
                failure.StartsWith("Trusted frame p95 ", StringComparison.Ordinal) &&
                failure.Contains(" exceeds ", StringComparison.Ordinal);
        }

        private static bool IsIndependentCpuWorkMetric(
            M136PerformanceMetricSummary frameMetric,
            M136PerformanceMetricSummary mainThreadMetric)
        {
            if (mainThreadMetric == null ||
                !mainThreadMetric.supported ||
                mainThreadMetric.sampleCount <= 0)
            {
                return false;
            }

            if (frameMetric == null ||
                !frameMetric.supported ||
                frameMetric.sampleCount <= 0 ||
                frameMetric.p95 <= 0d)
            {
                return true;
            }

            // In several built-player captures Unity's "Main Thread" recorder tracks frame cadence
            // almost exactly, including frame-cap wait, so it is not a useful CPU-work gate.
            return mainThreadMetric.p95 < frameMetric.p95 * 0.9d;
        }

        private static bool CpuStageSummaryLooksClean(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return true;
            }

            var parts = summary.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                var marker = part.IndexOf("maxMs=", StringComparison.Ordinal);
                if (marker < 0)
                {
                    continue;
                }

                var start = marker + "maxMs=".Length;
                var end = start;
                while (end < part.Length && (char.IsDigit(part[end]) || part[end] == '.' || part[end] == '-'))
                {
                    end++;
                }

                if (end <= start)
                {
                    continue;
                }

                if (double.TryParse(part.Substring(start, end - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var maxMs) &&
                    maxMs > CpuStageCleanMaxMs)
                {
                    return false;
                }
            }

            return true;
        }

        private static string AppendNote(string current, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return current ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(current) ? note : $"{current} {note}";
        }

        private static void ApplyObjectEvidence(M140ScenarioSummary summary, M136LiveObjectCountSnapshot objectEvidence)
        {
            if (summary == null || objectEvidence == null)
            {
                return;
            }

            summary.peakActiveEnemies = Math.Max(summary.peakActiveEnemies, objectEvidence.activeEnemies);
            summary.peakProjectiles = Math.Max(summary.peakProjectiles, objectEvidence.activeProjectiles);
            summary.peakRenderers = Math.Max(summary.peakRenderers, objectEvidence.activeRenderers);
            summary.observedBranchSession |= objectEvidence.observedBranchSession;
            summary.observedCombatController |= objectEvidence.observedCombatController;
            summary.observedBoss |= objectEvidence.observedBoss;
        }

        private static string CombineAttribution(string current, string scenarioId, string next)
        {
            if (string.IsNullOrWhiteSpace(next))
            {
                return current ?? string.Empty;
            }

            var tagged = string.IsNullOrWhiteSpace(scenarioId) ? next : $"{scenarioId}: {next}";
            return string.IsNullOrWhiteSpace(current) ? tagged : $"{current}; {tagged}";
        }

        private static bool IsPostLoadTraversalAttribution(string row)
        {
            return !string.IsNullOrWhiteSpace(row) &&
                row.StartsWith("traversal|post-load-delta|", StringComparison.Ordinal);
        }

        private static string BuildAttributionSummary(IEnumerable<string> rows, Func<string, bool> predicate)
        {
            if (rows == null || predicate == null)
            {
                return string.Empty;
            }

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row) || !predicate(row))
                {
                    continue;
                }

                counts[row] = counts.TryGetValue(row, out var count) ? count + 1 : 1;
            }

            if (counts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("; ", counts.Select(pair => $"{pair.Value.ToString(CultureInfo.InvariantCulture)}x {pair.Key}"));
        }

        private static void AppendFailure(M140ScenarioSummary summary, string failure)
        {
            var failures = (summary.failures ?? Array.Empty<string>()).ToList();
            if (!string.IsNullOrWhiteSpace(failure))
            {
                failures.Add(failure);
            }

            summary.failures = failures.ToArray();
            summary.passed = failures.Count == 0;
        }

        private static M136PerformanceMetricSummary Metric(M136PerformanceScenarioResult result, string id)
        {
            return (result.metrics ?? Array.Empty<M136PerformanceMetricSummary>())
                .FirstOrDefault(metric => string.Equals(metric.id, id, StringComparison.Ordinal))
                ?? new M136PerformanceMetricSummary { id = id, supported = false };
        }

        private static void CreateParentDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
