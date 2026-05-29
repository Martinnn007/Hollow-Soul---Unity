using System.Reflection;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Tests.EditMode
{
    public sealed class RoomEntryTransitionDimmingFixTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayTransitionState.ResetForTests();
            GameplayPauseState.SetPaused(false);
            M136PerformanceOperationCounters.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayTransitionState.ResetForTests();
            GameplayPauseState.SetPaused(false);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas != null && canvas.gameObject.name == "RoomTransitionCurtain")
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }
        }

        [Test]
        public void GameplayTransitionLockSuppressesGameplayInputWithoutPausing()
        {
            GameplayInputReader.SetExternalMoveOverride(Vector2.right);
            Assert.Greater(GameplayInputReader.ReadCurrent().Move.x, 0.5f);

            using (GameplayTransitionState.AcquireLock())
            {
                GameplayInputReader.SetExternalMoveOverride(Vector2.right);
                var locked = GameplayInputReader.ReadCurrent();
                Assert.AreEqual(Vector2.zero, locked.Move);
                Assert.IsFalse(GameplayPauseState.IsPaused);
            }

            Assert.IsFalse(GameplayTransitionState.IsLocked);
        }

        [Test]
        public void TransitionCurtainIsOpaqueParentedAndHiddenAfterReadyWithoutLingeringFrames()
        {
            var root = new GameObject("BranchSessionHarness");
            try
            {
                var session = root.AddComponent<BranchSessionController>();
                Invoke(session, "ShowTransitionCurtain");

                var curtain = root.transform.Find("RoomTransitionCurtain");
                Assert.IsNotNull(curtain);
                Assert.IsTrue(curtain.gameObject.activeSelf);
                Assert.AreSame(root.transform, curtain.parent);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, curtain.GetComponent<Canvas>().renderMode);
                Assert.AreEqual(1f, curtain.GetComponent<Image>().color.a);

                Invoke(session, "MarkTransitionRoomReadyForReveal");
                Invoke(session, "HideTransitionCurtain");

                Assert.IsFalse(curtain.gameObject.activeSelf);
                var snapshot = M136PerformanceOperationCounters.Snapshot();
                Assert.AreEqual(1, snapshot.TransitionCurtainShows);
                Assert.AreEqual(1, snapshot.TransitionCurtainHides);
                Assert.AreEqual(0, snapshot.TransitionCurtainMaxFramesAfterReady);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TransitionCurtainRemovesOrphanCurtainsBeforeShowing()
        {
            var orphan = new GameObject("RoomTransitionCurtain", typeof(RectTransform), typeof(Canvas), typeof(Image));
            var root = new GameObject("BranchSessionHarness");
            try
            {
                var session = root.AddComponent<BranchSessionController>();
                Invoke(session, "ShowTransitionCurtain");

                Assert.IsTrue(root.transform.Find("RoomTransitionCurtain") != null);
                Assert.IsTrue(orphan == null);
                Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().TransitionOrphanCurtainsRemoved);
            }
            finally
            {
                if (orphan != null)
                {
                    Object.DestroyImmediate(orphan);
                }

                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoomCombatControllerDoesNotTickTacticalDirectorWhileTransitionSuspended()
        {
            var root = new GameObject("RoomCombatHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                combat.SetTransitionSuspended(true);
                Invoke(combat, "Update");
                Assert.AreEqual(0, M136PerformanceOperationCounters.Snapshot().TacticalDirectorTicks);

                combat.SetTransitionSuspended(false);
                Invoke(combat, "Update");
                Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().TacticalDirectorTicks);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Invoke(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(target, null);
        }
    }
}
