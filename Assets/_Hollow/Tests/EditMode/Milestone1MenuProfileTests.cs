using System.IO;
using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.UI.MainMenu;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone1MenuProfileTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m1_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(tempRoot) && Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void JsonProfileStoreStartsWithThreeEmptySlots()
        {
            var store = new JsonProfileStore(tempRoot);

            var summaries = store.LoadSlotSummaries();

            Assert.AreEqual(ProfileSlotConstants.MaxSlots, summaries.Count);
            Assert.IsTrue(summaries[0].IsEmpty);
            Assert.IsTrue(summaries[1].IsEmpty);
            Assert.IsTrue(summaries[2].IsEmpty);
        }

        [Test]
        public void JsonProfileStorePersistsCreatedProfile()
        {
            var store = new JsonProfileStore(tempRoot);
            var created = store.CreateOrLoadProfile(new ProfileSlotId(1), "Engineer");

            var reloaded = new JsonProfileStore(tempRoot).LoadSlotSummaries()[1];

            Assert.IsFalse(created.IsEmpty);
            Assert.AreEqual(created.ProfileId, reloaded.ProfileId);
            Assert.AreEqual("Engineer", reloaded.DisplayName);
        }

        [Test]
        public void MainMenuViewModelCreatesProfileAndLaunchesPlatformRoute()
        {
            var store = new JsonProfileStore(tempRoot);
            var selectedContext = new SelectedProfileContext();
            var appState = new AppStateMachine();
            var viewModel = new MainMenuViewModel(store, selectedContext, appState);

            viewModel.SelectOrCreateSlot(0);
            var route = viewModel.LaunchPlatform(HollowPlatformKind.VisionOSBoundedTabletop);

            Assert.AreEqual(MainMenuState.Launching, viewModel.State);
            Assert.AreEqual(AppShellRoute.GameVisionOSBounded, route);
            Assert.AreEqual(AppShellRoute.GameVisionOSBounded, appState.CurrentRoute);
            Assert.IsTrue(selectedContext.HasSelection);
        }

        [Test]
        public void PlatformPresentationModeResolverMapsEveryM1Target()
        {
            Assert.AreEqual(AppShellRoute.GameWindows, PlatformPresentationModeResolver.RouteForPlatform(HollowPlatformKind.WindowsStandard3D));
            Assert.AreEqual(AppShellRoute.GameVisionOSBounded, PlatformPresentationModeResolver.RouteForPlatform(HollowPlatformKind.VisionOSBoundedTabletop));
            Assert.AreEqual(AppShellRoute.GameVisionOSImmersive, PlatformPresentationModeResolver.RouteForPlatform(HollowPlatformKind.VisionOSImmersive));
        }
    }
}
