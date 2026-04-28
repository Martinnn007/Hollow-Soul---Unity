using Hollow.Combat;
using Hollow.Data.Definitions;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone43CombatFeelTests
    {
        [Test]
        public void CombatFeelProfileDefaultLocksM43Values()
        {
            var profile = CombatFeelProfileDefinition.CreateRuntimeDefault();

            Assert.AreEqual(0.6f, profile.PlayerInvulnerabilitySeconds, 0.001f);
            Assert.AreEqual(1.5f, profile.CorpseGhostSeconds, 0.001f);
            Assert.IsFalse(profile.ShowWindupLabels);
            Assert.Greater(profile.PlayerKnockbackMeters, 0f);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void DamageRequestLegacyConstructorHasNoKnockback()
        {
            var source = new GameObject("Source");
            var request = new DamageRequest(1, source);

            Assert.AreEqual(1, request.Amount);
            Assert.AreEqual(source, request.Source);
            Assert.IsFalse(request.Feedback.HasKnockback);

            Object.DestroyImmediate(source);
        }

        [Test]
        public void DamageRequestCarriesKnockbackFeedback()
        {
            var source = new GameObject("Source");
            var request = new DamageRequest(
                2,
                source,
                DamageFeedbackContext.Knockback(Vector3.right, 0.7f, 0.14f));

            Assert.IsTrue(request.Feedback.HasKnockback);
            Assert.AreEqual(0.7f, request.Feedback.KnockbackMeters, 0.001f);
            Assert.AreEqual(Vector3.right, request.Feedback.Direction);

            Object.DestroyImmediate(source);
        }

        [Test]
        public void KnockbackReceiverMovesUsingRoomLocalCollision()
        {
            var target = new GameObject("KnockbackTarget");
            var receiver = target.AddComponent<CombatKnockbackReceiver>();
            receiver.Configure(null, 0.3f, false, 1f);

            receiver.ApplyKnockback(Vector3.right, 1f, 0.1f);
            receiver.Tick(0.1f);

            Assert.AreEqual(1f, target.transform.localPosition.x, 0.001f);
            Assert.AreEqual(0f, target.transform.localPosition.z, 0.001f);

            Object.DestroyImmediate(target);
        }

        [Test]
        public void PlayerDamageFeedbackBlocksFollowupDamageDuringInvulnerability()
        {
            var player = new GameObject("Player");
            var health = player.AddComponent<CombatantHealth>();
            health.Configure(6);
            var feedback = player.AddComponent<PlayerDamageFeedbackController>();
            feedback.Configure(null, CombatFeelProfileDefinition.CreateRuntimeDefault());

            var source = new GameObject("Enemy");
            Assert.IsTrue(DamageSystem.ApplyDamage(health, new DamageRequest(1, source)));
            Assert.AreEqual(5, health.CurrentHealth);
            Assert.IsTrue(feedback.IsInvulnerable);

            Assert.IsFalse(DamageSystem.ApplyDamage(health, new DamageRequest(1, source)));
            Assert.AreEqual(5, health.CurrentHealth);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void CorpseGhostPresenterCreatesVisualOnlyGhost()
        {
            var parent = new GameObject("RoomRoot");
            var enemyObject = new GameObject("Enemy");
            enemyObject.transform.SetParent(parent.transform, false);
            enemyObject.transform.localPosition = new Vector3(2f, 0.3f, -1f);
            enemyObject.transform.localScale = Vector3.one;
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();

            var ghost = CorpseGhostPresenter.SpawnFrom(enemy, CombatFeelProfileDefinition.CreateRuntimeDefault());

            Assert.IsNotNull(ghost);
            Assert.IsNull(ghost.GetComponent<Collider>());
            Assert.AreEqual(enemyObject.transform.localPosition, ghost.transform.localPosition);

            Object.DestroyImmediate(parent);
        }
    }
}
