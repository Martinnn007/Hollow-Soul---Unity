using System.Collections.Generic;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class SpikeHazardController : RoomHazardController
    {
        private readonly Dictionary<int, float> nextAllowedDamageByTarget = new();

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            if (Marker == null || Tuning == null)
            {
                return;
            }

            TryDamagePlayer(timeSeconds);
            TryDamageEnemies(timeSeconds);
        }

        private void TryDamagePlayer(float timeSeconds)
        {
            var health = Player != null ? Player.GetComponent<CombatantHealth>() : null;
            if (health == null || !health.IsAlive || !IsInside(Player.transform.localPosition, PlaceholderPlayerController.DefaultRadiusMeters))
            {
                return;
            }

            TryApplyHazardDamage(health, Player.gameObject, timeSeconds);
        }

        private void TryDamageEnemies(float timeSeconds)
        {
            if (Combat?.Enemies == null)
            {
                return;
            }

            foreach (var enemy in Combat.Enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.MovementMode == EnemyMovementMode.Flying)
                {
                    continue;
                }

                if (!IsInside(enemy.transform.localPosition, enemy.RadiusMeters))
                {
                    continue;
                }

                TryApplyHazardDamage(enemy.Health, enemy.gameObject, timeSeconds);
            }
        }

        private bool TryApplyHazardDamage(CombatantHealth health, GameObject target, float timeSeconds)
        {
            var targetId = target != null ? target.GetInstanceID() : 0;
            if (targetId == 0 ||
                nextAllowedDamageByTarget.TryGetValue(targetId, out var nextTime) && timeSeconds < nextTime)
            {
                return false;
            }

            var direction = target.transform.localPosition - transform.localPosition;
            direction.y = 0f;
            var applied = DamageSystem.ApplyDamage(
                health,
                new DamageRequest(
                    Tuning.SpikeDamage,
                    gameObject,
                    DamageFeedbackContext.Knockback(direction, 0.22f, 0.08f),
                    DamageThreatKind.Environmental));
            if (!applied)
            {
                return false;
            }

            nextAllowedDamageByTarget[targetId] = timeSeconds + Tuning.SpikeCooldownSeconds;
            VfxPresenter.Play(VfxCueId.HazardHit, target.transform.position, target.transform.parent);
            AudioPresenter.Play(AudioCueId.HazardHit, target.transform.position);
            return true;
        }

        private bool IsInside(Vector3 localPosition, float radius)
        {
            var flat = localPosition - transform.localPosition;
            flat.y = 0f;
            return flat.magnitude <= Marker.RadiusMeters + Mathf.Max(0f, radius);
        }
    }
}
