using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class Milestone102ReachableTacticalPositioningTests
    {
        private const string RatRoomPath = "Assets/_Hollow/Data/Rooms/DesignerApproved/Room_Small_RatRoom_001.hollowruntime.json";

        [Test]
        public void ActiveThreatReservationsAreNavMeshReachable()
        {
            var root = CreateRuntimeHarness(RatRoomPath, out var room, out var player);
            try
            {
                var enemies = CreateEnemyRing(root.transform, room, player, 7);
                var director = new RoomTacticalDirector();
                director.Tick(enemies, room, player, 8f);

                var activeIntents = enemies
                    .Select(enemy => director.ResolveIntent(enemy))
                    .Where(intent => intent.Role == EnemyTacticalRole.ActiveThreat)
                    .ToArray();

                Assert.Greater(activeIntents.Length, 0);
                foreach (var intent in activeIntents)
                {
                    Assert.IsTrue(intent.HasReservedPosition, intent.Summary);
                    Assert.IsTrue(intent.HasReachableReservedPosition, intent.Summary);
                    Assert.AreEqual(EnemyPathStatus.Ready, intent.ReservationPathStatus, intent.Summary);
                    Assert.GreaterOrEqual(intent.ReservationPathCornerCount, 1, intent.Summary);
                    StringAssert.Contains("navmesh_reachable", intent.Reason);
                    Assert.IsTrue(RoomLocalCollision.CanOccupy(room, intent.ReservedLocalPosition, 0.32f), intent.Summary);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReservationHelperRejectsDisconnectedNavMeshIslands()
        {
            var root = new GameObject("M102DisconnectedHarness");
            try
            {
                var roomObject = new GameObject("DisconnectedRoom");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(CreateDisconnectedRoomAsset(), RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);
                Assert.IsTrue(room.HasNavMeshBake, room.NavMeshBakeError);

                var reachable = RoomTacticalDirector.TryResolveReachableReservation(
                    room,
                    new Vector3(-3f, 0f, 0f),
                    new Vector3(3f, 0f, 0f),
                    0.25f,
                    out _,
                    out var status,
                    out _,
                    out _,
                    out var reason);

                Assert.IsFalse(reachable, reason);
                Assert.That(status, Is.EqualTo(EnemyPathStatus.Partial).Or.EqualTo(EnemyPathStatus.Unreachable));
                Assert.IsTrue(
                    reason.Contains("navmesh_path", System.StringComparison.Ordinal) ||
                    reason.Contains("candidate_not_on_navmesh", System.StringComparison.Ordinal),
                    reason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsReportAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone102ReachableTacticalPositioningAssetGenerator.DocsPath), Milestone102ReachableTacticalPositioningAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone102ReachableTacticalPositioningAssetGenerator.ReportPath), Milestone102ReachableTacticalPositioningAssetGenerator.ReportPath);
            Assert.IsEmpty(Milestone102ReachableTacticalPositioningValidator.CollectFailures());
        }

        private static GameObject CreateRuntimeHarness(string roomPath, out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M102ReachableHarness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(roomPath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            player.transform.localPosition = Vector3.zero;
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            return root;
        }

        private static EnemyRuntimeController[] CreateEnemyRing(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, int count)
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            var result = new EnemyRuntimeController[count];
            for (var index = 0; index < count; index++)
            {
                var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.transform.SetParent(parent, false);
                enemyObject.transform.localPosition = Quaternion.Euler(0f, index * (360f / count), 0f) * Vector3.forward * 3f;
                var definition = catalog.Resolve(index % 2 == 0 ? "spawnEnemySkeletonSword" : "spawnEnemyNormal");
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                enemy.ConfigureSpawnContext(null, null, catalog, DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel(), index);
                result[index] = enemy;
            }

            return result;
        }

        private static ImportedRoomRuntimeAsset CreateDisconnectedRoomAsset()
        {
            var layout = new RoomLayout(
                10,
                4,
                Rect.MinMaxRect(-5f, -2f, 5f, 2f),
                new List<Vector2Int>(),
                new List<Vector2Int>(),
                new[]
                {
                    new RoomLayoutFloorRegion("left_island", new Vector3(-3f, 0f, 0f), new Vector2(1f, 1f)),
                    new RoomLayoutFloorRegion("right_island", new Vector3(3f, 0f, 0f), new Vector2(1f, 1f))
                },
                new List<RoomLayoutObstacle>());

            return new ImportedRoomRuntimeAsset(
                "m102_disconnected_islands",
                "M102 Disconnected Islands",
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(10, 4)),
                new List<RoomDoorPort>(),
                new List<ImportedSpawnPoint>(),
                new List<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawn_point_safeStart",
                    position = new ImportedVector3 { x = -3f, y = 0f, z = 0f }
                },
                new List<ImportedRoomHazard>(),
                new List<ImportedRoomInteractiveObject>(),
                new List<ImportedRoomDecor>(),
                null);
        }
    }
}
