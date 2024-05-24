using UnityEngine;

[ExecuteInEditMode]
public class TwoSidedUI : MonoBehaviour
{
    [SerializeField]
    GameObject front;

    [SerializeField]
    GameObject back;

    [ContextMenu("Show Front")]
    public void ShowFront()
    {
        var euler = transform.eulerAngles;
        euler.y = 0;
        transform.localEulerAngles = euler;
    }

    [ContextMenu("Show Back")]
    public void ShowBack() { 
        var euler = transform.eulerAngles;
        euler.y = 180;
        transform.localEulerAngles = euler;
    }

    bool FrontVisible(float yRotation) => yRotation < 90f || yRotation > 270f;

    [ContextMenu("Flip")]
    public void Flip()
    {
        var euler = transform.localEulerAngles;
        euler.y = FrontVisible(euler.y) ? 180 : 0;
        transform.localEulerAngles = euler;
    }

    [ContextMenu("Normalize")]
    public void Normalize() { 
        var euler = transform.localEulerAngles;
        euler.y = FrontVisible(euler.y) ? 0 : 180;
        transform.localEulerAngles = euler;
    }

    void Update()
    {
        var showFront = FrontVisible(transform.eulerAngles.y);

        if (front != null) front.SetActive(showFront);
        if (back != null) back.SetActive(!showFront);
    }
}
