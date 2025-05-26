using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public ChatChannel currentChannel;
    private ChatChannel allChannel;
    private Dictionary<uint, ChatChannel> whisperChannels = new();

    private readonly List<GameObject> spawnedMessages = new();


    private void Start()
    {
        if (messageContainer == null || scrollRect == null)
        {
            messageContainer = GameObject.Find("MessageContainer").transform;
            scrollRect = GameObject.Find("Chat Scroll View").GetComponent<ScrollRect>();
        }
        allChannel = new ChatChannel { type = ChatChannelType.All };
        currentChannel = allChannel;

        chatInputField = GameObject.Find("Chat InputField").GetComponent<TMP_InputField>();
        if (tabContainer != null && tabPrefab != null)
            CreateTab("전체", allChannel);
    }
    //public void AddMessage(GamePlayer sender, string message, bool isWhisper)
    //{
    //    uint key = sender.netId;
    //    string formatted = isWhisper
    //        ? $"<color=#888888>{key}: {message}</color>"
    //        : $"<b>{key}:</b> {message}";

    //    if (isWhisper)
    //    {
    //        if (!whisperChannels.ContainsKey(key))
    //        {
    //            ChatChannel newWhisper = new ChatChannel { type = ChatChannelType.Whisper, targetPlayer = sender };
    //            whisperChannels[key] = newWhisper;
    //            CreateTab(sender.nickname, newWhisper);
    //        }
    //        whisperChannels[key].messages.Add(formatted);

    //        if (currentChannel != whisperChannels[key])
    //        {
    //            whisperChannels[key].hasUnreadMessages = true;
    //            UpdateTabVisual(whisperChannels[key]);
    //        }
    //    }
    //    else
    //    {
    //        allChannel.messages.Add(formatted);

    //        if (currentChannel != allChannel)
    //        {
    //            allChannel.hasUnreadMessages = true;
    //            UpdateTabVisual(allChannel);
    //        }
    //    }

    //    if (currentChannel != null)
    //        RefreshChatView();
    //}
    private void RefreshChatView()
    {
        foreach (Transform child in messageContainer)
            Destroy(child.gameObject);

        spawnedMessages.Clear();

        var channel = currentChannel ?? allChannel;
        foreach (var msg in channel.messages)
        {
            GameObject msgObj = Instantiate(messagePrefab, messageContainer);
            msgObj.GetComponent<TextMeshProUGUI>().text = msg;
            spawnedMessages.Add(msgObj);
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void CreateTab(string label, ChatChannel channel)
    {
        GameObject tab = Instantiate(tabPrefab, tabContainer);
        tab.GetComponentInChildren<TextMeshProUGUI>().text = label;
        channel.tabObject = tab;

        tab.GetComponent<Button>().onClick.AddListener(() =>
        {
            currentChannel = channel;
            channel.hasUnreadMessages = false;
            UpdateTabVisual(channel);
            RefreshChatView();
        });
    }
    private void UpdateTabVisual(ChatChannel channel)
    {
        if (channel.tabObject == null) return;

        var label = channel.tabObject.GetComponentInChildren<TextMeshProUGUI>();
        if (channel.hasUnreadMessages)
        {
            label.text = $"<b><color=yellow>{label.text}</color></b>";
        }
        else
        {
            if (channel.type == ChatChannelType.All)
                label.text = "전체";
            else if (channel.targetPlayer != null)
                label.text = channel.targetPlayer.nickname;
        }
    }
    public void SelectWhisperTarget(GamePlayer target)
    {
        uint key = target.netId;
        if (!whisperChannels.ContainsKey(key))
        {
            ChatChannel newChannel = new ChatChannel { type = ChatChannelType.Whisper, targetPlayer = target };
            whisperChannels[key] = newChannel;
            CreateTab(target.nickname, newChannel);
        }

        currentChannel = whisperChannels[key];
        RefreshChatView();
    }
    public void AddSystemMessage(string sender, string message)
    {
        string msg = sender == "System" ? message : $"{sender}: {message}";
        string formatted = $"<color=#AAAAAA>{msg}</color>";
        allChannel.messages.Add(formatted);

        if (currentChannel != allChannel)
        {
            allChannel.hasUnreadMessages = true;
            UpdateTabVisual(allChannel);
        }

        if (currentChannel == allChannel)
        {
            RefreshChatView();
        }
    }

    public void AddLocalNotice(string message)
    {
        string formatted = $"<color=#888888>{message}</color>";
        allChannel.messages.Add(formatted);

        if (currentChannel == allChannel)
            RefreshChatView();
    }

    public void AddWhisperMessage(uint senderNetId, uint targetNetId, string message)
    {
        if (NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        {
            if(NetworkClient.spawned.TryGetValue(senderNetId, out NetworkIdentity localidentity))
            {
                GamePlayer target = identity.GetComponent<GamePlayer>();
                GamePlayer player = localidentity.GetComponent<GamePlayer>();

                string formatted = $"<color=#888888>{player.nickname} : {message}</color>";

                if (!whisperChannels.ContainsKey(targetNetId))
                {
                    ChatChannel newChannel = new ChatChannel
                    {
                        type = ChatChannelType.Whisper,
                        targetPlayer = target
                    };
                    whisperChannels[targetNetId] = newChannel;
                    CreateTab(target.nickname, newChannel);
                }
                whisperChannels[targetNetId].messages.Add(formatted);

                if (currentChannel != whisperChannels[targetNetId])
                {
                    whisperChannels[targetNetId].hasUnreadMessages = true;
                    UpdateTabVisual(whisperChannels[targetNetId]);
                }

                if (currentChannel == whisperChannels[targetNetId])
                {
                    RefreshChatView();
                }
            }            
        }                
    }
}

