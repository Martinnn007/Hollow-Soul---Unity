using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Entities;
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
    public static class Milestone2Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.World/Hollow.World.asmdef",
            "Assets/_Hollow/Scripts/Hollow.World/GameSessionController.cs",
            "Assets/_Hollow/Scripts/Hollow.World/GameSessionState.cs",
            "Assets/_Hollow/Scripts/Hollow.Rooms/Hollow.Rooms.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeRoot.cs",
            "Assets/_Hollow/Scripts/Hollow.Entities/Hollow.Entities.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Entities/PlaceholderPlayerController.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/Hollow.Presentation.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Presentation/PlatformPresentationRoot.cs",
            "Assets/_Hollow/Prefabs/Rooms/RoomRuntimeRoot.prefab",
            "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab",
            "Docs/Milestone2SharedRuntimeShell.md"
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive)
        };

        [MenuItem("Hollow/Validation/Run Milestone 2 Validation")]
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
                    failures.Add($"Missing M2 file: {file}");
                }
            }

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            foreach (var (scenePath, _) in GameScenes)
            {
                if (!enabledScenes.Contains(scenePath))
                {
                    failures.Add($"M2 game scene is not enabled in build settings: {scenePath}");
                }
            }

            foreach (var (scenePath, platformKind) in GameScenes)
            {
                ValidateGameScene(scenePath, platformKind, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 2 validation passed.");
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

        private static void ValidateGameScene(string scenePath, HollowPlatformKind expectedPlatformKind, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M2 game scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);

            var sessionController = Object.FindFirstObjectByType<GameSessionController>();
            if (sessionController == null)
            {
                failures.Add($"{scenePath} is missing GameSessionController.");
            }
            else if (sessionController.PlatformKind != expectedPlatformKind)
            {
                failures.Add($"{scenePath} has GameSessionController platform {sessionController.PlatformKind}, expected {expectedPlatformKind}.");
            }

            var presentationRoot = Object.FindFirstObjectByType<PlatformPresentationRoot>();
            if (presentationRoot == null)
            {
                failures.Add($"{scenePath} is missing PlatformPresentationRoot.");
            }
            else
            {
                if (presentationRoot.PlatformKind != expectedPlatformKind)
                {
                    failures.Add($"{scenePath} has presentation platform {presentationRoot.PlatformKind}, expected {expectedPlatformKind}.");
                }

                var expectedScale = PresentationScalePolicy.WorldScaleFor(expectedPlatformKind);
                if (Math.Abs(presentationRoot.WorldScale - expectedScale) > 0.0001f)
                {
                    failures.Add($"{scenePath} has presentation scale {presentationRoot.WorldScale}, expected {expectedScale}.");
                }

                if (Math.Abs(presentationRoot.transform.localScale.x - expectedScale) > 0.0001f)
                {
                    failures.Add($"{scenePath} presentation transform scale is {presentationRoot.transform.localScale.x}, expected {expectedScale}.");
                }
            }

            if (Object.FindFirstObjectByType<RoomRuntimeRoot>() == null)
            {
                failures.Add($"{scenePath} is missing RoomRuntimeRoot.");
            }

            var playerSpawn = Object.FindFirstObjectByType<PlayerSpawnPoint>();
            if (playerSpawn == null)
            {
                failures.Add($"{scenePath} is missing PlayerSpawnPoint.");
            }
            else if (playerSpawn.transform.localPosition.sqrMagnitude > 0.0001f)
            {
                failures.Add($"{scenePath} player spawn is not at room-local origin.");
            }

            if (Object.FindFirstObjectByType<PlaceholderPlayerController>() == null)
            {
                failures.Add($"{scenePath} is missing PlaceholderPlayerController.");
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null)
            {
                failures.Add($"{scenePath} is missing unscaled PlatformShellCanvas.");
            }
            else if (presentationRoot != null && shellCanvas.transform.IsChildOf(presentationRoot.transform))
            {
                failures.Add($"{scenePath} PlatformShellCanvas must not be parented under the scaled world presentation root.");
            }
        }
    }
}
