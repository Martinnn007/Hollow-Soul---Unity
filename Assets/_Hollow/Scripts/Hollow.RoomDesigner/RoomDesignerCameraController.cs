using UnityEngine;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerCameraController
    {
        private const float FollowSharpness = 9f;
        private Vector3 targetPosition;
        private float targetHeight = 10f;
        private float targetDistance = 8f;
        private bool initialized;

        public Vector3 TargetPosition => targetPosition;

        public void SetTarget(Vector3 roomLocalTarget, RoomDesignerFootprintPreset preset)
        {
            targetPosition = new Vector3(roomLocalTarget.x, 0f, roomLocalTarget.z);
            var dimensions = RoomDesignerFootprintUtility.Dimensions(preset);
            var longest = Mathf.Max(dimensions.x, dimensions.y);
            targetHeight = Mathf.Max(10f, longest * 0.55f);
            targetDistance = Mathf.Max(8f, longest * 0.45f);
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

            var desiredPosition = targetPosition + new Vector3(0f, targetHeight, -targetDistance);
            if (immediate || !initialized)
            {
                camera.transform.position = desiredPosition;
                initialized = true;
            }
            else
            {
                var t = 1f - Mathf.Exp(-FollowSharpness * deltaTime);
                camera.transform.position = Vector3.Lerp(camera.transform.position, desiredPosition, t);
            }

            camera.transform.LookAt(targetPosition, Vector3.up);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.36f, 0.34f, 0.31f, 1f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 54f;
            if (camera.orthographic)
            {
                camera.orthographicSize = Mathf.Max(targetHeight * 0.55f, 6f);
            }
        }
    }
}
