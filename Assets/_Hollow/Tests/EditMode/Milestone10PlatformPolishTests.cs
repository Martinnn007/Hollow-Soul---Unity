using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone10PlatformPolishTests
    {
        private int previousTargetFrameRate;
        private int previousVSyncCount;

        [SetUp]
        public void SetUp()
        {
            previousTargetFrameRate = Application.targetFrameRate;
            previousVSyncCount = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.vSyncCount = previousVSyncCount;
        }

        [Test]
        public void GeneratedProfilesHaveDistinctPlatformPolishBudgets()
        {
            var windows = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(Milestone10AssetGenerator.WindowsProfilePath);
            var bounded = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(Milestone10AssetGenerator.BoundedProfilePath);
            var immersive = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(Milestone10AssetGenerator.ImmersiveProfilePath);

            Assert.IsNotNull(windows);
            Assert.IsNotNull(bounded);
            Assert.IsNotNull(immersive);
            Assert.AreEqual(PlatformPresentationMode.WindowsStandard3D, windows.Mode);
            Assert.AreEqual(PlatformPresentationMode.VisionOSBoundedTabletop, bounded.Mode);
            Assert.AreEqual(PlatformPresentationMode.VisionOSImmersive, immersive.Mode);
            Assert.AreEqual(1f, windows.WorldScale, 0.0001f);
            Assert.AreEqual(PresentationScalePolicy.VisionOSBoundedTabletopScale, bounded.WorldScale, 0.0001f);
            Assert.IsTrue(immersive.UseComfortVignette);
            Assert.GreaterOrEqual(windows.TargetFrameRate, 120);
            Assert.GreaterOrEqual(bounded.TargetFrameRate, 90);
            Assert.GreaterOrEqual(immersive.TargetFrameRate, 90);
        }

        [Test]
        public void PlatformPolishApplierUpdatesCameraRootAndPerformance()
        {
            var profile = ScriptableObject.CreateInstance<PlatformPolishProfileDefinition>();
            profile.Configure(
                PlatformPresentationMode.VisionOSImmersive,
                1f,
                new Vector3(0f, 4f, -7f),
                new Vector3(28f, 0f, 0f),
                52f,
                0.03f,
                70f,
                Color.black,
                Color.gray,
                90,
                0,
                0.85f,
                true,
                0.8f,
                0.2f);

            var rootObject = new GameObject("WorldPresentationRoot");
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            var applierObject = new GameObject("Applier");
            try
            {
                var root = rootObject.AddComponent<PlatformPresentationRoot>();
                var camera = cameraObject.GetComponent<Camera>();
                var applier = applierObject.AddComponent<PlatformPolishApplier>();
                applier.Configure(profile);

                applier.Apply(camera, root);

                Assert.AreEqual(1f, root.WorldScale, 0.0001f);
                Assert.AreEqual(52f, camera.fieldOfView, 0.0001f);
                Assert.AreEqual(new Vector3(0f, 4f, -7f), camera.transform.localPosition);
                Assert.AreEqual(90, Application.targetFrameRate);
                Assert.AreEqual(0, QualitySettings.vSyncCount);
                var vignette = camera.GetComponent<ComfortVignettePresenter>();
                Assert.IsNotNull(vignette);
                Assert.IsTrue(vignette.VignetteEnabled);
                Assert.AreEqual(0.8f, vignette.Radius, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(applierObject);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GameplayCameraFollowMovesRigOnTargetXZOnly()
        {
            var rigObject = new GameObject("GameplayCameraRig");
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            var playerObject = new GameObject("PlayerCharacter");
            try
            {
                cameraObject.transform.SetParent(rigObject.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 7f, -10f);
                cameraObject.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
                rigObject.transform.position = new Vector3(0f, 1.25f, 0f);
                playerObject.transform.position = new Vector3(4f, 0.4f, -3f);

                var follow = rigObject.AddComponent<GameplayCameraFollowController>();
                follow.Configure(playerObject.transform, Hollow.Platform.HollowPlatformKind.WindowsStandard3D);

                Assert.AreEqual(new Vector3(4f, 1.25f, -3f), rigObject.transform.position);
                Assert.AreEqual(new Vector3(0f, 7f, -10f), cameraObject.transform.localPosition);

                playerObject.transform.position = new Vector3(-2f, 0.4f, 5f);
                follow.ApplyImmediate();

                Assert.AreEqual(new Vector3(-2f, 1.25f, 5f), rigObject.transform.position);
                Assert.AreEqual(new Vector3(0f, 7f, -10f), cameraObject.transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(rigObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Milestone10ContentValidatorPassesGeneratedPipeline()
        {
            var report = ContentImportValidator.ValidateAll();
            Assert.IsTrue(report.IsValid, string.Join("\n", report.Failures));
        }
    }
}
