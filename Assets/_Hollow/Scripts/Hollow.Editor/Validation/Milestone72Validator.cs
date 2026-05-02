using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone72Validator
    {
        private static readonly Dictionary<string, (EnemyIntelligenceLevel Intelligence, EnemyInstinctDisposition Disposition)> EnemyExpectations = new()
        {
            ["Enemy_Normal.asset"] = (EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator),
            ["Enemy_Flying.asset"] = (EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Prey),
            ["Enemy_Fast.asset"] = (EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator),
            ["Enemy_Heavy.asset"] = (EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Mindless),
            ["Enemy_Charger.asset"] = (EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Predator),
            ["Enemy_Turret.asset"] = (EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel),
            ["Enemy_Splitter.asset"] = (EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator),
            ["Enemy_Boss.asset"] = (EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel)
        };

        [MenuItem("Hollow/Validation/Run Milestone 72 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateEnemyMetadata(failures);
            ValidateBossMetadata(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 72 validation passed.");
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
            ExpectFile("Assets/_Hollow/Scripts/Hollow.Combat/EnemyIntelligenceLevel.cs", failures);
            ExpectFile("Assets/_Hollow/Scripts/Hollow.Combat/EnemyInstinctDisposition.cs", failures);
            ExpectFile(Milestone72AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone72AssetGenerator.PdfPath, failures);
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M72 file: {path}");
            }
        }

        private static void ValidateEnemyMetadata(List<string> failures)
        {
            foreach (var expectation in EnemyExpectations)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{expectation.Key}");
                if (enemy == null)
                {
                    failures.Add($"Missing M72 enemy asset: {expectation.Key}");
                    continue;
                }

                if (enemy.Intelligence != expectation.Value.Intelligence || enemy.Disposition != expectation.Value.Disposition)
                {
                    failures.Add($"{enemy.SpawnKind} should be {expectation.Value.Intelligence.DisplayLabel()} / {expectation.Value.Disposition.ToSaveString()}.");
                }
            }
        }

        private static void ValidateBossMetadata(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            if (catalog == null)
            {
                failures.Add("M72 requires the M53 boss catalog.");
                return;
            }

            foreach (var boss in catalog.Bosses.Where(boss => boss != null))
            {
                var expected = BossDefinition.SignatureIntelligenceFor(boss.BehaviorId);
                if (boss.Intelligence != expected)
                {
                    failures.Add($"{boss.BossId} should be {expected.DisplayLabel()} intelligence metadata.");
                }
            }
        }
    }
}
