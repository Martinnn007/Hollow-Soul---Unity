using System;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public class DestructibleRoomObjectController : MonoBehaviour
    {
        private RoomInteractiveObjectMarker marker;
        private RoomRuntimeRoot room;
        private RoomCombatController combat;
        private RoomHazardTuningProfileDefinition tuning;
        private int currentHealth = 1;
        private bool destroyed;

        public event Action<DestructibleRoomObjectController, RoomInteractiveObjectDestroyedContext> Destroyed;

        public RoomInteractiveObjectMarker Marker => marker;

        public string ObjectId => marker != null ? marker.ObjectId : name;

        public string ObjectKind => marker != null ? marker.ObjectKind : RoomInteractiveObjectKind.StandardBarrel;

        public bool IsDestroyed => destroyed || marker != null && marker.IsDestroyed;

        public bool IsExplosive => ObjectKind == RoomInteractiveObjectKind.ExplosiveBarrel;

        public float RadiusMeters
        {
            get
            {
                var size = marker != null ? marker.SizeMeters : transform.localScale;
                return Mathf.Max(size.x, size.z) * 0.5f;
            }
        }

        public void Configure(
            RoomInteractiveObjectMarker nextMarker,
            RoomRuntimeRoot nextRoom,
            RoomCombatController nextCombat,
            RoomHazardTuningProfileDefinition nextTuning)
        {
            marker = nextMarker;
            room = nextRoom;
            combat = nextCombat;
            tuning = RoomHazardTuningProfileDefinition.Resolve(nextTuning);
            currentHealth = tuning.BarrelHealth;
            destroyed = marker != null && marker.IsDestroyed;
        }

        public bool TryApplyHit(int amount, GameObject source)
        {
            if (IsDestroyed || amount <= 0)
            {
                return false;
            }

            currentHealth -= Mathf.Max(1, amount);
            if (currentHealth > 0)
            {
                return true;
            }

            DestroyObject(source, wasExplosionChain: false);
            return true;
        }

        public void ApplyExplosion(GameObject source)
        {
            if (IsDestroyed)
            {
                return;
            }

            DestroyObject(source, wasExplosionChain: true);
        }

        private void DestroyObject(GameObject source, bool wasExplosionChain)
        {
            if (IsDestroyed)
            {
                return;
            }

            destroyed = true;
            marker?.MarkDestroyed();
            if (IsExplosive)
            {
                Explode(source);
            }
            else
            {
                VfxPresenter.Play(VfxCueId.BarrelBreak, transform.position, transform.parent);
                AudioPresenter.Play(AudioCueId.BarrelBreak, transform.position);
            }

            var coinDrop = !IsExplosive ? DeterministicCoinDropAmount() : 0;
            Destroyed?.Invoke(this, new RoomInteractiveObjectDestroyedContext(ObjectId, ObjectKind, transform.localPosition, coinDrop));
            gameObject.SetActive(false);
        }

        private void Explode(GameObject source)
        {
            VfxPresenter.Play(VfxCueId.BarrelExplode, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.BarrelExplode, transform.position);
            DamagePlayerInRadius();
            DamageEnemiesInRadius();
            ChainNearbyObjects(source);
        }

        private void DamagePlayerInRadius()
        {
            var player = combat != null ? combat.PlayerController : null;
            var health = player != null ? player.GetComponent<CombatantHealth>() : null;
            if (health == null || !health.IsAlive || !IsWithinExplosion(player.transform.localPosition, PlaceholderPlayerController.DefaultRadiusMeters))
            {
                return;
            }

            DamageSystem.ApplyDamage(
                health,
                new DamageRequest(
                    tuning.ExplosiveBarrelPlayerDamage,
                    gameObject,
                    DamageFeedbackContext.Knockback(player.transform.localPosition - transform.localPosition, 0.45f, 0.1f),
                    DamageThreatKind.Environmental,
                    DamageClassification.Explosion(ImpactForceClass.Heavy)));
        }

        private void DamageEnemiesInRadius()
        {
            if (combat?.Enemies == null)
            {
                return;
            }

            foreach (var enemy in combat.Enemies)
            {
                if (enemy == null || !enemy.IsAlive || !IsWithinExplosion(enemy.transform.localPosition, enemy.RadiusMeters))
                {
                    continue;
                }

                var damage = tuning.ExplosiveBarrelDamage;
                if (enemy.ArchetypeId == EnemyArchetypeId.Boss || enemy.BehaviorId == EnemyBehaviorId.BossWarden)
                {
                    damage = Mathf.Max(1, Mathf.CeilToInt(damage * tuning.BossExplosionDamageMultiplier));
                }

                DamageSystem.ApplyDamage(
                    enemy.Health,
                    new DamageRequest(
                        damage,
                        gameObject,
                        DamageFeedbackContext.Knockback(enemy.transform.localPosition - transform.localPosition, 0.45f, 0.1f),
                        DamageThreatKind.Environmental,
                        DamageClassification.Explosion(ImpactForceClass.Heavy)));
            }
        }

        private void ChainNearbyObjects(GameObject source)
        {
            if (combat?.DestructibleObjects == null)
            {
                return;
            }

            foreach (var roomObject in combat.DestructibleObjects)
            {
                if (roomObject == null || roomObject == this || roomObject.IsDestroyed)
                {
                    continue;
                }

                var delta = roomObject.transform.localPosition - transform.localPosition;
                delta.y = 0f;
                if (delta.magnitude <= tuning.ExplosionRadiusMeters + roomObject.RadiusMeters)
                {
                    roomObject.ApplyExplosion(gameObject);
                }
            }
        }

        private bool IsWithinExplosion(Vector3 localPosition, float radius)
        {
            var delta = localPosition - transform.localPosition;
            delta.y = 0f;
            return delta.magnitude <= tuning.ExplosionRadiusMeters + Mathf.Max(0f, radius);
        }

        private int DeterministicCoinDropAmount()
        {
            if (tuning.StandardBarrelCoinDropAmount <= 0 || tuning.StandardBarrelCoinDropChancePercent <= 0)
            {
                return 0;
            }

            var seed = StableHash($"{room?.LastBuiltAsset?.Id}:{ObjectId}");
            return seed % 100 < tuning.StandardBarrelCoinDropChancePercent
                ? tuning.StandardBarrelCoinDropAmount
                : 0;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
