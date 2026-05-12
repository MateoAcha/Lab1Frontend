using UnityEngine;

public class RemotePlayerGhost : MonoBehaviour
{
    private void Update()
    {
        if (OnlinePlayerSync.Instance == null || !OnlinePlayerSync.Instance.HasRemotePlayer)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        transform.position = Vector3.Lerp(
            transform.position,
            OnlinePlayerSync.Instance.RemotePlayerPosition,
            Time.deltaTime * 8f);
    }
}
