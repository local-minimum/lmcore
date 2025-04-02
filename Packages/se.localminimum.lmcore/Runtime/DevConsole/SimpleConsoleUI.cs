using LMCore.Extensions;
using LMCore.Utilities;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LMCore.DevConsole
{
    [RequireComponent(typeof(CanvasGroup))]
    public class SimpleConsoleUI : AbsConsoleUI
    {
        public override event InputEvent OnInput;

        [SerializeField]
        AnimationCurve FadeInCurve;

        [SerializeField]
        AnimationCurve FadeOutCurve;

        [SerializeField]
        TextMeshProUGUI outputUI;

        [SerializeField, Range(0, 1000)]
        int maxLines = 200;

        [SerializeField]
        Color logColor;
        [SerializeField]
        Color warnColor;
        [SerializeField]
        Color errorColor;
        [SerializeField]
        Color inputColor;

        [SerializeField]
        TMP_InputField field;

        [SerializeField]
        TextMeshProUGUI context;

        public override void Hide(bool instant)
        {
            field.interactable = false;
            if (instant)
            {
                var group = GetComponent<CanvasGroup>();
                group.alpha = 0f;
                gameObject.SetActive(false);
            }
            else
            {
                StartCoroutine(Fade(FadeOutCurve, () => gameObject.SetActive(false)));
            }
        }

        string LevelToColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Input:
                    return ColorUtility.ToHtmlStringRGB(inputColor);
                case LogLevel.Log:
                    return ColorUtility.ToHtmlStringRGB(logColor);
                case LogLevel.Warning:
                    return ColorUtility.ToHtmlStringRGB(warnColor);
                case LogLevel.Error:
                    return ColorUtility.ToHtmlStringRGB(errorColor);
                default:
                    return null;
            }
        }

        string DecorateMessage(string message, LogLevel level)
        {
            var color = LevelToColor(level);
            if (color == null) return message;

            return $"<color=#{LevelToColor(level)}>{message}</color>";
        }

        public override void Output(string message, LogLevel level)
        {
            outputUI.text = $"{outputUI.text}\n\n{DecorateMessage(message, level)}";
        }

        public override void Show()
        {
            field.interactable = true;
            gameObject.SetActive(true);
            StartCoroutine(Fade(FadeInCurve));
            if (field != null)
            {
                EventSystem.current.SetSelectedGameObject(field.gameObject);
                field.ActivateInputField();
            }
            else
            {
                DevConsole.Warn("No input field configured, panel won't accept input");
            }
        }

        IEnumerator<WaitForSeconds> Fade(
            AnimationCurve curve,
            System.Action cleanup = null)
        {
            var group = GetComponent<CanvasGroup>();
            var duration = curve.Duration();

            var durationProgress = new DurationProgress(duration);
            while (!durationProgress.Completed)
            {
                group.alpha = curve.Evaluate(durationProgress.Elapsed);
                yield return new WaitForSeconds(0.02f);
            }

            cleanup?.Invoke();
        }

        public void SubmitInput()
        {

            if (field == null) return;

            Debug.Log($"Console UI Raw message: '{field.text}'");

            Output($"<i>> {field.text}</i>", LogLevel.Input);

            OnInput?.Invoke(field.text);

            if (!string.IsNullOrEmpty(field.text))
            {
                field.text = "";
            }

            field.ActivateInputField();
        }

        bool refocus = false;

        public override bool Focused
        {
            get
            {
                if (field == null) return false;
                return field.isFocused;
            }
        }

        public override bool Showing =>
            gameObject.activeSelf;

        private void OnEnable()
        {
            DevConsole.OnContextChange += DevConsole_OnContextChange;
        }

        private void OnDisable()
        {
            DevConsole.OnContextChange -= DevConsole_OnContextChange;
        }

        private void DevConsole_OnContextChange(string[] context)
        {
            if (this.context == null) return;

            if (context.Length == 0)
            {
                this.context.text = ">";
                return;
            }

            int startIdx = 0;
            if (context.Length > 2)
            {
                startIdx = context.Length - 2;
            }

            var prefix = string.Join(">", context.Skip(startIdx));

            this.context.text = $"{(startIdx > 0 ? ">>" : "")}{prefix}>";
        }
    }
}
