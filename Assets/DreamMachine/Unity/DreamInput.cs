using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Unity 6.3 projects ship with the Input System package active and the
    /// legacy Input Manager off. This reads whichever one is actually enabled.
    /// </summary>
    static class DreamInput
    {
        public static bool PointerHeld;
        public static bool PointerPressed;
        public static Vector2 PointerPosition;
        public static Vector2 PointerDelta;
        public static float ScrollY;
        public static int TouchCount;

        public static void Sample()
        {
            PointerHeld = false;
            PointerPressed = false;
            PointerDelta = Vector2.zero;
            ScrollY = 0f;
            TouchCount = 0;
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                PointerHeld = mouse.leftButton.isPressed;
                PointerPressed = mouse.leftButton.wasPressedThisFrame;
                PointerPosition = mouse.position.ReadValue();
                PointerDelta = mouse.delta.ReadValue();
                ScrollY = mouse.scroll.ReadValue().y;
            }
            Touchscreen screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.isPressed)
            {
                TouchCount = 1;
                PointerHeld = true;
                PointerPressed = screen.primaryTouch.press.wasPressedThisFrame;
                PointerPosition = screen.primaryTouch.position.ReadValue();
                PointerDelta = screen.primaryTouch.delta.ReadValue();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (TouchCount == 0) TouchCount = Input.touchCount;
            if (!PointerHeld && Input.GetMouseButton(0))
            {
                PointerHeld = true;
                PointerPressed = Input.GetMouseButtonDown(0);
                PointerPosition = Input.mousePosition;
            }
            if (Mathf.Abs(ScrollY) < 0.01f) ScrollY = Input.mouseScrollDelta.y;
            if (TouchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                PointerDelta = touch.deltaPosition;
                PointerPosition = touch.position;
                PointerHeld = true;
            }
#endif
        }

        public static bool WasPressed(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Key key;
                if (TryMap(code, out key) && keyboard[key].wasPressedThisFrame) return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(code);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static bool TryMap(KeyCode code, out Key key)
        {
            switch (code)
            {
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Comma: key = Key.Comma; return true;
                case KeyCode.Period: key = Key.Period; return true;
                case KeyCode.Alpha1: key = Key.Digit1; return true;
                case KeyCode.F1: key = Key.F1; return true;
                case KeyCode.F2: key = Key.F2; return true;
                case KeyCode.F3: key = Key.F3; return true;
                case KeyCode.F4: key = Key.F4; return true;
                case KeyCode.F5: key = Key.F5; return true;
                default: key = Key.None; return false;
            }
        }
#endif
    }
}
