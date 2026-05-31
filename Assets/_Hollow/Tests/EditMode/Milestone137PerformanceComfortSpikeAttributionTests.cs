using System;
using System.IO;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone137PerformanceComfortSpikeAttributionTests
    {
        private const string WindowsPolishPath = "Assets/_Hollow/Data/Platform/Polish/PlatformPolish_WindowsStandard3D.asset";
        private const string VisionOsImmersivePolishPath = "Assets/_Hollow/Data/Platform/Polish/PlatformPolish_VisionOSImmersive.asset";

        [Test]
        public void WindowsProfileUsesComfortTargetAndVisionOsTargetRemainsUnchanged()
        {
            var windows = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(WindowsPolishPath);
            var vision = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(VisionOsImmersivePolishPath);

            Assert.IsNotNull(windows);
            Assert.AreEqual(M137PerformanceComfortPolicy.WindowsComfortTargetFrameRate, windows.TargetFrameRate);
            Assert.AreEqual(0, windows.VSyncCount);
            Assert.IsNotNull(vision);
            Assert.AreEqual(90, vision.TargetFrameRate);
        }

        [Test]
        public void WallVisibilityRefreshesOnCadenceOrCameraTurnInsteadOfEveryFrame()
        {
            var root = new GameObject("M137WallVisibilityHarness");
            var cameraObject = new GameObject("M137WallCamera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, 8f, -8f);
                camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

                var controller = root.AddComponent<RoomWallVisibilityController>();
                controller.Configure(Array.Empty<RoomWallVisibilityController.WallBinding>(), RoomBiomeIds.HollowThreshold);
                controller.ApplyVisibility(camera);

                Assert.IsFalse(controller.ShouldRefresh(camera, Time.unscaledTime + 0.01f));
                Assert.IsTrue(controller.ShouldRefresh(
                    camera,
                    Time.unscaledTime + M137PerformanceComfortPolicy.WallVisibilityMinRefreshIntervalSeconds + 0.01f));

                controller.ApplyVisibility(camera);
                camera.transform.rotation = Quaternion.Euler(45f, 5f, 0f);
                Assert.IsTrue(controller.ShouldRefresh(camera, Time.unscaledTime + 0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void CaptureSamplerUsesValueTypeFrameSamplesAndPreallocatesBuffers()
        {
            using var sampler = new M136FrameTelemetrySampler(512);
            Assert.IsTrue(sampler.UsesValueTypeFrameSamples);
            Assert.GreaterOrEqual(sampler.FrameSampleCapacity, 512);

            sampler.RecordSyntheticFrame(16.67d, 128d, 6d, 2d, 128d, 512d, 7d);
            Assert.AreEqual(1, sampler.FrameSamples.Count);
            Assert.AreEqual(0, sampler.FrameSamples[0].sampleIndex);
        }

        [Test]
        public void M137ReportArtifactsAreGeneratedAndPassing()
        {
            Milestone137PerformanceComfortSpikeAttributionAssetGenerator.Generate();

            Assert.IsTrue(File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.DocsPath));
            Assert.IsTrue(File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportJsonPath));
            Assert.IsTrue(File.Exists(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportPdfPath));

            var markdown = File.ReadAllText(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M137 Performance Comfort + Spike Attribution", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains("Windows target FPS: 60", markdown);
            StringAssert.Contains("Post-Fix Recapture Checklist", markdown);

            var json = JsonUtility.FromJson<Milestone137PerformanceComfortSpikeAttributionReport>(
                File.ReadAllText(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.IsTrue(json.passed, string.Join("\n", json.failures ?? Array.Empty<string>()));
            Assert.AreEqual(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.LockId, json.lockId);
            Assert.AreEqual(M137PerformanceComfortPolicy.WindowsComfortTargetFrameRate, json.windowsTargetFrameRate);
            Assert.AreEqual(M136EditorLaptopPerformancePolicy.ScenarioManifest.Length, json.recaptureChecklist.Length);

            var pdf = File.ReadAllBytes(Milestone137PerformanceComfortSpikeAttributionAssetGenerator.ReportPdfPath);
            Assert.Greater(pdf.Length, 4096);
            Assert.AreEqual((byte)'%', pdf[0]);
            Assert.AreEqual((byte)'P', pdf[1]);
            Assert.AreEqual((byte)'D', pdf[2]);
            Assert.AreEqual((byte)'F', pdf[3]);
        }

        [Test]
        public void M137ValidatorAcceptsGeneratedPerformanceComfortPass()
        {
            Milestone137PerformanceComfortSpikeAttributionAssetGenerator.Generate();
            Assert.IsTrue(Milestone137PerformanceComfortSpikeAttributionValidator.Validate(exitOnFailure: false));
        }
    }
}
