using System.Collections;
using Hollow.Combat;
using Hollow.Diagnostics;
using Hollow.Presentation;
using NUnit.Framework;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class VisionOSGameplayInputPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator BoundedVisionOSSceneMovesPlayerFromInjectedGamepadStick()
        {
            yield return SceneManager.LoadSceneAsync("Game_VisionOS_Bounded", LoadSceneMode.Single);

            PlayerMovementController player = null;
            VisionOSGameplayInputDiagnostics diagnostics = null;
            VolumeCamera volumeCamera = null;
            PlatformPresentationRoot presentationRoot = null;
            for (var frame = 0; frame < 30 && (player == null || diagnostics == null || volumeCamera == null || presentationRoot == null); frame++)
            {
                player = Object.FindFirstObjectByType<PlayerMovementController>();
                diagnostics = Object.FindFirstObjectByType<VisionOSGameplayInputDiagnostics>();
                volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
                presentationRoot = Object.FindFirstObjectByType<PlatformPresentationRoot>();
                yield return null;
            }

            Assert.IsNotNull(player, "Bounded visionOS gameplay scene should spawn a player movement controller.");
            Assert.IsNotNull(diagnostics, "Bounded visionOS gameplay scene should include input diagnostics.");
            Assert.IsNotNull(volumeCamera, "Bounded visionOS gameplay scene should include an explicit VolumeCamera.");
            Assert.IsNotNull(presentationRoot, "Bounded visionOS gameplay scene should include a presentation root.");
            Assert.AreEqual(PresentationOrientationPolicy.VisionOSGameplayWorldYawDegrees, presentationRoot.WorldYawDegrees, 0.001f);
            Assert.AreEqual(Quaternion.Euler(0f, PresentationOrientationPolicy.VisionOSGameplayWorldYawDegrees, 0f), presentationRoot.transform.localRotation);
            AssertVectorApproximately(new Vector3(8f, 5.333333f, 8f), volumeCamera.Dimensions);
            AssertVectorApproximately(new Vector3(0f, 2.6666665f, 0.25f), volumeCamera.transform.localPosition);
            Assert.AreEqual(0f, volumeCamera.transform.localPosition.y - volumeCamera.Dimensions.y * 0.5f, 0.001f);
            Assert.IsNull(GameObject.Find("VisionOSMovePad"), "The spatial diagnostics move pad should be disabled for gamepad-only testing.");

            var gamepad = InputSystem.AddDevice<Gamepad>();
            var startPosition = player.transform.position;

            Set(gamepad.leftStick, Vector2.right);
            for (var frame = 0; frame < 12; frame++)
            {
                yield return null;
            }

            var travelled = Vector3.Distance(startPosition, player.transform.position);
            Assert.Greater(travelled, 0.01f, "Injected gamepad left stick input should move the player.");
            StringAssert.Contains("Gamepad:", diagnostics.BuildHudLine(Vector2.right));
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }
    }
}
