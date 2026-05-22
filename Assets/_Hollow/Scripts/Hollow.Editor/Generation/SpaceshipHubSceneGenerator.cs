#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
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
    public static class SpaceshipHubSceneGenerator
    {
        public const string ShipUpgradeCatalogPath = "Assets/_Hollow/Data/Spaceship/ShipUpgradeCatalog.asset";
        public const string SpaceshipBranchDefinitionPath = "Assets/_Hollow/Data/Spaceship/SpaceshipBranchDefinition.asset";
        public const string SpaceshipWindowsScenePath = "Assets/_Hollow/Scenes/Spaceship_Windows.unity";
        public const string SpaceshipVisionOSBoundedScenePath = "Assets/_Hollow/Scenes/Spaceship_VisionOS_Bounded.unity";
        public const string SpaceshipVisionOSImmersiveScenePath = "Assets/_Hollow/Scenes/Spaceship_VisionOS_Immersive.unity";

        private const string ChallengeCatalogPath = "Assets/_Hollow/Data/Challenges/M47/ChallengeCatalog_M47.asset";
        private const string CharacterCatalogPath = "Assets/_Hollow/Data/Characters/M29/CharacterCatalog_M29.asset";
        private const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        private const string EnemyPrefabPath = "Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab";
        private const string ProjectilePrefabPath = "Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";
        private const string DifficultyTierPath = "Assets/_Hollow/Data/Enemies/Difficulty_DeveloperSample.asset";
        private const string RewardPickupPrefabPath = "Assets/_Hollow/Prefabs/Rewards/RoomRewardPickup.prefab";
        private const string HubReturnPortalPrefabPath = "Assets/_Hollow/Prefabs/Rewards/HubReturnPortal.prefab";
        private const string SpaceshipRoomRoot = "Assets/_Hollow/Data/Rooms/Spaceship";

        [MenuItem("Hollow/Generation/Generate Spaceship Meta Hub")]
        public static void Generate()
        {
            GenerateScenesForTests();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow branch-style spaceship hub scenes and upgrade catalog.");
        }

        public static void GenerateScenesForTests()
        {
            var upgradeCatalog = EnsureShipUpgradeCatalog();
            var shipDefinition = EnsureSpaceshipBranchDefinition();
            GenerateScene(SpaceshipWindowsScenePath, HollowPlatformKind.WindowsStandard3D, upgradeCatalog, shipDefinition);
            GenerateScene(SpaceshipVisionOSBoundedScenePath, HollowPlatformKind.VisionOSBoundedTabletop, upgradeCatalog, shipDefinition);
            GenerateScene(SpaceshipVisionOSImmersiveScenePath, HollowPlatformKind.VisionOSImmersive, upgradeCatalog, shipDefinition);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
        }

        public static ShipUpgradeCatalogDefinition EnsureShipUpgradeCatalog()
        {
            Directory.CreateDirectory("Assets/_Hollow/Data/Spaceship");
            var catalog = AssetDatabase.LoadAssetAtPath<ShipUpgradeCatalogDefinition>(ShipUpgradeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShipUpgradeCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, ShipUpgradeCatalogPath);
            }

            catalog.Configure(
                "ship_upgrade_catalog",
                ShipUpgradeCatalogDefinition.CreateRuntimeDefault().Upgrades);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        public static SpaceshipBranchDefinition EnsureSpaceshipBranchDefinition()
        {
            Directory.CreateDirectory("Assets/_Hollow/Data/Spaceship");
            var definition = AssetDatabase.LoadAssetAtPath<SpaceshipBranchDefinition>(SpaceshipBranchDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SpaceshipBranchDefinition>();
                AssetDatabase.CreateAsset(definition, SpaceshipBranchDefinitionPath);
            }

            var rooms = new[]
            {
                LoadRoomTextAsset(SpaceshipBranchDefinition.ArrivalsRoomId),
                LoadRoomTextAsset(SpaceshipBranchDefinition.MainHallRoomId),
                LoadRoomTextAsset(SpaceshipBranchDefinition.DeparturesRoomId),
                LoadRoomTextAsset(SpaceshipBranchDefinition.MissionCenterRoomId),
                LoadRoomTextAsset(SpaceshipBranchDefinition.TechnologyLabRoomId)
            };
            definition.Configure(rooms);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void GenerateScene(
            string path,
            HollowPlatformKind platformKind,
            ShipUpgradeCatalogDefinition upgradeCatalog,
            SpaceshipBranchDefinition shipDefinition)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = Path.GetFileNameWithoutExtension(path);

            InstantiatePrefab("Assets/_Hollow/Prefabs/Core/AppRoot.prefab");
            InstantiatePrefab(CameraRigPathFor(platformKind));
            CreateEventSystem();
            CreateDirectionalLight();
            CreateGameSessionRoot(platformKind, upgradeCatalog, shipDefinition);
            CreateShellCanvas(platformKind);

            if (platformKind != HollowPlatformKind.WindowsStandard3D)
            {
                VisionOSVolumeCameraSetup.EnsureOpenSceneVolumeCamera(
                    platformKind,
                    platformKind == HollowPlatformKind.VisionOSBoundedTabletop
                        ? VisionOSBoundedVolumeFraming.LevelBottomAnchored
                        : VisionOSBoundedVolumeFraming.MenuCentered);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/_Hollow/Scenes");
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateGameSessionRoot(
            HollowPlatformKind platformKind,
            ShipUpgradeCatalogDefinition upgradeCatalog,
            SpaceshipBranchDefinition shipDefinition)
        {
            var root = new GameObject("GameSessionRoot");
            var arrivalsRoom = LoadRoomTextAsset(SpaceshipBranchDefinition.ArrivalsRoomId);
            root.AddComponent<GameSessionController>().Configure(platformKind, arrivalsRoom, RuntimeSessionMode.SpaceshipHub);
            if (platformKind == HollowPlatformKind.VisionOSBoundedTabletop)
            {
                root.AddComponent<VisionOSGameplayInputDiagnostics>();
            }

            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            var difficultyTier = AssetDatabase.LoadAssetAtPath<DifficultyTierDefinition>(DifficultyTierPath);
            var rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPickupPrefabPath);
            var hubReturnPortalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HubReturnPortalPrefabPath);
            root.AddComponent<RoomCombatController>().Configure(enemyPrefab, projectilePrefab, enemyCatalog, difficultyTier);
            var branch = root.AddComponent<BranchSessionController>();
            branch.Configure(rewardPrefab, hubReturnPortalPrefab);
            branch.ConfigureSpaceshipBranch(shipDefinition);

            var serialized = new SerializedObject(branch);
            serialized.FindProperty("challengeCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ChallengeCatalogDefinition>(ChallengeCatalogPath);
            serialized.FindProperty("characterCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(CharacterCatalogPath);
            serialized.FindProperty("weaponCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            serialized.FindProperty("armorCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ArmorCatalogDefinition>(Milestone30AssetGenerator.ArmorCatalogPath);
            serialized.FindProperty("shieldCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ShieldCatalogDefinition>(Milestone69AssetGenerator.ShieldCatalogPath);
            serialized.FindProperty("shipUpgradeCatalog").objectReferenceValue = upgradeCatalog;
            serialized.FindProperty("spaceshipBranchDefinition").objectReferenceValue = shipDefinition;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var presentationObject = new GameObject("WorldPresentationRoot");
            presentationObject.transform.SetParent(root.transform, false);
            presentationObject.AddComponent<PlatformPresentationRoot>().Configure(platformKind);

            var room = InstantiatePrefab("Assets/_Hollow/Prefabs/Rooms/RoomRuntimeRoot.prefab");
            room.transform.SetParent(presentationObject.transform, false);
            room.transform.localPosition = Vector3.zero;

            var player = InstantiatePrefab(PlayerPrefabPath);
            player.transform.SetParent(presentationObject.transform, false);
            player.transform.localPosition = Vector3.zero;
            player.name = "PlayerCharacter";
            if (player.GetComponent<PlaceholderPlayerController>() == null)
            {
                player.AddComponent<PlaceholderPlayerController>();
            }

            if (player.GetComponent<PlayerDefenseController>() == null)
            {
                player.AddComponent<PlayerDefenseController>();
            }
        }

        private static string CameraRigPathFor(HollowPlatformKind platformKind)
        {
            return platformKind switch
            {
                HollowPlatformKind.VisionOSBoundedTabletop => "Assets/_Hollow/Prefabs/Cameras/VisionOSBoundedRig.prefab",
                HollowPlatformKind.VisionOSImmersive => "Assets/_Hollow/Prefabs/Cameras/VisionOSImmersiveRig.prefab",
                _ => "Assets/_Hollow/Prefabs/Cameras/WindowsCameraRig.prefab"
            };
        }

        private static TextAsset LoadRoomTextAsset(string roomId)
        {
            var path = $"{SpaceshipRoomRoot}/{roomId}.hollowruntime.json";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"Missing spaceship room at {path}");
            }

            return asset;
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

        private static void CreateShellCanvas(HollowPlatformKind platformKind)
        {
            var canvasObject = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var shell = canvasObject.AddComponent<PlatformShellController>();
            shell.Configure(platformKind);
            canvasObject.AddComponent<CombatHudController>();
            canvasObject.AddComponent<BranchMiniMapController>();
            canvasObject.AddComponent<PlayerBuildHudController>();
            canvasObject.AddComponent<PickupRevealController>();
            canvasObject.AddComponent<PauseMenuController>();
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

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            EnsureScene(scenes, SpaceshipWindowsScenePath);
            EnsureScene(scenes, SpaceshipVisionOSBoundedScenePath);
            EnsureScene(scenes, SpaceshipVisionOSImmersiveScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            var index = scenes.FindIndex(scene => scene.path == path);
            if (index >= 0)
            {
                scenes[index] = new EditorBuildSettingsScene(path, enabled: true);
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, enabled: true));
        }
    }
}
#endif
