using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone99AiPlayabilityStabilizationTests
    {
        private const string MacroSinglePath = "Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_single_1x1.hollowruntime.json";
        private const string MacroBlockPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_block_2x2.hollowruntime.json";
        private const string CoverArenaPath = "Assets/_Hollow/Data/Rooms/DesignerApproved/approved_cover_arena_single_1x1.hollowruntime.json";

        [Test]
        public void PartialSerializedCatalogResolvesAllKnownRuntimeSpawnKindsWithoutGenericFallback()
        {
            var partialCatalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            var normal = EnemyDefinition.CreateRuntimeNormal();
            try
            {
                partialCatalog.Configure(new[] { normal }, normal);

                foreach (var expected in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(definition => definition != null))
                {
                    var resolved = EnemyDefinitionResolver.Resolve(partialCatalog, expected.SpawnKind, out var usedFallback);
                    Assert.IsFalse(usedFallback, expected.SpawnKind);
                    Assert.NotNull(resolved, expected.SpawnKind);
                    Assert.AreEqual(expected.SpawnKind, resolved.SpawnKind, expected.SpawnKind);
                    Assert.AreEqual(expected.DisplayName, resolved.DisplayName, expected.SpawnKind);
                }
            }
            finally
            {
                Object.DestroyImmediate(partialCatalog);
                Object.DestroyImmediate(normal);
            }
        }

        [Test]
        public void UnknownSpawnKindStillFallsBackExplicitlyToNormal()
        {
            var partialCatalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            var normal = EnemyDefinition.CreateRuntimeNormal();
            try
            {
                partialCatalog.Configure(new[] { normal }, normal);
                var resolved = EnemyDefinitionResolver.Resolve(partialCatalog, "spawnEnemyMissingM99", out var usedFallback);

                Assert.IsTrue(usedFallback);
                Assert.NotNull(resolved);
                Assert.AreEqual("spawnEnemyNormal", resolved.SpawnKind);
            }
            finally
            {
                Object.DestroyImmediate(partialCatalog);
                Object.DestroyImmediate(normal);
            }
        }

        [TestCase(MacroSinglePath)]
        [TestCase(MacroBlockPath)]
        public void MacroRoomsBuildRuntimeNavMeshAndCanBeginCombat(string roomPath)
        {
            var root = new GameObject("M99MacroRoomHarness");
            try
            {
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(roomPath)));

                Assert.IsTrue(room.HasNavMeshBake, room.NavMeshBakeError);

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();

                var enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyPrefab.name = "EnemyPrefab";
                enemyPrefab.SetActive(false);
                enemyPrefab.transform.SetParent(root.transform, false);

                var combat = root.AddComponent<RoomCombatController>();
                combat.Configure(enemyPrefab, null, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                combat.BeginRoom(room, player, alreadyCleared: false);

                Assert.AreNotEqual(RoomObjectiveState.WaitingToStart, combat.ObjectiveState);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoomBuildKeepsDoorsVisibleAndKeepsBlockingInteractives()
        {
            var root = new GameObject("M99DoorCarvingHarness");
            try
            {
                var room = root.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(CoverArenaPath)));

                var doorMarkers = root.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name.StartsWith("doorAnchorActive.", System.StringComparison.Ordinal));
                Assert.AreEqual(room.DoorPorts.Count, doorMarkers);
                Assert.Greater(doorMarkers, 0);

                var blockingMarkers = root.GetComponentsInChildren<RoomInteractiveObjectMarker>(true)
                    .Where(marker => marker.BlocksMovement)
                    .ToArray();
                Assert.Greater(blockingMarkers.Length, 0);
                var unavailableDoorColor = MaterialResolver.FallbackColorFor(MaterialRole.DoorUnavailable);
                Assert.GreaterOrEqual(unavailableDoorColor.a, 0.85f);
                Assert.Greater(unavailableDoorColor.grayscale, 0.25f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NavMeshFailureFallsBackToLocalSteeringInsteadOfFreezing()
        {
            var root = new GameObject("M99NavMeshRecoveryHarness");
            try
            {
                var room = root.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(MacroSinglePath)));

                var current = new Vector3(-4f, 0f, -2f);
                var desired = current + Vector3.right * 0.22f;
                var request = new EnemyNavigationRequest(
                    room: room,
                    currentLocalPosition: current,
                    desiredLocalPosition: desired,
                    radiusMeters: 0.34f,
                    mode: EnemyNavigationMode.GroundedLocal,
                    intent: EnemyNavigationIntent.MoveToPlayer,
                    intelligence: EnemyIntelligenceLevel.Basic,
                    allowLocalDetour: true,
                    allowPathfinding: true,
                    finalGoalLocalPosition: new Vector3(2f, 0f, -2f),
                    maxStepDistanceMeters: 0.22f,
                    navMeshAgentBridge: null,
                    desiredSpeedMetersPerSecond: 1.5f);

                var result = EnemyNavigationAdapter.Resolve(request);
                var moved = result.ResolvedLocalPosition - current;
                moved.y = 0f;

                Assert.AreEqual(EnemyNavigationBackend.LocalSteering, result.Backend);
                Assert.AreEqual(EnemyPathStatus.FallbackLocal, result.PathStatus);
                Assert.Greater(moved.magnitude, 0.01f);
                StringAssert.Contains("navmesh_recovery", result.FallbackReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
