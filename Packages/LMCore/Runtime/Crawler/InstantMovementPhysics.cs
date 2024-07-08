using LMCore.AbstractClasses;
using LMCore.IO;
using LMCore.Juice;
using UnityEngine;

namespace LMCore.Crawler
{
    public class InstantMovementPhysics : MonoBehaviour, IEntityMover 
    {
        public bool Enabled => enabled && gameObject.activeSelf;

        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        [SerializeField]
        NodeShaker WallHitShakeTarget;

        public event EntityMovementStartEvent OnMoveStart;
        public event EntityMovementEndEvent OnMoveEnd;

        private LazyComponent<GridEntity> gEntity;
        private LazyComponent<CrawlerInput> cInput;
        private LazyComponent<GridEntityController> gController;

        void Awake()
        {
            gController = new LazyComponent<GridEntityController>(gameObject);
            cInput = new LazyComponent<CrawlerInput>(gameObject);
            gEntity = new LazyComponent<GridEntity>(gameObject);

            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            enabled = GameSettings.InstantMovement.Value;

            if (enabled)
            {
                gEntity.Value.Sync();
            }
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
            cInput.Value.OnMovement += CInput_OnMovement;
            gEntity.Value.OnLand.AddListener(OnLand);

            Movers.Activate(this);
        }

        private void OnDisable()
        {
            cInput.Value.OnMovement -= CInput_OnMovement;
            gEntity.Value.OnLand.RemoveListener(OnLand);

            Movers.Deactivate(this);
        }

        private void CInput_OnMovement(int tickId, Movement movement, float duration)
        {
            var startPosition = gEntity.Value.Position;
            var startLookDirection = gEntity.Value.LookDirection;

            if (movement.IsRotation())
            {
                var endLookDirection = gEntity.Value.LookDirection.ApplyRotation(gEntity.Value.Anchor, movement, out var endDown);

                OnMoveStart?.Invoke(
                    gEntity,
                    movement,
                    gEntity.Value.Position,
                    endLookDirection,
                    endDown,
                    true
                 );

                gEntity.Value.Rotate(movement);

                OnMoveEnd?.Invoke(
                    gEntity,
                    true
                );
            }
            else if (movement.IsTranslation())
            {
                var allowed = gController.Value.CanMoveTo(movement, GridSizeProvider.GridSize);

                var endCoordinates = gEntity.Value.LookDirection.Translate(gEntity.Value.Position);

                OnMoveStart?.Invoke(
                    gEntity,
                    movement,
                    endCoordinates,
                    gEntity.Value.LookDirection,
                    gEntity.Value.Anchor,
                    allowed 
                 );

                if (allowed)
                {
                    gEntity.Value.Translate(movement);
                }
                else
                {
                    WallHitShakeTarget?.Shake();
                    Debug.Log($"Can't move {movement} because collides with wall");
                }

                OnMoveEnd?.Invoke(
                    gEntity,
                    allowed
                );
            }
            gEntity.Value.Sync();
        }

        public void OnLand()
        {
            WallHitShakeTarget?.Shake();
        }
    }    
}
