using LMCore.IO;
using UnityEngine;

namespace LMCore.UI
{
    [RequireComponent(typeof(OptionButton))]
    public class MovementTypeSettingUI : MonoBehaviour
    {
        public enum MovementType { Smooth, Instant }

        [SerializeField]
        MovementType movementType;

        private void Start()
        {
            var selected = GameSettings.InstantMovement.Value == (movementType == MovementType.Instant);
            if (selected)
            {
                GetComponent<OptionButton>().Select();
            }
        }

        private void OnEnable()
        {
            GetComponent<OptionButton>().OnSelect += MovementTypeSettingUI_OnSelect;
        }


        private void OnDisable()
        {
            GetComponent<OptionButton>().OnSelect -= MovementTypeSettingUI_OnSelect;
        }

        private void MovementTypeSettingUI_OnSelect(OptionButton button)
        {
            GameSettings.InstantMovement.Value = movementType == MovementType.Instant;
        }
    }
}
