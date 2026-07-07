using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SocialPanelController : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 5f;
    private const float PanelWidth = 390f;
    private const float PanelHeight = 620f;

    private enum SocialTab
    {
        Friends,
        Inbox,
        Sent
    }

    private AuthApiClient _apiClient;
    private AuthMenuController _menuController;
    private bool _allowLobbyInvites;
    private int _roomNumber;
    private bool _built;
    private bool _refreshInFlight;
    private float _refreshStartedAt;
    private bool _actionInFlight;
    private Coroutine _refreshRoutine;
    private SocialTab _activeTab = SocialTab.Friends;
    private SocialSummaryResponse _summary;
    private string _listMessage = "Loading social data...";
    private FriendSummaryResponse _selectedFriend;

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _selectedFriendText;
    private TMP_InputField _usernameInput;
    private Button _friendsTabButton;
    private Button _inboxTabButton;
    private Button _sentTabButton;
    private GameObject _inboxNotificationDot;
    private Button _inviteFriendButton;
    private GameObject _sendFriendRow;
    private GameObject _friendActionsRow;
    private Transform _listContent;

    private GameObject _profileOverlay;
    private TextMeshProUGUI _profileNameText;
    private TextMeshProUGUI _profileLevelText;
    private TextMeshProUGUI _profileStatsText;
    private TextMeshProUGUI _profileLoadoutText;
    private TextMeshProUGUI _profileStatusText;
    private RectTransform _profilePreviewRoot;
    private Vector2 _profileSwordWeaponOffset = new Vector2(88f, -6f);
    private Vector2 _profileSwordWeaponSize = new Vector2(112f, 146f);
    private float _profileSwordWeaponRotation = -28f;
    private Vector2 _profileSpearWeaponOffset = new Vector2(92f, 0f);
    private Vector2 _profileSpearWeaponSize = new Vector2(92f, 178f);
    private float _profileSpearWeaponRotation = -35f;
    private Vector2 _profileRangedWeaponOffset = new Vector2(86f, 18f);
    private Vector2 _profileRangedWeaponSize = new Vector2(74f, 74f);
    private float _profileRangedWeaponRotation;

    public void Initialize(AuthApiClient apiClient, AuthMenuController menuController, bool allowLobbyInvites, int roomNumber)
    {
        _apiClient = apiClient;
        _menuController = menuController;
        _allowLobbyInvites = allowLobbyInvites;
        _roomNumber = roomNumber;

        BuildUiIfNeeded();
        UpdateContextText();
        Render();
    }

    public void SetApiClient(AuthApiClient apiClient)
    {
        _apiClient = apiClient;
        StartRefreshLoop();
    }

    public void SetContext(bool allowLobbyInvites, int roomNumber)
    {
        _allowLobbyInvites = allowLobbyInvites;
        _roomNumber = roomNumber;
        UpdateContextText();
        Render();
    }

    public void SetProfilePreviewWeaponOffsets(
        Vector2 swordOffset,
        Vector2 swordSize,
        float swordRotation,
        Vector2 spearOffset,
        Vector2 spearSize,
        float spearRotation,
        Vector2 rangedOffset,
        Vector2 rangedSize,
        float rangedRotation)
    {
        _profileSwordWeaponOffset = swordOffset;
        _profileSwordWeaponSize = ClampPreviewSize(swordSize, new Vector2(112f, 146f));
        _profileSwordWeaponRotation = swordRotation;
        _profileSpearWeaponOffset = spearOffset;
        _profileSpearWeaponSize = ClampPreviewSize(spearSize, new Vector2(92f, 178f));
        _profileSpearWeaponRotation = spearRotation;
        _profileRangedWeaponOffset = rangedOffset;
        _profileRangedWeaponSize = ClampPreviewSize(rangedSize, new Vector2(74f, 74f));
        _profileRangedWeaponRotation = rangedRotation;
    }

    public void RefreshNow()
    {
        BuildUiIfNeeded();
        if (!isActiveAndEnabled || _apiClient == null)
            return;

        if (_refreshInFlight && Time.realtimeSinceStartup - _refreshStartedAt < 12f)
            return;

        StartCoroutine(RefreshSummaryRoutine());
    }

    private void OnEnable()
    {
        StartRefreshLoop();
    }

    private void OnDisable()
    {
        if (_refreshRoutine != null)
        {
            StopCoroutine(_refreshRoutine);
            _refreshRoutine = null;
        }
    }

    private void BuildUiIfNeeded()
    {
        if (_built)
            return;

        _built = true;

        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = new Vector2(-28f, 0f);

        GameUiThemeRuntime.StylePanel(gameObject, new Color(0.07f, 0.10f, 0.14f, 0.96f), true);

        VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 16);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject header = CreateUIObject("Header", transform);
        AddLayout(header, -1f, 42f);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;

        _titleText = CreateText(header.transform, "Title", "Social", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = AddLayout(_titleText.gameObject, 220f, 38f);
        titleLayout.flexibleWidth = 1f;

        Button refreshButton = CreateButton("RefreshButton", header.transform, "Refresh", GameUiThemeRuntime.Current.secondaryButton, 16f);
        AddLayout(refreshButton.gameObject, 88f, 38f);
        refreshButton.onClick.AddListener(RefreshNow);

        GameObject tabs = CreateUIObject("Tabs", transform);
        AddLayout(tabs, -1f, 40f);
        HorizontalLayoutGroup tabsLayout = tabs.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 8f;
        tabsLayout.childAlignment = TextAnchor.MiddleCenter;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childForceExpandHeight = true;

        _friendsTabButton = CreateButton("FriendsTab", tabs.transform, "Friends", GameUiThemeRuntime.Current.primaryButton, 15f);
        _inboxTabButton = CreateButton("InboxTab", tabs.transform, "Inbox", GameUiThemeRuntime.Current.secondaryButton, 15f);
        _sentTabButton = CreateButton("SentTab", tabs.transform, "Sent", GameUiThemeRuntime.Current.secondaryButton, 15f);
        _inboxNotificationDot = CreateNotificationDot(_inboxTabButton.transform);
        AddFlexibleButtonLayout(_friendsTabButton.gameObject);
        AddFlexibleButtonLayout(_inboxTabButton.gameObject);
        AddFlexibleButtonLayout(_sentTabButton.gameObject);
        _friendsTabButton.onClick.AddListener(() => SetTab(SocialTab.Friends));
        _inboxTabButton.onClick.AddListener(() => SetTab(SocialTab.Inbox));
        _sentTabButton.onClick.AddListener(() => SetTab(SocialTab.Sent));

        _sendFriendRow = CreateUIObject("SendFriendRow", transform);
        AddLayout(_sendFriendRow, -1f, 42f);
        HorizontalLayoutGroup sendLayout = _sendFriendRow.AddComponent<HorizontalLayoutGroup>();
        sendLayout.spacing = 8f;
        sendLayout.childAlignment = TextAnchor.MiddleCenter;
        sendLayout.childControlWidth = true;
        sendLayout.childControlHeight = true;
        sendLayout.childForceExpandWidth = false;
        sendLayout.childForceExpandHeight = true;

        _usernameInput = CreateInput(_sendFriendRow.transform, "Username");
        LayoutElement inputLayout = AddLayout(_usernameInput.gameObject, 230f, 40f);
        inputLayout.flexibleWidth = 1f;
        Button sendButton = CreateButton("SendFriendButton", _sendFriendRow.transform, "Send", GameUiThemeRuntime.Current.successButton, 15f);
        AddLayout(sendButton.gameObject, 78f, 40f);
        sendButton.onClick.AddListener(SendFriendRequest);

        _statusText = CreateText(transform, "StatusText", "", 15f, FontStyles.Normal, TextAlignmentOptions.Left);
        AddLayout(_statusText.gameObject, -1f, 28f);

        GameObject scroll = CreateScrollArea(transform);
        AddLayout(scroll, -1f, 340f, 1f);

        _friendActionsRow = CreateUIObject("FriendActions", transform);
        AddLayout(_friendActionsRow, -1f, 88f);
        VerticalLayoutGroup actionsLayout = _friendActionsRow.AddComponent<VerticalLayoutGroup>();
        actionsLayout.spacing = 8f;
        actionsLayout.childControlWidth = true;
        actionsLayout.childControlHeight = true;
        actionsLayout.childForceExpandWidth = true;
        actionsLayout.childForceExpandHeight = false;

        _selectedFriendText = CreateText(_friendActionsRow.transform, "SelectedFriendText", "", 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        AddLayout(_selectedFriendText.gameObject, -1f, 22f);

        GameObject actionButtons = CreateUIObject("FriendActionButtons", _friendActionsRow.transform);
        AddLayout(actionButtons, -1f, 42f);
        HorizontalLayoutGroup buttonLayout = actionButtons.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 8f;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = true;

        Button viewProfileButton = CreateButton("ViewProfileButton", actionButtons.transform, "Profile", GameUiThemeRuntime.Current.primaryButton, 14f);
        Button removeButton = CreateButton("RemoveFriendButton", actionButtons.transform, "Remove", GameUiThemeRuntime.Current.dangerButton, 14f);
        _inviteFriendButton = CreateButton("InviteFriendButton", actionButtons.transform, "Invite", GameUiThemeRuntime.Current.successButton, 14f);
        viewProfileButton.onClick.AddListener(OpenSelectedProfile);
        removeButton.onClick.AddListener(RemoveSelectedFriend);
        _inviteFriendButton.onClick.AddListener(InviteSelectedFriend);

        Render();
    }

    private GameObject CreateScrollArea(Transform parent)
    {
        GameObject scrollObj = CreateUIObject("ListScroll", parent);
        GameUiThemeRuntime.StylePanel(scrollObj, new Color(0.09f, 0.12f, 0.17f, 0.92f), true);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = CreateUIObject("Viewport", scrollObj.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchToParent(viewportRect, 8f, 8f, 8f, 8f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
        contentLayout.spacing = 7f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        _listContent = content.transform;
        return scrollObj;
    }

    private void StartRefreshLoop()
    {
        if (!isActiveAndEnabled || _apiClient == null)
            return;

        if (_refreshRoutine == null)
            _refreshRoutine = StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        yield return RefreshSummaryRoutine();
        while (true)
        {
            yield return new WaitForSeconds(RefreshIntervalSeconds);
            yield return RefreshSummaryRoutine();
        }
    }

    private IEnumerator RefreshSummaryRoutine()
    {
        if (_refreshInFlight && Time.realtimeSinceStartup - _refreshStartedAt < 12f)
            yield break;

        if (_apiClient == null)
            yield break;

        if (string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            _listMessage = "Log in to use friends.";
            SetStatus("Log in to use friends.");
            _summary = null;
            Render();
            yield break;
        }

        bool hasCachedSummary = _summary != null;
        string previousVisibleSignature = hasCachedSummary ? GetVisibleListSignature() : "";
        if (!hasCachedSummary)
        {
            _listMessage = "Loading social data...";
            SetStatus("Loading...");
            Render();
        }

        _refreshInFlight = true;
        _refreshStartedAt = Time.realtimeSinceStartup;
        SocialSummaryResponse response = null;
        string error = null;
        yield return RunSummaryRequestSafely(
            data => response = data,
            err => error = err);
        _refreshInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            _listMessage = hasCachedSummary ? "" : error;
            if (!hasCachedSummary)
                _summary = null;
            SetStatus(error);
            _summary = _summary ?? new SocialSummaryResponse();
            Render();
            yield break;
        }

        _summary = response ?? new SocialSummaryResponse();
        _listMessage = "";
        RestoreSelectedFriendAfterRefresh();
        SetStatus("");
        if (hasCachedSummary && string.Equals(previousVisibleSignature, GetVisibleListSignature(), StringComparison.Ordinal))
        {
            UpdateNonListUi();
            yield break;
        }

        Render();
    }

    private IEnumerator RunSummaryRequestSafely(Action<SocialSummaryResponse> onSuccess, Action<string> onError)
    {
        IEnumerator request = null;
        string startError = null;
        try
        {
            request = _apiClient.GetSocialSummary(onSuccess, onError);
        }
        catch (Exception ex)
        {
            startError = "Could not start social summary: " + ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(startError))
        {
            onError?.Invoke(startError);
            yield break;
        }

        while (true)
        {
            object current;
            bool movedNext;
            string moveError = null;
            try
            {
                movedNext = request.MoveNext();
                current = movedNext ? request.Current : null;
            }
            catch (Exception ex)
            {
                movedNext = false;
                current = null;
                moveError = "Could not load social summary: " + ex.Message;
            }

            if (!string.IsNullOrWhiteSpace(moveError))
            {
                onError?.Invoke(moveError);
                yield break;
            }

            if (!movedNext)
                yield break;

            yield return current;
        }
    }

    private void SetTab(SocialTab tab)
    {
        _activeTab = tab;
        Render();
    }

    private void Render()
    {
        if (!_built)
            return;

        UpdateContextText();
        UpdateTabButtonVisuals();
        UpdateInboxNotificationDot();
        ClearChildren(_listContent);

        bool hasSummary = _summary != null;
        _sendFriendRow.SetActive(_activeTab == SocialTab.Friends);
        _friendActionsRow.SetActive(_activeTab == SocialTab.Friends && _selectedFriend != null);

        if (!hasSummary)
        {
            AddInfoRow(string.IsNullOrWhiteSpace(_listMessage) ? "Loading social data..." : _listMessage);
            return;
        }

        if (_activeTab == SocialTab.Friends)
            RenderFriends();
        else if (_activeTab == SocialTab.Inbox)
            RenderInbox();
        else
            RenderSentRequests();
    }

    private void RenderFriends()
    {
        FriendSummaryResponse[] friends = GetFriends();
        if (friends.Length == 0)
        {
            AddInfoRow("No friends yet.");
            _friendActionsRow.SetActive(false);
            return;
        }

        for (int i = 0; i < friends.Length; i++)
        {
            FriendSummaryResponse friend = friends[i];
            if (friend == null)
                continue;

            AddFriendRow(friend);
        }

        UpdateFriendActionsState();
    }

    private void RenderInbox()
    {
        bool any = false;
        GameInviteResponse[] invites = GetGameInvites();
        for (int i = 0; i < invites.Length; i++)
        {
            if (!IsInviteActive(invites[i]))
                continue;

            AddInviteRow(invites[i]);
            any = true;
        }

        FriendRequestResponse[] requests = GetIncomingRequests();
        for (int i = 0; i < requests.Length; i++)
        {
            if (!IsPending(requests[i]?.status))
                continue;

            AddIncomingRequestRow(requests[i]);
            any = true;
        }

        if (!any)
            AddInfoRow("Inbox is empty.");
    }

    private void RenderSentRequests()
    {
        bool any = false;
        FriendRequestResponse[] requests = GetSentRequests();
        for (int i = 0; i < requests.Length; i++)
        {
            if (!IsPending(requests[i]?.status))
                continue;

            AddSentRequestRow(requests[i]);
            any = true;
        }

        if (!any)
            AddInfoRow("No sent requests.");
    }

    private void UpdateNonListUi()
    {
        if (!_built)
            return;

        UpdateContextText();
        UpdateTabButtonVisuals();
        UpdateInboxNotificationDot();
        if (_sendFriendRow != null)
            _sendFriendRow.SetActive(_activeTab == SocialTab.Friends);
        UpdateFriendActionsState();
    }

    private void UpdateFriendActionsState()
    {
        if (_friendActionsRow == null)
            return;

        if (_activeTab != SocialTab.Friends || _selectedFriend == null)
        {
            _friendActionsRow.SetActive(false);
            return;
        }

        if (_selectedFriendText != null)
            _selectedFriendText.text = "Selected: " + GetFriendName(_selectedFriend);
        if (_inviteFriendButton != null)
        {
            _inviteFriendButton.gameObject.SetActive(_allowLobbyInvites);
            _inviteFriendButton.interactable = _allowLobbyInvites && _roomNumber > 0 && !_actionInFlight;
        }
        _friendActionsRow.SetActive(true);
    }

    private string GetVisibleListSignature()
    {
        if (_summary == null)
            return "none|" + _activeTab + "|" + (_listMessage ?? "");

        StringBuilder sb = new StringBuilder();
        sb.Append(_activeTab).Append('|')
            .Append(_allowLobbyInvites).Append('|')
            .Append(_roomNumber).Append('|')
            .Append(_actionInFlight).Append('|')
            .Append(_selectedFriend != null ? GetFriendUserId(_selectedFriend) : 0);

        if (_activeTab == SocialTab.Friends)
        {
            FriendSummaryResponse[] friends = GetFriends();
            sb.Append("|friends:").Append(friends.Length);
            for (int i = 0; i < friends.Length; i++)
            {
                FriendSummaryResponse friend = friends[i];
                if (friend == null)
                    continue;

                sb.Append('|')
                    .Append(GetFriendUserId(friend)).Append(':')
                    .Append(GetFriendName(friend)).Append(':')
                    .Append(GetFriendLevel(friend));
            }
        }
        else if (_activeTab == SocialTab.Inbox)
        {
            AppendInviteSignature(sb, GetGameInvites());
            AppendRequestSignature(sb, "incoming", GetIncomingRequests(), true);
        }
        else
        {
            AppendRequestSignature(sb, "sent", GetSentRequests(), false);
        }

        return sb.ToString();
    }

    private static void AppendInviteSignature(StringBuilder sb, GameInviteResponse[] invites)
    {
        sb.Append("|invites:");
        if (invites == null)
            return;

        for (int i = 0; i < invites.Length; i++)
        {
            GameInviteResponse invite = invites[i];
            if (!IsInviteActive(invite))
                continue;

            sb.Append('|')
                .Append(GetInviteIdentity(invite)).Append(':')
                .Append(GetInviteRemainingLabel(invite));
        }
    }

    private static void AppendRequestSignature(StringBuilder sb, string label, FriendRequestResponse[] requests, bool incoming)
    {
        sb.Append('|').Append(label).Append(':');
        if (requests == null)
            return;

        for (int i = 0; i < requests.Length; i++)
        {
            FriendRequestResponse request = requests[i];
            if (!IsPending(request?.status))
                continue;

            sb.Append('|')
                .Append(GetRequestId(request)).Append(':')
                .Append(incoming ? GetRequesterName(request) : GetRecipientName(request)).Append(':')
                .Append(request.status ?? "");
        }
    }

    private void AddFriendRow(FriendSummaryResponse friend)
    {
        bool selected = _selectedFriend != null && GetFriendUserId(_selectedFriend) == GetFriendUserId(friend);
        GameObject row = CreateRow("FriendRow", selected ? new Color(0.13f, 0.28f, 0.34f, 1f) : GameUiThemeRuntime.Current.surface, 50f);
        Button rowButton = row.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(rowButton, row.GetComponent<Image>(), row.GetComponent<Image>().color);
        rowButton.onClick.AddListener(() =>
        {
            _selectedFriend = friend;
            Render();
        });

        string label = GetFriendName(friend);
        int level = GetFriendLevel(friend);
        if (level > 0)
            label += "  Lv. " + level;

        TextMeshProUGUI nameText = CreateText(row.transform, "Name", label, 16f, FontStyles.Bold, TextAlignmentOptions.Left);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        if (_allowLobbyInvites)
        {
            Button inviteButton = CreateButton("InviteButton", row.transform, "Invite", GameUiThemeRuntime.Current.successButton, 13f);
            AddLayout(inviteButton.gameObject, 70f, 34f);
            inviteButton.interactable = _roomNumber > 0 && !_actionInFlight;
            inviteButton.onClick.AddListener(() =>
            {
                _selectedFriend = friend;
                InviteSelectedFriend();
            });
        }
    }

    private void AddIncomingRequestRow(FriendRequestResponse request)
    {
        GameObject row = CreateRow("FriendRequestRow", GameUiThemeRuntime.Current.surface, 64f);
        TextMeshProUGUI label = CreateText(row.transform, "Label", "Friend request from " + GetRequesterName(request), 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button accept = CreateButton("AcceptButton", row.transform, "Accept", GameUiThemeRuntime.Current.successButton, 13f);
        AddLayout(accept.gameObject, 68f, 34f);
        accept.onClick.AddListener(() => AcceptFriendRequest(request));

        Button decline = CreateButton("DeclineButton", row.transform, "Decline", GameUiThemeRuntime.Current.dangerButton, 13f);
        AddLayout(decline.gameObject, 72f, 34f);
        decline.onClick.AddListener(() => DeclineFriendRequest(request));
    }

    private void AddSentRequestRow(FriendRequestResponse request)
    {
        GameObject row = CreateRow("SentRequestRow", GameUiThemeRuntime.Current.surface, 56f);
        TextMeshProUGUI label = CreateText(row.transform, "Label", "Pending: " + GetRecipientName(request), 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button cancel = CreateButton("CancelButton", row.transform, "Cancel", GameUiThemeRuntime.Current.dangerButton, 13f);
        AddLayout(cancel.gameObject, 74f, 34f);
        cancel.onClick.AddListener(() => CancelFriendRequest(request));
    }

    private void AddInviteRow(GameInviteResponse invite)
    {
        GameObject row = CreateRow("GameInviteRow", new Color(0.12f, 0.20f, 0.30f, 1f), 74f);
        string text = "Game invite from " + GetInviteHostName(invite) + "\nRoom #" + GetInviteRoomNumber(invite) + "  " + GetInviteRemainingLabel(invite);
        TextMeshProUGUI label = CreateText(row.transform, "Label", text, 14f, FontStyles.Bold, TextAlignmentOptions.Left);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button accept = CreateButton("AcceptInviteButton", row.transform, "Join", GameUiThemeRuntime.Current.successButton, 13f);
        AddLayout(accept.gameObject, 62f, 34f);
        accept.onClick.AddListener(() => AcceptLobbyInvite(invite));

        Button decline = CreateButton("DeclineInviteButton", row.transform, "Decline", GameUiThemeRuntime.Current.dangerButton, 13f);
        AddLayout(decline.gameObject, 72f, 34f);
        decline.onClick.AddListener(() => DeclineLobbyInvite(invite));
    }

    private void AddInfoRow(string message)
    {
        GameObject row = CreateRow("InfoRow", new Color(0.09f, 0.12f, 0.17f, 0.75f), 54f);
        TextMeshProUGUI label = CreateText(row.transform, "Label", message, 15f, FontStyles.Normal, TextAlignmentOptions.Center);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private GameObject CreateRow(string name, Color color, float height)
    {
        GameObject row = CreateUIObject(name, _listContent);
        AddLayout(row, -1f, height);
        Image image = row.AddComponent<Image>();
        image.color = color;
        GameUiThemeRuntime.ApplyBorder(row);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 7, 7);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return row;
    }

    private void SendFriendRequest()
    {
        if (_actionInFlight || _apiClient == null)
            return;

        string username = _usernameInput != null ? (_usernameInput.text ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Enter a username.");
            return;
        }

        StartCoroutine(SendFriendRequestRoutine(username));
    }

    private IEnumerator SendFriendRequestRoutine(string username)
    {
        _actionInFlight = true;
        SetStatus("Sending request...");
        SocialActionResponse response = null;
        string error = null;
        yield return _apiClient.SendFriendRequest(username, data => response = data, err => error = err);
        _actionInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            Render();
            yield break;
        }

        if (_usernameInput != null)
            _usernameInput.text = "";
        ApplySocialActionResponse(response, ResponseMessage(response, "Friend request sent."));
    }

    private void AcceptFriendRequest(FriendRequestResponse request)
    {
        int requestId = GetRequestId(request);
        if (_actionInFlight || _apiClient == null || requestId <= 0)
            return;

        StartCoroutine(FriendRequestActionRoutine(
            callback => _apiClient.AcceptFriendRequest(requestId, callback, SetActionError),
            "Friend request accepted."));
    }

    private void DeclineFriendRequest(FriendRequestResponse request)
    {
        int requestId = GetRequestId(request);
        if (_actionInFlight || _apiClient == null || requestId <= 0)
            return;

        StartCoroutine(FriendRequestActionRoutine(
            callback => _apiClient.DeclineFriendRequest(requestId, callback, SetActionError),
            "Friend request declined."));
    }

    private void CancelFriendRequest(FriendRequestResponse request)
    {
        int requestId = GetRequestId(request);
        if (_actionInFlight || _apiClient == null || requestId <= 0)
            return;

        StartCoroutine(FriendRequestActionRoutine(
            callback => _apiClient.CancelFriendRequest(requestId, callback, SetActionError),
            "Friend request canceled."));
    }

    private IEnumerator FriendRequestActionRoutine(Func<Action<SocialActionResponse>, IEnumerator> createRequest, string successMessage)
    {
        _actionInFlight = true;
        SetStatus("Updating...");
        SocialActionResponse response = null;
        _lastActionError = null;
        yield return createRequest(data => response = data);
        _actionInFlight = false;

        if (!string.IsNullOrWhiteSpace(_lastActionError))
        {
            SetStatus(_lastActionError);
            Render();
            yield break;
        }

        ApplySocialActionResponse(response, ResponseMessage(response, successMessage));
    }

    private string _lastActionError;

    private void SetActionError(string error)
    {
        _lastActionError = error;
    }

    private void RemoveSelectedFriend()
    {
        if (_selectedFriend == null || _actionInFlight || _apiClient == null)
            return;

        int friendUserId = GetFriendUserId(_selectedFriend);
        if (friendUserId <= 0)
        {
            SetStatus("Friend id missing.");
            return;
        }

        StartCoroutine(RemoveSelectedFriendRoutine(friendUserId));
    }

    private IEnumerator RemoveSelectedFriendRoutine(int friendUserId)
    {
        _actionInFlight = true;
        SetStatus("Removing friend...");
        SocialActionResponse response = null;
        string error = null;
        yield return _apiClient.RemoveFriend(friendUserId, data => response = data, err => error = err);
        _actionInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            Render();
            yield break;
        }

        RemoveCachedFriend(friendUserId);
        _selectedFriend = null;
        ApplySocialActionResponse(response, ResponseMessage(response, "Friend removed."));
        StartCoroutine(RefreshSummaryQuietlyRoutine());
    }

    private IEnumerator RefreshSummaryQuietlyRoutine()
    {
        if (_refreshInFlight || _apiClient == null || string.IsNullOrWhiteSpace(AuthSession.AccessToken))
            yield break;

        bool hasCachedSummary = _summary != null;
        string previousVisibleSignature = hasCachedSummary ? GetVisibleListSignature() : "";
        _refreshInFlight = true;
        _refreshStartedAt = Time.realtimeSinceStartup;
        SocialSummaryResponse response = null;
        string error = null;
        yield return RunSummaryRequestSafely(
            data => response = data,
            err => error = err);
        _refreshInFlight = false;

        if (!string.IsNullOrWhiteSpace(error) || response == null)
            yield break;

        _summary = response;
        RestoreSelectedFriendAfterRefresh();
        if (hasCachedSummary && string.Equals(previousVisibleSignature, GetVisibleListSignature(), StringComparison.Ordinal))
        {
            UpdateNonListUi();
            yield break;
        }

        Render();
    }

    private void InviteSelectedFriend()
    {
        if (_selectedFriend == null || _actionInFlight || _apiClient == null)
            return;

        if (!_allowLobbyInvites || _roomNumber <= 0)
        {
            SetStatus("Invites are only available while hosting a lobby.");
            return;
        }

        StartCoroutine(InviteSelectedFriendRoutine(GetFriendName(_selectedFriend)));
    }

    private IEnumerator InviteSelectedFriendRoutine(string username)
    {
        _actionInFlight = true;
        SetStatus("Sending invite...");
        SocialActionResponse response = null;
        string error = null;
        yield return _apiClient.SendLobbyInvite(username, _roomNumber, data => response = data, err => error = err);
        _actionInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            Render();
            yield break;
        }

        ApplySocialActionResponse(response, ResponseMessage(response, "Invite sent."));
    }

    private void AcceptLobbyInvite(GameInviteResponse invite)
    {
        int inviteId = GetInviteId(invite);
        if (_actionInFlight || _apiClient == null || inviteId <= 0)
            return;

        StartCoroutine(AcceptLobbyInviteRoutine(invite));
    }

    private IEnumerator AcceptLobbyInviteRoutine(GameInviteResponse invite)
    {
        _actionInFlight = true;
        SetStatus("Joining invite...");
        GameInviteActionResponse response = null;
        string error = null;
        yield return _apiClient.AcceptLobbyInvite(GetInviteId(invite), data => response = data, err => error = err);
        _actionInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            Render();
            yield break;
        }

        if (response != null && response.summary != null)
            _summary = response.summary;

        GameInviteResponse acceptedInvite = response != null
            ? (response.invite ?? response.gameInvite)
            : null;
        int room = response != null && response.roomNumber > 0
            ? response.roomNumber
            : acceptedInvite != null && GetInviteRoomNumber(acceptedInvite) > 0
                ? GetInviteRoomNumber(acceptedInvite)
                : GetInviteRoomNumber(invite);

        if (room <= 0)
        {
            SetStatus("Invite accepted, but no room was returned.");
            Render();
            yield break;
        }

        SetStatus("Joining room #" + room + "...");
        _menuController?.JoinInvitedOnlineLobby(room);
    }

    private void DeclineLobbyInvite(GameInviteResponse invite)
    {
        int inviteId = GetInviteId(invite);
        if (_actionInFlight || _apiClient == null || inviteId <= 0)
            return;

        StartCoroutine(DeclineLobbyInviteRoutine(inviteId));
    }

    private IEnumerator DeclineLobbyInviteRoutine(int inviteId)
    {
        _actionInFlight = true;
        SetStatus("Declining invite...");
        SocialActionResponse response = null;
        string error = null;
        yield return _apiClient.DeclineLobbyInvite(inviteId, data => response = data, err => error = err);
        _actionInFlight = false;

        if (!string.IsNullOrWhiteSpace(error))
        {
            SetStatus(error);
            Render();
            yield break;
        }

        ApplySocialActionResponse(response, ResponseMessage(response, "Invite declined."));
    }

    private void ApplySocialActionResponse(SocialActionResponse response, string fallbackMessage)
    {
        if (response != null && response.summary != null)
        {
            _summary = response.summary;
            RestoreSelectedFriendAfterRefresh();
        }
        else
        {
            RefreshNow();
        }

        SetStatus(fallbackMessage);
        Render();
    }

    private string ResponseMessage(SocialActionResponse response, string fallback)
    {
        return response != null && !string.IsNullOrWhiteSpace(response.result)
            ? response.result
            : fallback;
    }

    private void OpenSelectedProfile()
    {
        if (_selectedFriend == null || _apiClient == null)
            return;

        ShowProfileOverlay();
        StartCoroutine(LoadProfileRoutine(_selectedFriend));
    }

    private IEnumerator LoadProfileRoutine(FriendSummaryResponse friend)
    {
        SetProfileLoading(friend);
        UserProfileSummaryResponse profile = null;
        string error = null;
        int userId = GetFriendUserId(friend);
        if (userId > 0)
        {
            yield return _apiClient.GetProfileByUserId(userId, data => profile = data, err => error = err);
        }
        else
        {
            yield return _apiClient.GetProfileByUsername(GetFriendName(friend), data => profile = data, err => error = err);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            _profileStatusText.text = error;
            yield break;
        }

        RenderProfile(profile ?? new UserProfileSummaryResponse { username = GetFriendName(friend), level = GetFriendLevel(friend) });
    }

    private void ShowProfileOverlay()
    {
        if (_profileOverlay == null)
            BuildProfileOverlay();

        _profileOverlay.SetActive(true);
        _profileOverlay.transform.SetAsLastSibling();
    }

    private void BuildProfileOverlay()
    {
        Transform parent = transform.parent != null ? transform.parent : transform;
        _profileOverlay = CreateUIObject("ProfileOverlay", parent);
        RectTransform overlayRect = _profileOverlay.GetComponent<RectTransform>();
        StretchToParent(overlayRect, 0f, 0f, 0f, 0f);

        Image dim = _profileOverlay.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.48f);
        dim.raycastTarget = true;

        GameObject card = CreateUIObject("ProfileCard", _profileOverlay.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(700f, 390f);
        cardRect.anchoredPosition = Vector2.zero;
        GameUiThemeRuntime.StylePanel(card, new Color(0.07f, 0.10f, 0.14f, 0.98f), true);

        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(22, 20, 16, 18);
        cardLayout.spacing = 8f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        GameObject header = CreateUIObject("Header", card.transform);
        AddLayout(header, -1f, 38f);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;

        _profileNameText = CreateText(header.transform, "Name", "Profile", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement nameLayout = AddLayout(_profileNameText.gameObject, 410f, 34f);
        nameLayout.flexibleWidth = 1f;
        _profileLevelText = CreateText(header.transform, "Level", "", 17f, FontStyles.Bold, TextAlignmentOptions.Right);
        AddLayout(_profileLevelText.gameObject, 0f, 32f);
        _profileLevelText.gameObject.SetActive(false);
        Button close = CreateButton("CloseButton", header.transform, "Close", GameUiThemeRuntime.Current.secondaryButton, 14f);
        AddLayout(close.gameObject, 78f, 32f);
        close.onClick.AddListener(() => _profileOverlay.SetActive(false));

        GameObject body = CreateUIObject("Body", card.transform);
        AddLayout(body, -1f, -1f, 1f);
        HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 14f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        GameObject info = CreateUIObject("Info", body.transform);
        AddLayout(info, 330f, -1f, 1f);
        VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 6f;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        _profileStatsText = CreateText(info.transform, "Stats", "", 16f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        AddLayout(_profileStatsText.gameObject, -1f, 116f);
        _profileLoadoutText = CreateText(info.transform, "Loadout", "", 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        AddLayout(_profileLoadoutText.gameObject, -1f, 92f);
        _profileStatusText = CreateText(info.transform, "Status", "", 14f, FontStyles.Normal, TextAlignmentOptions.Left);
        AddLayout(_profileStatusText.gameObject, -1f, 28f);

        GameObject preview = CreateUIObject("Preview", body.transform);
        AddLayout(preview, 280f, -1f, 1f);
        GameUiThemeRuntime.StylePanel(preview, new Color(0.10f, 0.13f, 0.18f, 0.95f), true);
        _profilePreviewRoot = preview.GetComponent<RectTransform>();

        _profileOverlay.SetActive(false);
    }

    private void SetProfileLoading(FriendSummaryResponse friend)
    {
        ClearChildren(_profilePreviewRoot);
        int level = GetFriendLevel(friend);
        _profileNameText.text = level > 0 ? GetFriendName(friend) + "  Lv. " + level : GetFriendName(friend);
        _profileLevelText.text = "";
        _profileStatsText.text = "Loading stats...";
        _profileLoadoutText.text = "";
        _profileStatusText.text = "";
    }

    private void RenderProfile(UserProfileSummaryResponse profile)
    {
        string username = !string.IsNullOrWhiteSpace(profile.displayName) ? profile.displayName : profile.username;
        if (string.IsNullOrWhiteSpace(username))
            username = "Player";

        SocialStatsSummary stats = GetProfileStats(profile);
        SocialLoadoutSummary loadout = GetProfileLoadout(profile);
        int level = profile.level > 0 ? profile.level : stats != null ? stats.level : 0;
        _profileNameText.text = level > 0 ? username + "  Lv. " + level : username;
        _profileLevelText.text = "";
        _profileStatsText.text = BuildStatsText(stats);
        _profileLoadoutText.text = BuildLoadoutText(loadout);
        _profileStatusText.text = "";
        RenderProfilePreview(profile);
    }

    private void RenderProfilePreview(UserProfileSummaryResponse profile)
    {
        ClearChildren(_profilePreviewRoot);

        SocialLoadoutSummary loadout = GetProfileLoadout(profile);
        int skinId = GetSkinId(loadout);
        Color skinColor = ParseColor(loadout != null ? loadout.skinColor : "", new Color(0.25f, 0.65f, 0.95f, 1f));
        Color weaponColor = ParseColor(loadout != null ? loadout.weaponColor : "", new Color(0.90f, 0.86f, 0.70f, 1f));
        WeaponKind weaponKind = ResolveProfileWeaponKind(loadout);

        GameObject body = CreateUIObject("CharacterBody", _profilePreviewRoot);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(150f, 190f);
        bodyRect.anchoredPosition = new Vector2(-38f, -8f);
        Image bodyImage = body.AddComponent<Image>();
        bodyImage.color = skinColor;
        bodyImage.preserveAspect = true;

        Sprite skinSprite = SkinVisualDatabase.GetSpriteSetOrDefault(skinId).PreviewOrFirstSprite;
        if (skinSprite != null)
        {
            bodyImage.sprite = skinSprite;
            bodyImage.color = Color.white;
        }

        if (weaponKind == WeaponKind.Ranged)
        {
            RenderProfileRangedOrb(weaponColor);
            return;
        }

        GameObject weapon = CreateUIObject("Weapon", _profilePreviewRoot);
        RectTransform weaponRect = weapon.GetComponent<RectTransform>();
        weaponRect.anchorMin = new Vector2(0.5f, 0.5f);
        weaponRect.anchorMax = new Vector2(0.5f, 0.5f);
        weaponRect.pivot = new Vector2(0.5f, 0.5f);
        bool spear = weaponKind == WeaponKind.Spear;
        weaponRect.sizeDelta = spear ? _profileSpearWeaponSize : _profileSwordWeaponSize;
        weaponRect.anchoredPosition = spear ? _profileSpearWeaponOffset : _profileSwordWeaponOffset;
        weaponRect.localRotation = Quaternion.Euler(0f, 0f, spear ? _profileSpearWeaponRotation : _profileSwordWeaponRotation);
        Image weaponImage = weapon.AddComponent<Image>();
        weaponImage.color = weaponColor;
        weaponImage.preserveAspect = true;

        Sprite weaponSprite = ResolveWeaponSprite(loadout, weaponKind);
        if (weaponSprite != null)
        {
            weaponImage.sprite = weaponSprite;
            weaponImage.color = Color.white;
        }
    }

    private void RenderProfileRangedOrb(Color weaponColor)
    {
        GameObject orb = CreateUIObject("RangedOrb", _profilePreviewRoot);
        RectTransform orbRect = orb.GetComponent<RectTransform>();
        orbRect.anchorMin = new Vector2(0.5f, 0.5f);
        orbRect.anchorMax = new Vector2(0.5f, 0.5f);
        orbRect.pivot = new Vector2(0.5f, 0.5f);
        orbRect.sizeDelta = _profileRangedWeaponSize;
        orbRect.anchoredPosition = _profileRangedWeaponOffset;
        orbRect.localRotation = Quaternion.Euler(0f, 0f, _profileRangedWeaponRotation);

        Image orbImage = orb.AddComponent<Image>();
        orbImage.sprite = SimpleSprite.Circle;
        orbImage.color = weaponColor;
        orbImage.preserveAspect = true;

        Outline outline = orb.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.28f);
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private string BuildStatsText(SocialStatsSummary stats)
    {
        if (stats == null)
            return "STATS\nNo games played yet.";

        int games = stats.gamesPlayed > 0 ? stats.gamesPlayed : stats.runsPlayed > 0 ? stats.runsPlayed : stats.matchesPlayed;
        int wins = stats.wins > 0 ? stats.wins : stats.gamesWon;
        int kills = stats.totalKills > 0 ? stats.totalKills : stats.kills > 0 ? stats.kills : stats.enemiesKilled;
        int giantKills = stats.giantKills > 0 ? stats.giantKills : stats.bossKills;
        float bestTime = stats.bestTimeSeconds > 0f ? stats.bestTimeSeconds : stats.bestRunTimeSeconds > 0f ? stats.bestRunTimeSeconds : stats.fastestWinSeconds;
        var sb = new StringBuilder();
        sb.AppendLine("STATS");
        bool hasStats = false;
        hasStats |= AppendStat(sb, "Games", games);
        hasStats |= AppendStat(sb, "Wins", wins);
        hasStats |= AppendStat(sb, "Kills", kills);
        hasStats |= AppendStat(sb, "Giant kills", giantKills);
        hasStats |= AppendStat(sb, "Revives", stats.revives);
        if (bestTime > 0f)
        {
            sb.AppendLine("Best time: " + FormatSeconds(bestTime));
            hasStats = true;
        }
        if (stats.totalTimeSeconds > 0f)
        {
            sb.AppendLine("Total time: " + FormatSeconds(stats.totalTimeSeconds));
            hasStats = true;
        }

        if (!hasStats)
            sb.Append("No games played yet.");

        return sb.ToString().TrimEnd();
    }

    private string BuildLoadoutText(SocialLoadoutSummary loadout)
    {
        if (loadout == null)
            return "Loadout unavailable.";

        string weapon = FirstNonEmpty(loadout.weaponName, loadout.weaponItemName, loadout.weaponType);
        var sb = new StringBuilder();
        sb.AppendLine("LOADOUT");
        sb.AppendLine("Weapon: " + (string.IsNullOrWhiteSpace(weapon) ? "None" : weapon));
        sb.AppendLine("Armor: " + FirstNonEmpty(loadout.armorName, loadout.armorItemName, "None"));
        sb.Append("Item: " + FirstNonEmpty(loadout.consumableName, loadout.itemName, "None"));
        return sb.ToString();
    }

    private Sprite ResolveWeaponSprite(SocialLoadoutSummary loadout, WeaponKind weaponKind)
    {
        if (loadout == null)
            return null;

        int weaponItemId = GetWeaponItemId(loadout);
        WeaponVisualEntry visual;

        if (weaponKind == WeaponKind.Ranged)
            return null;

        if (weaponKind == WeaponKind.Spear
            && WeaponVisualDatabase.TryGetSpearVisualGlobal(weaponItemId, out visual))
            return visual.ResolveSpearSprite();

        if (weaponKind == WeaponKind.Sword
            && WeaponVisualDatabase.TryGetSwordVisualGlobal(weaponItemId, out visual))
            return visual.ResolveSwordSwingSprite();

        if (WeaponVisualDatabase.TryGetSwordVisualGlobal(weaponItemId, out visual))
            return visual.ResolveSwordSwingSprite();

        if (WeaponVisualDatabase.TryGetSpearVisualGlobal(weaponItemId, out visual))
            return visual.ResolveSpearSprite();

        return null;
    }

    private static WeaponKind ResolveProfileWeaponKind(SocialLoadoutSummary loadout)
    {
        if (loadout == null)
            return WeaponKind.Spear;

        string type = (loadout.weaponType ?? "") + " " + (loadout.weaponName ?? "") + " " + (loadout.weaponItemName ?? "");
        return PlayerLoadout.ParseWeaponKind(type);
    }

    private static SocialStatsSummary GetProfileStats(UserProfileSummaryResponse profile)
    {
        if (profile == null)
            return null;
        return profile.stats ?? profile.playerStats;
    }

    private static SocialLoadoutSummary GetProfileLoadout(UserProfileSummaryResponse profile)
    {
        if (profile == null)
            return null;
        if (profile.loadout != null)
            return profile.loadout;
        if (profile.equippedLoadout != null)
            return profile.equippedLoadout;
        return profile.equipped;
    }

    private static int GetSkinId(SocialLoadoutSummary loadout)
    {
        if (loadout == null)
            return 0;
        return loadout.skinId > 0 ? loadout.skinId : loadout.equippedSkinId;
    }

    private static int GetWeaponItemId(SocialLoadoutSummary loadout)
    {
        if (loadout == null)
            return 0;
        if (loadout.weaponItemId > 0)
            return loadout.weaponItemId;
        if (loadout.equippedWeaponItemId > 0)
            return loadout.equippedWeaponItemId;
        return loadout.weaponId;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        if (values == null)
            return "";

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return "";
    }

    private static bool AppendStat(StringBuilder sb, string label, int value)
    {
        if (value <= 0)
            return false;

        sb.AppendLine(label + ": " + value);
        return true;
    }

    private static Vector2 ClampPreviewSize(Vector2 value, Vector2 fallback)
    {
        float x = Mathf.Approximately(value.x, 0f) ? fallback.x : Mathf.Abs(value.x);
        float y = Mathf.Approximately(value.y, 0f) ? fallback.y : Mathf.Abs(value.y);
        return new Vector2(Mathf.Max(1f, x), Mathf.Max(1f, y));
    }

    private static string FormatSeconds(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return minutes > 0 ? minutes + "m " + secs + "s" : secs + "s";
    }

    private void RestoreSelectedFriendAfterRefresh()
    {
        if (_selectedFriend == null || _summary == null)
            return;

        FriendSummaryResponse[] friends = GetFriends();
        int selectedId = GetFriendUserId(_selectedFriend);
        string selectedName = GetFriendName(_selectedFriend);
        for (int i = 0; i < friends.Length; i++)
        {
            FriendSummaryResponse friend = friends[i];
            if (friend == null)
                continue;

            if ((selectedId > 0 && GetFriendUserId(friend) == selectedId)
                || string.Equals(GetFriendName(friend), selectedName, StringComparison.OrdinalIgnoreCase))
            {
                _selectedFriend = friend;
                return;
            }
        }

        _selectedFriend = null;
    }

    private FriendSummaryResponse[] GetFriends()
    {
        if (_summary == null)
            return new FriendSummaryResponse[0];
        return FirstNonEmptyArray(_summary.friends, _summary.friendList, _summary.friendships);
    }

    private void RemoveCachedFriend(int friendUserId)
    {
        if (_summary == null || friendUserId <= 0)
            return;

        _summary.friends = RemoveCachedFriend(_summary.friends, friendUserId);
        _summary.friendList = RemoveCachedFriend(_summary.friendList, friendUserId);
        _summary.friendships = RemoveCachedFriend(_summary.friendships, friendUserId);
    }

    private static FriendSummaryResponse[] RemoveCachedFriend(FriendSummaryResponse[] friends, int friendUserId)
    {
        if (friends == null || friendUserId <= 0)
            return friends;

        List<FriendSummaryResponse> remaining = null;
        for (int i = 0; i < friends.Length; i++)
        {
            FriendSummaryResponse friend = friends[i];
            if (friend != null && GetFriendUserId(friend) == friendUserId)
            {
                if (remaining == null)
                {
                    remaining = new List<FriendSummaryResponse>(friends.Length);
                    for (int j = 0; j < i; j++)
                        remaining.Add(friends[j]);
                }
                continue;
            }

            if (remaining != null)
                remaining.Add(friend);
        }

        return remaining != null ? remaining.ToArray() : friends;
    }

    private FriendRequestResponse[] GetIncomingRequests()
    {
        if (_summary == null)
            return new FriendRequestResponse[0];
        return FirstNonEmptyArray(
            _summary.incomingFriendRequests,
            _summary.incomingRequests,
            _summary.receivedFriendRequests,
            _summary.friendRequests);
    }

    private FriendRequestResponse[] GetSentRequests()
    {
        if (_summary == null)
            return new FriendRequestResponse[0];
        return FirstNonEmptyArray(
            _summary.sentFriendRequests,
            _summary.sentRequests,
            _summary.outgoingFriendRequests);
    }

    private static T[] FirstNonEmptyArray<T>(params T[][] groups)
    {
        T[] fallback = null;
        for (int i = 0; i < groups.Length; i++)
        {
            T[] group = groups[i];
            if (group == null)
                continue;
            if (fallback == null)
                fallback = group;
            if (group.Length > 0)
                return group;
        }

        return fallback ?? new T[0];
    }

    private GameInviteResponse[] GetGameInvites()
    {
        if (_summary == null)
            return new GameInviteResponse[0];

        return MergeGameInviteArrays(
            _summary.gameInvites,
            _summary.invites,
            _summary.pendingGameInvites,
            _summary.lobbyInvites);
    }

    private static GameInviteResponse[] MergeGameInviteArrays(params GameInviteResponse[][] groups)
    {
        if (groups == null)
            return new GameInviteResponse[0];

        List<GameInviteResponse> merged = new List<GameInviteResponse>();
        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < groups.Length; i++)
        {
            GameInviteResponse[] group = groups[i];
            if (group == null)
                continue;

            for (int j = 0; j < group.Length; j++)
            {
                GameInviteResponse invite = group[j];
                if (invite == null)
                    continue;

                string key = GetInviteIdentity(invite);
                if (seen.Add(key))
                    merged.Add(invite);
            }
        }

        return merged.ToArray();
    }

    private static string GetInviteIdentity(GameInviteResponse invite)
    {
        int id = GetInviteId(invite);
        if (id > 0)
            return "id:" + id;

        return string.Join("|",
            GetInviteHostName(invite),
            GetInviteRoomNumber(invite).ToString(CultureInfo.InvariantCulture),
            invite != null ? (invite.createdAt ?? "") : "",
            invite != null ? (invite.expiresAt ?? "") : "");
    }

    private void UpdateContextText()
    {
        if (_titleText == null)
            return;

        _titleText.text = _allowLobbyInvites && _roomNumber > 0
            ? "Social  Room #" + _roomNumber
            : "Social";
    }

    private void UpdateTabButtonVisuals()
    {
        StyleTabButton(_friendsTabButton, _activeTab == SocialTab.Friends);
        StyleTabButton(_inboxTabButton, _activeTab == SocialTab.Inbox);
        StyleTabButton(_sentTabButton, _activeTab == SocialTab.Sent);
    }

    private void UpdateInboxNotificationDot()
    {
        if (_inboxNotificationDot != null)
            _inboxNotificationDot.SetActive(HasInboxItems());
    }

    private bool HasInboxItems()
    {
        GameInviteResponse[] invites = GetGameInvites();
        for (int i = 0; i < invites.Length; i++)
        {
            if (IsInviteActive(invites[i]))
                return true;
        }

        FriendRequestResponse[] requests = GetIncomingRequests();
        for (int i = 0; i < requests.Length; i++)
        {
            if (IsPending(requests[i]?.status))
                return true;
        }

        return false;
    }

    private static GameObject CreateNotificationDot(Transform parent)
    {
        GameObject dot = new GameObject("NotificationDot");
        dot.transform.SetParent(parent, false);

        RectTransform rect = dot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(11f, 11f);
        rect.anchoredPosition = new Vector2(-10f, -8f);

        Image image = dot.AddComponent<Image>();
        image.sprite = SimpleSprite.Circle;
        image.color = new Color(1f, 0.12f, 0.08f, 1f);
        image.raycastTarget = false;
        dot.SetActive(false);
        return dot;
    }

    private static void StyleTabButton(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        GameUiThemeRuntime.StyleButton(
            button,
            image,
            selected ? GameUiThemeRuntime.Current.primaryButton : GameUiThemeRuntime.Current.secondaryButton);
    }

    private void SetStatus(string message)
    {
        if (_statusText == null)
            return;

        _statusText.text = message ?? "";
        _statusText.color = string.IsNullOrWhiteSpace(message)
            ? GameUiThemeRuntime.Current.MutedText(0.7f)
            : GameUiThemeRuntime.Current.text;
    }

    private static int GetFriendUserId(FriendSummaryResponse friend)
    {
        if (friend == null)
            return 0;
        if (friend.userId > 0)
            return friend.userId;
        if (friend.friendUserId > 0)
            return friend.friendUserId;
        if (friend.friendId > 0)
            return friend.friendId;
        return friend.id;
    }

    private static string GetFriendName(FriendSummaryResponse friend)
    {
        if (friend == null)
            return "";
        if (!string.IsNullOrWhiteSpace(friend.displayName))
            return friend.displayName;
        if (!string.IsNullOrWhiteSpace(friend.username))
            return friend.username;
        if (!string.IsNullOrWhiteSpace(friend.friendUsername))
            return friend.friendUsername;
        return friend.name ?? "";
    }

    private static int GetFriendLevel(FriendSummaryResponse friend)
    {
        return friend != null ? friend.level : 0;
    }

    private static int GetRequestId(FriendRequestResponse request)
    {
        if (request == null)
            return 0;
        if (request.requestId > 0)
            return request.requestId;
        if (request.friendRequestId > 0)
            return request.friendRequestId;
        return request.id;
    }

    private static int GetInviteId(GameInviteResponse invite)
    {
        if (invite == null)
            return 0;
        if (invite.inviteId > 0)
            return invite.inviteId;
        if (invite.gameInviteId > 0)
            return invite.gameInviteId;
        return invite.id;
    }

    private static string GetRequesterName(FriendRequestResponse request)
    {
        if (request == null)
            return "";
        if (!string.IsNullOrWhiteSpace(request.requesterUsername))
            return request.requesterUsername;
        if (!string.IsNullOrWhiteSpace(request.senderUsername))
            return request.senderUsername;
        if (!string.IsNullOrWhiteSpace(request.fromUsername))
            return request.fromUsername;
        return !string.IsNullOrWhiteSpace(request.requester) ? request.requester : "Unknown";
    }

    private static string GetRecipientName(FriendRequestResponse request)
    {
        if (request == null)
            return "";
        if (!string.IsNullOrWhiteSpace(request.recipientUsername))
            return request.recipientUsername;
        if (!string.IsNullOrWhiteSpace(request.toUsername))
            return request.toUsername;
        return !string.IsNullOrWhiteSpace(request.recipient) ? request.recipient : "Unknown";
    }

    private static string GetInviteHostName(GameInviteResponse invite)
    {
        if (invite == null)
            return "";
        if (!string.IsNullOrWhiteSpace(invite.hostUsername))
            return invite.hostUsername;
        if (!string.IsNullOrWhiteSpace(invite.senderUsername))
            return invite.senderUsername;
        if (!string.IsNullOrWhiteSpace(invite.fromUsername))
            return invite.fromUsername;
        return !string.IsNullOrWhiteSpace(invite.host) ? invite.host : "Host";
    }

    private static int GetInviteRoomNumber(GameInviteResponse invite)
    {
        if (invite == null)
            return 0;
        return invite.roomNumber > 0 ? invite.roomNumber : invite.room;
    }

    private static bool IsPending(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            || string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInviteActive(GameInviteResponse invite)
    {
        if (invite == null || !IsPending(invite.status))
            return false;

        DateTime expires;
        if (TryParseUtc(invite.expiresAt, out expires))
            return DateTime.UtcNow < expires;

        return true;
    }

    private static string GetInviteRemainingLabel(GameInviteResponse invite)
    {
        DateTime expires;
        if (!TryParseUtc(invite != null ? invite.expiresAt : "", out expires))
            return "Expires soon";

        TimeSpan remaining = expires - DateTime.UtcNow;
        if (remaining.TotalSeconds <= 0)
            return "Expired";

        if (remaining.TotalMinutes >= 1)
            return "Expires in " + Mathf.CeilToInt((float)remaining.TotalMinutes) + "m";

        return "Expires in " + Mathf.CeilToInt((float)remaining.TotalSeconds) + "s";
    }

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        utc = default(DateTime);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        if (HasExplicitTimezone(trimmed))
        {
            DateTimeOffset offsetTime;
            if (DateTimeOffset.TryParse(
                    trimmed,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out offsetTime))
            {
                utc = offsetTime.UtcDateTime;
                return true;
            }
        }

        DateTime localTime;
        if (DateTime.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out localTime))
        {
            utc = localTime.ToUniversalTime();
            return true;
        }

        return false;
    }

    private static bool HasExplicitTimezone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            return true;

        int timeStart = value.IndexOf('T');
        if (timeStart < 0)
            timeStart = value.IndexOf(' ');
        timeStart = timeStart >= 0 ? timeStart + 1 : 0;

        return value.IndexOf('+', timeStart) >= 0 || value.IndexOf('-', timeStart) >= 0;
    }

    private static Color ParseColor(string value, Color fallback)
    {
        Color color;
        return !string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out color)
            ? color
            : fallback;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = GameUiThemeRuntime.Current.text;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, float fontSize)
    {
        GameObject obj = CreateUIObject(name, parent);
        Image image = obj.AddComponent<Image>();
        Button button = obj.AddComponent<Button>();
        GameUiThemeRuntime.StyleButton(button, image, color);

        TextMeshProUGUI text = CreateText(obj.transform, "Label", label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchToParent(text.GetComponent<RectTransform>(), 4f, 2f, 4f, 2f);
        text.raycastTarget = false;
        return button;
    }

    private static TMP_InputField CreateInput(Transform parent, string placeholder)
    {
        GameObject obj = CreateUIObject("Input", parent);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.11f, 1f);
        GameUiThemeRuntime.ApplyBorder(obj);

        TMP_InputField input = obj.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.characterLimit = 32;
        input.lineType = TMP_InputField.LineType.SingleLine;

        TextMeshProUGUI text = CreateText(obj.transform, "Text", "", 16f, FontStyles.Normal, TextAlignmentOptions.Left);
        StretchToParent(text.GetComponent<RectTransform>(), 12f, 6f, 12f, 6f);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI hint = CreateText(obj.transform, "Placeholder", placeholder, 16f, FontStyles.Normal, TextAlignmentOptions.Left);
        StretchToParent(hint.GetComponent<RectTransform>(), 12f, 6f, 12f, 6f);
        hint.color = GameUiThemeRuntime.Current.MutedText(0.45f);
        hint.enableWordWrapping = false;
        hint.overflowMode = TextOverflowModes.Ellipsis;

        input.textComponent = text;
        input.placeholder = hint;
        return input;
    }

    private static LayoutElement AddLayout(GameObject obj, float preferredWidth, float preferredHeight, float flexibleHeight = 0f)
    {
        LayoutElement layout = obj.GetComponent<LayoutElement>();
        if (layout == null)
            layout = obj.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f)
            layout.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f)
            layout.preferredHeight = preferredHeight;
        layout.flexibleHeight = flexibleHeight;
        if (flexibleHeight > 0f)
            layout.flexibleWidth = 1f;
        return layout;
    }

    private static void AddFlexibleButtonLayout(GameObject obj)
    {
        LayoutElement layout = AddLayout(obj, 0f, 38f);
        layout.flexibleWidth = 1f;
    }

    private static void StretchToParent(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }
}
