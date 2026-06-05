using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Hollow.Combat
{
    internal static class DebugKeyboardInput
    {
        public static bool NumberWasPressed(int number)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return number switch
            {
                0 => WasPressed(keyboard.digit0Key) || WasPressed(keyboard.numpad0Key),
                1 => WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key),
                2 => WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key),
                3 => WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key),
                4 => WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key),
                5 => WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key),
                6 => WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key),
                7 => WasPressed(keyboard.digit7Key) || WasPressed(keyboard.numpad7Key),
                8 => WasPressed(keyboard.digit8Key) || WasPressed(keyboard.numpad8Key),
                9 => WasPressed(keyboard.digit9Key) || WasPressed(keyboard.numpad9Key),
                _ => false
            };
        }

        private static bool WasPressed(KeyControl control)
        {
            return control != null && control.wasPressedThisFrame;
        }
    }
}
