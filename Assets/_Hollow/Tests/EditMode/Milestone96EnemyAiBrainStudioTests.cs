using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.EnemyAiBrainStudio;
using Hollow.Editor.EnemyAuthoring;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone96EnemyAiBrainStudioTests
    {
        [Test]
        public void TemplateLibraryProvidesBrainRoles()
        {
            var templates = EnemyAiBrainStudioTemplateGenerator.CreateRuntimeTemplates();
            try
            {
                Assert.That(templates.Count, Is.GreaterThanOrEqualTo(10));
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.BodyPressure);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.PreySkirmisher);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.TerritorialCritter);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.StationarySentinel);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.WeaponUser);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.RangedKiter);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.MagicCaster);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.HeavyBruiser);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.SwarmBackground);
                CollectionAssert.Contains(templates.Select(template => template.Role), EnemyAiBrainTemplateRole.BossMetadata);

                foreach (var template in templates)
                {
                    Assert.That(template.TemplateId, Is.Not.Empty);
                    Assert.That(template.DisplayName, Is.Not.Empty);
                    Assert.That(template.AttackWindupScale, Is.GreaterThan(0f));
                    Assert.That(template.AttackRecoveryScale, Is.GreaterThan(0f));
                    Assert.That(template.DisturbanceEscalationThreshold, Is.GreaterThan(0f));
                }
            }
            finally
            {
                DestroyTemplates(templates);
            }
        }

        [Test]
        public void AnalysisSuggestsExpectedRosterRoles()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            Assert.AreEqual(EnemyAiBrainTemplateRole.PreySkirmisher, EnemyAiBrainStudioAnalysis.SuggestRole(catalog.Resolve("spawnEnemyFlying")));
            Assert.AreEqual(EnemyAiBrainTemplateRole.StationarySentinel, EnemyAiBrainStudioAnalysis.SuggestRole(catalog.Resolve("spawnEnemyTurret")));
            Assert.AreEqual(EnemyAiBrainTemplateRole.WeaponUser, EnemyAiBrainStudioAnalysis.SuggestRole(catalog.Resolve("spawnEnemySkeletonSword")));
            Assert.AreEqual(EnemyAiBrainTemplateRole.HeavyBruiser, EnemyAiBrainStudioAnalysis.SuggestRole(catalog.Resolve("spawnEnemyKnight")));
            Assert.AreEqual(EnemyAiBrainTemplateRole.RangedKiter, EnemyAiBrainStudioAnalysis.SuggestRole(catalog.Resolve("spawnEnemyHollowArcher")));
            Assert.AreEqual(EnemyAiBrainTemplateRole.MagicCaster, EnemyAiBrainStudioAnalysis.SuggestRole(catalog.Resolve("spawnEnemyHollowAcolyte")));
        }

        [Test]
        public void TemplateApplicationChangesDraftOnly()
        {
            var source = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
            var templates = EnemyAiBrainStudioTemplateGenerator.CreateRuntimeTemplates();
            var template = templates.First(candidate => candidate.Role == EnemyAiBrainTemplateRole.BodyPressure);
            var draft = new EnemyAuthoringDraft();
            try
            {
                var sourceIntelligence = source.Intelligence;
                draft.Load(source);
                EnemyAiBrainStudioAnalysis.ApplyTemplateToEnemyDraft((EnemyDefinition)draft.Draft, template);

                Assert.AreEqual(sourceIntelligence, source.Intelligence);
                Assert.AreEqual(template.TargetIntelligence, ((EnemyDefinition)draft.Draft).Intelligence);
                Assert.AreEqual(template.TargetDisposition, ((EnemyDefinition)draft.Draft).Disposition);
                Assert.IsTrue(draft.IsDirty);
            }
            finally
            {
                draft.Dispose();
                DestroyTemplates(templates);
            }
        }

        [Test]
        public void ActionScorePreviewSortsCurrentRuntimeActions()
        {
            var enemy = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
            var previews = EnemyAiBrainStudioAnalysis.BuildActionPreview(
                enemy,
                1.35f,
                EnemyAwarenessState.Engaged,
                enemy.Disposition,
                enemy.Intelligence,
                meleePressure: 1.5f);

            Assert.That(previews.Count, Is.GreaterThan(0));
            for (var index = 1; index < previews.Count; index++)
            {
                Assert.That(previews[index - 1].Score, Is.GreaterThanOrEqualTo(previews[index].Score));
            }

            Assert.That(previews[0].ActionId, Is.Not.Empty);
            Assert.AreNotEqual(EnemyBehaviorCommandKind.None, previews[0].CommandKind);
        }

        [Test]
        public void ValidationReportsBrainContractNotes()
        {
            var enemy = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal");
            var result = EnemyAiBrainStudioAnalysis.ValidateEnemy(enemy);

            Assert.IsTrue(result.IsValid);
            Assert.That(result.Notes.Any(note => note.Contains("suggested brain role")), Is.True);
            Assert.That(result.Notes.Any(note => note.Contains("Senses")), Is.True);
            Assert.That(result.Notes.Any(note => note.Contains("Commitment")), Is.True);
        }

        [Test]
        public void DocsAndReportExist()
        {
            Assert.IsTrue(File.Exists("Docs/Hollow_M96_Enemy_AI_Brain_Studio.md"));
            Assert.IsTrue(File.Exists("output/reports/enemy_ai_brain_studio/m96_enemy_ai_brain_studio.md"));
        }

        private static void DestroyTemplates(System.Collections.Generic.IEnumerable<EnemyAiBrainTemplateDefinition> templates)
        {
            foreach (var template in templates)
            {
                Object.DestroyImmediate(template);
            }
        }
    }
}
