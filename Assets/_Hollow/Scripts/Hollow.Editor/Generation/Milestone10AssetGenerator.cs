using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Platform;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone10AssetGenerator
    {
        public const string PlatformPolishDirectory = "Assets/_Hollow/Data/Platform/Polish";
        public const string WindowsProfilePath = PlatformPolishDirectory + "/PlatformPolish_WindowsStandard3D.asset";
        public const string BoundedProfilePath = PlatformPolishDirectory + "/PlatformPolish_VisionOSBoundedTabletop.asset";
        public const string ImmersiveProfilePath = PlatformPolishDirectory + "/PlatformPolish_VisionOSImmersive.asset";
        public const string PlatformAddressableLabel = "hollow.platform";
        public static readonly Vector3 ArpgCameraLocalPosition = new(-6.5f, 8.25f, -6.5f);
        public static readonly Vector3 ArpgCameraLocalEulerAngles = new(42f, 45f, 0f);
        public const float ArpgCameraFieldOfView = 50f;

        [MenuItem("Hollow/Generation/Generate Milestone 10 Assets")]
        public static void Generate()
        {
            Milestone9AssetGenerator.Generate();
            Directory.CreateDirectory(PlatformPolishDirectory);

            var windows = CreateOrUpdateProfile(
                WindowsProfilePath,
                PlatformPresentationMode.WindowsStandard3D,
                1f,
                PresentationOrientationPolicy.DefaultWorldYawDegrees,
                ArpgCameraLocalPosition,
                ArpgCameraLocalEulerAngles,
                ArpgCameraFieldOfView,
                0.03f,
                95f,
                new Color(0.018f, 0.023f, 0.034f, 1f),
                new Color(0.64f, 0.70f, 0.82f, 1f),
                60,
                0,
                1f,
                false,
                0f,
                0f);
            var bounded = CreateOrUpdateProfile(
                BoundedProfilePath,
                PlatformPresentationMode.VisionOSBoundedTabletop,
                PresentationScalePolicy.VisionOSBoundedTabletopScale,
                PresentationOrientationPolicy.VisionOSGameplayWorldYawDegrees,
                new Vector3(0f, 1.35f, -2.4f),
                new Vector3(24f, 0f, 0f),
                48f,
                0.01f,
                35f,
                new Color(0.015f, 0.018f, 0.025f, 1f),
                new Color(0.58f, 0.64f, 0.74f, 1f),
                90,
                0,
                0.9f,
                false,
                0f,
                0f);
            var immersive = CreateOrUpdateProfile(
                ImmersiveProfilePath,
                PlatformPresentationMode.VisionOSImmersive,
                1f,
                PresentationOrientationPolicy.VisionOSGameplayWorldYawDegrees,
                ArpgCameraLocalPosition,
                ArpgCameraLocalEulerAngles,
                ArpgCameraFieldOfView,
                0.03f,
                70f,
                new Color(0.01f, 0.014f, 0.022f, 1f),
                new Color(0.52f, 0.58f, 0.70f, 1f),
                90,
                0,
                0.85f,
                true,
                0.82f,
                0.18f);

            ApplyProfileToCameraPrefab("Assets/_Hollow/Prefabs/Cameras/WindowsCameraRig.prefab", HollowPlatformKind.WindowsStandard3D, windows);
            ApplyProfileToCameraPrefab("Assets/_Hollow/Prefabs/Cameras/VisionOSBoundedRig.prefab", HollowPlatformKind.VisionOSBoundedTabletop, bounded);
            ApplyProfileToCameraPrefab("Assets/_Hollow/Prefabs/Cameras/VisionOSImmersiveRig.prefab", HollowPlatformKind.VisionOSImmersive, immersive);

            ApplyProfileToScene("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D, windows);
            ApplyProfileToScene("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop, bounded);
            ApplyProfileToScene("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive, immersive);
            ApplyProfileToScene("Assets/_Hollow/Scenes/MainMenu.unity", HollowPlatformKind.WindowsStandard3D, windows);
            VisionOSVolumeCameraSetup.ConfigureProject();
            VisionOSMainMenuSetup.ConfigureProject();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ConfigureAddressables(WindowsProfilePath, BoundedProfilePath, ImmersiveProfilePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 10 platform polish profiles, camera rig polish, and scene presentation settings.");
        }

        private static PlatformPolishProfileDefinition CreateOrUpdateProfile(
            string path,
            PlatformPresentationMode mode,
            float worldScale,
            float worldYawDegrees,
            Vector3 cameraPosition,
            Vector3 cameraEuler,
            float fieldOfView,
            float nearClip,
            float farClip,
            Color backgroundColor,
            Color ambientColor,
            int targetFrameRate,
            int vSyncCount,
            float renderScale,
            bool useComfortVignette,
            float vignetteRadius,
            float vignetteOpacity)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlatformPolishProfileDefinition>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.Configure(mode, worldScale, worldYawDegrees, cameraPosition, cameraEuler, fieldOfView, nearClip, farClip, backgroundColor, ambientColor, targetFrameRate, vSyncCount, renderScale, useComfortVignette, vignetteRadius, vignetteOpacity);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ApplyProfileToCameraPrefab(string prefabPath, HollowPlatformKind platformKind, PlatformPolishProfileDefinition profile)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var metadata = root.GetComponent<CameraRigMetadata>() ?? root.AddComponent<CameraRigMetadata>();
                metadata.Configure(platformKind);
                _ = root.GetComponent<GameplayCameraFollowController>() ?? root.AddComponent<GameplayCameraFollowController>();
                var applier = root.GetComponent<PlatformPolishApplier>() ?? root.AddComponent<PlatformPolishApplier>();
                applier.Configure(profile);
                applier.Apply(root.GetComponentInChildren<Camera>(includeInactive: true), presentationRoot: null);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyProfileToScene(string scenePath, HollowPlatformKind platformKind, PlatformPolishProfileDefinition profile)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            var rig = Object.FindObjectsByType<CameraRigMetadata>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.PlatformKind == platformKind);
            if (rig == null)
            {
                return;
            }

            var applier = rig.GetComponent<PlatformPolishApplier>() ?? rig.gameObject.AddComponent<PlatformPolishApplier>();
            _ = rig.GetComponent<GameplayCameraFollowController>() ?? rig.gameObject.AddComponent<GameplayCameraFollowController>();
            applier.Configure(profile);
            applier.Apply(rig.GetComponentInChildren<Camera>(includeInactive: true), Object.FindAnyObjectByType<PlatformPresentationRoot>());
            EditorUtility.SetDirty(rig.gameObject);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureAddressables(params string[] profilePaths)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            settings.AddLabel(PlatformAddressableLabel, postEvent: false);
            var group = settings.FindGroup(Milestone9AssetGenerator.AddressablesGroupName) ?? settings.CreateGroup(
                Milestone9AssetGenerator.AddressablesGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

            foreach (var path in profilePaths)
            {
                var profile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(path);
                if (profile != null)
                {
                    MarkAddressable(settings, group, path, $"hollow.platform.{profile.Mode}", PlatformAddressableLabel);
                }
            }

            EditorUtility.SetDirty(settings);
        }

        private static void MarkAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(label, true, force: true, postEvent: false);
        }
    }
}
