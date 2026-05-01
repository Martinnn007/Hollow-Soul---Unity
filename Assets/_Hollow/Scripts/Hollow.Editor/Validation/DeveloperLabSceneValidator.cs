using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class DeveloperLabSceneValidator
    {
        [MenuItem("Hollow/Developer Lab/Validate Developer Lab Scenes")]
        public static bool ValidateAllScenes()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateSceneFiles(failures);
            ValidateExportedRuntime(failures);
            ValidateContentDefinition(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Developer Lab scene validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateSceneFiles(List<string> failures)
        {
            RoomDesignerFootprintUtility.RoomBounds(RoomDesignerFootprintPreset.Wide2x1, out var minX, out var maxX, out var minZ, out var maxZ);
            for (var index = 0; index < DeveloperLabDefinition.RoomCount; index++)
            {
                var path = DeveloperLabSceneGenerator.ScenePaths[index];
                if (!File.Exists(path))
                {
                    failures.Add($"Missing Developer Lab scene: {path}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path);
                var metadata = FindMetadata(scene);
                if (metadata == null)
                {
                    failures.Add($"{path} is missing DeveloperLabSceneRoomMetadata.");
                    continue;
                }

                if (metadata.RoomIndex != index + 1)
                {
                    failures.Add($"{path} has room index {metadata.RoomIndex}; expected {index + 1}.");
                }

                if (metadata.RoomId != DeveloperLabDefinition.RoomAssetIds[index])
                {
                    failures.Add($"{path} has room id '{metadata.RoomId}'; expected '{DeveloperLabDefinition.RoomAssetIds[index]}'.");
                }

                var markers = MarkersInScene(scene).ToArray();
                if (markers.Length == 0)
                {
                    failures.Add($"{path} has no DeveloperLabSceneMarker entries.");
                }

                if (!markers.Any(marker => marker.IncludeInGallery))
                {
                    failures.Add($"{path} has no gallery markers.");
                }

                if (!markers.Any(marker => marker.ExportToRuntime && marker.Category == DeveloperLabContentCategory.DoorPort && marker.DoorState != RoomDesignerDoorKinds.Inactive))
                {
                    failures.Add($"{path} has no active exported door marker.");
                }

                foreach (var marker in markers)
                {
                    var position = marker.transform.localPosition;
                    if (position.x < minX - 0.5f || position.x > maxX + 0.5f || position.z < minZ - 0.5f || position.z > maxZ + 0.5f)
                    {
                        failures.Add($"{path} marker '{marker.EntryId}' is outside Wide2x1 bounds at {position}.");
                    }
                }
            }
        }

        private static void ValidateExportedRuntime(List<string> failures)
        {
            foreach (var roomId in DeveloperLabDefinition.RoomAssetIds)
            {
                var path = $"{Milestone55AssetGenerator.LabRoomDirectory}/{roomId}.hollowruntime.json";
                if (!File.Exists(path))
                {
                    failures.Add($"Missing exported Developer Lab runtime JSON: {path}");
                    continue;
                }

                if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(path), out var room, out var error))
                {
                    failures.Add($"Failed to import exported Developer Lab room '{roomId}': {error}");
                    continue;
                }

                if (RoomFootprintShapeUtility.Classify(room.Footprint) != RoomFootprintShape.Wide2x1)
                {
                    failures.Add($"Developer Lab room '{roomId}' must export as Wide2x1.");
                }
            }
        }

        private static void ValidateContentDefinition(List<string> failures)
        {
            var content = AssetDatabase.LoadAssetAtPath<DeveloperLabContentDefinition>(DeveloperLabSceneGenerator.ContentDefinitionPath);
            if (content == null)
            {
                failures.Add($"Missing Developer Lab content definition: {DeveloperLabSceneGenerator.ContentDefinitionPath}");
                return;
            }

            if (content.Rooms.Count != DeveloperLabDefinition.RoomCount)
            {
                failures.Add($"Developer Lab content definition should contain {DeveloperLabDefinition.RoomCount} rooms, found {content.Rooms.Count}.");
            }

            for (var index = 0; index < DeveloperLabDefinition.RoomCount; index++)
            {
                if (!content.TryGetRoom(DeveloperLabDefinition.RoomAssetIds[index], index + 1, out var room))
                {
                    failures.Add($"Developer Lab content definition is missing room {DeveloperLabDefinition.RoomAssetIds[index]}.");
                    continue;
                }

                if (room.Entries.Count == 0)
                {
                    failures.Add($"Developer Lab content room '{room.RoomId}' has no gallery entries.");
                }
            }
        }

        private static DeveloperLabSceneRoomMetadata FindMetadata(Scene scene)
        {
            return Object.FindObjectsByType<DeveloperLabSceneRoomMetadata>(FindObjectsSortMode.None)
                .FirstOrDefault(metadata => metadata != null && metadata.gameObject.scene == scene);
        }

        private static IEnumerable<DeveloperLabSceneMarker> MarkersInScene(Scene scene)
        {
            return Object.FindObjectsByType<DeveloperLabSceneMarker>(FindObjectsSortMode.None)
                .Where(marker => marker != null && marker.gameObject.scene == scene);
        }
    }
}
