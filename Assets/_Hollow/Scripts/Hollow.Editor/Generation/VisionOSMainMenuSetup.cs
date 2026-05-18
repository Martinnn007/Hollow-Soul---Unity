using System;
using System.IO;
using System.Linq;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Platform;
using Hollow.UI.MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hollow.Editor.Generation
{
    public static class VisionOSMainMenuSetup
    {
        public const string ScenePath = "Assets/_Hollow/Scenes/MainMenu_VisionOS.unity";
        public const string PrefabPath = "Assets/_Hollow/Prefabs/UI/MainMenuRoot_VisionOS.prefab";
        private const string AppRootPrefabPath = "Assets/_Hollow/Prefabs/Core/AppRoot.prefab";
        private const string CameraRigPrefabPath = "Assets/_Hollow/Prefabs/Cameras/VisionOSBoundedRig.prefab";
        private const string BootScenePath = "Assets/_Hollow/Scenes/Boot.unity";
        private const string ArenaScenePath = "Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity";

        [MenuItem("Hollow/Generation/Configure visionOS Main Menu")]
        public static void ConfigureProject()
        {
            EnsurePrefab();
            EnsureScene();
            EnsureArenaVolumeCamera();
            EnsureBootRoutesVisionOSMenu();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Configured Hollow visionOS guided main menu.");
        }

        public static GameObject EnsurePrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/_Hollow/Prefabs/UI");
            var root = new GameObject("MainMenuRoot_VisionOS", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");
            root.AddComponent<MainMenuPlatformPresenter>().Apply(HollowPlatformKind.VisionOSBoundedTabletop);
            ConfigureCanvas(root.GetComponent<Canvas>());
            root.AddComponent<VisionOSMainMenuScreen>();
            var controller = root.AddComponent<MainMenuController>();
            ConfigureController(controller);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        public static void EnsureScene()
        {
            VisionOSVolumeCameraSetup.EnsureConfigurations();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            InstantiatePrefab(AppRootPrefabPath);
            InstantiatePrefab(CameraRigPrefabPath);
            var menu = InstantiatePrefab(PrefabPath);
            var controller = menu.GetComponent<MainMenuController>();
            ConfigureController(controller);
            CreateEventSystem();
            CreateDirectionalLight();
            VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(
                HollowPlatformKind.VisionOSBoundedTabletop,
                VisionOSBoundedVolumeFraming.MenuCentered);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        public static void EnsureBootRoutesVisionOSMenu()
        {
            if (!File.Exists(BootScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var boot = UnityEngine.Object.FindFirstObjectByType<BootSceneController>();
            if (boot == null)
            {
                throw new MissingComponentException($"{BootScenePath} is missing BootSceneController.");
            }

            boot.ConfigureStartup(loadOnStart: true, AppShellRoute.MainMenu, preferVisionOSRoute: true, AppShellRoute.MainMenuVisionOS);
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void EnsureArenaVolumeCamera()
        {
            if (!File.Exists(ArenaScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(
                HollowPlatformKind.VisionOSBoundedTabletop,
                VisionOSBoundedVolumeFraming.LevelBottomAnchored);
            EditorSceneManager.SaveScene(scene);
        }

        public static void EnsureBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == ScenePath))
            {
                foreach (var scene in scenes)
                {
                    if (scene.path == ScenePath)
                    {
                        scene.enabled = true;
                    }
                }

                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            var insertIndex = scenes.FindIndex(scene => scene.path == "Assets/_Hollow/Scenes/MainMenu.unity");
            scenes.Insert(insertIndex >= 0 ? insertIndex + 1 : scenes.Count, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureController(MainMenuController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.ConfigureDefaults(HollowPlatformKind.VisionOSBoundedTabletop, AppShellRoute.MainMenuVisionOS);
            controller.ConfigureChallengeCatalog(AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(Milestone47AssetGenerator.ChallengeCatalogPath));
            controller.ConfigureCharacterCatalog(AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath));
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            if (SortingLayer.layers.Any(layer => layer.name == "PolySpatialUI"))
            {
                canvas.sortingLayerName = "PolySpatialUI";
            }

            EditorUtility.SetDirty(canvas);
        }

        private static GameObject InstantiatePrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new FileNotFoundException($"Missing prefab at {path}");
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null)
            {
                eventSystem.AddComponent(inputSystemModule);
            }
            else
            {
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
