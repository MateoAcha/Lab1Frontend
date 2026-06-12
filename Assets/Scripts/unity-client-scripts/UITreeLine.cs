using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class UITreeLine : MonoBehaviour
{
    public RectTransform nodeA;
    public RectTransform nodeB;

    private RectTransform _rt;

    private void Awake() => _rt = GetComponent<RectTransform>();

    private void LateUpdate()
    {
        if (_rt == null || nodeA == null || nodeB == null) return;

        Vector3 a = nodeA.position;
        Vector3 b = nodeB.position;
        transform.position = (a + b) * 0.5f;

        Vector3 dir = b - a;
        float worldDist = dir.magnitude;
        float sy = _rt.lossyScale.y;
        _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, sy > 0.001f ? worldDist / sy : 0f);

        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        _rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}
