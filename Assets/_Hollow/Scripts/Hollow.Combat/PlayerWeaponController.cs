using System;
using Hollow.Core;
using Hollow.Input;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public enum PlayerRollPhase
    {
        None = 0,
        Startup = 1,
        InvulnerableTravel = 2,
        Recovery = 3
    }

    public sealed class PlayerWeaponController : MonoBehaviour
    {
        public const float DefaultCooldownSeconds = 0.22f;
        public const float PlayerProjectileSpeedMetersPerSecond = ProjectileController.DefaultSpeedMetersPerSecond;
        public const float AttackMovementMultiplier = 0.55f;
        public const float RollStaminaCost = M135CombatReadinessPolicy.LockedRollStaminaCost;
        public const float StaminaRegenDelaySeconds = M135CombatReadinessPolicy.LockedStaminaRegenDelaySeconds;
        public const float GuardHeldStaminaRegenMultiplier = 0.25f;
        public const float RollStartupSeconds = M135CombatReadinessPolicy.LockedRollStartupSeconds;
        public const float RollInvulnerabilitySeconds = M135CombatReadinessPolicy.LockedRollInvulnerabilitySeconds;
        public const float RollRecoverySeconds = M135CombatReadinessPolicy.LockedRollRecoverySeconds;
        public const float RollDurationSeconds = RollStartupSeconds + RollInvulnerabilitySeconds + RollRecoverySeconds;
        public const float RollTravelSeconds = RollStartupSeconds + RollInvulnerabilitySeconds;
        public const float RollDistanceMeters = M135CombatReadinessPolicy.LockedRollDistanceMeters;

        private enum PlayerAttackExecutionState
        {
            Idle,
            Windup,
            Active,
            Recovery,
            RollStartup,
            RollInvulnerableTravel,
            RollRecovery
        }

        private enum StaminaSpendKind
        {
            Action,
            GuardDrain,
            GuardHit
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
        [SerializeField] private float staminaRegenPerSecond = 11f;
        [SerializeField] private WeaponSlot activeWeaponSlot = WeaponSlot.Melee;
        [SerializeField] private string meleeWeaponId = "starter_blade";
        [SerializeField] private string rangedWeaponId = WeaponIdAliases.StarterPistolId;
        [SerializeField] private WeaponCatalogDefinition weaponCatalog;
        [SerializeField] private CombatFeelProfileDefinition combatFeelProfile;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private RoomCombatController combatController;
        [SerializeField] private PlayerDefenseController defenseController;
        [SerializeField] private PlayerAimLockController aimLockController;

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
        private EnemyRuntimeController pendingAttackTarget;
        private PlayerAimAssistResult pendingAttackAssistResult;
        private float attackWindupEndTime;
        private float attackActiveEndTime;
        private float attackRecoveryEndTime;
        private bool pendingAttackApplied;
        private bool rangedDrawActive;
        private AttackKind rangedDrawAttackKind = AttackKind.Light;
        private WeaponAttackDefinition rangedDrawAttack;
        private Vector2 rangedDrawDirection = Vector2.up;
        private float rangedDrawStartTime;
        private float rangedDrawRequiredSeconds;
        private Vector2 rollDirection = Vector2.up;
        private float rollInvulnerableStartTime;
        private float rollEndTime;
        private float rollInvulnerableEndTime;
        private float lastActionEvaluationTime;
        private float staminaRegenBlockedUntil;
        private CombatantHealth health;

        public float CooldownSeconds => cooldownSeconds * cooldownMultiplier;

        public WeaponSlot ActiveWeaponSlot => activeWeaponSlot;

        public float CurrentStamina => currentStamina;

        public float MaxStamina => maxStamina;

        public string MeleeWeaponId => meleeWeaponId;

        public string RangedWeaponId => rangedWeaponId;

        public string ActiveWeaponDisplayName => ResolveWeapon(activeWeaponSlot)?.DisplayName ?? activeWeaponSlot.ToString();

        public bool DebugLightAttackSpeedDoubled => debugLightAttackSpeedDoubled;

        public bool IsAttackCommitted => rangedDrawActive ||
            attackExecutionState is PlayerAttackExecutionState.Windup
            or PlayerAttackExecutionState.Active
            or PlayerAttackExecutionState.Recovery;

        public bool HasVisualAimCommitment => IsAttackCommitted;

        public Vector2 VisualAimDirection
        {
            get
            {
                if (rangedDrawActive)
                {
                    return rangedDrawDirection.sqrMagnitude > 0.001f ? rangedDrawDirection.normalized : LastAimDirection;
                }

                if (attackExecutionState is PlayerAttackExecutionState.Windup
                    or PlayerAttackExecutionState.Active
                    or PlayerAttackExecutionState.Recovery)
                {
                    return pendingAttackDirection.sqrMagnitude > 0.001f
                        ? pendingAttackDirection.normalized
                        : LastAimDirection;
                }

                return LastAimDirection;
            }
        }

        public bool IsRangedDrawActive => rangedDrawActive;

        public float RangedDrawProgress01 => rangedDrawActive
            ? Mathf.Clamp01((lastActionEvaluationTime - rangedDrawStartTime) / Mathf.Max(0.01f, rangedDrawRequiredSeconds))
            : 0f;

        public string RangedDrawDebugLine
        {
            get
            {
                if (!rangedDrawActive)
                {
                    return "Bow draw: --";
                }

                return $"Bow draw {rangedDrawAttackKind} | {RangedDrawProgress01 * 100f:0}% | release";
            }
        }

        public bool IsRolling => CurrentRollPhase != PlayerRollPhase.None;

        public bool IsRollTraveling => CurrentRollPhase is PlayerRollPhase.Startup or PlayerRollPhase.InvulnerableTravel;

        public bool IsRollInvulnerable => IsRollInvulnerableAt(lastActionEvaluationTime);

        public PlayerRollPhase CurrentRollPhase => attackExecutionState switch
        {
            PlayerAttackExecutionState.RollStartup => PlayerRollPhase.Startup,
            PlayerAttackExecutionState.RollInvulnerableTravel => PlayerRollPhase.InvulnerableTravel,
            PlayerAttackExecutionState.RollRecovery => PlayerRollPhase.Recovery,
            _ => PlayerRollPhase.None
        };

        public Vector2 RollDirection => rollDirection.sqrMagnitude > 0.001f ? rollDirection.normalized : LastAimDirection;

        public float RollSpeedMetersPerSecond => RollDistanceMeters / RollTravelSeconds;

        public string RollDebugLine
        {
            get
            {
                var phase = CurrentRollPhase;
                if (phase == PlayerRollPhase.None)
                {
                    return $"Roll ready | STA {CurrentStamina:0}/{MaxStamina:0}";
                }

                return $"Roll {phase} | i-frame {(IsRollInvulnerable ? "ON" : "off")} | {RollPhaseRemainingSeconds:0.00}s";
            }
        }

        public float RollPhaseRemainingSeconds
        {
            get
            {
                var now = lastActionEvaluationTime;
                return CurrentRollPhase switch
                {
                    PlayerRollPhase.Startup => Mathf.Max(0f, rollInvulnerableStartTime - now),
                    PlayerRollPhase.InvulnerableTravel => Mathf.Max(0f, rollInvulnerableEndTime - now),
                    PlayerRollPhase.Recovery => Mathf.Max(0f, rollEndTime - now),
                    _ => 0f
                };
            }
        }

        public WeaponCatalogDefinition WeaponCatalog => weaponCatalog;

        public Vector2 LastAimDirection => lastAimDirection.sqrMagnitude > 0.001f
            ? lastAimDirection.normalized
            : aimLockController != null
                ? aimLockController.AttackDirection
                : Vector2.up;

        public float MeleeRangeBonusMeters => meleeRangeBonusMeters;

        public float RangedRangeBonusMeters => rangedRangeBonusMeters;

        public float EffectiveMeleeLightRangeMeters => EffectiveRange(
            ResolveAttack(ResolveWeapon(WeaponSlot.Melee), WeaponSlot.Melee, AttackKind.Light),
            WeaponSlot.Melee);

        public float EffectiveRangedLightRangeMeters => EffectiveRange(
            ResolveAttack(ResolveWeapon(WeaponSlot.Ranged), WeaponSlot.Ranged, AttackKind.Light),
            WeaponSlot.Ranged);

        public event Action<WeaponSlot> ActiveWeaponSlotChanged;

        public event Action<WeaponSlot, AttackKind, Vector2, float> WeaponActionAnimationRequested;

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
            EnsureAimLockController();
            aimLockController.Configure(controller);
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
            staminaRegenBlockedUntil = 0f;
            meleeWeaponId = string.IsNullOrWhiteSpace(nextMeleeWeaponId) ? "starter_blade" : nextMeleeWeaponId;
            rangedWeaponId = string.IsNullOrWhiteSpace(nextRangedWeaponId)
                ? WeaponIdAliases.StarterPistolId
                : WeaponIdAliases.Normalize(nextRangedWeaponId);
            SetActiveWeaponSlot(nextActiveWeaponSlot);
        }

        private void Update()
        {
            if (GameplayPauseState.IsPaused || GameplayTransitionState.IsLocked)
            {
                return;
            }

            var input = GameplayInputReader.ReadCurrent(ResolveGameplayRoot());
            TickInput(input, Time.deltaTime, Time.time);
        }

        public void TickInput(GameplayInputSnapshot input, float deltaTime, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(deltaTime, timeSeconds);
            RegenerateStamina(deltaTime, timeSeconds);
            EnsureAimLockController();
            aimLockController.Configure(combatController);
            aimLockController.TickAim(input, timeSeconds);
            if (rangedDrawActive)
            {
                HandleRangedDrawInput(input, timeSeconds);
                return;
            }

            if (input.SwapWeaponPressed)
            {
                ToggleWeaponSlot();
            }

            lastAimDirection = CurrentAim(input, timeSeconds);

            if (input.RollPressed && TryRoll(input.Move, CurrentAim(input, timeSeconds), timeSeconds))
            {
                return;
            }

            if (input.GuardHeld)
            {
                return;
            }

            if (input.LightAttackPressed)
            {
                TryAttack(AttackKind.Light, CurrentAim(input, timeSeconds), timeSeconds);
            }

            if (input.HeavyAttackPressed)
            {
                TryAttack(AttackKind.Heavy, CurrentAim(input, timeSeconds), timeSeconds);
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

            CancelRangedDraw();
            activeWeaponSlot = slot;
            ActiveWeaponSlotChanged?.Invoke(activeWeaponSlot);
        }

        public bool TryAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(0f, timeSeconds);
            if (IsGuarding)
            {
                CancelRangedDraw();
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
            if (rangedDrawActive ||
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                IsGuarding ||
                !TrySpendStamina(RollStaminaCost, timeSeconds, StaminaSpendKind.Action))
            {
                return false;
            }

            var direction = ResolveRollDirection(moveDirection, aimDirection);
            rollDirection = direction.normalized;
            rollInvulnerableStartTime = timeSeconds + RollStartupSeconds;
            rollInvulnerableEndTime = rollInvulnerableStartTime + RollInvulnerabilitySeconds;
            rollEndTime = timeSeconds + RollDurationSeconds;
            lastActionEvaluationTime = timeSeconds;
            attackExecutionState = PlayerAttackExecutionState.RollStartup;
            combatController?.EmitPlayerStimulus(EnemyStimulusKind.Roll, transform.localPosition, timeSeconds, EnemyStimulusTier.Normal, "roll");
            return true;
        }

        private Vector2 ResolveRollDirection(Vector2 moveDirection, Vector2 aimDirection)
        {
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                return Vector2.ClampMagnitude(moveDirection, 1f).normalized;
            }

            var direction = GameplayInputReader.NormalizeAimDirection(aimDirection);
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = LastAimDirection;
            }

            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
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

                if (attackExecutionState == PlayerAttackExecutionState.RollStartup)
                {
                    if (timeSeconds < rollInvulnerableStartTime)
                    {
                        return;
                    }

                    attackExecutionState = PlayerAttackExecutionState.RollInvulnerableTravel;
                    continue;
                }

                if (attackExecutionState == PlayerAttackExecutionState.RollInvulnerableTravel)
                {
                    if (timeSeconds < rollInvulnerableEndTime)
                    {
                        return;
                    }

                    attackExecutionState = PlayerAttackExecutionState.RollRecovery;
                    continue;
                }

                if (attackExecutionState == PlayerAttackExecutionState.RollRecovery)
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
                CancelRangedDraw();
                return false;
            }

            var aim = GameplayInputReader.NormalizeAimDirection(shootDirection);
            if (aim.sqrMagnitude < 0.001f)
            {
                aim = lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;
            }

            var weapon = ResolveWeapon(WeaponSlot.Ranged);
            var attack = ResolveAttack(weapon, WeaponSlot.Ranged, attackKind);
            if (weapon != null && weapon.RangedFireMode == WeaponRangedFireMode.DrawAndRelease)
            {
                return TryStartRangedDraw(attackKind, attack, aim, timeSeconds);
            }

            if (aim.sqrMagnitude < 0.001f ||
                timeSeconds < nextAllowedShotTime ||
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                projectilePrefab == null ||
                combatController == null ||
                !TrySpendStamina(AdjustedAttackStaminaCost(attack.StaminaCost), timeSeconds, StaminaSpendKind.Action))
            {
                return false;
            }

            if (attackKind == AttackKind.Light)
            {
                nextAllowedShotTime = timeSeconds + EffectiveRangedCooldown(attack, attackKind);
            }

            var assist = ResolveAttackAssist(WeaponSlot.Ranged, attackKind, attack, aim, timeSeconds);
            StartPendingAttack(WeaponSlot.Ranged, attackKind, attack, assist.Direction, timeSeconds, assist.Target, assist);
            return true;
        }

        private bool TryStartRangedDraw(AttackKind attackKind, WeaponAttackDefinition attack, Vector2 aim, float timeSeconds)
        {
            if (aim.sqrMagnitude < 0.001f ||
                timeSeconds < nextAllowedShotTime ||
                rangedDrawActive ||
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                projectilePrefab == null ||
                combatController == null)
            {
                return false;
            }

            rangedDrawActive = true;
            rangedDrawAttackKind = attackKind;
            rangedDrawAttack = attack;
            rangedDrawDirection = aim.normalized;
            rangedDrawStartTime = timeSeconds;
            rangedDrawRequiredSeconds = RequiredDrawSeconds(attack);
            lastAimDirection = rangedDrawDirection;
            lastActionEvaluationTime = timeSeconds;
            return true;
        }

        private void HandleRangedDrawInput(GameplayInputSnapshot input, float timeSeconds)
        {
            lastAimDirection = CurrentAim(input, timeSeconds);
            if (lastAimDirection.sqrMagnitude > 0.001f)
            {
                rangedDrawDirection = lastAimDirection.normalized;
            }

            if (input.SwapWeaponPressed || input.RollPressed || input.GuardHeld)
            {
                CancelRangedDraw();
                return;
            }

            var release = rangedDrawAttackKind == AttackKind.Heavy
                ? input.HeavyAttackReleased
                : input.LightAttackReleased;
            if (!release)
            {
                return;
            }

            TryReleaseRangedDraw(CurrentAim(input, timeSeconds), timeSeconds);
        }

        public bool TryReleaseRangedDraw(Vector2 releaseDirection, float timeSeconds)
        {
            TickAction(0f, timeSeconds);
            if (!rangedDrawActive)
            {
                return false;
            }

            if (timeSeconds - rangedDrawStartTime + 0.001f < rangedDrawRequiredSeconds)
            {
                CancelRangedDraw();
                return false;
            }

            var aim = GameplayInputReader.NormalizeAimDirection(releaseDirection);
            if (aim.sqrMagnitude < 0.001f)
            {
                aim = rangedDrawDirection.sqrMagnitude > 0.001f ? rangedDrawDirection : LastAimDirection;
            }

            if (timeSeconds < nextAllowedShotTime ||
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                projectilePrefab == null ||
                combatController == null ||
                !TrySpendStamina(AdjustedAttackStaminaCost(rangedDrawAttack.StaminaCost), timeSeconds, StaminaSpendKind.Action))
            {
                CancelRangedDraw();
                return false;
            }

            var attackKind = rangedDrawAttackKind;
            var attack = rangedDrawAttack;
            var assist = ResolveAttackAssist(WeaponSlot.Ranged, attackKind, attack, aim, timeSeconds);
            CancelRangedDraw();
            if (attackKind == AttackKind.Light)
            {
                nextAllowedShotTime = timeSeconds + EffectiveRangedCooldown(attack, attackKind);
            }

            StartPendingAttack(WeaponSlot.Ranged, attackKind, attack, assist.Direction, timeSeconds, assist.Target, assist);
            return true;
        }

        private void CancelRangedDraw()
        {
            rangedDrawActive = false;
            rangedDrawAttackKind = AttackKind.Light;
            rangedDrawAttack = default;
            rangedDrawDirection = Vector2.up;
            rangedDrawStartTime = 0f;
            rangedDrawRequiredSeconds = 0f;
            pendingAttackTarget = null;
            pendingAttackAssistResult = default;
        }

        private static float RequiredDrawSeconds(WeaponAttackDefinition attack)
        {
            return Mathf.Max(0.01f, attack.RequiredDrawSeconds > 0f ? attack.RequiredDrawSeconds : 1f);
        }

        private PlayerAimAssistResult ResolveAttackAssist(
            WeaponSlot slot,
            AttackKind attackKind,
            WeaponAttackDefinition attack,
            Vector2 requestedDirection,
            float timeSeconds)
        {
            if (aimLockController == null)
            {
                aimLockController = GetComponent<PlayerAimLockController>();
            }

            if (aimLockController == null)
            {
                return PlayerAimAssistResult.None(requestedDirection);
            }

            if (combatController != null)
            {
                aimLockController.Configure(combatController);
            }

            return aimLockController.ResolveAttackAssist(
                requestedDirection,
                EffectiveRange(attack, slot),
                slot == WeaponSlot.Melee,
                timeSeconds);
        }

        private void SpawnProjectile(
            ProjectileShotSpec shot,
            int attackDamage,
            float projectileSpeed,
            float lifetimeSeconds,
            WeaponAttackDefinition attack,
            PlayerShotAimSource aimSource,
            EnemyRuntimeController shotTarget,
            string lockedTargetName,
            float lockedTargetDistanceMeters,
            float shotTimeSeconds)
        {
            var direction = shot.Direction.sqrMagnitude > 0.001f ? shot.Direction.normalized : Vector2.up;
            var side = new Vector2(-direction.y, direction.x);
            var projectileObject = HollowRuntimePool.Rent(projectilePrefab, transform.parent);
            projectileObject.name = "PlayerProjectile";
            var projectileTransform = projectileObject.transform;
            var prefabScale = projectilePrefab != null ? projectilePrefab.transform.localScale : Vector3.one;
            projectileTransform.localScale = prefabScale == Vector3.zero ? Vector3.one : prefabScale;
            var projectileLocalPosition =
                transform.localPosition +
                new Vector3(direction.x, 0f, direction.y) * 0.42f +
                new Vector3(side.x, 0f, side.y) * shot.LateralOffsetMeters +
                new Vector3(0f, 0.45f, 0f);
            direction = ResolveProjectileDirectionFromOrigin(
                direction,
                shot.SpreadAngleDegrees,
                shotTarget,
                projectileLocalPosition);
            projectileTransform.localPosition = projectileLocalPosition;
            projectileTransform.localRotation = PlayerWeaponVisualPosePolicy.AimRotation(direction);
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
                lifetimeSeconds,
                gameObject);
            projectile.ConfigureCombatFeel(
                CombatFeelProfileDefinition.Resolve(combatFeelProfile),
                attack.AttackKind == AttackKind.Heavy,
                attack.ImpactForceClass,
                attack.KnockbackMeters);
            PlayerAimShotTelemetry.RecordShot(
                aimSource,
                lockedTargetName,
                lockedTargetDistanceMeters,
                projectileSpeed,
                Time.deltaTime,
                direction,
                shotTimeSeconds);
            VfxPresenter.Play(VfxCueId.ProjectileFire, projectileObject.transform.position, projectileObject.transform.parent);
        }

        private ProjectileShotSpec[] BuildProjectileShots(Vector2 aimDirection)
        {
            var baseDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.up;
            return projectilePassiveState.PatternKind switch
            {
                ProjectilePatternKind.DoubleBarrel => new[]
                {
                    new ProjectileShotSpec(baseDirection, -0.11f, 0f),
                    new ProjectileShotSpec(baseDirection, 0.11f, 0f)
                },
                ProjectilePatternKind.TripleShot => new[]
                {
                    new ProjectileShotSpec(Rotate(baseDirection, -30f), 0f, -30f),
                    new ProjectileShotSpec(baseDirection, 0f, 0f),
                    new ProjectileShotSpec(Rotate(baseDirection, 30f), 0f, 30f)
                },
                ProjectilePatternKind.QuadShot => new[]
                {
                    new ProjectileShotSpec(baseDirection, -0.09f, 0f),
                    new ProjectileShotSpec(baseDirection, 0.09f, 0f),
                    new ProjectileShotSpec(Rotate(baseDirection, -30f), 0f, -30f),
                    new ProjectileShotSpec(Rotate(baseDirection, 30f), 0f, 30f)
                },
                _ => new[] { new ProjectileShotSpec(baseDirection, 0f, 0f) }
            };
        }

        private Vector2 ResolveProjectileDirectionFromOrigin(
            Vector2 fallbackDirection,
            float spreadAngleDegrees,
            EnemyRuntimeController shotTarget,
            Vector3 projectileLocalPosition)
        {
            return fallbackDirection.sqrMagnitude > 0.001f ? fallbackDirection.normalized : Vector2.up;
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
            public ProjectileShotSpec(Vector2 direction, float lateralOffsetMeters, float spreadAngleDegrees)
            {
                Direction = direction;
                LateralOffsetMeters = lateralOffsetMeters;
                SpreadAngleDegrees = spreadAngleDegrees;
            }

            public Vector2 Direction { get; }

            public float LateralOffsetMeters { get; }

            public float SpreadAngleDegrees { get; }
        }

        private bool TryMeleeAttack(AttackKind attackKind, Vector2 attackDirection, float timeSeconds)
        {
            BindHealthEvents();
            TickAction(0f, timeSeconds);
            if (IsGuarding)
            {
                return false;
            }

            var aim = GameplayInputReader.NormalizeAimDirection(attackDirection);
            if (aim.sqrMagnitude < 0.001f)
            {
                aim = lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;
            }

            var weapon = ResolveWeapon(WeaponSlot.Melee);
            var attack = ResolveAttack(weapon, WeaponSlot.Melee, attackKind);
            if (timeSeconds < nextAllowedMeleeTime ||
                attackExecutionState != PlayerAttackExecutionState.Idle ||
                combatController == null ||
                !TrySpendStamina(AdjustedAttackStaminaCost(attack.StaminaCost), timeSeconds, StaminaSpendKind.Action))
            {
                return false;
            }

            if (attackKind == AttackKind.Light)
            {
                nextAllowedMeleeTime = timeSeconds + EffectiveMeleeCooldown(attack, attackKind);
            }

            var assist = ResolveAttackAssist(WeaponSlot.Melee, attackKind, attack, aim, timeSeconds);
            StartPendingAttack(WeaponSlot.Melee, attackKind, attack, assist.Direction, timeSeconds, assist.Target, assist);
            return true;
        }

        private void StartPendingAttack(
            WeaponSlot slot,
            AttackKind attackKind,
            WeaponAttackDefinition attack,
            Vector2 direction,
            float timeSeconds,
            EnemyRuntimeController shotTarget = null,
            PlayerAimAssistResult assistResult = default)
        {
            pendingAttackSlot = slot;
            pendingAttackKind = attackKind;
            pendingAttack = attack;
            pendingAttackDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : LastAimDirection;
            pendingAttackTarget = slot == WeaponSlot.Ranged ? shotTarget : null;
            pendingAttackAssistResult = assistResult;
            lastAimDirection = pendingAttackDirection;
            pendingAttackApplied = false;
            attackWindupEndTime = timeSeconds + attack.WindupSeconds;
            attackActiveEndTime = attackWindupEndTime + attack.ActiveSeconds;
            attackRecoveryEndTime = attackActiveEndTime + attack.RecoverySeconds;
            attackExecutionState = PlayerAttackExecutionState.Windup;
            aimLockController?.NotifyAttackCommitted(assistResult, timeSeconds);
            var actionDurationSeconds = Mathf.Max(0.01f, attack.WindupSeconds + attack.ActiveSeconds + attack.RecoverySeconds);
            WeaponActionAnimationRequested?.Invoke(slot, attackKind, pendingAttackDirection, actionDurationSeconds);
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
            var projectileSpeed = PlayerProjectileSpeedMetersPerSecond;
            var lifetimeSeconds = Mathf.Max(0.1f, effectiveRange / projectileSpeed);
            var aimSource = ResolveShotAimSource();
            TryGetLockedTargetTelemetry(pendingAttackTarget, out var lockedTargetName, out var lockedTargetDistanceMeters);
            foreach (var shot in BuildProjectileShots(cardinal))
            {
                SpawnProjectile(
                    shot,
                    attackDamage,
                    projectileSpeed,
                    lifetimeSeconds,
                    pendingAttack,
                    aimSource,
                    pendingAttackTarget,
                    lockedTargetName,
                    lockedTargetDistanceMeters,
                    timeSeconds);
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
                if (DamageSystem.ApplyDamage(
                    target.Health,
                    new DamageRequest(
                        damage,
                        gameObject,
                        DamageFeedbackContext.Knockback(direction, pendingAttack.KnockbackMeters, profile.KnockbackSeconds),
                        DamageClassification.PhysicalMelee(pendingAttack.ImpactForceClass))))
                {
                    aimLockController?.NotifyEnemyDamaged(target, timeSeconds);
                }

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
            CancelRangedDraw();
            attackExecutionState = PlayerAttackExecutionState.Idle;
            pendingAttackTarget = null;
            pendingAttackApplied = false;
            attackWindupEndTime = 0f;
            attackActiveEndTime = 0f;
            attackRecoveryEndTime = 0f;
            rollInvulnerableStartTime = 0f;
            rollEndTime = 0f;
            rollInvulnerableEndTime = 0f;
        }

        public bool IsRollInvulnerableAt(float timeSeconds)
        {
            return IsRolling &&
                timeSeconds >= rollInvulnerableStartTime &&
                timeSeconds < rollInvulnerableEndTime;
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

        private bool TrySpendStamina(float amount, float timeSeconds, StaminaSpendKind spendKind)
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
            if (spendKind != StaminaSpendKind.GuardDrain)
            {
                staminaRegenBlockedUntil = Mathf.Max(staminaRegenBlockedUntil, timeSeconds + StaminaRegenDelaySeconds);
            }

            return true;
        }

        public bool SpendStaminaForDefense(float amount)
        {
            return SpendStaminaForDefense(amount, delaysRegen: true, Time.time);
        }

        public bool SpendStaminaForDefense(float amount, bool delaysRegen, float timeSeconds)
        {
            return TrySpendStamina(amount, timeSeconds, delaysRegen ? StaminaSpendKind.GuardHit : StaminaSpendKind.GuardDrain);
        }

        private float AdjustedAttackStaminaCost(float amount)
        {
            return Mathf.Max(0f, amount) * Mathf.Max(0.01f, attackStaminaCostMultiplier);
        }

        private void RegenerateStamina(float deltaTime, float timeSeconds)
        {
            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (safeDeltaTime <= 0f ||
                timeSeconds < staminaRegenBlockedUntil ||
                IsAttackCommitted ||
                IsRolling)
            {
                return;
            }

            var guardMultiplier = IsGuarding ? GuardHeldStaminaRegenMultiplier : 1f;
            currentStamina = Mathf.Min(maxStamina, currentStamina + safeDeltaTime * staminaRegenPerSecond * guardMultiplier);
        }

        private Vector2 CurrentAim(GameplayInputSnapshot input)
        {
            return CurrentAim(input, Time.time);
        }

        private Vector2 CurrentAim(GameplayInputSnapshot input, float timeSeconds)
        {
            if (aimLockController != null)
            {
                return aimLockController.ResolveAttackDirection(input, timeSeconds);
            }

            if (input.HasShoot)
            {
                return input.Shoot;
            }

            return lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection : Vector2.up;
        }

        private PlayerShotAimSource ResolveShotAimSource()
        {
            if (aimLockController == null)
            {
                return PlayerShotAimSource.Unknown;
            }

            return aimLockController.HasManualAimOverride
                ? PlayerShotAimSource.ManualAim
                : PlayerShotAimSource.BodyFacing;
        }

        private bool TryGetLockedTargetTelemetry(
            EnemyRuntimeController shotTarget,
            out string targetName,
            out float distanceMeters)
        {
            targetName = string.Empty;
            distanceMeters = -1f;
            var target = IsValidTarget(shotTarget) ? shotTarget : null;
            if (target == null)
            {
                target = aimLockController != null ? aimLockController.LockedEnemy : null;
            }

            if (target == null)
            {
                return false;
            }

            targetName = target.name;
            distanceMeters = DistanceToTarget(target);
            return true;
        }

        private static bool IsValidTarget(EnemyRuntimeController target)
        {
            return target != null && target.IsAlive && target.gameObject.activeInHierarchy;
        }

        private float DistanceToTarget(EnemyRuntimeController target)
        {
            if (target == null)
            {
                return -1f;
            }

            var gameplayRoot = ResolveGameplayRoot();
            var targetPosition = gameplayRoot != null ? gameplayRoot.InverseTransformPoint(target.transform.position) : target.transform.position;
            var playerPosition = gameplayRoot != null ? gameplayRoot.InverseTransformPoint(transform.position) : transform.position;
            var delta = targetPosition - playerPosition;
            delta.y = 0f;
            return delta.magnitude;
        }

        private void EnsureAimLockController()
        {
            if (aimLockController == null)
            {
                aimLockController = GetComponent<PlayerAimLockController>() ?? gameObject.AddComponent<PlayerAimLockController>();
            }
        }

        private Transform ResolveGameplayRoot()
        {
            var presentationRoot = GetComponentInParent<PlatformPresentationRoot>();
            return presentationRoot != null ? presentationRoot.transform : transform.parent;
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

        private void OnPlayerDamaged(CombatantHealth damagedHealth)
        {
            if (IsRolling)
            {
                return;
            }

            if (rangedDrawActive)
            {
                ClearPendingAction();
                return;
            }

            if (ShouldDamageInterruptCommittedAttack(damagedHealth))
            {
                ClearPendingAction();
            }
        }

        private static bool ShouldDamageInterruptCommittedAttack(CombatantHealth damagedHealth)
        {
            if (damagedHealth == null ||
                damagedHealth.LastDamageAppliedAmount <= 0 ||
                damagedHealth.LastDamageRequest.Amount <= 0)
            {
                return true;
            }

            var request = damagedHealth.LastDamageRequest;
            if (request.Classification.Delivery != DamageDelivery.Projectile)
            {
                return true;
            }

            if (request.ThreatKind is DamageThreatKind.Boss
                or DamageThreatKind.Environmental
                or DamageThreatKind.StrongProjectile)
            {
                return true;
            }

            return request.Classification.ForceClass is ImpactForceClass.Heavy
                or ImpactForceClass.Massive;
        }

        private void OnPlayerDied(CombatantHealth _)
        {
            ClearPendingAction();
        }
    }
}
