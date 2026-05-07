using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone112RollReadabilityTests
    {
        [Test]
        public void RollHasVulnerableStartupInvulnerableTravelAndVulnerableRecovery()
        {
            var root = new GameObject("M112RollPhaseHarness");
            var source = new GameObject("M112DamageSource");
            source.transform.SetParent(root.transform, false);
            var player = CreateRollPlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var feedback = player.GetComponent<PlayerDamageFeedbackController>();
                var request = new DamageRequest(1, source, DamageFeedbackContext.Knockback(Vector3.forward, 1f, 0.1f));

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds * 0.5f);
                Assert.AreEqual(PlayerRollPhase.Startup, weapon.CurrentRollPhase);
                Assert.IsFalse(weapon.IsRollInvulnerable);
                Assert.AreEqual(1, feedback.ModifyIncomingDamage(request, 1));

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + 0.01f);
                Assert.AreEqual(PlayerRollPhase.InvulnerableTravel, weapon.CurrentRollPhase);
                Assert.IsTrue(weapon.IsRollInvulnerable);
                Assert.AreEqual(0, feedback.ModifyIncomingDamage(request, 1));

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + PlayerWeaponController.RollInvulnerabilitySeconds + 0.01f);
                Assert.AreEqual(PlayerRollPhase.Recovery, weapon.CurrentRollPhase);
                Assert.IsFalse(weapon.IsRollInvulnerable);
                Assert.AreEqual(1, feedback.ModifyIncomingDamage(request, 1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollTravelMovesThenRecoveryStopsMovementAndBlocksActions()
        {
            var root = new GameObject("M112RollMovementHarness");
            var player = CreateRollPlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var movement = player.GetComponent<PlayerMovementController>();

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.right, 0f));
                movement.Move(Vector2.zero, PlayerWeaponController.RollStartupSeconds + 0.08f);
                Assert.Greater(player.transform.localPosition.z, 0.4f);
                Assert.AreEqual(0f, player.transform.localPosition.x, 0.001f);

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + PlayerWeaponController.RollInvulnerabilitySeconds + 0.01f);
                Assert.AreEqual(PlayerRollPhase.Recovery, weapon.CurrentRollPhase);
                var recoveryPosition = player.transform.localPosition;
                movement.Move(Vector2.zero, PlayerWeaponController.RollRecoverySeconds * 0.5f);
                Assert.AreEqual(recoveryPosition.z, player.transform.localPosition.z, 0.001f);
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, PlayerWeaponController.RollDurationSeconds - 0.02f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollIFramesPreventKnockbackAsWellAsDamage()
        {
            var root = new GameObject("M112RollKnockbackHarness");
            var source = new GameObject("M112KnockbackSource");
            source.transform.SetParent(root.transform, false);
            var player = CreateRollPlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var health = player.GetComponent<CombatantHealth>();
                var knockback = player.GetComponent<CombatKnockbackReceiver>();
                var request = new DamageRequest(
                    1,
                    source,
                    DamageFeedbackContext.Knockback(Vector3.forward, 1f, 0.1f),
                    DamageClassification.PhysicalMelee(ImpactForceClass.Heavy));

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + 0.01f);
                Assert.IsFalse(DamageSystem.ApplyDamage(health, request));
                Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
                Assert.IsFalse(knockback.IsKnockbackActive);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RollVisualControllerEmitsStartTrailAndRecoveryCues()
        {
            var root = new GameObject("M112RollVisualHarness");
            var player = CreateRollPlayer(root.transform);
            try
            {
                var weapon = player.GetComponent<PlayerWeaponController>();
                var visual = player.AddComponent<PlayerRollVisualController>();
                visual.Bind(weapon);

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                visual.Tick(0f, 0f);
                Assert.IsTrue(HasCue(root, VfxCueId.PlayerRollStart));

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + 0.01f);
                visual.Tick(0.08f, PlayerWeaponController.RollStartupSeconds + 0.01f);
                Assert.IsTrue(HasCue(root, VfxCueId.PlayerRollTrail));

                weapon.TickAction(0f, PlayerWeaponController.RollStartupSeconds + PlayerWeaponController.RollInvulnerabilitySeconds + 0.01f);
                visual.Tick(0.08f, PlayerWeaponController.RollStartupSeconds + PlayerWeaponController.RollInvulnerabilitySeconds + 0.01f);
                Assert.IsTrue(HasCue(root, VfxCueId.PlayerRollRecovery));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRollPlayer(Transform parent)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.AddComponent<PlayerWeaponController>();
            player.AddComponent<PlayerMovementController>();
            player.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            player.AddComponent<CombatKnockbackReceiver>().Configure(null, Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            player.AddComponent<PlayerDamageFeedbackController>().Configure(null, null);
            return player;
        }

        private static bool HasCue(GameObject root, VfxCueId cue)
        {
            var expected = $"VFX.{cue}.Fallback";
            return root.GetComponentsInChildren<Transform>(true)
                .Any(child => child != null && child.name == expected);
        }
    }
}
