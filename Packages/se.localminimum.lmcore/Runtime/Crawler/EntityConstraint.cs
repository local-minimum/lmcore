using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    [RequireComponent(typeof(Anchor))]
    public class EntityConstraint : MonoBehaviour, IAnchorEffect
    {
        Anchor _anchor;
        Anchor anchor
        {
            get
            {
                if (_anchor == null)
                {
                    _anchor = GetComponent<Anchor>();
                }
                return _anchor;
            }
        }

        [SerializeField]
        List<Direction> refusedTranslations = new List<Direction>();

        public bool RefuseTranslation(Direction direction) =>
            refusedTranslations.Contains(anchor.PrefabRotation.InverseRotate(direction));


        [SerializeField]
        List<Direction> refusedLookDirections = new List<Direction>();

        public void SetRefusedLookDirection(List<Direction> directions, bool enforce)
        {
            refusedLookDirections = directions;
            EnforceForwardLookIfAllowed = true;
        }

        [SerializeField]
        bool EnforceForwardLookIfAllowed;

        [SerializeField]
        bool manageFOV = true;

        [SerializeField]
        float targetFOV = 30f;

        [SerializeField]
        float transitionTime = 0.1f;

        public void SetManageFOV(bool manage, float targetFOV = 30f, float transitionTime=0.1f)
        {
            manageFOV = manage;
            this.targetFOV = targetFOV;
            this.transitionTime = transitionTime;
        }

        public bool RefuseLookDirection(Direction direction) =>
            refusedLookDirections.Contains(anchor.PrefabRotation.InverseRotate(direction));

        public Direction GetAllowedLookDirection(Direction currentLookDirection, Direction translationDirection)
        {
            var permissable = DirectionExtensions
                .AllDirections
                .Where(d => !refusedLookDirections.Contains(d))
                .Select(d => anchor.PrefabRotation.Rotate(d))
                .ToList();

            if (permissable.Count() == 0) return Direction.None;

            if (EnforceForwardLookIfAllowed && permissable.Contains(translationDirection)) return translationDirection;

            if (permissable.Contains(currentLookDirection)) return currentLookDirection;
            if (permissable.Contains(translationDirection)) return translationDirection;

            // We need to do 180 (two rotations worth) to invert so lets avoid it if possible
            return permissable
                .OrderBy(d => d == currentLookDirection.Inverse() ? 2 : 1)
                .First();
        }

        public bool AllowsRotation => DirectionExtensions.AllDirections.Where(d => !refusedLookDirections.Contains(d)).Count() > 1;

        public void RotateRefused(AnchorYRotation rotation)
        {
            refusedTranslations = refusedTranslations.Select(d => rotation.Rotate(d)).ToList();
            refusedLookDirections = refusedLookDirections.Select(d => rotation.Rotate(d)).ToList();
        }


        private enum Phase { Inactive, Enter, Exit };
        Phase _phase = Phase.Inactive;
        Phase phase
        {
            get => _phase;
            set
            {
                if (manageFOV)
                {
                    _phase = value;
                    phaseStart = Time.timeSinceLevelLoad;
                }
            }
        }
        float phaseStart;
        Camera cam;

        public void EnterTile(GridEntity player)
        {
            if (!manageFOV) return;

            cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                phase = Phase.Enter;
            }

        }

        public void ExitTile(GridEntity player)
        {
            if (!manageFOV) return;

            cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                phase = Phase.Exit;
            }
        }

        [SerializeField]
        bool refuseFreeCamera;

        public bool RefuseFreeCamera => refuseFreeCamera;

        bool instantMove;

        private void OnEnable()
        {
            GameSettings.FOV.OnChange += FOV_OnChange;
            defaultFOV = GameSettings.FOV.Value;

            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            instantMove = GameSettings.InstantMovement.Value;
        }

        private void InstantMovement_OnChange(bool value)
        {
            instantMove = value;
        }

        private void OnDisable()
        {
            GameSettings.FOV.OnChange -= FOV_OnChange;
            GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
        }

        float defaultFOV;

        private void FOV_OnChange(float value)
        {
            defaultFOV = value;
        }

        private void Update()
        {
            if (phase == Phase.Inactive || cam == null) return;

            float progress = instantMove ? 1f : Mathf.Clamp01((Time.timeSinceLevelLoad - phaseStart) / transitionTime);

            if (phase == Phase.Exit)
            {
                progress = 1 - progress;
                if (progress == 0)
                {
                    phase = Phase.Inactive;
                }
            }
            else if (progress == 1)
            {
                phase = Phase.Inactive;
            }

            cam.fieldOfView = Mathf.Lerp(defaultFOV, targetFOV, progress);
        }
    }
}
