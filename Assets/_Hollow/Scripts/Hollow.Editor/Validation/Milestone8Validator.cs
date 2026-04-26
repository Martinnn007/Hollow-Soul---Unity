using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Core.App;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone8Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/Hollow.RoomDesigner.asmdef",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerProject.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerStore.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerCompiler.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerJsonExporter.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerUsdaExporter.cs",
            "Assets/_Hollow/Scripts/Hollow.RoomDesigner/RoomDesignerController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone8AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone8Validator.cs",
            "Assets/_Hollow/Prefabs/Designer/RoomDesignerRoot.prefab",
            "Assets/_Hollow/Scenes/RoomDesigner.unity",
            "Assets/_Hollow/Tests/EditMode/Milestone8RoomDesignerTests.cs",
            "Docs/Milestone8RoomDesigner.md"
        };

        [MenuItem("Hollow/Validation/Run Milestone 8 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M8 file: {file}");
                }
            }

            ValidateCompiler(failures);
            ValidateBuildSettings(failures);
            ValidateScene(failures);
            ValidateMenuRoute(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 8 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateCompiler(List<string> failures)
        {
            var project = RoomDesignerProject.CreateDefault();
            var json = RoomDesignerCompiler.ExportRuntimeJson(project);
            if (json.Contains("grid.y"))
            {
                failures.Add("M8 runtime JSON must not contain grid.y.");
            }

            if (!HollowRuntimeV2Importer.TryImport(json, out var asset, out var error))
            {
                failures.Add($"M8 default designer project failed V2 import: {error}");
                return;
            }

            if (asset.Layout.WidthTiles != 13 || asset.Layout.HeightTiles != 7)
            {
                failures.Add("M8 default designer room must compile as 13x7.");
            }

            if (asset.DoorPorts.Count != 4 || asset.EnemySpawns.Count != 4 || asset.ItemSpawns.Count != 1 || asset.Layout.Obstacles.Count == 0)
            {
                failures.Add("M8 default designer project must compile door ports, enemy spawns, reward spawn, and obstacles.");
            }
        }

        private static void ValidateBuildSettings(List<string> failures)
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (!enabledScenes.Contains("Assets/_Hollow/Scenes/RoomDesigner.unity"))
            {
                failures.Add("M8 RoomDesigner scene is not enabled in build settings.");
            }
        }

        private static void ValidateScene(List<string> failures)
        {
            EditorSceneManager.OpenScene("Assets/_Hollow/Scenes/RoomDesigner.unity");
            if (Object.FindFirstObjectByType<RoomDesignerController>() == null)
            {
                failures.Add("RoomDesigner scene must contain RoomDesignerController.");
            }

            if (Camera.main == null)
            {
                failures.Add("RoomDesigner scene must contain a MainCamera.");
            }
        }

        private static void ValidateMenuRoute(List<string> failures)
        {
            if (SceneLoaderService.SceneNameForRoute(AppShellRoute.RoomDesigner) != "RoomDesigner")
            {
                failures.Add("RoomDesigner route must resolve to RoomDesigner scene.");
            }

            var controllerType = typeof(MainMenuController);
            if (controllerType.GetMethod("OpenRoomDesigner") == null)
            {
                failures.Add("MainMenuController must expose OpenRoomDesigner.");
            }
        }
    }
}
