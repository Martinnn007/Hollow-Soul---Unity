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
        [SerializeField] private float radiusMeters = PlaceholderPlayerController.DefaultRadiusMeters;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;

        public float SpeedMetersPerSecond => speedMetersPerSecond + runSpeedBonusMetersPerSecond;

        public RoomRuntimeRoot RoomRuntimeRoot => roomRuntimeRoot;

        public void Configure(RoomRuntimeRoot room)
        {
            roomRuntimeRoot = room;
        }

        public void ConfigureStats(float speedBonusMetersPerSecond)
        {
            runSpeedBonusMetersPerSecond = Mathf.Max(0f, speedBonusMetersPerSecond);
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
            var current = transform.localPosition;
            var desired = current + new Vector3(move.x, 0f, move.y) * SpeedMetersPerSecond * deltaTime;
            var resolved = RoomLocalCollision.ResolveMove(roomRuntimeRoot, current, desired, radiusMeters);
            transform.localPosition = resolved;
            return resolved;
        }
    }
}
