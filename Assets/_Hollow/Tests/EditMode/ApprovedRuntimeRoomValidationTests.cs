using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class ApprovedRuntimeRoomValidationTests
    {
        [Test]
        public void AllApprovedRuntimeRoomsHaveValidGameplayPlacements()
        {
            var paths = ApprovedRuntimeRoomPaths();
            Assert.Greater(paths.Length, 0);

            foreach (var path in paths)
            {
                var room = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                var report = RuntimeRoomValidator.Validate(room);
                Assert.IsTrue(report.IsValid, $"{path}: {report.Summary()}");
            }
        }

        [Test]
        public void Milestone84BattlefieldRoomsUseWideConnectedLayouts()
        {
            foreach (var roomId in Milestone84AssetGenerator.BattlefieldRoomIds)
            {
                var path = $"{Milestone84AssetGenerator.BattlefieldRoomDirectory}/{roomId}.hollowruntime.json";
                var room = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                var report = RuntimeRoomValidator.Validate(room);

                Assert.IsTrue(report.IsValid, $"{roomId}: {report.Summary()}");
                Assert.AreEqual(26, room.Layout.WidthTiles, roomId);
                Assert.AreEqual(7, room.Layout.HeightTiles, roomId);
                Assert.AreEqual(2, room.Footprint.OccupiedCells.Count, roomId);
                Assert.AreEqual(6, room.DoorPorts.Count, roomId);
                Assert.IsTrue(room.Layout.Obstacles.All(obstacle => UnityEngine.Mathf.Abs(obstacle.Center.y - obstacle.Size.y * 0.5f) <= 0.001f), roomId);
            }
        }

        [Test]
        public void WatchtowerFastEnemySpawnDoesNotOverlapTopRightRock()
        {
            const string path = "Assets/_Hollow/Data/Rooms/DesignerApproved/approved_watchtower_tall_1x2.hollowruntime.json";
            var room = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
            var fastSpawn = room.EnemySpawns.Single(spawn => spawn.id == "spawn_enemy_3");

            Assert.AreEqual(5f, fastSpawn.position.x);
            Assert.AreEqual(5f, fastSpawn.position.z);
            Assert.IsTrue(RuntimeRoomValidator.Validate(room).IsValid);
        }

        private static string[] ApprovedRuntimeRoomPaths()
        {
            return Directory.GetFiles(Milestone16AssetGenerator.ApprovedRoomDirectory, "*.hollowruntime.json", SearchOption.AllDirectories)
                .OrderBy(path => path)
                .ToArray();
        }
    }
}
