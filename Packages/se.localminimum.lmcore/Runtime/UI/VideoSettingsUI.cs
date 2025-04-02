using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class VideoSettingsUI : MonoBehaviour
    {
        [SerializeField]
        OptionButton fullscreenUI;

        [SerializeField]
        OptionCarousell resolutionsUI;

        [SerializeField]
        Button resetResolutionBtn;

        [SerializeField]
        Button applyResolutionBtn;

        private void OnEnable()
        {
            fullscreenUI.Sync(Screen.fullScreen);
            fullscreenUI.OnSelect += FullscreenUI_OnSelect;

            resolutionsUI.OnChange += HandleResolutionsCarousell;

            ResetResolution();

            resolutionsUI.Interactable = Screen.fullScreen;

            resetResolutionBtn.onClick.AddListener(ResetResolution);
            applyResolutionBtn.onClick.AddListener(ApplyResolution);
        }

        private void OnDisable()
        {
            fullscreenUI.OnSelect -= FullscreenUI_OnSelect;

            resolutionsUI.OnChange -= HandleResolutionsCarousell;

            resetResolutionBtn.onClick.RemoveListener(ResetResolution);
            applyResolutionBtn.onClick.RemoveListener(ApplyResolution);
        }


        int activeResolutionIndex;

        void ResetResolution()
        {
            resolutionOptionIndex = Screen.resolutions.ToList().IndexOf(Screen.currentResolution);
            if (resolutionOptionIndex < 0) resolutionOptionIndex = 0;
            activeResolutionIndex = resolutionOptionIndex;
            HandleResolutionsCarousell(resolutionsUI, 0);
        }

        void ApplyResolution()
        {
            if (!Screen.fullScreen) return;
            var resolution = Screen.resolutions[resolutionOptionIndex];
            Screen.SetResolution(resolution.width, resolution.height, true);
            activeResolutionIndex = resolutionOptionIndex;
            HandleResolutionsCarousell(resolutionsUI, 0);
        }

        private void FullscreenUI_OnSelect(OptionButton button)
        {
            bool newValue = !Screen.fullScreen;
            Debug.Log("Got fullscreen toggle");
            Screen.fullScreen = newValue;
            fullscreenUI.Sync(newValue);
            resolutionsUI.Interactable = newValue;
            HandleResolutionsCarousell(resolutionsUI, 0, newValue);
        }

        int resolutionOptionIndex;

        private void HandleResolutionsCarousell(OptionCarousell carousell, int step) =>
            HandleResolutionsCarousell(carousell, step, Screen.fullScreen);

        private void HandleResolutionsCarousell(OptionCarousell carousell, int step, bool fullscreen)
        {
            resolutionOptionIndex += step;
            if (resolutionOptionIndex < 0)
            {
                resolutionOptionIndex += Screen.resolutions.Length;
            }
            else if (resolutionOptionIndex >= Screen.resolutions.Length)
            {
                resolutionOptionIndex = 0;
            }

            var res = Screen.resolutions[resolutionOptionIndex];
            carousell.SetSelectedOption($"{res.width} x {res.height} ({res.refreshRateRatio.value:0} Hz)");

            resetResolutionBtn.interactable = fullscreen && resolutionOptionIndex != activeResolutionIndex;
            applyResolutionBtn.interactable = fullscreen && resolutionOptionIndex != activeResolutionIndex;
        }
    }
}
