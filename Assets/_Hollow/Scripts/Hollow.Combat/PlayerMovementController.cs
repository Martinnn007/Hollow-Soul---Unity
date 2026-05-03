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
        [SerializeField] private PlayerDefenseController defenseController;
        [SerializeField] private PlayerWeaponController weaponController;
        private float temporarySpeedEndTime;
        private bool currentFrameGuardHeld;

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

            var isRolling = weaponController != null && weaponController.IsRolling;
            if (!isRolling && moveInput.sqrMagnitude < 0.0001f)
            {
                return transform.localPosition;
            }

            var move = isRolling
                ? weaponController.RollDirection
                : Vector2.ClampMagnitude(moveInput, 1f);
            var speed = isRolling
                ? weaponController.RollSpeedMetersPerSecond
                : SpeedMetersPerSecond * (weaponController != null && weaponController.IsAttackCommitted ? PlayerWeaponController.AttackMovementMultiplier : 1f);
            var step = new Vector3(move.x, 0f, move.y) * speed * deltaTime;
            var stepCount = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / CombatFeelTuning.MovementSubstepMeters));
            var increment = step / stepCount;
            var resolved = transform.localPosition;
            for (var index = 0; index < stepCount; index++)
            {
                var next = RoomLocalCollision.ResolveMove(roomRuntimeRoot, resolved, resolved + increment, radiusMeters);
                if ((next - resolved).sqrMagnitude < 0.000001f)
                {
                    break;
                }

                resolved = next;
            }

            transform.localPosition = resolved;
            return resolved;
        }
    }
}
