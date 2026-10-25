using UnityEngine;
#if NOVA_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NovaVanguard
{
    /// <summary>
    /// One place that turns keyboard/mouse, gamepad and on-screen touch controls into gameplay intents.
    /// Uses the new Input System when it's enabled, otherwise falls back to the legacy Input Manager.
    /// </summary>
    public static class InputRouter
    {
        // ---- values read by gameplay ----
        public static Vector2 Move;               // -1..1
        public static Vector3? AimPoint;          // world point on the y=0 plane (null = auto-aim)
        public static bool Fire, Boost;
        public static bool SecondaryPressed, UltimatePressed, PausePressed, CycleSecondaryPressed;
        public static int SelectPrimary = -1;     // 0..3 when a number key is pressed
        public static bool CyclePrimaryPressed;
        public static Vector3 DragDelta;          // world-space movement from drag-to-move touch mode

        // ---- written by on-screen controls ----
        public static Vector2 TouchStick;
        public static bool TouchFire, TouchBoost, TouchSecondary, TouchUltimate, TouchCyclePrimary, TouchCycleSecondary, TouchPause;
        public static Vector2 TouchDragScreenDelta;

        public static bool UsingTouch { get; private set; }
        public static bool ShowTouchControls => UsingTouch || Application.isMobilePlatform || (SaveSystem.Data != null && SaveSystem.Data.settings.touchControlsOnDesktop);

        static Vector2 lastMouse;

        public static void Tick(Camera cam)
        {
            Move = Vector2.zero; Fire = false; Boost = false;
            SecondaryPressed = UltimatePressed = PausePressed = CycleSecondaryPressed = CyclePrimaryPressed = false;
            SelectPrimary = -1;
            AimDirFromStick = null;

            bool mouseMoved = false, anyKey = false;
            Vector2 mouse = Vector2.zero;
            bool mouseFire = false;

#if NOVA_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
                float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
                Move += new Vector2(x, y);
                Boost |= kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                UltimatePressed |= kb.spaceKey.wasPressedThisFrame;
                PausePressed |= kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
                CycleSecondaryPressed |= kb.qKey.wasPressedThisFrame;
                CyclePrimaryPressed |= kb.eKey.wasPressedThisFrame;
                SecondaryPressed |= kb.fKey.wasPressedThisFrame;
                Fire |= kb.jKey.isPressed;
                if (kb.digit1Key.wasPressedThisFrame) SelectPrimary = 0;
                if (kb.digit2Key.wasPressedThisFrame) SelectPrimary = 1;
                if (kb.digit3Key.wasPressedThisFrame) SelectPrimary = 2;
                if (kb.digit4Key.wasPressedThisFrame) SelectPrimary = 3;
                anyKey = kb.anyKey.isPressed;
            }
            var ms = Mouse.current;
            if (ms != null)
            {
                mouse = ms.position.ReadValue();
                mouseFire = ms.leftButton.isPressed;
                SecondaryPressed |= ms.rightButton.wasPressedThisFrame;
                float scroll = ms.scroll.ReadValue().y;
                if (scroll > 0.1f) CyclePrimaryPressed = true;
                if (scroll < -0.1f) CycleSecondaryPressed = true;
                mouseMoved = (mouse - lastMouse).sqrMagnitude > 4f || ms.leftButton.wasPressedThisFrame;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                Move += gp.leftStick.ReadValue();
                var rs = gp.rightStick.ReadValue();
                if (rs.sqrMagnitude > 0.2f) { AimDirFromStick = new Vector3(rs.x, 0, rs.y); }
                Fire |= gp.rightTrigger.isPressed || rs.sqrMagnitude > 0.5f;
                Boost |= gp.leftTrigger.isPressed || gp.leftStickButton.isPressed;
                SecondaryPressed |= gp.rightShoulder.wasPressedThisFrame;
                UltimatePressed |= gp.buttonNorth.wasPressedThisFrame;
                CyclePrimaryPressed |= gp.buttonWest.wasPressedThisFrame;
                CycleSecondaryPressed |= gp.leftShoulder.wasPressedThisFrame;
                PausePressed |= gp.startButton.wasPressedThisFrame;
                if (gp.leftStick.ReadValue().sqrMagnitude > 0.1f || rs.sqrMagnitude > 0.1f) { UsingGamepad = true; }
            }
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed) UsingTouch = true;
#else
            Move += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Boost |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            UltimatePressed |= Input.GetKeyDown(KeyCode.Space);
            PausePressed |= Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            CycleSecondaryPressed |= Input.GetKeyDown(KeyCode.Q);
            CyclePrimaryPressed |= Input.GetKeyDown(KeyCode.E);
            SecondaryPressed |= Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1);
            Fire |= Input.GetKey(KeyCode.J);
            for (int i = 0; i < 4; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectPrimary = i;
            mouse = Input.mousePosition;
            mouseFire = Input.GetMouseButton(0) && Input.touchCount == 0;
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.1f) CyclePrimaryPressed = true;
            if (scroll < -0.1f) CycleSecondaryPressed = true;
            mouseMoved = (mouse - lastMouse).sqrMagnitude > 4f || Input.GetMouseButtonDown(0);
            anyKey = Input.anyKey;
            if (Input.touchCount > 0) UsingTouch = true;
#endif
            if (anyKey && !UsingTouchHeld()) UsingTouch = false;
            if (mouseMoved && !Application.isMobilePlatform && !UsingTouchHeld()) { UsingTouch = false; UsingGamepad = false; }

            // Touch controls
            Move += TouchStick;
            Fire |= TouchFire;
            Boost |= TouchBoost;
            SecondaryPressed |= Consume(ref TouchSecondary);
            UltimatePressed |= Consume(ref TouchUltimate);
            CyclePrimaryPressed |= Consume(ref TouchCyclePrimary);
            CycleSecondaryPressed |= Consume(ref TouchCycleSecondary);
            PausePressed |= Consume(ref TouchPause);
            if (Move.sqrMagnitude > 1f) Move.Normalize();

            // Drag-to-move: convert screen delta to world delta on the playfield
            DragDelta = Vector3.zero;
            if (TouchDragScreenDelta != Vector2.zero && cam != null)
            {
                float worldPerPixel = GameBounds.Width / Mathf.Max(1f, Screen.width) * 1.25f;
                DragDelta = new Vector3(TouchDragScreenDelta.x, 0f, TouchDragScreenDelta.y) * worldPerPixel;
                TouchDragScreenDelta = Vector2.zero;
            }

            // Aim
            AimPoint = null;
            if (!UsingTouch && !UsingGamepad && cam != null && !Application.isMobilePlatform)
            {
                var ray = cam.ScreenPointToRay(mouse);
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) AimPoint = ray.GetPoint(d);
                Fire |= mouseFire && !UiBlocking;
            }
            if (SaveSystem.Data != null && SaveSystem.Data.settings.autoFire) Fire = true;
            lastMouse = mouse;
        }

        public static Vector3? AimDirFromStick;
        public static bool UsingGamepad;
        /// <summary>Set by the UI when the pointer is over an interactive element (so clicking a button doesn't shoot).</summary>
        public static bool UiBlocking;

        static bool UsingTouchHeld() => TouchFire || TouchBoost || TouchStick != Vector2.zero;
        static bool Consume(ref bool b) { bool v = b; b = false; return v; }

        public static void ClearTouch()
        {
            TouchStick = Vector2.zero; TouchFire = TouchBoost = TouchSecondary = TouchUltimate = TouchCyclePrimary = TouchCycleSecondary = TouchPause = false;
            TouchDragScreenDelta = Vector2.zero;
        }
    }
}
