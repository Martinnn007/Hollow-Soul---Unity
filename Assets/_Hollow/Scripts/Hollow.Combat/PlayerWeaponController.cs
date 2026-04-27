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
        [SerializeField] private int meleeDamageBonus = 1;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaRegenPerSecond = 18f;
        [SerializeField] private WeaponSlot activeWeaponSlot = WeaponSlot.Ranged;
        [SerializeField] private string meleeWeaponId = "starter_blade";
        [SerializeField] private string rangedWeaponId = "starter_bolt";
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private RoomCombatController combatController;

        private float nextAllowedShotTime;
        private float nextAllowedMeleeTime;

        public float CooldownSeconds => cooldownSeconds * cooldownMultiplier;

        public WeaponSlot ActiveWeaponSlot => activeWeaponSlot;

        public float CurrentStamina => currentStamina;

        public float MaxStamina => maxStamina;

        public string MeleeWeaponId => meleeWeaponId;

        public string RangedWeaponId => rangedWeaponId;

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

        public void ConfigureBuildStats(
            float nextCooldownMultiplier,
            int nextRangedDamageBonus,
            int nextMeleeDamageBonus,
            float nextMaxStamina,
            float nextStaminaRegenPerSecond,
            string nextMeleeWeaponId,
            string nextRangedWeaponId,
            float nextCurrentStamina)
        {
            ConfigureStats(nextCooldownMultiplier, nextRangedDamageBonus);
            meleeDamageBonus = Mathf.Max(0, nextMeleeDamageBonus);
            maxStamina = Mathf.Max(1f, nextMaxStamina);
            staminaRegenPerSecond = Mathf.Max(0f, nextStaminaRegenPerSecond);
            currentStamina = Mathf.Clamp(nextCurrentStamina <= 0f ? maxStamina : nextCurrentStamina, 0f, maxStamina);
            meleeWeaponId = string.IsNullOrWhiteSpace(nextMeleeWeaponId) ? "starter_blade" : nextMeleeWeaponId;
            rangedWeaponId = string.IsNullOrWhiteSpace(nextRangedWeaponId) ? "starter_bolt" : nextRangedWeaponId;
        }

        private void Update()
        {
            var input = GameplayInputReader.ReadCurrent();
            RegenerateStamina(Time.deltaTime);
            if (input.SwapWeaponPressed)
            {
                ToggleWeaponSlot();
            }

            if (input.LightAttackPressed)
            {
                TryAttack(AttackKind.Light, input.HasShoot ? input.Shoot : Vector2.up, Time.time);
            }

            if (input.HeavyAttackPressed)
            {
                TryAttack(AttackKind.Heavy, input.HasShoot ? input.Shoot : Vector2.up, Time.time);
            }

            if (input.HasShoot)
            {
                TryFire(input.Shoot, Time.time);
            }
        }

        public void ToggleWeaponSlot()
        {
            activeWeaponSlot = activeWeaponSlot == WeaponSlot.Ranged ? WeaponSlot.Melee : WeaponSlot.Ranged;
        }

        public bool TryAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            return activeWeaponSlot == WeaponSlot.Melee
                ? TryMeleeAttack(attackKind, attackDirection, timeSeconds)
                : TryFireWithAttack(attackKind, attackDirection, timeSeconds);
        }

        public bool TryFire(Vector2 shootDirection, float timeSeconds)
        {
            return TryFireWithAttack(AttackKind.Light, shootDirection, timeSeconds);
        }

        private bool TryFireWithAttack(AttackKind attackKind, Vector2 shootDirection, float timeSeconds)
        {
            var cardinal = GameplayInputReader.CardinalizeShoot(shootDirection);
            var staminaCost = attackKind == AttackKind.Heavy ? 12f : 0f;
            if (cardinal.sqrMagnitude < 0.001f ||
                timeSeconds < nextAllowedShotTime ||
                projectilePrefab == null ||
                combatController == null ||
                !TrySpendStamina(staminaCost))
            {
                return false;
            }

            var attackCooldown = attackKind == AttackKind.Heavy ? CooldownSeconds * 2.15f : CooldownSeconds;
            var attackDamageBonus = projectileDamageBonus + (attackKind == AttackKind.Heavy ? 1 : 0);
            nextAllowedShotTime = timeSeconds + attackCooldown;
            var projectileObject = Instantiate(projectilePrefab, transform.parent);
            projectileObject.name = "PlayerProjectile";
            projectileObject.transform.localPosition = transform.localPosition + new Vector3(cardinal.x, 0f, cardinal.y) * 0.42f + new Vector3(0f, 0.45f, 0f);
            MaterialResolver.ApplyTo(projectileObject, MaterialRole.Projectile);
            var projectile = projectileObject.GetComponent<ProjectileController>() ?? projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(roomRuntimeRoot, combatController, new Vector3(cardinal.x, 0f, cardinal.y), attackDamageBonus);
            VfxPresenter.Play(VfxCueId.ProjectileFire, projectileObject.transform.position, projectileObject.transform.parent);
            AudioPresenter.Play(AudioCueId.ProjectileFire, projectileObject.transform.position);
            return true;
        }

        private bool TryMeleeAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            var cardinal = GameplayInputReader.CardinalizeShoot(attackDirection);
            if (cardinal.sqrMagnitude < 0.001f)
            {
                cardinal = Vector2.up;
            }

            var cooldown = attackKind == AttackKind.Heavy ? 0.55f * cooldownMultiplier : 0.28f * cooldownMultiplier;
            var staminaCost = attackKind == AttackKind.Heavy ? 18f : 6f;
            if (timeSeconds < nextAllowedMeleeTime || combatController == null || !TrySpendStamina(staminaCost))
            {
                return false;
            }

            nextAllowedMeleeTime = timeSeconds + Mathf.Max(0.05f, cooldown);
            var direction = new Vector3(cardinal.x, 0f, cardinal.y);
            var hitCenter = transform.localPosition + direction * (attackKind == AttackKind.Heavy ? 0.82f : 0.68f) + new Vector3(0f, 0.45f, 0f);
            var radius = attackKind == AttackKind.Heavy ? 0.64f : 0.48f;
            var target = combatController.FindEnemyHit(hitCenter, radius);
            if (target != null)
            {
                var damage = Mathf.Max(1, meleeDamageBonus + (attackKind == AttackKind.Heavy ? 1 : 0));
                DamageSystem.ApplyDamage(target.Health, new DamageRequest(damage, gameObject));
                VfxPresenter.Play(VfxCueId.EnemyHit, target.transform.position, target.transform.parent);
                AudioPresenter.Play(AudioCueId.EnemyHit, target.transform.position);
            }

            return true;
        }

        private bool TrySpendStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (currentStamina + 0.001f < amount)
            {
                return false;
            }

            currentStamina -= amount;
            return true;
        }

        private void RegenerateStamina(float deltaTime)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + Mathf.Max(0f, deltaTime) * staminaRegenPerSecond);
        }
    }
}
