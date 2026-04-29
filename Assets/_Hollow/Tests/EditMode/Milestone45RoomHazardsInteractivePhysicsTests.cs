using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone45RoomHazardsInteractivePhysicsTests
    {
        [Test]
        public void HazardProfileDefaultLocksM45Values()
        {
            var profile = RoomHazardTuningProfileDefinition.CreateRuntimeDefault();

            Assert.AreEqual(1, profile.SpikeDamage);
            Assert.AreEqual(0.85f, profile.SpikeCooldownSeconds, 0.001f);
            Assert.AreEqual(1.8f, profile.ExplosionRadiusMeters, 0.001f);
            Assert.AreEqual(0.5f, profile.BossExplosionDamageMultiplier, 0.001f);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void DesignerCompilerExportsSpikeAndBarrel()
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "Hazard Room");
            project.cells.Add(new RoomDesignerCell(1, 0, 0, RoomDesignerCellKinds.Spike));
            project.markers.Add(new RoomDesignerMarker("barrel_standard_00", RoomDesignerMarkerKinds.StandardBarrel, 2, 0f, 0));
            project.markers.Add(new RoomDesignerMarker("barrel_explosive_00", RoomDesignerMarkerKinds.ExplosiveBarrel, 3, 0f, 0));

            var asset = RoomDesignerCompiler.Compile(project);

            Assert.AreEqual(1, asset.Hazards.Count);
            Assert.AreEqual(RoomHazardKind.Spike, asset.Hazards[0].kind);
            Assert.AreEqual(2, asset.InteractiveObjects.Count);
            Assert.AreEqual(RoomInteractiveObjectKind.StandardBarrel, asset.InteractiveObjects[0].kind);
            Assert.AreEqual(RoomInteractiveObjectKind.ExplosiveBarrel, asset.InteractiveObjects[1].kind);
        }

        [Test]
        public void RuntimeBuilderCreatesHazardAndInteractiveMarkers()
        {
            var roomObject = new GameObject("Room");
            try
            {
                var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "Hazard Runtime Room");
                project.cells.Add(new RoomDesignerCell(1, 0, 0, RoomDesignerCellKinds.Spike));
                project.markers.Add(new RoomDesignerMarker("barrel_standard_00", RoomDesignerMarkerKinds.StandardBarrel, 2, 0f, 0));
                var room = roomObject.AddComponent<RoomRuntimeRoot>();

                room.BuildFrom(RoomDesignerCompiler.Compile(project));

                Assert.AreEqual(1, room.HazardMarkers.Count);
                Assert.AreEqual(1, room.InteractiveObjectMarkers.Count);
                Assert.IsTrue(RoomLocalCollision.IntersectsObstacle(room, new Vector3(2f, 0f, 0f), 0.25f));

                room.InteractiveObjectMarkers[0].MarkDestroyed();

                Assert.IsFalse(RoomLocalCollision.IntersectsObstacle(room, new Vector3(2f, 0f, 0f), 0.25f));
            }
            finally
            {
                Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void SpikeDamagesPlayerWithEnvironmentalThreat()
        {
            var spikeObject = new GameObject("Spike");
            var playerObject = new GameObject("Player");
            try
            {
                var marker = spikeObject.AddComponent<RoomHazardMarker>();
                marker.Configure(new ImportedRoomHazard
                {
                    id = "spike_test",
                    kind = RoomHazardKind.Spike,
                    center = new ImportedVector3 { x = 0f, y = 0f, z = 0f },
                    radius = 0.55f
                });
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                var health = playerObject.AddComponent<CombatantHealth>();
                health.Configure(6);
                var spike = spikeObject.AddComponent<SpikeHazardController>();
                spike.Configure(marker, null, null, player, RoomHazardTuningProfileDefinition.CreateRuntimeDefault());

                spike.Tick(0f, 0f);

                Assert.AreEqual(5, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(spikeObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void FlyingMoveCanCrossHoleButNotMissingMacroRegion()
        {
            var roomObject = new GameObject("Room");
            try
            {
                var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.L3Cell, "Flying Hole Room");
                project.cells.Add(new RoomDesignerCell(0, 0, 0, RoomDesignerCellKinds.Hole));
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(RoomDesignerCompiler.Compile(project));

                var overHole = RoomLocalCollision.ResolveFlyingMove(room, new Vector3(0f, 0f, 0f), 0.25f);
                var missingQuadrant = RoomLocalCollision.ResolveFlyingMove(room, new Vector3(8f, 0f, 5f), 0.25f);

                Assert.AreEqual(0f, overHole.x, 0.001f);
                Assert.AreEqual(0f, overHole.z, 0.001f);
                Assert.IsFalse(Mathf.Approximately(8f, missingQuadrant.x) && Mathf.Approximately(5f, missingQuadrant.z));
            }
            finally
            {
                Object.DestroyImmediate(roomObject);
            }
        }
    }
}
