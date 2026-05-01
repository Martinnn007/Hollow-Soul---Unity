using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone66AssetGenerator
    {
        public const string DocsPath = "Docs/Milestone66DeveloperLabSceneAuthoringPipeline.md";
        public const string ReportPath = "output/reports/m66_developer_lab_scene_authoring_pipeline.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 66 Assets")]
        public static void Generate()
        {
            Milestone55AssetGenerator.Generate();
            DeveloperLabSceneGenerator.GenerateScenes();
            DeveloperLabSceneExporter.ExportAllScenes();
            AssetDatabase.Refresh();
            CuratedRoomDesignerDraftGenerator.Generate();
            AssignToGameScenes(AssetDatabase.LoadAssetAtPath<DeveloperLabContentDefinition>(DeveloperLabSceneGenerator.ContentDefinitionPath));
            WriteDocs();
            WriteReport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 66 Developer Lab scene authoring pipeline assets.");
        }

        private static void AssignToGameScenes(DeveloperLabContentDefinition contentDefinition)
        {
            if (contentDefinition == null)
            {
                throw new FileNotFoundException($"Missing Developer Lab content definition at {DeveloperLabSceneGenerator.ContentDefinitionPath}.");
            }

            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindAnyObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureDeveloperLabContent(contentDefinition);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteDocs()
        {
            WriteText(DocsPath, @"# M66: Developer Lab Unity Scene Authoring Pipeline

M66 turns the Developer Lab into a Unity scene-authored inspection pipeline.

## Workflow
- Open scenes under `Assets/_Hollow/Scenes/DeveloperLab/`.
- Move `AuthoringMarkers/*` objects to change exported runtime positions.
- Keep child meshes and ArtPass previews visual-only; moving child visuals does not affect exported data.
- Use `Hollow/Developer Lab/Export Selected Developer Lab Scene` to export the current scene.
- Use `Hollow/Developer Lab/Export All Developer Lab Scenes` to refresh all room JSON plus the content definition.
- Use `Hollow/Developer Lab/Validate Developer Lab Scenes` before relying on the lab for QA.

## Outputs
- Room shell JSON: `Assets/_Hollow/Data/Rooms/DeveloperLab/{roomId}.hollowruntime.json`
- Gallery layout: `Assets/_Hollow/Data/DeveloperLab/DeveloperLabContentDefinition.asset`
- Runtime scenes read the content definition through `BranchSessionController`.

## Rules
- Developer Lab remains non-authoritative for saves, rewards, challenge records, and normal branch generation.
- Scene markers are source-of-truth for Developer Lab only.
- Room Designer and normal runtime rooms are unchanged by moving lab markers until the export tools are run.
");
        }

        private static void WriteReport()
        {
            WriteText(ReportPath, $@"# M66 Developer Lab Scene Authoring Pipeline Report

- Scene folder: `{DeveloperLabSceneGenerator.SceneDirectory}`
- Content asset: `{DeveloperLabSceneGenerator.ContentDefinitionPath}`
- Runtime JSON folder: `{Milestone55AssetGenerator.LabRoomDirectory}`
- Expected scenes: {DeveloperLabDefinition.RoomCount}
- Game scene wiring: BranchSessionController now has a Developer Lab content definition slot.

Manual export remains required; saving a Unity lab scene does not automatically mutate runtime JSON.
");
        }

        private static void WriteText(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents);
        }
    }
}
