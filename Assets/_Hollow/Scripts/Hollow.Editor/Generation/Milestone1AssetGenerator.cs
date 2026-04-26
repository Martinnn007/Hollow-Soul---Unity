using System;
using System.IO;
using Hollow.Core.App;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using Hollow.UI.MainMenu;
using Hollow.UI.Shell;
using Hollow.World;
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
        private const string SampleRoomRuntimePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [MenuItem("Hollow/Generation/Generate Milestone 1 Assets")]
        public static void Generate()
        {
            EnsureDirectories();
            GeneratePrefabs();
            GenerateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 1/2 scenes and prefabs.");
        }

        private static void EnsureDirectories()
        {
            foreach (var path in new[]
            {
                $"{Root}/Prefabs/Core",
                $"{Root}/Prefabs/UI",
                $"{Root}/Prefabs/Cameras",
                $"{Root}/Prefabs/Player",
                $"{Root}/Prefabs/Rooms",
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
            SavePrefab(CreateRoomRuntimeRoot(), $"{Root}/Prefabs/Rooms/RoomRuntimeRoot.prefab");
            SavePrefab(CreatePlayerCharacter(), $"{Root}/Prefabs/Player/PlayerCharacter.prefab");
            SavePrefab(CreateCameraRig("WindowsCameraRig", HollowPlatformKind.WindowsStandard3D, new Vector3(0f, 7f, -10f), new Vector3(35f, 0f, 0f)), $"{Root}/Prefabs/Cameras/WindowsCameraRig.prefab");
            SavePrefab(CreateCameraRig("VisionOSBoundedRig", HollowPlatformKind.VisionOSBoundedTabletop, new Vector3(0f, 1.35f, -2.4f), new Vector3(24f, 0f, 0f)), $"{Root}/Prefabs/Cameras/VisionOSBoundedRig.prefab");
            SavePrefab(CreateCameraRig("VisionOSImmersiveRig", HollowPlatformKind.VisionOSImmersive, new Vector3(0f, 6f, -9f), new Vector3(32f, 0f, 0f)), $"{Root}/Prefabs/Cameras/VisionOSImmersiveRig.prefab");
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

        private static GameObject CreateRoomRuntimeRoot()
        {
            var root = new GameObject("RoomRuntimeRoot");
            root.AddComponent<RoomRuntimeRoot>().ConfigureDefault();
            root.AddComponent<PlaceholderRoomAuthoring>();

            CreateCubeChild(root.transform, "Floor_13x7m", new Vector3(0f, -0.05f, 0f), new Vector3(13f, 0.1f, 7f));
            CreateCubeChild(root.transform, "Wall_North", new Vector3(0f, 0.5f, -3.56f), new Vector3(13.24f, 1f, 0.12f));
            CreateCubeChild(root.transform, "Wall_South", new Vector3(0f, 0.5f, 3.56f), new Vector3(13.24f, 1f, 0.12f));
            CreateCubeChild(root.transform, "Wall_East", new Vector3(6.56f, 0.5f, 0f), new Vector3(0.12f, 1f, 7f));
            CreateCubeChild(root.transform, "Wall_West", new Vector3(-6.56f, 0.5f, 0f), new Vector3(0.12f, 1f, 7f));
            CreateCubeChild(root.transform, "OriginMarker_0_0", new Vector3(0f, 0.01f, 0f), new Vector3(0.25f, 0.02f, 0.25f));

            var spawn = new GameObject("PlayerSpawn_Center", typeof(PlayerSpawnPoint));
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.localPosition = Vector3.zero;
            return root;
        }

        private static GameObject CreatePlayerCharacter()
        {
            var root = new GameObject("PlayerCharacter", typeof(CapsuleCollider));
            root.AddComponent<PlaceholderPlayerController>().ConfigureDefault();

            var collider = root.GetComponent<CapsuleCollider>();
            collider.radius = PlaceholderPlayerController.DefaultRadiusMeters;
            collider.height = PlaceholderPlayerController.DefaultHeightMeters;
            collider.center = new Vector3(0f, PlaceholderPlayerController.DefaultHeightMeters * 0.5f, 0f);

            var visibleCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visibleCapsule.name = "PlayerHeight_1_78m";
            visibleCapsule.transform.SetParent(root.transform, false);
            visibleCapsule.transform.localPosition = collider.center;
            visibleCapsule.transform.localScale = new Vector3(
                PlaceholderPlayerController.DefaultRadiusMeters * 2f,
                PlaceholderPlayerController.DefaultHeightMeters * 0.5f,
                PlaceholderPlayerController.DefaultRadiusMeters * 2f);

            var childCollider = visibleCapsule.GetComponent<Collider>();
            if (childCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(childCollider);
            }

            return root;
        }

        private static GameObject CreateCameraRig(string name, HollowPlatformKind platformKind, Vector3 cameraPosition, Vector3 cameraRotation)
        {
            var root = new GameObject(name);
            root.AddComponent<CameraRigMetadata>().Configure(platformKind);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = cameraPosition;
            cameraObject.transform.localRotation = Quaternion.Euler(cameraRotation);
            cameraObject.tag = "MainCamera";
            return root;
        }

        private static GameObject CreateCubeChild(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            return cube;
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
            CreateGameSessionRoot(platformKind);
            CreateShellCanvas(platformKind);
            SaveScene(scene, $"{Root}/Scenes/{sceneName}.unity");
        }

        private static void CreateGameSessionRoot(HollowPlatformKind platformKind)
        {
            var root = new GameObject("GameSessionRoot");
            var sampleRoomRuntimeJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SampleRoomRuntimePath);
            root.AddComponent<GameSessionController>().Configure(platformKind, sampleRoomRuntimeJson);

            var presentationObject = new GameObject("WorldPresentationRoot");
            presentationObject.transform.SetParent(root.transform, false);
            presentationObject.AddComponent<PlatformPresentationRoot>().Configure(platformKind);

            var room = InstantiatePrefab($"{Root}/Prefabs/Rooms/RoomRuntimeRoot.prefab");
            room.transform.SetParent(presentationObject.transform, false);
            room.transform.localPosition = Vector3.zero;

            var player = InstantiatePrefab($"{Root}/Prefabs/Player/PlayerCharacter.prefab");
            player.transform.SetParent(presentationObject.transform, false);
            player.transform.localPosition = Vector3.zero;
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
