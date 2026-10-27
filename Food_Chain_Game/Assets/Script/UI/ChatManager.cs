using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ChatChannelType
{
    All,
    Whisper
}

public class ChatChannel
{
    public ChatChannelType type;
    public GamePlayer targetPlayer;
    public string label;
    public List<string> messages = new();
    public bool hasUnreadMessages = false;
    public GameObject tabObject;
}
public class ChatManager : NetworkBehaviour
{
    public Transform messageContainer;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    public Transform tabContainer;
    public GameObject tabPrefab;

    public TMP_InputField chatInputField;

    [Tooltip("채널 하나에 보관·표시하는 최대 메시지 수. 넘으면 오래된 것부터 지운다")]
    [SerializeField] private int maxMessagesPerChannel = 100;
    [SerializeField] private string allTabLabel = "전체";
    [SerializeField] private string whisperTargetGoneNotice = "귓속말 상대가 게임에 없습니다.";
    [SerializeField] private string whisperAliveDeadNotice = "산 사람과 유령은 귓속말을 주고받을 수 없습니다.";

    public ChatChannel currentChannel;
    private readonly ChatChannel allChannel = new ChatChannel { type = ChatChannelType.All };
    private Dictionary<uint, ChatChannel> whisperChannels = new();
    private ChatInputFocus focusHandler;

    private readonly List<GameObject> spawnedMessages = new();

    // 플레이어가 입력한 글(채팅, 닉네임)에 든 <size>, <color> 같은 태그가 그대로 적용되지 않게 한다
    private static readonly Regex NoParseTag = new Regex(@"<\s*/?\s*noparse[^>]*>", RegexOptions.IgnoreCase);

    private ChatChannel ActiveChannel => currentChannel ?? allChannel;

