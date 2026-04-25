using System;
using System.IO;
using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.UI.MainMenu;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hollow.Editor.Generation
{
    public static class Milestone1AssetGenerator
    {
        private const string Root = "Assets/_Hollow";

        [MenuItem("Hollow/Generation/Generate Milestone 1 Assets")]
        public static void Generate()
        {
            EnsureDirectories();
            GeneratePrefabs();
            GenerateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 1 scenes and prefabs.");
        }

        private static void EnsureDirectories()
        {
            foreach (var path in new[]
            {
                $"{Root}/Prefabs/Core",
                $"{Root}/Prefabs/UI",
                $"{Root}/Prefabs/Cameras",
                $"{Root}/Scenes"
            })
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void GeneratePrefabs()
        {
            SavePrefab(CreateAppRoot(), $"{Root}/Prefabs/Core/AppRoot.prefab");
            SavePrefab(CreateMainMenuRoot(), $"{Root}/Prefabs/UI/MainMenuRoot.prefab");
            SavePrefab(CreateProfileSlotCard(), $"{Root}/Prefabs/UI/ProfileSlotCard.prefab");
            SavePrefab(CreateCameraRig("WindowsCameraRig", new Vector3(0f, 1.6f, -6f)), $"{Root}/Prefabs/Cameras/WindowsCameraRig.prefab");
            SavePrefab(CreateCameraRig("VisionOSBoundedRig", new Vector3(0f, 1.4f, -4f)), $"{Root}/Prefabs/Cameras/VisionOSBoundedRig.prefab");
            SavePrefab(CreateCameraRig("VisionOSImmersiveRig", new Vector3(0f, 1.7f, -5f)), $"{Root}/Prefabs/Cameras/VisionOSImmersiveRig.prefab");
        }

        private static GameObject CreateAppRoot()
        {
            var root = new GameObject("AppRoot");
            root.AddComponent<HollowBootstrap>();
            root.AddComponent<ProfileSessionHost>();
            return root;
        }

        private static GameObject CreateMainMenuRoot()
        {
            var root = new GameObject("MainMenuRoot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.AddComponent<MainMenuPlatformPresenter>();
            root.AddComponent<MainMenuScreen>();
            root.AddComponent<MainMenuController>();
            return root;
        }

        private static GameObject CreateProfileSlotCard()
        {
            var root = new GameObject("ProfileSlotCard", typeof(RectTransform), typeof(Image), typeof(Button));
            root.AddComponent<ProfileSlotCardView>();
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(200f, 110f);
            root.GetComponent<Image>().color = new Color(0.13f, 0.20f, 0.36f);
            return root;
        }

        private static GameObject CreateCameraRig(string name, Vector3 cameraPosition)
        {
            var root = new GameObject(name);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = cameraPosition;
            cameraObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            cameraObject.tag = "MainCamera";
            return root;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void GenerateScenes()
        {
            GenerateBootScene();
            GenerateMainMenuScene();
            GeneratePlatformScene("Game_Windows", HollowPlatformKind.WindowsStandard3D, "WindowsCameraRig");
            GeneratePlatformScene("Game_VisionOS_Bounded", HollowPlatformKind.VisionOSBoundedTabletop, "VisionOSBoundedRig");
            GeneratePlatformScene("Game_VisionOS_Immersive", HollowPlatformKind.VisionOSImmersive, "VisionOSImmersiveRig");

            EditorBuildSettings.scenes = new[]
            {
                BuildScene($"{Root}/Scenes/Boot.unity"),
                BuildScene($"{Root}/Scenes/MainMenu.unity"),
                BuildScene($"{Root}/Scenes/Game_Windows.unity"),
                BuildScene($"{Root}/Scenes/Game_VisionOS_Bounded.unity"),
                BuildScene($"{Root}/Scenes/Game_VisionOS_Immersive.unity")
            };
        }

        private static EditorBuildSettingsScene BuildScene(string path)
        {
            return new EditorBuildSettingsScene(path, true);
        }

        private static void GenerateBootScene()
        {
            var scene = NewScene("Boot");
            InstantiatePrefab($"{Root}/Prefabs/Core/AppRoot.prefab");
            var boot = new GameObject("BootSceneController");
            boot.AddComponent<BootSceneController>();
            SaveScene(scene, $"{Root}/Scenes/Boot.unity");
        }

        private static void GenerateMainMenuScene()
        {
            var scene = NewScene("MainMenu");
            InstantiatePrefab($"{Root}/Prefabs/Core/AppRoot.prefab");
            InstantiatePrefab($"{Root}/Prefabs/Cameras/WindowsCameraRig.prefab");
            InstantiatePrefab($"{Root}/Prefabs/UI/MainMenuRoot.prefab");
            CreateEventSystem();
            CreateDirectionalLight();
            SaveScene(scene, $"{Root}/Scenes/MainMenu.unity");
        }

        private static void GeneratePlatformScene(string sceneName, HollowPlatformKind platformKind, string cameraRigName)
        {
            var scene = NewScene(sceneName);
            InstantiatePrefab($"{Root}/Prefabs/Core/AppRoot.prefab");
            InstantiatePrefab($"{Root}/Prefabs/Cameras/{cameraRigName}.prefab");
            CreateEventSystem();
            CreateDirectionalLight();
            CreateShellCanvas(platformKind);
            SaveScene(scene, $"{Root}/Scenes/{sceneName}.unity");
        }

        private static Scene NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
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

        private static void CreateShellCanvas(HollowPlatformKind platformKind)
        {
            var canvasObject = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var controller = canvasObject.AddComponent<PlatformShellController>();
            controller.Configure(platformKind);
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
