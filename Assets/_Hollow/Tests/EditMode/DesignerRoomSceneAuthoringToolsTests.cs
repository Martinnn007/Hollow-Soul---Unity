using System.IO;
using System.Linq;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Editor.DesignerRooms;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Tests.EditMode
{
    public sealed class DesignerRoomSceneAuthoringToolsTests
    {
        [Test]
        public void EnemyRuntimeKindPaletteIncludesCurrentRoster()
        {
            var options = DesignerRoomSceneAuthoringUtility.RuntimeKindsFor(DesignerRoomSceneMarkerKind.EnemySpawn);

            Assert.Contains(RoomDesignerMarkerKinds.EnemyRat, options.ToArray());
            Assert.Contains(RoomDesignerMarkerKinds.EnemySpider, options.ToArray());
            Assert.Contains(RoomDesignerMarkerKinds.EnemySkeletonSpear, options.ToArray());
            Assert.Contains(RoomDesignerMarkerKinds.EnemyHollowArcher, options.ToArray());
            Assert.Contains(RoomDesignerMarkerKinds.EnemyWraith, options.ToArray());
        }

        [Test]
        public void PolishAuthoringLocalizationExposesCoreLabels()
        {
            var previous = DesignerRoomAuthoringLocalization.CurrentLanguage;
            try
            {
                DesignerRoomAuthoringLocalization.CurrentLanguage = DesignerRoomAuthoringLanguage.Polish;

                Assert.AreEqual("Paleta", DesignerRoomAuthoringLocalization.PanelLabels[0]);
                Assert.AreEqual("Drzwi", DesignerRoomAuthoringLocalization.DisplayNameForRuntimeKind(RoomDesignerDoorKinds.Door));
                Assert.AreEqual("Szczur", DesignerRoomAuthoringLocalization.DisplayNameForRuntimeKind(RoomDesignerMarkerKinds.EnemyRat));
                Assert.AreEqual("Spawn wroga", DesignerRoomAuthoringLocalization.MarkerKindLabel(DesignerRoomSceneMarkerKind.EnemySpawn));
            }
            finally
            {
                DesignerRoomAuthoringLocalization.CurrentLanguage = previous;
            }
        }

        [Test]
        public void SnapMarkerRoundsNormalMarkersToOneMeterGrid()
        {
            var root = CreateRoot();
            var marker = DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.EnemySpawn,
                RoomDesignerMarkerKinds.EnemyRat,
                new Vector3(1.3f, 0f, -1.7f));
            marker.transform.localPosition = new Vector3(2.49f, 0.25f, -2.51f);

            DesignerRoomSceneAuthoringUtility.SnapMarker(marker, recordUndo: false);

            Assert.AreEqual(2f, marker.transform.localPosition.x, 0.001f);
            Assert.AreEqual(-3f, marker.transform.localPosition.z, 0.001f);
        }

        [Test]
        public void SnapDoorMarkerUsesNearestValidRoomEdge()
        {
            var root = CreateRoot();
            var marker = DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.DoorPort,
                RoomDesignerDoorKinds.Door,
                new Vector3(6.2f, 0f, 0.2f));

            DesignerRoomSceneAuthoringUtility.SnapMarker(marker, recordUndo: false);

            Assert.AreEqual("east", marker.DoorDirection);
            Assert.AreEqual(6.5f, marker.transform.localPosition.x, 0.001f);
            Assert.AreEqual(0f, marker.transform.localPosition.z, 0.001f);
        }

        [Test]
        public void BuildProjectFromScenePreservesTypedEnemyAndDoor()
        {
            var root = CreateRoot();
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.SafeStart,
                RoomDesignerMarkerKinds.SafeStart,
                new Vector3(0f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.EnemySpawn,
                RoomDesignerMarkerKinds.EnemyRat,
                new Vector3(3f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.DoorPort,
                RoomDesignerDoorKinds.Door,
                new Vector3(6.5f, 0f, 0f));

            var project = DesignerRoomSceneAuthoringUtility.BuildRoomDesignerProject(SceneManager.GetActiveScene());
            var report = RoomDesignerDraftValidator.Validate(project);

            Assert.IsTrue(report.IsValid, string.Join("; ", report.Errors));
            Assert.IsTrue(project.markers.Any(marker => marker.kind == RoomDesignerMarkerKinds.EnemyRat));
            Assert.IsTrue(project.doorPorts.Any(door => door.direction == "east" && door.state == RoomDesignerDoorKinds.Door));
        }

        [Test]
        public void ValidationReportsMissingSafeStart()
        {
            var root = CreateRoot();
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.EnemySpawn,
                RoomDesignerMarkerKinds.EnemySpider,
                new Vector3(3f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.DoorPort,
                RoomDesignerDoorKinds.Door,
                new Vector3(6.5f, 0f, 0f));

            var report = DesignerRoomSceneAuthoringUtility.ValidateScene(SceneManager.GetActiveScene());

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(error => error.Contains("safe-start")));
        }

        [Test]
        public void VisualPreviewBuildsNonExportedPrefabLayer()
        {
            var root = CreateRoot();
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.SafeStart,
                RoomDesignerMarkerKinds.SafeStart,
                Vector3.zero);
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.EnemySpawn,
                RoomDesignerMarkerKinds.EnemySkeletonSpear,
                new Vector3(2f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.ItemSpawn,
                RoomDesignerMarkerKinds.RoomReward,
                new Vector3(3f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.ItemSpawn,
                RoomDesignerMarkerKinds.ChestSpawn,
                new Vector3(4f, 0f, 0f));
            var obstacle = DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.Obstacle,
                RoomDesignerCellKinds.Rock,
                new Vector3(-2f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.DoorPort,
                RoomDesignerDoorKinds.Door,
                new Vector3(6.5f, 0f, 0f));

            var preview = DesignerRoomSceneVisualPreviewBuilder.BuildPreview(SceneManager.GetActiveScene());

            Assert.IsNotNull(preview);
            Assert.AreEqual(DesignerRoomSceneVisualPreviewBuilder.PreviewRootName, preview.name);
            Assert.AreEqual(0, preview.GetComponentsInChildren<DesignerRoomSceneMarker>(true).Length);
            Assert.IsNotNull(preview.transform.Find("Floor"));
            Assert.IsNotNull(preview.transform.Find("Obstacles"));
            Assert.IsNotNull(preview.transform.Find("Doors"));
            Assert.IsNotNull(preview.transform.Find("Spawns/Items"));
            Assert.IsFalse(HasPreviewChildContaining(preview, "SafeStart."));
            Assert.IsFalse(HasPreviewChildContaining(preview, "Skeleton Spear."));
            Assert.IsFalse(HasPreviewChildContaining(preview, "Room Reward."));
            Assert.IsTrue(HasPreviewChildContaining(preview, "Chest."));
            Assert.GreaterOrEqual(preview.GetComponentsInChildren<Light>(true).Length, 2);
            Assert.IsFalse(obstacle.GetComponent<Renderer>().enabled);

            var project = DesignerRoomSceneAuthoringUtility.BuildRoomDesignerProject(SceneManager.GetActiveScene());
            Assert.IsTrue(project.markers.Any(marker => marker.kind == RoomDesignerMarkerKinds.EnemySkeletonSpear));
        }

        [Test]
        public void VisualPreviewCanBeCleared()
        {
            var root = CreateRoot();
            var safeStart = DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.SafeStart,
                RoomDesignerMarkerKinds.SafeStart,
                Vector3.zero);

            DesignerRoomSceneVisualPreviewBuilder.BuildPreview(SceneManager.GetActiveScene());
            Assert.IsTrue(DesignerRoomSceneVisualPreviewBuilder.HasPreview(SceneManager.GetActiveScene()));
            Assert.IsFalse(safeStart.GetComponent<Renderer>().enabled);

            DesignerRoomSceneVisualPreviewBuilder.ClearPreview(SceneManager.GetActiveScene());

            Assert.IsFalse(DesignerRoomSceneVisualPreviewBuilder.HasPreview(SceneManager.GetActiveScene()));
            Assert.IsTrue(safeStart.GetComponent<Renderer>().enabled);
        }

        [Test]
        public void PlaytestLauncherBuildsRuntimeJsonAndHandoff()
        {
            var root = CreateRoot();
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.SafeStart,
                RoomDesignerMarkerKinds.SafeStart,
                Vector3.zero);
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.EnemySpawn,
                RoomDesignerMarkerKinds.EnemySpider,
                new Vector3(2f, 0f, 0f));
            DesignerRoomSceneAuthoringUtility.CreateMarker(
                root,
                DesignerRoomSceneMarkerKind.DoorPort,
                RoomDesignerDoorKinds.Door,
                new Vector3(6.5f, 0f, 0f));

            var runtimeJson = DesignerRoomPlaytestLauncher.BuildRuntimeJsonForScene(SceneManager.GetActiveScene());

            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(runtimeJson, out var imported, out var error), error);
            Assert.IsTrue(imported.EnemySpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.EnemySpider));

            DesignerRoomPlaytestLauncher.PrimeHandoffForScene(SceneManager.GetActiveScene(), "heavy");
            Assert.IsTrue(RoomPlaytestHandoff.TryConsume(out var handoffJson, out var mode, out var returnRoute, out var characterId));
            Assert.AreEqual(RuntimeSessionMode.TransientRoomDesignerPlaytest, mode);
            Assert.AreEqual(AppShellRoute.MainMenu, returnRoute);
            Assert.AreEqual("heavy", characterId);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(handoffJson, out _, out error), error);
        }

        [Test]
        public void PlaytestLauncherSanitizesUnknownLoadout()
        {
            DesignerRoomPlaytestLauncher.SelectedCharacterId = "unknown";

            Assert.AreEqual("balanced", DesignerRoomPlaytestLauncher.SelectedCharacterId);
        }

        [Test]
        public void PlaytestLauncherGuideArtifactsExist()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_Room_Playtest_Launcher_Guide.md"));
            Assert.IsTrue(File.Exists("output/pdf/Hollow_Room_Playtest_Launcher_Guide.pdf"));
        }

        private static DesignerRoomSceneMarker CreateRoot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rootObject = new GameObject("DesignerRoomRoot.AuthoringTest");
            var root = rootObject.AddComponent<DesignerRoomSceneMarker>();
            root.ConfigureAuthoring(
                "authoring_test_room",
                DesignerRoomSceneMarkerKind.RoomRoot,
                "combat",
                "authoring_test_room",
                string.Empty,
                "Test room root.",
                false,
                "Authoring Test Room",
                true,
                true,
                0.5f);
            return root;
        }

        private static bool HasPreviewChildContaining(GameObject preview, string text)
        {
            return preview.GetComponentsInChildren<Transform>(true)
                .Any(transform => transform.name.IndexOf(text, System.StringComparison.Ordinal) >= 0);
        }
    }
}
