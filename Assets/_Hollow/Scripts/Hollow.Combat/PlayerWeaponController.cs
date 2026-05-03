using System;
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
        public const float AttackMovementMultiplier = 0.55f;
        public const float RollStaminaCost = 18f;
        public const float RollDurationSeconds = 0.46f;
        public const float RollInvulnerabilitySeconds = 0.32f;
        public const float RollDistanceMeters = 1.9f;

        private enum PlayerAttackExecutionState
        {
            Idle,
            Windup,
            Active,
            Recovery,
            Rolling
        }

        [SerializeField] private float cooldownSeconds = DefaultCooldownSeconds;
        [SerializeField] private float cooldownMultiplier = 1f;
        [SerializeField] private int projectileDamageBonus;
        [SerializeField] private int meleeDamageBonus = 1;
        [SerializeField] private int temporaryDamageBonus;
        [SerializeField] private float meleeRangeBonusMeters;
        [SerializeField] private float rangedRangeBonusMeters;
        [SerializeField] private float attackStaminaCostMultiplier = 1f;
        private ProjectilePassiveState projectilePassiveState = ProjectilePassiveState.Default;
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
        private bool debugLightAttackSpeedDoubled;
        private Vector2 lastAimDirection = Vector2.up;
        private PlayerAttackExecutionState attackExecutionState = PlayerAttackExecutionState.Idle;
        private WeaponSlot pendingAttackSlot = WeaponSlot.Ranged;
        private AttackKind pendingAttackKind = AttackKind.Light;
        private WeaponAttackDefinition pendingAttack;
        private Vector2 pendingAttackDirection = Vector2.up;
        private float attackWindupEndTime;
        private float attackActiveEndTime;
        private float attackRecoveryEndTime;
        private bool pendingAttackApplied;
        private Vector2 rollDirection = Vector2.up;
        private float rollEndTime;
        private float rollInvulnerableEndTime;
        private float lastActionEvaluationTime;
        private CombatantHealth health;

        public float CooldownSeconds => cooldownSeconds * cooldownMultiplier;

        public WeaponSlot ActiveWeaponSlot => activeWeaponSlot;

        public float CurrentStamina => currentStamina;

        public float MaxStamina => maxStamina;

        public string MeleeWeaponId => meleeWeaponId;

        public string RangedWeaponId => rangedWeaponId;

        public string ActiveWeaponDisplayName => ResolveWeapon(activeWeaponSlot)?.DisplayName ?? activeWeaponSlot.ToString();

        public bool DebugLightAttackSpeedDoubled => debugLightAttackSpeedDoubled;

        public bool IsAttackCommitted => attackExecutionState is PlayerAttackExecutionState.Windup
            or PlayerAttackExecutionState.Active
            or PlayerAttackExecutionState.Recovery;

        public bool IsRolling => attackExecutionState == PlayerAttackExecutionState.Rolling;

        public bool IsRollInvulnerable => IsRolling && lastActionEvaluationTime < rollInvulnerableEndTime;

        public Vector2 RollDirection => rollDirection.sqrMagnitude > 0.001f ? rollDirection.normalized : LastAimDirection;

        public float RollSpeedMetersPerSecond => RollDistanceMeters / RollDurationSeconds;

        public WeaponCatalogDefinition WeaponCatalog => weaponCatalog;

        public Vector2 LastAimDirection => lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;

        public float MeleeRangeBonusMeters => meleeRangeBonusMeters;

        public float RangedRangeBonusMeters => rangedRangeBonusMeters;

        public float EffectiveMeleeLightRangeMeters => EffectiveRange(
            ResolveAttack(ResolveWeapon(WeaponSlot.Melee), WeaponSlot.Melee, AttackKind.Light),
            WeaponSlot.Melee);

        public float EffectiveRangedLightRangeMeters => EffectiveRange(
            ResolveAttack(ResolveWeapon(WeaponSlot.Ranged), WeaponSlot.Ranged, AttackKind.Light),
            WeaponSlot.Ranged);

        public event Action<WeaponSlot> ActiveWeaponSlotChanged;

        public event Action<WeaponSlot, AttackKind, Vector2> WeaponAttackVisualRequested;

        private void OnDisable()
        {
            UnbindHealthEvents();
        }

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

        public void ConfigureProjectilePassives(ProjectilePassiveState nextProjectilePassiveState)
        {
            projectilePassiveState = nextProjectilePassiveState.PatternKind == 0
                ? ProjectilePassiveState.Default
                : nextProjectilePassiveState;
        }

        public void SetDebugLightAttackSpeedDoubled(bool enabled)
        {
            debugLightAttackSpeedDoubled = enabled;
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
            float nextRangedRangeBonusMeters = 0f,
            float nextAttackStaminaCostMultiplier = 1f)
        {
            ConfigureStats(nextCooldownMultiplier, nextRangedDamageBonus);
            if (nextWeaponCatalog != null)
            {
                weaponCatalog = nextWeaponCatalog;
            }

            meleeDamageBonus = Mathf.Max(0, nextMeleeDamageBonus);
            meleeRangeBonusMeters = Mathf.Max(0f, nextMeleeRangeBonusMeters);
            rangedRangeBonusMeters = Mathf.Max(0f, nextRangedRangeBonusMeters);
            attackStaminaCostMultiplier = Mathf.Max(0.01f, nextAttackStaminaCostMultiplier);
            maxStamina = Mathf.Max(1f, nextMaxStamina);
            staminaRegenPerSecond = Mathf.Max(0f, nextStaminaRegenPerSecond);
            currentStamina = Mathf.Clamp(nextCurrentStamina <= 0f ? maxStamina : nextCurrentStamina, 0f, maxStamina);
            meleeWeaponId = string.IsNullOrWhiteSpace(nextMeleeWeaponId) ? "starter_blade" : nextMeleeWeaponId;
            rangedWeaponId = string.IsNullOrWhiteSpace(nextRangedWeaponId) ? "starter_bolt" : nextRangedWeaponId;
            SetActiveWeaponSlot(nextActiveWeaponSlot);
        }

        private void Update()
        {
            if (GameplayPauseState.IsPaused)
            {
                return;
            }

            BindHealthEvents();
            var input = GameplayInputReader.ReadCurrent();
            RegenerateStamina(Time.deltaTime);
            TickAction(Time.deltaTime, Time.time);
            if (input.SwapWeaponPressed)
            {
                ToggleWeaponSlot();
            }

            if (input.HasShoot)
            {
                lastAimDirection = input.Shoot;
            }

            if (input.RollPressed && TryRoll(input.Move, CurrentAim(input), Time.time))
            {
                return;
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
            SetActiveWeaponSlot(activeWeaponSlot == WeaponSlot.Ranged ? WeaponSlot.Melee : WeaponSlot.Ranged);
        }

        public void SetActiveWeaponSlot(WeaponSlot slot)
        {
            if (activeWeaponSlot == slot)
            {
                ActiveWeaponSlotChanged?.Invoke(activeWeaponSlot);
                return;
            }

            activeWeaponSlot = slot;
            ActiveWeaponSlotChanged?.Invoke(activeWeaponSlot);
        }

        public bool TryAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(0f, timeSeconds);
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

        public bool TryRoll(Vector2 moveDirection, Vector2 aimDirection, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(0f, timeSeconds);
            if (attackExecutionState != PlayerAttackExecutionState.Idle || IsGuarding || !TrySpendStamina(RollStaminaCost))
            {
                return false;
            }

            var direction = moveDirection.sqrMagnitude > 0.001f
                ? Vector2.ClampMagnitude(moveDirection, 1f)
                : GameplayInputReader.CardinalizeShoot(aimDirection);
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = LastAimDirection;
            }

            rollDirection = direction.normalized;
            rollEndTime = timeSeconds + RollDurationSeconds;
            rollInvulnerableEndTime = timeSeconds + RollInvulnerabilitySeconds;
            lastActionEvaluationTime = timeSeconds;
            attackExecutionState = PlayerAttackExecutionState.Rolling;
            combatController?.EmitPlayerStimulus(EnemyStimulusKind.Roll, transform.localPosition, timeSeconds, EnemyStimulusTier.Normal, "roll");
            return true;
        }

        public void TickAction(float deltaTime, float timeSeconds)
        {
            lastActionEvaluationTime = timeSeconds;
            for (var guard = 0; guard < 5; guard++)
            {
                if (attackExecutionState == PlayerAttackExecutionState.Idle)
                {
                    return;
                }

                if (attackExecutionState == PlayerAttackExecutionState.Rolling)
                {
                    if (timeSeconds < rollEndTime)
                    {
                        return;
                    }

                    ClearPendingAction();
                    return;
                }

                if (attackExecutionState == PlayerAttackExecutionState.Windup)
                {
                    if (timeSeconds < attackWindupEndTime)
                    {
                        return;
                    }

                    attackExecutionState = PlayerAttackExecutionState.Active;
                    ExecutePendingAttack(timeSeconds);
                    continue;
                }

                if (attackExecutionState == PlayerAttackExecutionState.Active)
                {
                    if (timeSeconds < attackActiveEndTime)
                    {
                        return;
                    }

                    attackExecutionState = PlayerAttackExecutionState.Recovery;
                    continue;
                }

                if (attackExecutionState == PlayerAttackExecutionState.Recovery)
                {
                    if (timeSeconds < attackRecoveryEndTime)
                    {
                        return;
                    }

                    ClearPendingAction();
                    return;
                }
            }
        }

        private bool TryFireWithAttack(AttackKind attackKind, Vector2 shootDirection, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(0f, timeSeconds);
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
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                projectilePrefab == null ||
                combatController == null ||
                !TrySpendStamina(AdjustedAttackStaminaCost(attack.StaminaCost)))
            {
                return false;
            }

            var attackCooldown = EffectiveRangedCooldown(attack, attackKind);
            nextAllowedShotTime = timeSeconds + attackCooldown;
            StartPendingAttack(WeaponSlot.Ranged, attackKind, attack, cardinal, timeSeconds);
            return true;
        }

        private void SpawnProjectile(ProjectileShotSpec shot, int attackDamage, float projectileSpeed, float lifetimeSeconds, WeaponAttackDefinition attack)
        {
            var direction = shot.Direction.sqrMagnitude > 0.001f ? shot.Direction.normalized : Vector2.up;
            var side = new Vector2(-direction.y, direction.x);
            var projectileObject = Instantiate(projectilePrefab, transform.parent);
            projectileObject.name = "PlayerProjectile";
            projectileObject.transform.localPosition =
                transform.localPosition +
                new Vector3(direction.x, 0f, direction.y) * 0.42f +
                new Vector3(side.x, 0f, side.y) * shot.LateralOffsetMeters +
                new Vector3(0f, 0.45f, 0f);
            MaterialResolver.ApplyTo(
                projectileObject,
                projectilePassiveState.VisualStyle == ProjectileVisualStyle.RedPower
                    ? MaterialRole.ProjectilePower
                    : MaterialRole.Projectile);
            var projectile = projectileObject.GetComponent<ProjectileController>() ?? projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(
                roomRuntimeRoot,
                combatController,
                new Vector3(direction.x, 0f, direction.y),
                attackDamage,
                projectileSpeed,
                lifetimeSeconds);
            projectile.ConfigureCombatFeel(
                CombatFeelProfileDefinition.Resolve(combatFeelProfile),
                attack.AttackKind == AttackKind.Heavy,
                attack.ImpactForceClass,
                attack.KnockbackMeters);
            VfxPresenter.Play(VfxCueId.ProjectileFire, projectileObject.transform.position, projectileObject.transform.parent);
        }

        private ProjectileShotSpec[] BuildProjectileShots(Vector2 aimDirection)
        {
            var baseDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.up;
            return projectilePassiveState.PatternKind switch
            {
                ProjectilePatternKind.DoubleBarrel => new[]
                {
                    new ProjectileShotSpec(baseDirection, -0.11f),
                    new ProjectileShotSpec(baseDirection, 0.11f)
                },
                ProjectilePatternKind.TripleShot => new[]
                {
                    new ProjectileShotSpec(Rotate(baseDirection, -30f), 0f),
                    new ProjectileShotSpec(baseDirection, 0f),
                    new ProjectileShotSpec(Rotate(baseDirection, 30f), 0f)
                },
                ProjectilePatternKind.QuadShot => new[]
                {
                    new ProjectileShotSpec(baseDirection, -0.09f),
                    new ProjectileShotSpec(baseDirection, 0.09f),
                    new ProjectileShotSpec(Rotate(baseDirection, -30f), 0f),
                    new ProjectileShotSpec(Rotate(baseDirection, 30f), 0f)
                },
                _ => new[] { new ProjectileShotSpec(baseDirection, 0f) }
            };
        }

        private float EffectiveRangedCooldown(WeaponAttackDefinition attack, AttackKind attackKind)
        {
            float cooldown;
            if (attackKind != AttackKind.Light || projectilePassiveState.RangedLightFireRateBonusPerSecond <= 0f)
            {
                cooldown = Mathf.Max(0.05f, attack.CooldownSeconds * cooldownMultiplier);
                return ApplyDebugLightAttackSpeedCooldown(attackKind, cooldown);
            }

            var baseShotsPerSecond = 1f / Mathf.Max(0.05f, attack.CooldownSeconds);
            var effectiveShotsPerSecond = baseShotsPerSecond + projectilePassiveState.RangedLightFireRateBonusPerSecond;
            cooldown = Mathf.Max(0.05f, (1f / effectiveShotsPerSecond) * cooldownMultiplier);
            return ApplyDebugLightAttackSpeedCooldown(attackKind, cooldown);
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos).normalized;
        }

        private readonly struct ProjectileShotSpec
        {
            public ProjectileShotSpec(Vector2 direction, float lateralOffsetMeters)
            {
                Direction = direction;
                LateralOffsetMeters = lateralOffsetMeters;
            }

            public Vector2 Direction { get; }

            public float LateralOffsetMeters { get; }
        }

        private bool TryMeleeAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(0f, timeSeconds);
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
            var cooldown = EffectiveMeleeCooldown(attack, attackKind);
            if (timeSeconds < nextAllowedMeleeTime ||
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                combatController == null ||
                !TrySpendStamina(AdjustedAttackStaminaCost(attack.StaminaCost)))
            {
                return false;
            }

            nextAllowedMeleeTime = timeSeconds + cooldown;
            StartPendingAttack(WeaponSlot.Melee, attackKind, attack, cardinal, timeSeconds);
            return true;
        }

        private void StartPendingAttack(WeaponSlot slot, AttackKind attackKind, WeaponAttackDefinition attack, Vector2 direction, float timeSeconds)
        {
            pendingAttackSlot = slot;
            pendingAttackKind = attackKind;
            pendingAttack = attack;
            pendingAttackDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : LastAimDirection;
            pendingAttackApplied = false;
            attackWindupEndTime = timeSeconds + attack.WindupSeconds;
            attackActiveEndTime = attackWindupEndTime + attack.ActiveSeconds;
            attackRecoveryEndTime = attackActiveEndTime + attack.RecoverySeconds;
            attackExecutionState = PlayerAttackExecutionState.Windup;
        }

        private void ExecutePendingAttack(float timeSeconds)
        {
            if (pendingAttackApplied)
            {
                return;
            }

            pendingAttackApplied = true;
            if (pendingAttackSlot == WeaponSlot.Ranged)
            {
                ExecutePendingRangedAttack(timeSeconds);
                return;
            }

            ExecutePendingMeleeAttack(timeSeconds);
        }

        private void ExecutePendingRangedAttack(float timeSeconds)
        {
            if (projectilePrefab == null || combatController == null)
            {
                return;
            }

            var cardinal = pendingAttackDirection.sqrMagnitude > 0.001f ? pendingAttackDirection : LastAimDirection;
            var attackDamage = Mathf.Max(1, Mathf.RoundToInt((pendingAttack.Damage + projectileDamageBonus + CurrentTemporaryDamageBonus) * projectilePassiveState.RangedDamageMultiplier));
            var effectiveRange = EffectiveRange(pendingAttack, WeaponSlot.Ranged);
            var projectileSpeed = ProjectileController.DefaultSpeedMetersPerSecond;
            var lifetimeSeconds = Mathf.Max(0.1f, effectiveRange / projectileSpeed);
            foreach (var shot in BuildProjectileShots(cardinal))
            {
                SpawnProjectile(shot, attackDamage, projectileSpeed, lifetimeSeconds, pendingAttack);
            }

            combatController.EmitPlayerStimulus(
                EnemyStimulusKind.RangedAttack,
                transform.localPosition,
                timeSeconds,
                RoomCombatController.StimulusTierForPlayerAttack(pendingAttackKind),
                pendingAttackKind == AttackKind.Heavy ? "heavy_ranged" : "light_ranged");
            WeaponAttackVisualRequested?.Invoke(WeaponSlot.Ranged, pendingAttackKind, cardinal);
            AudioPresenter.Play(AudioCueId.ProjectileFire, transform.position);
        }

        private void ExecutePendingMeleeAttack(float timeSeconds)
        {
            if (combatController == null)
            {
                return;
            }

            var cardinal = pendingAttackDirection.sqrMagnitude > 0.001f ? pendingAttackDirection : LastAimDirection;
            var direction = new Vector3(cardinal.x, 0f, cardinal.y).normalized;
            var effectiveRange = EffectiveRange(pendingAttack, WeaponSlot.Melee);
            WeaponAttackVisualRequested?.Invoke(WeaponSlot.Melee, pendingAttackKind, cardinal);
            MeleeSwipePresenter.Spawn(transform.parent, transform.localPosition, direction, effectiveRange, pendingAttackKind);
            combatController.EmitPlayerStimulus(
                EnemyStimulusKind.MeleeAttack,
                transform.localPosition,
                timeSeconds,
                RoomCombatController.StimulusTierForPlayerAttack(pendingAttackKind),
                pendingAttackKind == AttackKind.Heavy ? "heavy_melee" : "light_melee");
            var radius = Mathf.Max(0.25f, effectiveRange * 0.48f);
            var hitCenter = transform.localPosition + direction * Mathf.Max(0.35f, effectiveRange * 0.72f) + new Vector3(0f, CombatFeelTuning.MeleeHitHeightMeters, 0f);
            var target = combatController.FindEnemyHit(hitCenter, radius);
            if (target != null && IsInsideMeleeHitArc(target, direction, pendingAttack.HitArcDegrees))
            {
                var damage = Mathf.Max(1, pendingAttack.Damage + meleeDamageBonus + CurrentTemporaryDamageBonus);
                var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
                DamageSystem.ApplyDamage(
                    target.Health,
                    new DamageRequest(
                        damage,
                        gameObject,
                        DamageFeedbackContext.Knockback(direction, pendingAttack.KnockbackMeters, profile.KnockbackSeconds),
                        DamageClassification.PhysicalMelee(pendingAttack.ImpactForceClass)));
                VfxPresenter.Play(VfxCueId.EnemyHit, target.transform.position, target.transform.parent);
                AudioPresenter.Play(AudioCueId.EnemyHit, target.transform.position);
            }
            else
            {
                var destructible = combatController.FindDestructibleHit(hitCenter, radius);
                if (destructible != null)
                {
                    destructible.TryApplyHit(Mathf.Max(1, pendingAttack.Damage + meleeDamageBonus + CurrentTemporaryDamageBonus), gameObject);
                }
            }
        }

        private bool IsInsideMeleeHitArc(EnemyRuntimeController target, Vector3 direction, float arcDegrees)
        {
            if (target == null)
            {
                return false;
            }

            var toTarget = target.transform.localPosition - transform.localPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            return Vector3.Angle(direction.normalized, toTarget.normalized) <= Mathf.Clamp(arcDegrees, 1f, 360f) * 0.5f;
        }

        private void ClearPendingAction()
        {
            attackExecutionState = PlayerAttackExecutionState.Idle;
            pendingAttackApplied = false;
            attackWindupEndTime = 0f;
            attackActiveEndTime = 0f;
            attackRecoveryEndTime = 0f;
            rollEndTime = 0f;
            rollInvulnerableEndTime = 0f;
        }

        private float EffectiveMeleeCooldown(WeaponAttackDefinition attack, AttackKind attackKind)
        {
            var cooldown = Mathf.Max(0.05f, attack.CooldownSeconds * cooldownMultiplier);
            return ApplyDebugLightAttackSpeedCooldown(attackKind, cooldown);
        }

        private float ApplyDebugLightAttackSpeedCooldown(AttackKind attackKind, float cooldown)
        {
            if (!debugLightAttackSpeedDoubled || attackKind != AttackKind.Light)
            {
                return Mathf.Max(0.05f, cooldown);
            }

            return Mathf.Max(0.05f, cooldown * 0.5f);
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

        private float AdjustedAttackStaminaCost(float amount)
        {
            return Mathf.Max(0f, amount) * Mathf.Max(0.01f, attackStaminaCostMultiplier);
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

        private void BindHealthEvents()
        {
            var nextHealth = GetComponent<CombatantHealth>();
            if (nextHealth == health)
            {
                return;
            }

            UnbindHealthEvents();
            health = nextHealth;
            if (health == null)
            {
                return;
            }

            health.Damaged += OnPlayerDamaged;
            health.Died += OnPlayerDied;
        }

        private void UnbindHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnPlayerDamaged;
            health.Died -= OnPlayerDied;
            health = null;
        }

        private void OnPlayerDamaged(CombatantHealth _)
        {
            if (attackExecutionState != PlayerAttackExecutionState.Rolling)
            {
                ClearPendingAction();
            }
        }

        private void OnPlayerDied(CombatantHealth _)
        {
            ClearPendingAction();
        }
    }
}
