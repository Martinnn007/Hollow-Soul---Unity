using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class DeveloperLabSceneExporter
    {
        [MenuItem("Hollow/Developer Lab/Export Selected Developer Lab Scene")]
        public static void ExportSelectedScene()
        {
            var scene = SceneManager.GetActiveScene();
            var metadata = FindMetadata(scene);
            if (metadata == null)
            {
                throw new MissingComponentException("Active scene is missing DeveloperLabSceneRoomMetadata.");
            }

            var room = ExportScene(scene, metadata);
            UpsertContentRoom(room);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Exported Developer Lab scene '{metadata.RoomId}'.");
        }

        [MenuItem("Hollow/Developer Lab/Export All Developer Lab Scenes")]
        public static void ExportAllScenes()
        {
            Directory.CreateDirectory(DeveloperLabSceneGenerator.ContentDataDirectory);
            Directory.CreateDirectory(Milestone55AssetGenerator.LabRoomDirectory);
            var rooms = new List<DeveloperLabRoomContentDefinition>();
            foreach (var scenePath in DeveloperLabSceneGenerator.ScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    throw new FileNotFoundException($"Missing Developer Lab authoring scene: {scenePath}");
                }

                var scene = EditorSceneManager.OpenScene(scenePath);
                var metadata = FindMetadata(scene);
                if (metadata == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing DeveloperLabSceneRoomMetadata.");
                }

                rooms.Add(ExportScene(scene, metadata));
            }

            var content = LoadOrCreateContentDefinition();
            content.Configure("m66_developer_lab_scene_content_v1", rooms);
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Exported all Developer Lab scenes to runtime JSON and content definition.");
        }

        public static DeveloperLabRoomContentDefinition ExportScene(Scene scene, DeveloperLabSceneRoomMetadata metadata)
        {
            var markers = MarkersInScene(scene).ToArray();
            var project = BuildRoomProject(metadata, markers);
            var validation = RoomDesignerDraftValidator.Validate(project);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Developer Lab scene '{metadata.RoomId}' failed Room Designer validation: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = metadata.RoomId;
            manifest.hollowRuntime.displayName = string.IsNullOrWhiteSpace(metadata.Title) ? metadata.RoomId : metadata.Title;
            manifest.hollowRuntime.roomType = "developer-lab";
            manifest.hollowRuntime.rewardType = "inspection-only";
            manifest.hollowRuntime.prototypeStatus = "m66-scene-authored-developer-lab";

            var outputPath = string.IsNullOrWhiteSpace(metadata.OutputJsonPath)
                ? $"{Milestone55AssetGenerator.LabRoomDirectory}/{metadata.RoomId}.hollowruntime.json"
                : metadata.OutputJsonPath;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Milestone55AssetGenerator.LabRoomDirectory);
            File.WriteAllText(outputPath, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            return BuildContentRoom(metadata, markers);
        }

        private static RoomDesignerProject BuildRoomProject(DeveloperLabSceneRoomMetadata metadata, IEnumerable<DeveloperLabSceneMarker> markers)
        {
            var preset = Enum.TryParse<RoomDesignerFootprintPreset>(metadata.FootprintPreset, out var parsed)
                ? parsed
                : RoomDesignerFootprintPreset.Wide2x1;
            var project = RoomDesignerProject.CreateDefault(preset, metadata.Title);
            project.projectId = metadata.RoomId;
            project.displayName = metadata.Title;
            project.cells.RemoveAll(cell => cell.kind != RoomDesignerCellKinds.Ground);
            project.markers.Clear();
            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Inactive;
            }

            foreach (var marker in markers.Where(marker => marker != null && marker.ExportToRuntime))
            {
                switch (marker.Category)
                {
                    case DeveloperLabContentCategory.RoomCell:
                        project.cells.Add(new RoomDesignerCell(
                            Mathf.RoundToInt(marker.transform.localPosition.x),
                            Mathf.RoundToInt(marker.transform.localPosition.z),
                            0,
                            string.IsNullOrWhiteSpace(marker.CellKind) ? RoomDesignerCellKinds.Rock : marker.CellKind));
                        break;
                    case DeveloperLabContentCategory.RoomMarker:
                        project.markers.Add(new RoomDesignerMarker(
                            marker.EntryId,
                            string.IsNullOrWhiteSpace(marker.MarkerKind) ? RoomDesignerMarkerKinds.RoomReward : marker.MarkerKind,
                            marker.transform.localPosition.x,
                            marker.transform.localPosition.y,
                            marker.transform.localPosition.z));
                        break;
                    case DeveloperLabContentCategory.DoorPort:
                        ApplyDoor(project, marker);
                        break;
                }
            }

            if (project.markers.All(marker => marker.kind != RoomDesignerMarkerKinds.SafeStart))
            {
                project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, -10f, 0f, 0f));
            }

            return project;
        }

        private static void ApplyDoor(RoomDesignerProject project, DeveloperLabSceneMarker marker)
        {
            var direction = string.IsNullOrWhiteSpace(marker.DoorDirection) ? "east" : marker.DoorDirection;
            var id = $"{direction}_{marker.DoorLaneIndex}";
            var door = project.doorPorts.FirstOrDefault(candidate => candidate.id == id);
            if (door == null)
            {
                door = RoomDesignerDoorPortState.Create(direction, marker.DoorLaneIndex, marker.transform.localPosition.x, marker.transform.localPosition.z, marker.DoorState, marker.HostCellX, marker.HostCellZ);
                project.doorPorts.Add(door);
            }

            door.direction = direction;
            door.laneIndex = marker.DoorLaneIndex;
            door.hostCellX = marker.HostCellX;
            door.hostCellZ = marker.HostCellZ;
            door.x = marker.transform.localPosition.x;
            door.z = marker.transform.localPosition.z;
            door.state = string.IsNullOrWhiteSpace(marker.DoorState) ? RoomDesignerDoorKinds.Door : marker.DoorState;
        }

        private static DeveloperLabRoomContentDefinition BuildContentRoom(DeveloperLabSceneRoomMetadata metadata, IEnumerable<DeveloperLabSceneMarker> markers)
        {
            var contentEntries = markers
                .Where(marker => marker != null && marker.IncludeInGallery)
                .Select(marker => marker.ToContentEntry())
                .ToArray();
            var room = new DeveloperLabRoomContentDefinition();
            room.Configure(metadata.RoomId, metadata.RoomIndex, metadata.Title, contentEntries);
            return room;
        }

        private static void UpsertContentRoom(DeveloperLabRoomContentDefinition room)
        {
            var content = LoadOrCreateContentDefinition();
            var rooms = content.Rooms
                .Where(existing => existing.RoomIndex != room.RoomIndex && existing.RoomId != room.RoomId)
                .ToList();
            rooms.Add(room);
            rooms = rooms.OrderBy(existing => existing.RoomIndex).ToList();
            content.Configure("m66_developer_lab_scene_content_v1", rooms);
            EditorUtility.SetDirty(content);
        }

        private static DeveloperLabContentDefinition LoadOrCreateContentDefinition()
        {
            Directory.CreateDirectory(DeveloperLabSceneGenerator.ContentDataDirectory);
            var content = AssetDatabase.LoadAssetAtPath<DeveloperLabContentDefinition>(DeveloperLabSceneGenerator.ContentDefinitionPath);
            if (content != null)
            {
                return content;
            }

            content = ScriptableObject.CreateInstance<DeveloperLabContentDefinition>();
            AssetDatabase.CreateAsset(content, DeveloperLabSceneGenerator.ContentDefinitionPath);
            return content;
        }

        private static DeveloperLabSceneRoomMetadata FindMetadata(Scene scene)
        {
            return Object.FindObjectsByType<DeveloperLabSceneRoomMetadata>(FindObjectsSortMode.None)
                .FirstOrDefault(metadata => metadata != null && metadata.gameObject.scene == scene);
        }

        private static IEnumerable<DeveloperLabSceneMarker> MarkersInScene(Scene scene)
        {
            return Object.FindObjectsByType<DeveloperLabSceneMarker>(FindObjectsSortMode.None)
                .Where(marker => marker != null && marker.gameObject.scene == scene)
                .OrderBy(marker => marker.EntryId, StringComparer.Ordinal);
        }
    }
}