    private void Start()
    {
        allChannel.label = allTabLabel;
        if (currentChannel == null)
            currentChannel = allChannel;

        // 인스펙터에 연결돼 있으면 그대로 쓰고, 비어 있을 때만 이름으로 찾는다
        if (messageContainer == null)
        {
            var containerObj = GameObject.Find("MessageContainer");
            if (containerObj != null) messageContainer = containerObj.transform;
        }
        if (scrollRect == null)
        {
            var scrollObj = GameObject.Find("Chat Scroll View");
            if (scrollObj != null) scrollRect = scrollObj.GetComponent<ScrollRect>();
        }
        if (chatInputField == null)
        {
            var inputObj = GameObject.Find("Chat InputField");
            if (inputObj != null) chatInputField = inputObj.GetComponent<TMP_InputField>();
        }

        if (messageContainer == null || scrollRect == null || chatInputField == null)
            Debug.LogWarning("[ChatManager] 채팅 UI(MessageContainer / Chat Scroll View / Chat InputField)를 찾지 못했습니다.");

        if (chatInputField != null)
        {
            focusHandler = chatInputField.gameObject.GetComponent<ChatInputFocus>();
            EventTrigger trigger = chatInputField.gameObject.GetComponent<EventTrigger>();

            if (focusHandler != null && trigger != null)
            {
                trigger.triggers ??= new List<EventTrigger.Entry>();

                EventTrigger.Entry selectEntry = new EventTrigger.Entry();
                selectEntry.eventID = EventTriggerType.Select;
                selectEntry.callback.AddListener((_) => focusHandler.OnInputSelected());
                trigger.triggers.Add(selectEntry);

                EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
                deselectEntry.eventID = EventTriggerType.Deselect;
                deselectEntry.callback.AddListener((_) => focusHandler.OnInputDeselected());
                trigger.triggers.Add(deselectEntry);
            }
            else
            {
                Debug.LogWarning("[ChatManager] Chat InputField에 ChatInputFocus 또는 EventTrigger가 없습니다.");
            }
        }

        if (tabContainer != null && tabPrefab != null)
            CreateTab(allTabLabel, allChannel);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Return))
            return;

        if (chatInputField == null || EventSystem.current == null)
            return;

        if (!ChatInputFocus.IsFocused)
        {
            // 다른 입력 칸(메모 등)에서 누른 엔터는 채팅 포커스로 가져오지 않는다
            if (IsOtherInputFieldSelected()) return;
            // 채팅창이 꺼져 있으면 포커스를 줄 수 없다
            if (!chatInputField.gameObject.activeInHierarchy) return;

            EventSystem.current.SetSelectedGameObject(chatInputField.gameObject);

            chatInputField.ActivateInputField();
            chatInputField.MoveTextEnd(false);
            return;
        }
        string message = chatInputField.text;
        bool hasText = !string.IsNullOrWhiteSpace(message);

        if (!hasText)
        {
            EventSystem.current.SetSelectedGameObject(null);
            chatInputField.DeactivateInputField();
        }
        else
        {
            SendChat(message);

            chatInputField.text = string.Empty;
            chatInputField.ActivateInputField();
            chatInputField.MoveTextEnd(false);
        }
    }

    private bool IsOtherInputFieldSelected()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || selected == chatInputField.gameObject) return false;

        return selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<InputField>() != null;
    }

    public void SendChat(string msg)
    {
        if (NetworkClient.connection == null || NetworkClient.connection.identity == null)
            return;

        var identity = NetworkClient.connection.identity;

        var gamePlayer = identity.GetComponent<GamePlayer>();
        var roomPlayer = identity.GetComponent<RoomPlayer>();

        if (gamePlayer != null)
        {
            if (currentChannel != null && currentChannel.type == ChatChannelType.Whisper)
            {
                // 상대가 게임에서 나갔으면 보내지 않는다(전체 채팅으로 새어 나가면 안 된다)
                if (currentChannel.targetPlayer == null)
                {
                    AppendMessage(currentChannel, $"<color=#888888>{Sanitize(whisperTargetGoneNotice)}</color>");
                    return;
                }

                // 서버도 막지만(CmdSendWhisper), 보낸 글이 말없이 사라지지 않도록 여기서 먼저 알려 준다
                if (currentChannel.targetPlayer.isAlive != gamePlayer.isAlive)
                {
                    AppendMessage(currentChannel, $"<color=#888888>{Sanitize(whisperAliveDeadNotice)}</color>");
                    return;
                }

                gamePlayer.CmdSendWhisper(currentChannel.targetPlayer.netId, msg);
            }
            else
            {
                gamePlayer.CmdSendChatMessage(msg);
            }
            return;
        }

        if (roomPlayer != null)
        {
            roomPlayer.CmdSendChatMessage(msg);
            return;
        }

        Debug.LogWarning("[ChatManager] GamePlayer/RoomPlayer를 찾을 수 없습니다.");
    }

    private static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return "<noparse>" + NoParseTag.Replace(text, string.Empty) + "</noparse>";
    }

    // 메시지 하나를 채널에 추가한다. 보고 있는 채널이면 그 한 줄만 화면에 붙인다
    // (예전에는 메시지가 올 때마다 전체를 지우고 다시 만들어서, 대화가 길어질수록 느려졌다)
    private void AppendMessage(ChatChannel channel, string formatted)
    {
        if (channel == null) return;

        int max = Mathf.Max(1, maxMessagesPerChannel);

        channel.messages.Add(formatted);
        while (channel.messages.Count > max)
            channel.messages.RemoveAt(0);

        if (channel != ActiveChannel)
        {
            channel.hasUnreadMessages = true;
            UpdateTabVisual(channel);
            return;
        }

        while (spawnedMessages.Count >= max)
        {
            if (spawnedMessages[0] != null) Destroy(spawnedMessages[0]);
            spawnedMessages.RemoveAt(0);
        }

        SpawnMessageObject(formatted);
        ScrollToBottom();
    }

    private void SpawnMessageObject(string formatted)
    {
        if (messagePrefab == null || messageContainer == null) return;

        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        var text = msgObj.GetComponent<TextMeshProUGUI>();
        if (text != null) text.text = formatted;
        spawnedMessages.Add(msgObj);
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // 채널(탭)을 바꿨을 때만 전체를 다시 그린다
    private void RefreshChatView()
    {
        if (messageContainer == null) return;

        foreach (Transform child in messageContainer)
            Destroy(child.gameObject);

        spawnedMessages.Clear();

        foreach (var msg in ActiveChannel.messages)
            SpawnMessageObject(msg);

        ScrollToBottom();
    }

    private void CreateTab(string label, ChatChannel channel)
    {
        channel.label = label;

        if (tabPrefab == null || tabContainer == null) return;

        GameObject tab = Instantiate(tabPrefab, tabContainer);
        channel.tabObject = tab;
        UpdateTabVisual(channel);

        var button = tab.GetComponent<Button>();
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            currentChannel = channel;
            channel.hasUnreadMessages = false;
            UpdateTabVisual(channel);
            RefreshChatView();
        });
    }
    private void UpdateTabVisual(ChatChannel channel)
    {
        if (channel == null || channel.tabObject == null) return;

        var label = channel.tabObject.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;

        string baseLabel = channel.type == ChatChannelType.All ? allTabLabel : Sanitize(channel.label);

        label.text = channel.hasUnreadMessages
            ? $"<b><color=yellow>{baseLabel}</color></b>"
            : baseLabel;
    }
    public void SelectWhisperTarget(GamePlayer target)
    {
        if (target == null) return;

        uint key = target.netId;
        if (!whisperChannels.TryGetValue(key, out ChatChannel channel))
        {
            channel = new ChatChannel { type = ChatChannelType.Whisper, targetPlayer = target };
            whisperChannels[key] = channel;
            CreateTab(target.nickname, channel);
        }

        currentChannel = channel;
        channel.hasUnreadMessages = false;
        UpdateTabVisual(channel);
        RefreshChatView();
    }
    public void AddSystemMessage(string sender, string message)
    {
        string msg = sender == "System" ? Sanitize(message) : $"{Sanitize(sender)}: {Sanitize(message)}";
        AppendMessage(allChannel, $"<color=#AAAAAA>{msg}</color>");
    }

    public void AddLocalNotice(string message)
    {
        AppendMessage(allChannel, $"<color=#888888>{Sanitize(message)}</color>");
    }

    // speakerNetId: 말한 사람, partnerNetId: 이 화면에서 귓속말 탭의 상대(내가 보냈으면 받는 사람, 받았으면 보낸 사람)
    public void AddWhisperMessage(uint speakerNetId, uint partnerNetId, string message)
    {
        if (!NetworkClient.spawned.TryGetValue(partnerNetId, out NetworkIdentity partnerIdentity) || partnerIdentity == null)
            return;

        GamePlayer partner = partnerIdentity.GetComponent<GamePlayer>();
        if (partner == null) return;

        string speakerName = "???";
        if (NetworkClient.spawned.TryGetValue(speakerNetId, out NetworkIdentity speakerIdentity) && speakerIdentity != null)
        {
            GamePlayer speaker = speakerIdentity.GetComponent<GamePlayer>();
            if (speaker != null) speakerName = speaker.nickname;
        }

        if (!whisperChannels.TryGetValue(partnerNetId, out ChatChannel channel))
        {
            channel = new ChatChannel
            {
                type = ChatChannelType.Whisper,
                targetPlayer = partner
            };
            whisperChannels[partnerNetId] = channel;
            CreateTab(partner.nickname, channel);
        }

        AppendMessage(channel, $"<color=#888888>{Sanitize(speakerName)} : {Sanitize(message)}</color>");
    }
}
