using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone76AssetGenerator
    {
        public const string AttackDirectory = "Assets/_Hollow/Data/EnemyAttacks/M76";
        public const string DocsPath = "Docs/Hollow_M76_Enemy_Attack_Profiles.md";
        public const string ReportPath = "output/reports/m76_enemy_attack_profiles.md";
        public const string PdfPath = "output/pdf/Hollow_M76_Enemy_Attack_Profiles.pdf";

        [MenuItem("Hollow/Generation/Generate Milestone 76 Assets")]
        public static void Generate()
        {
            Milestone75AssetGenerator.Generate();
            Directory.CreateDirectory(AttackDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var profiles = GenerateProfiles();
            AssignEnemyProfiles(profiles);
            AssignBossProfiles(profiles);
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 76 enemy attack profile assets.");
        }

        public static IReadOnlyList<EnemyAttackProfileSpec> AllProfileSpecs()
        {
            return EnemyAttackProfileDefaults.AllEnemySpecs
                .Concat(EnemyAttackProfileDefaults.AllBossSpecs)
                .ToArray();
        }

        public static IReadOnlyDictionary<string, string> EnemyAssetPathsBySpawnKind()
        {
            return new Dictionary<string, string>
            {
                ["spawnEnemyNormal"] = "Assets/_Hollow/Data/Enemies/Enemy_Normal.asset",
                ["spawnEnemyFlying"] = "Assets/_Hollow/Data/Enemies/Enemy_Flying.asset",
                ["spawnEnemyFast"] = "Assets/_Hollow/Data/Enemies/Enemy_Fast.asset",
                ["spawnEnemyHeavy"] = "Assets/_Hollow/Data/Enemies/Enemy_Heavy.asset",
                ["spawnEnemyCharger"] = "Assets/_Hollow/Data/Enemies/Enemy_Charger.asset",
                ["spawnEnemyTurret"] = "Assets/_Hollow/Data/Enemies/Enemy_Turret.asset",
                ["spawnEnemySplitter"] = "Assets/_Hollow/Data/Enemies/Enemy_Splitter.asset",
                ["spawnEnemySpittingPod"] = "Assets/_Hollow/Data/Enemies/Enemy_SpittingPod.asset",
                ["spawnEnemyRat"] = "Assets/_Hollow/Data/Enemies/Enemy_Rat.asset",
                ["spawnEnemySpider"] = "Assets/_Hollow/Data/Enemies/Enemy_Spider.asset",
                ["spawnEnemySkeletonSword"] = "Assets/_Hollow/Data/Enemies/Enemy_SkeletonSword.asset",
                ["spawnEnemySkeletonSpear"] = "Assets/_Hollow/Data/Enemies/Enemy_SkeletonSpear.asset",
                ["spawnEnemyKnight"] = "Assets/_Hollow/Data/Enemies/Enemy_Knight.asset",
                ["spawnEnemyGiant"] = "Assets/_Hollow/Data/Enemies/Enemy_Giant.asset",
                ["spawnEnemyBoss"] = "Assets/_Hollow/Data/Enemies/Enemy_Boss.asset"
            };
        }

        private static Dictionary<string, EnemyAttackProfileDefinition> GenerateProfiles()
        {
            var result = new Dictionary<string, EnemyAttackProfileDefinition>();
            foreach (var spec in AllProfileSpecs())
            {
                var path = $"{AttackDirectory}/{spec.AssetName}";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyAttackProfileDefinition>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnemyAttackProfileDefinition>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                profile.Configure(spec);
                EditorUtility.SetDirty(profile);
                result[ProfileKey(spec.IsBoss, spec.OwnerId, spec.AttackId)] = profile;
            }

            return result;
        }

        private static void AssignEnemyProfiles(IReadOnlyDictionary<string, EnemyAttackProfileDefinition> profiles)
        {
            foreach (var row in EnemyAssetPathsBySpawnKind())
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(row.Value);
                if (enemy == null)
                {
                    continue;
                }

                var assigned = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == row.Key)
                    .Select(spec => profiles.TryGetValue(ProfileKey(false, spec.OwnerId, spec.AttackId), out var profile) ? profile : null)
                    .Where(profile => profile != null)
                    .ToArray();
                enemy.ConfigureAttackProfiles(assigned);
                EditorUtility.SetDirty(enemy);
            }
        }

        private static void AssignBossProfiles(IReadOnlyDictionary<string, EnemyAttackProfileDefinition> profiles)
        {
            foreach (var row in Milestone75AssetGenerator.BossRows())
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{Milestone53AssetGenerator.BossDirectory}/{row.FileName}");
                if (boss == null)
                {
                    continue;
                }

                var assigned = EnemyAttackProfileDefaults.AllBossSpecs
                    .Where(spec => spec.OwnerId == boss.BossId)
                    .Select(spec => profiles.TryGetValue(ProfileKey(true, spec.OwnerId, spec.AttackId), out var profile) ? profile : null)
                    .Where(profile => profile != null)
                    .ToArray();
                boss.ConfigureAttackProfiles(assigned);
                EditorUtility.SetDirty(boss);
            }
        }

        private static string ProfileKey(bool isBoss, string ownerId, string attackId)
        {
            return $"{(isBoss ? "boss" : "enemy")}:{ownerId}:{attackId}";
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M76: Enemy Attack Profiles + Impact Catalogue V1");
            builder.AppendLine();
            builder.AppendLine("M76 moves enemy and boss impact tuning into authored attack profile assets. Runtime behavior remains behavior-specific, but damage type, delivery, element, force class, knockback, guard recoil, cooldown, and range now come from attack profiles.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Contract");
            builder.AppendLine();
            builder.AppendLine("- Separate `EnemyAttackProfileDefinition` assets are the source of truth for attack impact data.");
            builder.AppendLine("- Runtime keeps existing enemy and boss behavior patterns while resolving contact, lunge, charge, projectile, split, and summon profiles by attack id.");
            builder.AppendLine("- `DamageClassification` remains the shared taxonomy: channel, delivery, element, and force class.");
            builder.AppendLine("- Guarded non-parry hits apply reduced recoil from `guardKnockbackMultiplier`; perfect parry prevents player recoil.");
            builder.AppendLine("- Existing `ActiveStability` thresholds still reduce or cancel received knockback after guard recoil is calculated.");
            builder.AppendLine("- Balance stays readability-first: average M75 damage and pressure should remain familiar.");
            builder.AppendLine();
            AppendProfileSection(builder, "Enemy Attack Profiles", EnemyAttackProfileDefaults.AllEnemySpecs);
            builder.AppendLine();
            AppendProfileSection(builder, "Boss Attack Profiles", EnemyAttackProfileDefaults.AllBossSpecs);
            builder.AppendLine();
            builder.AppendLine("## Compatibility");
            builder.AppendLine();
            builder.AppendLine("- No save schema change; attack profiles resolve from current catalog data on Continue.");
            builder.AppendLine("- No elemental resistance system is added in M76.");
            builder.AppendLine("- No generic attack planner is added; behavior-specific AI remains.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M76 Enemy Attack Profiles Report

- Added `{nameof(EnemyAttackProfileDefinition)}` as the authored impact source for enemy and boss attacks.
- Profile count: {AllProfileSpecs().Count}.
- Enemy owners: {EnemyAttackProfileDefaults.AllEnemySpecs.Select(spec => spec.OwnerId).Distinct().Count()}.
- Boss owners: {EnemyAttackProfileDefaults.AllBossSpecs.Select(spec => spec.OwnerId).Distinct().Count()}.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Runtime policy: behavior-specific AI remains; damage classification, force, knockback, and guard recoil resolve from attack profiles.
");
        }

        private static void AppendProfileSection(
            StringBuilder builder,
            string title,
            IEnumerable<EnemyAttackProfileSpec> specs)
        {
            builder.AppendLine($"## {title}");
            builder.AppendLine();
            builder.AppendLine("| Owner | Attack | Runtime | Classification | Force | Threat | Damage | Knockback | Guard Recoil | Cooldown | Range | Notes |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var spec in specs)
            {
                builder.AppendLine(
                    $"| {OwnerLabel(spec.OwnerId)} | {spec.DisplayName} | {spec.RuntimeKind} | {ClassificationLabel(spec)} | {spec.ForceClass} | {spec.ThreatKind} | {spec.Damage} | {spec.KnockbackMeters:0.00}m | x{spec.GuardKnockbackMultiplier:0.00} | {spec.CooldownSeconds:0.00}s | {spec.RangeMeters:0.00}m | {spec.Notes} |");
            }
        }

        private static string OwnerLabel(string ownerId)
        {
            return ownerId switch
            {
                "spawnEnemyNormal" => "Normal Chaser",
                "spawnEnemyFlying" => "Flying Chaser",
                "spawnEnemyFast" => "Fast Chaser",
                "spawnEnemyHeavy" => "Heavy Chaser",
                "spawnEnemyCharger" => "Ash Charger",
                "spawnEnemyTurret" => "Bone Turret",
                "spawnEnemySplitter" => "Husk Splitter",
                "spawnEnemySpittingPod" => "Spitting Pod",
                "spawnEnemyRat" => "Rat",
                "spawnEnemySpider" => "Spider",
                "spawnEnemySkeletonSword" => "Skeleton Sword",
                "spawnEnemySkeletonSpear" => "Skeleton Spear",
                "spawnEnemyKnight" => "Knight",
                "spawnEnemyGiant" => "Giant",
                "spawnEnemyBoss" => "Stone Warden Spawn",
                "stone_warden" => "Stone Warden",
                "splinter_saint" => "Splinter Saint",
                "gravel_maw" => "Gravel Maw",
                "cartouche_widow" => "Cartouche Widow",
                "iron_reliquary" => "Iron Reliquary",
                "mirror_husk" => "Mirror Husk",
                "ash_comet" => "Ash Comet",
                "choir_of_teeth" => "Choir of Teeth",
                "rust_bishop" => "Rust Bishop",
                "hollow_star_larva" => "Hollow Star Larva",
                _ => ownerId
            };
        }

        private static string ClassificationLabel(EnemyAttackProfileSpec spec)
        {
            return spec.DamageElement == DamageElement.None
                ? $"{spec.DamageChannel}/{spec.DamageDelivery}"
                : $"{spec.DamageChannel}/{spec.DamageDelivery}/{spec.DamageElement}";
        }

        private static void GeneratePdfWithReportLab()
        {
            const string scriptPath = "tools/generate_m76_enemy_attack_profiles_pdf.py";
            if (!File.Exists(scriptPath))
            {
                Debug.LogWarning($"M76 PDF generator script not found at {scriptPath}.");
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
                    Debug.LogWarning("M76 PDF generation did not start.");
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

                Debug.LogWarning($"M76 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M76 PDF generation skipped: {exception.Message}");
            }
        }
    }
}
