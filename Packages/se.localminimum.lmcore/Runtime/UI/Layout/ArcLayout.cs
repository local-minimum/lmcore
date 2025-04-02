using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
    [ExecuteInEditMode]
    public class ArcLayout : MonoBehaviour
    {
        public enum Layout { StretchEvenlySpaced, SpaceBetween }

        public Layout layout = Layout.StretchEvenlySpaced;

        [Range(-1f, 2f)]
        public float startProgress = 0f;

        [Range(-1f, 2f)]
        public float endProgress = 1f;

        [Range(-0.5f, 0.5f)]
        public float spaceBetween = 0.025f;

        public enum Easing { Instant, Linear };

        public Easing easing = Easing.Instant;

        public float easingDuration = 0.5f;

        Dictionary<ArcItemUI, int> order = new Dictionary<ArcItemUI, int>();
        Dictionary<ArcItemUI, float> startPositions = new Dictionary<ArcItemUI, float>();
        Dictionary<ArcItemUI, float> startTimes = new Dictionary<ArcItemUI, float>();

        public enum Ordering { Start, EmptySlot, End };
        public Ordering ordering = Ordering.EmptySlot;

        [SerializeField, Tooltip("Cards spawn in at a certain progress")]
        bool forceStartPosition = true;
        [SerializeField, Tooltip("Only valid when forcing start position")]
        float forcedStartPosition = -0.5f;

        protected string PrefixLogMessage(string message) =>
            $"ArcLayout ({startPositions.Count} items): {message}";

        int NewItemOrdinal
        {
            get
            {
                if (order.Count == 0) return 0;

                switch (ordering)
                {
                    case Ordering.Start:
                        return order.Values.Min() - 1;
                    case Ordering.End:
                        return order.Values.Max() + 1;
                    case Ordering.EmptySlot:
                        var prev = order.Values.Min();
                        foreach (var current in order.Values.OrderBy(x => x))
                        {
                            if (prev == current) continue;
                            if (prev + 1 == current)
                            {
                                prev = current;
                                continue;
                            }
                            return prev + 1;
                        }

                        return prev + 1;
                }

                return 0;
            }
        }

        public void AddItem(GameObject go)
        {
            var item = go.GetComponent<ArcItemUI>();
            if (item == null) return;

            if (forceStartPosition)
            {
                item.Position = forcedStartPosition;
                // Debug.Log(PrefixLogMessage($"Adding {item.name} with forced {forcedStartPosition} position"));
            }
            startPositions.Add(item, item.Position);
        }

        public void RemoveItem(GameObject go, Transform newParent)
        {
            var item = go.GetComponent<ArcItemUI>();
            if (item == null) return;

            startPositions.Remove(item);
            startTimes.Remove(item);
            order.Remove(item);

            item.transform.SetParent(newParent);
        }

        List<ArcItemUI> Items
        {
            get
            {
                var items = GetComponentsInChildren<ArcItemUI>();
                var newItems = new List<ArcItemUI>();

                foreach (var item in items)
                {
                    if (!startPositions.ContainsKey(item))
                    {
                        if (forceStartPosition)
                        {
                            item.Position = forcedStartPosition;
                        }

                        startPositions.Add(item, item.Position);
                    }

                    if (!order.ContainsKey(item))
                    {
                        var ordinal = NewItemOrdinal;
                        order.Add(item, ordinal);
                        newItems.Add(item);
                    }
                }

                var outdated = startPositions.Keys.Where(k => !items.Contains(k)).ToList();
                foreach (var key in outdated)
                {
                    startPositions.Remove(key);
                    startTimes.Remove(key);
                    order.Remove(key);
                }

                var sorted = items.OrderBy(i => order[i]).ToList();

                foreach (var item in newItems)
                {
                    item.transform.SetSiblingIndex(sorted.IndexOf(item));
                }
                return sorted;
            }
        }

        float LerpEvenlySpaced(int index, int count) => Mathf.Lerp(startProgress, endProgress, (float)index / (count - 1));

        List<KeyValuePair<ArcItemUI, float>> GetTargetPositions(List<ArcItemUI> items)
        {
            var count = items.Count;

            if (count == 0) return new List<KeyValuePair<ArcItemUI, float>>();

            if (layout == Layout.StretchEvenlySpaced)
            {
                if (count == 1) return new List<KeyValuePair<ArcItemUI, float>> {
                new KeyValuePair<ArcItemUI, float>(items[0], 0.5f)
            };

                return items
                    .Select((item, idx) => new KeyValuePair<ArcItemUI, float>(item, LerpEvenlySpaced(idx, count)))
                    .ToList();
            }

            if (layout == Layout.SpaceBetween)
            {
                if (count == 1) return new List<KeyValuePair<ArcItemUI, float>> {
                new KeyValuePair<ArcItemUI, float>(items[0], 0.5f)
            };

                var spacingNeed = spaceBetween * (count - 1);
                var itemNeed = items.Select((item, idx) => item.progressClaimed * (idx == 0 || idx == count - 1 ? 0.5f : 1f)).Sum();


                var position = 0.5f - ((itemNeed + spacingNeed) / 2f);

                return items
                    .Select((item, idx) =>
                    {
                        if (idx != 0)
                        {
                            position += item.progressClaimed * 0.5f;
                        }
                        var ret = new KeyValuePair<ArcItemUI, float>(item, position);
                        if (idx != count - 1)
                        {
                            position += (item.progressClaimed * 0.5f) + spaceBetween;
                        }
                        return ret;
                    })
                    .ToList();
            }

            Debug.LogWarning(PrefixLogMessage($"Does not know how to handle {layout} for {count} items"));
            return items
                .Select(items => new KeyValuePair<ArcItemUI, float>(items, 0))
                .ToList();
        }

        private void Update()
        {
            var targets = GetTargetPositions(Items);

            if (easing == Easing.Instant)
            {
                foreach (var (item, target) in targets)
                {
                    if (item.Position != target)
                    {
                        item.Position = target;
                    }
                }
            }
            else if (easing == Easing.Linear)
            {
                foreach (var (item, target) in targets)
                {
                    if (item.Position != target)
                    {
                        if (!startTimes.ContainsKey(item))
                        {
                            startTimes.Add(item, Time.timeSinceLevelLoad);
                            startPositions[item] = item.Position;
                        }

                        var progress = (Time.timeSinceLevelLoad - startTimes[item]) / easingDuration;
                        item.Position = Mathf.Lerp(startPositions[item], target, progress);
                    }
                    else if (startTimes.ContainsKey(item))
                    {
                        startTimes.Remove(item);
                    }
                }
            }
        }
    }
}
