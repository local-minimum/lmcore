using LMCore.IO;
using LMCore.Juice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public class NaiveInstantMovement : MonoBehaviour, IEntityMover 
    {
        public bool Enabled => enabled && gameObject.activeSelf;

        public IGridSizeProvider GridSizeProvider { get; set; }

        [SerializeField]
        NodeShaker WallHitShakeTarget;

        CrawlerInput cInput;
        GridEntity gEntity;

        private GridEntityController _gController;

        public event EntityMovementEvent OnMoveStart;
        public event EntityMovementEvent OnMoveEnd;

        private GridEntityController gController
        {
            get
            {
                if (_gController == null)
                {
                    _gController = GetComponent<GridEntityController>();
                }
                return _gController;
            }
        }

        void Awake()
        {
            gEntity = GetComponent<GridEntity>();
            gEntity.Sync();
            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            enabled = GameSettings.InstantMovement.Value;
        }

        private void OnDestroy()
        {
            GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
            Movers.Deactivate(this);
        }

        private void InstantMovement_OnChange(bool value)
        {
            enabled = value;
        }

        private void OnEnable()
        {
            if (cInput == null)
            {
                cInput = GetComponent<CrawlerInput>();
            }
            cInput.OnMovement += CInput_OnMovement;
            gEntity.OnLand.AddListener(OnLand);

            Movers.Activate(this);
        }

        private void OnDisable()
        {
            cInput.OnMovement -= CInput_OnMovement;
            gEntity.OnLand.RemoveListener(OnLand);

            Movers.Deactivate(this);
        }

        private void CInput_OnMovement(int tickId, Movement movement, float duration)
        {
            var startPosition = gEntity.Position;
            var startLookDirection = gEntity.LookDirection;

            if (movement.IsRotation())
            {
                OnMoveStart?.Invoke(
                    gEntity,
                    movement,
                    startPosition,
                    startLookDirection,
                    gEntity.Position,
                    gEntity.LookDirection.ApplyRotation(movement),
                    true
                 );

                gEntity.Rotate(movement);

                OnMoveEnd?.Invoke(
                    gEntity,
                    movement,
                    startPosition,
                    startLookDirection,
                    gEntity.Position,
                    gEntity.LookDirection,
                    true
                );
            }
            else if (movement.IsTranslation())
            {
                var allowed = gController.CanMoveTo(movement, GridSizeProvider.GridSize);

                OnMoveStart?.Invoke(
                    gEntity,
                    movement,
                    startPosition,
                    startLookDirection,
                    gEntity.Position,
                    gEntity.LookDirection.ApplyRotation(movement),
                    allowed 
                 );
                if (allowed)
                {
                    gEntity.Translate(movement);
                }
                else
                {
                    WallHitShakeTarget?.Shake();
                    Debug.Log($"Can't move {movement} because collides with wall");
                }

                OnMoveEnd?.Invoke(
                    gEntity,
                    movement,
                    startPosition,
                    startLookDirection,
                    gEntity.Position,
                    gEntity.LookDirection,
                    allowed
                );
            }
            gEntity.Sync();
        }

        public void OnLand()
        {
            WallHitShakeTarget?.Shake();
        }
    }    
}
