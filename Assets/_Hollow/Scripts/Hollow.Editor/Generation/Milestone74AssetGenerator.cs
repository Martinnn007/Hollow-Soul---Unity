using System.Collections.Generic;
using System.IO;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone74AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M74_Movement_Intent_V2.md";
        public const string ReportPath = "output/reports/m74_movement_intent_v2.md";
        public const string PdfPath = "output/pdf/Hollow_M74_Movement_Intent_V2.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 74 Assets")]
        public static void Generate()
        {
            Milestone72AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            RetuneEnemyMovementRanges();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 74 movement intent assets.");
        }

        public static IReadOnlyList<(string FileName, string Name, float Min, float Max)> EnemyRows()
        {
            return new[]
            {
                ("Enemy_Normal.asset", "Normal Chaser", 1.05f, 1.75f),
                ("Enemy_Flying.asset", "Flying Chaser", 2.75f, 4.25f),
                ("Enemy_Fast.asset", "Fast Chaser", 0.9f, 1.45f),
                ("Enemy_Heavy.asset", "Heavy Chaser", 1.35f, 2.15f),
                ("Enemy_Charger.asset", "Ash Charger", 0.8f, 1.35f),
                ("Enemy_Turret.asset", "Bone Turret", 5.25f, 7.5f),
                ("Enemy_Splitter.asset", "Husk Splitter", 1.25f, 2f),
                ("Enemy_Boss.asset", "Stone Warden Spawn", 4.5f, 6.5f)
            };
        }

        private static void RetuneEnemyMovementRanges()
        {
            foreach (var row in EnemyRows())
            {
                RetuneEnemyRange(row.FileName, row.Min, row.Max);
            }
        }

        private static void RetuneEnemyRange(string fileName, float preferredMin, float preferredMax)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Hollow/Data/Enemies/{fileName}");
            if (enemy == null)
            {
                return;
            }

            enemy.Configure(
                enemy.SpawnKind,
                enemy.DisplayName,
                enemy.ArchetypeId,
                enemy.BehaviorId,
                enemy.MovementMode,
                enemy.MaxHealth,
                enemy.SpeedMetersPerSecond,
                enemy.ContactDamage,
                enemy.ContactCooldownSeconds,
                enemy.RadiusMeters,
                enemy.AttackRangeMeters,
                enemy.AttackCooldownSeconds,
                enemy.ProjectileDamage,
                enemy.ProjectileSpeedMetersPerSecond,
                enemy.ChargeSpeedMetersPerSecond,
                enemy.ChargeCooldownSeconds,
                enemy.SplitSpawnKind,
                enemy.SplitCount,
                enemy.BodyClass,
                enemy.Intelligence,
                enemy.Disposition,
                preferredMin,
                preferredMax,
                enemy.Color);
            EditorUtility.SetDirty(enemy);
        }

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M74: Movement Intent V2

M74 adds authored preferred range bands and a lightweight local steering pass for Hollow's non-boss enemy movement. The feature sharpens roster identity while staying deliberately local: no pathfinding, no line of sight, no squad tactics, no home leash system, and no boss behavior changes.

## Runtime Contract

- Preferred range steering applies only during ordinary chase, wander, and hold movement.
- Windups, active charges, stun, death, entry grace, ranged attacks, contact damage, and boss behavior remain unchanged.
- Intelligence controls precision: Instinctive enemies use bands mainly for prey retreat and anti-shove smoothing, Simple enemies are loose, and Basic or higher enemies respect bands more cleanly.
- separation is a soft nudge away from nearby living non-boss enemies.
- Player contact smoothing uses a small contact buffer so enemies stop constantly shoving into the player while still allowing hits and brief overlaps.
- Retreat behavior uses short readable bursts of about 0.75 seconds, then reassesses.

## Current Roster Range Table

| Enemy | Preferred Min | Preferred Max | Notes |
|---|---:|---:|---|
| Normal Chaser | 1.05m | 1.75m | Loose direct pressure. |
| Flying Chaser | 2.75m | 4.25m | Prey retreat and wander band. |
| Fast Chaser | 0.90m | 1.45m | Close fast pressure. |
| Heavy Chaser | 1.35m | 2.15m | Slower mindless pressure with more body room. |
| Ash Charger | 0.80m | 1.35m | Instinctive predator; charge behavior unchanged. |
| Bone Turret | 5.25m | 7.50m | Stationary data envelope only. |
| Husk Splitter | 1.25m | 2.00m | Basic predator spacing. |
| Stone Warden Spawn | 4.50m | 6.50m | Data completeness only; boss behavior unchanged. |

## Deferred Work

- Home leash behavior is intentionally deferred.
- Obstacle steering, pathfinding, line of sight, and squad coordination are outside M74.
- Future movement milestones can add authored leash clarity only after this local steering layer is stable.
");
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M74 Movement Intent V2 Report

- Movement range bands authored on `EnemyDefinition`.
- Runtime steering: preferred range, soft enemy separation, contact-buffer smoothing, and capped prey retreat bursts.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Save policy: no schema change; Continue derives movement bands from the current enemy catalog.
- Boss policy: boss runtime behavior remains unchanged.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            const string scriptPath = "tools/generate_m74_movement_intent_pdf.py";
            if (!File.Exists(scriptPath))
            {
                Debug.LogWarning($"M74 PDF generator script not found at {scriptPath}.");
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
                    Debug.LogWarning("M74 PDF generation did not start.");
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

                Debug.LogWarning($"M74 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M74 PDF generation skipped: {exception.Message}");
            }
        }
    }
}
