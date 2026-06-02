using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Presentation;
using Hollow.Rooms;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hollow.Performance
{
    public enum LightingRenderAuditMarkerKind
    {
        Prop = 0,
        Enemy = 1,
        Projectile = 2,
        Vfx = 3
    }

    public sealed class LightingRenderAuditMarker : MonoBehaviour
    {
        [SerializeField] private LightingRenderAuditMarkerKind kind;

        public LightingRenderAuditMarkerKind Kind => kind;

        public void Configure(LightingRenderAuditMarkerKind nextKind)
        {
            kind = nextKind;
        }
    }

    public sealed class LightingRenderAuditOrbitMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 axis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 90f;

        public void Configure(Vector3 nextAxis, float nextDegreesPerSecond)
        {
            axis = nextAxis.sqrMagnitude > 0.0001f ? nextAxis.normalized : Vector3.up;
            degreesPerSecond = nextDegreesPerSecond;
        }

        private void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.unscaledDeltaTime, Space.Self);
        }
    }

    public sealed class LightingRenderAuditRunner : MonoBehaviour
    {
        private const float WarmupSeconds = 0.35f;
        private const float SampleSeconds = 1.75f;
        private static bool startedFromCommandLine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromCommandLine()
        {
            if (startedFromCommandLine)
            {
                return;
            }

            if (!LightingRenderAuditOptions.TryParse(Environment.GetCommandLineArgs(), out var options))
            {
                return;
            }

            startedFromCommandLine = true;
            var host = new GameObject("LightingRenderAuditRunner");
            DontDestroyOnLoad(host);
            host.AddComponent<LightingRenderAuditRunner>().StartCoroutine(RunAndExit(options));
        }

        public static IEnumerator RunAllScenarios(LightingRenderAuditOptions options, Action<LightingRenderAuditReport> onComplete = null)
        {
            var host = new GameObject("LightingRenderAuditRunner.Manual");
            var runner = host.AddComponent<LightingRenderAuditRunner>();
            LightingRenderAuditReport report = null;
            yield return runner.Run(options, next => report = next);
            Object.Destroy(host);
            onComplete?.Invoke(report);
        }

        private static IEnumerator RunAndExit(LightingRenderAuditOptions options)
        {
            var runner = FindAnyObjectByType<LightingRenderAuditRunner>();
            LightingRenderAuditReport report = null;
            yield return runner.Run(options, next => report = next);
            if (options.autoExit)
            {
#if !UNITY_EDITOR
                Application.Quit(report != null && report.passed ? 0 : 1);
#endif
            }
        }

        private IEnumerator Run(LightingRenderAuditOptions options, Action<LightingRenderAuditReport> onComplete)
        {
            options ??= LightingRenderAuditOptions.Smoke();
            Directory.CreateDirectory(options.ResolvedOutputRoot);
            var summaries = new List<LightingRenderAuditScenarioSummary>();
            using var fpsOverride = new M136CaptureFpsOverride(true, options.targetFrameRate);
            var scenarioIds = options.scenarioIds ?? Array.Empty<string>();
            for (var index = 0; index < scenarioIds.Length; index++)
            {
                var scenario = LightingRenderAuditScenarioPolicy.Find(scenarioIds[index]);
                if (scenario == null)
                {
                    summaries.Add(new LightingRenderAuditScenarioSummary
                    {
                        scenarioId = scenarioIds[index] ?? string.Empty,
                        displayName = scenarioIds[index] ?? string.Empty,
                        passed = false,
                        failures = new[] { "Scenario is not in the lighting render audit manifest." }
                    });
                    continue;
                }

                LightingRenderAuditScenarioSummary summary = null;
                yield return RunScenario(scenario, options, index, next => summary = next);
                if (summary != null)
                {
                    summaries.Add(summary);
                }
            }

            var report = LightingRenderAuditReportGenerator.BuildReport(options, summaries);
            if (options.writeReports)
            {
                LightingRenderAuditReportGenerator.WriteReport(report, options.JsonReportPath, options.MarkdownReportPath);
            }

            onComplete?.Invoke(report);
        }

        private static IEnumerator RunScenario(
            LightingRenderAuditScenarioDefinition scenario,
            LightingRenderAuditOptions options,
            int scenarioIndex,
            Action<LightingRenderAuditScenarioSummary> onComplete)
        {
            if (scenario.branchTraversalPrewarm)
            {
                yield return RunBranchTraversalPrewarmScenario(scenario, options, scenarioIndex, onComplete);
                yield break;
            }

            var root = new GameObject("LightingRenderAudit_" + scenario.id);
            var createdMaterials = new List<Material>();
            var samples = new List<LightingRenderAuditFrameSample>();
            var frameMs = new List<double>();
            var mainMs = new List<double>();
            var renderMs = new List<double>();
            var gpuMs = new List<double>();
            var gcKb = new List<double>();
            var mainRecorder = default(ProfilerRecorder);
            var renderRecorder = default(ProfilerRecorder);
            var gcRecorder = default(ProfilerRecorder);
            RoomLightingController controller = null;
            Camera camera = null;
            var screenshotPath = Path.Combine(options.ResolvedOutputRoot, scenario.id + ".png");
            var transitionBiomes = new[]
            {
                Hollow.Data.Definitions.RoomBiomeIds.HollowThreshold,
                Hollow.Data.Definitions.RoomBiomeIds.VerdantRuins,
                Hollow.Data.Definitions.RoomBiomeIds.CorruptedAshenShrine
            };

            try
            {
                DontDestroyOnLoad(root);
                root.transform.position = new Vector3(scenarioIndex * 32f, 0f, 0f);
                controller = root.AddComponent<RoomLightingController>();
                controller.ApplyBiome(scenario.biomeId, force: true);
                camera = CreateCamera(root.transform, scenario.id);
                CreateSceneGeometry(root.transform, scenario, createdMaterials);
                yield return null;

                controller.ApplyBiome(scenario.biomeId, force: true);
                TryStartRecorder(ref mainRecorder, ProfilerCategory.Internal, "Main Thread");
                TryStartRecorder(ref renderRecorder, ProfilerCategory.Internal, "Render Thread");
                TryStartRecorder(ref gcRecorder, ProfilerCategory.Memory, "GC Allocated In Frame");

                var startTime = Time.unscaledTime;
                var warmupEnd = startTime + WarmupSeconds;
                var sampleEnd = warmupEnd + SampleSeconds;
                var transitionIndex = 0;
                var nextTransitionTime = warmupEnd;
                while (Time.unscaledTime < sampleEnd)
                {
                    if (scenario.transitionSweep && Time.unscaledTime >= nextTransitionTime)
                    {
                        var biome = transitionBiomes[transitionIndex % transitionBiomes.Length];
                        controller.ApplyBiome(biome, force: true);
                        transitionIndex++;
                        nextTransitionTime = Time.unscaledTime + 0.35f;
                    }
                    else
                    {
                        controller.ApplyBiome(controller.AppliedBiomeId, force: false);
                    }

                    if (Time.unscaledTime >= warmupEnd)
                    {
                        var sample = CaptureSample(root.transform, samples.Count, Time.unscaledTime - warmupEnd, mainRecorder, renderRecorder, gcRecorder);
                        samples.Add(sample);
                        frameMs.Add(sample.frameTimeMs);
                        if (sample.mainThreadMs > 0d)
                        {
                            mainMs.Add(sample.mainThreadMs);
                        }

                        if (sample.renderThreadMs > 0d)
                        {
                            renderMs.Add(sample.renderThreadMs);
                        }

                        if (sample.gpuFrameMs > 0d)
                        {
                            gpuMs.Add(sample.gpuFrameMs);
                        }

                        if (sample.gcAllocatedKb >= 0d)
                        {
                            gcKb.Add(sample.gcAllocatedKb);
                        }
                    }

                    yield return null;
                }

                DisposeRecorder(ref mainRecorder);
                DisposeRecorder(ref renderRecorder);
                DisposeRecorder(ref gcRecorder);
                yield return CaptureScreenshot(screenshotPath);
                controller.ApplyBiome(controller.AppliedBiomeId, force: true);
                var finalSnapshot = BiomeLightingDiagnostics.LastSnapshot;
                var visual = M140VisualScreenshotValidator.Validate(screenshotPath, scenario.id);
                var summary = BuildSummary(
                    scenario,
                    controller,
                    finalSnapshot,
                    screenshotPath,
                    visual,
                    samples,
                    frameMs,
                    mainMs,
                    renderMs,
                    gpuMs,
                    gcKb);
                onComplete?.Invoke(summary);
            }
            finally
            {
                DisposeRecorder(ref mainRecorder);
                DisposeRecorder(ref renderRecorder);
                DisposeRecorder(ref gcRecorder);
                for (var index = 0; index < createdMaterials.Count; index++)
                {
                    if (createdMaterials[index] != null)
                    {
                        Object.Destroy(createdMaterials[index]);
                    }
                }

                Object.Destroy(root);
            }
        }

        private static IEnumerator RunBranchTraversalPrewarmScenario(
            LightingRenderAuditScenarioDefinition scenario,
            LightingRenderAuditOptions options,
            int scenarioIndex,
            Action<LightingRenderAuditScenarioSummary> onComplete)
        {
            var root = new GameObject("LightingRenderAudit_" + scenario.id);
            var cache = new BranchLiveRoomCache();
            var createdMaterials = new List<Material>();
            var samples = new List<LightingRenderAuditFrameSample>();
            var frameMs = new List<double>();
            var mainMs = new List<double>();
            var renderMs = new List<double>();
            var gpuMs = new List<double>();
            var gcKb = new List<double>();
            var evidenceFailures = new List<string>();
            var mainRecorder = default(ProfilerRecorder);
            var renderRecorder = default(ProfilerRecorder);
            var gcRecorder = default(ProfilerRecorder);
            var screenshotPath = Path.Combine(options.ResolvedOutputRoot, scenario.id + ".png");
            RoomLightingController finalController = null;
            var allRoomsPreparedBeforeReveal = true;
            var allGlobalsAppliedBeforeReveal = true;
            var latestPreparedFrame = -1;
            var latestGlobalAppliedFrame = -1;
            var latestRevealFrame = -1;

            try
            {
                DontDestroyOnLoad(root);
                root.transform.position = new Vector3(scenarioIndex * 32f, 0f, 0f);
                CreateCamera(root.transform, scenario.id);
                var rooms = CreateAuditBranchRooms();
                var assets = new Dictionary<string, ImportedRoomRuntimeAsset>(StringComparer.Ordinal)
                {
                    [BranchRoomId.Origin.Value] = CreateAuditRoomAsset("audit_branch_hollow_threshold", RoomBiomeIds.HollowThreshold),
                    [BranchRoomId.North.Value] = CreateAuditRoomAsset("audit_branch_verdant_ruins", RoomBiomeIds.VerdantRuins),
                    [BranchRoomId.East.Value] = CreateAuditRoomAsset("audit_branch_corrupted_ashen_shrine", RoomBiomeIds.CorruptedAshenShrine)
                };

                yield return cache.BuildBranchRooms(
                    rooms,
                    room => assets[room.Id.Value],
                    root.transform,
                    (room, runtimeRoot) =>
                    {
                        CreateMarkerObjects(runtimeRoot.transform, LightingRenderAuditMarkerKind.Prop, Mathf.Max(1, scenario.propLightCount), PrimitiveType.Cube, new Color(0.32f, 0.27f, 0.18f), createdMaterials, -2.2f, -1.4f, 0.42f);
                        CreateMarkerObjects(runtimeRoot.transform, LightingRenderAuditMarkerKind.Enemy, Mathf.Max(0, scenario.enemyCount), PrimitiveType.Capsule, new Color(0.72f, 0.14f, 0.11f), createdMaterials, -2.4f, 0.1f, 0.5f);
                        CreateMarkerObjects(runtimeRoot.transform, LightingRenderAuditMarkerKind.Projectile, Mathf.Max(0, scenario.projectileCount), PrimitiveType.Sphere, new Color(1f, 0.58f, 0.18f), createdMaterials, -2.4f, 1.3f, 0.2f);
                        CreateVfxObjects(runtimeRoot.transform, Mathf.Max(0, scenario.vfxCount), createdMaterials);
                        CreateBiomeEmitters(runtimeRoot.transform, scenario);
                    });

                TryStartRecorder(ref mainRecorder, ProfilerCategory.Internal, "Main Thread");
                TryStartRecorder(ref renderRecorder, ProfilerCategory.Internal, "Render Thread");
                TryStartRecorder(ref gcRecorder, ProfilerCategory.Memory, "GC Allocated In Frame");

                for (var index = 0; index < rooms.Length; index++)
                {
                    var room = rooms[index];
                    if (!cache.TryGetRoom(room.Id, out var instance))
                    {
                        evidenceFailures.Add($"Branch room `{room.Id.Value}` was not present in the live cache.");
                        allRoomsPreparedBeforeReveal = false;
                        allGlobalsAppliedBeforeReveal = false;
                        continue;
                    }

                    var expectedBiomeId = RoomBiomeIds.Normalize(assets[room.Id.Value].BiomeId);
                    var lighting = instance.RuntimeRoot != null ? instance.RuntimeRoot.GetComponent<RoomLightingController>() : null;
                    var preparedBeforeReveal = lighting != null && lighting.IsPreparedFor(expectedBiomeId);
                    allRoomsPreparedBeforeReveal &= preparedBeforeReveal;
                    latestPreparedFrame = Math.Max(latestPreparedFrame, lighting != null ? lighting.PreparedFrame : int.MaxValue);
                    if (!preparedBeforeReveal)
                    {
                        evidenceFailures.Add($"Branch room `{room.Id.Value}` was not lighting-prepared while hidden.");
                    }

                    if (!cache.PrepareRoomForEntry(room.Id))
                    {
                        evidenceFailures.Add($"Branch room `{room.Id.Value}` failed room-entry preparation.");
                    }

                    if (!cache.ActivateRoom(room.Id))
                    {
                        evidenceFailures.Add($"Branch room `{room.Id.Value}` failed activation.");
                        allGlobalsAppliedBeforeReveal = false;
                        continue;
                    }

                    var revealFrame = Time.frameCount;
                    latestRevealFrame = Math.Max(latestRevealFrame, revealFrame);
                    finalController = lighting;
                    var globalBeforeReveal = lighting != null &&
                                             lighting.AppliedProfile != null &&
                                             string.Equals(lighting.AppliedBiomeId, expectedBiomeId, StringComparison.Ordinal) &&
                                             lighting.GlobalAppliedFrame <= revealFrame;
                    allGlobalsAppliedBeforeReveal &= globalBeforeReveal;
                    latestGlobalAppliedFrame = Math.Max(latestGlobalAppliedFrame, lighting != null ? lighting.GlobalAppliedFrame : int.MaxValue);
                    if (!globalBeforeReveal)
                    {
                        evidenceFailures.Add($"Branch room `{room.Id.Value}` did not apply active-room globals before reveal.");
                    }

                    yield return null;
                    var sample = CaptureSample(root.transform, samples.Count, samples.Count * Time.unscaledDeltaTime, mainRecorder, renderRecorder, gcRecorder);
                    samples.Add(sample);
                    frameMs.Add(sample.frameTimeMs);
                    if (sample.mainThreadMs > 0d)
                    {
                        mainMs.Add(sample.mainThreadMs);
                    }

                    if (sample.renderThreadMs > 0d)
                    {
                        renderMs.Add(sample.renderThreadMs);
                    }

                    if (sample.gpuFrameMs > 0d)
                    {
                        gpuMs.Add(sample.gpuFrameMs);
                    }

                    if (sample.gcAllocatedKb >= 0d)
                    {
                        gcKb.Add(sample.gcAllocatedKb);
                    }
                }

                DisposeRecorder(ref mainRecorder);
                DisposeRecorder(ref renderRecorder);
                DisposeRecorder(ref gcRecorder);
                yield return CaptureScreenshot(screenshotPath);

                if (finalController != null)
                {
                    finalController.ApplyBiome(finalController.AppliedBiomeId, force: true);
                }

                var finalSnapshot = BiomeLightingDiagnostics.LastSnapshot;
                var visual = M140VisualScreenshotValidator.Validate(screenshotPath, scenario.id);
                var summary = BuildSummary(
                    scenario,
                    finalController,
                    finalSnapshot,
                    screenshotPath,
                    visual,
                    samples,
                    frameMs,
                    mainMs,
                    renderMs,
                    gpuMs,
                    gcKb);
                summary.requiresPrewarmEvidence = true;
                summary.prewarmPreparedBeforeReveal = allRoomsPreparedBeforeReveal;
                summary.prewarmGlobalAppliedBeforeReveal = allGlobalsAppliedBeforeReveal;
                summary.prewarmPreparedFrame = latestPreparedFrame < 0 ? 0 : latestPreparedFrame;
                summary.prewarmGlobalAppliedFrame = latestGlobalAppliedFrame < 0 ? 0 : latestGlobalAppliedFrame;
                summary.prewarmRevealFrame = latestRevealFrame < 0 ? Time.frameCount : latestRevealFrame;
                summary.note = "Full branch traversal built rooms hidden, prepared room rigs before activation, and applied active-room global lighting before the first visible frame.";
                summary.failures = LightingRenderAuditReportGenerator.ValidateScenario(summary)
                    .Concat(evidenceFailures)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                summary.passed = summary.failures.Length == 0;
                onComplete?.Invoke(summary);
            }
            finally
            {
                DisposeRecorder(ref mainRecorder);
                DisposeRecorder(ref renderRecorder);
                DisposeRecorder(ref gcRecorder);
                cache.DisposeBranchRooms();
                for (var index = 0; index < createdMaterials.Count; index++)
                {
                    if (createdMaterials[index] != null)
                    {
                        Object.Destroy(createdMaterials[index]);
                    }
                }

                Object.Destroy(root);
            }
        }

        private static Camera CreateCamera(Transform root, string scenarioId)
        {
            var cameraObject = new GameObject("LightingAuditCamera_" + scenarioId);
            cameraObject.transform.SetParent(root, false);
            cameraObject.transform.localPosition = new Vector3(0f, 9f, -8f);
            cameraObject.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.cameraType = CameraType.Game;
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.Skybox;
            return camera;
        }

        private static void CreateSceneGeometry(Transform root, LightingRenderAuditScenarioDefinition scenario, List<Material> materials)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "LightingAuditFloor";
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.08f, 10f);
            AssignMaterial(floor, new Color(0.16f, 0.15f, 0.14f), materials);

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "LightingAuditBackdrop";
            backdrop.transform.SetParent(root, false);
            backdrop.transform.localPosition = new Vector3(0f, 2.5f, 4.8f);
            backdrop.transform.localScale = new Vector3(16f, 5f, 0.12f);
            AssignMaterial(backdrop, new Color(0.11f, 0.12f, 0.14f), materials);

            CreateMarkerObjects(root, LightingRenderAuditMarkerKind.Prop, Mathf.Max(0, scenario.propLightCount), PrimitiveType.Cube, new Color(0.35f, 0.32f, 0.24f), materials, -5f, -2.2f);
            CreateMarkerObjects(root, LightingRenderAuditMarkerKind.Enemy, Mathf.Max(0, scenario.enemyCount), PrimitiveType.Capsule, new Color(0.7f, 0.16f, 0.12f), materials, -5f, 0.6f);
            CreateMarkerObjects(root, LightingRenderAuditMarkerKind.Projectile, Mathf.Max(0, scenario.projectileCount), PrimitiveType.Sphere, new Color(1f, 0.56f, 0.18f), materials, -5f, 2.2f, 0.22f);
            CreateVfxObjects(root, Mathf.Max(0, scenario.vfxCount), materials);
            CreateBiomeEmitters(root, scenario);
        }

        private static void CreateMarkerObjects(
            Transform root,
            LightingRenderAuditMarkerKind kind,
            int count,
            PrimitiveType primitive,
            Color color,
            List<Material> materials,
            float startX,
            float z,
            float scale = 0.65f)
        {
            for (var index = 0; index < count; index++)
            {
                var gameObject = GameObject.CreatePrimitive(primitive);
                gameObject.name = "LightingAudit" + kind + "_" + index;
                gameObject.transform.SetParent(root, false);
                var x = startX + (index % 10) * 1.1f;
                var row = index / 10;
                gameObject.transform.localPosition = new Vector3(x, scale * 0.5f, z - row * 0.9f);
                gameObject.transform.localScale = Vector3.one * scale;
                AssignMaterial(gameObject, color, materials);
                gameObject.AddComponent<LightingRenderAuditMarker>().Configure(kind);
                if (kind == LightingRenderAuditMarkerKind.Projectile)
                {
                    gameObject.AddComponent<LightingRenderAuditOrbitMotion>().Configure(Vector3.up + Vector3.right, 180f + index * 7f);
                }
            }
        }

        private static void CreateVfxObjects(Transform root, int count, List<Material> materials)
        {
            for (var index = 0; index < count; index++)
            {
                var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gameObject.name = "LightingAuditVfx_" + index;
                gameObject.transform.SetParent(root, false);
                gameObject.transform.localPosition = new Vector3(-4f + (index % 8) * 1.1f, 0.45f, -0.6f - (index / 8) * 0.7f);
                gameObject.transform.localScale = Vector3.one * 0.35f;
                AssignMaterial(gameObject, new Color(0.2f, 0.85f, 1f), materials);
                gameObject.AddComponent<LightingRenderAuditMarker>().Configure(LightingRenderAuditMarkerKind.Vfx);
                var particles = gameObject.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.loop = true;
                main.startLifetime = 0.45f;
                main.startSpeed = 0.15f;
                main.startSize = 0.08f;
                main.maxParticles = 24;
                var emission = particles.emission;
                emission.rateOverTime = 8f;
                particles.Play();
            }
        }

        private static void CreateBiomeEmitters(Transform root, LightingRenderAuditScenarioDefinition scenario)
        {
            var propCount = Mathf.Max(0, scenario.propLightCount);
            var effectCount = Mathf.Max(0, scenario.dynamicEffectLightCount);
            for (var index = 0; index < propCount; index++)
            {
                CreateEmitter(root, "LightingAuditPropEmitter_" + index, BiomeLightEmitterKind.Prop, index, new Vector3(-5f + index * 1.4f, 1.2f, -3.2f), new Color(1f, 0.74f, 0.36f), 1.25f, 4.2f, castsShadows: index == 0);
            }

            for (var index = 0; index < effectCount; index++)
            {
                CreateEmitter(root, "LightingAuditEffectEmitter_" + index, BiomeLightEmitterKind.DynamicEffect, 100 + index, new Vector3(-4f + index * 1.6f, 1.1f, 1.4f), new Color(0.28f, 0.88f, 1f), 1.6f, 3.5f, castsShadows: index == 0);
            }
        }

        private static void CreateEmitter(Transform root, string name, BiomeLightEmitterKind kind, int priority, Vector3 localPosition, Color color, float intensity, float range, bool castsShadows)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(root, false);
            gameObject.transform.localPosition = localPosition;
            var emitter = gameObject.AddComponent<BiomeLightEmitter>();
            emitter.Configure(kind, color, intensity, range, priority, castsShadows);
        }

        private static void AssignMaterial(GameObject gameObject, Color color, List<Material> materials)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var material = new Material(shader)
            {
                color = color
            };
            materials.Add(material);
            renderer.sharedMaterial = material;
        }

        private static LightingRenderAuditFrameSample CaptureSample(Transform root, int sampleIndex, float elapsedSeconds, ProfilerRecorder mainRecorder, ProfilerRecorder renderRecorder, ProfilerRecorder gcRecorder)
        {
            var lights = root.GetComponentsInChildren<Light>(false).Where(light => light != null && light.enabled && light.gameObject.activeInHierarchy).ToArray();
            var budgetedLocalLights = root.GetComponentsInChildren<BiomeLightEmitter>(false)
                .Where(emitter => emitter != null && emitter.isActiveAndEnabled)
                .Select(emitter => emitter.GetComponent<Light>())
                .Where(light => light != null && light.enabled && light.gameObject.activeInHierarchy && light.type != LightType.Directional)
                .ToArray();
            var renderers = root.GetComponentsInChildren<Renderer>(false).Where(renderer => renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
            var particles = root.GetComponentsInChildren<ParticleSystem>(false).Where(system => system != null && system.gameObject.activeInHierarchy).ToArray();
            var markers = root.GetComponentsInChildren<LightingRenderAuditMarker>(false);
            var snapshot = BiomeLightingDiagnostics.LastSnapshot;
            return new LightingRenderAuditFrameSample
            {
                sampleIndex = sampleIndex,
                elapsedSeconds = elapsedSeconds,
                frameTimeMs = Mathf.Max(0f, Time.unscaledDeltaTime) * 1000d,
                mainThreadMs = RecorderMilliseconds(mainRecorder),
                renderThreadMs = RecorderMilliseconds(renderRecorder),
                gpuFrameMs = LatestGpuFrameMs(),
                gcAllocatedKb = RecorderBytes(gcRecorder) / 1024d,
                activeLights = lights.Length,
                activeLocalLights = budgetedLocalLights.Length,
                shadowedLights = budgetedLocalLights.Count(light => light.shadows != LightShadows.None),
                activePropLights = snapshot.ActivePropLightCount,
                activeDynamicEffectLights = snapshot.ActiveDynamicEffectLightCount,
                activeEnemies = markers.Count(marker => marker.Kind == LightingRenderAuditMarkerKind.Enemy),
                activeProjectiles = markers.Count(marker => marker.Kind == LightingRenderAuditMarkerKind.Projectile),
                activeVfx = markers.Count(marker => marker.Kind == LightingRenderAuditMarkerKind.Vfx),
                activeCameras = root.GetComponentsInChildren<Camera>(false).Count(camera => camera != null && camera.enabled && camera.gameObject.activeInHierarchy),
                activeRenderers = renderers.Length,
                activeParticleSystems = particles.Length
            };
        }

        private static LightingRenderAuditScenarioSummary BuildSummary(
            LightingRenderAuditScenarioDefinition scenario,
            RoomLightingController controller,
            BiomeLightingSnapshot snapshot,
            string screenshotPath,
            M140VisualValidationSummary visual,
            List<LightingRenderAuditFrameSample> samples,
            List<double> frameMs,
            List<double> mainMs,
            List<double> renderMs,
            List<double> gpuMs,
            List<double> gcKb)
        {
            var profile = controller != null ? controller.AppliedProfile : null;
            var summary = new LightingRenderAuditScenarioSummary
            {
                scenarioId = scenario.id,
                displayName = scenario.displayName,
                requestedBiomeId = scenario.biomeId,
                appliedBiomeId = snapshot.BiomeId,
                biomeId = snapshot.BiomeId,
                profileId = snapshot.ProfileId,
                lightingSnapshot = LightingRenderAuditSnapshotSummary.From(snapshot),
                screenshotPath = screenshotPath,
                visual = visual,
                sampleCount = samples.Count,
                frameP50Ms = LightingRenderAuditReportGenerator.Percentile(frameMs, 0.5d),
                frameP95Ms = LightingRenderAuditReportGenerator.Percentile(frameMs, 0.95d),
                frameMaxMs = frameMs.Count == 0 ? 0d : frameMs.Max(),
                mainThreadP95Ms = LightingRenderAuditReportGenerator.Percentile(mainMs, 0.95d),
                renderThreadP95Ms = LightingRenderAuditReportGenerator.Percentile(renderMs, 0.95d),
                gpuFrameP95Ms = LightingRenderAuditReportGenerator.Percentile(gpuMs, 0.95d),
                gcAllocatedAverageKb = gcKb.Count == 0 ? 0d : gcKb.Average(),
                peakActiveLights = samples.Count == 0 ? snapshot.ActiveLightCount : samples.Max(sample => sample.activeLights),
                peakActiveLocalLights = samples.Count == 0 ? snapshot.ActiveLocalLightCount : samples.Max(sample => sample.activeLocalLights),
                peakShadowedLights = samples.Count == 0 ? snapshot.ShadowedLightCount : samples.Max(sample => sample.shadowedLights),
                peakPropLights = samples.Count == 0 ? snapshot.ActivePropLightCount : samples.Max(sample => sample.activePropLights),
                peakDynamicEffectLights = samples.Count == 0 ? snapshot.ActiveDynamicEffectLightCount : samples.Max(sample => sample.activeDynamicEffectLights),
                peakActivePropLights = samples.Count == 0 ? snapshot.ActivePropLightCount : samples.Max(sample => sample.activePropLights),
                peakActiveDynamicEffectLights = samples.Count == 0 ? snapshot.ActiveDynamicEffectLightCount : samples.Max(sample => sample.activeDynamicEffectLights),
                peakEnemies = samples.Count == 0 ? 0 : samples.Max(sample => sample.activeEnemies),
                peakProjectiles = samples.Count == 0 ? 0 : samples.Max(sample => sample.activeProjectiles),
                peakVfx = samples.Count == 0 ? 0 : samples.Max(sample => sample.activeVfx),
                peakRenderers = samples.Count == 0 ? 0 : samples.Max(sample => sample.activeRenderers),
                peakParticleSystems = samples.Count == 0 ? 0 : samples.Max(sample => sample.activeParticleSystems),
                maxActiveLocalLights = snapshot.MaxActiveLocalLights,
                maxShadowedLocalLights = snapshot.MaxShadowedLocalLights,
                maxPropLights = snapshot.MaxPropLights,
                maxDynamicEffectLights = snapshot.MaxDynamicEffectLights,
                cameraBackgroundColor = profile != null ? LightingRenderAuditReportGenerator.FormatColor(profile.CameraBackgroundColor) : string.Empty,
                ambientSkyColor = profile != null ? LightingRenderAuditReportGenerator.FormatColor(profile.AmbientSkyColor) : string.Empty,
                fogColor = profile != null ? LightingRenderAuditReportGenerator.FormatColor(profile.FogColor) : string.Empty,
                fogEnabled = profile != null && profile.FogEnabled,
                note = scenario.transitionSweep ? "Transition sweep ends on the last applied biome and proves global lighting updates without stale profile evidence." : "Synthetic lighting/render load scenario.",
                samples = samples.ToArray()
            };
            summary.failures = LightingRenderAuditReportGenerator.ValidateScenario(summary).ToArray();
            summary.passed = summary.failures.Length == 0;
            return summary;
        }

        private static BranchRoomState[] CreateAuditBranchRooms()
        {
            return new[]
            {
                CreateAuditBranchRoom(BranchRoomId.Origin, "audit_branch_hollow_threshold", BranchRoomRole.Origin),
                CreateAuditBranchRoom(BranchRoomId.North, "audit_branch_verdant_ruins", BranchRoomRole.Combat),
                CreateAuditBranchRoom(BranchRoomId.East, "audit_branch_corrupted_ashen_shrine", BranchRoomRole.Reward)
            };
        }

        private static BranchRoomState CreateAuditBranchRoom(BranchRoomId id, string assetId, BranchRoomRole role)
        {
            return new BranchRoomState(
                id,
                Vector2Int.zero,
                new BranchRoomInstanceId(id.Value),
                assetId,
                null,
                role);
        }

        private static ImportedRoomRuntimeAsset CreateAuditRoomAsset(string id, string biomeId)
        {
            const int width = 6;
            const int height = 6;
            var bounds = Rect.MinMaxRect(-3f, -3f, 3f, 3f);
            var layout = new RoomLayout(
                width,
                height,
                bounds,
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                },
                Array.Empty<Vector2Int>(),
                new[] { new RoomLayoutFloorRegion("audit_floor", Vector3.zero, new Vector2(4.5f, 4.5f)) },
                Array.Empty<RoomLayoutObstacle>());
            return new ImportedRoomRuntimeAsset(
                id,
                id,
                biomeId,
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(width, height)),
                new[] { CreateAuditDoorPort("north_0", "north", 0f) },
                Array.Empty<ImportedSpawnPoint>(),
                Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawnSafeStart",
                    position = CreateAuditVector3(0f, 0f, -1f)
                },
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                new ImportedHollowRoomManifest
                {
                    hollowRuntime = new ImportedHollowRuntime
                    {
                        canonicalRoomId = id,
                        biomeId = biomeId,
                        displayName = id
                    }
                });
        }

        private static RoomDoorPort CreateAuditDoorPort(string id, string direction, float x)
        {
            return new RoomDoorPort(
                id,
                direction,
                0,
                new Vector2Int(0, 2),
                new Vector2(x, 3f),
                new Vector3(x, 0f, 3f),
                "standard");
        }

        private static ImportedVector3 CreateAuditVector3(float x, float y, float z)
        {
            return new ImportedVector3 { x = x, y = y, z = z };
        }

        private static IEnumerator CaptureScreenshot(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? LightingRenderAuditReportGenerator.DefaultReportDirectory);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static void TryStartRecorder(ref ProfilerRecorder recorder, ProfilerCategory category, string statName)
        {
            try
            {
                recorder = ProfilerRecorder.StartNew(category, statName, 128);
            }
            catch
            {
                recorder = default;
            }
        }

        private static double RecorderMilliseconds(ProfilerRecorder recorder)
        {
            return recorder.Valid && recorder.LastValue > 0 ? recorder.LastValue / 1000000d : 0d;
        }

        private static double RecorderBytes(ProfilerRecorder recorder)
        {
            return recorder.Valid && recorder.LastValue > 0 ? recorder.LastValue : 0d;
        }

        private static double LatestGpuFrameMs()
        {
            try
            {
                FrameTimingManager.CaptureFrameTimings();
                var timings = new FrameTiming[1];
                return FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0d ? timings[0].gpuFrameTime : 0d;
            }
            catch
            {
                return 0d;
            }
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }
    }
}
