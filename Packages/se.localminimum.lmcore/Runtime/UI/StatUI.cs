using TMPro;
using UnityEngine;

namespace LMCore.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class StatUI : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI Label;

        [SerializeField]
        TextMeshProUGUI Value;

        public string LabelText => Label.text;
        public string ValueText => Value.text;


        public void Show(string label)
        {
            Label.text = label;
            Value.text = "";
            gameObject.SetActive(true);
        }

        public void Show(string label, int value) =>
            Show(label, value.ToString());

        public void Show(string label, string value)
        {
            Label.text = label;
            Value.text = value;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
