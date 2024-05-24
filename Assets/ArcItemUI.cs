using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcItemUI : MonoBehaviour
{
    private ArcAnchorUI _anchorUI;
    private ArcAnchorUI anchor {
        get
        {
            if (_anchorUI == null)
            {
                _anchorUI = GetComponentInParent<ArcAnchorUI>();
            }
            return _anchorUI;
        }
    }

    void UpdateAnchor()
    {
        var rt = GetComponent<RectTransform>();

        var pt = anchor.GetPoint(_position, out float normalAngle);

        var rotation = rt.rotation.eulerAngles;
        rotation.z = normalAngle;

        rt.localRotation = Quaternion.Euler(rotation);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.localPosition = pt;
    }

    [SerializeField, HideInInspector]
    float _position = 0.5f;
    public float Position
    {
        get { return _position; }
        set
        {
            _position = value;
            UpdateAnchor();
        }
    }

    [Tooltip("For layouting with space between"), Range(0, 1)]
    public float progressClaimed = 0.1f;
}
