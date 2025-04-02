using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDLookAtPlayer : MonoBehaviour
    {
        [SerializeField]
        bool LockPitch = true;

        [SerializeField]
        bool invertLook = true;

        [Header("Rotation Criteria")]
        [SerializeField, Tooltip("If true, raycasting towards camera on mask may not collide with anything")]
        bool OnlyWhenVisible = true;

        [SerializeField, Tooltip("If anything in this mask is hit closer than the camera it is not visible and won't rotate")]
        LayerMask VisibilityMask;


        Camera _PlayerCam;
        Camera PlayerCam
        {
            get
            {
                if (_PlayerCam == null)
                {
                    _PlayerCam = GetComponentInParent<TiledDungeon>().Player.GetComponentInChildren<Camera>();
                }
                return _PlayerCam;
            }
        }

        Quaternion _RestingLocalRotation;

        private void Awake()
        {
            _RestingLocalRotation = transform.rotation;
        }

        void Update()
        {
            var look = PlayerCam.transform.position - transform.position;

            if (OnlyWhenVisible)
            {
                var distance = look.magnitude;
                if (Physics.Raycast(transform.position, look, distance, VisibilityMask))
                {
                    transform.localRotation = _RestingLocalRotation;
                    return;
                }
            }

            // TODO: Handle enemies being upside down and climbing walls
            if (LockPitch)
            {
                look.y = 0;
            }
            if (invertLook)
            {
                look *= -1;
            }

            transform.rotation = Quaternion.LookRotation(look, Vector3.up);
        }
    }
}
