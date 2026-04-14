using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float speed = 10f;

    private void LateUpdate()
    {
        if (PlayerController.main == null)
        {
            return;
        }

        Vector3 target = PlayerController.main.transform.position;
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }
}
