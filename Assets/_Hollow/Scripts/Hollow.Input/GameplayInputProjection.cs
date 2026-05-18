using UnityEngine;

namespace Hollow.Input
{
    public static class GameplayInputProjection
    {
        public static Vector2 ScreenVectorToGameplayVector(Vector2 screenVector, Transform gameplayRoot)
        {
            var rawMagnitude = Mathf.Min(1f, screenVector.magnitude);
            if (rawMagnitude <= 0.0001f)
            {
                return Vector2.zero;
            }

            if (gameplayRoot == null)
            {
                return Vector2.ClampMagnitude(screenVector, 1f);
            }

            var camera = ResolveFloorPointingCamera();
            if (camera == null)
            {
                return Vector2.ClampMagnitude(screenVector, 1f);
            }

            var right = ProjectCameraAxis(camera.transform.right, gameplayRoot != null ? gameplayRoot.TransformDirection(Vector3.right) : Vector3.right);
            var forward = ProjectCameraAxis(camera.transform.forward, gameplayRoot != null ? gameplayRoot.TransformDirection(Vector3.forward) : Vector3.forward);
            if (right.sqrMagnitude <= 0.0001f || forward.sqrMagnitude <= 0.0001f)
            {
                return Vector2.ClampMagnitude(screenVector, 1f);
            }

            var worldDirection = right.normalized * screenVector.x + forward.normalized * screenVector.y;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector2.zero;
            }

            var gameplayDirection = gameplayRoot != null
                ? gameplayRoot.InverseTransformDirection(worldDirection)
                : worldDirection;
            var projected = new Vector2(gameplayDirection.x, gameplayDirection.z);
            return Vector2.ClampMagnitude(projected, rawMagnitude);
        }

        private static Vector3 ProjectCameraAxis(Vector3 cameraAxis, Vector3 fallbackAxis)
        {
            var projected = Vector3.ProjectOnPlane(cameraAxis, Vector3.up);
            if (projected.sqrMagnitude > 0.0001f)
            {
                return projected;
            }

            return Vector3.ProjectOnPlane(fallbackAxis, Vector3.up);
        }

        public static bool TryScreenPointToGameplayPlane(
            Vector2 screenPosition,
            Transform gameplayRoot,
            Vector3 planeWorldPoint,
            out Vector3 localPoint)
        {
            localPoint = Vector3.zero;
            var camera = ResolveFloorPointingCamera(screenPosition);
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!TryIntersectHorizontalPlane(ray, planeWorldPoint, out var worldPoint))
            {
                return false;
            }

            localPoint = gameplayRoot != null ? gameplayRoot.InverseTransformPoint(worldPoint) : worldPoint;
            return true;
        }

        private static Camera ResolveFloorPointingCamera()
        {
            return ResolveFloorPointingCamera(Vector2.zero, requireScreenPoint: false);
        }

        private static Camera ResolveFloorPointingCamera(Vector2 screenPosition)
        {
            return ResolveFloorPointingCamera(screenPosition, requireScreenPoint: true);
        }

        private static Camera ResolveFloorPointingCamera(Vector2 screenPosition, bool requireScreenPoint)
        {
            var main = Camera.main;
            if (IsUsableCamera(main, screenPosition, requireScreenPoint) &&
                IsFloorPointingCamera(main, screenPosition))
            {
                return main;
            }

            Camera best = null;
            var bestScore = float.NegativeInfinity;
            Camera fallback = null;
            var fallbackScore = float.NegativeInfinity;

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                ConsiderCamera(cameras[i], screenPosition, requireScreenPoint, ref best, ref bestScore, ref fallback, ref fallbackScore);
            }

            if (best != null)
            {
                return best;
            }

            if (fallback != null)
            {
                return fallback;
            }

            return main != null ? main : Object.FindAnyObjectByType<Camera>();
        }

        private static void ConsiderCamera(
            Camera camera,
            Vector2 screenPosition,
            bool requireScreenPoint,
            ref Camera best,
            ref float bestScore,
            ref Camera fallback,
            ref float fallbackScore)
        {
            if (!IsUsableCamera(camera, screenPosition, requireScreenPoint))
            {
                return;
            }

            var score = camera.depth;
            if (camera.CompareTag("MainCamera"))
            {
                score += 100f;
            }

            if (IsFloorPointingCamera(camera, screenPosition))
            {
                if (score > bestScore)
                {
                    best = camera;
                    bestScore = score;
                }

                return;
            }

            if (score > fallbackScore)
            {
                fallback = camera;
                fallbackScore = score;
            }
        }

        private static bool IsUsableCamera(Camera camera, Vector2 screenPosition, bool requireScreenPoint)
        {
            if (camera == null || !camera.isActiveAndEnabled || camera.cameraType != CameraType.Game)
            {
                return false;
            }

            var pixelRect = camera.pixelRect;
            return !requireScreenPoint ||
                   pixelRect.width <= 0.01f ||
                   pixelRect.height <= 0.01f ||
                   pixelRect.Contains(screenPosition);
        }

        private static bool IsFloorPointingCamera(Camera camera, Vector2 screenPosition)
        {
            var ray = camera.ScreenPointToRay(screenPosition);
            return Vector3.Dot(Vector3.up, ray.direction) < -0.0001f;
        }

        private static bool TryIntersectHorizontalPlane(Ray ray, Vector3 planeWorldPoint, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            var denominator = Vector3.Dot(Vector3.up, ray.direction);
            if (Mathf.Abs(denominator) <= 0.0001f)
            {
                return false;
            }

            var enter = Vector3.Dot(Vector3.up, planeWorldPoint - ray.origin) / denominator;
            worldPoint = ray.GetPoint(enter);
            return true;
        }
    }
}
