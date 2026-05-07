using Hollow.Input;
using UnityEngine;

namespace Hollow.Combat
{
    public enum PlayerTargetLockMode
    {
        None = 0,
        Auto = 1,
        ManualRetarget = 2
    }

    public sealed class PlayerAimLockController : MonoBehaviour
    {
        public const float ExplicitLockRangeMeters = 10f;
        public const float SoftAutoLockRangeMeters = 5.5f;
        public const float AutoSwitchCloserMarginMeters = 0.75f;
        public const float RecentTargetMemorySeconds = 4f;
        public const float RetargetCooldownSeconds = 0.2f;
        public const float MouseAimIntentMemorySeconds = 1.25f;
        private const float RetargetSectorDegrees = 80f;
        private const int RingSegments = 48;

        [SerializeField] private RoomCombatController combatController;

        private EnemyRuntimeController lockedEnemy;
        private EnemyRuntimeController recentEnemy;
        private Transform playerVisualRoot;
        private GameObject lockMarker;
        private Material lockMarkerMaterial;
        private Vector2 bodyFacingDirection = Vector2.up;
        private Vector2 attackDirection = Vector2.up;
        private float recentEnemyTimeSeconds = -999f;
        private float nextRetargetTimeSeconds;
        private float lastMouseAimIntentTimeSeconds = -999f;
        private PlayerTargetLockMode currentLockMode = PlayerTargetLockMode.None;
        private bool hasManualAimOverride;

        public Vector2 BodyFacingDirection => SafeDirection(bodyFacingDirection);

        public Vector2 AttackDirection => SafeDirection(attackDirection);

        public EnemyRuntimeController LockedEnemy => IsValidTarget(lockedEnemy) ? lockedEnemy : null;

        public PlayerTargetLockMode CurrentLockMode => LockedEnemy != null ? currentLockMode : PlayerTargetLockMode.None;

        public bool IsTargetLocked => CurrentLockMode != PlayerTargetLockMode.None && LockedEnemy != null;

        public bool IsExplicitlyLocked => CurrentLockMode == PlayerTargetLockMode.ManualRetarget && LockedEnemy != null;

        public bool HasManualAimOverride => hasManualAimOverride;

        public void Configure(RoomCombatController controller)
        {
            combatController = controller;
            ValidateCurrentTarget();
        }

        public void BindPresentation(GameObject nextPlayerVisualRoot)
        {
            playerVisualRoot = nextPlayerVisualRoot != null ? nextPlayerVisualRoot.transform : null;
            ApplyPresentationFacing();
        }

        public void TickAim(GameplayInputSnapshot input, float timeSeconds)
        {
            UpdateBodyFacing(input);
            ValidateCurrentTarget();

            if (input.LockTargetPressed)
            {
                ToggleManualLock();
            }

            if (IsTargetLocked && input.HasShoot && timeSeconds >= nextRetargetTimeSeconds)
            {
                var retargetDirection = GameplayInputReader.QuantizeEightAxis(input.Shoot);
                var retarget = FindTargetInDirection(retargetDirection, ExplicitLockRangeMeters, lockedEnemy);
                if (retarget != null)
                {
                    lockedEnemy = retarget;
                    currentLockMode = PlayerTargetLockMode.ManualRetarget;
                    nextRetargetTimeSeconds = timeSeconds + RetargetCooldownSeconds;
                }
            }

            UpdateAutoLock();
            RecordMouseAimIntent(input, timeSeconds);
            hasManualAimOverride = !IsTargetLocked &&
                (input.HasShoot || (IsMouseAimRecentlyActive(timeSeconds) && TryResolveMouseAimDirection(input, out _)));
            attackDirection = ResolveAttackDirection(input, timeSeconds);
            ApplyPresentationFacing();
            UpdateLockMarker();
        }

        public Vector2 ResolveAttackDirection(GameplayInputSnapshot input, float timeSeconds)
        {
            UpdateBodyFacing(input);
            return ResolveAttackDirection(input, timeSeconds, BodyFacingDirection);
        }

