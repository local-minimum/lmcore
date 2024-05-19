using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[ExecuteInEditMode]
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

    [SerializeField, Range(0, 1)]
    float position = 0.5f;

    private void Update()
    {
        var rt = GetComponent<RectTransform>();

        var pt = anchor.GetPoint(position, out float normalAngle);

        var rotation = rt.rotation.eulerAngles;
        rotation.z = normalAngle;

        rt.localRotation = Quaternion.Euler(rotation);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.localPosition = pt;
    }
}
