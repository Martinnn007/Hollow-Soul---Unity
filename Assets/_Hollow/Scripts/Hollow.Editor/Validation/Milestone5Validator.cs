using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone5Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyCatalog.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyRuntimeController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/DifficultyTierDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/CombatReadabilityPresenter.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/CombatDiagnosticsModel.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/BossEncounterService.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone5AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone5Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone5EnemyArchetypeTests.cs",
            "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset",
            "Assets/_Hollow/Data/Enemies/Difficulty_DeveloperSample.asset",
            "Docs/Milestone5EnemyArchetypesAndDiagnostics.md"
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive)
        };

        [MenuItem("Hollow/Validation/Run Milestone 5 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();

            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M5 file: {file}");
                }
            }

            ValidateDataAssets(failures);
            ValidatePrefabs(failures);
            foreach (var (scenePath, platformKind) in GameScenes)
            {
                ValidateScene(scenePath, platformKind, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 5 validation passed.");
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

        private static void ValidateDataAssets(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/_Hollow/Data/Enemies/EnemyCatalog.asset");
            if (catalog == null || catalog.Definitions.Count < 4)
            {
                failures.Add("EnemyCatalog.asset must contain at least the four default enemy definitions.");
                return;
            }

            ValidateDefinition(catalog, "spawnEnemyNormal", EnemyArchetypeId.Normal, failures);
            ValidateDefinition(catalog, "spawnEnemyFlying", EnemyArchetypeId.Flying, failures);
            ValidateDefinition(catalog, "spawnEnemyFast", EnemyArchetypeId.Fast, failures);
            ValidateDefinition(catalog, "spawnEnemyHeavy", EnemyArchetypeId.Heavy, failures);

            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyTierDefinition>("Assets/_Hollow/Data/Enemies/Difficulty_DeveloperSample.asset");
            if (difficulty == null || difficulty.DisplayName != "Developer Sample")
            {
                failures.Add("Difficulty_DeveloperSample.asset is missing or has the wrong display name.");
            }
        }

        private static void ValidateDefinition(EnemyCatalog catalog, string spawnKind, EnemyArchetypeId archetypeId, List<string> failures)
        {
            var definition = catalog.Resolve(spawnKind);
            if (definition == null || definition.ArchetypeId != archetypeId)
            {
                failures.Add($"EnemyCatalog failed to resolve {spawnKind} as {archetypeId}.");
            }
        }

        private static void ValidatePrefabs(List<string> failures)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab");
            if (enemy == null || enemy.GetComponent<EnemyRuntimeController>() == null || enemy.GetComponent<CombatantHealth>() == null)
            {
                failures.Add("EnemyBase.prefab must include EnemyRuntimeController-compatible behavior and CombatantHealth.");
            }
        }

        private static void ValidateScene(string scenePath, HollowPlatformKind expectedPlatformKind, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M5 game scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            var combat = Object.FindAnyObjectByType<RoomCombatController>();
            if (combat == null)
            {
                failures.Add($"{scenePath} is missing RoomCombatController.");
            }
            else
            {
                if (combat.EnemyCatalog == null)
                {
                    failures.Add($"{scenePath} RoomCombatController has no enemy catalog.");
                }

                if (combat.DifficultyTier == null)
                {
                    failures.Add($"{scenePath} RoomCombatController has no difficulty tier.");
                }
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null || shellCanvas.GetComponent<CombatHudController>() == null)
            {
                failures.Add($"{scenePath} PlatformShellCanvas must include CombatHudController.");
            }

            var presentationRoot = Object.FindAnyObjectByType<PlatformPresentationRoot>();
            if (presentationRoot == null || Mathf.Abs(presentationRoot.WorldScale - PresentationScalePolicy.WorldScaleFor(expectedPlatformKind)) > 0.0001f)
            {
                failures.Add($"{scenePath} has invalid presentation scaling.");
            }

            var session = Object.FindAnyObjectByType<GameSessionController>();
            if (session == null || session.SampleRoomRuntimeJson == null)
            {
                failures.Add($"{scenePath} is missing sample room JSON wiring.");
            }
        }
    }
}
