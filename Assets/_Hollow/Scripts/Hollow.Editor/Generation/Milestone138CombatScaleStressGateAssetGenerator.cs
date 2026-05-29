using Hollow.Performance;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone138CombatScaleStressGateAssetGenerator
    {
        [MenuItem("Hollow/Performance/Run M138 Combat Scale Stress Gate")]
        public static void RunStressGate()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M138 Combat Scale Stress Gate",
                    "Enter Play Mode, then run this menu item. The automated PlayMode test can run the same gate without manual gameplay.",
                    "OK");
                return;
            }

            var existing = Object.FindFirstObjectByType<M138CombatScaleStressEditorDriver>();
            if (existing != null)
            {
                Debug.Log("M138 combat scale stress gate is already running.");
                return;
            }

            var runner = new GameObject("M138 Combat Scale Stress Gate");
            runner.hideFlags = HideFlags.HideAndDontSave;
            runner.AddComponent<M138CombatScaleStressEditorDriver>().Run();
        }

        private sealed class M138CombatScaleStressEditorDriver : MonoBehaviour
        {
            public void Run()
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(M138CombatScaleStressRunner.RunAllScenarios(
                    M138CombatScaleStressRunOptions.FullGate(),
                    report =>
                    {
                        Debug.Log($"M138 combat scale stress gate {(report.passed ? "PASSED" : "FAILED")}. Report: {M138CombatScaleStressReportGenerator.DefaultMarkdownReportPath}");
                        Destroy(gameObject);
                    }));
            }
        }
    }
}
