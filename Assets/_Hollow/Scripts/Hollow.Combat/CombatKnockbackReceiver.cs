using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class CombatKnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private float radiusMeters = 0.3f;
        [SerializeField] private bool ignoreObstacles;
        [SerializeField] private float resistanceMultiplier = 1f;
        [SerializeField] private int stability;

        private Vector3 velocity;
        private float remainingSeconds;
        private PlayerDefenseController defenseController;
        private EnemyRuntimeController enemyRuntime;

        public bool IsKnockbackActive => remainingSeconds > 0f;

        public void Configure(RoomRuntimeRoot room, float radius, bool nextIgnoreObstacles, float nextResistanceMultiplier)
        {
            roomRuntimeRoot = room;
            radiusMeters = Mathf.Max(CombatFeelTuning.MinimumCollisionRadiusMeters, radius);
            ignoreObstacles = nextIgnoreObstacles;
            resistanceMultiplier = Mathf.Max(0f, nextResistanceMultiplier);
            enemyRuntime = GetComponent<EnemyRuntimeController>();
        }

        public void ConfigureStability(int nextStability)
        {
            stability = Mathf.Max(0, nextStability);
            defenseController = GetComponent<PlayerDefenseController>();
        }

        public void ApplyKnockback(Vector3 direction, float meters, float seconds)
        {
            ApplyKnockback(direction, meters, seconds, DamageClassification.PhysicalMelee(ImpactForceClass.Light));
        }

        public void ApplyKnockback(Vector3 direction, float meters, float seconds, DamageClassification classification)
        {
            var enemy = ResolveEnemyRuntime();
            if (enemy != null && (enemy.IsInspectionFrozen || enemy.IsRootedStaticEnemy))
            {
                remainingSeconds = 0f;
                velocity = Vector3.zero;
                return;
            }

            var flatDirection = new Vector3(direction.x, 0f, direction.z);
            var distance = Mathf.Max(0f, meters) * resistanceMultiplier * StabilityKnockbackMultiplier(classification);
            if (flatDirection.sqrMagnitude < 0.001f || distance <= 0f)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0.01f, seconds);
            velocity = flatDirection.normalized * (distance / remainingSeconds);
            enemy?.SyncNavMeshAgentAfterExternalDisplacement("knockback_start");
            VfxPresenter.Play(VfxCueId.KnockbackImpact, transform.position, transform.parent);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            var enemy = ResolveEnemyRuntime();
            if (enemy != null && enemy.IsRootedStaticEnemy)
            {
                remainingSeconds = 0f;
                velocity = Vector3.zero;
                return;
            }

            if (remainingSeconds <= 0f)
            {
                return;
            }

            var stepSeconds = Mathf.Min(Mathf.Max(0f, deltaTime), remainingSeconds);
            remainingSeconds -= stepSeconds;
            var desired = transform.localPosition + velocity * stepSeconds;
            transform.localPosition = ignoreObstacles
                ? RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, desired, radiusMeters)
                : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
            enemy?.SyncNavMeshAgentAfterExternalDisplacement("knockback");

            if (remainingSeconds <= 0f)
            {
                velocity = Vector3.zero;
            }
        }

        private float StabilityKnockbackMultiplier(DamageClassification classification)
        {
            var activeStability = Mathf.Max(0, defenseController != null ? defenseController.ActiveStability : stability);
            var multiplier = classification.ForceClass switch
            {
                ImpactForceClass.Light => activeStability >= 1 ? 0f : 1f,
                ImpactForceClass.Medium => activeStability >= 3 ? 0f : activeStability >= 1 ? 0.65f : 1f,
                ImpactForceClass.Heavy => activeStability >= 5 ? 0.35f : activeStability >= 3 ? 0.65f : activeStability >= 1 ? 0.85f : 1f,
                ImpactForceClass.Massive => activeStability >= 5 ? 0.65f : activeStability >= 3 ? 0.8f : 1f,
                _ => 1f
            };

            if (classification.Channel is DamageChannel.Explosion or DamageChannel.Environmental)
            {
                multiplier = Mathf.Max(0.5f, multiplier);
            }

            return multiplier;
        }

        private EnemyRuntimeController ResolveEnemyRuntime()
        {
            if (enemyRuntime == null)
            {
                enemyRuntime = GetComponent<EnemyRuntimeController>();
            }

            return enemyRuntime;
        }
    }
}
