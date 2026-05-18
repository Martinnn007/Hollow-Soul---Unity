using Hollow.Diagnostics;
using Hollow.Input;
using NUnit.Framework;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hollow.Tests.EditMode
{
    public sealed class VisionOSGameplayInputDiagnosticsTests : InputTestFixture
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
        public void KeyboardWasdProducesMovement()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            Press(keyboard.dKey);

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            Assert.Greater(move.x, 0.6f);
            Assert.Greater(move.y, 0.6f);
            Assert.LessOrEqual(move.magnitude, 1f);
        }

        [Test]
        public void NonCurrentKeyboardStillProducesMovement()
        {
            var physicalKeyboard = InputSystem.AddDevice<Keyboard>();
            var onScreenKeyboard = InputSystem.AddDevice<Keyboard>();

            Press(physicalKeyboard.wKey);
            onScreenKeyboard.MakeCurrent();

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            Assert.Greater(move.y, 0.9f);
        }

        [Test]
        public void ExternalMoveOverrideProducesMovement()
        {
            GameplayInputReader.SetExternalMoveOverride(new Vector2(-0.5f, 0.25f));

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            Assert.AreEqual(-0.5f, move.x, 0.001f);
            Assert.AreEqual(0.25f, move.y, 0.001f);
        }

        [Test]
        public void GamepadLeftStickPreservesAnalogMagnitude()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Set(gamepad.leftStick, new Vector2(0.35f, -0.45f));

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            var sampled = gamepad.leftStick.ReadValue();
            Assert.AreEqual(sampled.x, move.x, 0.001f);
            Assert.AreEqual(sampled.y, move.y, 0.001f);
        }

        [Test]
        public void CameraRelativeMovementProjectsIntoRotatedGameplayRoot()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var root = new GameObject("RotatedGameplayRoot");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                Press(keyboard.wKey);

                var move = GameplayInputReader.ReadMoveForDiagnostics(root.transform);
                Assert.Less(move.x, -0.6f);
                Assert.Greater(move.y, 0.6f);
                Assert.AreEqual(1f, move.magnitude, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CameraRelativeRightStickAimProjectsIntoRotatedGameplayRoot()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var root = new GameObject("RotatedAimRoot");
            var cameraObject = new GameObject("Main Camera");
            try
            {
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                Set(gamepad.rightStick, Vector2.up);

                var input = GameplayInputReader.ReadCurrent(root.transform);
                Assert.Less(input.Shoot.x, -0.6f);
                Assert.Greater(input.Shoot.y, 0.6f);
                Assert.AreEqual(1f, input.Shoot.magnitude, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GamepadDpadFallbackProducesMovement()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Press(gamepad.dpad.up);
            Press(gamepad.dpad.left);

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            Assert.Less(move.x, -0.6f);
            Assert.Greater(move.y, 0.6f);
            Assert.LessOrEqual(move.magnitude, 1.0001f);
        }

        [Test]
        public void StrongestGamepadMoveWinsAcrossConnectedGamepads()
        {
            var strongGamepad = InputSystem.AddDevice<Gamepad>();
            var weakGamepad = InputSystem.AddDevice<Gamepad>();

            Set(weakGamepad.leftStick, new Vector2(0.25f, 0f));
            Set(strongGamepad.leftStick, new Vector2(0f, -0.8f));

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            var sampled = strongGamepad.leftStick.ReadValue();
            Assert.AreEqual(0f, move.x, 0.001f);
            Assert.AreEqual(sampled.y, move.y, 0.001f);
        }

        [Test]
        public void JoystickStickFallbackProducesMovement()
        {
            var joystick = InputSystem.AddDevice<Joystick>();

            Set(joystick.stick, new Vector2(-0.4f, 0.55f));

            var move = GameplayInputReader.ReadMoveForDiagnostics();
            var sampled = joystick.stick.ReadValue();
            Assert.AreEqual(sampled.x, move.x, 0.001f);
            Assert.AreEqual(sampled.y, move.y, 0.001f);
        }

        [Test]
        public void DiagnosticsDescribeConnectedInputDevicesListsAvailability()
        {
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Gamepad>();
            InputSystem.AddDevice<Joystick>();

            var description = GameplayInputReader.DescribeConnectedInputDevices();
            StringAssert.Contains("keyboard=yes", description);
            StringAssert.Contains("gamepad=yes", description);
            StringAssert.Contains("joystick=yes", description);
            StringAssert.Contains("currentKeyboard=", description);
            StringAssert.Contains("currentGamepad=", description);
            StringAssert.Contains("gamepadDetails=[", description);
            StringAssert.Contains("layout=", description);
            StringAssert.Contains("lastUpdate=", description);
            StringAssert.Contains("devices=", description);
        }

        [Test]
        public void DiagnosticsInputSamplesIncludeKeyboardGamepadAndExternalValues()
        {
            InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Set(gamepad.leftStick, new Vector2(0.5f, 0.25f));
            GameplayInputReader.SetExternalMoveOverride(Vector2.up);

            var samples = GameplayInputReader.DescribeCurrentInputSamples();
            StringAssert.Contains("keyboardMove=", samples);
            StringAssert.Contains($"gamepadMove={FormatVector(gamepad.leftStick.ReadValue())}", samples);
            StringAssert.Contains("dpad=", samples);
            StringAssert.Contains("layout=", samples);
            StringAssert.Contains("externalMove=(0.00,1.00)", samples);
            StringAssert.Contains("keyboards=[", samples);
            StringAssert.Contains("gamepads=[", samples);
        }

        [Test]
        public void DiagnosticsHudLineReportsCurrentMove()
        {
            InputSystem.AddDevice<Gamepad>();
            var gameObject = new GameObject("Diagnostics");
            try
            {
                var diagnostics = gameObject.AddComponent<VisionOSGameplayInputDiagnostics>();

                var hudLine = diagnostics.BuildHudLine(new Vector2(0.25f, -0.5f));
                StringAssert.Contains("Keyboard:", hudLine);
                StringAssert.Contains("Gamepad:", hudLine);
                StringAssert.Contains("connected/no events", hudLine);
                StringAssert.Contains("Joystick:", hudLine);
                StringAssert.Contains("Move: 0.25/-0.50", hudLine);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static string FormatVector(Vector2 value)
        {
            return string.Concat(
                "(",
                value.x.ToString("0.00", CultureInfo.InvariantCulture),
                ",",
                value.y.ToString("0.00", CultureInfo.InvariantCulture),
                ")");
        }
    }
}
