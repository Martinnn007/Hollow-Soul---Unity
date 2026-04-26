using Hollow.Input;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        public const float DefaultCooldownSeconds = 0.22f;

        [SerializeField] private float cooldownSeconds = DefaultCooldownSeconds;
        [SerializeField] private float cooldownMultiplier = 1f;
        [SerializeField] private int projectileDamageBonus;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private RoomCombatController combatController;

        private float nextAllowedShotTime;

        public float CooldownSeconds => cooldownSeconds * cooldownMultiplier;

        public void Configure(RoomRuntimeRoot room, RoomCombatController controller, GameObject prefab)
        {
            roomRuntimeRoot = room;
            combatController = controller;
            projectilePrefab = prefab;
        }

        public void ConfigureStats(float nextCooldownMultiplier, int nextProjectileDamageBonus)
        {
            cooldownMultiplier = nextCooldownMultiplier <= 0f ? 1f : nextCooldownMultiplier;
            projectileDamageBonus = Mathf.Max(0, nextProjectileDamageBonus);
        }

        private void Update()
        {
            var input = GameplayInputReader.ReadCurrent();
            if (input.HasShoot)
            {
                TryFire(input.Shoot, Time.time);
            }
        }

        public bool TryFire(Vector2 shootDirection, float timeSeconds)
        {
            var cardinal = GameplayInputReader.CardinalizeShoot(shootDirection);
            if (cardinal.sqrMagnitude < 0.001f || timeSeconds < nextAllowedShotTime || projectilePrefab == null || combatController == null)
            {
                return false;
            }

            nextAllowedShotTime = timeSeconds + CooldownSeconds;
            var projectileObject = Instantiate(projectilePrefab, transform.parent);
            projectileObject.name = "PlayerProjectile";
            projectileObject.transform.localPosition = transform.localPosition + new Vector3(cardinal.x, 0f, cardinal.y) * 0.42f + new Vector3(0f, 0.45f, 0f);
            MaterialResolver.ApplyTo(projectileObject, MaterialRole.Projectile);
            var projectile = projectileObject.GetComponent<ProjectileController>() ?? projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(roomRuntimeRoot, combatController, new Vector3(cardinal.x, 0f, cardinal.y), projectileDamageBonus);
            VfxPresenter.Play(VfxCueId.ProjectileFire, projectileObject.transform.position, projectileObject.transform.parent);
            AudioPresenter.Play(AudioCueId.ProjectileFire, projectileObject.transform.position);
            return true;
        }
    }
}
