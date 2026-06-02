using Hollow.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class ManualAimProjectionTests : InputTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            GameplayInputReader.SetExternalMoveOverride(Vector2.zero);
        }

        [TearDown]
        public override void TearDown()
        {
            GameplayInputReader.SetExternalMoveOverride(Vector2.zero);
            base.TearDown();
        }

        [Test]
        public void WindowsProfileCameraYawPitchUsesCameraUpForScreenY()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var root = new GameObject("WindowsRoot");
            var cameraObject = new GameObject("Main Camera");
            var hiddenCameras = HideExistingCameras();
            try
            {
                var camera = ConfigureCamera(cameraObject, new Vector3(-6.5f, 8.25f, -6.5f), new Vector3(42f, 45f, 0f));
                Set(gamepad.rightStick, Vector2.up);

                var input = GameplayInputReader.ReadCurrent(root.transform);
                var expected = ExpectedProjectedDirection(camera.transform.up, root.transform);

                AssertProjected(expected, input.Shoot);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
                RestoreHiddenCameras(hiddenCameras);
            }
        }

        [Test]
        public void VisionOSBoundedYawAndScaleProjectsIntoGameplayRoot()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var root = new GameObject("VisionOSBoundedRoot");
            var cameraObject = new GameObject("Main Camera");
            var hiddenCameras = HideExistingCameras();
            try
            {
                root.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                root.transform.localScale = Vector3.one * 0.5f;
                var camera = ConfigureCamera(cameraObject, new Vector3(0f, 1.35f, -2.4f), new Vector3(24f, 0f, 0f));
                Set(gamepad.rightStick, Vector2.up);

                var input = GameplayInputReader.ReadCurrent(root.transform);
                var expected = ExpectedProjectedDirection(camera.transform.up, root.transform);

                AssertProjected(expected, input.Shoot);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
                RestoreHiddenCameras(hiddenCameras);
            }
        }

        [Test]
        public void StraightDownCameraScreenUpProjectsToWorldForward()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var root = new GameObject("StraightDownRoot");
            var cameraObject = new GameObject("Main Camera");
            var hiddenCameras = HideExistingCameras();
            try
            {
                ConfigureCamera(cameraObject, new Vector3(0f, 12f, 0f), new Vector3(90f, 0f, 0f));
                Set(gamepad.rightStick, Vector2.up);

                var input = GameplayInputReader.ReadCurrent(root.transform);

                Assert.AreEqual(0f, input.Shoot.x, 0.001f);
                Assert.AreEqual(1f, input.Shoot.y, 0.001f);
                Assert.AreEqual(1f, input.Shoot.magnitude, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
                RestoreHiddenCameras(hiddenCameras);
            }
        }

        [Test]
        public void NoCameraFallbackKeepsRawScreenVector()
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var activeStates = new bool[cameras.Length];
            try
            {
                for (var index = 0; index < cameras.Length; index++)
                {
                    activeStates[index] = cameras[index].gameObject.activeSelf;
                    cameras[index].gameObject.SetActive(false);
                }

                var projected = GameplayInputProjection.ScreenVectorToGameplayVector(new Vector2(0.25f, 0.75f), null);
                var expected = Vector2.ClampMagnitude(new Vector2(0.25f, 0.75f), 1f);

                Assert.AreEqual(expected.x, projected.x, 0.001f);
                Assert.AreEqual(expected.y, projected.y, 0.001f);
            }
            finally
            {
                for (var index = 0; index < cameras.Length; index++)
                {
                    if (cameras[index] != null)
                    {
                        cameras[index].gameObject.SetActive(activeStates[index]);
                    }
                }
            }
        }

        private static Camera ConfigureCamera(GameObject cameraObject, Vector3 position, Vector3 eulerAngles)
        {
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.Euler(eulerAngles);
            camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
            return camera;
        }

        private static HiddenCameraState[] HideExistingCameras()
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var states = new HiddenCameraState[cameras.Length];
            for (var index = 0; index < cameras.Length; index++)
            {
                states[index] = new HiddenCameraState(cameras[index], cameras[index].gameObject.activeSelf);
                cameras[index].gameObject.SetActive(false);
            }

            return states;
        }

        private static void RestoreHiddenCameras(HiddenCameraState[] states)
        {
            for (var index = 0; index < states.Length; index++)
            {
                if (states[index].Camera != null)
                {
                    states[index].Camera.gameObject.SetActive(states[index].WasActive);
                }
            }
        }

        private static Vector2 ExpectedProjectedDirection(Vector3 cameraAxis, Transform gameplayRoot)
        {
            var world = Vector3.ProjectOnPlane(cameraAxis, Vector3.up).normalized;
            var local = gameplayRoot.InverseTransformDirection(world);
            return new Vector2(local.x, local.z).normalized;
        }

        private static void AssertProjected(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(1f, actual.magnitude, 0.001f);
        }

        private readonly struct HiddenCameraState
        {
            public HiddenCameraState(Camera camera, bool wasActive)
            {
                Camera = camera;
                WasActive = wasActive;
            }

            public Camera Camera { get; }

            public bool WasActive { get; }
        }
    }
}
