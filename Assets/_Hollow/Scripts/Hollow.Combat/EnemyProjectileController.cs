using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyProjectileController : MonoBehaviour
    {
        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private CombatantHealth playerHealth;
        private Vector3 localDirection = Vector3.forward;
        private float speedMetersPerSecond = 5f;
        private float lifetimeSeconds = 2f;
        private float ageSeconds;
        private int damage = 1;
        private float hitRadiusMeters = 0.24f;

        public int Damage => damage;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public void Configure(
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            Vector3 direction,
            int nextDamage,
            float nextSpeedMetersPerSecond,
            float nextLifetimeSeconds = 2f)
        {
            roomRuntimeRoot = room;
            playerController = player;
            playerHealth = playerController != null ? playerController.GetComponent<CombatantHealth>() : null;
            localDirection = direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
            damage = Mathf.Max(0, nextDamage);
            speedMetersPerSecond = Mathf.Max(0.1f, nextSpeedMetersPerSecond);
            lifetimeSeconds = Mathf.Max(0.1f, nextLifetimeSeconds);
            ageSeconds = 0f;
            MaterialResolver.ApplyTo(gameObject, MaterialRole.EnemyProjectile);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.EnemyProjectile, transform, Vector3.zero, Vector3.one);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool Tick(float deltaTime)
        {
            ageSeconds += Mathf.Max(0f, deltaTime);
            transform.localPosition += localDirection * speedMetersPerSecond * deltaTime;

            if (ageSeconds >= lifetimeSeconds ||
                RoomLocalCollision.IsOutsideBounds(roomRuntimeRoot, transform.localPosition, hitRadiusMeters) ||
                RoomLocalCollision.IntersectsObstacle(roomRuntimeRoot, transform.localPosition, hitRadiusMeters))
            {
                DestroyProjectile();
                return false;
            }

            if (playerHealth != null && playerHealth.IsAlive && playerController != null)
            {
                var playerPosition = playerController.transform.localPosition;
                playerPosition.y = transform.localPosition.y;
                if (Vector3.Distance(playerPosition, transform.localPosition) <= hitRadiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.08f)
                {
                    if (DamageSystem.ApplyDamage(playerHealth, new DamageRequest(damage, gameObject)))
                    {
                        VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                        AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
                    }

                    DestroyProjectile();
                    return false;
                }
            }

            return true;
        }

        private void DestroyProjectile()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
