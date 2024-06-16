using LMCore.IO;
using LMCore.Juice;
using UnityEngine;

namespace LMCore.Crawler
{
    public class NaiveInstantMovement : MonoBehaviour
    {
        [SerializeField]
        IGridSizeProvider gridSizeProvider;

        [SerializeField]
        NodeShaker WallHitShakeTarget;

        CrawlerInput cInput;
        GridEntity gEntity;

        private GridEntityController _gController;
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
        }

        private void OnDisable()
        {
            cInput.OnMovement -= CInput_OnMovement;
        }



        private void CInput_OnMovement(int tickId, Movement movement, float duration)
        {
            if (movement.IsRotation())
            {
                gEntity.Rotate(movement);
            }
            else if (movement.IsTranslation())
            {
                if (gController.CanMoveTo(movement, gridSizeProvider.GridSize))
                {
                    gEntity.Translate(movement);
                }
                else
                {
                    WallHitShakeTarget?.Shake();
                    Debug.Log($"Can't move {movement} because collides with wall");
                }
            }
            gEntity.Sync();
        }
    }
}
