using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance;

    public Transform messageContainer;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    private readonly List<GameObject> spawnedMessages = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if(messageContainer == null || scrollRect == null)
        {
            messageContainer = GameObject.Find("MessageContainer").transform;
            scrollRect = GameObject.Find("Chat Scroll View").GetComponent<ScrollRect>();
        }
    }

    [ClientRpc]
    public void RpcReceiveMessage(string sender, string message)
    {
        if (!messageContainer || !messagePrefab || !scrollRect)
        {
            Debug.LogError("ChatManager: ������Ʈ�� �������� �ʾҽ��ϴ�.");
            return;
        }

        string formatted;
        if (sender == "System")
            formatted = $"<color=#AAAAAA>{message}</color>";
        else
            formatted = $"<b>{sender}:</b> {message}";

        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        msgObj.GetComponent<TextMeshProUGUI>().text = formatted;
        spawnedMessages.Add(msgObj);

        if (spawnedMessages.Count > 30)
        {
            Destroy(spawnedMessages[0]);
            spawnedMessages.RemoveAt(0);
        }

        // ��ũ�� �Ʒ��� �ڵ� �̵�
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
