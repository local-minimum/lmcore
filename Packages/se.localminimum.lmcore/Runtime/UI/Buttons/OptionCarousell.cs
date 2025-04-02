using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{

    public delegate void OptionCarousellUpdateEvent(OptionCarousell carousell, int step);

    public class OptionCarousell : MonoBehaviour
    {
        public event OptionCarousellUpdateEvent OnChange;

        [SerializeField]
        Button previousButton;

        [SerializeField]
        Button nextButton;

        [SerializeField]
        TextMeshProUGUI selectedOption;

        public void SetSelectedOption(string text)
        {
            selectedOption.text = text;
        }

        public bool EnableNext
        {
            get => nextButton.interactable;
            set
            {
                nextButton.interactable = value;
            }
        }

        public bool EnablePrevious
        {
            get => previousButton.interactable;
            set
            {
                previousButton.interactable = value;
            }
        }

        public bool Interactable
        {
            set
            {
                previousButton.interactable = value;
                nextButton.interactable = value;
            }
        }

        private void OnEnable()
        {
            previousButton.onClick.AddListener(HandlePrevious);
            nextButton.onClick.AddListener(HandleNext);
        }

        private void OnDisable()
        {
            previousButton.onClick.RemoveListener(HandlePrevious);
            nextButton.onClick.RemoveListener(HandleNext);
        }

        void HandleNext()
        {
            OnChange?.Invoke(this, 1);
        }

        void HandlePrevious()
        {
            OnChange?.Invoke(this, -1);
        }
    }
}
