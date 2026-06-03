using Hollow.Core;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BossKeyPickup : MonoBehaviour, IPooledRuntimeObject
    {
        public const float DefaultRotationDegreesPerSecond = 24f;
        public const float DefaultHoverAmplitudeMeters = 0.035f;
        public const float DefaultHoverFrequencyHz = 0.75f;

        [SerializeField] private string roomId;
        [SerializeField] private bool claimed;
        [SerializeField] private float rotationDegreesPerSecond = DefaultRotationDegreesPerSecond;
        [SerializeField] private float hoverAmplitudeMeters = DefaultHoverAmplitudeMeters;
        [SerializeField] private float hoverFrequencyHz = DefaultHoverFrequencyHz;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation = Quaternion.identity;
        private float animationElapsedSeconds;
        private bool basePoseCaptured;

        public string RoomId => roomId;

        public bool Claimed => claimed;

        public float RotationDegreesPerSecond => Mathf.Max(0f, rotationDegreesPerSecond);

        public float HoverAmplitudeMeters => Mathf.Max(0f, hoverAmplitudeMeters);

        public float HoverFrequencyHz => Mathf.Max(0f, hoverFrequencyHz);

        public void Configure(string nextRoomId)
        {
            roomId = nextRoomId ?? string.Empty;
            claimed = false;
            CaptureBasePose();
        }

        private void Update()
        {
            TickPresentation(Time.deltaTime);
        }

        public void TickPresentation(float deltaTime)
        {
            if (!basePoseCaptured)
            {
                CaptureBasePose();
            }

            animationElapsedSeconds += Mathf.Max(0f, deltaTime);
            var hoverOffset = Mathf.Sin(animationElapsedSeconds * Mathf.PI * 2f * HoverFrequencyHz) * HoverAmplitudeMeters;
            transform.localPosition = baseLocalPosition + Vector3.up * hoverOffset;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, animationElapsedSeconds * RotationDegreesPerSecond, 0f);
        }

        public bool Claim()
        {
            if (claimed)
            {
                return false;
            }

            claimed = true;
            return true;
        }

        public void OnRentFromPool()
        {
            claimed = false;
            basePoseCaptured = false;
            animationElapsedSeconds = 0f;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            ResetPresentationPose();
            roomId = string.Empty;
            claimed = false;
            basePoseCaptured = false;
            animationElapsedSeconds = 0f;
        }

        private void CaptureBasePose()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            animationElapsedSeconds = 0f;
            basePoseCaptured = true;
        }

        private void ResetPresentationPose()
        {
            if (!basePoseCaptured)
            {
                return;
            }

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
        }
    }
}
