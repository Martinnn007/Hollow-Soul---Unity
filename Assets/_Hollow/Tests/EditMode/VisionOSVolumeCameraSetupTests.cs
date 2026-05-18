using Hollow.Editor.Generation;
using Hollow.Presentation;
using NUnit.Framework;
using Unity.PolySpatial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class VisionOSVolumeCameraSetupTests
    {
        [Test]
        public void VisionOSVolumeConfigurationsExistAndUseExpectedModes()
        {
            var bounded = AssetDatabase.LoadAssetAtPath<VolumeCameraWindowConfiguration>(VisionOSVolumeCameraSetup.BoundedConfigPath);
            var immersive = AssetDatabase.LoadAssetAtPath<VolumeCameraWindowConfiguration>(VisionOSVolumeCameraSetup.ImmersiveConfigPath);

            Assert.IsNotNull(bounded);
            Assert.IsNotNull(immersive);
            Assert.AreEqual(VolumeCamera.PolySpatialVolumeCameraMode.Bounded, bounded.Mode);
            Assert.AreEqual(VolumeCamera.PolySpatialVolumeCameraMode.Unbounded, immersive.Mode);
            Assert.AreEqual(VisionOSVolumeCameraSetup.BoundedOutputDimensions, bounded.Dimensions);
            Assert.AreEqual(Vector3.one, immersive.Dimensions);
            AssertVolumeAspectMatches(VisionOSVolumeCameraSetup.BoundedSourceDimensions, VisionOSVolumeCameraSetup.BoundedOutputDimensions);
            Assert.AreEqual(
                VisionOSVolumeCameraSetup.BoundedLevelFloorY,
                SourceBottomY(VisionOSVolumeCameraSetup.BoundedLevelSourceCenter, VisionOSVolumeCameraSetup.BoundedSourceDimensions),
                0.001f);
        }

        [TestCase("Assets/_Hollow/Scenes/Boot.unity", VolumeCamera.PolySpatialVolumeCameraMode.Unbounded)]
        [TestCase("Assets/_Hollow/Scenes/MainMenu.unity", VolumeCamera.PolySpatialVolumeCameraMode.Unbounded)]
        [TestCase("Assets/_Hollow/Scenes/MainMenu_VisionOS.unity", VolumeCamera.PolySpatialVolumeCameraMode.Bounded)]
        [TestCase("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", VolumeCamera.PolySpatialVolumeCameraMode.Bounded)]
        [TestCase("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", VolumeCamera.PolySpatialVolumeCameraMode.Unbounded)]
        [TestCase("Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity", VolumeCamera.PolySpatialVolumeCameraMode.Bounded)]
        public void VisionOSRuntimeScenesHaveExplicitVolumeCamera(string scenePath, VolumeCamera.PolySpatialVolumeCameraMode expectedMode)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>(FindObjectsInactive.Include);

            Assert.IsNotNull(volumeCamera, $"{scenePath} should have an explicit VolumeCamera.");
            Assert.IsTrue(volumeCamera.OpenWindowOnLoad);
            Assert.IsNotNull(volumeCamera.WindowConfiguration);
            Assert.AreEqual(expectedMode, volumeCamera.WindowConfiguration.Mode);
            if (expectedMode == VolumeCamera.PolySpatialVolumeCameraMode.Bounded)
            {
                Assert.AreEqual(VisionOSVolumeCameraSetup.BoundedSourceDimensions, volumeCamera.Dimensions);
                Assert.AreEqual(VisionOSVolumeCameraSetup.BoundedOutputDimensions, volumeCamera.WindowConfiguration.Dimensions);
                AssertVectorApproximately(ExpectedBoundedSourceCenterFor(scenePath), volumeCamera.transform.localPosition);
                AssertVolumeAspectMatches(volumeCamera.Dimensions, volumeCamera.WindowConfiguration.Dimensions);

                if (UsesBottomAnchoredLevelFraming(scenePath))
                {
                    Assert.AreEqual(
                        VisionOSVolumeCameraSetup.BoundedLevelFloorY,
                        SourceBottomY(volumeCamera.transform.localPosition, volumeCamera.Dimensions),
                        0.001f);
                    AssertWorldRootUnmoved(scenePath);
                }
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
    }
}
