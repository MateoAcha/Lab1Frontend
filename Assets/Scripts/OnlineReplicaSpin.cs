using UnityEngine;

public class OnlineReplicaSpin : MonoBehaviour
{
    public float zDegreesPerSecond;

    private void Update()
    {
        if (Mathf.Abs(zDegreesPerSecond) <= 0.001f)
            return;

        transform.Rotate(0f, 0f, zDegreesPerSecond * Time.deltaTime);
    }
}
