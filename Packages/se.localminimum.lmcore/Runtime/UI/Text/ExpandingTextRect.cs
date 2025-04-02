using System.Linq;
using TMPro;
using UnityEngine;

namespace LMCore.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class ExpandingTextRect : MonoBehaviour
    {
        [SerializeField]
        bool validateWidthToo;

        TextMeshProUGUI _Text;

        TextMeshProUGUI Text
        {
            get
            {
                if (_Text == null)
                {
                    _Text = GetComponent<TextMeshProUGUI>();
                }
                return _Text;
            }
        }

        private void OnEnable()
        {
            Text.OnPreRenderText += Text_OnPreRenderText;
            Recalculate();
        }

        private void OnDisable()
        {
            Text.OnPreRenderText -= Text_OnPreRenderText;
        }

        bool recalculate;

        private void Text_OnPreRenderText(TMP_TextInfo obj)
        {
            Recalculate();
            recalculate = true;
        }

        [ContextMenu("Recalculate")]
        void Recalculate()
        {
            var rt = transform as RectTransform;
            var textInfo = Text.textInfo;

            var lines = textInfo.lineCount;
            var size = rt.sizeDelta;

            size.y = textInfo.lineInfo
                .Take(lines)
                .Sum(l => l.lineHeight + Text.lineSpacing + Text.paragraphSpacing) + Text.margin.y + Text.margin.w;


            if (validateWidthToo && textInfo.lineInfo.Length > 0)
            {
                size.x = textInfo.lineInfo.Max(l => l.lineExtents.max.x - l.lineExtents.min.x);
            }

            if (size != rt.sizeDelta)
            {
                rt.sizeDelta = size;
                Text.ForceMeshUpdate();
                var sentinel = GetComponent<ChildFitterSentinel>();
                if (sentinel != null)
                {
                    sentinel.SetDirty();
                }
            }

            recalculate = false;
        }


        private void Update()
        {
            if (recalculate)
            {
                Recalculate();
            }
        }
    }
}