        public Vector2 ResolveGuardDirection(GameplayInputSnapshot input, float timeSeconds)
        {
            UpdateBodyFacing(input);
            var fallback = GameplayInputReader.NormalizeAimDirection(input.Move);
            return ResolveAttackDirection(input, timeSeconds, fallback.sqrMagnitude > 0.001f ? fallback : BodyFacingDirection);
        }

        public bool TryGetLockedTargetDirection(out Vector2 direction)
        {
            direction = Vector2.zero;
            var target = LockedEnemy;
            if (!IsTargetLocked || target == null)
            {
                return false;
            }

            direction = DirectionTo(target);
            return true;
        }

        private Vector2 ResolveAttackDirection(GameplayInputSnapshot input, float timeSeconds, Vector2 fallbackDirection)
        {
            RecordMouseAimIntent(input, timeSeconds);
            if (IsTargetLocked)
            {
                return DirectionTo(LockedEnemy);
            }

            if (input.HasShoot)
            {
                return SafeDirection(input.Shoot);
            }

            if (IsMouseAimRecentlyActive(timeSeconds) && TryResolveMouseAimDirection(input, out var mouseDirection))
            {
                return mouseDirection;
            }

            return SafeDirection(fallbackDirection);
        }

        public void NotifyEnemyDamaged(EnemyRuntimeController enemy)
        {
            NotifyEnemyDamaged(enemy, Time.time);
        }

