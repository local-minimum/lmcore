using Ink.Runtime;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class StoryCollection : MonoBehaviour, IOnLoadSave
    {
        static HashSet<StoryCollection> _Collections = new HashSet<StoryCollection>();
        public static IEnumerable<StoryCollection> Collections => _Collections;


        [SerializeField]
        string Id = "stories";

        [SerializeField]
        List<TextAsset> InkJsons = new List<TextAsset>();

        List<Story> InkStories = new List<Story>();

        int activeIndex = 0;

        protected string PrefixLogMessage(string message) =>
            $"StoryCollection {name} {activeIndex + 1}/{InkJsons.Count}: {message}";

        private void UnsafeLoadStories()
        {
            while (InkStories.Count <= activeIndex)
            {
                Debug.Log(PrefixLogMessage("Loading next story"));
                InkStories.Add(new Story(InkJsons[InkStories.Count].text));
            }
        }

        public bool HasContinuableStory
        {
            get
            {
                var n = InkJsons.Count;
                if (activeIndex >= n) return false;

                while (activeIndex < n)
                {
                    UnsafeLoadStories();

                    if (InkStories.Last().canContinue) return true;

                    activeIndex++;
                }

                return true;
            }
        }

        public bool ClaimStory(out Story story)
        {
            if (activeIndex >= InkJsons.Count)
            {
                Debug.LogWarning(PrefixLogMessage("No story left to claim"));
                story = null;
                return false;
            }

            UnsafeLoadStories();

            story = InkStories[activeIndex];
            Debug.Log(PrefixLogMessage($"Claiming story {story}"));
            return true;
        }

        public int OnLoadPriority => 1000;

        public KeyValuePair<string, int> Save() =>
            new KeyValuePair<string, int>(Id, activeIndex);

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            activeIndex = save.storyCollections.GetValueOrDefault(Id);
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }

        private void Awake()
        {
            _Collections.Add(this);
        }

        private void OnDestroy()
        {
            _Collections.Remove(this);
        }
    }
}
