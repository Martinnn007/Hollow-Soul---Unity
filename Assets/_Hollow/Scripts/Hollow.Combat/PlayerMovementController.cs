using Hollow.Entities;
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
        private float temporarySpeedEndTime;

        public float SpeedMetersPerSecond => speedMetersPerSecond + runSpeedBonusMetersPerSecond + CurrentTemporarySpeedBonus;

        private float CurrentTemporarySpeedBonus => Time.time < temporarySpeedEndTime ? temporarySpeedBonusMetersPerSecond : 0f;

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
            var input = GameplayInputReader.ReadCurrent();
            Move(input.Move, Time.deltaTime);
        }

        public Vector3 Move(Vector2 moveInput, float deltaTime)
        {
            if (moveInput.sqrMagnitude < 0.0001f || deltaTime <= 0f)
            {
                return transform.localPosition;
            }

            var move = Vector2.ClampMagnitude(moveInput, 1f);
            var step = new Vector3(move.x, 0f, move.y) * SpeedMetersPerSecond * deltaTime;
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
