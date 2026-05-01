using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Input;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerDefenseController : MonoBehaviour, IIncomingDamageModifier
    {
        public const int DefensePerPassiveDamageReduction = 2;
        public const int GuardDamageReduction = 1;
        public const float GuardDrainStaminaPerSecond = 12f;
        public const float GuardBlockStaminaCost = 12f;
        public const float GuardPushMeters = 0.25f;
        public const float ParryWindowSeconds = 0.3f;
        public const float GuardConeDegrees = 140f;
        public const float ParryStaminaCost = 16f;

        [SerializeField] private int defense;
        [SerializeField] private int baseStability = 1;
        [SerializeField] private int activeGuardShieldStabilityBonus = 1;
        [SerializeField] private float guardStaminaCostMultiplier = 1f;
        [SerializeField] private bool isGuarding;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private ShieldGuardProfileDefinition shieldProfile;

        private ShieldGuardVisualController visualController;
        private Vector3 guardFacing = Vector3.forward;
        private float parryWindowEndTime;
        private bool lastGuardHeld;
        private bool parryConsumed;

        public int Defense => defense;

        public int BaseStability => Mathf.Max(0, baseStability);

        public int ActiveStability => BaseStability + (isGuarding ? Mathf.Max(0, activeGuardShieldStabilityBonus) : 0);

        public bool IsGuarding => isGuarding;

        public bool IsInParryWindow => IsInParryWindowAt(Time.time);

        public ShieldGuardResult LastGuardResult { get; private set; }

        public int LastDamageReduction { get; private set; }

        public bool LastHitWasGuarded { get; private set; }

        public Vector3 GuardFacing => guardFacing.sqrMagnitude < 0.001f ? Vector3.forward : guardFacing.normalized;

        public float GuardMoveMultiplier => ShieldGuardProfileDefinition.Resolve(shieldProfile).GuardMoveMultiplier;

        public void Configure(int nextDefense)
        {
            defense = Mathf.Max(0, nextDefense);
            ResolveReferences();
        }

        public void ConfigureStability(int nextBaseStability, int nextActiveGuardShieldStabilityBonus, float nextGuardStaminaCostMultiplier)
        {
            baseStability = Mathf.Max(0, nextBaseStability);
            activeGuardShieldStabilityBonus = Mathf.Max(0, nextActiveGuardShieldStabilityBonus);
            guardStaminaCostMultiplier = Mathf.Max(0.01f, nextGuardStaminaCostMultiplier);
            ResolveReferences();
        }

        public void ConfigureShieldProfile(ShieldGuardProfileDefinition profile)
        {
            shieldProfile = ShieldGuardProfileDefinition.Resolve(profile);
            ResolveReferences();
            visualController?.Configure(shieldProfile);
        }

        public void Bind(RoomRuntimeRoot room)
        {
            roomRuntimeRoot = room;
            ResolveReferences();
        }

        private void Update()
        {
            if (GameplayPauseState.IsPaused)
            {
                return;
            }

            var input = GameplayInputReader.ReadCurrent();
            Tick(input, Time.deltaTime, Time.time);
        }

        public void Tick(bool guardHeld, float deltaTime)
        {
            Tick(
                new GameplayInputSnapshot(
                    Vector2.zero,
                    Vector2.zero,
                    interactPressed: false,
                    swapWeaponPressed: false,
                    lightAttackPressed: false,
                    heavyAttackPressed: false,
                    useActiveItemPressed: false,
                    useConsumableCardPressed: false,
                    guardHeld: guardHeld),
                deltaTime,
                Time.time);
        }

        public void Tick(GameplayInputSnapshot input, float deltaTime, float timeSeconds)
        {
            ResolveReferences();
            UpdateGuardFacing(input);
            if (!input.GuardHeld)
            {
                isGuarding = false;
                lastGuardHeld = false;
                visualController?.SetState(false, false, GuardFacing);
                return;
            }

            var profile = ShieldGuardProfileDefinition.Resolve(shieldProfile);
            if (!lastGuardHeld)
            {
                parryConsumed = false;
                parryWindowEndTime = timeSeconds + profile.ParryWindowSeconds;
                VfxPresenter.Play(VfxCueId.ShieldGuardStart, transform.position, transform.parent);
                AudioPresenter.Play(AudioCueId.ShieldGuardStart, transform.position);
            }

            lastGuardHeld = true;
            var drainCost = AdjustGuardStaminaCost(profile.GuardDrainStaminaPerSecond * Mathf.Max(0f, deltaTime));
            isGuarding = SpendStamina(drainCost);
            if (!isGuarding)
            {
                LastGuardResult = ShieldGuardResult.FailedNoStamina;
                visualController?.ShowFeedback(LastGuardResult);
            }

            visualController?.SetState(isGuarding, IsInParryWindowAt(timeSeconds), GuardFacing);
        }

        public int ModifyIncomingDamage(DamageRequest request, int currentAmount)
        {
            LastDamageReduction = 0;
            LastHitWasGuarded = false;
            LastGuardResult = ShieldGuardResult.None;
            if (currentAmount <= 0)
            {
                return 0;
            }

            var damageFeedback = GetComponent<PlayerDamageFeedbackController>();
            if (damageFeedback != null && damageFeedback.IsInvulnerable)
            {
                return currentAmount;
            }

            var reducedAmount = currentAmount;
            var passiveReduction = Mathf.Min(currentAmount - 1, Mathf.FloorToInt(defense / (float)DefensePerPassiveDamageReduction));
            if (passiveReduction > 0)
            {
                reducedAmount -= passiveReduction;
                LastDamageReduction += passiveReduction;
                LastGuardResult = ShieldGuardResult.PassiveReduced;
            }

            if (!isGuarding || request.ThreatKind == DamageThreatKind.Environmental)
            {
                return reducedAmount;
            }

            var profile = ShieldGuardProfileDefinition.Resolve(shieldProfile);
            var sourceDirection = SourceDirection(request);
            if (!IsInsideGuardCone(sourceDirection, profile.GuardConeDegrees))
            {
                LastGuardResult = ShieldGuardResult.FailedOutOfCone;
                visualController?.ShowFeedback(LastGuardResult);
                return reducedAmount;
            }

            var parryable = request.ThreatKind == DamageThreatKind.Light;
            if (parryable && IsInParryWindow && !parryConsumed)
            {
                if (SpendStamina(AdjustGuardStaminaCost(profile.ParryStaminaCost)))
                {
                    parryConsumed = true;
                    LastHitWasGuarded = true;
                    LastDamageReduction += reducedAmount;
                    LastGuardResult = ShieldGuardResult.PerfectParry;
                    ApplyParryCounter(request, sourceDirection, profile);
                    PlayGuardFeedback(LastGuardResult);
                    return 0;
                }

                LastGuardResult = ShieldGuardResult.FailedNoStamina;
            }

            if (SpendStamina(AdjustGuardStaminaCost(profile.GuardHitStaminaCost)))
            {
                var beforeGuard = reducedAmount;
                reducedAmount = Mathf.Max(0, reducedAmount - profile.GuardDamageReduction);
                LastDamageReduction += beforeGuard - reducedAmount;
                LastHitWasGuarded = true;
                LastGuardResult = parryable ? ShieldGuardResult.GuardBlocked : ShieldGuardResult.RejectedThreat;
                PushSourceAway(request.Source, profile.GuardPushMeters);
                PlayGuardFeedback(LastGuardResult);
                return reducedAmount;
            }

            LastGuardResult = ShieldGuardResult.FailedNoStamina;
            visualController?.ShowFeedback(LastGuardResult);
            return reducedAmount;
        }

        public bool IsInParryWindowAt(float timeSeconds)
        {
            return isGuarding && !parryConsumed && timeSeconds <= parryWindowEndTime;
        }

        private bool SpendStamina(float amount)
        {
            return weaponController == null || weaponController.SpendStaminaForDefense(amount);
        }

        private float AdjustGuardStaminaCost(float amount)
        {
            return Mathf.Max(0f, amount) * Mathf.Max(0.01f, guardStaminaCostMultiplier);
        }

        private void ApplyParryCounter(DamageRequest request, Vector3 sourceDirection, ShieldGuardProfileDefinition profile)
        {
            var source = request.Source;
            if (source == null)
            {
                return;
            }

            var projectile = source.GetComponentInParent<EnemyProjectileController>();
            if (projectile != null)
            {
                projectile.Neutralize();
                return;
            }

            var enemy = source.GetComponentInParent<EnemyRuntimeController>();
            if (enemy == null || !enemy.IsAlive || profile.ParryCounterDamage <= 0)
            {
                return;
            }

            DamageSystem.ApplyDamage(
                enemy.Health,
                new DamageRequest(
                    profile.ParryCounterDamage,
                    gameObject,
                    DamageFeedbackContext.Knockback(sourceDirection, profile.GuardPushMeters, 0.08f),
                    DamageThreatKind.Light));
            PushSourceAway(source, profile.GuardPushMeters);
        }

        private void PushSourceAway(GameObject source, float meters)
        {
            if (source == null)
            {
                return;
            }

            var enemy = source.GetComponentInParent<EnemyRuntimeController>();
            if (enemy == null || !enemy.IsAlive)
            {
                return;
            }

            var direction = enemy.transform.localPosition - transform.localPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            var desired = enemy.transform.localPosition + direction.normalized * Mathf.Max(0f, meters);
            enemy.transform.localPosition = roomRuntimeRoot != null
                ? RoomLocalCollision.ResolveMove(roomRuntimeRoot, enemy.transform.localPosition, desired, enemy.RadiusMeters)
                : desired;
        }

        private void PlayGuardFeedback(ShieldGuardResult result)
        {
            visualController?.ShowFeedback(result);
            var cue = result switch
            {
                ShieldGuardResult.PerfectParry => VfxCueId.ShieldParryCounter,
                ShieldGuardResult.GuardBlocked => VfxCueId.ShieldBlock,
                ShieldGuardResult.RejectedThreat => VfxCueId.ShieldBlock,
                _ => VfxCueId.ShieldUnavailable
            };
            var audioCue = result switch
            {
                ShieldGuardResult.PerfectParry => AudioCueId.ShieldParryCounter,
                ShieldGuardResult.GuardBlocked => AudioCueId.ShieldBlock,
                ShieldGuardResult.RejectedThreat => AudioCueId.ShieldBlock,
                _ => AudioCueId.ShieldUnavailable
            };
            VfxPresenter.Play(cue, transform.position, transform.parent);
            AudioPresenter.Play(audioCue, transform.position);
        }

        private void UpdateGuardFacing(GameplayInputSnapshot input)
        {
            var facing = input.HasShoot
                ? new Vector3(input.Shoot.x, 0f, input.Shoot.y)
                : input.Move.sqrMagnitude > 0.001f
                    ? new Vector3(input.Move.x, 0f, input.Move.y)
                    : GuardFacing;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
            {
                guardFacing = facing.normalized;
            }
        }

        private Vector3 SourceDirection(DamageRequest request)
        {
            if (request.Source != null)
            {
                var direction = request.Source.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized;
                }
            }

            if (request.Feedback.HasDirection)
            {
                var direction = -request.Feedback.Direction;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized;
                }
            }

            return Vector3.zero;
        }

        private bool IsInsideGuardCone(Vector3 sourceDirection, float coneDegrees)
        {
            if (sourceDirection.sqrMagnitude < 0.001f)
            {
                return true;
            }

            return Vector3.Angle(GuardFacing, sourceDirection.normalized) <= coneDegrees * 0.5f;
        }

        private void ResolveReferences()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (roomRuntimeRoot == null)
            {
                roomRuntimeRoot = GetComponentInParent<RoomRuntimeRoot>() ?? FindAnyObjectByType<RoomRuntimeRoot>();
            }

            if (shieldProfile == null)
            {
                shieldProfile = ShieldGuardProfileDefinition.Resolve(null);
            }

            if (visualController == null)
            {
                visualController = GetComponent<ShieldGuardVisualController>() ?? gameObject.AddComponent<ShieldGuardVisualController>();
                visualController.Configure(shieldProfile);
            }
        }
    }
}
