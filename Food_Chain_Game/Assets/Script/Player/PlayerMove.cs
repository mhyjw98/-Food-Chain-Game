using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove : NetworkBehaviour
{
    [SyncVar]public float moveSpeed = 5f;
    private Rigidbody2D rigid;

    public static bool isStop = false;
    public static bool isEvent = false;
    private GamePlayer localPlayer;

    public TMP_InputField chatInputField;
    public Vector2 movement;
    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        localPlayer = GetComponent<GamePlayer>();
        chatInputField = FindObjectOfType<TMP_InputField>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        Attack();
        Move();
        Whisper();
    }
    void FixedUpdate()
    {
        rigid.MovePosition(rigid.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void Move()
    {
        if (!isLocalPlayer) return;
        if (isStop) return;
        if (isEvent) return;
        if (GameMamager.Instance != null)
        {
            if (!GameMamager.Instance.isRoundActive)
            {
                movement = Vector2.zero;
                return;
            }               
        }
        if (SettingManager.isKeySetting == true) return;

        movement = Vector2.zero;

        if (Input.GetKey(KeySetting.keys[KeyAction.UP]))
        {
            movement.y = 1;
        }
        if (Input.GetKey(KeySetting.keys[KeyAction.DOWN]))
        {
            movement.y = -1;
        }
        if (Input.GetKey(KeySetting.keys[KeyAction.LEFT]))
        {
            movement.x = -1;
        }
        if (Input.GetKey(KeySetting.keys[KeyAction.RIGHT]))
        {
            movement.x = 1;
        }

        movement = movement.normalized;
    }
    private void Attack()
    {
        if (Input.GetKey(KeySetting.keys[KeyAction.ATTACK]))
        {
            if (!TryGetComponent(out GamePlayer gp)) return;
            if (gp.scanner == null) return;

            uint targetNetId = gp.scanner.CurrentTargetNetId;

            gp.CmdAttack(targetNetId);
        }
    }   
    
    private void Whisper()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string msg = chatInputField.text.Trim();

            if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                ChatManager chatManager = FindObjectOfType<ChatManager>();

                if (chatManager.currentChannel.type == ChatChannelType.Whisper && chatManager.currentChannel.targetPlayer != null)
                {
                    localPlayer.CmdSendWhisper(chatManager.currentChannel.targetPlayer.netId, msg);
                }
                else
                {
                    localPlayer.CmdSendChatMessage(msg);
                }
            }

            chatInputField.text = "";
        }
    }
}
