using System.IO;
using Hollow.Platform;
using Unity.PolySpatial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.Generation
{
    public enum VisionOSBoundedVolumeFraming
    {
        MenuCentered,
        LevelBottomAnchored
    }

    public static class VisionOSVolumeCameraSetup
    {
        public const string BoundedConfigPath = "Assets/Resources/Hollow_VisionOS_BoundedVolumeCamera.asset";
        public const string ImmersiveConfigPath = "Assets/Resources/Hollow_VisionOS_ImmersiveVolumeCamera.asset";
        public const string VolumeCameraObjectName = "VisionOSVolumeCamera";
        public const float BoundedLevelFloorY = 0f;

        public static readonly Vector3 BoundedSourceDimensions = new(8f, 5.333333f, 8f);
        public static readonly Vector3 BoundedMenuSourceCenter = new(0f, 0.9f, 0.25f);
        public static readonly Vector3 BoundedLevelSourceCenter = new(
            0f,
            BoundedLevelFloorY + BoundedSourceDimensions.y * 0.5f,
            BoundedMenuSourceCenter.z);
        public static readonly Vector3 BoundedSourceCenter = BoundedMenuSourceCenter;
        public static readonly Vector3 BoundedOutputDimensions = new(1.2f, 0.8f, 1.2f);
        public static readonly Vector3 ImmersiveSourceDimensions = Vector3.one;

        [MenuItem("Hollow/Generation/Configure visionOS Volume Cameras")]
        public static void ConfigureProject()
        {
            EnsureConfigurations();
            EnsureSceneVolumeCamera("Assets/_Hollow/Scenes/Boot.unity", HollowPlatformKind.VisionOSImmersive);
            EnsureSceneVolumeCamera("Assets/_Hollow/Scenes/MainMenu.unity", HollowPlatformKind.VisionOSImmersive);
            EnsureSceneVolumeCamera(
                "Assets/_Hollow/Scenes/MainMenu_VisionOS.unity",
                HollowPlatformKind.VisionOSBoundedTabletop,
                VisionOSBoundedVolumeFraming.MenuCentered);
            EnsureSceneVolumeCamera(
                "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
                HollowPlatformKind.VisionOSBoundedTabletop,
                VisionOSBoundedVolumeFraming.LevelBottomAnchored);
            EnsureSceneVolumeCamera("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive);
            EnsureSceneVolumeCamera(
                "Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity",
                HollowPlatformKind.VisionOSBoundedTabletop,
                VisionOSBoundedVolumeFraming.LevelBottomAnchored);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Configured explicit Hollow visionOS Volume Cameras.");
        }

        public static void EnsureConfigurations()
        {
            Directory.CreateDirectory("Assets/Resources");
            EnsureWindowConfiguration(
                BoundedConfigPath,
                VolumeCamera.PolySpatialVolumeCameraMode.Bounded,
                BoundedOutputDimensions);
            EnsureWindowConfiguration(
                ImmersiveConfigPath,
                VolumeCamera.PolySpatialVolumeCameraMode.Unbounded,
                Vector3.one);
        }

        public static void EnsureSceneVolumeCamera(
            string scenePath,
            HollowPlatformKind platformKind,
            VisionOSBoundedVolumeFraming boundedFraming = VisionOSBoundedVolumeFraming.MenuCentered)
        {
            if (!File.Exists(scenePath))
            {
                return;
            }

            EnsureConfigurations();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EnsureOpenSceneVolumeCamera(platformKind, boundedFraming);
            EditorSceneManager.SaveScene(scene);
        }

        public static VolumeCamera EnsureOpenSceneVolumeCamera(
            HollowPlatformKind platformKind,
            VisionOSBoundedVolumeFraming boundedFraming = VisionOSBoundedVolumeFraming.MenuCentered)
        {
            var existing = Object.FindFirstObjectByType<VolumeCamera>(FindObjectsInactive.Include);
            var volumeObject = existing != null
                ? existing.gameObject
                : new GameObject(VolumeCameraObjectName);
            volumeObject.name = VolumeCameraObjectName;

            var volumeCamera = volumeObject.GetComponent<VolumeCamera>() ?? volumeObject.AddComponent<VolumeCamera>();
            ConfigureVolumeCamera(volumeCamera, platformKind, boundedFraming);
            EditorUtility.SetDirty(volumeObject);
            EditorUtility.SetDirty(volumeCamera);
            return volumeCamera;
        }

        public static void ConfigureVolumeCamera(
            VolumeCamera volumeCamera,
            HollowPlatformKind platformKind,
            VisionOSBoundedVolumeFraming boundedFraming = VisionOSBoundedVolumeFraming.MenuCentered)
        {
            if (volumeCamera == null)
            {
                return;
            }

            var isBounded = platformKind == HollowPlatformKind.VisionOSBoundedTabletop;
            volumeCamera.WindowConfiguration = AssetDatabase.LoadAssetAtPath<VolumeCameraWindowConfiguration>(
                isBounded ? BoundedConfigPath : ImmersiveConfigPath);
            volumeCamera.OpenWindowOnLoad = true;
            volumeCamera.CullingMask = ~0;
            volumeCamera.ScaleWithWindow = true;

            volumeCamera.transform.localRotation = Quaternion.identity;
            volumeCamera.transform.localScale = Vector3.one;
            if (isBounded)
            {
                volumeCamera.transform.localPosition = SourceCenterFor(boundedFraming);
                volumeCamera.Dimensions = BoundedSourceDimensions;
            }
            else
            {
                volumeCamera.transform.localPosition = Vector3.zero;
                volumeCamera.Dimensions = ImmersiveSourceDimensions;
            }
        }

        public static Vector3 SourceCenterFor(VisionOSBoundedVolumeFraming boundedFraming)
        {
            return boundedFraming == VisionOSBoundedVolumeFraming.LevelBottomAnchored
                ? BoundedLevelSourceCenter
                : BoundedMenuSourceCenter;
        }

        private static VolumeCameraWindowConfiguration EnsureWindowConfiguration(
            string path,
            VolumeCamera.PolySpatialVolumeCameraMode mode,
            Vector3 outputDimensions)
        {
            var configuration = AssetDatabase.LoadAssetAtPath<VolumeCameraWindowConfiguration>(path);
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance<VolumeCameraWindowConfiguration>();
                AssetDatabase.CreateAsset(configuration, path);
            }

            var serialized = new SerializedObject(configuration);
            serialized.FindProperty("m_Mode").enumValueIndex = (int)mode;
            serialized.FindProperty("m_OutputDimensions").vector3Value = outputDimensions;
            serialized.FindProperty("m_WindowResizeLimit").enumValueIndex = (int)VolumeCamera.PolySpatialWindowResizeLimits.FixedSize;
            serialized.FindProperty("m_WorldAlignment").enumValueIndex = (int)VolumeCamera.PolySpatialWindowWorldAlignment.GravityAligned;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configuration);
            return configuration;
        }
    }
}
