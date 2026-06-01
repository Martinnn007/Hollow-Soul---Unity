using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hollow.Performance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Build
{
    [InitializeOnLoad]
    public static class AutomatedTruthGateRunner
    {
        public const string GameWindowsScenePath = "Assets/_Hollow/Scenes/Game_Windows.unity";

        private const string PendingModeKey = "Hollow.AutomatedTruthGate.PendingMode";
        private const string ReturnScenePathKey = "Hollow.AutomatedTruthGate.ReturnScenePath";
        private const string ExitPlayOnCompleteKey = "Hollow.AutomatedTruthGate.ExitPlayOnComplete";
        private const string ExitEditorOnCompleteKey = "Hollow.AutomatedTruthGate.ExitEditorOnComplete";
        private const string LastResultKey = "Hollow.AutomatedTruthGate.LastResult";

        static AutomatedTruthGateRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Hollow/Performance/Run Automated Truth Gate Smoke")]
        public static void RunSmokeMenu()
        {
            RunPlayModeGate(AutomatedTruthGateOptions.SmokeGate(), exitEditorOnComplete: false);
        }

        [MenuItem("Hollow/Performance/Run Automated Truth Gate Full")]
        public static void RunFullMenu()
        {
            RunPlayModeGate(AutomatedTruthGateOptions.FullGate(), exitEditorOnComplete: false);
        }

        [MenuItem("Hollow/Performance/Run Automated Truth Gate Built Player")]
        public static void RunBuiltPlayerMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Automated Truth Gate",
                    "Stop Play Mode before running the built-player truth gate. M140 builds and launches a standalone player from Edit Mode.",
                    "OK");
                return;
            }

            var report = RunBuiltPlayerGate();
            LogReport(report);
        }

        public static void RunBatchSmoke()
        {
            RunPlayModeGate(AutomatedTruthGateOptions.SmokeGate(), exitEditorOnComplete: true);
        }

        public static void RunBatchFull()
        {
            RunPlayModeGate(AutomatedTruthGateOptions.FullGate(), exitEditorOnComplete: true);
        }

        public static void RunBatchBuiltPlayer()
        {
            var report = RunBuiltPlayerGate();
            LogReport(report);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report != null && report.passed ? 0 : 1);
            }
        }

        public static AutomatedTruthGateReport RunBuiltPlayerGate()
        {
            var options = AutomatedTruthGateOptions.BuiltPlayerGate();
            var stopwatch = Stopwatch.StartNew();
            var m140Report = M140BuildRealGateRunner.RunMacOSAppleSiliconGate();
            stopwatch.Stop();

            var reportRoot = string.IsNullOrWhiteSpace(m140Report?.reportRoot)
                ? M140BuildRealReportGenerator.DefaultReportDirectory
                : m140Report.reportRoot;
            var editorJson = Path.Combine(reportRoot, M140BuildRealGateRunner.LatestEditorJsonFileName);
            var editorMarkdown = Path.Combine(reportRoot, M140BuildRealGateRunner.LatestEditorMarkdownFileName);
            var scenarioCount = m140Report?.playerReports?.Where(report => report != null).Sum(report => report.scenarioCount) ?? 0;
            var stage = AutomatedTruthGateReportGenerator.FromM140EditorResult(
                m140Report?.result ?? M140GateResult.Failed,
                scenarioCount,
                m140Report?.failures ?? new[] { "M140 macOS Apple Silicon gate did not produce an editor report." },
                editorJson,
                editorMarkdown,
                stopwatch.Elapsed.TotalMilliseconds);
            var report = AutomatedTruthGateReportGenerator.BuildReport(options, new[] { stage });
            AutomatedTruthGateReportGenerator.WriteReport(report, options.jsonReportPath, options.markdownReportPath);
            return report;
        }

        private static void RunPlayModeGate(AutomatedTruthGateOptions options, bool exitEditorOnComplete)
        {
            options ??= AutomatedTruthGateOptions.SmokeGate();
            if (EditorApplication.isPlaying)
            {
                StartPlayModeDriver(options, exitPlayOnComplete: false, exitEditorOnComplete: exitEditorOnComplete);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Automated truth gate is waiting for the current Play Mode transition to finish.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (Application.isBatchMode)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            var currentScenePath = SceneManager.GetActiveScene().path;
            SessionState.SetString(ReturnScenePathKey, currentScenePath ?? string.Empty);
            SessionState.SetString(PendingModeKey, options.mode ?? AutomatedTruthGateMode.Smoke);
            SessionState.SetBool(ExitPlayOnCompleteKey, true);
            SessionState.SetBool(ExitEditorOnCompleteKey, exitEditorOnComplete);
            SessionState.SetString(LastResultKey, string.Empty);

            EditorSceneManager.OpenScene(GameWindowsScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var pendingMode = SessionState.GetString(PendingModeKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(pendingMode))
                {
                    SessionState.SetString(PendingModeKey, string.Empty);
                    var options = string.Equals(pendingMode, AutomatedTruthGateMode.Full, StringComparison.OrdinalIgnoreCase)
                        ? AutomatedTruthGateOptions.FullGate()
                        : AutomatedTruthGateOptions.SmokeGate();
                    StartPlayModeDriver(
                        options,
                        SessionState.GetBool(ExitPlayOnCompleteKey, false),
                        SessionState.GetBool(ExitEditorOnCompleteKey, false));
                }

                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            var returnScenePath = SessionState.GetString(ReturnScenePathKey, string.Empty);
            SessionState.SetString(ReturnScenePathKey, string.Empty);
            SessionState.SetBool(ExitPlayOnCompleteKey, false);
            if (!string.IsNullOrWhiteSpace(returnScenePath) &&
                File.Exists(returnScenePath) &&
                SceneManager.GetActiveScene().path != returnScenePath)
            {
                EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            }

            if (SessionState.GetBool(ExitEditorOnCompleteKey, false))
            {
                SessionState.SetBool(ExitEditorOnCompleteKey, false);
                var result = SessionState.GetString(LastResultKey, M140GateResult.Failed);
                EditorApplication.Exit(string.Equals(result, M140GateResult.Passed, StringComparison.Ordinal) ? 0 : 1);
            }
        }

        private static void StartPlayModeDriver(AutomatedTruthGateOptions options, bool exitPlayOnComplete, bool exitEditorOnComplete)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Automated truth gate PlayMode driver can only start in Play Mode.");
                return;
            }

            var existing = Object.FindAnyObjectByType<AutomatedTruthGatePlayModeDriver>();
            if (existing != null)
            {
                Debug.Log("Automated truth gate is already running.");
                return;
            }

            var runner = new GameObject("Automated Truth Gate");
            runner.hideFlags = HideFlags.HideAndDontSave;
            runner.AddComponent<AutomatedTruthGatePlayModeDriver>().Run(options, exitPlayOnComplete, exitEditorOnComplete);
        }

        private static void LogReport(AutomatedTruthGateReport report)
        {
            var markdownPath = AutomatedTruthGateReportGenerator.MarkdownReportPathForMode(report?.mode ?? AutomatedTruthGateMode.Smoke);
            var message = $"Automated truth gate {report?.result ?? "NotRun"}. Report: {markdownPath}";
            if (report != null && report.passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        private sealed class AutomatedTruthGatePlayModeDriver : MonoBehaviour
        {
            private bool running;

            public void Run(AutomatedTruthGateOptions options, bool exitPlayOnComplete, bool exitEditorOnComplete)
            {
                if (running)
                {
                    return;
                }

                running = true;
                DontDestroyOnLoad(gameObject);
                StartCoroutine(RunRoutine(options, exitPlayOnComplete, exitEditorOnComplete));
            }

            private IEnumerator RunRoutine(AutomatedTruthGateOptions options, bool exitPlayOnComplete, bool exitEditorOnComplete)
            {
                options ??= AutomatedTruthGateOptions.SmokeGate();
                AutomatedTruthGateReport combined = null;
                yield return AutomatedTruthGatePlayModeRunner.Run(options, next => combined = next);

                SessionState.SetString(LastResultKey, combined?.result ?? M140GateResult.Failed);
                LogReport(combined);
                running = false;
                if (exitPlayOnComplete || exitEditorOnComplete)
                {
                    EditorApplication.isPlaying = false;
                }

                Destroy(gameObject);
            }
        }
    }
}
