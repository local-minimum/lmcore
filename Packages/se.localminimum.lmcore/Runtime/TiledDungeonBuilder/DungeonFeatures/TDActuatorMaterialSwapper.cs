using LMCore.TiledDungeon.DungeonFeatures;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    [RequireComponent(typeof(TDActuator))]
    public class TDActuatorMaterialSwapper : MonoBehaviour
    {
        [SerializeField]
        Renderer target;

        [SerializeField]
        Material afterPressMaterial;

        [SerializeField]
        Material afterDePressMaterial;

        private void OnEnable()
        {
            GetComponent<TDActuator>().OnInteractionEnd += TDActuatorMaterialSwapper_OnInteractionEnd;
        }

        private void OnDisable()
        {
            GetComponent<TDActuator>().OnInteractionEnd -= TDActuatorMaterialSwapper_OnInteractionEnd;
        }

        private void TDActuatorMaterialSwapper_OnInteractionEnd(TDActuator actuator, bool pressed)
        {
            target.material = pressed ? afterPressMaterial : afterDePressMaterial;
        }
    }
}
