using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OnlinePlayerSync : MonoBehaviour
{
    public static OnlinePlayerSync Instance { get; private set; }
    public Vector3 RemotePlayerPosition { get; private set; }
    public bool HasRemotePlayer { get; private set; }

    // Called by GameStateHost/GameStateGuest to push the other player's position
    // at WebSocket speed (replaces the lobby ping value during gameplay).
    public void SetRemotePosition(Vector3 pos)
    {
        RemotePlayerPosition = pos;
        HasRemotePlayer = true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(SyncLoop());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator SyncLoop()
    {
        string weapon = PlayerLoadout.EquippedWeapon     != null ? PlayerLoadout.EquippedWeapon.itemName     : "";
        string armor  = PlayerLoadout.EquippedArmor      != null ? PlayerLoadout.EquippedArmor.itemName      : "";
        string item   = PlayerLoadout.EquippedConsumable != null ? PlayerLoadout.EquippedConsumable.itemName : "";

        while (true)
        {
            if (PlayerController.main == null)
            {
                yield return new WaitForSeconds(2f);
                continue;
            }

            Vector3 pos = PlayerController.main.transform.position;

            string json = JsonUtility.ToJson(new PingBody
            {
                weapon = weapon, armor = armor, item = item,
                x = pos.x, y = pos.y
            });

            string url = GameStatsTracker.ApiBaseUrl.TrimEnd('/') + "/lobby/ping";
            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(AuthSession.AccessToken))
                request.SetRequestHeader("Authorization", $"Bearer {AuthSession.AccessToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success
                && request.responseCode >= 200 && request.responseCode < 300)
            {
                try
                {
                    PingResponse resp = JsonUtility.FromJson<PingResponse>(request.downloadHandler.text);
                    if (resp?.players != null)
                    {
                        foreach (LobbyPlayerData p in resp.players)
                        {
                            if (p != null && p.username != AuthSession.Username)
                            {
                                RemotePlayerPosition = new Vector3(p.x, p.y, 0f);
                                HasRemotePlayer = true;
                                break;
                            }
                        }
                    }
                }
                catch { }
            }

            yield return new WaitForSeconds(2f);
        }
    }

    [Serializable] private class PingBody     { public string weapon, armor, item; public float x, y; }
    [Serializable] private class PingResponse { public LobbyPlayerData[] players; public bool started; }
}
