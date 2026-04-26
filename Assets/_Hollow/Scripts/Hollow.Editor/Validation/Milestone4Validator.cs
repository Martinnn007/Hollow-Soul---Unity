using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone4Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/Hollow.Combat.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Combat/RoomCombatController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/PlayerMovementController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/PlayerWeaponController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/ProjectileController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/ChaserEnemyController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/CombatHudController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone4AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone4Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone4CombatLoopTests.cs",
            "Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab",
            "Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab",
            "Docs/Milestone4PlayableCombatLoop.md"
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive)
        };

        [MenuItem("Hollow/Validation/Run Milestone 4 Validation")]
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
                    failures.Add($"Missing M4 file: {file}");
                }
            }

            ValidatePrefabs(failures);
            foreach (var (scenePath, platformKind) in GameScenes)
            {
                ValidateScene(scenePath, platformKind, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 4 validation passed.");
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

        private static void ValidatePrefabs(List<string> failures)
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab");
            if (player == null ||
                player.GetComponent<PlaceholderPlayerController>() == null ||
                player.GetComponent<CombatantHealth>() == null ||
                player.GetComponent<PlayerMovementController>() == null ||
                player.GetComponent<PlayerWeaponController>() == null)
            {
                failures.Add("PlayerCharacter.prefab is missing M4 playable player components.");
            }

            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab");
            if (enemy == null || enemy.GetComponent<CombatantHealth>() == null || enemy.GetComponent<ChaserEnemyController>() == null)
            {
                failures.Add("EnemyBase.prefab is missing M4 chaser enemy components.");
            }

            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab");
            if (projectile == null || projectile.GetComponent<ProjectileController>() == null)
            {
                failures.Add("ProjectileBase.prefab is missing M4 projectile component.");
            }
        }

        private static void ValidateScene(string scenePath, HollowPlatformKind expectedPlatformKind, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M4 game scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            var combat = Object.FindFirstObjectByType<RoomCombatController>();
            if (combat == null)
            {
                failures.Add($"{scenePath} is missing RoomCombatController.");
            }
            else
            {
                if (combat.EnemyPrefab == null)
                {
                    failures.Add($"{scenePath} RoomCombatController has no enemy prefab.");
                }

                if (combat.ProjectilePrefab == null)
                {
                    failures.Add($"{scenePath} RoomCombatController has no projectile prefab.");
                }
            }

            var session = Object.FindFirstObjectByType<GameSessionController>();
            if (session == null || session.SampleRoomRuntimeJson == null)
            {
                failures.Add($"{scenePath} is missing GameSessionController sample room JSON.");
            }

            var presentationRoot = Object.FindFirstObjectByType<PlatformPresentationRoot>();
            if (presentationRoot == null)
            {
                failures.Add($"{scenePath} is missing PlatformPresentationRoot.");
            }
            else if (Mathf.Abs(presentationRoot.WorldScale - PresentationScalePolicy.WorldScaleFor(expectedPlatformKind)) > 0.0001f)
            {
                failures.Add($"{scenePath} has incorrect presentation scale.");
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null)
            {
                failures.Add($"{scenePath} is missing PlatformShellCanvas.");
            }
            else if (presentationRoot != null && shellCanvas.transform.IsChildOf(presentationRoot.transform))
            {
                failures.Add($"{scenePath} PlatformShellCanvas must stay outside WorldPresentationRoot.");
            }
        }
    }
}
