using System;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone5BeautifulGraphicsBudgetTests
    {
        private int previousTargetFrameRate;
        private int previousVSyncCount;
        private RenderPipelineAsset previousPipeline;

        [SetUp]
        public void SetUp()
        {
            previousTargetFrameRate = Application.targetFrameRate;
            previousVSyncCount = QualitySettings.vSyncCount;
            previousPipeline = QualitySettings.renderPipeline;
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.vSyncCount = previousVSyncCount;
            QualitySettings.renderPipeline = previousPipeline;
        }

        [Test]
        public void GeneratedRenderProfilesHaveDistinctBudgets()
        {
            Milestone5BeautifulGraphicsBudgetAssetGenerator.Generate();

            var dev = Load(Milestone5BeautifulGraphicsBudgetAssetGenerator.DevCoolProfilePath);
            var windows = Load(Milestone5BeautifulGraphicsBudgetAssetGenerator.WindowsQualityProfilePath);
            var bounded = Load(Milestone5BeautifulGraphicsBudgetAssetGenerator.VisionOSBoundedProfilePath);
            var immersive = Load(Milestone5BeautifulGraphicsBudgetAssetGenerator.VisionOSImmersiveProfilePath);

            Assert.AreEqual(HollowRenderProfileKind.DevCool, dev.ProfileKind);
            Assert.AreEqual(HollowRenderProfileKind.WindowsQuality, windows.ProfileKind);
            Assert.AreEqual(HollowRenderProfileKind.VisionOSBounded, bounded.ProfileKind);
            Assert.AreEqual(HollowRenderProfileKind.VisionOSImmersive, immersive.ProfileKind);
            Assert.AreEqual(1f, dev.RenderScale, 0.001f);
            Assert.AreEqual(windows.RenderScale, dev.RenderScale, 0.001f);
            Assert.Less(dev.TargetFrameRate, windows.TargetFrameRate);
            Assert.IsFalse(dev.AdditionalLightShadows);
            Assert.IsTrue(windows.AdditionalLightShadows);
            Assert.AreEqual(0.9f, bounded.RenderScale, 0.001f);
            Assert.AreEqual(0.85f, immersive.RenderScale, 0.001f);
            Assert.IsFalse(bounded.AdditionalLightShadows);
            Assert.IsFalse(immersive.AdditionalLightShadows);
            Assert.AreEqual(1, immersive.ShadowCascadeCount);
        }

        [Test]
        public void PlatformPolishProfilesResolveRenderProfiles()
        {
            Milestone5BeautifulGraphicsBudgetAssetGenerator.Generate();

            var windowsPolish = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(Milestone10AssetGenerator.WindowsProfilePath);
            var boundedPolish = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(Milestone10AssetGenerator.BoundedProfilePath);
            var immersivePolish = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(Milestone10AssetGenerator.ImmersiveProfilePath);

            Assert.AreEqual(HollowRenderProfileKind.WindowsQuality, windowsPolish.RenderProfile.ProfileKind);
            Assert.AreEqual(HollowRenderProfileKind.VisionOSBounded, boundedPolish.RenderProfile.ProfileKind);
            Assert.AreEqual(HollowRenderProfileKind.VisionOSImmersive, immersivePolish.RenderProfile.ProfileKind);
        }

        [Test]
        public void RenderProfileApplierUsesProfilePerformanceAndPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var profile = ScriptableObject.CreateInstance<HollowRenderProfileDefinition>();
            try
            {
                profile.Configure(
                    HollowRenderProfileKind.DevCool,
                    pipeline,
                    45,
                    0,
                    1f,
                    true,
                    true,
                    true,
                    2048,
                    45f,
                    2,
                    false,
                    2,
                    true,
                    56,
                    40,
                    10,
                    2048,
                    1024,
                    22.22f);

                RenderProfileApplier.Apply(profile);

                Assert.AreEqual(45, Application.targetFrameRate);
                Assert.AreEqual(0, QualitySettings.vSyncCount);
                Assert.AreEqual(pipeline, QualitySettings.renderPipeline);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void TextureAuditDetectsWorldAndUiBudgetProblems()
        {
            Assert.IsTrue(Milestone5BeautifulGraphicsBudgetAudit.TryEvaluateTexture(
                new Milestone5TextureAuditInput("Assets/_Hollow/Art/Textures/floor.png", TextureImporterType.Default, false, 1024, TextureImporterCompression.Compressed),
                1024,
                768,
                out var worldWarning));
            StringAssert.Contains("mipmaps disabled", worldWarning);

            Assert.IsTrue(Milestone5BeautifulGraphicsBudgetAudit.TryEvaluateTexture(
                new Milestone5TextureAuditInput("Assets/_Hollow/UI/Icon.png", TextureImporterType.Sprite, true, 512, TextureImporterCompression.Compressed),
                1024,
                768,
                out var uiWarning));
            StringAssert.Contains("mipmaps enabled", uiWarning);

            Assert.IsTrue(Milestone5BeautifulGraphicsBudgetAudit.TryEvaluateTexture(
                new Milestone5TextureAuditInput("Assets/_Hollow/Art/Textures/hero.png", TextureImporterType.Default, true, 4096, TextureImporterCompression.Uncompressed),
                2048,
                1024,
                out var compressionWarning));
            StringAssert.Contains("exceeds", compressionWarning);
        }

        [Test]
        public void VfxAuditDetectsUncappedParticleBursts()
        {
            var root = new GameObject("VFX_TestBurst");
            try
            {
                var particles = root.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.maxParticles = 512;

                Assert.IsTrue(Milestone5BeautifulGraphicsBudgetAudit.TryEvaluateVfxPrefab(root, out var warning));
                StringAssert.Contains("max particles", warning);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReportAndValidatorPassGeneratedBudget()
        {
            Milestone5BeautifulGraphicsBudgetAssetGenerator.Generate();

            Assert.IsTrue(File.Exists(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportJsonPath));
            Assert.IsTrue(Milestone5BeautifulGraphicsBudgetValidator.Validate(exitOnFailure: false));

            var report = JsonUtility.FromJson<Milestone5BeautifulGraphicsBudgetReport>(
                File.ReadAllText(Milestone5BeautifulGraphicsBudgetAssetGenerator.ReportJsonPath));
            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? Array.Empty<string>()));
            Assert.AreEqual(4, report.profiles.Length);
            Assert.AreEqual(5, report.liveCaptureScenarios.Length);
        }

        private static HollowRenderProfileDefinition Load(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<HollowRenderProfileDefinition>(path);
            Assert.IsNotNull(profile, path);
            return profile;
        }
    }
}
