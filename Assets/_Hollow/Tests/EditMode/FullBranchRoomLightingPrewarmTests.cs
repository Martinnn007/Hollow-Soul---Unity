using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hollow.Branches;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class FullBranchRoomLightingPrewarmTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (var index = 0; index < createdObjects.Count; index++)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void FullBranchCachePreparesEveryRoomBeforeActivation()
        {
            var cache = new BranchLiveRoomCache();
            var parent = CreateParent("LightingPrewarmCacheParent");
            var rooms = CreateRooms();
            var assets = CreateAssets(rooms);

            Run(cache.BuildBranchRooms(rooms, room => assets[room.Id.Value], parent));

            foreach (var room in rooms)
            {
                Assert.IsTrue(cache.TryGetRoom(room.Id, out var instance));
                Assert.IsFalse(instance.RootObject.activeSelf, "Cached room should remain hidden before entry.");
                var lighting = instance.RuntimeRoot.GetComponent<RoomLightingController>();
                Assert.NotNull(lighting);
                Assert.IsTrue(lighting.IsPreparedFor(assets[room.Id.Value].BiomeId));
                Assert.AreEqual(string.Empty, lighting.AppliedBiomeId, "Hidden rooms should be prepared without applying active-room globals.");
                Assert.NotNull(instance.RuntimeRoot.transform.Find("RoomLightingRig/BiomeKeyLight"));
            }

            cache.DisposeBranchRooms();
        }

        [Test]
        public void InactiveCachedRoomsDoNotLeakActiveLights()
        {
            var activeLightCountBefore = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Count(light => light.enabled && light.gameObject.activeInHierarchy);
            var cache = new BranchLiveRoomCache();
            var parent = CreateParent("LightingPrewarmNoLeakParent");
            var rooms = CreateRooms();
            var assets = CreateAssets(rooms);

            Run(cache.BuildBranchRooms(rooms, room => assets[room.Id.Value], parent));

            var activeLightCountAfter = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Count(light => light.enabled && light.gameObject.activeInHierarchy);
            Assert.AreEqual(activeLightCountBefore, activeLightCountAfter);
            cache.DisposeBranchRooms();
        }

        [Test]
        public void ActivateRoomAppliesGlobalLightingSynchronouslyBeforeReveal()
        {
            var cache = new BranchLiveRoomCache();
            var parent = CreateParent("LightingPrewarmActivateParent");
            var room = CreateRoom(BranchRoomId.Origin, "origin_asset", BranchRoomRole.Origin);
            var asset = CreateRuntimeRoomAsset("origin_asset", RoomBiomeIds.VerdantRuins);

            Run(cache.BuildBranchRooms(new[] { room }, candidate => asset, parent));
            Assert.IsTrue(cache.PrepareRoomForEntry(room.Id));
            var activationFrame = Time.frameCount;
            Assert.IsTrue(cache.ActivateRoom(room.Id));

            Assert.IsTrue(cache.TryGetRoom(room.Id, out var instance));
            var lighting = instance.RuntimeRoot.GetComponent<RoomLightingController>();
            Assert.NotNull(lighting);
            Assert.AreEqual(RoomBiomeIds.VerdantRuins, lighting.AppliedBiomeId);
            Assert.LessOrEqual(lighting.GlobalAppliedFrame, activationFrame);
            Assert.AreEqual(RoomBiomeIds.VerdantRuins, BiomeLightingDiagnostics.LastSnapshot.BiomeId);
            Assert.IsTrue(instance.RootObject.activeSelf);
            cache.DisposeBranchRooms();
        }

        [Test]
        public void SpaceshipStyleFullRoomCachePreparesAllStaticRooms()
        {
            var cache = new BranchLiveRoomCache();
            var parent = CreateParent("LightingPrewarmShipParent");
            var rooms = new[]
            {
                CreateRoom(BranchRoomId.Origin, "ship_arrivals", BranchRoomRole.Origin),
                CreateRoom(BranchRoomId.North, "ship_main_hall", BranchRoomRole.Reward),
                CreateRoom(BranchRoomId.East, "ship_mission_center", BranchRoomRole.Treasure)
            };
            var assets = new Dictionary<string, ImportedRoomRuntimeAsset>(StringComparer.Ordinal)
            {
                ["origin"] = CreateRuntimeRoomAsset("ship_arrivals", RoomBiomeIds.HollowThreshold),
                ["north"] = CreateRuntimeRoomAsset("ship_main_hall", RoomBiomeIds.VerdantRuins),
                ["east"] = CreateRuntimeRoomAsset("ship_mission_center", RoomBiomeIds.CorruptedAshenShrine)
            };

            Run(cache.BuildBranchRooms(rooms, room => assets[room.Id.Value], parent));

            foreach (var room in rooms)
            {
                Assert.IsTrue(cache.TryGetRoom(room.Id, out var instance));
                var lighting = instance.RuntimeRoot.GetComponent<RoomLightingController>();
                Assert.NotNull(lighting);
                Assert.IsTrue(lighting.IsPreparedFor(assets[room.Id.Value].BiomeId));
            }

            cache.DisposeBranchRooms();
        }

        private Transform CreateParent(string name)
        {
            var parent = new GameObject(name);
            createdObjects.Add(parent);
            return parent.transform;
        }

        private static void Run(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
            }
        }

        private static BranchRoomState[] CreateRooms()
        {
            return new[]
            {
                CreateRoom(BranchRoomId.Origin, "origin_asset", BranchRoomRole.Origin),
                CreateRoom(BranchRoomId.North, "north_asset", BranchRoomRole.Combat),
                CreateRoom(BranchRoomId.East, "east_asset", BranchRoomRole.Reward)
            };
        }

        private static Dictionary<string, ImportedRoomRuntimeAsset> CreateAssets(IEnumerable<BranchRoomState> rooms)
        {
            var biomes = new[] { RoomBiomeIds.HollowThreshold, RoomBiomeIds.VerdantRuins, RoomBiomeIds.CorruptedAshenShrine };
            var assets = new Dictionary<string, ImportedRoomRuntimeAsset>(StringComparer.Ordinal);
            var index = 0;
            foreach (var room in rooms)
            {
                assets[room.Id.Value] = CreateRuntimeRoomAsset(room.RuntimeRoomAssetId, biomes[index % biomes.Length]);
                index++;
            }

            return assets;
        }

        private static BranchRoomState CreateRoom(BranchRoomId id, string roomAssetId, BranchRoomRole role)
        {
            return new BranchRoomState(
                id,
                Vector2Int.zero,
                new BranchRoomInstanceId(id.Value),
                roomAssetId,
                null,
                role);
        }

        private static ImportedRoomRuntimeAsset CreateRuntimeRoomAsset(string id, string biomeId)
        {
            var layout = new RoomLayout(
                4,
                4,
                Rect.MinMaxRect(-2f, -2f, 2f, 2f),
                Array.Empty<Vector2Int>(),
                Array.Empty<Vector2Int>(),
                new[] { new RoomLayoutFloorRegion("floor", Vector3.zero, new Vector2(2f, 2f)) },
                Array.Empty<RoomLayoutObstacle>());
            return new ImportedRoomRuntimeAsset(
                id,
                id,
                biomeId,
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(4, 4)),
                Array.Empty<RoomDoorPort>(),
                Array.Empty<ImportedSpawnPoint>(),
                Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawnSafeStart",
                    position = new ImportedVector3 { x = 0f, y = 0f, z = 0f }
                },
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                null);
        }
    }
}
