using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone1Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Persistence/Hollow.Persistence.asmdef",
            "Assets/_Hollow/Scripts/Hollow.UI/Hollow.UI.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Persistence/JsonProfileStore.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/MainMenu/MainMenuViewModel.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/MainMenu/MainMenuController.cs",
            "Assets/_Hollow/Prefabs/Core/AppRoot.prefab",
            "Assets/_Hollow/Prefabs/UI/MainMenuRoot.prefab",
            "Assets/_Hollow/Prefabs/UI/ProfileSlotCard.prefab",
            "Assets/_Hollow/Prefabs/Cameras/WindowsCameraRig.prefab",
            "Assets/_Hollow/Prefabs/Cameras/VisionOSBoundedRig.prefab",
            "Assets/_Hollow/Prefabs/Cameras/VisionOSImmersiveRig.prefab",
            "Assets/_Hollow/Scenes/Boot.unity",
            "Assets/_Hollow/Scenes/MainMenu.unity",
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        private static readonly string[] RequiredBuildScenes =
        {
            "Assets/_Hollow/Scenes/Boot.unity",
            "Assets/_Hollow/Scenes/MainMenu.unity",
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 1 Validation")]
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
                    failures.Add($"Missing M1 file: {file}");
                }
            }

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            foreach (var scenePath in RequiredBuildScenes)
            {
                if (!enabledScenes.Contains(scenePath))
                {
                    failures.Add($"M1 scene is not enabled in build settings: {scenePath}");
                }
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 1 validation passed.");
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
    }
}
