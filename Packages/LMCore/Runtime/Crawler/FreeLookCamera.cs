using LMCore.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.Crawler
{
    public class FreeLookCamera : MonoBehaviour
    {
        public enum SnapbackMode { 
            None, 
            ByActivationToggle, 
            ByActivationRelease, 
            ByManualReset, 
            ByMovement };

        [HelpBox("This script should be on a parent to the camera itself to work properly", HelpBoxMessageType.Warning)]
        [SerializeField, Header("Snapback")]
        protected SnapbackMode Snapback = SnapbackMode.ByActivationToggle;
        [SerializeField, Range(0, 1), Tooltip("0 = No snapback, 1 = Instant")]
        float snapbackLerp = 0.2f;
        [SerializeField, Tooltip("If angle from resting rotation is less than this, stop lerping / reset")]
        float identityThreshold = 1f;

        [SerializeField, Header("Freedom cone"), Tooltip("0=No looking 1=Crazy spinny"), Range(0,1)]
        float lookAmount = 1f;

        [SerializeField, Range(0, 0.5f), Tooltip("The higher look amount is the lower this must be to avoid spinning bug")]
        float verticalLookClamp = 0.1f;

        bool freeLooking;

        GridEntity _entity;
        GridEntity Entity { 
            get { 
                if (_entity == null)
                {
                    _entity = GetComponentInParent<GridEntity>();
                }
                return _entity;
            }
        }

        Camera cam;

        public void OnFreeLook(InputAction.CallbackContext context)
        {
            if (Snapback == SnapbackMode.ByActivationToggle)
            {
                if (context.performed)
                {
                    freeLooking = !freeLooking;
                    Cursor.visible = !freeLooking;
                }
            }
            else if (Snapback == SnapbackMode.ByActivationRelease)
            {
                if (context.performed)
                {
                    freeLooking = true;
                    Cursor.visible = !freeLooking;
                } else if (context.canceled)
                {
                    freeLooking = false;
                    Cursor.visible = !freeLooking;
                }
            } else if (context.performed)
            {
                freeLooking = true;
                Cursor.visible = !freeLooking;
            }

            Debug.Log($"Looking {freeLooking}");
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
        }

        private void OnDisable()
        {
            GridEntity.OnMove -= GridEntity_OnMove;
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (Entity == entity && Snapback == SnapbackMode.ByMovement)
            {
                freeLooking = false;
                Cursor.visible = !freeLooking;
            }
        }

        private void Update()
        {
            var entity = Entity;
            if (freeLooking) {
                var lookat = cam.ScreenToWorldPoint(
                    new Vector3(
                        PointerCoordinates.x,
                        Mathf.Clamp(PointerCoordinates.y, Screen.height * -verticalLookClamp, Screen.height * (1f + verticalLookClamp)), 
                        Entity.Dungeon.GridSize));

                var target = Quaternion.LookRotation(lookat - transform.position, transform.up);
                cam.transform.rotation = Quaternion.Lerp(transform.rotation, target, lookAmount);
            }
            else if (cam.transform.localRotation != Quaternion.identity)
            {
                cam.transform.localRotation = Quaternion
                    .Lerp(cam.transform.localRotation, Quaternion.identity, snapbackLerp);

                if (Quaternion.Angle(cam.transform.localRotation, Quaternion.identity) < identityThreshold)
                {
                    cam.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
}
