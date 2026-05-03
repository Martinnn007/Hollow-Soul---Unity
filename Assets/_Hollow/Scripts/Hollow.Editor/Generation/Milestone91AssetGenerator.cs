using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone91AssetGenerator
    {
        public const string DataRoot = "Assets/_Hollow/Data/EnemySpacing/M91";
        public const string DocsPath = "Docs/Hollow_M91_Preferred_Distance_And_Commitment_Tuning.md";
        public const string ReportPath = "output/reports/m91_preferred_distance_and_commitment_tuning.md";

        [MenuItem("Hollow/Generation/Generate Milestone 91 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(DataRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            WriteSpacingAssets();
            WriteDocs();
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 91 spacing profiles, docs, and report.");
        }

        private static void WriteSpacingAssets()
        {
            foreach (var enemy in EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"))
            {
                var source = enemy.SpacingProfile;
                var profile = LoadOrCreateProfile($"{DataRoot}/{Sanitize(enemy.SpawnKind)}_SpacingProfile.asset");
                profile.Configure(
                    source.SpacingProfileId,
                    source.OwnerSpawnKind,
                    source.DisplayName,
                    source.DefaultIdealDistanceMeters,
                    source.DefaultCloseToleranceMeters,
                    source.DefaultLongToleranceMeters,
                    source.ClosePressureBias,
                    source.RetreatBurstSeconds,
                    source.RetreatReassessSeconds,
                    source.MaxResetCountBeforeCommit,
                    source.FallbackRecoveryMovementMode,
                    source.FallbackRecoveryDistanceMeters,
                    source.FallbackRecoverySpeedMultiplier,
                    source.ActionOverrides.Select(CloneOverride));
                EditorUtility.SetDirty(profile);
            }

            foreach (var boss in BossCatalogDefinition.CreateRuntimeRoster())
            {
                var source = boss.SpacingProfileMetadata;
                var profile = LoadOrCreateProfile($"{DataRoot}/{Sanitize(boss.BossId)}_BossSpacingMetadata.asset");
                profile.Configure(
                    source.SpacingProfileId,
                    source.OwnerSpawnKind,
                    source.DisplayName,
                    source.DefaultIdealDistanceMeters,
                    source.DefaultCloseToleranceMeters,
                    source.DefaultLongToleranceMeters,
                    source.ClosePressureBias,
                    source.RetreatBurstSeconds,
                    source.RetreatReassessSeconds,
                    source.MaxResetCountBeforeCommit,
                    source.FallbackRecoveryMovementMode,
                    source.FallbackRecoveryDistanceMeters,
                    source.FallbackRecoverySpeedMultiplier,
                    source.ActionOverrides.Select(CloneOverride));
                EditorUtility.SetDirty(profile);
            }
        }

        private static EnemySpacingProfileDefinition LoadOrCreateProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<EnemySpacingProfileDefinition>(path);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<EnemySpacingProfileDefinition>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static EnemyActionSpacingOverride CloneOverride(EnemyActionSpacingOverride source)
        {
            var clone = new EnemyActionSpacingOverride();
            if (source == null)
            {
                return clone;
            }

            clone.Configure(
                source.ActionId,
                source.DesiredStartDistanceMeters,
                source.CommitRangeMinMeters,
                source.CommitRangeMaxMeters,
                source.CloseToleranceMeters,
                source.LongToleranceMeters,
                source.RecoveryMovementMode,
                source.RecoveryMovementDistanceMeters,
                source.RecoverySpeedMultiplier,
                source.MaxResetCountBeforeCommit);
            return clone;
        }

        private static void WriteDocs()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault().Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            var builder = new StringBuilder();
            builder.AppendLine("# M91: Preferred Distance + Commitment Tuning V2");
            builder.AppendLine();
            builder.AppendLine("M91 turns preferred distance into an action-first spacing contract. The old `preferredRangeMinMeters` and `preferredRangeMaxMeters` fields remain valid serialized fallback metadata, but behavior-tree decisions and runtime spacing now read `EnemySpacingProfileDefinition` envelopes and per-action range overrides.");
            builder.AppendLine();
            builder.AppendLine("## Contract");
            builder.AppendLine();
            builder.AppendLine("- Preferred Distance is a soft authored envelope, not a rigid hover band.");
            builder.AppendLine("- action-specific range overrides define desired start distance, commit range, tolerances, recovery spacing, and retreat caps.");
            builder.AppendLine("- If an action is blocked by budget or range, an enemy may use one short reset when authored, then it must commit, hold, face, or remain punishable.");
            builder.AppendLine("- Recovery spacing is identity-specific: weapon users stay mostly planted, creatures recoil briefly, ranged and magic enemies get one short reset or phase drift, and giants/heavies remain punishable.");
            builder.AppendLine("- Boss spacing profiles are metadata only in M91. Boss runtime spacing remains unchanged.");
            builder.AppendLine("- Contact damage remains M79 active-window-only.");
            builder.AppendLine();
            builder.AppendLine("## Current Roster Spacing Table");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Spawn Kind | Deprecated Fallback Range | Ideal | Tolerance | Reset Cap | Fallback Recovery | Action Overrides |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | ---: | --- | ---: |");
            foreach (var enemy in enemies)
            {
                var profile = enemy.SpacingProfile;
                builder.AppendLine($"| {enemy.DisplayName} | `{enemy.SpawnKind}` | {enemy.PreferredRangeMinMeters:0.00}-{enemy.PreferredRangeMaxMeters:0.00}m | {profile.DefaultIdealDistanceMeters:0.00}m | -{profile.DefaultCloseToleranceMeters:0.00}/+{profile.DefaultLongToleranceMeters:0.00}m | {profile.MaxResetCountBeforeCommit} | {profile.FallbackRecoveryMovementMode} | {profile.ActionOverrides.Count} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Action-Specific Range Examples");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Action | Desired Start | Commit Range | Recovery Spacing | Reset Cap |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | ---: |");
            foreach (var enemy in enemies)
            {
                foreach (var spacing in enemy.SpacingProfile.ActionOverrides.Take(4))
                {
                    builder.AppendLine($"| {enemy.DisplayName} | `{spacing.ActionId}` | {spacing.DesiredStartDistanceMeters:0.00}m | {spacing.CommitRangeMinMeters:0.00}-{spacing.CommitRangeMaxMeters:0.00}m | {spacing.RecoveryMovementMode} {spacing.RecoveryMovementDistanceMeters:0.00}m | {spacing.MaxResetCountBeforeCommit} |");
                }
            }

            builder.AppendLine();
            builder.AppendLine("## Boss Metadata");
            builder.AppendLine();
            builder.AppendLine("| Boss | Metadata Profile | Action Overrides | Runtime Use |");
            builder.AppendLine("| --- | --- | ---: | --- |");
            foreach (var boss in bosses)
            {
                builder.AppendLine($"| {boss.DisplayName} | `{boss.SpacingProfileMetadata.SpacingProfileId}` | {boss.SpacingProfileMetadata.ActionOverrides.Count} | ignored by boss runtime in M91 |");
            }

            builder.AppendLine();
            builder.AppendLine("## Tuning Notes");
            builder.AppendLine();
            builder.AppendLine("- Chasers and creature enemies are allowed to enter attack range instead of hovering at fallback min/max edges.");
            builder.AppendLine("- Ranged/firearm/caster profiles prefer a readable reset/backstep once, then hold or fire rather than endlessly retreating.");
            builder.AppendLine("- Weapon-user recovery is intentionally planted or tiny drift so whiffs stay punishable.");
            builder.AppendLine("- Phase-drift recovery is reserved for ghost/magic identities and remains local; no new pathfinding backend is introduced.");
            builder.AppendLine("- The next useful feel pass should tune action profiles and behavior tree weights together, not reintroduce hard distance gates.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault().Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            var overrideCount = enemies.Sum(enemy => enemy.SpacingProfile.ActionOverrides.Count);
            var plantedRecoveries = enemies.Sum(enemy => enemy.SpacingProfile.ActionOverrides.Count(row => row.RecoveryMovementMode == EnemySpacingRecoveryMode.Planted));
            var mobileRecoveries = overrideCount - plantedRecoveries;

            File.WriteAllText(ReportPath, $@"# M91 Preferred Distance + Commitment Tuning Report

- Non-boss spacing profiles resolved: {enemies.Length}.
- Boss metadata spacing profiles resolved: {bosses.Length}.
- Current action spacing overrides resolved: {overrideCount}.
- Planted recovery overrides: {plantedRecoveries}.
- Mobile recovery overrides: {mobileRecoveries}.
- Deprecated fallback fields still valid: `preferredRangeMinMeters` / `preferredRangeMaxMeters`.
- Runtime source of truth for combat spacing: `EnemySpacingProfileDefinition`.
- Action-specific range overrides: enabled.
- Recovery spacing: enabled.
- Retreat caps: enabled.
- Boss runtime spacing: unchanged.
- Docs: `{DocsPath}`.
- Report: `{ReportPath}`.
");
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "spacing";
            }

            var chars = value.Trim().ToCharArray();
            for (var index = 0; index < chars.Length; index++)
            {
                if (!char.IsLetterOrDigit(chars[index]) && chars[index] != '_' && chars[index] != '-')
                {
                    chars[index] = '_';
                }
            }

            return new string(chars);
        }
    }
}
