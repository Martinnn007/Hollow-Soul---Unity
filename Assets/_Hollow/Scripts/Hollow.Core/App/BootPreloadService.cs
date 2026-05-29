using System;
using System.Collections;
using Hollow.Core.Diagnostics;
using UnityEngine;

namespace Hollow.Core.App
{
    [Serializable]
    public sealed class BootPreloadSettings
    {
        [SerializeField] private bool preloadResources = true;
        [SerializeField] private bool warmShaderCollections = true;
        [SerializeField] private HollowShaderWarmupProfile shaderWarmupProfile;
        [SerializeField] private string shaderWarmupProfileResourcePath = BootPreloadService.DefaultShaderWarmupProfileResourcePath;
        [SerializeField] private bool warmPrimitivePools = true;
        [SerializeField] private int primitiveWarmCount = 4;
        [SerializeField] private int workPerFrame = 6;
        [SerializeField] private string[] resourcePreloadPaths = BootPreloadService.DefaultResourcePreloadPaths;

        public bool PreloadResources => preloadResources;

        public bool WarmShaderCollections => warmShaderCollections;

        public HollowShaderWarmupProfile ShaderWarmupProfile => shaderWarmupProfile;

        public string ShaderWarmupProfileResourcePath => shaderWarmupProfileResourcePath ?? string.Empty;

        public bool WarmPrimitivePools => warmPrimitivePools;

        public int PrimitiveWarmCount => Mathf.Max(0, primitiveWarmCount);

        public int WorkPerFrame => Mathf.Max(1, workPerFrame);

        public string[] ResourcePreloadPaths => resourcePreloadPaths ?? Array.Empty<string>();

        public HollowShaderWarmupProfile ResolveShaderWarmupProfile()
        {
            if (shaderWarmupProfile != null)
            {
                return shaderWarmupProfile;
            }

            var resourcePath = ShaderWarmupProfileResourcePath;
            return string.IsNullOrWhiteSpace(resourcePath)
                ? null
                : Resources.Load<HollowShaderWarmupProfile>(resourcePath);
        }

        public static BootPreloadSettings Default()
        {
            return new BootPreloadSettings();
        }

        public void ConfigureForTests(
            bool nextPreloadResources,
            bool nextWarmShaders,
            bool nextWarmPrimitivePools,
            int nextPrimitiveWarmCount = 0,
            int nextWorkPerFrame = 6,
            string[] nextResourcePreloadPaths = null,
            HollowShaderWarmupProfile nextShaderWarmupProfile = null,
            string nextShaderWarmupProfileResourcePath = null)
        {
            preloadResources = nextPreloadResources;
            warmShaderCollections = nextWarmShaders;
            shaderWarmupProfile = nextShaderWarmupProfile;
            shaderWarmupProfileResourcePath = nextShaderWarmupProfileResourcePath ?? string.Empty;
            warmPrimitivePools = nextWarmPrimitivePools;
            primitiveWarmCount = Mathf.Max(0, nextPrimitiveWarmCount);
            workPerFrame = Mathf.Max(1, nextWorkPerFrame);
            resourcePreloadPaths = nextResourcePreloadPaths ?? Array.Empty<string>();
        }
    }

    public readonly struct BootPreloadStageProgress
    {
        public BootPreloadStageProgress(string stage, float progress01)
        {
            Stage = stage ?? string.Empty;
            Progress01 = Mathf.Clamp01(progress01);
        }

        public string Stage { get; }

        public float Progress01 { get; }
    }

    public sealed class BootPreloadReport
    {
        public int StageCount { get; internal set; }

        public int ResourceLoadCount { get; internal set; }

        public int WarmRequestCount { get; internal set; }

        public int WarmCompletionCount { get; internal set; }

        public int ShaderWarmAttempts { get; internal set; }

        public int ShaderWarmCollections { get; internal set; }

        public int ShaderWarmSuccesses { get; internal set; }

        public int ShaderWarmMisses { get; internal set; }

        public float ShaderWarmMaxMilliseconds { get; internal set; }

        public string LastStage { get; internal set; } = string.Empty;
    }

    public sealed class BootPreloadService
    {
        public static readonly string[] DefaultResourcePreloadPaths =
        {
            "Hollow",
            "UI/Hud",
            "UI/Minimap"
        };

        public const string DefaultShaderWarmupProfileResourcePath = "Hollow/HollowBootShaderWarmupProfile";