        public void NotifyEnemyDamaged(EnemyRuntimeController enemy, float timeSeconds)
        {
            if (!IsValidTarget(enemy))
            {
                return;
            }

            recentEnemy = enemy;
            recentEnemyTimeSeconds = timeSeconds;
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

        private void ToggleManualLock()
        {
            if (CurrentLockMode == PlayerTargetLockMode.ManualRetarget)
            {
                currentLockMode = PlayerTargetLockMode.None;
                lockedEnemy = null;
                HideLockMarker();
                return;
            }

            if (IsTargetLocked)
            {
                currentLockMode = PlayerTargetLockMode.ManualRetarget;
                UpdateLockMarker();
                return;
            }

            lockedEnemy = FindNearestTarget(ExplicitLockRangeMeters);
            currentLockMode = lockedEnemy != null ? PlayerTargetLockMode.ManualRetarget : PlayerTargetLockMode.None;
            UpdateLockMarker();
        }

        private EnemyRuntimeController ResolveSoftTarget(float timeSeconds)
        {
            if (IsValidTarget(recentEnemy) &&
                timeSeconds - recentEnemyTimeSeconds <= RecentTargetMemorySeconds &&
                DistanceTo(recentEnemy) <= ExplicitLockRangeMeters)
            {
                return recentEnemy;
            }

            return FindNearestTarget(SoftAutoLockRangeMeters);
        }

        private EnemyRuntimeController FindNearestTarget(float rangeMeters)
        {
            if (combatController == null)
            {
                return null;
            }

            EnemyRuntimeController best = null;
            var bestScore = float.MaxValue;
            foreach (var enemy in combatController.Enemies)
            {
                if (!IsValidTarget(enemy))
                {
                    continue;
                }

                var distance = DistanceTo(enemy);
                if (distance > rangeMeters)
                {
                    continue;
                }

                if (distance >= bestScore)
                {
                    continue;
                }

                bestScore = distance;
                best = enemy;
            }

            return best;
        }

        private EnemyRuntimeController FindTargetInDirection(Vector2 direction, float rangeMeters, EnemyRuntimeController exclude)
        {
            if (combatController == null || direction.sqrMagnitude <= 0.001f)
            {
                return null;
            }

            EnemyRuntimeController best = null;
            var bestScore = float.MaxValue;
            foreach (var enemy in combatController.Enemies)
            {
                if (!IsValidTarget(enemy) || enemy == exclude)
                {
                    continue;
                }

                var distance = DistanceTo(enemy);
                if (distance > rangeMeters)
                {
                    continue;
                }

                var toEnemy = DirectionTo(enemy);
                var angle = Vector2.Angle(direction, toEnemy);
                if (angle > RetargetSectorDegrees * 0.5f)
                {
                    continue;
                }

                var score = angle + distance * 3f;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = enemy;
            }

            return best;
        }

        private void UpdateAutoLock()
        {
            if (currentLockMode == PlayerTargetLockMode.ManualRetarget)
            {
                return;
            }

            var nearest = FindNearestTarget(SoftAutoLockRangeMeters);
            if (nearest == null)
            {
                lockedEnemy = null;
                currentLockMode = PlayerTargetLockMode.None;
                return;
            }

            if (currentLockMode != PlayerTargetLockMode.Auto || !IsValidTarget(lockedEnemy))
            {
                lockedEnemy = nearest;
                currentLockMode = PlayerTargetLockMode.Auto;
                return;
            }

            if (nearest == lockedEnemy)
            {
                return;
            }

            var currentDistance = DistanceTo(lockedEnemy);
            var nearestDistance = DistanceTo(nearest);
            if (nearestDistance + AutoSwitchCloserMarginMeters <= currentDistance)
            {
                lockedEnemy = nearest;
            }
        }

        private void ValidateCurrentTarget()
        {
            if (currentLockMode == PlayerTargetLockMode.None)
            {
                return;
            }

            var maxRange = currentLockMode == PlayerTargetLockMode.ManualRetarget
                ? ExplicitLockRangeMeters * 1.25f
                : SoftAutoLockRangeMeters;
            if (IsValidTarget(lockedEnemy) && DistanceTo(lockedEnemy) <= maxRange)
            {
                return;
            }

            lockedEnemy = null;
            currentLockMode = PlayerTargetLockMode.None;
            HideLockMarker();
        }

        private void ApplyPresentationFacing()
        {
            if (playerVisualRoot == null)
            {
                return;
            }

            var facing = AttackDirection;
            var direction = new Vector3(facing.x, 0f, facing.y);
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            playerVisualRoot.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void UpdateLockMarker()
        {
            if (!IsTargetLocked)
            {
                HideLockMarker();
                return;
            }

            EnsureLockMarker();
            if (lockMarker == null)
            {
                return;
            }

            lockMarker.SetActive(true);
            lockMarker.transform.SetParent(LockedEnemy.transform, false);
            lockMarker.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            lockMarker.transform.localRotation = Quaternion.identity;

            var radius = Mathf.Max(0.42f, LockedEnemy.RadiusMeters + 0.22f);
            var line = lockMarker.GetComponent<LineRenderer>();
            if (line == null)
            {
                return;
            }

            line.positionCount = RingSegments;
            for (var index = 0; index < RingSegments; index++)
            {
                var angle = (Mathf.PI * 2f * index) / RingSegments;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void EnsureLockMarker()
        {
            if (lockMarker != null)
            {
                return;
            }

            lockMarker = new GameObject("PlayerTargetLockRing");
            var line = lockMarker.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.startColor = new Color(1f, 0.88f, 0.22f, 0.9f);
            line.endColor = new Color(1f, 0.88f, 0.22f, 0.9f);
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lockMarkerMaterial = new Material(shader)
                {
                    color = new Color(1f, 0.88f, 0.22f, 0.9f)
                };
                line.material = lockMarkerMaterial;
            }
        }

        private void HideLockMarker()
        {
            if (lockMarker != null)
            {
                lockMarker.SetActive(false);
            }
        }

        private bool IsValidTarget(EnemyRuntimeController enemy)
        {
            return enemy != null && enemy.IsAlive && enemy.gameObject.activeInHierarchy;
        }

        private float DistanceTo(EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return float.MaxValue;
            }

            var delta = enemy.transform.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private Vector2 DirectionTo(EnemyRuntimeController enemy)
        {
            if (enemy == null)
            {
                return BodyFacingDirection;
            }

            var delta = enemy.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.001f)
            {
                return BodyFacingDirection;
            }

            return SafeDirection(new Vector2(delta.x, delta.z));
        }

        private bool TryResolveMouseAimDirection(GameplayInputSnapshot input, out Vector2 direction)
        {
            direction = Vector2.zero;
            if (!input.HasPointerScreenPosition)
            {
                return false;
            }

            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(input.PointerScreenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            var point = ray.GetPoint(enter);
            var delta = point - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction = SafeDirection(new Vector2(delta.x, delta.z));
            return true;
        }

        private static Vector2 SafeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        }
    }
}
