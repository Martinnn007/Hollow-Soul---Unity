using System.Collections.Generic;
using System.IO;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone75AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M75_Enemy_Attacks_And_Senses.md";
        public const string ReportPath = "output/reports/m75_enemy_attacks_and_senses.md";
        public const string PdfPath = "output/pdf/Hollow_M75_Enemy_Attacks_And_Senses.pdf";
        public const float DefaultLungeWindupSeconds = 0.22f;
        public const float DefaultLungeActiveSeconds = 0.18f;
        public const float DefaultLungeDistanceMeters = 0.75f;
        public const float DefaultLungeCooldownSeconds = 1.15f;

        [MenuItem("Hollow/Generation/Generate Milestone 75 Assets")]
        public static void Generate()
        {
            Milestone74AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            RetuneEnemyAttacksAndSenses();
            RetuneBossSenseMetadata();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 75 enemy attacks and senses assets.");
        }

        public static IReadOnlyList<(string FileName, string Name, float Sight, float Angle, float Hearing, bool LungeEnabled, float LungeTrigger)> EnemyRows()
        {
            return new[]
            {
                ("Enemy_Normal.asset", "Normal Chaser", 6.5f, 150f, 4.5f, true, 1.4f),
                ("Enemy_Flying.asset", "Flying Chaser", 7.5f, 240f, 6.5f, true, 1.35f),
                ("Enemy_Fast.asset", "Fast Chaser", 7f, 170f, 5f, true, 1.25f),
                ("Enemy_Heavy.asset", "Heavy Chaser", 5f, 110f, 3.5f, true, 1.7f),
                ("Enemy_Charger.asset", "Ash Charger", 7f, 120f, 5f, false, 1.4f),
                ("Enemy_Turret.asset", "Bone Turret", 9.5f, 70f, 2.5f, false, 1.4f),
                ("Enemy_Splitter.asset", "Husk Splitter", 6.5f, 160f, 5f, true, 1.6f),
                ("Enemy_Boss.asset", "Stone Warden Spawn", 8f, 160f, 4.5f, false, 1.4f)
            };
        }

        public static IReadOnlyList<(string FileName, string Name, float Sight, float Angle, float Hearing)> BossRows()
        {
            return new[]
            {
                ("Boss_stone_warden.asset", "Stone Warden", 8f, 140f, 5f),
                ("Boss_splinter_saint.asset", "Splinter Saint", 8f, 180f, 5.5f),
                ("Boss_gravel_maw.asset", "Gravel Maw", 6.5f, 110f, 6f),
                ("Boss_cartouche_widow.asset", "Cartouche Widow", 10f, 220f, 6.5f),
                ("Boss_iron_reliquary.asset", "Iron Reliquary", 8.5f, 120f, 4f),
                ("Boss_mirror_husk.asset", "Mirror Husk", 9f, 220f, 6f),
                ("Boss_ash_comet.asset", "Ash Comet", 9f, 160f, 7f),
                ("Boss_choir_of_teeth.asset", "Choir of Teeth", 10f, 300f, 7f),
                ("Boss_rust_bishop.asset", "Rust Bishop", 9.5f, 180f, 5.5f),
                ("Boss_hollow_star_larva.asset", "Hollow Star Larva", 0f, 0f, 9.5f)
            };
        }

        private static void RetuneEnemyAttacksAndSenses()
        {
            foreach (var row in EnemyRows())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{row.FileName}");
                if (enemy == null)
                {
                    continue;
                }

                enemy.ConfigureSenseAndLunge(
                    row.Sight,
                    row.Angle,
                    row.Hearing,
                    row.LungeEnabled,
                    row.LungeTrigger,
                    DefaultLungeWindupSeconds,
                    DefaultLungeActiveSeconds,
                    DefaultLungeDistanceMeters,
                    DefaultLungeCooldownSeconds);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void RetuneBossSenseMetadata()
        {
            foreach (var row in BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    continue;
                }

                boss.ConfigureSenseMetadata(row.Sight, row.Angle, row.Hearing);
                EditorUtility.SetDirty(boss);
            }
        }

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M75: Enemy Attacks + Senses V1

