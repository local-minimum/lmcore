using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{

    public delegate void OptionButtonSelectEvent(OptionButton button);

    public class OptionButton : MonoBehaviour
    {
        public event OptionButtonSelectEvent OnSelect;

        [SerializeField]
        Image target;

        [SerializeField]
        Sprite selectedSprite;

        [SerializeField]
        Sprite unselectedSprite;

        [SerializeField]
        List<OptionButton> group = new List<OptionButton>();

        public void Select()
        {
            target.sprite = selectedSprite;
            foreach (var other in group)
            {
                if (other == this) continue;
                other.target.sprite = other.unselectedSprite;
            }

            OnSelect?.Invoke(this);
        }

        public void Sync(bool value)
        {
            target.sprite = value ? selectedSprite : unselectedSprite;
        }
    }
}
