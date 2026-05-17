using UnityEngine;
using UnityEngine.UI;

public class CameraFollow : MonoBehaviour
{
    public float speed = 10f;
    public float baseOrthoSize = 6f;
    public float zoomPadding = 3f;
    public float maxOrthoSize = 14f;
    [Header("Split Screen")]
    [Range(0.1f, 1f)] public float splitFraction = 0.65f;
    [Range(0.1f, 1f)] public float mergeFraction = 0.45f;
    public float splitSpeed = 2f;
    public float splitOrthoSize = 9f;

    private Camera _cam;
    private Camera _cam2;
    private GameObject _cam2Obj;
    private GameObject _dividerCanvasObj;
    private GameObject _dividerObj;
    private RectTransform _dividerRect;
    private bool _wantSplit;
    private float _splitT;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        DestroyCam2();
        if (_dividerCanvasObj != null)
            Destroy(_dividerCanvasObj);
    }

    private void LateUpdate()
    {
        if (!MultiplayerState.IsMultiplayer)
        {
            FollowSingle();
            return;
        }

        Transform p1t = PlayerController.main != null ? PlayerController.main.transform : null;
        Transform p2t = GetP2();
        bool bothAlive = p1t != null && p2t != null;

        Vector3 p1 = p1t != null ? p1t.position : (p2t != null ? p2t.position : transform.position);
        Vector3 p2 = p2t != null ? p2t.position : p1;

        if (bothAlive)
        {
            if (!_wantSplit && IsBeyondThreshold(p1, p2, splitFraction)) _wantSplit = true;
            else if (_wantSplit && !IsBeyondThreshold(p1, p2, mergeFraction)) _wantSplit = false;
        }
        else
        {
            _wantSplit = false;
        }

        float targetT = bothAlive && _wantSplit ? 1f : 0f;
        _splitT = Mathf.MoveTowards(_splitT, targetT, Time.deltaTime * splitSpeed);

        if (_splitT > 0.001f)
            EnsureCam2();
        else
            DestroyCam2();

        UpdateCameras(p1, p2, bothAlive, Vector2.Distance(p1, p2));
        UpdateDivider();
    }

    private void FollowSingle()
    {
        _splitT = 0f;
        _wantSplit = false;
        DestroyCam2();
        HideDivider();
        _cam.rect = new Rect(0f, 0f, 1f, 1f);

        Transform targetTransform = PlayerController.main != null && PlayerController.main.enabled
            ? PlayerController.main.transform
            : MultiplayerState.GetNearestPlayer(transform.position);

        if (MultiplayerState.IsOnline
            && OnlinePlayerSync.Instance != null
            && OnlinePlayerSync.Instance.HasRemotePlayer
            && (PlayerController.main == null || !PlayerController.main.enabled))
        {
            Vector3 remoteTarget = OnlinePlayerSync.Instance.RemotePlayerPosition;
            remoteTarget.z = transform.position.z;
            transform.position = Vector3.Lerp(transform.position, remoteTarget, Time.deltaTime * speed);
            return;
        }

        if (targetTransform == null) return;
        Vector3 target = targetTransform.position;
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
    }

    private void UpdateCameras(Vector3 p1, Vector3 p2, bool bothAlive, float dist)
    {
        float t = _splitT;
        Vector3 mid = (p1 + p2) * 0.5f;

        // cam1 viewport shrinks from full width to left half as split increases
        float splitW = Mathf.Lerp(1f, 0.5f, t);
        _cam.rect = new Rect(0f, 0f, splitW, 1f);

        // cam1 position: midpoint when merged, p1 when split
        Vector3 cam1Target = Vector3.Lerp(mid, p1, t);
        cam1Target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, cam1Target, Time.deltaTime * speed);

        // cam1 ortho size: zoomed when merged, splitOrthoSize when split
        float mergedSize = bothAlive
            ? Mathf.Clamp(dist * 0.5f + zoomPadding, baseOrthoSize, maxOrthoSize)
            : baseOrthoSize;
        float desiredSize = Mathf.Lerp(mergedSize, splitOrthoSize, t);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, desiredSize, Time.deltaTime * speed);

        if (_cam2 == null) return;

        // cam2 viewport grows from nothing (right edge) to right half
        _cam2.enabled = t > 0.001f;
        _cam2.rect = new Rect(splitW, 0f, 1f - splitW, 1f);

        // cam2 position: midpoint when merged, p2 when split
        Vector3 cam2Target = Vector3.Lerp(mid, p2, t);
        cam2Target.z = _cam2.transform.position.z;
        _cam2.transform.position = Vector3.Lerp(_cam2.transform.position, cam2Target, Time.deltaTime * speed);
        _cam2.orthographicSize = Mathf.Lerp(_cam2.orthographicSize, splitOrthoSize, Time.deltaTime * speed);
    }

    private void EnsureCam2()
    {
        if (_cam2 != null) return;

        _cam2Obj = new GameObject("Camera_P2");
        _cam2Obj.transform.position = new Vector3(0f, 0f, -10f);

        _cam2 = _cam2Obj.AddComponent<Camera>();
        _cam2.orthographic = true;
        _cam2.orthographicSize = _cam.orthographicSize;
        _cam2.backgroundColor = _cam.backgroundColor;
        _cam2.cullingMask = _cam.cullingMask;
        _cam2.clearFlags = _cam.clearFlags;
        _cam2.depth = _cam.depth - 1;
        _cam2.rect = new Rect(1f, 0f, 0f, 1f);
        _cam2.enabled = false;
    }

    private void DestroyCam2()
    {
        if (_cam2Obj == null) return;
        Destroy(_cam2Obj);
        _cam2Obj = null;
        _cam2 = null;
    }

    private void UpdateDivider()
    {
        if (_splitT < 0.001f)
        {
            HideDivider();
            return;
        }

        EnsureDivider();
        _dividerObj.SetActive(true);

        // Divider slides from right edge to screen center as split increases
        float splitW = Mathf.Lerp(1f, 0.5f, _splitT);
        _dividerRect.anchorMin = new Vector2(splitW, 0f);
        _dividerRect.anchorMax = new Vector2(splitW, 1f);
        _dividerRect.anchoredPosition = Vector2.zero;
        _dividerRect.sizeDelta = new Vector2(4f, 0f);
    }

    private void EnsureDivider()
    {
        if (_dividerObj != null) return;

        _dividerCanvasObj = new GameObject("SplitScreenCanvas");
        Canvas canvas = _dividerCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        _dividerCanvasObj.AddComponent<CanvasScaler>();
        _dividerCanvasObj.AddComponent<GraphicRaycaster>();

        _dividerObj = new GameObject("SplitDivider");
        _dividerObj.transform.SetParent(_dividerCanvasObj.transform, false);

        _dividerRect = _dividerObj.AddComponent<RectTransform>();
        _dividerRect.pivot = new Vector2(0.5f, 0.5f);

        _dividerObj.AddComponent<Image>().color = Color.black;
    }

    private void HideDivider()
    {
        if (_dividerObj != null)
            _dividerObj.SetActive(false);
    }

    // Returns true when players exceed `fraction` of the base visible screen dimensions
    // on either axis — uses baseOrthoSize so the threshold doesn't shift as the camera zooms.
    private bool IsBeyondThreshold(Vector3 p1, Vector3 p2, float fraction)
    {
        float dx = Mathf.Abs(p1.x - p2.x);
        float dy = Mathf.Abs(p1.y - p2.y);
        float halfW = baseOrthoSize * _cam.aspect;
        float halfH = baseOrthoSize;
        return dx > halfW * 2f * fraction || dy > halfH * 2f * fraction;
    }

    private Transform GetP2()
    {
        Transform p1t = PlayerController.main != null ? PlayerController.main.transform : null;
        return MultiplayerState.GetOtherPlayer(p1t);
    }
}
