using System;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class VisionOSVolumeCameraSetupTests
    {
        private static Type VolumeCameraType => Type.GetType("Unity.PolySpatial.VolumeCamera, Unity.PolySpatial");

        private static Type VolumeCameraWindowConfigurationType =>
            Type.GetType("Unity.PolySpatial.VolumeCameraWindowConfiguration, Unity.PolySpatial");

        [Test]
        public void VisionOSVolumeConfigurationsExistAndUseExpectedModes()
        {
            RequirePolySpatial();

            var bounded = AssetDatabase.LoadAssetAtPath(
                VisionOSVolumeCameraSetup.BoundedConfigPath,
                VolumeCameraWindowConfigurationType);
            var immersive = AssetDatabase.LoadAssetAtPath(
                VisionOSVolumeCameraSetup.ImmersiveConfigPath,
                VolumeCameraWindowConfigurationType);

            Assert.IsNotNull(bounded);
            Assert.IsNotNull(immersive);
            Assert.AreEqual("Bounded", ReadEnumName(bounded, "Mode", "m_Mode"));
            Assert.AreEqual("Unbounded", ReadEnumName(immersive, "Mode", "m_Mode"));
            AssertVectorApproximately(
                VisionOSVolumeCameraSetup.BoundedOutputDimensions,
                ReadVector3(bounded, "Dimensions", "m_OutputDimensions"));
            AssertVectorApproximately(Vector3.one, ReadVector3(immersive, "Dimensions", "m_OutputDimensions"));
            AssertVolumeAspectMatches(VisionOSVolumeCameraSetup.BoundedSourceDimensions, VisionOSVolumeCameraSetup.BoundedOutputDimensions);
            Assert.AreEqual(
                VisionOSVolumeCameraSetup.BoundedLevelFloorY,
                SourceBottomY(VisionOSVolumeCameraSetup.BoundedLevelSourceCenter, VisionOSVolumeCameraSetup.BoundedSourceDimensions),
                0.001f);
        }

        [TestCase("Assets/_Hollow/Scenes/Boot.unity", "Unbounded")]
        [TestCase("Assets/_Hollow/Scenes/MainMenu.unity", "Unbounded")]
        [TestCase("Assets/_Hollow/Scenes/MainMenu_VisionOS.unity", "Bounded")]
        [TestCase("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", "Bounded")]
        [TestCase("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", "Unbounded")]
        [TestCase("Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity", "Bounded")]
        public void VisionOSRuntimeScenesHaveExplicitVolumeCamera(string scenePath, string expectedMode)
        {
            RequirePolySpatial();
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var volumeCamera = VisionOSVolumeCameraSetup.FindOpenSceneVolumeCamera();

            Assert.IsNotNull(volumeCamera, $"{scenePath} should have an explicit VolumeCamera.");
            Assert.IsTrue(
                VisionOSVolumeCameraSetup.TryGetOpenWindowOnLoad(volumeCamera, out var openWindowOnLoad) &&
                openWindowOnLoad);
            var windowConfiguration = ReadObject(volumeCamera, "WindowConfiguration", "m_WindowConfiguration");
            Assert.IsNotNull(windowConfiguration);
            Assert.AreEqual(expectedMode, ReadEnumName(windowConfiguration, "Mode", "m_Mode"));
            if (expectedMode == "Bounded")
            {
                var dimensions = ReadVector3(volumeCamera, "Dimensions", "m_Dimensions");
                AssertVectorApproximately(VisionOSVolumeCameraSetup.BoundedSourceDimensions, dimensions);
                AssertVectorApproximately(
                    VisionOSVolumeCameraSetup.BoundedOutputDimensions,
                    ReadVector3(windowConfiguration, "Dimensions", "m_OutputDimensions"));
                AssertVectorApproximately(ExpectedBoundedSourceCenterFor(scenePath), volumeCamera.transform.localPosition);
                AssertVolumeAspectMatches(
                    dimensions,
                    ReadVector3(windowConfiguration, "Dimensions", "m_OutputDimensions"));

                if (UsesBottomAnchoredLevelFraming(scenePath))
                {
                    Assert.AreEqual(
                        VisionOSVolumeCameraSetup.BoundedLevelFloorY,
                        SourceBottomY(volumeCamera.transform.localPosition, dimensions),
                        0.001f);
                    AssertWorldRootUnmoved(scenePath);
                }
            }
        }

        private static void RequirePolySpatial()
        {
            if (VolumeCameraType == null || VolumeCameraWindowConfigurationType == null)
            {
                Assert.Ignore("PolySpatial is not available in this editor; skipping visionOS VolumeCamera setup tests.");
            }
        }

        private static void AssertVolumeAspectMatches(Vector3 source, Vector3 output)
        {
            Assert.AreEqual(output.x / output.y, source.x / source.y, 0.001f);
            Assert.AreEqual(output.z / output.y, source.z / source.y, 0.001f);
        }

        private static Vector3 ExpectedBoundedSourceCenterFor(string scenePath)
        {
            return UsesBottomAnchoredLevelFraming(scenePath)
                ? VisionOSVolumeCameraSetup.BoundedLevelSourceCenter
                : VisionOSVolumeCameraSetup.BoundedMenuSourceCenter;
        }

        private static bool UsesBottomAnchoredLevelFraming(string scenePath)
        {
            return scenePath.EndsWith("Game_VisionOS_Bounded.unity")
                || scenePath.EndsWith("ArenaMode.unity");
        }

        private static float SourceBottomY(Vector3 center, Vector3 dimensions)
        {
            return center.y - dimensions.y * 0.5f;
        }

        private static void AssertWorldRootUnmoved(string scenePath)
        {
            var worldRoot = GameObject.Find("WorldPresentationRoot");
            Assert.IsNotNull(worldRoot, $"{scenePath} should keep a WorldPresentationRoot.");
            AssertVectorApproximately(Vector3.zero, worldRoot.transform.localPosition);

            if (scenePath.EndsWith("Game_VisionOS_Bounded.unity"))
            {
                AssertVectorApproximately(
                    Vector3.one * PresentationScalePolicy.VisionOSBoundedTabletopScale,
                    worldRoot.transform.localScale);
            }
            else
            {
                Assert.AreEqual(worldRoot.transform.localScale.x, worldRoot.transform.localScale.y, 0.001f);
                Assert.AreEqual(worldRoot.transform.localScale.x, worldRoot.transform.localScale.z, 0.001f);
            }
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }

        private static string ReadEnumName(UnityEngine.Object target, string propertyName, string serializedName)
        {
            if (target == null)
            {
                return string.Empty;
            }

            var property = target.GetType().GetProperty(propertyName);
            var propertyValue = property?.GetValue(target);
            if (propertyValue != null)
            {
                return propertyValue.ToString();
            }

            var serialized = new SerializedObject(target);
            var serializedProperty = serialized.FindProperty(serializedName);
            if (serializedProperty == null ||
                serializedProperty.enumNames == null ||
                serializedProperty.enumValueIndex < 0 ||
                serializedProperty.enumValueIndex >= serializedProperty.enumNames.Length)
            {
                return string.Empty;
            }

            return serializedProperty.enumNames[serializedProperty.enumValueIndex];
        }

        private static UnityEngine.Object ReadObject(Component component, string propertyName, string serializedName)
        {
            if (component == null)
            {
                return null;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property?.GetValue(component) is UnityEngine.Object propertyValue)
            {
                return propertyValue;
            }

            var serialized = new SerializedObject(component);
            return serialized.FindProperty(serializedName)?.objectReferenceValue;
        }

        private static Vector3 ReadVector3(UnityEngine.Object target, string propertyName, string serializedName)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            var property = target.GetType().GetProperty(propertyName);
            if (property?.GetValue(target) is Vector3 propertyValue)
            {
                return propertyValue;
            }

            var serialized = new SerializedObject(target);
            var serializedProperty = serialized.FindProperty(serializedName);
            return serializedProperty != null
                ? serializedProperty.vector3Value
                : Vector3.zero;
        }
    }
}
