using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone101NavMeshLocomotionOwnershipTests
    {
        private const string RatRoomPath = "Assets/_Hollow/Data/Rooms/DesignerApproved/Room_Small_RatRoom_001.hollowruntime.json";

        [Test]
        public void BridgeTracksAgentAndHollowOwnership()
        {
            var root = new GameObject("M101OwnershipHarness");
            try
            {
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(RatRoomPath)));

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

                var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.transform.SetParent(root.transform, false);
                enemyObject.transform.localPosition = Vector3.zero;
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyRat"), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

                var bridge = enemy.NavMeshAgentBridge;
                Assert.NotNull(bridge);
                Assert.AreEqual(EnemyLocomotionOwnership.UnityNavMeshAgent, bridge.CurrentOwnership);

                bridge.ClaimHollowManual(clearPath: true, "m101_manual_test");
                Assert.AreEqual(EnemyLocomotionOwnership.HollowManual, bridge.CurrentOwnership);
                Assert.AreEqual("m101_manual_test", bridge.LastOwnershipReason);

                var syncCount = bridge.SyncToTransformCount;
                enemy.transform.localPosition += Vector3.right * 0.05f;
                enemy.SyncNavMeshAgentAfterExternalDisplacement("m101_external_displacement");
                Assert.AreEqual(EnemyLocomotionOwnership.HollowManual, bridge.CurrentOwnership);
                Assert.GreaterOrEqual(bridge.SyncToTransformCount, syncCount);
                StringAssert.Contains("m101_external_displacement", bridge.LastSyncReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeControllerRoutesNavigationMovesThroughSyncHelper()
        {
            var source = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Combat/EnemyRuntimeController.cs");
            Assert.IsFalse(source.Contains("transform.localPosition = ResolveNavigationMove(", System.StringComparison.Ordinal));
            StringAssert.Contains("ApplyNavigationMove", source);
            StringAssert.Contains("active_lunge", source);
            StringAssert.Contains("active_charge", source);
            StringAssert.Contains("bump_separation", source);
        }

        [Test]
        public void DocsReportAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone101NavMeshLocomotionOwnershipAssetGenerator.DocsPath));
            Assert.IsTrue(File.Exists(Milestone101NavMeshLocomotionOwnershipAssetGenerator.ReportPath));
            Assert.IsEmpty(Milestone101NavMeshLocomotionOwnershipValidator.CollectFailures());
        }
    }
}