M75 gives every contact-capable enemy a short authored lunge attack and adds local senses that drive lightweight awareness. The intent is to fix preferred-range stalls without adding pathfinding, obstacle line of sight, alert sharing, saved awareness state, or boss behavior changes.

## Runtime Contract

- Enemy Attacks: Normal, Fast, Heavy, Flying, and Husk Splitter can start a melee lunge from the edge of their preferred range.
- Ash Charger keeps its charge attack. Bone Turret stays stationary and ranged-only. Boss runtime behavior remains unchanged.
- Senses: sight uses radius plus cone angle only, and hearing uses simple local stimulus radius checks.
- Awareness: enemies move through Unaware, Suspicious, Alerted, and Engaged. Once Engaged, they stay Engaged until death, room reset, or Continue.
- Stimuli: footsteps can raise suspicion, player attacks force engagement inside hearing range, and direct damage always engages the target.
- Budgets: melee lunges use a separate 0.30s room budget. Ranged and charge attacks keep the existing M72 pressure budget where only Tactical and Cunning gain priority bonuses.

## Current Roster Sense And Lunge Table

| Enemy | Sight | Cone | Hearing | Lunge |
|---|---:|---:|---:|---|
| Normal Chaser | 6.5m | 150deg | 4.5m | yes, 1.40m |
| Flying Chaser | 7.5m | 240deg | 6.5m | yes, 1.35m, endangered or engaged |
| Fast Chaser | 7.0m | 170deg | 5.0m | yes, 1.25m |
| Heavy Chaser | 5.0m | 110deg | 3.5m | yes, 1.70m |
| Ash Charger | 7.0m | 120deg | 5.0m | no, charge attack only |
| Bone Turret | 9.5m | 70deg | 2.5m | no, ranged-only sentinel |
| Husk Splitter | 6.5m | 160deg | 5.0m | yes, 1.60m |
| Stone Warden Spawn | 8.0m | 160deg | 4.5m | no, data completeness |

## Current Boss Sense Metadata

| Boss | Sight | Cone | Hearing | Runtime Policy |
|---|---:|---:|---:|---|
| Stone Warden | 8.0m | 140deg | 5.0m | metadata only |
| Splinter Saint | 8.0m | 180deg | 5.5m | metadata only |
| Gravel Maw | 6.5m | 110deg | 6.0m | metadata only |
| Cartouche Widow | 10.0m | 220deg | 6.5m | metadata only |
| Iron Reliquary | 8.5m | 120deg | 4.0m | metadata only |
| Mirror Husk | 9.0m | 220deg | 6.0m | metadata only |
| Ash Comet | 9.0m | 160deg | 7.0m | metadata only |
| Choir of Teeth | 10.0m | 300deg | 7.0m | metadata only |
| Rust Bishop | 9.5m | 180deg | 5.5m | metadata only |
| Hollow Star Larva | 0.0m | 0deg | 9.5m | metadata only, blind hearing-forward profile |

## Deferred Work

- No pathfinding, obstacle line of sight, squad tactics, stealth UI, or alert sharing.
- Awareness timers and stimuli reset on Continue; authored senses and lunge values come from the current catalog.
- Future milestones can add richer investigation, blind/deaf enemy variants, leash tuning, and authored attack suites once this V1 contract is stable.
");
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M75 Enemy Attacks And Senses Report

- Sense metadata added to `EnemyDefinition` and boss metadata.
- Awareness runtime: Unaware, Suspicious, Alerted, Engaged.
- Contact-capable enemies now have short lunge attacks that can break preferred range.
- Melee lunge pressure uses a separate `{RoomCombatController.EnemyMeleeAttackBudgetWindowSeconds:0.00}s` budget window.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Boss policy: boss runtime behavior remains unchanged.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            const string scriptPath = "tools/generate_m75_enemy_attacks_and_senses_pdf.py";
            if (!File.Exists(scriptPath))
            {
                Debug.LogWarning($"M75 PDF generator script not found at {scriptPath}.");
                return;
            }

            try
            {
                var startInfo = new DiagnosticsProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = scriptPath,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = DiagnosticsProcess.Start(startInfo);
                if (process == null)
                {
                    Debug.LogWarning("M75 PDF generation did not start.");
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

                Debug.LogWarning($"M75 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M75 PDF generation skipped: {exception.Message}");
            }
        }
    }
}
