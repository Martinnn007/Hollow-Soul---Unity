using System.Collections;
using Hollow.Diagnostics;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class Milestone24PlatformSceneSmokePlayModeTests
    {
        [UnityTest]
        public IEnumerator MainMenuRoomDesignerAndAllPlatformScenesExposeQaProbeData()
        {
            yield return LoadAndAssertMenuScene("MainMenu");
            yield return LoadAndAssertMenuScene("MainMenu_VisionOS");
            yield return LoadAndAssertDesignerScene("RoomDesigner");
            yield return LoadAndAssertGameScene("Game_Windows", expectedScale: 1f);
            yield return LoadAndAssertGameScene("Game_VisionOS_Bounded", expectedScale: PresentationScalePolicy.VisionOSBoundedTabletopScale);
            yield return LoadAndAssertGameScene("Game_VisionOS_Immersive", expectedScale: 1f);
        }

        private static IEnumerator LoadAndAssertMenuScene(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;

            var snapshot = PlatformRuntimeQaProbe.CaptureCurrentScene();
            Assert.AreEqual(sceneName, snapshot.sceneName);
            Assert.IsTrue(snapshot.hasMainMenuController, "MainMenu should expose MainMenuController.");
            Assert.IsTrue(snapshot.hasPresentationCatalog, "Presentation catalog should be available in runtime smoke.");
        }

        private static IEnumerator LoadAndAssertDesignerScene(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;

            var snapshot = PlatformRuntimeQaProbe.CaptureCurrentScene();
            Assert.AreEqual(sceneName, snapshot.sceneName);
            Assert.IsTrue(snapshot.hasRoomDesignerController, "RoomDesigner should expose RoomDesignerController.");
            Assert.IsTrue(snapshot.hasPresentationCatalog, "Presentation catalog should be available in runtime smoke.");
        }

        private static IEnumerator LoadAndAssertGameScene(string sceneName, float expectedScale)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var snapshot = PlatformRuntimeQaProbe.CaptureCurrentScene();
            Assert.AreEqual(sceneName, snapshot.sceneName);
            Assert.IsTrue(snapshot.hasGameSessionController, $"{sceneName} should expose GameSessionController.");
            Assert.IsTrue(snapshot.hasRoomRuntimeRoot, $"{sceneName} should expose RoomRuntimeRoot.");
            Assert.IsTrue(snapshot.hasPresentationRoot, $"{sceneName} should expose PlatformPresentationRoot.");
            Assert.IsTrue(snapshot.hasPlatformShellCanvas, $"{sceneName} should expose PlatformShellCanvas.");
            Assert.IsTrue(snapshot.hudOutsideWorldRoot, $"{sceneName} HUD/shell must remain outside WorldPresentationRoot.");
            Assert.AreEqual(expectedScale, snapshot.worldScale, 0.001f, $"{sceneName} world scale drifted.");
            Assert.IsTrue(snapshot.hasPresentationCatalog, "Presentation catalog should be available in runtime smoke.");

            if (sceneName == "Game_VisionOS_Bounded")
            {
                Assert.IsNotNull(Object.FindFirstObjectByType<VisionOSGameplayInputDiagnostics>(), "Bounded visionOS gameplay should expose input diagnostics.");
            }
        }
    }
}
