using Hollow.Core;
using Hollow.Core.App;
using Hollow.Diagnostics;
using Hollow.Platform;
using NUnit.Framework;
using UnityEngine;

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
            Assert.AreEqual("MainMenu_VisionOS", SceneLoaderService.SceneNameForRoute(AppShellRoute.MainMenuVisionOS));
            Assert.AreEqual("Spaceship_Windows", SceneLoaderService.SceneNameForRoute(AppShellRoute.SpaceshipWindows));
            Assert.AreEqual("Spaceship_VisionOS_Bounded", SceneLoaderService.SceneNameForRoute(AppShellRoute.SpaceshipVisionOSBounded));
            Assert.AreEqual("Spaceship_VisionOS_Immersive", SceneLoaderService.SceneNameForRoute(AppShellRoute.SpaceshipVisionOSImmersive));
            Assert.AreEqual(AppShellRoute.SpaceshipWindows, PlatformPresentationModeResolver.SpaceshipRouteForPlatform(HollowPlatformKind.WindowsStandard3D));
            Assert.AreEqual(AppShellRoute.SpaceshipVisionOSBounded, PlatformPresentationModeResolver.SpaceshipRouteForPlatform(HollowPlatformKind.VisionOSBoundedTabletop));
            Assert.AreEqual(AppShellRoute.SpaceshipVisionOSImmersive, PlatformPresentationModeResolver.SpaceshipRouteForPlatform(HollowPlatformKind.VisionOSImmersive));
        }

        [Test]
        public void BootRoutesVisionOSRuntimeToGuidedMenu()
        {
            var route = BootSceneController.ResolveStartupRoute(
                RuntimePlatform.VisionOS,
                AppShellRoute.MainMenu,
                preferVisionOSRoute: true,
                AppShellRoute.MainMenuVisionOS);

            Assert.AreEqual(AppShellRoute.MainMenuVisionOS, route);
        }

        [Test]
        public void BootKeepsMainMenuRouteOutsideVisionOS()
        {
            var route = BootSceneController.ResolveStartupRoute(
                RuntimePlatform.OSXEditor,
                AppShellRoute.MainMenu,
                preferVisionOSRoute: true,
                AppShellRoute.GameVisionOSImmersive);

            Assert.AreEqual(AppShellRoute.MainMenu, route);
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
