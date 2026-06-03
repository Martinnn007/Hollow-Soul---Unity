using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hollow.Input;
using Hollow.Persistence;
using Hollow.Presentation;
using Hollow.UI.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class PauseMenuFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            GameplayPauseState.SetPaused(false);
            Time.timeScale = 1f;
        }

        [Test]
        public void GameplayInputSnapshotCarriesPausePressed()
        {
            var snapshot = new GameplayInputSnapshot(
                Vector2.zero,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: true);

            Assert.IsTrue(snapshot.PausePressed);
        }

        [Test]
        public void PlatformShellAddsPauseMenuController()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var controller = shell.AddComponent<PlatformShellController>();

                controller.ApplyConfiguration();

                Assert.IsNotNull(shell.GetComponent<PauseMenuController>());
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void PauseMenuFreezesAndRestoresGameplayTime()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var pause = shell.AddComponent<PauseMenuController>();
                Time.timeScale = 1f;

                pause.ShowRoot();

                Assert.AreEqual(PauseMenuState.Root, pause.State);
                Assert.IsTrue(GameplayPauseState.IsPaused);
                Assert.AreEqual(0f, Time.timeScale);

                pause.Resume();

                Assert.AreEqual(PauseMenuState.Hidden, pause.State);
                Assert.IsFalse(GameplayPauseState.IsPaused);
                Assert.AreEqual(1f, Time.timeScale);
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void ControlsPanelShowsKeyboardAndDualShockReference()
        {
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var pause = shell.AddComponent<PauseMenuController>();

                pause.ShowControls();

                var labels = shell.GetComponentsInChildren<Text>(includeInactive: true).Select(text => text.text).ToArray();
                Assert.Contains("Keyboard", labels);
                Assert.Contains("DualShock 5", labels);
                Assert.Contains("Pause: Escape", labels);
                Assert.Contains("Pause: Options", labels);
            }
            finally
            {
                Object.DestroyImmediate(shell);
            }
        }

        [Test]
        public void SettingsPanelSwitchesRuntimeRenderProfile()
        {
            var previousTargetFrameRate = Application.targetFrameRate;
            var previousVSyncCount = QualitySettings.vSyncCount;
            var previousPipeline = QualitySettings.renderPipeline;
            var hadPreviousPreference = PlayerPrefs.HasKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            var previousPreference = hadPreviousPreference ? PlayerPrefs.GetString(RuntimeRenderProfileSettings.PlayerPrefsKey) : string.Empty;
            var hadPreviousResolutionPreference = PlayerPrefs.HasKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
            var previousResolutionPreference = hadPreviousResolutionPreference ? PlayerPrefs.GetString(RuntimeRenderResolutionSettings.PlayerPrefsKey) : string.Empty;
            var pipelineSnapshots = CapturePipelineMembers(RuntimeRenderProfileSettings.ProfileFor(RuntimeRenderProfileMode.Cool)?.RenderPipelineAsset);
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
                PlayerPrefs.DeleteKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
                RuntimeRenderProfileSettings.ResetForTests();
                RuntimeRenderResolutionSettings.ResetForTests();
                var pause = shell.AddComponent<PauseMenuController>();

                pause.ShowSettings();

                var labels = shell.GetComponentsInChildren<Text>(includeInactive: true).Select(text => text.text).ToArray();
                Assert.Contains("Graphics", labels);
                Assert.Contains("Cool", labels);
                Assert.Contains("Quality", labels);
                Assert.Contains("Render Resolution", labels);
                Assert.Contains("Native", labels);
                Assert.Contains("Balanced", labels);
                Assert.Contains("Low", labels);
                Assert.AreEqual(RuntimeRenderProfileMode.Cool, RuntimeRenderProfileSettings.CurrentMode);

                FindButton(shell, "Quality").onClick.Invoke();

                Assert.AreEqual(RuntimeRenderProfileMode.Quality, RuntimeRenderProfileSettings.CurrentMode);
                RuntimeRenderProfileSettings.ResetForTests();
                Assert.AreEqual(RuntimeRenderProfileMode.Quality, RuntimeRenderProfileSettings.CurrentMode);
                labels = shell.GetComponentsInChildren<Text>(includeInactive: true).Select(text => text.text).ToArray();
                Assert.Contains("Cool", labels);
                Assert.Contains("Quality", labels);
            }
            finally
            {
                Object.DestroyImmediate(shell);
                RestorePipelineMembers(pipelineSnapshots);
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
            }
        }

        [Test]
        public void SettingsPanelSwitchesRuntimeRenderResolution()
        {
            var previousTargetFrameRate = Application.targetFrameRate;
            var previousVSyncCount = QualitySettings.vSyncCount;
            var previousPipeline = QualitySettings.renderPipeline;
            var hadPreviousPreference = PlayerPrefs.HasKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            var previousPreference = hadPreviousPreference ? PlayerPrefs.GetString(RuntimeRenderProfileSettings.PlayerPrefsKey) : string.Empty;
            var hadPreviousResolutionPreference = PlayerPrefs.HasKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
            var previousResolutionPreference = hadPreviousResolutionPreference ? PlayerPrefs.GetString(RuntimeRenderResolutionSettings.PlayerPrefsKey) : string.Empty;
            var pipelineSnapshots = CapturePipelineMembers(RuntimeRenderProfileSettings.ProfileFor(RuntimeRenderProfileMode.Cool)?.RenderPipelineAsset);
            var shell = new GameObject("PlatformShellCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
                PlayerPrefs.DeleteKey(RuntimeRenderResolutionSettings.PlayerPrefsKey);
                RuntimeRenderProfileSettings.ResetForTests();
                RuntimeRenderResolutionSettings.ResetForTests();
                var pause = shell.AddComponent<PauseMenuController>();

                pause.ShowSettings();

                Assert.AreEqual(RuntimeRenderResolutionMode.Balanced, RuntimeRenderResolutionSettings.CurrentMode);
                FindButton(shell, "Low").onClick.Invoke();
                Assert.AreEqual(RuntimeRenderResolutionMode.Low, RuntimeRenderResolutionSettings.CurrentMode);

                FindButton(shell, "Native").onClick.Invoke();
                Assert.AreEqual(RuntimeRenderResolutionMode.Native, RuntimeRenderResolutionSettings.CurrentMode);

                FindButton(shell, "Balanced").onClick.Invoke();
                Assert.AreEqual(RuntimeRenderResolutionMode.Balanced, RuntimeRenderResolutionSettings.CurrentMode);

                RuntimeRenderResolutionSettings.ResetForTests();
                Assert.AreEqual(RuntimeRenderResolutionMode.Balanced, RuntimeRenderResolutionSettings.CurrentMode);
                Assert.AreEqual(0.75f, ReadFloat(RuntimeRenderProfileSettings.CurrentProfile.RenderPipelineAsset, "renderScale", "m_RenderScale"), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(shell);
                RestorePipelineMembers(pipelineSnapshots);
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
            }
        }

        [Test]
        public void RunSnapshotCanPersistChallengeIdentity()
        {
            var snapshot = new RunSaveSnapshot
            {
                runId = "challenge-run",
                challengeId = "blade_trial"
            };

            Assert.AreEqual("blade_trial", snapshot.challengeId);
        }

        private static Button FindButton(GameObject root, string label)
        {
            var button = root.GetComponentsInChildren<Button>(includeInactive: true)
                .FirstOrDefault(candidate => candidate.GetComponentInChildren<Text>(includeInactive: true)?.text == label);
            Assert.NotNull(button, label);
            return button;
        }

        private static List<PipelineMemberSnapshot> CapturePipelineMembers(RenderPipelineAsset pipeline)
        {
            var snapshots = new List<PipelineMemberSnapshot>();
            if (pipeline == null)
            {
                return snapshots;
            }

            Capture(snapshots, pipeline, "renderScale", "m_RenderScale");
            Capture(snapshots, pipeline, "supportsHDR", "m_SupportsHDR");
            Capture(snapshots, pipeline, "supportsCameraDepthTexture", "m_RequireDepthTexture");
            Capture(snapshots, pipeline, "supportsCameraOpaqueTexture", "m_RequireOpaqueTexture");
            Capture(snapshots, pipeline, "mainLightShadowmapResolution", "m_MainLightShadowmapResolution");
            Capture(snapshots, pipeline, "shadowDistance", "m_ShadowDistance");
            Capture(snapshots, pipeline, "shadowCascadeCount", "m_ShadowCascadeCount");
            Capture(snapshots, pipeline, "supportsAdditionalLightShadows", "m_AdditionalLightShadowsSupported");
            Capture(snapshots, pipeline, "maxAdditionalLights", "m_AdditionalLightsPerObjectLimit");
            return snapshots;
        }

        private static void Capture(List<PipelineMemberSnapshot> snapshots, RenderPipelineAsset pipeline, string propertyName, string fieldName)
        {
            snapshots.Add(new PipelineMemberSnapshot(pipeline, propertyName, fieldName, ReadMember(pipeline, propertyName, fieldName)));
        }

        private static void RestorePipelineMembers(List<PipelineMemberSnapshot> snapshots)
        {
            for (var index = 0; index < snapshots.Count; index++)
            {
                var snapshot = snapshots[index];
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
