using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Input;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerMovementController : MonoBehaviour
    {
        public const float DefaultSpeedMetersPerSecond = 4f;

        [SerializeField] private float speedMetersPerSecond = DefaultSpeedMetersPerSecond;
        [SerializeField] private float runSpeedBonusMetersPerSecond;
        [SerializeField] private float temporarySpeedBonusMetersPerSecond;
        [SerializeField] private float radiusMeters = PlaceholderPlayerController.DefaultRadiusMeters;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private RoomCombatController roomCombatController;
        [SerializeField] private PlayerDefenseController defenseController;
        [SerializeField] private PlayerWeaponController weaponController;
        private float temporarySpeedEndTime;
        private bool currentFrameGuardHeld;
        private EnemyRuntimeController lastBodyBlockedEnemy;
        private float nextBodyBumpStimulusTime;
        private const float BodyBumpStimulusIntervalSeconds = 0.2f;

        public float SpeedMetersPerSecond => (speedMetersPerSecond + runSpeedBonusMetersPerSecond + CurrentTemporarySpeedBonus) * GuardSpeedMultiplier;

        private float CurrentTemporarySpeedBonus => Time.time < temporarySpeedEndTime ? temporarySpeedBonusMetersPerSecond : 0f;

        private float GuardSpeedMultiplier => (defenseController != null && defenseController.IsGuarding) || currentFrameGuardHeld
            ? (defenseController != null ? defenseController.GuardMoveMultiplier : ShieldGuardProfileDefinition.Resolve(null).GuardMoveMultiplier)
            : 1f;

        public RoomRuntimeRoot RoomRuntimeRoot => roomRuntimeRoot;

        public void Configure(RoomRuntimeRoot room)
        {
            roomRuntimeRoot = room;
        }

        public void Configure(RoomRuntimeRoot room, RoomCombatController combat)
        {
            roomRuntimeRoot = room;
            roomCombatController = combat;
        }

        public void BindCombatController(RoomCombatController combat)
        {
            roomCombatController = combat;
        }

        public void ConfigureStats(float speedBonusMetersPerSecond)
        {
            runSpeedBonusMetersPerSecond = Mathf.Max(0f, speedBonusMetersPerSecond);
        }

        public void ConfigureDerivedStats(float nextSpeedMetersPerSecond)
        {
            speedMetersPerSecond = Mathf.Max(0.1f, nextSpeedMetersPerSecond);
            runSpeedBonusMetersPerSecond = 0f;
        }

        public void ApplyTemporarySpeedBonus(float bonusMetersPerSecond, float durationSeconds)
        {
            temporarySpeedBonusMetersPerSecond = Mathf.Max(0f, bonusMetersPerSecond);
            temporarySpeedEndTime = Time.time + Mathf.Max(0f, durationSeconds);
        }

        private void Update()
        {
            if (GameplayPauseState.IsPaused)
            {
                return;
            }

            var input = GameplayInputReader.ReadCurrent();
            currentFrameGuardHeld = input.GuardHeld;
            Move(input.Move, Time.deltaTime);
            currentFrameGuardHeld = false;
        }

        public Vector3 Move(Vector2 moveInput, float deltaTime)
        {
            if (defenseController == null)
            {
                defenseController = GetComponent<PlayerDefenseController>();
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (deltaTime <= 0f)
            {
                return transform.localPosition;
            }

            var isRollTraveling = weaponController != null && weaponController.IsRollTraveling;
            if (!isRollTraveling && moveInput.sqrMagnitude < 0.0001f)
            {
                if (roomRuntimeRoot != null && !RoomLocalCollision.CanOccupy(roomRuntimeRoot, transform.localPosition, radiusMeters))
                {
                    transform.localPosition = RoomLocalCollision.ResolveNearestOccupiablePosition(
                        roomRuntimeRoot,
                        transform.localPosition,
                        radiusMeters,
                        Vector3.zero,
                        2.5f);
                }

                var bodyResult = ResolveEnemyBodies(transform.localPosition, transform.localPosition, Vector3.zero, false, Vector3.zero);
                transform.localPosition = bodyResult.Position;
                if (bodyResult.WasBlocked)
                {
                    EmitBodyBumpStimulus(bodyResult.BlockingEnemy, transform.localPosition);
                }

                return transform.localPosition;
            }

            var move = isRollTraveling
                ? weaponController.RollDirection
                : Vector2.ClampMagnitude(moveInput, 1f);
            var rollDirectionLocal = isRollTraveling ? new Vector3(move.x, 0f, move.y) : Vector3.zero;
            var speed = isRollTraveling
                ? weaponController.RollSpeedMetersPerSecond
                : SpeedMetersPerSecond * (weaponController != null && weaponController.IsAttackCommitted ? PlayerWeaponController.AttackMovementMultiplier : 1f);
            var step = new Vector3(move.x, 0f, move.y) * speed * deltaTime;
            var stepCount = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / CombatFeelTuning.MovementSubstepMeters));
            var increment = step / stepCount;
            var resolved = transform.localPosition;
            if (roomRuntimeRoot != null && !RoomLocalCollision.CanOccupy(roomRuntimeRoot, resolved, radiusMeters))
            {
                resolved = RoomLocalCollision.ResolveNearestOccupiablePosition(
                    roomRuntimeRoot,
                    resolved,
                    radiusMeters,
                    step.sqrMagnitude > 0.001f ? step.normalized : Vector3.zero,
                    2.5f);
            }

            for (var index = 0; index < stepCount; index++)
            {
                var next = RoomLocalCollision.ResolveMove(roomRuntimeRoot, resolved, resolved + increment, radiusMeters);
                var bodyResult = ResolveEnemyBodies(resolved, next, increment, isRollTraveling, rollDirectionLocal);
                next = bodyResult.Position;
                if (bodyResult.WasBlocked)
                {
                    EmitBodyBumpStimulus(bodyResult.BlockingEnemy, next);
                }

                if ((next - resolved).sqrMagnitude < 0.000001f)
                {
                    break;
                }

                resolved = next;
            }

            transform.localPosition = resolved;
            return resolved;
        }

        private PlayerEnemyBodyCollisionResult ResolveEnemyBodies(
            Vector3 currentLocal,
            Vector3 desiredLocal,
            Vector3 movementDirection,
            bool isRollTraveling,
            Vector3 rollDirectionLocal)
        {
            if (roomCombatController == null)
            {
                return new PlayerEnemyBodyCollisionResult(desiredLocal, null, false);
            }

            var result = PlayerEnemyBodyCollision.Resolve(
                roomRuntimeRoot,
                roomCombatController.Enemies,
                currentLocal,
                desiredLocal,
                radiusMeters,
                isRollTraveling,
                rollDirectionLocal);
            if (!result.WasBlocked && result.Position == desiredLocal)
            {
                return result;
            }

            if (roomRuntimeRoot != null && !RoomLocalCollision.CanOccupy(roomRuntimeRoot, result.Position, radiusMeters))
            {
                var corrected = RoomLocalCollision.ResolveNearestOccupiablePosition(
                    roomRuntimeRoot,
                    result.Position,
                    radiusMeters,
                    movementDirection.sqrMagnitude > 0.001f ? movementDirection.normalized : Vector3.zero,
                    1.5f);
                return new PlayerEnemyBodyCollisionResult(corrected, result.BlockingEnemy, result.WasBlocked);
            }

            return result;
        }

        private void EmitBodyBumpStimulus(EnemyRuntimeController enemy, Vector3 localPosition)
        {
            if (enemy == null)
            {
                return;
            }

            var now = Time.time;
            if (enemy == lastBodyBlockedEnemy && now < nextBodyBumpStimulusTime)
            {
                return;
            }

            lastBodyBlockedEnemy = enemy;
            nextBodyBumpStimulusTime = now + BodyBumpStimulusIntervalSeconds;
            enemy.ReceiveStimulus(EnemyStimulusKind.Bump, localPosition, now, EnemyStimulusTier.Normal, "player_body_block");
        }
    }
}
