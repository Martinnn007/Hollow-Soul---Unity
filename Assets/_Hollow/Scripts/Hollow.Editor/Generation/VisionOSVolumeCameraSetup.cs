using System;
using System.IO;
using System.Linq;
using Hollow.Platform;
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

        private static Type VolumeCameraType => Type.GetType("Unity.PolySpatial.VolumeCamera, Unity.PolySpatial");

        private static Type VolumeCameraWindowConfigurationType =>
            Type.GetType("Unity.PolySpatial.VolumeCameraWindowConfiguration, Unity.PolySpatial");

        public static bool IsPolySpatialAvailable =>
            VolumeCameraType != null &&
            VolumeCameraWindowConfigurationType != null;

        [MenuItem("Hollow/Generation/Configure visionOS Volume Cameras")]
        public static void ConfigureProject()
        {
            if (!IsPolySpatialAvailable)
            {
                Debug.LogWarning("PolySpatial is not available in this editor. Skipping visionOS VolumeCamera generation.");
                return;
            }

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
            if (!IsPolySpatialAvailable)
            {
                Debug.LogWarning("PolySpatial is not available in this editor. Skipping visionOS VolumeCamera configuration assets.");
                return;
            }

            Directory.CreateDirectory("Assets/Resources");
            EnsureWindowConfiguration(
                BoundedConfigPath,
                "Bounded",
                BoundedOutputDimensions);
            EnsureWindowConfiguration(
                ImmersiveConfigPath,
                "Unbounded",
                Vector3.one);
        }

        public static void EnsureSceneVolumeCamera(
            string scenePath,
            HollowPlatformKind platformKind,
            VisionOSBoundedVolumeFraming boundedFraming = VisionOSBoundedVolumeFraming.MenuCentered)
        {
            if (!IsPolySpatialAvailable)
            {
                Debug.LogWarning($"PolySpatial is not available in this editor. Skipping VolumeCamera setup for {scenePath}.");
                return;
            }

            if (!File.Exists(scenePath))
            {
                return;
            }

            EnsureConfigurations();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EnsureOpenSceneVolumeCamera(platformKind, boundedFraming);
            EditorSceneManager.SaveScene(scene);
        }

        public static Component EnsureOpenSceneVolumeCamera(
            HollowPlatformKind platformKind,
            VisionOSBoundedVolumeFraming boundedFraming = VisionOSBoundedVolumeFraming.MenuCentered)
        {
            var volumeCameraType = VolumeCameraType;
            if (volumeCameraType == null)
            {
                Debug.LogWarning("PolySpatial is not available in this editor. Cannot create VolumeCamera.");
                return null;
            }

            var existing = FindOpenSceneVolumeCamera();
            var volumeObject = existing != null
                ? existing.gameObject
                : new GameObject(VolumeCameraObjectName);
            volumeObject.name = VolumeCameraObjectName;

            var volumeCamera = volumeObject.GetComponent(volumeCameraType) ?? volumeObject.AddComponent(volumeCameraType);
            ConfigureVolumeCamera(volumeCamera, platformKind, boundedFraming);
            EditorUtility.SetDirty(volumeObject);
            EditorUtility.SetDirty(volumeCamera);
            return volumeCamera;
        }

        public static void ConfigureVolumeCamera(
            Component volumeCamera,
            HollowPlatformKind platformKind,
            VisionOSBoundedVolumeFraming boundedFraming = VisionOSBoundedVolumeFraming.MenuCentered)
        {
            if (volumeCamera == null)
            {
                return;
            }

            var isBounded = platformKind == HollowPlatformKind.VisionOSBoundedTabletop;
            var configuration = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                isBounded ? BoundedConfigPath : ImmersiveConfigPath);
            SetObjectValue(volumeCamera, "WindowConfiguration", "m_WindowConfiguration", configuration);
            SetBoolValue(volumeCamera, "OpenWindowOnLoad", "m_OpenWindowOnLoad", true);
            SetIntValue(volumeCamera, "CullingMask", "m_CullingMask", ~0);
            SetBoolValue(volumeCamera, "ScaleWithWindow", "m_ScaleWithWindow", true);

            volumeCamera.transform.localRotation = Quaternion.identity;
            volumeCamera.transform.localScale = Vector3.one;
            if (isBounded)
            {
                volumeCamera.transform.localPosition = SourceCenterFor(boundedFraming);
                SetVector3Value(volumeCamera, "Dimensions", "m_Dimensions", BoundedSourceDimensions);
            }
            else
            {
                volumeCamera.transform.localPosition = Vector3.zero;
                SetVector3Value(volumeCamera, "Dimensions", "m_Dimensions", ImmersiveSourceDimensions);
            }
        }

        public static Vector3 SourceCenterFor(VisionOSBoundedVolumeFraming boundedFraming)
        {
            return boundedFraming == VisionOSBoundedVolumeFraming.LevelBottomAnchored
                ? BoundedLevelSourceCenter
                : BoundedMenuSourceCenter;
        }

        public static Component FindOpenSceneVolumeCamera()
        {
            var volumeCameraType = VolumeCameraType;
            if (volumeCameraType == null)
            {
                return null;
            }

            return UnityEngine.Object
                .FindObjectsByType(volumeCameraType, FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<Component>()
                .FirstOrDefault();
        }

        public static bool TryGetOpenWindowOnLoad(Component volumeCamera, out bool openWindowOnLoad)
        {
            return TryGetBoolValue(volumeCamera, "OpenWindowOnLoad", "m_OpenWindowOnLoad", out openWindowOnLoad);
        }

        public static bool HasWindowConfiguration(Component volumeCamera)
        {
            return TryGetObjectValue(volumeCamera, "WindowConfiguration", "m_WindowConfiguration", out var configuration) &&
                configuration != null;
        }

        private static ScriptableObject EnsureWindowConfiguration(
            string path,
            string modeName,
            Vector3 outputDimensions)
        {
            var configurationType = VolumeCameraWindowConfigurationType;
            var volumeCameraType = VolumeCameraType;
            if (configurationType == null || volumeCameraType == null)
            {
                Debug.LogWarning("PolySpatial is not available in this editor. Cannot create VolumeCameraWindowConfiguration.");
                return null;
            }

            var configuration = AssetDatabase.LoadAssetAtPath(path, configurationType) as ScriptableObject;
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance(configurationType);
                AssetDatabase.CreateAsset(configuration, path);
            }

            var serialized = new SerializedObject(configuration);
            serialized.FindProperty("m_Mode").enumValueIndex = EnumIndex(
                volumeCameraType.GetNestedType("PolySpatialVolumeCameraMode"),
                modeName);
            serialized.FindProperty("m_OutputDimensions").vector3Value = outputDimensions;
            serialized.FindProperty("m_WindowResizeLimit").enumValueIndex = EnumIndex(
                volumeCameraType.GetNestedType("PolySpatialWindowResizeLimits"),
                "FixedSize");
            serialized.FindProperty("m_WorldAlignment").enumValueIndex = EnumIndex(
                volumeCameraType.GetNestedType("PolySpatialWindowWorldAlignment"),
                "GravityAligned");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configuration);
            return configuration;
        }

        private static int EnumIndex(Type enumType, string name)
        {
            if (enumType == null)
            {
                return 0;
            }

            var names = Enum.GetNames(enumType);
            var index = Array.IndexOf(names, name);
            return index >= 0 ? index : 0;
        }

        private static void SetObjectValue(Component component, string propertyName, string serializedName, UnityEngine.Object value)
        {
            if (component == null)
            {
                return;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value);
                return;
            }

            var serialized = new SerializedObject(component);
            var serializedProperty = serialized.FindProperty(serializedName);
            if (serializedProperty != null)
            {
                serializedProperty.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBoolValue(Component component, string propertyName, string serializedName, bool value)
        {
            if (component == null)
            {
                return;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value);
                return;
            }

            var serialized = new SerializedObject(component);
            var serializedProperty = serialized.FindProperty(serializedName);
            if (serializedProperty != null)
            {
                serializedProperty.boolValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetIntValue(Component component, string propertyName, string serializedName, int value)
        {
            if (component == null)
            {
                return;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                if (property.PropertyType == typeof(LayerMask))
                {
                    property.SetValue(component, new LayerMask { value = value });
                }
                else
                {
                    property.SetValue(component, value);
                }

                return;
            }

            var serialized = new SerializedObject(component);
            var serializedProperty = serialized.FindProperty(serializedName) ??
                serialized.FindProperty($"{serializedName}.m_Bits");
            if (serializedProperty != null)
            {
                serializedProperty.intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetVector3Value(Component component, string propertyName, string serializedName, Vector3 value)
        {
            if (component == null)
            {
                return;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value);
                return;
            }

            var serialized = new SerializedObject(component);
            var serializedProperty = serialized.FindProperty(serializedName);
            if (serializedProperty != null)
            {
                serializedProperty.vector3Value = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static bool TryGetBoolValue(Component component, string propertyName, string serializedName, out bool value)
        {
            value = false;
            if (component == null)
            {
                return false;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.GetValue(component) is bool propertyValue)
            {
                value = propertyValue;
                return true;
            }

            var serialized = new SerializedObject(component);
            var serializedProperty = serialized.FindProperty(serializedName);
            if (serializedProperty == null)
            {
                return false;
            }

            value = serializedProperty.boolValue;
            return true;
        }

        private static bool TryGetObjectValue(
            Component component,
            string propertyName,
            string serializedName,
            out UnityEngine.Object value)
        {
            value = null;
            if (component == null)
            {
                return false;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null)
            {
                value = property.GetValue(component) as UnityEngine.Object;
                return true;
            }

            var serialized = new SerializedObject(component);
            var serializedProperty = serialized.FindProperty(serializedName);
            if (serializedProperty == null)
            {
                return false;
            }

            value = serializedProperty.objectReferenceValue;
            return true;
        }
    }
}
