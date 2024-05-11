using LMCore.Crawler;
using LMCore.IO;
using UnityEngine;
using LMCore.Extensions;

public class NaiveInstantMovement : MonoBehaviour
{
    [SerializeField]
    private int StepSize = 3;

    CrawlerInput2 cInput;
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

    void Start()
    {
        gEntity = GetComponent<GridEntity>();
        gEntity.Sync();
    }


    private void OnEnable()
    {
        if (cInput == null)
        {
            cInput = GetComponent<CrawlerInput2>();
        }
        cInput.OnMovement += CInput_OnMovement;
    }

    private void OnDisable()
    {
        cInput.OnMovement -= CInput_OnMovement;
    }



    private void CInput_OnMovement(int tickId, Movement movement, float duration)
    {
        if (movement.IsRotation()) {
            gEntity.Rotate(movement);
        }
        else if (movement.IsTranslation())
        {
            if (gController.CanMoveTo(movement, StepSize))
            {
                gEntity.Translate(movement);
            }
        }
        gEntity.Sync();
    }
}
