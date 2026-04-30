using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Editor.Validation;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone53BossRosterFrameworkTests
    {
        [Test]
        public void RuntimeBossRosterContainsTenFixedHpBosses()
        {
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            Assert.AreEqual(10, bosses.Length);
            AssertBoss(bosses, "stone_warden", BossWorldBand.World1, 24);
            AssertBoss(bosses, "splinter_saint", BossWorldBand.World1, 22);
            AssertBoss(bosses, "gravel_maw", BossWorldBand.World1, 28);
            AssertBoss(bosses, "cartouche_widow", BossWorldBand.World2, 32);
            AssertBoss(bosses, "iron_reliquary", BossWorldBand.World2, 36);
            AssertBoss(bosses, "mirror_husk", BossWorldBand.World2, 34);
            AssertBoss(bosses, "ash_comet", BossWorldBand.World2, 38);
            AssertBoss(bosses, "choir_of_teeth", BossWorldBand.World3, 42);
            AssertBoss(bosses, "rust_bishop", BossWorldBand.World3, 46);
            AssertBoss(bosses, "hollow_star_larva", BossWorldBand.World3, 50);
            Assert.AreEqual(10, bosses.Select(boss => boss.Arena.arenaId).Distinct().Count());
        }

        [Test]
        public void BossSelectionIsDeterministicAndWorldBanded()
        {
            var catalog = BossCatalogDefinition.CreateRuntimeDefault();
            var first = BossSelectionResolver.Resolve(catalog, 53001, 53001, 1, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            var second = BossSelectionResolver.Resolve(catalog, 53001, 53001, 1, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.AreEqual(first.BossId, second.BossId);
            Assert.AreEqual(BossWorldBand.World1, first.WorldBand);

            var world2 = BossSelectionResolver.Resolve(catalog, 53001, 53001, 2, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            var world3 = BossSelectionResolver.Resolve(catalog, 53001, 53001, 3, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            Assert.AreEqual(BossWorldBand.World2, world2.WorldBand);
            Assert.AreEqual(BossWorldBand.World3, world3.WorldBand);
        }

        [Test]
        public void EncounterAssignmentPersistsBossMetadata()
        {
            var assignment = new RoomEncounterAssignment(
                "boss_01",
                "stone_warden_boss",
                new[] { "spawnEnemyBoss" },
                2,
                5,
                7,
                "iron_reliquary",
                "boss_arena_cover_maze",
                2,
                "phase_1");
            var plan = new EncounterPlan(new[] { assignment });
            var save = plan.ToSaveState();
            var restored = EncounterPlan.FromSaveState(save);
            Assert.IsTrue(restored.TryResolve("boss_01", out var restoredAssignment));
            Assert.AreEqual("iron_reliquary", restoredAssignment.BossId);
            Assert.AreEqual("boss_arena_cover_maze", restoredAssignment.BossArenaId);
            Assert.AreEqual(2, restoredAssignment.BossWorldBand);
            Assert.AreEqual("phase_1", restoredAssignment.BossPhaseState);
        }

        [Test]
        public void Milestone53ValidatorPassesRuntimeChecks()
        {
            var report = Milestone53Validator.Validate();
            var runtimeFailures = report.Failures
                .Where(failure => !failure.Contains("Missing M53 docs") &&
                                  !failure.Contains("Generated M53 boss catalog"))
                .ToArray();
            CollectionAssert.IsEmpty(runtimeFailures);
        }

        private static void AssertBoss(BossDefinition[] bosses, string id, BossWorldBand band, int hp)
        {
            var boss = bosses.FirstOrDefault(candidate => candidate.BossId == id);
            Assert.NotNull(boss, id);
            Assert.AreEqual(band, boss.WorldBand, id);
            Assert.AreEqual(hp, boss.MaxHealth, id);
            Assert.IsTrue(hp >= 20 && hp <= 50, id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(boss.DisplayName), id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(boss.Arena.arenaId), id);
            Assert.Greater(boss.Attacks.Count, 0, id);
        }
    }
}
