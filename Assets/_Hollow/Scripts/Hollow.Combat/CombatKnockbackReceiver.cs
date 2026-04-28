using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class CombatKnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private float radiusMeters = 0.3f;
        [SerializeField] private bool ignoreObstacles;
        [SerializeField] private float resistanceMultiplier = 1f;

        private Vector3 velocity;
        private float remainingSeconds;

        public bool IsKnockbackActive => remainingSeconds > 0f;

        public void Configure(RoomRuntimeRoot room, float radius, bool nextIgnoreObstacles, float nextResistanceMultiplier)
        {
            roomRuntimeRoot = room;
            radiusMeters = Mathf.Max(CombatFeelTuning.MinimumCollisionRadiusMeters, radius);
            ignoreObstacles = nextIgnoreObstacles;
            resistanceMultiplier = Mathf.Clamp01(nextResistanceMultiplier);
        }

        public void ApplyKnockback(Vector3 direction, float meters, float seconds)
        {
            var flatDirection = new Vector3(direction.x, 0f, direction.z);
            var distance = Mathf.Max(0f, meters) * resistanceMultiplier;
            if (flatDirection.sqrMagnitude < 0.001f || distance <= 0f)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0.01f, seconds);
            velocity = flatDirection.normalized * (distance / remainingSeconds);
            VfxPresenter.Play(VfxCueId.KnockbackImpact, transform.position, transform.parent);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (remainingSeconds <= 0f)
            {
                return;
            }

            var stepSeconds = Mathf.Min(Mathf.Max(0f, deltaTime), remainingSeconds);
            remainingSeconds -= stepSeconds;
            var desired = transform.localPosition + velocity * stepSeconds;
            transform.localPosition = ignoreObstacles
                ? RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, desired, radiusMeters)
                : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);

            if (remainingSeconds <= 0f)
            {
                velocity = Vector3.zero;
            }
        }
    }
}
