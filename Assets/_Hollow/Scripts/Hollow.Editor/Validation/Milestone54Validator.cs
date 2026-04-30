using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone54Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Rewards/ProjectilePassiveResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ProjectilePassiveState.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ProjectilePatternKind.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/ProjectileVisualStyle.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone54AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/ItemCataloguePdfExporter.cs",
            Milestone54AssetGenerator.DocsPath
        };

        [MenuItem("Hollow/Validation/Run Milestone 54 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M54 file: {file}");
                }
            }

            var standard = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone52AssetGenerator.StandardRewardPoolPath);
            var treasure = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.TreasureRewardPoolPath);
            var boss = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone54AssetGenerator.BossRewardPoolPath);
            ValidatePools(standard, treasure, boss, failures);
            ValidateProjectilePassiveResolver(failures);
            ValidatePdf(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 54 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidatePools(RewardPoolDefinition standard, RewardPoolDefinition treasure, RewardPoolDefinition boss, List<string> failures)
        {
            if (standard == null)
            {
                failures.Add("M54 requires the M52 standard reward pool to preserve sparse room rewards.");
            }

            if (treasure == null || boss == null)
            {
                failures.Add("M54 treasure/boss reward pools are missing.");
                return;
            }

            foreach (var id in ProjectilePassiveResolver.AllProjectilePassiveIds)
            {
                var treasureReward = treasure.Rewards.FirstOrDefault(reward => reward != null && reward.RewardId == id);
                var bossReward = boss.Rewards.FirstOrDefault(reward => reward != null && reward.RewardId == id);
                if (treasureReward == null || bossReward == null)
                {
                    failures.Add($"M54 projectile passive `{id}` must be present in treasure and boss pools.");
                    continue;
                }

                var expectedStacks = ProjectilePassiveResolver.MaxStacksForReward(id);
                if (treasureReward.MaxStacks != expectedStacks || bossReward.MaxStacks != expectedStacks)
                {
                    failures.Add($"M54 projectile passive `{id}` has incorrect max stack metadata.");
                }
            }

            if (standard != null && standard.Rewards.Any(reward => reward != null && ProjectilePassiveResolver.IsM54ProjectilePassive(reward.RewardId)))
            {
                failures.Add("M54 projectile passives must not appear in the standard room reward pool.");
            }
        }

        private static void ValidateProjectilePassiveResolver(List<string> failures)
        {
            var build = new PlayerRunBuild();
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.DoubleBarrelId, 1);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.TripleShotId, 1);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, ProjectilePassiveResolver.FireRateUpMaxStacks);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, ProjectilePassiveResolver.FireRateUpMaxStacks);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, ProjectilePassiveResolver.FireRateUpMaxStacks);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId, ProjectilePassiveResolver.FireRateUpMaxStacks);
            build.Inventory.AddPassiveItem(ProjectilePassiveResolver.PowerUpId, 1);

            var state = ProjectilePassiveResolver.Resolve(build);
            if (state.PatternKind != ProjectilePatternKind.TripleShot)
            {
                failures.Add("M54 projectile resolver should choose the strongest owned pattern.");
            }

            if (!Mathf.Approximately(state.RangedLightFireRateBonusPerSecond, 3f))
            {
                failures.Add("M54 Fire-rate Up must cap at +3 ranged light shots per second.");
            }

            if (!Mathf.Approximately(state.RangedDamageMultiplier, 2f) || state.VisualStyle != ProjectileVisualStyle.RedPower)
            {
                failures.Add("M54 Power-up must resolve to x2 ranged damage and red projectile visual style.");
            }
        }

        private static void ValidatePdf(List<string> failures)
        {
            if (!File.Exists(Milestone54AssetGenerator.PdfPath))
            {
                failures.Add($"M54 item catalogue PDF is missing: {Milestone54AssetGenerator.PdfPath}");
                return;
            }

            var bytes = File.ReadAllBytes(Milestone54AssetGenerator.PdfPath);
            if (bytes.Length < 1000 || !System.Text.Encoding.ASCII.GetString(bytes.Take(128).ToArray()).Contains("%PDF"))
            {
                failures.Add("M54 item catalogue PDF does not look like a valid PDF file.");
            }
        }
    }
}
