using UnityEngine;
using UnityEngine.InputSystem;

namespace Hollow.Input
{
    public static class GameplayInputReader
    {
        private const float StickDeadZone = 0.2f;
        private const float MouseAimMovePixels = 0.5f;

        public static GameplayInputSnapshot ReadCurrent()
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
                ReadMove(),
                ReadShoot(),
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

        private static Vector2 ReadMove()
        {
            var move = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
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
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                move += gamepad.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        private static Vector2 ReadShoot()
        {
            var shoot = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
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
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                shoot += gamepad.rightStick.ReadValue();
            }

            return shoot;
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
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        private static bool ReadSwapWeaponPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.leftShoulder.wasPressedThisFrame;
        }

        private static bool ReadLightAttackPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.jKey.wasPressedThisFrame ||
                 keyboard.leftArrowKey.isPressed ||
                 keyboard.rightArrowKey.isPressed ||
                 keyboard.downArrowKey.isPressed ||
                 keyboard.upArrowKey.isPressed))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightShoulder.wasPressedThisFrame;
        }

        private static bool ReadLightAttackHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.jKey.isPressed ||
                 keyboard.leftArrowKey.isPressed ||
                 keyboard.rightArrowKey.isPressed ||
                 keyboard.downArrowKey.isPressed ||
                 keyboard.upArrowKey.isPressed))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightShoulder.isPressed;
        }

        private static bool ReadLightAttackReleased(bool lightAttackHeld)
        {
            if (lightAttackHeld)
            {
                return false;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.jKey.wasReleasedThisFrame ||
                 keyboard.leftArrowKey.wasReleasedThisFrame ||
                 keyboard.rightArrowKey.wasReleasedThisFrame ||
                 keyboard.downArrowKey.wasReleasedThisFrame ||
                 keyboard.upArrowKey.wasReleasedThisFrame))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightShoulder.wasReleasedThisFrame;
        }

        private static bool ReadHeavyAttackPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.kKey.wasPressedThisFrame)
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightTrigger.wasPressedThisFrame;
        }

        private static bool ReadHeavyAttackHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.kKey.isPressed)
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightTrigger.isPressed;
        }

        private static bool ReadHeavyAttackReleased(bool heavyAttackHeld)
        {
            if (heavyAttackHeld)
            {
                return false;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.kKey.wasReleasedThisFrame)
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasReleasedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightTrigger.wasReleasedThisFrame;
        }

        private static bool ReadUseActiveItemPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
        }

        private static bool ReadUseConsumableCardPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
        }

        private static bool ReadRollPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        }

        private static bool ReadLockTargetPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightStickButton.wasPressedThisFrame;
        }

        private static bool ReadGuardHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.leftTrigger.ReadValue() > 0.5f;
        }

        public static bool ReadPausePressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.startButton.wasPressedThisFrame;
        }

        public static bool ReadDebugHudTogglePressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.f3Key.wasPressedThisFrame;
        }
    }
}
