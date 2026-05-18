using System;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rewards;
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
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";
        private const string DifficultyTierPath = "Assets/_Hollow/Data/Enemies/Difficulty_DeveloperSample.asset";
        private const string RewardPickupPrefabPath = "Assets/_Hollow/Prefabs/Rewards/RoomRewardPickup.prefab";
        private const string HubReturnPortalPrefabPath = "Assets/_Hollow/Prefabs/Rewards/HubReturnPortal.prefab";

        [MenuItem("Hollow/Generation/Generate Milestone 1 Assets")]
        public static void Generate()
        {
            EnsureDirectories();
            GenerateDataAssets();
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
                $"{Root}/Prefabs/Combat",
                $"{Root}/Prefabs/Rewards",
                $"{Root}/Data/Enemies",
                $"{Root}/Scenes"
            })
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void GenerateDataAssets()
        {
            var normal = SaveAsset(CreateEnemyDefinition(
                "spawnEnemyNormal",
                "Normal Chaser",
                EnemyArchetypeId.Normal,
                EnemyMovementMode.Grounded,
                3,
                1.5f,
                1,
                new Color(0.85f, 0.16f, 0.14f, 1f)), $"{Root}/Data/Enemies/Enemy_Normal.asset");
            var flying = SaveAsset(CreateEnemyDefinition(
                "spawnEnemyFlying",
                "Flying Chaser",
                EnemyArchetypeId.Flying,
                EnemyMovementMode.Flying,
                3,
                1.8f,
                1,
                new Color(0.25f, 0.65f, 1f, 1f)), $"{Root}/Data/Enemies/Enemy_Flying.asset");
            var fast = SaveAsset(CreateEnemyDefinition(
                "spawnEnemyFast",
                "Fast Chaser",
                EnemyArchetypeId.Fast,
                EnemyMovementMode.Grounded,
                2,
                2.4f,
                1,
                new Color(1f, 0.66f, 0.18f, 1f)), $"{Root}/Data/Enemies/Enemy_Fast.asset");
            var heavy = SaveAsset(CreateEnemyDefinition(
                "spawnEnemyHeavy",
                "Heavy Chaser",
                EnemyArchetypeId.Heavy,
                EnemyMovementMode.Grounded,
                6,
                0.9f,
                2,
                new Color(0.62f, 0.22f, 0.82f, 1f)), $"{Root}/Data/Enemies/Enemy_Heavy.asset");

            var catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            catalog.Configure(new[] { normal, flying, fast, heavy }, normal);
            SaveAsset(catalog, EnemyCatalogPath);

            var difficulty = ScriptableObject.CreateInstance<DifficultyTierDefinition>();
            difficulty.Configure("Developer Sample", 1f, 1f, 1f);
            SaveAsset(difficulty, DifficultyTierPath);
        }

        private static EnemyDefinition CreateEnemyDefinition(
            string spawnKind,
            string displayName,
            EnemyArchetypeId archetypeId,
            EnemyMovementMode movementMode,
            int health,
            float speed,
            int contactDamage,
            Color color)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(spawnKind, displayName, archetypeId, movementMode, health, speed, contactDamage, 1f, 0.32f, color);
            return definition;
        }

        private static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void GeneratePrefabs()
        {
            SavePrefab(CreateAppRoot(), $"{Root}/Prefabs/Core/AppRoot.prefab");
            SavePrefab(CreateMainMenuRoot(), $"{Root}/Prefabs/UI/MainMenuRoot.prefab");
            SavePrefab(CreateVisionOSMainMenuRoot(), $"{Root}/Prefabs/UI/MainMenuRoot_VisionOS.prefab");
            SavePrefab(CreateProfileSlotCard(), $"{Root}/Prefabs/UI/ProfileSlotCard.prefab");
            SavePrefab(CreateRoomRuntimeRoot(), $"{Root}/Prefabs/Rooms/RoomRuntimeRoot.prefab");
            SavePrefab(CreatePlayerCharacter(), $"{Root}/Prefabs/Player/PlayerCharacter.prefab");
            SavePrefab(CreateEnemyBase(), $"{Root}/Prefabs/Combat/EnemyBase.prefab");
            SavePrefab(CreateProjectileBase(), $"{Root}/Prefabs/Combat/ProjectileBase.prefab");
            SavePrefab(CreateRewardPickup(), RewardPickupPrefabPath);
            SavePrefab(CreateHubReturnPortal(), HubReturnPortalPrefabPath);
            SavePrefab(CreateCameraRig("WindowsCameraRig", HollowPlatformKind.WindowsStandard3D, Milestone10AssetGenerator.ArpgCameraLocalPosition, Milestone10AssetGenerator.ArpgCameraLocalEulerAngles), $"{Root}/Prefabs/Cameras/WindowsCameraRig.prefab");
            SavePrefab(CreateCameraRig("VisionOSBoundedRig", HollowPlatformKind.VisionOSBoundedTabletop, new Vector3(0f, 1.35f, -2.4f), new Vector3(24f, 0f, 0f)), $"{Root}/Prefabs/Cameras/VisionOSBoundedRig.prefab");
            SavePrefab(CreateCameraRig("VisionOSImmersiveRig", HollowPlatformKind.VisionOSImmersive, Milestone10AssetGenerator.ArpgCameraLocalPosition, Milestone10AssetGenerator.ArpgCameraLocalEulerAngles), $"{Root}/Prefabs/Cameras/VisionOSImmersiveRig.prefab");
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

        private static GameObject CreateVisionOSMainMenuRoot()
        {
            var root = new GameObject("MainMenuRoot_VisionOS", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = LayerMask.NameToLayer("UI");
            root.AddComponent<MainMenuPlatformPresenter>().Apply(HollowPlatformKind.VisionOSBoundedTabletop);
            var canvas = root.GetComponent<Canvas>();
            canvas.sortingOrder = 20;
            if (SortingLayer.layers.Any(layer => layer.name == "PolySpatialUI"))
            {
                canvas.sortingLayerName = "PolySpatialUI";
            }

            root.AddComponent<VisionOSMainMenuScreen>();
            root.AddComponent<MainMenuController>().ConfigureDefaults(HollowPlatformKind.VisionOSBoundedTabletop, AppShellRoute.MainMenuVisionOS);
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
            root.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            root.AddComponent<PlayerMovementController>();
            root.AddComponent<PlayerWeaponController>();

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

            MaterialResolver.ApplyTo(visibleCapsule, MaterialRole.PlayerBody);
            return root;
        }

        private static GameObject CreateEnemyBase()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "EnemyBase";
            root.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
            root.AddComponent<CombatantHealth>().Configure(ChaserEnemyController.DefaultHealth);
            root.AddComponent<ChaserEnemyController>();
            MaterialResolver.ApplyTo(root, MaterialRole.EnemyNormal);
            return root;
        }

        private static GameObject CreateProjectileBase()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "ProjectileBase";
            root.transform.localScale = Vector3.one * 0.22f;
            root.AddComponent<ProjectileController>();
            var collider = root.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            MaterialResolver.ApplyTo(root, MaterialRole.Projectile);
            return root;
        }

        private static GameObject CreateRewardPickup()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "RoomRewardPickup";
            root.transform.localScale = Vector3.one * 0.35f;
            root.AddComponent<RoomRewardPickup>();
            var collider = root.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            MaterialResolver.ApplyTo(root, MaterialRole.RewardPickup);
            return root;
        }

        private static GameObject CreateHubReturnPortal()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "HubReturnPortal";
            root.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            root.AddComponent<HubReturnPortal>();
            var collider = root.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            MaterialResolver.ApplyTo(root, MaterialRole.HubReturnPortal);
            return root;
        }

        private static GameObject CreateCameraRig(string name, HollowPlatformKind platformKind, Vector3 cameraPosition, Vector3 cameraRotation)
        {
            var root = new GameObject(name);
            root.AddComponent<CameraRigMetadata>().Configure(platformKind);
            root.AddComponent<GameplayCameraFollowController>();
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
            VisionOSVolumeCameraSetup.EnsureConfigurations();
            GenerateBootScene();
            GenerateMainMenuScene();
            GenerateVisionOSMainMenuScene();
            GeneratePlatformScene("Game_Windows", HollowPlatformKind.WindowsStandard3D, "WindowsCameraRig");
            GeneratePlatformScene("Game_VisionOS_Bounded", HollowPlatformKind.VisionOSBoundedTabletop, "VisionOSBoundedRig");
            GeneratePlatformScene("Game_VisionOS_Immersive", HollowPlatformKind.VisionOSImmersive, "VisionOSImmersiveRig");

            EditorBuildSettings.scenes = new[]
            {
                BuildScene($"{Root}/Scenes/Boot.unity"),
                BuildScene($"{Root}/Scenes/MainMenu.unity"),
                BuildScene($"{Root}/Scenes/MainMenu_VisionOS.unity"),
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
            VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(HollowPlatformKind.VisionOSImmersive);
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
            VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(HollowPlatformKind.VisionOSImmersive);
            SaveScene(scene, $"{Root}/Scenes/MainMenu.unity");
        }

        private static void GenerateVisionOSMainMenuScene()
        {
            var scene = NewScene("MainMenu_VisionOS");
            InstantiatePrefab($"{Root}/Prefabs/Core/AppRoot.prefab");
            InstantiatePrefab($"{Root}/Prefabs/Cameras/VisionOSBoundedRig.prefab");
            var menu = InstantiatePrefab($"{Root}/Prefabs/UI/MainMenuRoot_VisionOS.prefab");
            var controller = menu.GetComponent<MainMenuController>();
            controller.ConfigureDefaults(HollowPlatformKind.VisionOSBoundedTabletop, AppShellRoute.MainMenuVisionOS);
            controller.ConfigureChallengeCatalog(AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(Milestone47AssetGenerator.ChallengeCatalogPath));
            controller.ConfigureCharacterCatalog(AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath));
            CreateEventSystem();
            CreateDirectionalLight();
            VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(
                HollowPlatformKind.VisionOSBoundedTabletop,
                VisionOSBoundedVolumeFraming.MenuCentered);
            SaveScene(scene, $"{Root}/Scenes/MainMenu_VisionOS.unity");
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
            if (platformKind != HollowPlatformKind.WindowsStandard3D)
            {
                VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(
                    platformKind,
                    platformKind == HollowPlatformKind.VisionOSBoundedTabletop
                        ? VisionOSBoundedVolumeFraming.LevelBottomAnchored
                        : VisionOSBoundedVolumeFraming.MenuCentered);
            }
            SaveScene(scene, $"{Root}/Scenes/{sceneName}.unity");
        }

        private static void CreateGameSessionRoot(HollowPlatformKind platformKind)
        {
            var root = new GameObject("GameSessionRoot");
            var sampleRoomRuntimeJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SampleRoomRuntimePath);
            root.AddComponent<GameSessionController>().Configure(platformKind, sampleRoomRuntimeJson);
            if (platformKind == HollowPlatformKind.VisionOSBoundedTabletop)
            {
                root.AddComponent<VisionOSGameplayInputDiagnostics>();
            }

            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Combat/EnemyBase.prefab");
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Combat/ProjectileBase.prefab");
            var enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            var difficultyTier = AssetDatabase.LoadAssetAtPath<DifficultyTierDefinition>(DifficultyTierPath);
            var rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPickupPrefabPath);
            var hubReturnPortalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HubReturnPortalPrefabPath);
            root.AddComponent<RoomCombatController>().Configure(enemyPrefab, projectilePrefab, enemyCatalog, difficultyTier);
            root.AddComponent<BranchSessionController>().Configure(rewardPrefab, hubReturnPortalPrefab);

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
            canvasObject.AddComponent<CombatHudController>();
            canvasObject.AddComponent<BranchMiniMapController>();
            canvasObject.AddComponent<PlayerBuildHudController>();
            canvasObject.AddComponent<PickupRevealController>();
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

    }
}
