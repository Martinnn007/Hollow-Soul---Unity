using System.Collections;
using Hollow.Core.Diagnostics;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using Hollow.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class Milestone12BootSmokePlayModeTests
    {
        [UnityTest]
        public IEnumerator BootScenePreloadsAndRoutesToMainMenu()
        {
            M136PerformanceOperationCounters.Reset();
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);

            var timeout = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>());

            var operations = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(1, operations.BootLoadingStarts);
            Assert.AreEqual(1, operations.BootLoadingCompletions);
            Assert.AreEqual(0, operations.BootLoadingFailures);
            Assert.Greater(operations.BootLoadingStageCount, 0);
            Assert.Greater(operations.BootPreloadShaderWarmAttempts, 0);
            Assert.AreEqual(operations.BootPreloadShaderWarmAttempts, operations.BootPreloadShaderWarmSuccesses);
            Assert.AreEqual(0, operations.BootPreloadShaderWarmMisses);
        }

        [UnityTest]
        public IEnumerator MainMenuAndWindowsGameScenesLoadWithRuntimeRoots()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>());

            yield return SceneManager.LoadSceneAsync("Game_Windows", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.AreEqual("Game_Windows", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindFirstObjectByType<GameSessionController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<RoomRuntimeRoot>());
        }
    }
}