        public IEnumerator Run(BootPreloadSettings settings, Action<BootPreloadStageProgress> progress, BootPreloadReport report = null)
        {
            settings ??= BootPreloadSettings.Default();
            report ??= new BootPreloadReport();

            yield return Stage("Initializing platform", 0.05f, report, progress);
            Application.runInBackground = true;

            if (settings.PreloadResources)
            {
                var paths = settings.ResourcePreloadPaths;
                for (var index = 0; index < paths.Length; index++)
                {
                    var path = paths[index];
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    yield return Stage($"Loading resources: {path}", Mathf.Lerp(0.12f, 0.58f, (index + 1f) / Mathf.Max(1, paths.Length)), report, progress);
                    var loaded = Resources.LoadAll<UnityEngine.Object>(path);
                    report.ResourceLoadCount += loaded.Length;
                    M136PerformanceOperationCounters.ReportBootPreloadResourceLoad(loaded.Length);
                    yield return null;
                }
            }

            if (settings.WarmPrimitivePools)
            {
                yield return Stage("Warming shared fallback pools", 0.66f, report, progress);
                yield return WarmPrimitive("boot.primitive.quad", PrimitiveType.Quad, settings, report);
                yield return WarmPrimitive("boot.primitive.cube", PrimitiveType.Cube, settings, report);
                yield return WarmPrimitive("boot.primitive.sphere", PrimitiveType.Sphere, settings, report);
            }

            if (settings.WarmShaderCollections)
            {
                yield return WarmShaderCollections(settings, report, progress);
            }

            yield return Stage("Validating startup", 0.94f, report, progress);
            yield return null;
            yield return Stage("Ready", 1f, report, progress);
        }

        private static IEnumerator WarmPrimitive(string key, PrimitiveType primitiveType, BootPreloadSettings settings, BootPreloadReport report)
        {
            if (settings.PrimitiveWarmCount <= 0 || !Application.isPlaying)
            {
                yield break;
            }

            report.WarmRequestCount++;
            M136PerformanceOperationCounters.ReportBootPreloadWarmRequest();
            yield return HollowRuntimePool.WarmPrimitivePool(key, primitiveType, settings.PrimitiveWarmCount, settings.WorkPerFrame);
            report.WarmCompletionCount++;
            M136PerformanceOperationCounters.ReportBootPreloadWarmCompletion();
        }

        private static IEnumerator WarmShaderCollections(BootPreloadSettings settings, BootPreloadReport report, Action<BootPreloadStageProgress> progress)
        {
            var profile = settings.ResolveShaderWarmupProfile();
            if (profile == null)
            {
                report.ShaderWarmMisses++;
                M136PerformanceOperationCounters.ReportBootPreloadShaderWarmMiss();
                yield break;
            }

            if (!profile.EnabledForBoot)
            {
                yield break;
            }

            yield return Stage("Warming rendering", 0.82f, report, progress);
            var collections = profile.Collections;
            report.ShaderWarmCollections += profile.CollectionCount;
            M136PerformanceOperationCounters.ReportBootPreloadShaderWarmCollectionCount(profile.CollectionCount);

            for (var index = 0; index < collections.Length; index++)
            {
                var collection = collections[index];
                if (collection == null)
                {
                    report.ShaderWarmMisses++;
                    M136PerformanceOperationCounters.ReportBootPreloadShaderWarmMiss();
                    continue;
                }

                var start = Time.realtimeSinceStartup;
                report.ShaderWarmAttempts++;
                M136PerformanceOperationCounters.ReportBootPreloadShaderWarmAttempt();
                try
                {
                    collection.WarmUp();
                    var elapsed = (Time.realtimeSinceStartup - start) * 1000f;
                    report.ShaderWarmSuccesses++;
                    if (elapsed > report.ShaderWarmMaxMilliseconds)
                    {
                        report.ShaderWarmMaxMilliseconds = elapsed;
                    }

                    M136PerformanceOperationCounters.ReportBootPreloadShaderWarmSuccess(elapsed);
                }
                catch (Exception exception)
                {
                    report.ShaderWarmMisses++;
                    M136PerformanceOperationCounters.ReportBootPreloadShaderWarmMiss();
                    Debug.LogWarning($"Shader warmup collection failed: {collection.name}. {exception.GetType().Name}: {exception.Message}");
                }

                yield return null;
            }
        }

        private static IEnumerator Stage(string stage, float progress01, BootPreloadReport report, Action<BootPreloadStageProgress> progress)
        {
            report.StageCount++;
            report.LastStage = stage ?? string.Empty;
            var start = Time.realtimeSinceStartup;
            progress?.Invoke(new BootPreloadStageProgress(stage, progress01));
            yield return null;
            M136PerformanceOperationCounters.ReportBootLoadingStage((Time.realtimeSinceStartup - start) * 1000f);
        }
    }
}
