using Hollow.Core;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using Hollow.World;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone2RuntimeShellTests
    {
        [Test]
        public void PresentationScalePolicyKeepsOnlyBoundedVisionAtTabletopScale()
        {
            Assert.AreEqual(1f, PresentationScalePolicy.WorldScaleFor(HollowPlatformKind.WindowsStandard3D));
            Assert.AreEqual(0.1f, PresentationScalePolicy.WorldScaleFor(HollowPlatformKind.VisionOSBoundedTabletop));
            Assert.AreEqual(1f, PresentationScalePolicy.WorldScaleFor(HollowPlatformKind.VisionOSImmersive));
        }

        [Test]
        public void GameSessionStateCarriesProfileAndPlatformScale()
        {
            var profile = new ProfileSlotSummary(0, "profile-001", "Sample Profile", 1, 1, 0, false);

            var state = GameSessionState.Create(
                RuntimeSessionMode.ProfileBacked,
                HollowPlatformKind.VisionOSBoundedTabletop,
                profile,
                Vector3.zero);

            Assert.IsTrue(state.HasProfile);
            Assert.AreEqual("profile-001", state.ProfileId);
            Assert.AreEqual("Sample Profile", state.ProfileDisplayName);
            Assert.AreEqual(0.1f, state.PresentationScale);
        }

        [Test]
        public void RoomRuntimeRootDefaultsToIsaacSingleRoomMeters()
        {
            var gameObject = new GameObject("Room");
            try
            {
                var room = gameObject.AddComponent<RoomRuntimeRoot>();
                room.ConfigureDefault();

                Assert.AreEqual(13f, room.RoomSizeMeters.x);
                Assert.AreEqual(7f, room.RoomSizeMeters.y);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameSessionControllerInitializesBoundedSceneWithoutScalingHud()
        {
            var root = new GameObject("GameSessionRoot");
            try
            {
                var controller = root.AddComponent<GameSessionController>();
                controller.Configure(HollowPlatformKind.VisionOSBoundedTabletop);

                var presentationObject = new GameObject("WorldPresentationRoot");
                presentationObject.transform.SetParent(root.transform, false);
                presentationObject.AddComponent<PlatformPresentationRoot>();

                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(presentationObject.transform, false);
                roomObject.AddComponent<RoomRuntimeRoot>().ConfigureDefault();

                var spawnObject = new GameObject("PlayerSpawn_Center");
                spawnObject.transform.SetParent(roomObject.transform, false);
                spawnObject.AddComponent<PlayerSpawnPoint>();

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(presentationObject.transform, false);
                playerObject.AddComponent<PlaceholderPlayerController>();

                controller.InitializeSession();

                Assert.AreEqual(HollowPlatformKind.VisionOSBoundedTabletop, controller.PlatformKind);
                Assert.AreEqual(0.1f, controller.PresentationRoot.WorldScale);
                Assert.AreEqual(Vector3.zero, controller.PlayerController.transform.position);
                Assert.AreEqual(Vector3.zero, controller.SessionState.PlayerSpawnPosition);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
