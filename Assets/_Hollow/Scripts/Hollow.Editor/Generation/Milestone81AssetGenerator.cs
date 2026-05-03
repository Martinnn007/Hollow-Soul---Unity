using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Hollow.Editor.Generation
{
    public static class Milestone81AssetGenerator
    {
        public const string ActionDirectory = "Assets/_Hollow/Data/EnemyActions/M81";
        public const string DocsPath = "Docs/Hollow_M81_Enemy_Action_Profiles_V2.md";
        public const string ReportPath = "output/reports/m81_enemy_action_profiles_v2.md";
        public const string PdfPath = "output/pdf/Hollow_M81_Enemy_Action_Profiles_V2.pdf";
        public const string GeneratorScriptPath = "tools/generate_m81_enemy_action_profiles_v2_pdf.py";
        public const string VerifyScriptPath = "tools/verify_m81_enemy_action_profiles_v2_pdf.py";

        [MenuItem("Hollow/Generation/Generate Milestone 81 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(ActionDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var actions = GenerateActionProfiles();
            AssignEnemyActions(actions);
            AssignBossActions(actions);
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 81 enemy action profile assets.");
        }

        public static IReadOnlyList<EnemyActionProfileSpec> AllActionSpecs()
        {
            return EnemyActionProfileDefaults.AllSpecs;
        }

        private static Dictionary<string, EnemyActionProfileDefinition> GenerateActionProfiles()
        {
            var result = new Dictionary<string, EnemyActionProfileDefinition>();
            foreach (var spec in AllActionSpecs())
            {
                var path = $"{ActionDirectory}/{spec.AssetName}";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyActionProfileDefinition>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnemyActionProfileDefinition>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                profile.Configure(spec, LoadLinkedAttackAsset(spec));
                EditorUtility.SetDirty(profile);
                result[ProfileKey(spec.IsBoss, spec.OwnerId, spec.ActionId)] = profile;
            }

            return result;
        }

        private static void AssignEnemyActions(IReadOnlyDictionary<string, EnemyActionProfileDefinition> profiles)
        {
            foreach (var row in Milestone76AssetGenerator.EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                var assigned = EnemyActionProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == row.Key)
                    .Select(spec => profiles.TryGetValue(ProfileKey(false, spec.OwnerId, spec.ActionId), out var profile) ? profile : null)
                    .Where(profile => profile != null)
                    .ToArray();
                enemy.ConfigureActionProfiles(assigned);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void AssignBossActions(IReadOnlyDictionary<string, EnemyActionProfileDefinition> profiles)
        {
            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    continue;
                }

                var assigned = EnemyActionProfileDefaults.AllBossSpecs
                    .Where(spec => spec.OwnerId == boss.BossId)
                    .Select(spec => profiles.TryGetValue(ProfileKey(true, spec.OwnerId, spec.ActionId), out var profile) ? profile : null)
                    .Where(profile => profile != null)
                    .ToArray();
                boss.ConfigureActionProfiles(assigned);
                EditorUtility.SetDirty(boss);
            }
        }

        private static EnemyAttackProfileDefinition LoadLinkedAttackAsset(EnemyActionProfileSpec spec)
        {
            if (!spec.HasLinkedAttack)
            {
                return null;
            }

            var attackSpec = (spec.IsBoss ? EnemyAttackProfileDefaults.AllBossSpecs : EnemyAttackProfileDefaults.AllEnemySpecs)
                .FirstOrDefault(candidate => candidate.OwnerId == spec.OwnerId && candidate.AttackId == spec.LinkedAttackId);
            if (string.IsNullOrWhiteSpace(attackSpec.AttackId))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<EnemyAttackProfileDefinition>($"{Milestone76AssetGenerator.AttackDirectory}/{attackSpec.AssetName}");
        }

        private static string ProfileKey(bool isBoss, string ownerId, string actionId)
        {
            return $"{(isBoss ? "boss" : "enemy")}:{ownerId}:{actionId}";
        }

        private static void GeneratePdfWithReportLab()
        {
            if (!File.Exists(GeneratorScriptPath))
            {
                Debug.LogWarning($"M81 PDF generator script not found at {GeneratorScriptPath}.");
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = GeneratorScriptPath,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Debug.LogWarning("M81 PDF generation did not start.");
                    return;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log(string.IsNullOrWhiteSpace(output) ? $"Generated {PdfPath}." : output.Trim());
                    return;
                }

                Debug.LogWarning($"M81 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M81 PDF generation skipped: {exception.Message}");
            }
        }
    }
}
