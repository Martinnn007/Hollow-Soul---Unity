using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone5BeautifulGraphicsBudgetReport
    {
        public string lockId;
        public bool passed;
        public string[] failures = Array.Empty<string>();
        public string[] warnings = Array.Empty<string>();
        public Milestone5RenderProfileSummary[] profiles = Array.Empty<Milestone5RenderProfileSummary>();
        public string[] liveCaptureScenarios = Array.Empty<string>();
    }

    [Serializable]
    public sealed class Milestone5RenderProfileSummary
    {
        public string kind;
        public string assetPath;
        public string renderPipelinePath;
        public int targetFrameRate;
        public float renderScale;
        public bool hdr;
        public bool depthTexture;
        public bool opaqueTexture;
        public int shadowResolution;
        public float shadowDistance;
        public int cascadeCount;
        public bool additionalLightShadows;
        public int maxAdditionalLights;
        public int maxParticleSystems;
        public int maxVfx;
        public int maxLights;
        public float gpuP95BudgetMs;
    }

    public static class Milestone5BeautifulGraphicsBudgetAssetGenerator
    {
        public const string LockId = "M5_BEAUTIFUL_GRAPHICS_BUDGET";
        public const string RenderProfileDirectory = "Assets/_Hollow/Data/Platform/RenderProfiles";
        public const string RenderPipelineDirectory = "Assets/_Hollow/Data/Platform/RenderPipelines";
        public const string DevCoolProfilePath = RenderProfileDirectory + "/RenderProfile_DevCool.asset";
        public const string WindowsQualityProfilePath = RenderProfileDirectory + "/RenderProfile_WindowsQuality.asset";
        public const string VisionOSBoundedProfilePath = RenderProfileDirectory + "/RenderProfile_VisionOSBounded.asset";
        public const string VisionOSImmersiveProfilePath = RenderProfileDirectory + "/RenderProfile_VisionOSImmersive.asset";
        public const string DevCoolPipelinePath = RenderPipelineDirectory + "/URP_DevCool.asset";
        public const string WindowsQualityPipelinePath = RenderPipelineDirectory + "/URP_WindowsQuality.asset";
        public const string VisionOSBoundedPipelinePath = RenderPipelineDirectory + "/URP_VisionOSBounded.asset";
        public const string VisionOSImmersivePipelinePath = RenderPipelineDirectory + "/URP_VisionOSImmersive.asset";
        public const string ReportJsonPath = "output/reports/m5_beautiful_graphics_budget.json";
        public const string ReportMarkdownPath = "output/reports/m5_beautiful_graphics_budget.md";

        private const string SourcePcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        private const string SourceMobilePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";

        [MenuItem("Hollow/Generation/Generate Milestone 5 Beautiful Graphics Budget")]
        public static void Generate()
        {
            Directory.CreateDirectory(RenderProfileDirectory);
            Directory.CreateDirectory(RenderPipelineDirectory);
            var devPipeline = CreatePipeline(DevCoolPipelinePath, SourcePcPipelinePath, 1f, true, true, true, 2048, 45f, 2, false, 2);
            var windowsPipeline = CreatePipeline(WindowsQualityPipelinePath, SourcePcPipelinePath, 1f, true, true, true, 2048, 50f, 2, true, 4);
            var boundedPipeline = CreatePipeline(VisionOSBoundedPipelinePath, SourceMobilePipelinePath, 0.9f, true, false, false, 1024, 35f, 1, false, 2);
            var immersivePipeline = CreatePipeline(VisionOSImmersivePipelinePath, SourceMobilePipelinePath, 0.85f, true, false, false, 1024, 30f, 1, false, 1);

            var dev = CreateProfile(DevCoolProfilePath, HollowRenderProfileKind.DevCool, devPipeline, 45, 1f, true, true, true, 2048, 45f, 2, false, 2, true, 56, 40, 10, 2048, 1024, 22.22f);
            var windows = CreateProfile(WindowsQualityProfilePath, HollowRenderProfileKind.WindowsQuality, windowsPipeline, 60, 1f, true, true, true, 2048, 50f, 2, true, 4, true, 72, 56, 14, 2048, 1024, 16.67f);
            var bounded = CreateProfile(VisionOSBoundedProfilePath, HollowRenderProfileKind.VisionOSBounded, boundedPipeline, 90, 0.9f, true, false, false, 1024, 35f, 1, false, 2, false, 48, 32, 8, 1024, 768, 11.11f);
            var immersive = CreateProfile(VisionOSImmersiveProfilePath, HollowRenderProfileKind.VisionOSImmersive, immersivePipeline, 90, 0.85f, true, false, false, 1024, 30f, 1, false, 1, false, 40, 28, 6, 1024, 768, 11.11f);

            AssignRenderProfile(Milestone10AssetGenerator.WindowsProfilePath, windows);
            AssignRenderProfile(Milestone10AssetGenerator.BoundedProfilePath, bounded);
            AssignRenderProfile(Milestone10AssetGenerator.ImmersiveProfilePath, immersive);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReports(BuildReport());
            Debug.Log("Generated Milestone 5 beautiful graphics budget profiles and report.");
        }

        public static Milestone5BeautifulGraphicsBudgetReport BuildReport()
        {
            var failures = new List<string>();
            var warnings = new List<string>();
            var profiles = new[]
            {
                LoadProfile(DevCoolProfilePath, HollowRenderProfileKind.DevCool, failures),
                LoadProfile(WindowsQualityProfilePath, HollowRenderProfileKind.WindowsQuality, failures),
                LoadProfile(VisionOSBoundedProfilePath, HollowRenderProfileKind.VisionOSBounded, failures),
                LoadProfile(VisionOSImmersiveProfilePath, HollowRenderProfileKind.VisionOSImmersive, failures)
            };

            ValidateDistinctBudgets(profiles, failures);
            ValidatePlatformMappings(failures);
            warnings.AddRange(Milestone5BeautifulGraphicsBudgetAudit.CollectProjectWarnings(profiles));

            return new Milestone5BeautifulGraphicsBudgetReport
            {
                lockId = LockId,
                passed = failures.Count == 0,
                failures = failures.ToArray(),
                warnings = warnings.ToArray(),
                profiles = BuildSummaries(profiles),
                liveCaptureScenarios = new[]
                {
                    "normal_combat",
                    "enemy_stress_30",
                    "projectile_heavy_room",
                    "anchor_boss_smoke",
                    "reward_room_pickups"
                }
            };
        }

        public static void WriteReports(Milestone5BeautifulGraphicsBudgetReport report)
        {
            Directory.CreateDirectory("output/reports");
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, prettyPrint: true));
            File.WriteAllText(ReportMarkdownPath, BuildMarkdown(report));
        }

        private static RenderPipelineAsset CreatePipeline(
            string targetPath,
            string sourcePath,
            float renderScale,
            bool hdr,
            bool depthTexture,
            bool opaqueTexture,
            int shadowResolution,
            float shadowDistance,
            int cascadeCount,
            bool additionalLightShadows,
            int maxAdditionalLights)
        {
            if (!File.Exists(targetPath))
            {
                AssetDatabase.CopyAsset(sourcePath, targetPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(targetPath);
            var serialized = new SerializedObject(pipeline);
            Set(serialized, "m_RenderScale", renderScale);
            Set(serialized, "m_SupportsHDR", hdr);
            Set(serialized, "m_RequireDepthTexture", depthTexture);
            Set(serialized, "m_RequireOpaqueTexture", opaqueTexture);
            Set(serialized, "m_MainLightShadowmapResolution", shadowResolution);
            Set(serialized, "m_ShadowDistance", shadowDistance);
            Set(serialized, "m_ShadowCascadeCount", cascadeCount);
            Set(serialized, "m_AdditionalLightShadowsSupported", additionalLightShadows);
            Set(serialized, "m_AdditionalLightsPerObjectLimit", maxAdditionalLights);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static HollowRenderProfileDefinition CreateProfile(
            string path,
            HollowRenderProfileKind kind,
            RenderPipelineAsset pipeline,
            int targetFrameRate,
            float renderScale,
            bool hdr,
            bool depthTexture,
            bool opaqueTexture,
            int shadowResolution,
            float shadowDistance,
            int cascadeCount,
            bool additionalLightShadows,
            int maxAdditionalLights,
            bool ssao,
            int maxParticleSystems,
            int maxVfx,
            int maxLights,
            int worldTextureMax,
            int uiSpriteMax,
            float gpuP95BudgetMs)
        {
            var profile = AssetDatabase.LoadAssetAtPath<HollowRenderProfileDefinition>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<HollowRenderProfileDefinition>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.Configure(kind, pipeline, targetFrameRate, 0, renderScale, hdr, depthTexture, opaqueTexture, shadowResolution, shadowDistance, cascadeCount, additionalLightShadows, maxAdditionalLights, ssao, maxParticleSystems, maxVfx, maxLights, worldTextureMax, uiSpriteMax, gpuP95BudgetMs);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void AssignRenderProfile(string polishPath, HollowRenderProfileDefinition renderProfile)
        {
            var polish = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(polishPath);
            if (polish == null)
            {
                return;
            }

            polish.ConfigureRenderProfile(renderProfile);
            EditorUtility.SetDirty(polish);
        }

        private static HollowRenderProfileDefinition LoadProfile(string path, HollowRenderProfileKind expectedKind, List<string> failures)
        {
            var profile = AssetDatabase.LoadAssetAtPath<HollowRenderProfileDefinition>(path);
            if (profile == null)
            {
                failures.Add($"Missing render profile `{path}`.");
                return null;
            }

            if (profile.ProfileKind != expectedKind)
            {
                failures.Add($"{path} has kind {profile.ProfileKind}, expected {expectedKind}.");
            }

            if (profile.RenderPipelineAsset == null)
            {
                failures.Add($"{path} has no render pipeline asset.");
            }

            return profile;
        }

        private static void ValidateDistinctBudgets(IReadOnlyList<HollowRenderProfileDefinition> profiles, List<string> failures)
        {
            var dev = profiles[0];
            var windows = profiles[1];
            var bounded = profiles[2];
            var immersive = profiles[3];
            if (dev != null && windows != null)
            {
                if (dev.RenderScale != windows.RenderScale || dev.TargetFrameRate >= windows.TargetFrameRate)
                {
                    failures.Add("Dev Cool must keep Windows render scale while using a lower FPS cap.");
                }

                if (dev.AdditionalLightShadows && windows.AdditionalLightShadows)
                {
                    failures.Add("Dev Cool must not keep every expensive Windows light-shadow setting enabled.");
                }
            }

            if (bounded != null && windows != null && RenderPipelinePath(bounded) == RenderPipelinePath(windows))
            {
                failures.Add("VisionOS Bounded must not point at the Windows Quality render pipeline.");
            }

            if (immersive != null && windows != null && RenderPipelinePath(immersive) == RenderPipelinePath(windows))
            {
                failures.Add("VisionOS Immersive must not point at the Windows Quality render pipeline.");
            }

            if (bounded != null && bounded.AdditionalLightShadows)
            {
                failures.Add("VisionOS Bounded must disable additional-light shadows.");
            }

            if (immersive != null && (immersive.AdditionalLightShadows || immersive.ShadowCascadeCount > 1 || immersive.RenderScale > 0.85f + 0.001f))
            {
                failures.Add("VisionOS Immersive must use reduced render scale, one cascade, and no additional-light shadows.");
            }
        }

        private static void ValidatePlatformMappings(List<string> failures)
        {
            ValidateMapping(Milestone10AssetGenerator.WindowsProfilePath, WindowsQualityProfilePath, failures);
            ValidateMapping(Milestone10AssetGenerator.BoundedProfilePath, VisionOSBoundedProfilePath, failures);
            ValidateMapping(Milestone10AssetGenerator.ImmersiveProfilePath, VisionOSImmersiveProfilePath, failures);
        }

        private static void ValidateMapping(string polishPath, string expectedRenderProfilePath, List<string> failures)
        {
            var polish = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(polishPath);
            var expected = AssetDatabase.LoadAssetAtPath<HollowRenderProfileDefinition>(expectedRenderProfilePath);
            if (polish == null || expected == null || polish.RenderProfile != expected)
            {
                failures.Add($"{polishPath} is not mapped to {expectedRenderProfilePath}.");
            }
        }

        private static Milestone5RenderProfileSummary[] BuildSummaries(IReadOnlyList<HollowRenderProfileDefinition> profiles)
        {
            var summaries = new List<Milestone5RenderProfileSummary>();
            foreach (var profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                summaries.Add(new Milestone5RenderProfileSummary
                {
                    kind = profile.ProfileKind.ToString(),
                    assetPath = AssetDatabase.GetAssetPath(profile),
                    renderPipelinePath = RenderPipelinePath(profile),
                    targetFrameRate = profile.TargetFrameRate,
                    renderScale = profile.RenderScale,
                    hdr = profile.SupportsHdr,
                    depthTexture = profile.RequiresDepthTexture,
                    opaqueTexture = profile.RequiresOpaqueTexture,
                    shadowResolution = profile.MainLightShadowResolution,
                    shadowDistance = profile.ShadowDistance,
                    cascadeCount = profile.ShadowCascadeCount,
                    additionalLightShadows = profile.AdditionalLightShadows,
                    maxAdditionalLights = profile.MaxAdditionalLights,
                    maxParticleSystems = profile.MaxActiveParticleSystems,
                    maxVfx = profile.MaxActiveVfx,
                    maxLights = profile.MaxActiveLights,
                    gpuP95BudgetMs = profile.GpuFrameP95BudgetMs
                });
            }

            return summaries.ToArray();
        }

        private static string RenderPipelinePath(HollowRenderProfileDefinition profile)
        {
            return profile != null && profile.RenderPipelineAsset != null ? AssetDatabase.GetAssetPath(profile.RenderPipelineAsset) : string.Empty;
        }

        private static void Set(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void Set(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void Set(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static string BuildMarkdown(Milestone5BeautifulGraphicsBudgetReport report)
        {
            var lines = new List<string>
            {
                "# M5 Beautiful Graphics Budget",
                string.Empty,
                $"- Result: {(report.passed ? "PASSED" : "FAILED")}",
                $"- Lock: `{report.lockId}`",
                string.Empty,
                "| Profile | FPS | Render Scale | Shadows | Add Light Shadows | GPU p95 Budget |",
                "| --- | ---: | ---: | --- | --- | ---: |"
            };

            foreach (var profile in report.profiles ?? Array.Empty<Milestone5RenderProfileSummary>())
            {
                lines.Add($"| {profile.kind} | {profile.targetFrameRate} | {profile.renderScale:0.##} | {profile.shadowResolution}/{profile.shadowDistance:0.#}m/{profile.cascadeCount}c | {profile.additionalLightShadows} | {profile.gpuP95BudgetMs:0.##}ms |");
            }

            lines.Add(string.Empty);
            lines.Add("## Live Capture Scenarios");
            foreach (var scenario in report.liveCaptureScenarios ?? Array.Empty<string>())
            {
                lines.Add($"- `{scenario}`");
            }

            if (report.failures != null && report.failures.Length > 0)
            {
                lines.Add(string.Empty);
                lines.Add("## Failures");
                foreach (var failure in report.failures)
                {
                    lines.Add($"- {failure}");
                }
            }

            if (report.warnings != null && report.warnings.Length > 0)
            {
                lines.Add(string.Empty);
                lines.Add("## Audit Warnings");
                foreach (var warning in report.warnings)
                {
                    lines.Add($"- {warning}");
                }
            }

            return string.Join("\n", lines) + "\n";
        }
    }
}
