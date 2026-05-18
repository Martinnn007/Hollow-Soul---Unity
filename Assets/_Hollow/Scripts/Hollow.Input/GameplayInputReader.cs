using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Hollow.Input
{
    public static class GameplayInputReader
    {
        private const float StickDeadZone = 0.2f;
        private const float MouseAimMovePixels = 0.5f;

        private static Vector2 externalMoveOverride;
        private static int externalMoveOverrideFrame = -1000;

        public static bool HasKeyboardDevice => HasAnyKeyboardDevice();

        public static bool HasGamepadDevice => Gamepad.current != null || Gamepad.all.Count > 0;

        public static bool HasJoystickDevice => Joystick.current != null || Joystick.all.Count > 0;

        public static GameplayInputSnapshot ReadCurrent()
        {
            return ReadCurrent(null);
        }

        public static GameplayInputSnapshot ReadCurrent(Transform gameplayRoot)
        {
            var pausePressed = ReadPausePressed();
            if (GameplayPauseState.IsPaused)
            {
                return new GameplayInputSnapshot(
                    Vector2.zero,
                    Vector2.zero,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    pausePressed,
                    false,
                    false);
            }

            var pointerScreenPosition = ReadPointerScreenPosition(out var hasPointerScreenPosition, out var mouseAimIntent);
            var lightAttackHeld = ReadLightAttackHeld();
            var heavyAttackHeld = ReadHeavyAttackHeld();
            return new GameplayInputSnapshot(
                ReadMove(gameplayRoot),
                ReadShoot(gameplayRoot),
                ReadInteractPressed(),
                ReadSwapWeaponPressed(),
                ReadLightAttackPressed(),
                ReadHeavyAttackPressed(),
                ReadUseActiveItemPressed(),
                ReadUseConsumableCardPressed(),
                ReadGuardHeld(),
                pausePressed,
                ReadRollPressed(),
                ReadLockTargetPressed(),
                pointerScreenPosition,
                hasPointerScreenPosition,
                mouseAimIntent,
                lightAttackHeld,
                ReadLightAttackReleased(lightAttackHeld),
                heavyAttackHeld,
                ReadHeavyAttackReleased(heavyAttackHeld));
        }

        public static Vector2 CardinalizeShoot(Vector2 rawShoot)
        {
            if (rawShoot.sqrMagnitude < StickDeadZone * StickDeadZone)
            {
                return Vector2.zero;
            }

            return Mathf.Abs(rawShoot.x) >= Mathf.Abs(rawShoot.y)
                ? new Vector2(Mathf.Sign(rawShoot.x), 0f)
                : new Vector2(0f, Mathf.Sign(rawShoot.y));
        }

        public static Vector2 QuantizeEightAxis(Vector2 rawDirection)
        {
            if (rawDirection.sqrMagnitude < StickDeadZone * StickDeadZone)
            {
                return Vector2.zero;
            }

            var angle = Mathf.Atan2(rawDirection.y, rawDirection.x) * Mathf.Rad2Deg;
            var snapped = Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped)).normalized;
        }

        public static Vector2 NormalizeAimDirection(Vector2 rawDirection)
        {
            if (rawDirection.sqrMagnitude < StickDeadZone * StickDeadZone)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(rawDirection, 1f).normalized;
        }

        public static Vector2 ReadMoveForDiagnostics()
        {
            return ReadMove();
        }

        public static Vector2 ReadMoveForDiagnostics(Transform gameplayRoot)
        {
            return ReadMove(gameplayRoot);
        }

        public static void SetExternalMoveOverride(Vector2 move)
        {
            externalMoveOverride = Vector2.ClampMagnitude(move, 1f);
            externalMoveOverrideFrame = Time.frameCount;
        }

        public static string DescribeConnectedInputDevices()
        {
            var builder = new StringBuilder(256);
            builder.Append("keyboard=").Append(HasKeyboardDevice ? "yes" : "no");
            builder.Append(" gamepad=").Append(HasGamepadDevice ? "yes" : "no");
            builder.Append(" joystick=").Append(HasJoystickDevice ? "yes" : "no");
            builder.Append(" currentKeyboard=").Append(Keyboard.current?.displayName ?? "none");
            builder.Append(" currentGamepad=").Append(Gamepad.current?.displayName ?? "none");
            builder.Append(" currentJoystick=").Append(Joystick.current?.displayName ?? "none");
            builder.Append(" devices=");

            var devices = InputSystem.devices;
            if (devices.Count == 0)
            {
                builder.Append("none");
                return builder.ToString();
            }

            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder
                    .Append(device.displayName)
                    .Append('(')
                    .Append(device.layout)
                    .Append('/');

                if (device.enabled)
                {
                    builder.Append("enabled");
                }
                else
                {
                    builder.Append("disabled");
                }

                builder.Append(')');
            }

            builder.Append(" gamepadDetails=[");
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                AppendGamepadDeviceDetails(builder, Gamepad.all[i]);
            }

            builder.Append(']');
            return builder.ToString();
        }

        public static string DescribeCurrentInputSamples()
        {
            return DescribeCurrentInputSamples(null);
        }

        public static string DescribeCurrentInputSamples(Transform gameplayRoot)
        {
            var builder = new StringBuilder(512);
            AppendVector(builder.Append("keyboardMove="), ReadKeyboardMove());
            AppendVector(builder.Append(" keyboardShoot="), ReadKeyboardShoot());
            AppendVector(builder.Append(" gamepadMove="), ReadStrongestGamepadMove());
            AppendVector(builder.Append(" gamepadShoot="), ReadStrongestGamepadStick(gamepad => gamepad.rightStick));
            AppendVector(builder.Append(" joystickMove="), ReadStrongestJoystickStick());
            AppendVector(builder.Append(" genericStick="), ReadStrongestGenericControllerVector());
            AppendVector(builder.Append(" externalMove="), ReadExternalMoveOverride());
            if (gameplayRoot != null)
            {
                AppendVector(builder.Append(" projectedMove="), ReadMove(gameplayRoot));
                AppendVector(builder.Append(" projectedShoot="), ReadShoot(gameplayRoot));
            }

            builder.Append(" keyboards=[");
            var keyboardIndex = 0;
            AppendKeyboardSample(builder, Keyboard.current, ref keyboardIndex, "current");
            foreach (var device in InputSystem.devices)
            {
                if (device == Keyboard.current || device is not Keyboard keyboard)
                {
                    continue;
                }

                AppendKeyboardSample(builder, keyboard, ref keyboardIndex, "device");
            }

            builder.Append("] gamepads=[");
            var gamepadIndex = 0;
            AppendGamepadSample(builder, Gamepad.current, ref gamepadIndex, "current");
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad == Gamepad.current)
                {
                    continue;
                }

                AppendGamepadSample(builder, gamepad, ref gamepadIndex, "device");
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static Vector2 ReadMove()
        {
            return ReadMove(null);
        }

        private static Vector2 ReadMove(Transform gameplayRoot)
        {
            var move = ReadKeyboardMove();
            move += ReadBestMoveStick();
            move += ReadExternalMoveOverride();

            return GameplayInputProjection.ScreenVectorToGameplayVector(Vector2.ClampMagnitude(move, 1f), gameplayRoot);
        }

        private static Vector2 ReadShoot()
        {
            return ReadShoot(null);
        }

        private static Vector2 ReadShoot(Transform gameplayRoot)
        {
            var shoot = ReadKeyboardShoot();
            shoot += ReadStrongestGamepadStick(gamepad => gamepad.rightStick);

            return GameplayInputProjection.ScreenVectorToGameplayVector(shoot, gameplayRoot);
        }

        private static Vector2 ReadPointerScreenPosition(out bool hasPointerScreenPosition, out bool mouseAimIntent)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                hasPointerScreenPosition = false;
                mouseAimIntent = false;
                return Vector2.zero;
            }

            hasPointerScreenPosition = true;
            var mouseDelta = mouse.delta.ReadValue();
            mouseAimIntent =
                mouseDelta.sqrMagnitude >= MouseAimMovePixels * MouseAimMovePixels ||
                mouse.leftButton.isPressed ||
                mouse.rightButton.isPressed;
            return mouse.position.ReadValue();
        }

        private static bool ReadInteractPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.eKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.buttonSouth);
        }

        private static bool ReadSwapWeaponPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.tabKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.leftShoulder);
        }

        private static bool ReadLightAttackPressed()
        {
            if (ReadAnyKeyboard(keyboard =>
                (keyboard.jKey.wasPressedThisFrame ||
                 keyboard.leftArrowKey.isPressed ||
                 keyboard.rightArrowKey.isPressed ||
                 keyboard.downArrowKey.isPressed ||
                 keyboard.upArrowKey.isPressed)))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.rightShoulder);
        }

        private static bool ReadLightAttackHeld()
        {
            if (ReadAnyKeyboard(keyboard =>
                (keyboard.jKey.isPressed ||
                 keyboard.leftArrowKey.isPressed ||
                 keyboard.rightArrowKey.isPressed ||
                 keyboard.downArrowKey.isPressed ||
                 keyboard.upArrowKey.isPressed)))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return true;
            }

            return ReadAnyGamepadButtonHeld(gamepad => gamepad.rightShoulder);
        }

        private static bool ReadLightAttackReleased(bool lightAttackHeld)
        {
            if (lightAttackHeld)
            {
                return false;
            }

            if (ReadAnyKeyboard(keyboard =>
                (keyboard.jKey.wasReleasedThisFrame ||
                 keyboard.leftArrowKey.wasReleasedThisFrame ||
                 keyboard.rightArrowKey.wasReleasedThisFrame ||
                 keyboard.downArrowKey.wasReleasedThisFrame ||
                 keyboard.upArrowKey.wasReleasedThisFrame)))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                return true;
            }

            return ReadAnyGamepadButtonReleased(gamepad => gamepad.rightShoulder);
        }

        private static bool ReadHeavyAttackPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.kKey.wasPressedThisFrame))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.rightTrigger);
        }

        private static bool ReadHeavyAttackHeld()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.kKey.isPressed))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                return true;
            }

            return ReadAnyGamepadButtonHeld(gamepad => gamepad.rightTrigger);
        }

        private static bool ReadHeavyAttackReleased(bool heavyAttackHeld)
        {
            if (heavyAttackHeld)
            {
                return false;
            }

            if (ReadAnyKeyboard(keyboard => keyboard.kKey.wasReleasedThisFrame))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasReleasedThisFrame)
            {
                return true;
            }

            return ReadAnyGamepadButtonReleased(gamepad => gamepad.rightTrigger);
        }

        private static bool ReadUseActiveItemPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.qKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.buttonNorth);
        }

        private static bool ReadUseConsumableCardPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.fKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.buttonWest);
        }

        private static bool ReadRollPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.spaceKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.buttonEast);
        }

        private static bool ReadLockTargetPressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.lKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.rightStickButton);
        }

        private static bool ReadGuardHeld()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                return true;
            }

            return ReadAnyGamepadTriggerHeld(gamepad => gamepad.leftTrigger, 0.5f);
        }

        public static bool ReadPausePressed()
        {
            if (ReadAnyKeyboard(keyboard => keyboard.escapeKey.wasPressedThisFrame))
            {
                return true;
            }

            return ReadAnyGamepadButtonPressed(gamepad => gamepad.startButton);
        }

        public static bool ReadDebugHudTogglePressed()
        {
            return ReadAnyKeyboard(keyboard => keyboard.f3Key.wasPressedThisFrame);
        }

        private static bool HasAnyKeyboardDevice()
        {
            if (Keyboard.current != null)
            {
                return true;
            }

            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ReadExternalMoveOverride()
        {
            return Time.frameCount - externalMoveOverrideFrame <= 1 ? externalMoveOverride : Vector2.zero;
        }

        private static Vector2 ReadKeyboardMove()
        {
            var move = ReadKeyboardMove(Keyboard.current);
            foreach (var device in InputSystem.devices)
            {
                if (device == Keyboard.current || device is not Keyboard keyboard)
                {
                    continue;
                }

                move += ReadKeyboardMove(keyboard);
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        private static Vector2 ReadKeyboardMove(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var move = Vector2.zero;
            if (keyboard.aKey.isPressed)
            {
                move.x -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                move.x += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                move.y -= 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                move.y += 1f;
            }

            return move;
        }

        private static Vector2 ReadKeyboardShoot()
        {
            var shoot = ReadKeyboardShoot(Keyboard.current);
            foreach (var device in InputSystem.devices)
            {
                if (device == Keyboard.current || device is not Keyboard keyboard)
                {
                    continue;
                }

                shoot += ReadKeyboardShoot(keyboard);
            }

            return shoot;
        }

        private static Vector2 ReadKeyboardShoot(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var shoot = Vector2.zero;
            if (keyboard.leftArrowKey.isPressed)
            {
                shoot.x -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                shoot.x += 1f;
            }

            if (keyboard.downArrowKey.isPressed)
            {
                shoot.y -= 1f;
            }

            if (keyboard.upArrowKey.isPressed)
            {
                shoot.y += 1f;
            }

            return shoot;
        }

        private static bool ReadAnyKeyboard(Func<Keyboard, bool> predicate)
        {
            if (Keyboard.current != null && predicate(Keyboard.current))
            {
                return true;
            }

            foreach (var device in InputSystem.devices)
            {
                if (device == Keyboard.current || device is not Keyboard keyboard)
                {
                    continue;
                }

                if (predicate(keyboard))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ReadBestMoveStick()
        {
            var gamepadMove = ReadStrongestGamepadMove();
            if (gamepadMove != Vector2.zero)
            {
                return gamepadMove;
            }

            var joystickMove = ReadStrongestJoystickStick();
            if (joystickMove != Vector2.zero)
            {
                return joystickMove;
            }

            return ReadStrongestGenericControllerVector();
        }

        private static Vector2 ReadStrongestGamepadMove()
        {
            var strongest = Vector2.zero;
            var strongestMagnitude = 0f;

            AccumulateGamepadMove(Gamepad.current, ref strongest, ref strongestMagnitude);
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad == Gamepad.current)
                {
                    continue;
                }

                AccumulateGamepadMove(gamepad, ref strongest, ref strongestMagnitude);
            }

            return strongest;
        }

        private static void AccumulateGamepadMove(Gamepad gamepad, ref Vector2 strongest, ref float strongestMagnitude)
        {
            if (gamepad == null)
            {
                return;
            }

            ConsiderVectorCandidate(ReadStickWithDeadZone(gamepad.leftStick), ref strongest, ref strongestMagnitude);
            ConsiderVectorCandidate(ReadStickWithDeadZone(gamepad.dpad), ref strongest, ref strongestMagnitude);
        }

        private static Vector2 ReadStrongestGamepadStick(Func<Gamepad, Vector2Control> selectStick)
        {
            var strongest = ReadStickWithDeadZone(Gamepad.current != null ? selectStick(Gamepad.current) : null);
            var strongestMagnitude = strongest.sqrMagnitude;

            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad == Gamepad.current)
                {
                    continue;
                }

                var candidate = ReadStickWithDeadZone(selectStick(gamepad));
                ConsiderVectorCandidate(candidate, ref strongest, ref strongestMagnitude);
            }

            return strongest;
        }

        private static Vector2 ReadStrongestJoystickStick()
        {
            var strongest = ReadStickWithDeadZone(Joystick.current?.stick);
            var strongestMagnitude = strongest.sqrMagnitude;

            foreach (var joystick in Joystick.all)
            {
                if (joystick == Joystick.current)
                {
                    continue;
                }

                var candidate = ReadStickWithDeadZone(joystick.stick);
                ConsiderVectorCandidate(candidate, ref strongest, ref strongestMagnitude);
            }

            return strongest;
        }

        private static Vector2 ReadStrongestGenericControllerVector()
        {
            var strongest = Vector2.zero;
            var strongestMagnitude = 0f;

            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard || device is Mouse || device is Gamepad || device is Joystick || !IsLikelyControllerDevice(device))
                {
                    continue;
                }

                foreach (var control in device.allControls)
                {
                    if (control is not Vector2Control vector)
                    {
                        continue;
                    }

                    var candidate = ReadStickWithDeadZone(vector);
                    ConsiderVectorCandidate(candidate, ref strongest, ref strongestMagnitude);
                }
            }

            return strongest;
        }

        private static Vector2 ReadStickWithDeadZone(Vector2Control stick)
        {
            if (stick == null)
            {
                return Vector2.zero;
            }

            var value = Vector2.ClampMagnitude(stick.ReadValue(), 1f);
            return value.sqrMagnitude >= StickDeadZone * StickDeadZone ? value : Vector2.zero;
        }

        private static void ConsiderVectorCandidate(Vector2 candidate, ref Vector2 strongest, ref float strongestMagnitude)
        {
            var candidateMagnitude = candidate.sqrMagnitude;
            if (candidateMagnitude > strongestMagnitude)
            {
                strongest = candidate;
                strongestMagnitude = candidateMagnitude;
            }
        }

        private static bool ReadAnyGamepadButtonPressed(Func<Gamepad, ButtonControl> selectButton)
        {
            return ReadAnyGamepadButton(selectButton, button => button.wasPressedThisFrame);
        }

        private static bool ReadAnyGamepadButtonHeld(Func<Gamepad, ButtonControl> selectButton)
        {
            return ReadAnyGamepadButton(selectButton, button => button.isPressed);
        }

        private static bool ReadAnyGamepadButtonReleased(Func<Gamepad, ButtonControl> selectButton)
        {
            return ReadAnyGamepadButton(selectButton, button => button.wasReleasedThisFrame);
        }

        private static bool ReadAnyGamepadTriggerHeld(Func<Gamepad, ButtonControl> selectButton, float threshold)
        {
            return ReadAnyGamepadButton(selectButton, button => button.ReadValue() > threshold);
        }

        private static bool ReadAnyGamepadButton(Func<Gamepad, ButtonControl> selectButton, Func<ButtonControl, bool> predicate)
        {
            if (Gamepad.current != null && predicate(selectButton(Gamepad.current)))
            {
                return true;
            }

            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad == Gamepad.current)
                {
                    continue;
                }

                if (predicate(selectButton(gamepad)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLikelyControllerDevice(InputDevice device)
        {
            var description = device.description;
            var identity = string.Concat(
                device.layout,
                " ",
                device.name,
                " ",
                device.displayName,
                " ",
                description.product,
                " ",
                description.manufacturer).ToLowerInvariant();

            return
                identity.Contains("gamepad") ||
                identity.Contains("controller") ||
                identity.Contains("dualshock") ||
                identity.Contains("dualsense") ||
                identity.Contains("xbox") ||
                identity.Contains("joystick");
        }

        private static void AppendKeyboardSample(StringBuilder builder, Keyboard keyboard, ref int index, string role)
        {
            if (keyboard == null)
            {
                return;
            }

            if (index > 0)
            {
                builder.Append("; ");
            }

            builder
                .Append(role)
                .Append('#')
                .Append(index)
                .Append(':')
                .Append(keyboard.displayName)
                .Append(" wasd=")
                .Append(keyboard.wKey.isPressed ? '1' : '0')
                .Append(keyboard.aKey.isPressed ? '1' : '0')
                .Append(keyboard.sKey.isPressed ? '1' : '0')
                .Append(keyboard.dKey.isPressed ? '1' : '0')
                .Append(" arrows=")
                .Append(keyboard.upArrowKey.isPressed ? '1' : '0')
                .Append(keyboard.leftArrowKey.isPressed ? '1' : '0')
                .Append(keyboard.downArrowKey.isPressed ? '1' : '0')
                .Append(keyboard.rightArrowKey.isPressed ? '1' : '0')
                .Append(" space=")
                .Append(keyboard.spaceKey.isPressed ? '1' : '0');

            index++;
        }

        private static void AppendGamepadSample(StringBuilder builder, Gamepad gamepad, ref int index, string role)
        {
            if (gamepad == null)
            {
                return;
            }

            if (index > 0)
            {
                builder.Append("; ");
            }

            builder
                .Append(role)
                .Append('#')
                .Append(index)
                .Append(':')
                .Append(gamepad.displayName)
                .Append(" layout=")
                .Append(gamepad.layout)
                .Append(gamepad.enabled ? " enabled=1" : " enabled=0")
                .Append(" lastUpdate=")
                .Append(gamepad.lastUpdateTime.ToString("0.000", CultureInfo.InvariantCulture))
                .Append(" left=");

            AppendVector(builder, gamepad.leftStick.ReadValue())
                .Append(" dpad=");

            AppendVector(builder, gamepad.dpad.ReadValue())
                .Append(" right=");

            AppendVector(builder, gamepad.rightStick.ReadValue())
                .Append(" south=")
                .Append(gamepad.buttonSouth.isPressed ? '1' : '0')
                .Append(" east=")
                .Append(gamepad.buttonEast.isPressed ? '1' : '0')
                .Append(" start=")
                .Append(gamepad.startButton.isPressed ? '1' : '0');

            index++;
        }

        private static void AppendGamepadDeviceDetails(StringBuilder builder, Gamepad gamepad)
        {
            if (gamepad == null)
            {
                builder.Append("none");
                return;
            }

            var description = gamepad.description;
            builder
                .Append(gamepad.displayName)
                .Append(" layout=")
                .Append(gamepad.layout)
                .Append(gamepad.enabled ? " enabled=1" : " enabled=0")
                .Append(" manufacturer=")
                .Append(string.IsNullOrWhiteSpace(description.manufacturer) ? "unknown" : description.manufacturer)
                .Append(" product=")
                .Append(string.IsNullOrWhiteSpace(description.product) ? "unknown" : description.product)
                .Append(" lastUpdate=")
                .Append(gamepad.lastUpdateTime.ToString("0.000", CultureInfo.InvariantCulture))
                .Append(" left=");

            AppendVector(builder, gamepad.leftStick.ReadValue())
                .Append(" dpad=");

            AppendVector(builder, gamepad.dpad.ReadValue())
                .Append(" right=");

            AppendVector(builder, gamepad.rightStick.ReadValue());
        }

        private static StringBuilder AppendVector(StringBuilder builder, Vector2 value)
        {
            return builder
                .Append('(')
                .Append(value.x.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(value.y.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(')');
        }
    }
}
