using TMPro;
using UnityEngine;

public class PlayerReviveState : MonoBehaviour
{
    public float reviveRange = 2.2f;
    public float reviveDuration = 2.4f;
    public float reviveHpFraction = 0.45f;
    public float downedRotationDegrees = 90f;

    private Health _health;
    private Rigidbody2D _body;
    private Transform _visualRoot;
    private Quaternion _visualStartRotation = Quaternion.identity;
    private PlayerAnimator _animator;
    private TextMeshPro _promptText;
    private GameObject _barRoot;
    private Transform _barFill;
    private float _reviveProgress;

    public bool IsDowned { get; private set; }
    public float ReviveProgress01 => _reviveProgress;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _body = GetComponent<Rigidbody2D>();
        CacheVisualRoot();
        BuildRevivePrompt();
        BuildReviveBar();
        ApplyVisualState();
    }

    private void Update()
    {
        RefreshPromptVisibility();
        RefreshReviveBar();
    }

    public void Down()
    {
        if (IsDowned)
            return;

        IsDowned = true;
        _reviveProgress = 0f;
        if (_health != null)
            _health.SetHealthSilently(0f, Mathf.Max(_health.maxHp, 1f));
        if (_body != null)
            _body.linearVelocity = Vector2.zero;
        ApplyVisualState();
    }

    public void Revive()
    {
        float maxHp = _health != null ? Mathf.Max(_health.maxHp, 1f) : 1f;
        float revivedHp = Mathf.Max(1f, maxHp * Mathf.Clamp01(reviveHpFraction));
        if (_health != null)
            _health.ReviveWithHealth(revivedHp);

        IsDowned = false;
        _reviveProgress = 0f;
        ApplyVisualState();
    }

    public void ApplySyncedState(bool downed, float progress01)
    {
        if (downed)
        {
            if (!IsDowned)
                Down();
            SetReviveProgress01(progress01);
            return;
        }

        if (IsDowned)
        {
            IsDowned = false;
            _reviveProgress = 0f;
            ApplyVisualState();
        }
        else
        {
            SetReviveProgress01(0f);
        }
    }

    public bool CanBeRevivedBy(PlayerController reviver)
    {
        return IsDowned &&
            reviver != null &&
            !reviver.IsDowned &&
            reviver.transform != transform &&
            Vector2.Distance(reviver.transform.position, transform.position) <= reviveRange;
    }

    public bool ReceiveRevive(PlayerController reviver, bool held, float deltaTime)
    {
        if (!held || !CanBeRevivedBy(reviver))
        {
            SetReviveProgress01(0f);
            return false;
        }

        float duration = Mathf.Max(0.05f, reviveDuration);
        SetReviveProgress01(_reviveProgress + deltaTime / duration);
        if (_reviveProgress < 1f)
            return false;

        Revive();
        return true;
    }

    public void SetReviveProgress01(float progress01)
    {
        _reviveProgress = Mathf.Clamp01(progress01);
        RefreshReviveBar();
    }

    private void CacheVisualRoot()
    {
        Transform sprite = transform.Find("Sprite");
        _visualRoot = sprite != null ? sprite : transform;
        _visualStartRotation = _visualRoot.localRotation;
        _animator = _visualRoot.GetComponent<PlayerAnimator>();
    }

    private void ApplyVisualState()
    {
        if (_visualRoot == null)
            CacheVisualRoot();

        if (_visualRoot != null)
        {
            _visualRoot.localRotation = IsDowned
                ? _visualStartRotation * Quaternion.Euler(0f, 0f, downedRotationDegrees)
                : _visualStartRotation;
        }

        if (_animator != null)
            _animator.enabled = !IsDowned;

        RefreshPromptVisibility();
        RefreshReviveBar();
    }

    private void BuildRevivePrompt()
    {
        GameObject promptObj = new GameObject("RevivePrompt");
        promptObj.transform.SetParent(transform, false);
        promptObj.transform.localPosition = new Vector3(0f, 1.42f, 0f);

        _promptText = promptObj.AddComponent<TextMeshPro>();
        _promptText.text = "Press F to revive";
        _promptText.font = TMP_Settings.defaultFontAsset;
        _promptText.fontSize = 3.8f;
        _promptText.alignment = TextAlignmentOptions.Center;
        _promptText.color = new Color(1f, 0.96f, 0.58f, 1f);
        _promptText.enableWordWrapping = false;
        _promptText.raycastTarget = false;
        MeshRenderer promptRenderer = _promptText.GetComponent<MeshRenderer>();
        if (promptRenderer != null)
            promptRenderer.sortingOrder = 35;
        promptObj.SetActive(false);
    }

    private void BuildReviveBar()
    {
        _barRoot = new GameObject("ReviveBar");
        _barRoot.transform.SetParent(transform, false);
        _barRoot.transform.localPosition = new Vector3(0f, 1.12f, 0f);

        GameObject back = new GameObject("Back");
        back.transform.SetParent(_barRoot.transform, false);
        back.transform.localScale = new Vector3(1.25f, 0.11f, 1f);
        SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
        backRenderer.sprite = SimpleSprite.Square;
        backRenderer.color = new Color(0f, 0f, 0f, 0.72f);
        backRenderer.sortingOrder = 34;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(_barRoot.transform, false);
        _barFill = fill.transform;
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = SimpleSprite.Square;
        fillRenderer.color = new Color(0.38f, 1f, 0.62f, 0.95f);
        fillRenderer.sortingOrder = 35;
        _barRoot.SetActive(false);
    }

    private void RefreshPromptVisibility()
    {
        if (_promptText == null)
            return;

        bool visible = false;
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            if (CanBeRevivedBy(players[i]))
            {
                visible = true;
                break;
            }
        }
        _promptText.gameObject.SetActive(visible);
    }

    private void RefreshReviveBar()
    {
        if (_barRoot == null || _barFill == null)
            return;

        bool visible = IsDowned && _reviveProgress > 0.001f;
        _barRoot.SetActive(visible);
        float width = 1.19f * Mathf.Clamp01(_reviveProgress);
        _barFill.localScale = new Vector3(width, 0.07f, 1f);
        _barFill.localPosition = new Vector3((-1.19f + width) * 0.5f, 0f, 0f);
    }
}
