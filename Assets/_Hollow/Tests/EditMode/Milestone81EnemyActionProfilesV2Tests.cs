using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone81EnemyActionProfilesV2Tests
    {
        [Test]
        public void CurrentEnemyAndBossRostersResolveActionProfiles()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var group in EnemyActionProfileDefaults.AllEnemySpecs.GroupBy(spec => spec.OwnerId))
            {
                var definition = catalog.Resolve(group.Key);
                Assert.NotNull(definition, group.Key);
                var actionIds = definition.ActionProfiles.Select(profile => profile.ActionId).ToArray();
                foreach (var spec in group)
                {
                    Assert.Contains(spec.ActionId, actionIds, group.Key);
                }

                foreach (var action in definition.ActionProfiles)
                {
                    AssertActionValid(action);
                }
            }

            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            foreach (var group in EnemyActionProfileDefaults.AllBossSpecs.GroupBy(spec => spec.OwnerId))
            {
                var boss = bosses.FirstOrDefault(candidate => candidate.BossId == group.Key);
                Assert.NotNull(boss, group.Key);
                var actionIds = boss.ActionProfiles.Select(profile => profile.ActionId).ToArray();
                foreach (var spec in group)
                {
                    Assert.Contains(spec.ActionId, actionIds, group.Key);
                }

                foreach (var action in boss.ActionProfiles)
                {
                    AssertActionValid(action);
                }
            }
        }

        [Test]
        public void CurrentActionsWrapEveryM76AttackProfile()
        {
            foreach (var attack in EnemyAttackProfileDefaults.AllEnemySpecs)
            {
                var action = EnemyActionProfileDefaults.ResolveEnemyAction(attack.OwnerId, attack.AttackId);
                Assert.NotNull(action, $"{attack.OwnerId}:{attack.AttackId}");
                Assert.AreEqual(EnemyActionUsageState.CurrentRuntime, action.UsageState);
                Assert.AreEqual(attack.AttackId, action.LinkedAttackId);
                Assert.NotNull(action.LinkedAttackProfile);
                Assert.AreEqual(attack.AttackId, action.LinkedAttackProfile.AttackId);
            }

            foreach (var attack in EnemyAttackProfileDefaults.AllBossSpecs)
            {
                var action = EnemyActionProfileDefaults.ResolveBossAction(attack.OwnerId, attack.AttackId);
                Assert.NotNull(action, $"{attack.OwnerId}:{attack.AttackId}");
                Assert.AreEqual(EnemyActionUsageState.CurrentRuntime, action.UsageState);
                Assert.AreEqual(attack.AttackId, action.LinkedAttackId);
                Assert.NotNull(action.LinkedAttackProfile);
                Assert.AreEqual(attack.AttackId, action.LinkedAttackProfile.AttackId);
            }
        }

        [Test]
        public void UnlinkedFutureAndTemplateActionsAreExplicitlyNonDamaging()
        {
            foreach (var spec in Milestone81AssetGenerator.AllActionSpecs())
            {
                if (!spec.HasLinkedAttack)
                {
                    Assert.IsTrue(spec.ExplicitlyNonDamaging, spec.ActionId);
                    Assert.AreNotEqual(EnemyActionUsageState.CurrentRuntime, spec.UsageState, spec.ActionId);
                }
            }
        }

        [Test]
        public void ActionSchemaAndTemplateCoverageAreValid()
        {
            foreach (var spec in Milestone81AssetGenerator.AllActionSpecs())
            {
                Assert.IsNotEmpty(spec.ActionId);
                Assert.IsNotEmpty(spec.DisplayName);
                Assert.GreaterOrEqual(spec.MinRangeMeters, 0f, spec.ActionId);
                Assert.GreaterOrEqual(spec.IdealRangeMeters, spec.MinRangeMeters, spec.ActionId);
                Assert.LessOrEqual(spec.IdealRangeMeters, spec.MaxRangeMeters, spec.ActionId);
                Assert.Greater(spec.BaseWeight, 0f, spec.ActionId);
                Assert.GreaterOrEqual(spec.PressureCost, 0, spec.ActionId);
                Assert.IsNotEmpty(spec.CooldownGroup, spec.ActionId);
                Assert.IsNotEmpty(spec.TelegraphNote, spec.ActionId);
                Assert.IsNotEmpty(spec.PoiseBreakNote, spec.ActionId);
                Assert.IsNotEmpty(spec.RecoveryPunishNote, spec.ActionId);
                Assert.Greater(spec.AllowedDispositions.Count, 0, spec.ActionId);
                Assert.Greater(spec.BestUserTags.Count, 0, spec.ActionId);
                Assert.GreaterOrEqual(spec.FacingArcDegrees, 0f, spec.ActionId);
                Assert.LessOrEqual(spec.FacingArcDegrees, 360f, spec.ActionId);
            }

            Assert.GreaterOrEqual(EnemyActionProfileDefaults.LibraryTemplateSpecs.Count, 60);
            foreach (EnemyActionCategory category in Enum.GetValues(typeof(EnemyActionCategory)))
            {
                Assert.IsTrue(
                    EnemyActionProfileDefaults.LibraryTemplateSpecs.Any(spec => spec.Category == category),
                    category.ToString());
            }
        }

        [Test]
        public void ResolveActionProfileFallsBackFromDefinitions()
        {
            var normal = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
            var claw = normal.ResolveActionProfile("claw_lunge");
            Assert.NotNull(claw);
            Assert.AreEqual("claw_lunge", claw.LinkedAttackId);

            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            var stone = bosses.First(boss => boss.BossId == "stone_warden");
            var charge = stone.ResolveActionProfile("stone_charge");
            Assert.NotNull(charge);
            Assert.AreEqual("stone_charge", charge.LinkedAttackId);
        }

        [Test]
        public void CataloguePdfExtractsRequiredTextAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists(Milestone81AssetGenerator.DocsPath), Milestone81AssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone81AssetGenerator.PdfPath), Milestone81AssetGenerator.PdfPath);
            var markdown = File.ReadAllText(Milestone81AssetGenerator.DocsPath);
            StringAssert.Contains("Enemy Action Profiles V2", markdown);
            StringAssert.Contains("Body", markdown);
            StringAssert.Contains("Weapon", markdown);
            StringAssert.Contains("Magic", markdown);
            StringAssert.Contains("Defense", markdown);
            StringAssert.Contains("Hazard", markdown);
            StringAssert.Contains("poise", markdown);
            StringAssert.Contains("counterplay", markdown);
            StringAssert.Contains("Rat", markdown);
            StringAssert.Contains("Spider", markdown);
            StringAssert.Contains("Boss Action Profiles", markdown);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone81Validator.Validate());
        }

        private static void AssertActionValid(EnemyActionProfileDefinition action)
        {
            Assert.NotNull(action);
            Assert.IsNotEmpty(action.ActionId);
            Assert.IsNotEmpty(action.DisplayName);
            Assert.GreaterOrEqual(action.MinRangeMeters, 0f, action.ActionId);
            Assert.GreaterOrEqual(action.IdealRangeMeters, action.MinRangeMeters, action.ActionId);
            Assert.LessOrEqual(action.IdealRangeMeters, action.MaxRangeMeters, action.ActionId);
            Assert.Greater(action.BaseWeight, 0f, action.ActionId);
            Assert.GreaterOrEqual(action.PressureCost, 0, action.ActionId);
            Assert.IsNotEmpty(action.CooldownGroup, action.ActionId);
            Assert.Greater(action.AllowedDispositions.Count, 0, action.ActionId);
            Assert.Greater(action.BestUserTags.Count, 0, action.ActionId);
            Assert.GreaterOrEqual(action.FacingArcDegrees, 0f, action.ActionId);
            Assert.LessOrEqual(action.FacingArcDegrees, 360f, action.ActionId);
            Assert.GreaterOrEqual(action.PunishabilityRating, 0, action.ActionId);
            Assert.LessOrEqual(action.PunishabilityRating, 5, action.ActionId);
            Assert.GreaterOrEqual(action.GuardPressureRating, 0, action.ActionId);
            Assert.LessOrEqual(action.GuardPressureRating, 5, action.ActionId);

            if (!action.HasLinkedAttack)
            {
                Assert.IsTrue(action.ExplicitlyNonDamaging, action.ActionId);
            }
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath(Milestone81AssetGenerator.VerifyScriptPath);
            Assert.IsTrue(File.Exists(scriptPath), scriptPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            if (!process.WaitForExit(15000))
            {
                process.Kill();
                Assert.Fail("Timed out while verifying the M81 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }
    }
}
