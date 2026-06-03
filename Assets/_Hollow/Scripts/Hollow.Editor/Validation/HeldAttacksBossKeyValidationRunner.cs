using System;
using System.IO;
using Hollow.Editor.Build;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class HeldAttacksBossKeyValidationRunner
    {
        private const string EditModeResultsPath = "output/reports/held-attacks-boss-key-editmode-results.xml";

        public static void RunEditModeTestsBatch()
        {
            var result = RunEditModeTests(EditModeResultsPath);
            Debug.Log(
                $"Held attacks + boss key EditMode tests completed: {result.passCount}/{result.totalCount} passed, " +
                $"{result.failCount} failed, {result.inconclusiveCount} inconclusive, {result.skipCount} skipped.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(result.Passed ? 0 : 1);
            }
        }

        public static WholeGameAuditRunner.FocusedAuditTestRunResult RunEditModeTests(string outputPath)
        {
            var absoluteOutputPath = Path.GetFullPath(outputPath ?? EditModeResultsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath) ?? "output/reports");

            var callbacks = ScriptableObject.CreateInstance<ValidationTestCallbacks>();
            callbacks.Configure(absoluteOutputPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(callbacks, priority: 1000);

            var previousExitSuppression = MilestoneValidationExitPolicy.SuppressEditorExit;
            MilestoneValidationExitPolicy.SuppressEditorExit = true;
            try
            {
                var settings = new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    assemblyNames = new[] { "Hollow.Tests.EditMode" }
                })
                {
                    runSynchronously = true
                };

                api.Execute(settings);
                return WholeGameAuditRunner.FocusedAuditTestRunResult.From(callbacks.Result, absoluteOutputPath);
            }
            finally
            {
                MilestoneValidationExitPolicy.SuppressEditorExit = previousExitSuppression;
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api);
                UnityEngine.Object.DestroyImmediate(callbacks);
            }
        }

        private sealed class ValidationTestCallbacks : ScriptableObject, ICallbacks
        {
            private string outputPath = string.Empty;

            public ITestResultAdaptor Result { get; private set; }

            public void Configure(string nextOutputPath)
            {
                outputPath = nextOutputPath ?? string.Empty;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Result = result;
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    TestRunnerApi.SaveResultToFile(result, outputPath);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
