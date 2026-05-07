using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone108DesignerDebuggingPassValidator
    {
        private static readonly string[] RequiredDocsText =
        {
            "Designer Debugging Pass",
            "NavMesh path",
            "tactical",
            "Behavior graph",
            "chosen command",
            "blocked",
            "awareness",
            "active attack window"
        };

        [MenuItem("Hollow/Validation/Run Milestone 108 Designer Debugging Pass Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateOverlayContract(failures);
            ValidateArtifacts(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 108 Designer Debugging Pass validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateOverlayContract(List<string> failures)
        {
            EnemyDesignerDebugOverlay.ResetDiagnostics();
            EnemyDesignerDebugOverlay.SetEnabled(true);
            var enemyObject = new GameObject("M108DesignerDebugEnemy");
            try
            {
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                var text = EnemyDesignerDebugOverlay.BuildOverlayText(enemy);
                ExpectText(text, "State", failures);
                ExpectText(text, "Action", failures);
                ExpectText(text, "Tactical", failures);
                ExpectText(text, "Nav", failures);
                ExpectText(text, "Blocked", failures);
                ExpectText(text, "BT", failures);
                ExpectText(EnemyDesignerDebugOverlay.DiagnosticsSummary, "Designer Debug active enemies", failures);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                EnemyDesignerDebugOverlay.SetEnabled(false);
            }
        }

        private static void ValidateArtifacts(List<string> failures)
        {
            ExpectFile(Milestone108DesignerDebuggingPassAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone108DesignerDebuggingPassAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone108DesignerDebuggingPassAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone108DesignerDebuggingPassAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                ExpectText(docs, required, failures);
            }
        }

        private static void ExpectText(string value, string required, List<string> failures)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !value.Contains(required, System.StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"M108 expected text `{required}`.");
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M108 artifact `{path}`.");
            }
        }
    }
}
