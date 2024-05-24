using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    Dictionary<ArcItemUI, float> startPositions = new Dictionary<ArcItemUI, float>();
    Dictionary<ArcItemUI, float> startTimes = new Dictionary<ArcItemUI, float>();

    ArcItemUI[] Items
    {
        get
        {
            var items = GetComponentsInChildren<ArcItemUI>();

            foreach (var item in items)
            {
                if (!startPositions.ContainsKey(item))
                {
                    startPositions.Add(item, item.Position);
                }
            }

            var outdated = startPositions.Keys.Where(k => !items.Contains(k)).ToList();
            foreach (var key in outdated)
            {
                startPositions.Remove(key);
                startTimes.Remove(key);
            }

            return items;
        }
    }

    float LerpEvenlySpaced(int index, int count) => Mathf.Lerp(startProgress, endProgress, (float)index / (count - 1));

    List<KeyValuePair<ArcItemUI, float>> GetTargetPositions(ArcItemUI[] items)
    {
        if (items.Length == 0) return new List<KeyValuePair<ArcItemUI, float>>();

        if (layout == Layout.StretchEvenlySpaced)
        {
            if (items.Length == 1) return new List<KeyValuePair<ArcItemUI, float>> {
                new KeyValuePair<ArcItemUI, float>(items[0], 0.5f)
            };

            return items
                .Select((item, idx) => new KeyValuePair<ArcItemUI, float>(item, LerpEvenlySpaced(idx, items.Length)))
                .ToList();
        }

        if (layout == Layout.SpaceBetween)
        {
            if (items.Length == 1) return new List<KeyValuePair<ArcItemUI, float>> {
                new KeyValuePair<ArcItemUI, float>(items[0], 0.5f)
            };

            var spacingNeed = spaceBetween * (items.Length - 1);
            var itemNeed = items.Select((item, idx) => item.progressClaimed * (idx == 0 || idx == items.Length - 1 ? 0.5f : 1f)).Sum();


            var position = 0.5f - (itemNeed + spacingNeed) / 2f;

            return items
                .Select((item, idx) => {
                    if (idx != 0)
                    {
                        position += item.progressClaimed * 0.5f;
                    }
                    var ret = new KeyValuePair<ArcItemUI, float>(item, position);
                    if (idx != items.Length - 1)
                    {
                        position += item.progressClaimed * 0.5f + spaceBetween;
                    }
                    return ret;
                 })
                .ToList();
        }

        Debug.LogWarning($"Arc Layout: {name} does not know how to handle {layout} for {items.Length} items");
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
        } else if (easing == Easing.Linear) { 
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
                } else if (startTimes.ContainsKey(item))
                {
                    startTimes.Remove(item);
                }
            }        
        }
    }
}
