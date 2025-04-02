using UnityEngine;

namespace LMCore.UI
{
    public delegate void RecalculateSizeEvent();

    [System.Flags]
    public enum Dimension { Nothing = 0, Width = 1, Height = 2 };

    public abstract class AbsFitter : MonoBehaviour
    {
        abstract public event RecalculateSizeEvent OnRecalculateSize;
        abstract public void Recalculate();

        abstract public Dimension Dimensions { get; }
    }
}
