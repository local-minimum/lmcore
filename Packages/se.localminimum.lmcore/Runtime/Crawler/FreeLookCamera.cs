using LMCore.Extensions;
using LMCore.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.Crawler
{
    public class FreeLookCamera : MonoBehaviour
    {
        public enum SnapbackMode
        {
            None,
            ByActivationToggle,
            ByActivationRelease,
            ByManualReset,
            ByMovement
        };

        [HelpBox("This script should be on a parent to the camera itself to work properly", HelpBoxMessageType.Warning)]
        [SerializeField, Header("Snapback")]
        protected SnapbackMode Snapback = SnapbackMode.ByActivationToggle;
        [SerializeField, Range(0, 1), Tooltip("0 = No snapback, 1 = Instant")]
        float snapbackLerp = 0.2f;
        [SerializeField, Tooltip("If angle from resting rotation is less than this, stop lerping / reset")]
        float identityThreshold = 1f;

        [SerializeField, Header("Freedom cone"), Tooltip("0=No looking 1=Crazy spinny"), Range(0, 1)]
        float lookAmount = 1f;

        [SerializeField, Range(0, 0.5f), Tooltip("The higher look amount is the lower this must be to avoid spinning bug")]
        float verticalLookClamp = 0.1f;

        [SerializeField, Range(0f, 3f)]
        float forwardTranslation = 1f;

        [SerializeField, Range(0, 1)]
        float translationLerp = 0.05f;

        bool freeLooking;

        GridEntity _entity;
        GridEntity Entity
        {
            get
            {
                if (_entity == null)
                {
                    _entity = GetComponentInParent<GridEntity>();
                }
                return _entity;
            }
        }

        CustomCursor _cusomCursor;
        CustomCursor customCursor
        {
            get
            {
                if (_cusomCursor == null)
                {
                    _cusomCursor = GetComponentInParent<CustomCursor>(true);
                }

                return _cusomCursor;
            }
        }

        bool NativeCursorAllowed =>
            customCursor == null ? true : customCursor.NativeCursorShouldBeVisible;

        void SyncNativeCursor()
        {
            Cursor.visible = !freeLooking && NativeCursorAllowed;
        }

        Camera cam;

        bool allowed = true;

        Vector2 restorePoint;
        void RememberCursorPosition()
        {
            restorePoint = Mouse.current.position.ReadValue();
        }

        void RestoreCursorPosition()
        {
            Mouse.current.WarpCursorPosition(restorePoint);
        }

        public void OnFreeLook(InputAction.CallbackContext context)
        {
            bool allowNative = NativeCursorAllowed;

            if (Snapback == SnapbackMode.ByActivationToggle)
            {
                if (context.performed)
                {
                    freeLooking = !freeLooking;
                    if (freeLooking)
                    {
                        RememberCursorPosition();
                    }
                    else
                    {
                        RestoreCursorPosition();
                    }
                    SyncNativeCursor();
                }
            }
            else if (Snapback == SnapbackMode.ByActivationRelease)
            {
                if (context.performed)
                {
                    freeLooking = true;
                    RememberCursorPosition();
                    SyncNativeCursor();
                }
                else if (context.canceled)
                {
                    freeLooking = false;
                    RestoreCursorPosition();
                    SyncNativeCursor();
                }
            }
            else if (context.performed)
            {
                freeLooking = true;
                RememberCursorPosition();
                SyncNativeCursor();
            }

            Debug.Log($"Looking {freeLooking}");
        }

        public void RefuseFreelook()
        {
            allowed = false;
            SyncNativeCursor();
        }

        public void AllowFreelook()
        {
            allowed = true;
            SyncNativeCursor();
        }

        Vector2 PointerCoordinates;
        public void OnPointer(InputAction.CallbackContext context)
        {
            PointerCoordinates = context.ReadValue<Vector2>();
        }

        private void Start()
        {
            cam = GetComponentInChildren<Camera>(true);
        }

        private void OnEnable()
        {
            GridEntity.OnMove += GridEntity_OnMove;
            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        }

        private void OnDisable()
        {
            GridEntity.OnMove -= GridEntity_OnMove;
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (Entity == entity && Snapback == SnapbackMode.ByMovement)
            {
                if (freeLooking) RestoreCursorPosition();
                freeLooking = false;
                SyncNativeCursor();
            }
        }

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (entity != Entity) return;

            var anchor = entity.NodeAnchor;
            if (anchor == null)
            {
                AllowFreelook();
            }
            else
            {
                var constraint = anchor.Constraint;
                if (constraint == null || !constraint.RefuseFreeCamera)
                {
                    AllowFreelook();
                }
                else
                {
                    if (freeLooking) RestoreCursorPosition();
                    RefuseFreelook();
                }
            }
        }

        private void Update()
        {
            var entity = Entity;
            if (allowed && freeLooking)
            {
                var lookat = cam.ScreenToWorldPoint(
                    new Vector3(
                        PointerCoordinates.x,
                        Mathf.Clamp(PointerCoordinates.y, Screen.height * -verticalLookClamp, Screen.height * (1f + verticalLookClamp)),
                        Entity.Dungeon.GridSize));

                var target = Quaternion.LookRotation(lookat - transform.position, transform.up);
                cam.transform.rotation = Quaternion.Lerp(transform.rotation, target, lookAmount);

                // Only translate forward when not on wall
                var z = cam.transform.localPosition.z;
                if (!Entity.AnchorDirection.IsPlanarCardinal())
                {
                    cam.transform.localPosition = Vector3.forward * Mathf.Lerp(z, forwardTranslation, translationLerp);
                }
                else
                {
                    cam.transform.localPosition = Vector3.forward * Mathf.Lerp(z, 0, 0.5f);
                }
            }
            else if (cam.transform.localRotation != Quaternion.identity)
            {
                cam.transform.localRotation = Quaternion
                    .Lerp(cam.transform.localRotation, Quaternion.identity, snapbackLerp);

                var z = cam.transform.localPosition.z;
                cam.transform.localPosition = Vector3.forward * Mathf.Lerp(z, 0, 0.5f);

                if (Quaternion.Angle(cam.transform.localRotation, Quaternion.identity) < identityThreshold)
                {
                    cam.transform.localRotation = Quaternion.identity;
                    cam.transform.localPosition = Vector3.zero;
                }
            }
        }
    }
}
