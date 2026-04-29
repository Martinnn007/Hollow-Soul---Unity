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
        [SerializeField] private int temporaryDamageBonus;
        [SerializeField] private float meleeRangeBonusMeters;
        [SerializeField] private float rangedRangeBonusMeters;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaRegenPerSecond = 18f;
        [SerializeField] private WeaponSlot activeWeaponSlot = WeaponSlot.Ranged;
        [SerializeField] private string meleeWeaponId = "starter_blade";
        [SerializeField] private string rangedWeaponId = "starter_bolt";
        [SerializeField] private WeaponCatalogDefinition weaponCatalog;
        [SerializeField] private CombatFeelProfileDefinition combatFeelProfile;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private RoomCombatController combatController;
        [SerializeField] private PlayerDefenseController defenseController;

        private float nextAllowedShotTime;
        private float nextAllowedMeleeTime;
        private float temporaryDamageEndTime;
        private Vector2 lastAimDirection = Vector2.up;

        public float CooldownSeconds => cooldownSeconds * cooldownMultiplier;

        public WeaponSlot ActiveWeaponSlot => activeWeaponSlot;

        public float CurrentStamina => currentStamina;

        public float MaxStamina => maxStamina;

        public string MeleeWeaponId => meleeWeaponId;

        public string RangedWeaponId => rangedWeaponId;

        public string ActiveWeaponDisplayName => ResolveWeapon(activeWeaponSlot)?.DisplayName ?? activeWeaponSlot.ToString();

        public WeaponCatalogDefinition WeaponCatalog => weaponCatalog;

        public float MeleeRangeBonusMeters => meleeRangeBonusMeters;

        public float RangedRangeBonusMeters => rangedRangeBonusMeters;

        public float EffectiveMeleeLightRangeMeters => EffectiveRange(
            ResolveAttack(ResolveWeapon(WeaponSlot.Melee), WeaponSlot.Melee, AttackKind.Light),
            WeaponSlot.Melee);

        public float EffectiveRangedLightRangeMeters => EffectiveRange(
            ResolveAttack(ResolveWeapon(WeaponSlot.Ranged), WeaponSlot.Ranged, AttackKind.Light),
            WeaponSlot.Ranged);

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

        public void ConfigureWeaponCatalog(WeaponCatalogDefinition nextWeaponCatalog)
        {
            weaponCatalog = nextWeaponCatalog;
        }

        public void ConfigureCombatFeel(CombatFeelProfileDefinition profile)
        {
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
        }

        public void ConfigureBuildStats(
            float nextCooldownMultiplier,
            int nextRangedDamageBonus,
            int nextMeleeDamageBonus,
            float nextMaxStamina,
            float nextStaminaRegenPerSecond,
            string nextMeleeWeaponId,
            string nextRangedWeaponId,
            WeaponSlot nextActiveWeaponSlot,
            float nextCurrentStamina,
            WeaponCatalogDefinition nextWeaponCatalog = null,
            float nextMeleeRangeBonusMeters = 0f,
            float nextRangedRangeBonusMeters = 0f)
        {
            ConfigureStats(nextCooldownMultiplier, nextRangedDamageBonus);
            if (nextWeaponCatalog != null)
            {
                weaponCatalog = nextWeaponCatalog;
            }

            meleeDamageBonus = Mathf.Max(0, nextMeleeDamageBonus);
            meleeRangeBonusMeters = Mathf.Max(0f, nextMeleeRangeBonusMeters);
            rangedRangeBonusMeters = Mathf.Max(0f, nextRangedRangeBonusMeters);
            maxStamina = Mathf.Max(1f, nextMaxStamina);
            staminaRegenPerSecond = Mathf.Max(0f, nextStaminaRegenPerSecond);
            currentStamina = Mathf.Clamp(nextCurrentStamina <= 0f ? maxStamina : nextCurrentStamina, 0f, maxStamina);
            meleeWeaponId = string.IsNullOrWhiteSpace(nextMeleeWeaponId) ? "starter_blade" : nextMeleeWeaponId;
            rangedWeaponId = string.IsNullOrWhiteSpace(nextRangedWeaponId) ? "starter_bolt" : nextRangedWeaponId;
            activeWeaponSlot = nextActiveWeaponSlot;
        }

        private void Update()
        {
            if (GameplayPauseState.IsPaused)
            {
                return;
            }

            var input = GameplayInputReader.ReadCurrent();
            RegenerateStamina(Time.deltaTime);
            if (input.SwapWeaponPressed)
            {
                ToggleWeaponSlot();
            }

            if (input.HasShoot)
            {
                lastAimDirection = input.Shoot;
            }

            if (input.GuardHeld)
            {
                return;
            }

            if (input.LightAttackPressed)
            {
                TryAttack(AttackKind.Light, CurrentAim(input), Time.time);
            }

            if (input.HeavyAttackPressed)
            {
                TryAttack(AttackKind.Heavy, CurrentAim(input), Time.time);
            }
        }

        public void ToggleWeaponSlot()
        {
            activeWeaponSlot = activeWeaponSlot == WeaponSlot.Ranged ? WeaponSlot.Melee : WeaponSlot.Ranged;
        }

        public void SetActiveWeaponSlot(WeaponSlot slot)
        {
            activeWeaponSlot = slot;
        }

        public bool TryAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            if (IsGuarding)
            {
                return false;
            }

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
            if (IsGuarding)
            {
                return false;
            }

            var cardinal = GameplayInputReader.CardinalizeShoot(shootDirection);
            if (cardinal.sqrMagnitude < 0.001f)
            {
                cardinal = lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;
            }

            var weapon = ResolveWeapon(WeaponSlot.Ranged);
            var attack = ResolveAttack(weapon, WeaponSlot.Ranged, attackKind);
            if (cardinal.sqrMagnitude < 0.001f ||
                timeSeconds < nextAllowedShotTime ||
                projectilePrefab == null ||
                combatController == null ||
                !TrySpendStamina(attack.StaminaCost))
            {
                return false;
            }

            var attackCooldown = attack.CooldownSeconds * cooldownMultiplier;
            var attackDamage = attack.Damage + projectileDamageBonus + CurrentTemporaryDamageBonus;
            var effectiveRange = EffectiveRange(attack, WeaponSlot.Ranged);
            var projectileSpeed = ProjectileController.DefaultSpeedMetersPerSecond;
            var lifetimeSeconds = Mathf.Max(0.1f, effectiveRange / projectileSpeed);
            nextAllowedShotTime = timeSeconds + attackCooldown;
            var projectileObject = Instantiate(projectilePrefab, transform.parent);
            projectileObject.name = "PlayerProjectile";
            projectileObject.transform.localPosition = transform.localPosition + new Vector3(cardinal.x, 0f, cardinal.y) * 0.42f + new Vector3(0f, 0.45f, 0f);
            MaterialResolver.ApplyTo(projectileObject, MaterialRole.Projectile);
            var projectile = projectileObject.GetComponent<ProjectileController>() ?? projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(
                roomRuntimeRoot,
                combatController,
                new Vector3(cardinal.x, 0f, cardinal.y),
                attackDamage,
                projectileSpeed,
                lifetimeSeconds);
            projectile.ConfigureCombatFeel(
                CombatFeelProfileDefinition.Resolve(combatFeelProfile),
                attackKind == AttackKind.Heavy);
            VfxPresenter.Play(VfxCueId.ProjectileFire, projectileObject.transform.position, projectileObject.transform.parent);
            AudioPresenter.Play(AudioCueId.ProjectileFire, projectileObject.transform.position);
            return true;
        }

        private bool TryMeleeAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            if (IsGuarding)
            {
                return false;
            }

            var cardinal = GameplayInputReader.CardinalizeShoot(attackDirection);
            if (cardinal.sqrMagnitude < 0.001f)
            {
                cardinal = lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;
            }

            var weapon = ResolveWeapon(WeaponSlot.Melee);
            var attack = ResolveAttack(weapon, WeaponSlot.Melee, attackKind);
            var cooldown = attack.CooldownSeconds * cooldownMultiplier;
            if (timeSeconds < nextAllowedMeleeTime || combatController == null || !TrySpendStamina(attack.StaminaCost))
            {
                return false;
            }

            nextAllowedMeleeTime = timeSeconds + Mathf.Max(0.05f, cooldown);
            var direction = new Vector3(cardinal.x, 0f, cardinal.y);
            var effectiveRange = EffectiveRange(attack, WeaponSlot.Melee);
            MeleeSwipePresenter.Spawn(transform.parent, transform.localPosition, direction, effectiveRange, attackKind);
            var radius = Mathf.Max(0.25f, effectiveRange * 0.48f);
            var hitCenter = transform.localPosition + direction * Mathf.Max(0.35f, effectiveRange * 0.72f) + new Vector3(0f, CombatFeelTuning.MeleeHitHeightMeters, 0f);
            var target = combatController.FindEnemyHit(hitCenter, radius);
            if (target != null)
            {
                var damage = Mathf.Max(1, attack.Damage + meleeDamageBonus + CurrentTemporaryDamageBonus);
                var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
                var knockback = profile.EnemyMeleeKnockbackMeters *
                                (attackKind == AttackKind.Heavy ? profile.HeavyAttackKnockbackMultiplier : 1f);
                DamageSystem.ApplyDamage(
                    target.Health,
                    new DamageRequest(
                        damage,
                        gameObject,
                        DamageFeedbackContext.Knockback(direction, knockback, profile.KnockbackSeconds)));
                VfxPresenter.Play(VfxCueId.EnemyHit, target.transform.position, target.transform.parent);
                AudioPresenter.Play(AudioCueId.EnemyHit, target.transform.position);
            }
            else
            {
                var destructible = combatController.FindDestructibleHit(hitCenter, radius);
                if (destructible != null)
                {
                    destructible.TryApplyHit(Mathf.Max(1, attack.Damage + meleeDamageBonus + CurrentTemporaryDamageBonus), gameObject);
                }
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

        public bool SpendStaminaForDefense(float amount)
        {
            return TrySpendStamina(amount);
        }

        private void RegenerateStamina(float deltaTime)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + Mathf.Max(0f, deltaTime) * staminaRegenPerSecond);
        }

        private Vector2 CurrentAim(GameplayInputSnapshot input)
        {
            if (input.HasShoot)
            {
                return input.Shoot;
            }

            return lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;
        }

        private WeaponDefinition ResolveWeapon(WeaponSlot slot)
        {
            var weaponId = slot == WeaponSlot.Melee ? meleeWeaponId : rangedWeaponId;
            return weaponCatalog != null ? weaponCatalog.Resolve(weaponId, slot) : null;
        }

        private bool IsGuarding
        {
            get
            {
                if (defenseController == null)
                {
                    defenseController = GetComponent<PlayerDefenseController>();
                }

                return defenseController != null && defenseController.IsGuarding;
            }
        }

        public void ApplyTemporaryDamageBonus(int damageBonus, float durationSeconds)
        {
            temporaryDamageBonus = Mathf.Max(0, damageBonus);
            temporaryDamageEndTime = Time.time + Mathf.Max(0f, durationSeconds);
        }

        private int CurrentTemporaryDamageBonus => Time.time < temporaryDamageEndTime ? temporaryDamageBonus : 0;

        private static WeaponAttackDefinition ResolveAttack(WeaponDefinition weapon, WeaponSlot slot, AttackKind attackKind)
        {
            if (weapon != null)
            {
                return attackKind == AttackKind.Heavy ? weapon.HeavyAttack : weapon.LightAttack;
            }

            return attackKind == AttackKind.Heavy
                ? WeaponAttackDefinition.DefaultHeavy(slot)
                : WeaponAttackDefinition.DefaultLight(slot);
        }

        private float EffectiveRange(WeaponAttackDefinition attack, WeaponSlot slot)
        {
            var rangeBonus = slot == WeaponSlot.Melee ? meleeRangeBonusMeters : rangedRangeBonusMeters;
            return Mathf.Max(0.1f, attack.RangeMeters + rangeBonus);
        }
    }
}
