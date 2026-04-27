using UnityEngine;
using UnityEngine.InputSystem;

namespace Hollow.Input
{
    public static class GameplayInputReader
    {
        private const float StickDeadZone = 0.2f;

        public static GameplayInputSnapshot ReadCurrent()
        {
            return new GameplayInputSnapshot(
                ReadMove(),
                ReadShoot(),
                ReadInteractPressed(),
                ReadSwapWeaponPressed(),
                ReadLightAttackPressed(),
                ReadHeavyAttackPressed(),
                ReadUseActiveItemPressed(),
                ReadUseConsumableCardPressed());
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
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame)
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
    }
}
