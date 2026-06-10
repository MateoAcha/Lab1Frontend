using UnityEngine;

public class SpearThrustVisual : MonoBehaviour
{
    public Vector3 baseLocalPosition;
    public float thrustDistance = 0.08f;
    public float duration = 0.12f;

    private float _startTime;

    private void Start()
    {
        _startTime = Time.time;
        if (baseLocalPosition == Vector3.zero)
            baseLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        float progress = duration <= 0f ? 1f : Mathf.Clamp01((Time.time - _startTime) / duration);
        float pulse = Mathf.Sin(progress * Mathf.PI);
        transform.localPosition = baseLocalPosition + new Vector3(thrustDistance * pulse, 0f, 0f);
    }
}
