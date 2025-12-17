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

    public static bool isStop;
    public static bool isEvent;
    private GamePlayer localPlayer;
   
    public Vector2 movement;
    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        localPlayer = GetComponent<GamePlayer>();
        
        isStop = false;
        isEvent = false;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        Attack();
        Move();
    }
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

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
        if (Input.GetKey(KeySetting.keys[KeyAction.INTERACT]))
        {
            if (!TryGetComponent(out GamePlayer gp)) return;
            if (gp.scanner == null) return;

            uint targetNetId = gp.scanner.CurrentTargetNetId;

            gp.CmdAttack(targetNetId);
        }
    }  
    
    public void StopMove()
    {
        isEvent = true;
        movement = Vector2.zero;
    }
}
