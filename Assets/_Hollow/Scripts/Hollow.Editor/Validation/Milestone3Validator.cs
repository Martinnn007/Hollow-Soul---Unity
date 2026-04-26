using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using Hollow.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone3Validator
    {
        private const string SampleRoomRuntimePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rooms/HollowRuntimeV2Importer.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/HollowRuntimeV2Models.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomLayout.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomLayoutObstacle.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomDoorPort.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone3AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone3Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone3HollowRuntimeImportTests.cs",
            SampleRoomRuntimePath,
            "Docs/Milestone3HollowRuntimeV2Import.md"
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive)
        };

        [MenuItem("Hollow/Validation/Run Milestone 3 Validation")]
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
                    failures.Add($"Missing M3 file: {file}");
                }
            }

            var sampleTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(SampleRoomRuntimePath);
            if (sampleTextAsset == null)
            {
                failures.Add($"M3 sample JSON did not import as a TextAsset: {SampleRoomRuntimePath}");
            }
            else
            {
                ValidateImportedAsset(sampleTextAsset.text, failures);
            }

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            foreach (var (scenePath, platformKind) in GameScenes)
            {
                if (!enabledScenes.Contains(scenePath))
                {
                    failures.Add($"M3 game scene is not enabled in build settings: {scenePath}");
                }

                ValidateScene(scenePath, platformKind, sampleTextAsset, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 3 validation passed.");
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

        private static void ValidateImportedAsset(string json, List<string> failures)
        {
            if (!HollowRuntimeV2Importer.TryImport(json, out var asset, out var error))
            {
                failures.Add($"M3 sample JSON failed import: {error}");
                return;
            }

            if (asset.Layout.WidthTiles != 13 || asset.Layout.HeightTiles != 7)
            {
                failures.Add($"M3 sample room dimensions are {asset.Layout.WidthTiles}x{asset.Layout.HeightTiles}, expected 13x7.");
            }

            if (asset.DoorPorts.Count != 4)
            {
                failures.Add($"M3 sample door count is {asset.DoorPorts.Count}, expected 4.");
            }

            if (asset.Layout.Obstacles.Count != 16)
            {
                failures.Add($"M3 sample obstacle count is {asset.Layout.Obstacles.Count}, expected 16.");
            }

            if (asset.EnemySpawns.Count != 4)
            {
                failures.Add($"M3 sample enemy spawn count is {asset.EnemySpawns.Count}, expected 4.");
            }
        }

        private static void ValidateScene(string scenePath, HollowPlatformKind expectedPlatformKind, TextAsset expectedSample, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M3 game scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            var controller = Object.FindFirstObjectByType<GameSessionController>();
            if (controller == null)
            {
                failures.Add($"{scenePath} is missing GameSessionController.");
                return;
            }

            if (controller.PlatformKind != expectedPlatformKind)
            {
                failures.Add($"{scenePath} has platform {controller.PlatformKind}, expected {expectedPlatformKind}.");
            }

            if (controller.SampleRoomRuntimeJson == null)
            {
                failures.Add($"{scenePath} GameSessionController has no M3 sample JSON assigned.");
            }
            else if (expectedSample != null && controller.SampleRoomRuntimeJson != expectedSample)
            {
                failures.Add($"{scenePath} GameSessionController references {controller.SampleRoomRuntimeJson.name}, expected {expectedSample.name}.");
            }

            var presentationRoot = Object.FindFirstObjectByType<PlatformPresentationRoot>();
            if (presentationRoot == null)
            {
                failures.Add($"{scenePath} is missing PlatformPresentationRoot.");
            }
            else
            {
                var expectedScale = PresentationScalePolicy.WorldScaleFor(expectedPlatformKind);
                if (Math.Abs(presentationRoot.WorldScale - expectedScale) > 0.0001f)
                {
                    failures.Add($"{scenePath} presentation scale is {presentationRoot.WorldScale}, expected {expectedScale}.");
                }
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas != null && presentationRoot != null && shellCanvas.transform.IsChildOf(presentationRoot.transform))
            {
                failures.Add($"{scenePath} PlatformShellCanvas must stay outside WorldPresentationRoot.");
            }
        }
    }
}
