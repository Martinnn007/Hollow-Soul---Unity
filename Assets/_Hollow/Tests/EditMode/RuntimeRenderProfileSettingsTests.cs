using System;
using System.Collections.Generic;
using System.Reflection;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class RuntimeRenderProfileSettingsTests
    {
        private readonly List<PipelineMemberSnapshot> pipelineSnapshots = new();
        private int previousTargetFrameRate;
        private int previousVSyncCount;
        private RenderPipelineAsset previousPipeline;
        private bool hadPreviousPreference;
        private string previousPreference;
        private bool hadPreviousResolutionPreference;
        private string previousResolutionPreference;

        [SetUp]
        public void SetUp()
        {
            previousTargetFrameRate = Application.targetFrameRate;
            previousVSyncCount = QualitySettings.vSyncCount;
            previousPipeline = QualitySettings.renderPipeline;
            hadPreviousPreference = PlayerPrefs.HasKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            previousPreference = hadPreviousPreference ? PlayerPrefs.GetString(RuntimeRenderProfileSettings.PlayerPrefsKey) : string.Empty;
            hadPreviousResolutionPreference = PlayerPrefs.HasKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
            previousResolutionPreference = hadPreviousResolutionPreference ? PlayerPrefs.GetString(RuntimeRenderResolutionSettings.PlayerPrefsKey) : string.Empty;
            PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            PlayerPrefs.DeleteKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
            RuntimeRenderProfileSettings.ResetForTests();
            RuntimeRenderResolutionSettings.ResetForTests();
            CapturePipelineMembers(RuntimeRenderProfileSettings.ProfileFor(RuntimeRenderProfileMode.Cool)?.RenderPipelineAsset);
        }

        [TearDown]
        public void TearDown()
        {
            RestorePipelineMembers();
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.vSyncCount = previousVSyncCount;
            QualitySettings.renderPipeline = previousPipeline;
            if (hadPreviousPreference)
            {
                PlayerPrefs.SetString(RuntimeRenderProfileSettings.PlayerPrefsKey, previousPreference);
            }
            else
            {
                PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            }

            if (hadPreviousResolutionPreference)
            {
                PlayerPrefs.SetString(RuntimeRenderResolutionSettings.PlayerPrefsKey, previousResolutionPreference);
            }
            else
            {
                PlayerPrefs.DeleteKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
            }

            RuntimeRenderProfileSettings.ResetForTests();
            RuntimeRenderResolutionSettings.ResetForTests();
            pipelineSnapshots.Clear();
        }

        [Test]
        public void DefaultsToCoolWhenNoPreferenceExists()
        {
            Assert.AreEqual(RuntimeRenderProfileMode.Cool, RuntimeRenderProfileSettings.CurrentMode);
            Assert.NotNull(RuntimeRenderProfileSettings.CurrentProfile);
            Assert.AreEqual(HollowRenderProfileKind.DevCool, RuntimeRenderProfileSettings.CurrentProfile.ProfileKind);
        }

        [Test]
        public void SelectedModePersistsThroughPlayerPrefs()
        {
            RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Quality);
            RuntimeRenderProfileSettings.ResetForTests();

            Assert.AreEqual(RuntimeRenderProfileMode.Quality, RuntimeRenderProfileSettings.CurrentMode);
            Assert.AreEqual(HollowRenderProfileKind.WindowsQuality, RuntimeRenderProfileSettings.CurrentProfile.ProfileKind);
        }

        [Test]
        public void ApplyingCoolAndQualityUpdatesRuntimePipelineValues()
        {
            var cool = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Cool, persist: false);

            Assert.NotNull(cool);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(cool.RenderPipelineAsset, QualitySettings.renderPipeline);
            Assert.AreEqual(0.75f, ReadFloat(cool.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);

            var quality = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Quality, persist: false);

            Assert.NotNull(quality);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(quality.RenderPipelineAsset, QualitySettings.renderPipeline);
            Assert.AreEqual(1f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);
        }

        [Test]
        public void UnsetRenderResolutionUsesProfileDefaultScale()
        {
            var cool = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Cool, persist: false);

            Assert.AreEqual(RuntimeRenderResolutionMode.Balanced, RuntimeRenderResolutionSettings.CurrentMode);
            Assert.IsFalse(RuntimeRenderResolutionSettings.HasExplicitMode);
            Assert.AreEqual(0.75f, ReadFloat(cool.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);

            var quality = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Quality, persist: false);

            Assert.AreEqual(RuntimeRenderResolutionMode.Native, RuntimeRenderResolutionSettings.CurrentMode);
            Assert.IsFalse(RuntimeRenderResolutionSettings.HasExplicitMode);
            Assert.AreEqual(1f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);
        }

        [Test]
        public void SelectedRenderResolutionPersistsThroughPlayerPrefs()
        {
            foreach (var mode in new[] { RuntimeRenderResolutionMode.Native, RuntimeRenderResolutionMode.Balanced, RuntimeRenderResolutionMode.Low })
            {
                PlayerPrefs.DeleteKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
                RuntimeRenderResolutionSettings.ResetForTests();

                RuntimeRenderResolutionSettings.SetMode(mode);
                RuntimeRenderResolutionSettings.ResetForTests();

                Assert.AreEqual(mode, RuntimeRenderResolutionSettings.CurrentMode);
                Assert.IsTrue(RuntimeRenderResolutionSettings.HasExplicitMode);
            }
        }

        [Test]
        public void ApplyingRenderResolutionUpdatesPipelineRenderScaleOnly()
        {
            var quality = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Quality, persist: false);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(1f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);

            RuntimeRenderResolutionSettings.SetMode(RuntimeRenderResolutionMode.Low, persist: false);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(quality.RenderPipelineAsset, QualitySettings.renderPipeline);
            Assert.AreEqual(0.5f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);

            RuntimeRenderResolutionSettings.SetMode(RuntimeRenderResolutionMode.Balanced, persist: false);
            Assert.AreEqual(0.75f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);

            RuntimeRenderResolutionSettings.SetMode(RuntimeRenderResolutionMode.Native, persist: false);
            Assert.AreEqual(1f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);
        }

        [Test]
        public void SwitchingRenderProfileKeepsExplicitRenderResolutionOverride()
        {
            RuntimeRenderResolutionSettings.SetMode(RuntimeRenderResolutionMode.Low, persist: false);

            var quality = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Quality, persist: false);
            Assert.AreEqual(RuntimeRenderResolutionMode.Low, RuntimeRenderResolutionSettings.CurrentMode);
            Assert.AreEqual(0.5f, ReadFloat(quality.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);

            var cool = RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Cool, persist: false);
            Assert.AreEqual(RuntimeRenderResolutionMode.Low, RuntimeRenderResolutionSettings.CurrentMode);
            Assert.AreEqual(0.5f, ReadFloat(cool.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);
        }

        [Test]
        public void DesktopPlatformPolishUsesSelectedRuntimeProfile()
        {
            var profile = ScriptableObject.CreateInstance<PlatformPolishProfileDefinition>();
            try
            {
                profile.Configure(
                    PlatformPresentationMode.WindowsStandard3D,
                    1f,
                    Vector3.zero,
                    Vector3.zero,
                    50f,
                    0.03f,
                    80f,
                    Color.black,
                    Color.gray,
                    120,
                    0,
                    1f,
                    false,
                    0f,
                    0f);

                RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Cool, persist: false);
                var applierObject = new GameObject("RuntimeRenderProfilePlatformPolish");
                try
                {
                    var applier = applierObject.AddComponent<PlatformPolishApplier>();
                    applier.Configure(profile);

                    applier.Apply(null, null);

                    Assert.AreEqual(60, Application.targetFrameRate);
                    Assert.AreEqual(0.75f, ReadFloat(RuntimeRenderProfileSettings.CurrentProfile.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);
                }
                finally
                {
                    Object.DestroyImmediate(applierObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        private void CapturePipelineMembers(RenderPipelineAsset pipeline)
        {
            if (pipeline == null)
            {
                return;
            }

            Capture(pipeline, "renderScale", "m_RenderScale");
            Capture(pipeline, "supportsHDR", "m_SupportsHDR");
            Capture(pipeline, "supportsCameraDepthTexture", "m_RequireDepthTexture");
            Capture(pipeline, "supportsCameraOpaqueTexture", "m_RequireOpaqueTexture");
            Capture(pipeline, "mainLightShadowmapResolution", "m_MainLightShadowmapResolution");
            Capture(pipeline, "shadowDistance", "m_ShadowDistance");
            Capture(pipeline, "shadowCascadeCount", "m_ShadowCascadeCount");
            Capture(pipeline, "supportsAdditionalLightShadows", "m_AdditionalLightShadowsSupported");
            Capture(pipeline, "maxAdditionalLights", "m_AdditionalLightsPerObjectLimit");
        }

        private void Capture(RenderPipelineAsset pipeline, string propertyName, string fieldName)
        {
            pipelineSnapshots.Add(new PipelineMemberSnapshot(pipeline, propertyName, fieldName, ReadMember(pipeline, propertyName, fieldName)));
        }

        private void RestorePipelineMembers()
        {
            for (var index = 0; index < pipelineSnapshots.Count; index++)
            {
                var snapshot = pipelineSnapshots[index];
                WriteMember(snapshot.Pipeline, snapshot.PropertyName, snapshot.FieldName, snapshot.Value);
            }
        }

        private static float ReadFloat(RenderPipelineAsset pipeline, string propertyName, string fieldName)
        {
            var value = ReadMember(pipeline, propertyName, fieldName);
            return value is float floatValue ? floatValue : Convert.ToSingle(value);
        }

        private static object ReadMember(object target, string propertyName, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            var type = target.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead)
            {
                return property.GetValue(target);
            }

            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(target) : null;
        }

        private static void WriteMember(object target, string propertyName, string fieldName, object value)
        {
            if (target == null || value == null)
            {
                return;
            }

            var type = target.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return;
            }

            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private readonly struct PipelineMemberSnapshot
        {
            public PipelineMemberSnapshot(RenderPipelineAsset pipeline, string propertyName, string fieldName, object value)
            {
                Pipeline = pipeline;
                PropertyName = propertyName;
                FieldName = fieldName;
                Value = value;
            }

            public RenderPipelineAsset Pipeline { get; }

            public string PropertyName { get; }

            public string FieldName { get; }

            public object Value { get; }
        }
    }
}
