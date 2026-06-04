using Hollow.Input;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public enum PlayerTargetLockMode
    {
        None = 0,
        Auto = 1,
        ManualRetarget = 2
    }

    public enum PlayerAimAssistSource
    {
        None = 0,
        BodyFacing = 1,
        ManualAim = 2,
        MouseHover = 3,
        AimCone = 4,
        RecentTarget = 5,
        ManualFocus = 6
    }

    public readonly struct PlayerAimAssistResult
    {
        public PlayerAimAssistResult(
            EnemyRuntimeController target,
            Vector2 direction,
            PlayerAimAssistSource source,
            PlayerTargetLockMode lockMode,
            float score,
            float distanceMeters)
        {
            Target = target;
            Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            Source = source;
            LockMode = lockMode;
            Score = score;
            DistanceMeters = distanceMeters;
        }

        public EnemyRuntimeController Target { get; }

        public Vector2 Direction { get; }

        public PlayerAimAssistSource Source { get; }

        public PlayerTargetLockMode LockMode { get; }

        public float Score { get; }

        public float DistanceMeters { get; }

        public bool HasTarget => Target != null;

        public static PlayerAimAssistResult None(Vector2 direction)
        {
            return new PlayerAimAssistResult(
                null,
                direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up,
                PlayerAimAssistSource.None,
                PlayerTargetLockMode.None,
                float.MaxValue,
                -1f);
        }
    }

    public sealed class PlayerAimLockController : MonoBehaviour
    {
        public const float ExplicitLockRangeMeters = 10f;
        public const float SoftAutoLockRangeMeters = 5.5f;
        public const float AutoSwitchCloserMarginMeters = 0.75f;
        public const float RecentTargetMemorySeconds = 4f;
        public const float RetargetCooldownSeconds = 0.2f;
        public const float MouseAimIntentMemorySeconds = 1.25f;

        [SerializeField] private RoomCombatController combatController;

        private Transform playerVisualRoot;
        private GameObject lockMarker;
        private Material lockMarkerMaterial;
        private Vector2 bodyFacingDirection = Vector2.up;
        private Vector2 attackDirection = Vector2.up;
        private Vector2 lastResolvedAttackDirection = Vector2.up;
        private float lastMouseAimIntentTimeSeconds = -999f;
        private bool hasManualAimOverride;

        public Vector2 BodyFacingDirection => SafeDirection(bodyFacingDirection);

        public Vector2 AttackDirection => SafeDirection(attackDirection);

        public Vector2 LastResolvedAttackDirection => SafeDirection(lastResolvedAttackDirection);

        public EnemyRuntimeController CurrentFocusTarget => null;

        public EnemyRuntimeController CurrentAssistTarget => null;

        public EnemyRuntimeController RecentAttackTarget => null;

        public EnemyRuntimeController RecentDamagedTarget => null;

        public EnemyRuntimeController LockedEnemy => null;

        public PlayerTargetLockMode CurrentLockMode => PlayerTargetLockMode.None;

        public bool IsTargetLocked => false;

        public bool IsExplicitlyLocked => false;

        public bool HasAssistTarget => false;

        public bool HasManualAimOverride => hasManualAimOverride;

        public void Configure(RoomCombatController controller)
        {
            combatController = controller;
            HideLockMarker();
        }

        public void BindPresentation(GameObject nextPlayerVisualRoot)
        {
            playerVisualRoot = nextPlayerVisualRoot != null ? nextPlayerVisualRoot.transform : null;
            ApplyPresentationFacing();
        }

        public void TickAim(GameplayInputSnapshot input, float timeSeconds)
        {
            UpdateBodyFacing(input);
            RecordMouseAimIntent(input, timeSeconds);
            attackDirection = ResolveInputDirection(input, timeSeconds);
            lastResolvedAttackDirection = attackDirection;
            hasManualAimOverride = HasExplicitAim(input, timeSeconds);
            HideLockMarker();
            ApplyPresentationFacing();
        }

        public Vector2 ResolveAttackDirection(GameplayInputSnapshot input, float timeSeconds)
        {
            UpdateBodyFacing(input);
            RecordMouseAimIntent(input, timeSeconds);
            attackDirection = ResolveInputDirection(input, timeSeconds);
            lastResolvedAttackDirection = attackDirection;
            hasManualAimOverride = HasExplicitAim(input, timeSeconds);
            HideLockMarker();
            ApplyPresentationFacing();
            return attackDirection;
        }

        public Vector2 ResolveGuardDirection(GameplayInputSnapshot input, float timeSeconds)
        {
            UpdateBodyFacing(input);
            RecordMouseAimIntent(input, timeSeconds);
            hasManualAimOverride = HasExplicitAim(input, timeSeconds);
            return ResolveInputDirection(input, timeSeconds);
        }

        public bool TryGetLockedTargetDirection(out Vector2 direction)
        {
            direction = Vector2.zero;
            return false;
        }

        public bool TryGetLocomotionFacingDirection(out Vector2 direction)
        {
            direction = Vector2.zero;
            if (hasManualAimOverride && attackDirection.sqrMagnitude > 0.001f)
            {
                direction = SafeDirection(attackDirection);
                return true;
            }

            if (combatController == null)
            {
                return false;
            }

            var bestDistanceSqr = SoftAutoLockRangeMeters * SoftAutoLockRangeMeters;
            EnemyRuntimeController bestEnemy = null;
            var playerPosition = transform.position;
            var enemies = combatController.Enemies;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsAlive)
                {
                    continue;
                }

                var delta = enemy.transform.position - playerPosition;
                delta.y = 0f;
                var distanceSqr = delta.sqrMagnitude;
                if (distanceSqr <= 0.001f || distanceSqr > bestDistanceSqr)
                {
                    continue;
                }

                bestDistanceSqr = distanceSqr;
                bestEnemy = enemy;
            }

            if (bestEnemy == null)
            {
                return false;
            }

            var bestDelta = bestEnemy.transform.position - playerPosition;
            direction = SafeDirection(new Vector2(bestDelta.x, bestDelta.z));
            return true;
        }

        public PlayerAimAssistResult ResolveAttackAssist(
            GameplayInputSnapshot input,
            float rangeMeters,
            bool isMelee,
            float timeSeconds)
        {
            return PlayerAimAssistResult.None(ResolveAttackDirection(input, timeSeconds));
        }

        public PlayerAimAssistResult ResolveAttackAssist(
            Vector2 requestedDirection,
            float rangeMeters,
            bool isMelee,
            float timeSeconds)
        {
            var direction = requestedDirection.sqrMagnitude > 0.001f
                ? SafeDirection(requestedDirection)
                : BodyFacingDirection;
            attackDirection = direction;
            lastResolvedAttackDirection = direction;
            HideLockMarker();
            ApplyPresentationFacing();
            return PlayerAimAssistResult.None(direction);
        }

        public void NotifyAttackCommitted(PlayerAimAssistResult result, float timeSeconds)
        {
            lastResolvedAttackDirection = result.Direction.sqrMagnitude > 0.001f
                ? SafeDirection(result.Direction)
                : AttackDirection;
        }

        public void NotifyEnemyDamaged(EnemyRuntimeController enemy)
        {
        }

        public void NotifyEnemyDamaged(EnemyRuntimeController enemy, float timeSeconds)
        {
        }

        private void OnDisable()
        {
            HideLockMarker();
        }

        private void OnDestroy()
        {
            if (lockMarker != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(lockMarker);
                }
                else
                {
                    DestroyImmediate(lockMarker);
                }
            }

            if (lockMarkerMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(lockMarkerMaterial);
                }
                else
                {
                    DestroyImmediate(lockMarkerMaterial);
                }
            }
        }

        private void UpdateBodyFacing(GameplayInputSnapshot input)
        {
            var moveFacing = GameplayInputReader.NormalizeAimDirection(input.Move);
            if (moveFacing.sqrMagnitude > 0.001f)
            {
                bodyFacingDirection = moveFacing;
            }
        }

        private void RecordMouseAimIntent(GameplayInputSnapshot input, float timeSeconds)
        {
            if (input.MouseAimIntent && input.HasPointerScreenPosition)
            {
                lastMouseAimIntentTimeSeconds = timeSeconds;
            }
        }

        private bool IsMouseAimRecentlyActive(float timeSeconds)
        {
            return timeSeconds - lastMouseAimIntentTimeSeconds <= MouseAimIntentMemorySeconds;
        }

        private Vector2 ResolveInputDirection(GameplayInputSnapshot input, float timeSeconds)
        {
            if (input.HasShoot)
            {
                return SafeDirection(input.Shoot);
            }

            if (IsMouseAimRecentlyActive(timeSeconds) && TryResolveMouseAimDirection(input, out var mouseDirection))
            {
                return mouseDirection;
            }

            return BodyFacingDirection;
        }

        private bool HasExplicitAim(GameplayInputSnapshot input, float timeSeconds)
        {
            return input.HasShoot ||
                (IsMouseAimRecentlyActive(timeSeconds) && input.HasPointerScreenPosition);
        }

        private bool TryResolveMouseAimDirection(GameplayInputSnapshot input, out Vector2 direction)
        {
            direction = Vector2.zero;
            if (!TryResolveMouseAimPoint(input, out var pointerLocalPoint))
            {
                return false;
            }

            var playerLocalPosition = LocalGameplayPosition(transform);
            var delta = pointerLocalPoint - new Vector2(playerLocalPosition.x, playerLocalPosition.z);
            if (delta.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction = delta.normalized;
            return true;
        }

        private bool TryResolveMouseAimPoint(GameplayInputSnapshot input, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            if (!input.HasPointerScreenPosition)
            {
                return false;
            }

            var root = ResolveGameplayRoot();
            if (!GameplayInputProjection.TryScreenPointToGameplayPlane(input.PointerScreenPosition, root, transform.position, out var local))
            {
                return false;
            }

            localPoint = new Vector2(local.x, local.z);
            return true;
        }

        private Vector3 LocalGameplayPosition(Transform target)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            var root = ResolveGameplayRoot();
            return root != null ? root.InverseTransformPoint(target.position) : target.localPosition;
        }

        private Transform ResolveGameplayRoot()
        {
            var presentationRoot = GetComponentInParent<PlatformPresentationRoot>();
            if (presentationRoot != null)
            {
                return presentationRoot.transform;
            }

            return combatController != null ? combatController.transform : transform.parent;
        }

        private void ApplyPresentationFacing()
        {
            if (playerVisualRoot == null)
            {
                return;
            }

            var facing = AttackDirection;
            if (facing.sqrMagnitude <= 0.001f)
            {
                return;
            }

            playerVisualRoot.localRotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.y), Vector3.up);
        }

        private void HideLockMarker()
        {
            if (lockMarker != null)
            {
                lockMarker.SetActive(false);
            }
        }

        private static Vector2 SafeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        }
    }
}
