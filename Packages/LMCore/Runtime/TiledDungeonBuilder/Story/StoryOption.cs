using Ink.Runtime;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LMCore.TiledDungeon.Narrative
{
    public delegate void StyleStoryOptionEvent(StoryOption option, StoryOption.Style style);
    public delegate void OptionSelectEvent(StoryOption option, string selectEvent);

    /// <summary>
    /// Option in a story.
    /// 
    /// It supports various tags in Ink.
    /// - Styling (when option becomes visible):
    ///   - AVATAR (string): name of an avatar to go with the option
    ///   - TRIGGER (string): name of an animation trigger to play
    ///   - NARRATOR (bool): styling option as being a narration
    ///   - BACKGROUND (color)
    ///   - TEXT (color)
    ///   
    /// - Events (when option is selected)
    ///   - EVENT (string): Invokes OnSelect event with the value for each event specified for the option
    /// </summary>
    public class StoryOption : MonoBehaviour
    {
        private static string StringFor(List<KeyValuePair<string, string>> tags, string key) =>
            tags.FirstOrDefault(kvp => kvp.Key.ToUpper() == key.ToUpper()).Value;

        /// <summary>
        /// Event for those interested in styling and reacting to an option being available
        /// </summary>
        public static event StyleStoryOptionEvent OnStyle;
        public static event OptionSelectEvent OnSelect;

        public struct Style
        {
            public readonly string AvatarId;
            public readonly Color BackgroundColor;
            public readonly Color TextColor;
            public readonly string AnimationTrigger;
            public readonly bool IsNarration;

            public Style(List<KeyValuePair<string, string>> tags)
            {
                AvatarId = StringFor(tags, "AVATAR");
                AnimationTrigger = StringFor(tags, "TRIGGER");
                IsNarration = Style.BoolFor(tags, "NARRATOR");
                BackgroundColor = Style.ColorFor(tags, "BACKGROUND");
                TextColor = Style.ColorFor(tags, "TEXT");
            }

            private static Style _NoStyle = new Style();
            public static Style NoStyle => _NoStyle;

            private static bool BoolFor(List<KeyValuePair<string, string>> parsed, string key)
            {
                switch (StringFor(parsed, key)?.ToUpper())
                {
                    case "YES":
                    case "Y":
                    case "TRUE":
                    case "T":
                    case "1":
                        return true;
                    default:
                        return false;
                }
            }

            private static Color ColorFor(List<KeyValuePair<string, string>> parsed, string key)
            {
                var color = StringFor(parsed, key);
                if (ColorUtility.TryParseHtmlString(color, out var parsedColor)) return parsedColor;

                return Color.clear;
            }
        }

        [SerializeField]
        TextMeshProUGUI ShadowText;
        [SerializeField]
        TextMeshProUGUI OptionText;
        Button Button;

        string PrefixLogMessage(string message) =>
            $"StoryOption: {message}";

        public void Configure(Choice choice, UnityAction onSelect)
        {
            Debug.Log(PrefixLogMessage($"{choice.index}: {choice.text}"));

            if (Button == null)
            {
                Button = GetComponentInChildren<Button>();
            }


            ShadowText.text = choice.text;
            OptionText.text = choice.text;

            var parsedTags = choice.tags?
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Select(tag =>
                {
                    var parts = tag.Split(":", 1).Select(s => s.Trim()).ToList();
                    return new KeyValuePair<string, string>(parts[0], parts[1]);
                })
                .ToList();

            if (parsedTags != null)
            {
                OnStyle?.Invoke(this, new Style(parsedTags));
            }
            else
            {
                OnStyle?.Invoke(this, Style.NoStyle);
            }

            // Setting this false here forces sticky selection the second time
            // an option is showed after it was selected the previous time.
            Button.interactable = false;
            Button.onClick.RemoveAllListeners();
            if (onSelect != null)
            {
                var events = parsedTags?.Where(kvp => kvp.Key == "EVENT").Select(kvp => kvp.Value).ToList();
                if (events != null && events.Count > 0)
                {
                    Button.onClick.AddListener(() =>
                    {
                        foreach (var e in events)
                        {
                            OnSelect?.Invoke(this, e);
                        }
                    });
                }
                // Do something with tags spawning events if of certain type... if clicked
                Button.onClick.AddListener(onSelect);
                Button.interactable = true;
            }
            else
            {
                Button.interactable = false;
            }
        }
    }
}
