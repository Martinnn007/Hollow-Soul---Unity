using Hollow.Performance;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone139LongRunSoakGateAssetGenerator
    {
        [MenuItem("Hollow/Performance/Run M139 Long-Run Soak Gate")]
        public static void RunSoakGate()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M139 Long-Run Soak Gate",
                    "Enter Play Mode, then run this menu item. The PlayMode smoke test can run the same gate without manual gameplay.",
                    "OK");
                return;
            }

            var existing = Object.FindFirstObjectByType<M139LongRunSoakEditorDriver>();
            if (existing != null)
            {
                Debug.Log("M139 long-run soak gate is already running.");
                return;
            }

            var runner = new GameObject("M139 Long-Run Soak Gate");
            runner.hideFlags = HideFlags.HideAndDontSave;
            runner.AddComponent<M139LongRunSoakEditorDriver>().Run();
        }

        private sealed class M139LongRunSoakEditorDriver : MonoBehaviour
        {
            public void Run()
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(M139LongRunSoakRunner.RunAllScenarios(
                    M139LongRunSoakOptions.FullGate(),
                    report =>
                    {
                        Debug.Log($"M139 long-run soak gate {(report.passed ? "PASSED" : "FAILED")}. Report: {M139LongRunSoakReportGenerator.DefaultMarkdownReportPath}");
                        Destroy(gameObject);
                    }));
            }
        }
    }
}
