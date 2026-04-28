using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class CuratedRoomDesignerDraftGenerator
    {
        public const string CuratedDraftDirectory = "Assets/_Hollow/Data/Rooms/DesignerDrafts/CuratedRuntime";
        public const string CuratedDraftCatalogPath = "Assets/_Hollow/Data/Rooms/DesignerDrafts/CuratedRoomDesignerDraftCatalog.asset";
        public const string RoomDesignerScenePath = "Assets/_Hollow/Scenes/RoomDesigner.unity";

        [MenuItem("Hollow/Generation/Generate Curated Room Designer Drafts")]
        public static void Generate()
        {
            Directory.CreateDirectory(CuratedDraftDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(CuratedDraftCatalogPath) ?? CuratedDraftDirectory);

            var sourcePaths = SourceRuntimeRoomPaths().ToArray();
            var generatedPaths = new List<string>();
            foreach (var sourcePath in sourcePaths)
            {
                var project = RoomDesignerRuntimeDraftImporter.FromRuntimeJson(File.ReadAllText(sourcePath), sourcePath);
                var outputPath = $"{CuratedDraftDirectory}/{project.projectId}.roomdesigner.json";
                File.WriteAllText(outputPath, JsonUtility.ToJson(project, prettyPrint: true));
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                generatedPaths.Add(outputPath);
            }

            AssetDatabase.Refresh();
            var textAssets = generatedPaths
                .Select(path => AssetDatabase.LoadAssetAtPath<TextAsset>(path))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToArray();

            var catalog = AssetDatabase.LoadAssetAtPath<RoomDesignerCuratedDraftCatalogDefinition>(CuratedDraftCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RoomDesignerCuratedDraftCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, CuratedDraftCatalogPath);
            }

            catalog.Configure("curated_runtime_room_designer_drafts_v1", textAssets);
            EditorUtility.SetDirty(catalog);
            AssignCatalogToRoomDesignerScene(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {textAssets.Length} curated Room Designer runtime drafts.");
        }

        public static IReadOnlyList<string> SourceRuntimeRoomPaths()
        {
            var paths = new List<string>();
            paths.AddRange(Milestone13AssetGenerator.MacroFixtures.Select(fixture => fixture.AssetPath));
            paths.AddRange(Milestone36AssetGenerator.ApprovedRoomIds.Select(id => $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{id}.hollowruntime.json"));
            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct()
                .OrderBy(path => path)
                .ToArray();
        }

        public static string CuratedProjectIdForRuntimeRoom(string runtimeJson)
        {
            var asset = HollowRuntimeV2Importer.Import(runtimeJson);
            return $"curated_{Sanitize(asset.Id)}";
        }

        private static void AssignCatalogToRoomDesignerScene(RoomDesignerCuratedDraftCatalogDefinition catalog)
        {
            if (catalog == null || !File.Exists(RoomDesignerScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(RoomDesignerScenePath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<RoomDesignerController>();
            if (controller == null)
            {
                Debug.LogWarning($"Room Designer scene '{RoomDesignerScenePath}' does not contain a RoomDesignerController.");
                return;
            }

            var serializedObject = new SerializedObject(controller);
            var property = serializedObject.FindProperty("curatedDraftCatalog");
            if (property == null)
            {
                Debug.LogWarning("RoomDesignerController.curatedDraftCatalog serialized field was not found.");
                return;
            }

            property.objectReferenceValue = catalog;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static string Sanitize(string value)
        {
            var sanitized = new string((value ?? string.Empty)
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray())
                .Trim('_')
                .ToLowerInvariant();
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "runtime_room" : sanitized;
        }
    }
}
