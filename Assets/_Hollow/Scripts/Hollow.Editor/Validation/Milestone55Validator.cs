using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone55Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Branches/DeveloperLabDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/DeveloperInspectionBranchBuilder.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/DeveloperLabRoomPopulator.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/DebugSpawnMenuController.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/InspectionEntityMode.cs",
            Milestone55AssetGenerator.DocsPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 55 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M55 file: {file}");
                }
            }

            foreach (var roomId in DeveloperLabDefinition.RoomAssetIds)
            {
                var path = $"{Milestone55AssetGenerator.LabRoomDirectory}/{roomId}.hollowruntime.json";
                if (!File.Exists(path))
                {
                    failures.Add($"Missing generated Developer Lab room: {path}");
                }
            }

            ValidateGraph(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 55 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateGraph(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<Hollow.Data.Definitions.BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null || catalog.Wide2x1 == null)
            {
                failures.Add("M55 requires the branch room template catalog with a Wide2x1 fixture.");
                return;
            }

            if (!HollowRuntimeV2Importer.TryImport(catalog.Single1x1 != null ? catalog.Single1x1.text : catalog.Wide2x1.text, out var legacy, out var legacyError))
            {
                failures.Add($"M55 could not import a legacy room fixture: {legacyError}");
                return;
            }

            var content = BranchSessionContent.Create(legacy, catalog, DeveloperLabDefinition.Seed, out var importError);
            if (!string.IsNullOrWhiteSpace(importError))
            {
                failures.Add($"M55 branch content import warning must be resolved: {importError}");
                return;
            }

            var graph = DeveloperInspectionBranchBuilder.CreateGraph(content, DeveloperLabDefinition.Seed);
            if (graph.BranchId != DeveloperLabDefinition.BranchId)
            {
                failures.Add("Developer Lab graph has the wrong branch id.");
            }

            if (graph.RoomCount != DeveloperLabDefinition.RoomCount)
            {
                failures.Add($"Developer Lab graph should contain {DeveloperLabDefinition.RoomCount} rooms.");
            }

            for (var index = 0; index < DeveloperLabDefinition.RoomCount; index++)
            {
                var roomId = index == 0 ? BranchRoomId.Origin : new BranchRoomId($"lab_room_{index + 1:00}");
                if (!graph.TryGetRoom(roomId, out var room))
                {
                    failures.Add($"Developer Lab graph is missing room {roomId}.");
                    continue;
                }

                if (room.Coordinate != new Vector2Int(index * 2, 0))
                {
                    failures.Add($"Developer Lab room {roomId} must be placed left-to-right at x={index * 2}.");
                }

                if (RoomFootprintShapeUtility.Classify(room.Footprint) != RoomFootprintShape.Wide2x1)
                {
                    failures.Add($"Developer Lab room {roomId} must use a Wide2x1 footprint.");
                }

                if (!room.IsCleared)
                {
                    failures.Add($"Developer Lab room {roomId} must start pre-cleared.");
                }
            }

            if (graph.Connections.Count != (DeveloperLabDefinition.RoomCount - 1) * 2)
            {
                failures.Add("Developer Lab graph must be a single bidirectional left-to-right chain.");
            }

            if (!graph.Connections.All(connection => connection.FromDirection is "east" or "west" && connection.HasExplicitPorts))
            {
                failures.Add("Developer Lab graph connections must use explicit east/west ports.");
            }
        }
    }
}
