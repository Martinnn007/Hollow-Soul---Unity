using System.Collections;
using System.IO;
using System.Linq;
using Hollow.Core.App;
using Hollow.Core.Diagnostics;
using Hollow.Editor.Generation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class BootLoadingStartupTests
    {
        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var screen in Object.FindObjectsByType<BootLoadingScreenController>(FindObjectsInactive.Include))
            {
                if (screen != null)
                {
                    Object.DestroyImmediate(screen.gameObject);
                }
            }
        }

        [Test]
        public void BootLoadingScreenShowsProgressFailureAndReadyStates()
        {
            var root = new GameObject("BootHarness");
            try
            {
                var screen = BootLoadingScreenController.Create(root.transform);

                screen.Show("Studio", "Hollow Soul", "Loading catalogs");
                Assert.AreEqual(BootLoadingScreenState.Loading, screen.State);
                Assert.AreEqual("Loading catalogs", screen.CurrentStage);

                screen.SetStage("Warming shaders", 0.75f);
                Assert.AreEqual("Warming shaders", screen.CurrentStage);
                Assert.AreEqual(0.75f, screen.CurrentProgress01, 0.001f);

                screen.MarkReady();
                Assert.AreEqual(BootLoadingScreenState.Ready, screen.State);
                Assert.AreEqual(1f, screen.CurrentProgress01, 0.001f);

                screen.ShowFailure("catalog missing");
                Assert.AreEqual(BootLoadingScreenState.Failed, screen.State);
                Assert.AreEqual("Startup failed", screen.CurrentStage);
                Assert.IsTrue(screen.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BootPreloadDefaultsStayGlobalAndDoNotIncludeBranchRuntimeState()
        {
            var paths = BootPreloadService.DefaultResourcePreloadPaths;

            CollectionAssert.Contains(paths, "Hollow");
            Assert.IsFalse(paths.Any(path => path.Contains("Navigation", System.StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(paths.Any(path => path.Contains("RuntimeRoom", System.StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(paths.Any(path => path.Contains("BranchEnemyPool", System.StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void BootPreloadServiceReportsStagesAndResourceLoads()
        {
            var settings = BootPreloadSettings.Default();
            settings.ConfigureForTests(
                nextPreloadResources: true,
                nextWarmShaders: false,
                nextWarmPrimitivePools: false,
                nextResourcePreloadPaths: new[] { "UI/Hud" });
            var service = new BootPreloadService();
            var report = new BootPreloadReport();
            var lastProgress = new BootPreloadStageProgress();

            RunToCompletion(service.Run(settings, progress => lastProgress = progress, report));

            Assert.Greater(report.StageCount, 0);
            Assert.Greater(report.ResourceLoadCount, 0);
            Assert.AreEqual("Ready", report.LastStage);
            Assert.AreEqual("Ready", lastProgress.Stage);
            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.Greater(snapshot.BootLoadingStageCount, 0);
            Assert.Greater(snapshot.BootPreloadResourceLoads, 0);
        }

        [Test]
        public void BootSceneControllerUsesFastEditorMinimumWithoutChangingRoutePolicy()
        {
            var root = new GameObject("BootSceneControllerHarness");
            try
            {
                var controller = root.AddComponent<BootSceneController>();
                controller.ConfigureBootLoading(
                    showScreen: true,
                    nextStudioName: "Studio",
                    nextGameTitle: "Hollow Soul",
                    nextMinimumVisibleSeconds: 1.5f,
                    nextAllowEditorFastBoot: true,
                    nextEditorMinimumVisibleSeconds: 0.25f,
                    nextPreloadSettings: BootPreloadSettings.Default());

                Assert.AreEqual(0.25f, controller.EffectiveMinimumVisibleSeconds(), 0.001f);
                Assert.AreEqual(
                    AppShellRoute.MainMenuVisionOS,
                    BootSceneController.ResolveStartupRoute(RuntimePlatform.VisionOS, AppShellRoute.MainMenu, true, AppShellRoute.MainMenuVisionOS));
                Assert.AreEqual(
                    AppShellRoute.MainMenu,
                    BootSceneController.ResolveStartupRoute(RuntimePlatform.OSXEditor, AppShellRoute.MainMenu, true, AppShellRoute.MainMenuVisionOS));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void M136CountersExposeBootLoadingTelemetry()
        {
            M136PerformanceOperationCounters.ReportBootLoadingStart();
            M136PerformanceOperationCounters.ReportBootLoadingStage(12.5f);
            M136PerformanceOperationCounters.ReportBootPreloadResourceLoad(7);
            M136PerformanceOperationCounters.ReportBootPreloadWarmRequest();
            M136PerformanceOperationCounters.ReportBootPreloadWarmCompletion();
            M136PerformanceOperationCounters.ReportBootPreloadShaderWarmAttempt();
            M136PerformanceOperationCounters.ReportBootPreloadShaderWarmCollectionCount(2);
            M136PerformanceOperationCounters.ReportBootPreloadShaderWarmSuccess(3.5f);
            M136PerformanceOperationCounters.ReportBootPreloadShaderWarmMiss();
            M136PerformanceOperationCounters.ReportBootLoadingCompletion(250f);
            M136PerformanceOperationCounters.ReportBootLoadingFailure();

            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(1, snapshot.BootLoadingStarts);
            Assert.AreEqual(1, snapshot.BootLoadingCompletions);
            Assert.AreEqual(1, snapshot.BootLoadingFailures);
            Assert.AreEqual(1, snapshot.BootLoadingStageCount);
            Assert.AreEqual(12.5f, snapshot.BootLoadingMaxStageMilliseconds, 0.001f);
            Assert.AreEqual(250f, snapshot.BootLoadingMaxMilliseconds, 0.001f);
            Assert.AreEqual(7, snapshot.BootPreloadResourceLoads);
            Assert.AreEqual(1, snapshot.BootPreloadWarmRequests);
            Assert.AreEqual(1, snapshot.BootPreloadWarmCompletions);
            Assert.AreEqual(1, snapshot.BootPreloadShaderWarmAttempts);
            Assert.AreEqual(2, snapshot.BootPreloadShaderWarmCollections);
            Assert.AreEqual(1, snapshot.BootPreloadShaderWarmSuccesses);
            Assert.AreEqual(1, snapshot.BootPreloadShaderWarmMisses);
            Assert.AreEqual(3.5f, snapshot.BootPreloadShaderWarmMaxMilliseconds, 0.001f);
        }

        [Test]
        public void BootPreloadServiceWarmsCuratedShaderCollectionsWithoutBlanketWarmup()
        {
            var collection = new ShaderVariantCollection();
            var profile = ScriptableObject.CreateInstance<HollowShaderWarmupProfile>();
            try
            {
                profile.Configure(
                    "Test Warmup",
                    nextEnabledForBoot: true,
                    nextTargetRenderProfileLabel: "Test",
                    nextCollections: new[] { collection },
                    nextMaxExpectedWarmupMilliseconds: 25f,
                    nextNotes: "Test profile");
                var settings = BootPreloadSettings.Default();
                settings.ConfigureForTests(
                    nextPreloadResources: false,
                    nextWarmShaders: true,
                    nextWarmPrimitivePools: false,
                    nextShaderWarmupProfile: profile);
                var report = new BootPreloadReport();

                RunToCompletion(new BootPreloadService().Run(settings, _ => { }, report));

                Assert.AreEqual(1, report.ShaderWarmCollections);
                Assert.AreEqual(1, report.ShaderWarmAttempts);
                Assert.AreEqual(1, report.ShaderWarmSuccesses);
                Assert.AreEqual(0, report.ShaderWarmMisses);

                var snapshot = M136PerformanceOperationCounters.Snapshot();
                Assert.AreEqual(1, snapshot.BootPreloadShaderWarmCollections);
                Assert.AreEqual(1, snapshot.BootPreloadShaderWarmAttempts);
                Assert.AreEqual(1, snapshot.BootPreloadShaderWarmSuccesses);
                Assert.AreEqual(0, snapshot.BootPreloadShaderWarmMisses);
                var bootPreloadSource = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Core/App/BootPreloadService.cs");
                Assert.IsFalse(bootPreloadSource.Contains("Warmup" + "AllShaders"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(collection);
            }
        }

        [Test]
        public void ShaderWarmupAssetsAndStudioBrandingAreConfigured()
        {
            var validation = HollowShaderWarmupAssetGenerator.ValidateBootShaderWarmupAssets();

            Assert.IsTrue(validation.ProfileExists, validation.Message);
            Assert.IsTrue(validation.CollectionExists, validation.Message);
            Assert.Greater(validation.CollectionCount, 0, validation.Message);
            Assert.AreEqual("CineFit Studio", PlayerSettings.companyName);
        }

        private static void RunToCompletion(IEnumerator routine)
        {
            var guard = 0;
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator nested)
                {
                    RunToCompletion(nested);
                }

                guard++;
                Assert.Less(guard, 256);
            }
        }
    }
}
