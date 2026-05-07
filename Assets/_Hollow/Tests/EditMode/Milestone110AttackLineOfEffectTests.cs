using System;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone110AttackLineOfEffectTests
    {
        [Test]
        public void BodyLaneAttackNeedsRepositionWhenRockBlocksPlayer()
        {
            var root = CreateRoomHarness(
                new RoomLayoutObstacle("rock_0", "rock", Vector3.zero, new Vector3(0.6f, 1f, 0.6f), true),
                out var room);
            var profile = CreateProfile("claw_lunge", EnemyAttackRuntimeKind.MeleeLunge, DamageDelivery.Melee, "body lane test");
            try
            {
                var result = EnemyAttackReachabilityService.Evaluate(
                    room,
                    new Vector3(0f, 0f, -1.1f),
                    new Vector3(0f, 0f, 1.1f),
                    0.32f,
                    PlaceholderPlayerController.DefaultRadiusMeters,
                    profile,
                    profile.RuntimeKind,
                    canReposition: true);

                Assert.AreEqual(EnemyAttackReachabilityStatus.NeedsReposition, result.Status);
                StringAssert.Contains("blocked_by_obstacle", result.Reason);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProjectileLineUsesProjectileBlockersInsteadOfAllMovementBlockers()
        {
            var root = CreateRoomHarness(
                new RoomLayoutObstacle("low_cover_0", "lowCover", Vector3.zero, new Vector3(0.6f, 1f, 0.6f), false),
                out var room);
            var projectile = CreateProfile("arrow_shot", EnemyAttackRuntimeKind.Projectile, DamageDelivery.Projectile, "direct shot");
            var melee = CreateProfile("rusty_slash", EnemyAttackRuntimeKind.WeaponMelee, DamageDelivery.Melee, "weapon slash");
            try
            {
                var from = new Vector3(0f, 0f, -1.1f);
                var target = new Vector3(0f, 0f, 1.1f);

                Assert.IsTrue(EnemyAttackReachabilityService.CanCommit(room, from, target, 0.32f, 0.3f, projectile, projectile.RuntimeKind, out _));
                Assert.IsFalse(EnemyAttackReachabilityService.CanCommit(room, from, target, 0.32f, 0.3f, melee, melee.RuntimeKind, out var meleeReason));
                StringAssert.Contains("blocked_by_obstacle", meleeReason);
            }
            finally
            {
                Object.DestroyImmediate(projectile);
                Object.DestroyImmediate(melee);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BallisticArcIgnoresMidCoverButRejectsBlockedLanding()
        {
            var root = CreateRoomHarness(
                new RoomLayoutObstacle("rock_0", "rock", Vector3.zero, new Vector3(0.6f, 1f, 0.6f), true),
                out var room);
            var lob = CreateProfile("spit_lob", EnemyAttackRuntimeKind.Projectile, DamageDelivery.Projectile, "Visible ballistic lob.");
            try
            {
                var overCover = EnemyAttackReachabilityService.Evaluate(
                    room,
                    new Vector3(0f, 0f, -1.8f),
                    new Vector3(0f, 0f, 1.8f),
                    0.44f,
                    0.3f,
                    lob,
                    lob.RuntimeKind,
                    canReposition: false);
                var blockedLanding = EnemyAttackReachabilityService.Evaluate(
                    room,
                    new Vector3(0f, 0f, -1.8f),
                    Vector3.zero,
                    0.44f,
                    0.3f,
                    lob,
                    lob.RuntimeKind,
                    canReposition: false);

                Assert.AreEqual(EnemyAttackObstructionPolicy.BallisticArc, lob.ResolvedObstructionPolicy);
                Assert.AreEqual(EnemyAttackReachabilityStatus.Clear, overCover.Status);
                Assert.AreEqual(EnemyAttackReachabilityStatus.Blocked, blockedLanding.Status);
                StringAssert.Contains("blocked_landing", blockedLanding.Reason);
            }
            finally
            {
                Object.DestroyImmediate(lob);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeChaserDoesNotStartMeleeThroughObstruction()
        {
            var root = CreateRoomHarness(
                new RoomLayoutObstacle("rock_0", "rock", Vector3.zero, new Vector3(0.4f, 1f, 0.4f), true),
                out var room);
            try
            {
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                player.transform.localPosition = new Vector3(0f, 0f, 0.65f);

                var enemyObject = new GameObject("NormalChaser");
                enemyObject.transform.SetParent(root.transform, false);
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"), null);
                enemy.transform.localPosition = new Vector3(0f, 0f, -0.65f);

                Assert.IsFalse(enemy.CanStartBehaviorMeleeAction("claw_lunge", 3f));
                Assert.AreEqual(EnemyAttackReachabilityStatus.NeedsReposition, enemy.LastAttackReachability.Status);
                StringAssert.Contains("blocked_by_obstacle", enemy.LastAttackReachabilityReason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EntryPositionResolverMovesPlayerInsideWhenDoorInsetIsBlocked()
        {
            var root = CreateRoomHarness(
                new RoomLayoutObstacle("entry_rock_0", "rock", new Vector3(0f, 0f, -2.45f), new Vector3(0.9f, 1f, 0.9f), true),
                out var room);
            try
            {
                var rawDoorInset = new Vector3(0f, 0f, -2.6f);
                var resolved = RoomLocalCollision.ResolveNearestOccupiablePosition(
                    room,
                    rawDoorInset,
                    PlaceholderPlayerController.DefaultRadiusMeters,
                    Vector3.forward,
                    3f);

                Assert.IsTrue(RoomLocalCollision.CanOccupy(room, resolved, PlaceholderPlayerController.DefaultRadiusMeters));
                Assert.Greater(resolved.z, rawDoorInset.z + 0.35f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeChargerRepositionsInsteadOfChargingThroughObstruction()
        {
            var root = CreateRoomHarness(
                new RoomLayoutObstacle("rock_0", "rock", Vector3.zero, new Vector3(0.55f, 1f, 0.55f), true),
                out var room);
            try
            {
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                player.transform.localPosition = new Vector3(0f, 0f, 1.2f);

                var enemyObject = new GameObject("AshCharger");
                enemyObject.transform.SetParent(root.transform, false);
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(room, player, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCharger"), null);
                enemy.transform.localPosition = new Vector3(0f, 0f, -1.2f);

                Assert.IsFalse(enemy.CanStartBudgetedAttack(3f));
                Assert.AreEqual(EnemyAttackReachabilityStatus.NeedsReposition, enemy.LastAttackReachability.Status);

                var director = new RoomTacticalDirector();
                Assert.IsTrue(director.TryResolveClearAttackReposition(enemy, room, player, "ash_charge", out var reserved, out var reason), reason);
                Assert.Greater(Mathf.Abs(reserved.x), 0.1f);

                var attack = enemy.ResolveAttackProfileForAi("ash_charge");
                var reachability = EnemyAttackReachabilityService.Evaluate(
                    room,
                    reserved,
                    player.transform.localPosition,
                    enemy.RadiusMeters,
                    PlaceholderPlayerController.DefaultRadiusMeters,
                    attack,
                    attack.RuntimeKind,
                    canReposition: false);
                Assert.IsTrue(reachability.CanCommit, reachability.Reason);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRoomHarness(RoomLayoutObstacle obstacle, out RoomRuntimeRoot room)
        {
            var root = new GameObject("M110LineOfEffectHarness");
            room = root.AddComponent<RoomRuntimeRoot>();
            var layout = new RoomLayout(
                7,
                7,
                Rect.MinMaxRect(-3.5f, -3.5f, 3.5f, 3.5f),
                Array.Empty<Vector2Int>(),
                Array.Empty<Vector2Int>(),
                new[] { new RoomLayoutFloorRegion("floor", Vector3.zero, new Vector2(3.5f, 3.5f)) },
                new[] { obstacle });
            var asset = new ImportedRoomRuntimeAsset(
                "m110_line_of_effect_room",
                "M110 Line Of Effect Room",
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(7, 7)),
                Array.Empty<RoomDoorPort>(),
                Array.Empty<ImportedSpawnPoint>(),
                Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint { id = "safe_start", kind = "safeStart", position = new ImportedVector3() },
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                null);
            room.BuildFrom(asset);
            return root;
        }

        private static EnemyAttackProfileDefinition CreateProfile(
            string attackId,
            EnemyAttackRuntimeKind runtimeKind,
            DamageDelivery delivery,
            string notes)
        {
            return EnemyAttackProfileDefinition.CreateRuntime(new EnemyAttackProfileSpec(
                "m110",
                false,
                attackId,
                attackId,
                runtimeKind,
                1,
                1f,
                0.2f,
                0.1f,
                3f,
                1,
                5f,
                DamageChannel.Physical,
                delivery,
                DamageElement.None,
                ImpactForceClass.Light,
                DamageThreatKind.Light,
                0.3f,
                0.35f,
                notes));
        }
    }
}
