using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerInputReader
    {
        public static RoomDesignerInputSnapshot ReadCurrent()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            var moveX = Held(keyboard?.dKey, keyboard?.rightArrowKey) ? 1 : Held(keyboard?.aKey, keyboard?.leftArrowKey) ? -1 : 0;
            var moveZ = Held(keyboard?.wKey, keyboard?.upArrowKey) ? 1 : Held(keyboard?.sKey, keyboard?.downArrowKey) ? -1 : 0;
            var toolDelta = Pressed(keyboard?.eKey) ? 1 : Pressed(keyboard?.qKey) ? -1 : 0;
            var layerDelta = Pressed(keyboard?.xKey) ? 1 : Pressed(keyboard?.zKey) ? -1 : 0;

            if (gamepad != null)
            {
                var left = gamepad.leftStick.ReadValue();
                var dpad = gamepad.dpad.ReadValue();
                moveX = NonZero(moveX, AxisToStep(left.x), AxisToStep(dpad.x));
                moveZ = NonZero(moveZ, -AxisToStep(left.y), -AxisToStep(dpad.y));
                toolDelta = NonZero(toolDelta, gamepad.rightShoulder.wasPressedThisFrame ? 1 : 0, gamepad.leftShoulder.wasPressedThisFrame ? -1 : 0);
                layerDelta = NonZero(layerDelta, gamepad.rightTrigger.wasPressedThisFrame ? 1 : 0, gamepad.leftTrigger.wasPressedThisFrame ? -1 : 0);
            }

            return new RoomDesignerInputSnapshot(
                moveX,
                moveZ,
                toolDelta,
                layerDelta,
                Pressed(keyboard?.spaceKey, keyboard?.enterKey) || gamepad?.buttonSouth.wasPressedThisFrame == true,
                Pressed(keyboard?.backspaceKey, keyboard?.deleteKey) || gamepad?.buttonEast.wasPressedThisFrame == true,
                Pressed(keyboard?.fKey) || gamepad?.buttonWest.wasPressedThisFrame == true,
                Pressed(keyboard?.tabKey) || gamepad?.buttonNorth.wasPressedThisFrame == true,
                Pressed(keyboard?.pKey),
                Pressed(keyboard?.jKey),
                Pressed(keyboard?.uKey),
                Pressed(keyboard?.escapeKey) || gamepad?.startButton.wasPressedThisFrame == true,
                Pressed(keyboard?.vKey),
                Pressed(keyboard?.cKey) || gamepad?.selectButton.wasPressedThisFrame == true);
        }

        private static bool Pressed(params KeyControl[] keys)
        {
            foreach (var key in keys)
            {
                if (key?.wasPressedThisFrame == true)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Held(params KeyControl[] keys)
        {
            foreach (var key in keys)
            {
                if (key?.isPressed == true)
                {
                    return true;
                }
            }

            return false;
        }

        private static int AxisToStep(float value)
        {
            return value > 0.55f ? 1 : value < -0.55f ? -1 : 0;
        }

        private static int NonZero(params int[] values)
        {
            foreach (var value in values)
            {
                if (value != 0)
                {
                    return value;
                }
            }

            return 0;
        }
    }
}
