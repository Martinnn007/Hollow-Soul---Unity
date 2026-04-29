using System.Linq;
using Hollow.Input;
using Hollow.Persistence;
using Hollow.UI.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class PauseMenuFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            GameplayPauseState.SetPaused(false);
            Time.timeScale = 1f;
        }

        [Test]
        public void GameplayInputSnapshotCarriesPausePressed()
        {
            var snapshot = new GameplayInputSnapshot(
                Vector2.zero,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: true);

            Assert.IsTrue(snapshot.PausePressed);
        }

        [Test]
        public void PlatformShellAddsPauseMenuController()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var controller = shell.AddComponent<PlatformShellController>();

                controller.ApplyConfiguration();

                Assert.IsNotNull(shell.GetComponent<PauseMenuController>());
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void PauseMenuFreezesAndRestoresGameplayTime()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var pause = shell.AddComponent<PauseMenuController>();
                Time.timeScale = 1f;

                pause.ShowRoot();

                Assert.AreEqual(PauseMenuState.Root, pause.State);
                Assert.IsTrue(GameplayPauseState.IsPaused);
                Assert.AreEqual(0f, Time.timeScale);

                pause.Resume();

                Assert.AreEqual(PauseMenuState.Hidden, pause.State);
                Assert.IsFalse(GameplayPauseState.IsPaused);
                Assert.AreEqual(1f, Time.timeScale);
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void ControlsPanelShowsKeyboardAndDualShockReference()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var pause = shell.AddComponent<PauseMenuController>();

                pause.ShowControls();

                var labels = shell.GetComponentsInChildren<Text>(includeInactive: true).Select(text => text.text).ToArray();
                Assert.Contains("Keyboard", labels);
                Assert.Contains("DualShock 5", labels);
                Assert.Contains("Pause: Escape", labels);
                Assert.Contains("Pause: Options", labels);
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void RunSnapshotCanPersistChallengeIdentity()
        {
            var snapshot = new RunSaveSnapshot
            {
                runId = "challenge-run",
                challengeId = "blade_trial"
            };

            Assert.AreEqual("blade_trial", snapshot.challengeId);
        }
    }
}
