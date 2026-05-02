using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone74Validator
    {
        [MenuItem("Hollow/Validation/Run Milestone 74 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemyRanges(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 74 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone74AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone74AssetGenerator.PdfPath, failures);
            ExpectFile("tools/generate_m74_movement_intent_pdf.py", failures);
            ExpectFile("tools/verify_m74_movement_intent_pdf.py", failures);
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M74 file: {path}");
            }
        }

        private static void ValidateEnemyRanges(List<string> failures)
        {
            foreach (var row in Milestone74AssetGenerator.EnemyRows())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{row.FileName}");
                if (enemy == null)
                {
                    failures.Add($"Missing M74 enemy asset: {row.FileName}");
                    continue;
                }

                if (!Approximately(enemy.PreferredRangeMinMeters, row.Min) ||
                    !Approximately(enemy.PreferredRangeMaxMeters, row.Max))
                {
                    failures.Add($"{enemy.SpawnKind} should use preferred range {row.Min:0.##}m-{row.Max:0.##}m.");
                }
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.001f;
        }
    }
}
