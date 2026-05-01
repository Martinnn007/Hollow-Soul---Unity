using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone66Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/DeveloperLabContentDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/DeveloperLabSceneRoomMetadata.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/DeveloperLabSceneMarker.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/DeveloperLabSceneGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/DeveloperLabSceneExporter.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/DeveloperLabSceneValidator.cs",
            Milestone66AssetGenerator.DocsPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 66 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M66 file: {file}");
                }
            }

            var content = AssetDatabase.LoadAssetAtPath<DeveloperLabContentDefinition>(DeveloperLabSceneGenerator.ContentDefinitionPath);
            if (content != null)
            {
                if (content.Rooms.Count != DeveloperLabDefinition.RoomCount)
                {
                    failures.Add($"Developer Lab content definition should contain {DeveloperLabDefinition.RoomCount} rooms.");
                }
            }

            if (!Directory.Exists(DeveloperLabSceneGenerator.SceneDirectory))
            {
                failures.Add($"Missing Developer Lab scene directory: {DeveloperLabSceneGenerator.SceneDirectory}. Run Hollow/Generation/Generate Milestone 66 Assets.");
            }
            else
            {
                if (!DeveloperLabSceneValidator.ValidateAllScenes())
                {
                    failures.Add("Developer Lab scene validation failed. See console for detailed scene/export/content errors.");
                }
            }

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 66 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }
    }
}
