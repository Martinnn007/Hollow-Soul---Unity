using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Navigation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone97UnityNavMeshReplacementTests
    {
        private const string RatRoomPath = "Assets/_Hollow/Data/Rooms/DesignerApproved/Room_Small_RatRoom_001.hollowruntime.json";
        private const string UnbakedSamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void AdapterUsesUnityNavMeshAsAuthoritativeGroundedBackend()
        {
            Assert.AreEqual(EnemyNavigationBackend.UnityNavMesh, EnemyNavigationAdapter.CurrentBackend);
        }

        [Test]
        public void ApprovedRoomNavMeshCatalogResolvesRatRoom()
        {
            var catalog = RoomNavMeshCatalogDefinition.LoadDefault();
            Assert.IsNotNull(catalog, RoomNavMeshBakeUtility.CatalogAssetPath);
            Assert.IsTrue(catalog.TryGetNavMeshData("Room_Small_RatRoom_001", out var navMeshData));
            Assert.IsNotNull(navMeshData);
        }

        [Test]
        public void RuntimeRoomAttachesBakedNavMeshData()
        {
            var root = new GameObject("M97NavMeshRoom");
            try
            {
                var room = root.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(RatRoomPath)));

                Assert.IsTrue(room.HasNavMeshBake, room.NavMeshBakeError);
                Assert.IsTrue(string.IsNullOrWhiteSpace(room.NavMeshBakeError));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GroundedEnemyReceivesConfiguredNavMeshAgentBridge()
        {
            var root = new GameObject("M97EnemyHarness");
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

                Assert.IsNotNull(enemy.NavMeshAgentBridge);
                Assert.IsNotNull(enemy.NavMeshAgentBridge.Agent);
                Assert.IsTrue(enemy.NavMeshAgentBridge.IsConfiguredForNavigation);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MissingPrebakeBuildsRuntimeNavMeshAndAllowsRoomCombatStart()
        {
            var root = new GameObject("M97MissingBakeHarness");
            try
            {
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(UnbakedSamplePath)));
                Assert.IsTrue(room.HasNavMeshBake, room.NavMeshBakeError);
                Assert.IsTrue(room.HasRuntimeBuiltNavMesh);

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();

                var combat = root.AddComponent<RoomCombatController>();
                combat.BeginRoom(room, player, alreadyCleared: false);

                Assert.AreNotEqual(RoomObjectiveState.WaitingToStart, combat.ObjectiveState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DestroyedBlockingInteractiveDisablesCarvingObstacle()
        {
            var markerObject = new GameObject("BlockingBarrel");
            try
            {
                var marker = markerObject.AddComponent<RoomInteractiveObjectMarker>();
                marker.Configure(new ImportedRoomInteractiveObject
                {
                    id = "barrel_01",
                    kind = RoomInteractiveObjectKind.StandardBarrel,
                    size = new ImportedVector3 { x = 1f, y = 1f, z = 1f },
                    blocksMovement = true
                });
                var obstacle = markerObject.AddComponent<NavMeshObstacle>();
                obstacle.enabled = true;

                marker.MarkDestroyed();

                Assert.IsFalse(obstacle.enabled);
            }
            finally
            {
                Object.DestroyImmediate(markerObject);
            }
        }

        [Test]
        public void DocsReportAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone97NavMeshReplacementAssetGenerator.DocsPath), Milestone97NavMeshReplacementAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone97NavMeshReplacementAssetGenerator.ReportPath), Milestone97NavMeshReplacementAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone97NavMeshReplacementAssetGenerator.DocsPath);
            StringAssert.Contains("Unity NavMesh", markdown);
            StringAssert.Contains("NavMeshAgent", markdown);
            StringAssert.Contains("prebaked", markdown);
            Assert.IsEmpty(Milestone97NavMeshReplacementValidator.CollectFailures());
        }
    }
}
