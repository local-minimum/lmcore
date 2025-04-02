using LMCore.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace LMCore.UI
{
    public class CustomCursor : MonoBehaviour
    {
        [System.Serializable]
        public enum Mode
        {
            CustomAndNative,
            NativeOnly,
            NativeOnlyAlwaysVisible
        }

        private Mouse virtualMouse;

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField,
            HelpBox("If other scripts manages the cursor directly, then the mode may mostly relate to initial state")]
        private Mode _mode = Mode.CustomAndNative;
        public Mode mode
        {
            get => _mode;
            set
            {
                _mode = value;
                if (!Ready)
                {
                    SetupVirtualMouse();
                }

                SyncCustomAndNativeCursorPositions();
                SyncVisibility();
            }
        }

        /// <summary>
        /// Input mode when custom cursor is active
        /// </summary>
        [SerializeField]
        string ActiveInputMode = "Gamepad";

        [Header("Custom Cursor")]
        [SerializeField]
        RectTransform CursorTransform;

        [SerializeField, Range(100, 4000)]
        float cursorSpeed = 1000f;

        [SerializeField]
        float padding = 5;

        Canvas _canvas;
        Canvas canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = CursorTransform.GetComponentInParent<Canvas>(true);
                }
                return _canvas;
            }
        }

        private void OnEnable()
        {
            SetupVirtualMouse();

            InputSystem.onAfterUpdate += UpdateMotion;
        }

        void SetupVirtualMouse()
        {
            if (virtualMouse == null)
            {
                virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
            }
            else if (!virtualMouse.added)
            {
                InputSystem.AddDevice(virtualMouse);
            }

            InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);

            if (CursorTransform != null)
            {
                Vector2 position = CursorTransform.anchoredPosition;
                InputState.Change(virtualMouse.position, position);
            }
        }

        private void OnDisable()
        {
            RemoveVirtualMouse();
            InputSystem.onAfterUpdate -= UpdateMotion;
        }

        void RemoveVirtualMouse()
        {
            if (virtualMouse != null && virtualMouse.added) InputSystem.RemoveDevice(virtualMouse);
        }

        bool Ready => virtualMouse != null && Gamepad.current != null;


        private void UpdateMotion()
        {
            if (!Ready || !CursorTransform.gameObject.activeSelf) return;


            Vector2 deltaValue = Gamepad.current.leftStick.ReadValue();
            deltaValue *= cursorSpeed * Time.deltaTime;

            Vector2 currentPosition = virtualMouse.position.ReadValue();
            Vector2 newPosition = currentPosition + deltaValue;

            newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
            newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

            InputState.Change(virtualMouse.position, newPosition);
            InputState.Change(virtualMouse.delta, deltaValue);

            AnchorCursor(newPosition);
        }

        /// <summary>
        /// Callback on the PlayerInput user defined simulated left click event.
        /// 
        /// E.g. button south on gamepad
        /// </summary>
        public void HandleVirtualClick(InputAction.CallbackContext callbackContext)
        {
            if (!enabled && mode == Mode.CustomAndNative) return;

            if (callbackContext.performed)
            {
                VirtualLeftMouseButtonPressed = true;
            }
            else if (callbackContext.canceled)
            {
                VirtualLeftMouseButtonPressed = false;
            }
        }

        string mostRecentScheme;
        bool CustomCursorSchemeActive => mostRecentScheme == ActiveInputMode;

        /// <summary>
        /// Callback on the PlayerInput behavior ControlSchemeChange event
        /// </summary>
        public void HandleControlSchemeChange(PlayerInput input)
        {
            // Must sync before we toggle!
            SyncCustomAndNativeCursorPositions();

            Debug.Log($"CustomCursor: {mostRecentScheme} => {input.currentControlScheme}");
            mostRecentScheme = input.currentControlScheme;

            if (!enabled) return;

            SyncVisibility();
        }

        public bool NativeCursorShouldBeVisible =>
            !CustomCursorSchemeActive || mode == Mode.NativeOnlyAlwaysVisible;

        public bool CustomCursorShouldBeVisible =>
            CustomCursorSchemeActive && mode == Mode.CustomAndNative;

        void SyncVisibility()
        {
            // Only show custom cursor when mode allows custom cursor
            CursorTransform.gameObject.SetActive(CustomCursorShouldBeVisible);

            // Always show native cursor when we are not on a custom cursor control scheme
            // but also show it when behavior is disabled and disabled behavior not set to keep
            // hiding the default native 
            Cursor.visible = NativeCursorShouldBeVisible;
        }

        /// <summary>
        /// Updating Custom Cursor and native cursor with eachothers positions
        /// </summary>
        void SyncCustomAndNativeCursorPositions()
        {
            if (!Ready) return;

            if (CustomCursorSchemeActive)
            {
                Mouse.current.WarpCursorPosition(virtualMouse.position.ReadValue());
            }
            else
            {
                InputState.Change(virtualMouse.position, Mouse.current.position.ReadValue());
                AnchorCursor(Mouse.current.position.ReadValue());
            }
        }

        /// <summary>
        /// If virtual left mouse button is pressed
        /// </summary>
        bool VirtualLeftMouseButtonPressed
        {
            set
            {
                if (virtualMouse == null) return;

                virtualMouse.CopyState<MouseState>(out var mouseState);

                mouseState.WithButton(MouseButton.Left, value);
                InputState.Change(virtualMouse, mouseState);

                Debug.Log($"Virtual left mouse button pressed: {value}");
            }
        }

        /// <summary>
        /// Translates the virtual cursor rect transform to the position
        /// </summary>
        /// <param name="position">Mouse position coordinates</param>
        void AnchorCursor(Vector2 position)
        {
            if (CursorTransform == null) return;

            Vector2 anchoredPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                position,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                out anchoredPosition);

            CursorTransform.anchoredPosition = anchoredPosition;
        }
    }
}
