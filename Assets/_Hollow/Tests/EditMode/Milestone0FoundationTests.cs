using Hollow.Core;
using Hollow.Core.App;
using Hollow.Diagnostics;
using Hollow.Platform;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone0FoundationTests
    {
        [Test]
        public void HollowIdPreservesStableStringValue()
        {
            var id = new HollowId("world_crypt");

            Assert.AreEqual("world_crypt", id.Value);
            Assert.AreEqual("world_crypt", id.ToString());
            Assert.IsFalse(id.IsEmpty);
        }

        [Test]
        public void AppStateMachineTransitionsBetweenRoutes()
        {
            var appStateMachine = new AppStateMachine();

            appStateMachine.TransitionTo(AppShellRoute.MainMenu);

            Assert.AreEqual(AppShellRoute.MainMenu, appStateMachine.CurrentRoute);
        }

        [Test]
        public void SceneLoaderMapsPlatformRoutesToSceneNames()
        {
            Assert.AreEqual("Game_Windows", SceneLoaderService.SceneNameForRoute(AppShellRoute.GameWindows));
            Assert.AreEqual("Game_VisionOS_Bounded", SceneLoaderService.SceneNameForRoute(AppShellRoute.GameVisionOSBounded));
            Assert.AreEqual("Game_VisionOS_Immersive", SceneLoaderService.SceneNameForRoute(AppShellRoute.GameVisionOSImmersive));
        }

        [Test]
        public void PlatformServicesExposeExpectedCapabilities()
        {
            IPlatformServices windows = new WindowsPlatformServices();
            IPlatformServices bounded = new VisionOSBoundedPlatformServices();
            IPlatformServices immersive = new VisionOSImmersivePlatformServices();

            Assert.IsFalse(windows.SupportsSpatialTabletop);
            Assert.IsTrue(bounded.SupportsSpatialTabletop);
            Assert.IsTrue(immersive.SupportsImmersivePresentation);
        }

        [Test]
        public void Milestone0SmokeReportPasses()
        {
            var report = ValidationHarness.RunMilestone0Smoke();

            Assert.IsTrue(report.Passed);
        }
    }
}
