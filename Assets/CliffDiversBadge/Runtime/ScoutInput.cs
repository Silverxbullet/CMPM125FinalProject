using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CliffDiversBadge
{
    // No Input Actions asset required. Works with either enabled input backend.
    public static class ScoutInput
    {
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2(
                    (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) -
                    (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) -
                    (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0)), 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Vector2.ClampMagnitude(new Vector2(
                    (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
                    (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) -
                    (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)), 1f);
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool DivePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Space);
#else
                return false;
#endif
            }
        }

        public static bool RetryPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.R);
#else
                return false;
#endif
            }
        }
    }
}
