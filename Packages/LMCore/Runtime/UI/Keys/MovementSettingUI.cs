using LMCore.IO;
using UnityEngine;

namespace LMCore.UI
{
    public class MovementSettingUI : MonoBehaviour
    {
        [SerializeField]
        SimpleButton InstantMovement;
        [SerializeField]
        SimpleButton SmoothMovement;

        public void SetInstantMovement(bool value)
        {
            GameSettings.InstantMovement.Value = value;
        }

        private void OnEnable()
        {
            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
        }

        private void OnDisable()
        {
            GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
        }

        private void Start()
        {
            InstantMovement_OnChange(GameSettings.InstantMovement.Value);
        }

        private void InstantMovement_OnChange(bool value)
        {
            if (value)
            {
                InstantMovement.Selected();
                SmoothMovement.DeSelect();
            }
            else
            {
                SmoothMovement.Selected();
                InstantMovement.DeSelect();
            }
        }

    }
}
