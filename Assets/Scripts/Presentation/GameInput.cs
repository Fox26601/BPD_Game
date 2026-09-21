using UnityEngine;
using UnityEngine.InputSystem;

namespace BPD.Presentation
{
    public enum ArrowSlot
    {
        None = 0,
        Up = 1,
        Left = 2,
        Right = 3,
        Down = 4
    }

    /// <summary>
    /// Thin Input System helpers for card swipe / keyboard on mobile + PC.
    /// </summary>
    public static class GameInput
    {
        public static bool WasPressedLeft()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
        }

        public static bool WasPressedRight()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
        }

        public static bool WasPressedUp()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.upArrowKey.wasPressedThisFrame ||
                    keyboard.wKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame);
        }

        public static bool WasPressedDown()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame);
        }

        public static bool WasPressedConfirm()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        public static bool WasPressedSkill() => WasPressedUp();

        public static bool TryGetPointerPosition(out Vector2 position)
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                position = default;
                return false;
            }

            position = pointer.position.ReadValue();
            return true;
        }

        public static bool WasPointerPressed()
        {
            var pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }

        public static bool IsPointerPressed()
        {
            var pointer = Pointer.current;
            return pointer != null && pointer.press.isPressed;
        }

        public static bool WasPointerReleased()
        {
            var pointer = Pointer.current;
            return pointer != null && pointer.press.wasReleasedThisFrame;
        }
    }
}
