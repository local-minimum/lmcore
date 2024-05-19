using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcAnchorUI : MonoBehaviour
{
    [System.Serializable]
    public enum Anchor { Bottom, Left, Top, Right };

    [SerializeField]
    Anchor anchor = Anchor.Bottom;

    public float Radius
    {
        get
        {
            var size = GetComponent<RectTransform>().rect.size;

            if (anchor == Anchor.Bottom || anchor == Anchor.Top)
            {
                return size.y / 2 + Mathf.Pow(size.x, 2) / (8 * size.y);
            }
            return size.x / 2 + Mathf.Pow(size.y, 2) / (8 * size.x);
        }
    }

    private float Theta
    {
        get
        {
            var rect = GetComponent<RectTransform>().rect;
            var r = Radius;

            return anchor == Anchor.Bottom || anchor == Anchor.Top ? Mathf.Acos((r - rect.height) / r) : Mathf.Acos((r - rect.width) / r);
        }
    }

    public Vector2 Origo
    {
        get
        {
            var rect = GetComponent<RectTransform>().rect;
            var r = Radius;
            var t = Theta;

            var x = (anchor == Anchor.Bottom || anchor == Anchor.Top) ? rect.x + rect.width / 2 : (anchor == Anchor.Left ? -1 : 1) * r * Mathf.Cos(t);
            var y = (anchor == Anchor.Bottom || anchor == Anchor.Top) ? (anchor == Anchor.Bottom ? -1 : 1) * r * Mathf.Cos(t) : rect.y + rect.height / 2;

            return new Vector2(x, y);
        }
    }

    public Vector2 GetPoint(float progress, out float normalDegrees)
    {
        var o = Origo;
        var r = Radius;
        var theta = Theta;

        var a = Mathf.Lerp(theta, -theta, progress);
        switch (anchor)
        {
            case Anchor.Bottom:
                a += Mathf.PI / 2;
                break;
            case Anchor.Right:
                a += Mathf.PI;
                break;
            case Anchor.Top:
                a += Mathf.PI * 1.5f;
                break;
       }

        normalDegrees = (Mathf.PI * 3 / 2 + a) * Mathf.Rad2Deg;

        return new Vector2(o.x + Mathf.Cos(a) * r, o.y + Mathf.Sin(a) * r);
    }
}
