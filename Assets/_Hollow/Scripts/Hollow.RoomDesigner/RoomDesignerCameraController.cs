using UnityEngine;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerCameraController
    {
        private const float FollowSharpness = 9f;
        private Vector3 targetPosition;
        private float targetHeight = 10f;
        private float targetDistance = 8f;
        private float targetOrthographicSize = 7f;
        private bool initialized;

        public Vector3 TargetPosition => targetPosition;

        public RoomDesignerCameraViewMode ViewMode { get; private set; } = RoomDesignerCameraViewMode.Perspective;

        public void SetViewMode(RoomDesignerCameraViewMode mode)
        {
            ViewMode = mode;
        }

        public void SetTarget(Vector3 roomLocalTarget, RoomDesignerFootprintPreset preset)
        {
            targetPosition = new Vector3(roomLocalTarget.x, 0f, roomLocalTarget.z);
            var dimensions = RoomDesignerFootprintUtility.Dimensions(preset);
            var longest = Mathf.Max(dimensions.x, dimensions.y);
            targetHeight = Mathf.Max(10f, longest * 0.55f);
            targetDistance = Mathf.Max(8f, longest * 0.45f);
            targetOrthographicSize = Mathf.Max(6f, longest * 0.56f);
        }

        public void ApplyImmediate(Camera camera)
        {
            Apply(camera, 1f, immediate: true);
        }

        public void Tick(Camera camera, float deltaTime)
        {
            Apply(camera, Mathf.Max(0.001f, deltaTime), immediate: false);
        }

        private void Apply(Camera camera, float deltaTime, bool immediate)
        {
            if (camera == null)
            {
                return;
            }

            var desiredPosition = ViewMode == RoomDesignerCameraViewMode.TopDown
                ? targetPosition + new Vector3(0f, Mathf.Max(16f, targetHeight + 8f), 0f)
                : targetPosition + new Vector3(0f, targetHeight, -targetDistance);
            var desiredRotation = ViewMode == RoomDesignerCameraViewMode.TopDown
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.LookRotation((targetPosition - desiredPosition).normalized, Vector3.up);
            if (immediate || !initialized)
            {
                camera.transform.position = desiredPosition;
                camera.transform.rotation = desiredRotation;
                initialized = true;
            }
            else
            {
                var t = 1f - Mathf.Exp(-FollowSharpness * deltaTime);
                camera.transform.position = Vector3.Lerp(camera.transform.position, desiredPosition, t);
                camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, desiredRotation, t);
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.36f, 0.34f, 0.31f, 1f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 54f;
            camera.orthographic = ViewMode == RoomDesignerCameraViewMode.TopDown;
            if (camera.orthographic)
            {
                camera.orthographicSize = immediate || !initialized
                    ? targetOrthographicSize
                    : Mathf.Lerp(camera.orthographicSize, targetOrthographicSize, 1f - Mathf.Exp(-FollowSharpness * deltaTime));
            }
        }
    }
}
