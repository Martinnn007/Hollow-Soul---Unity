using System.Collections;
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
