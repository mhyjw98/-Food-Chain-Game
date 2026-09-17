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

    private GamePlayer localPlayer;
   
    public Vector2 movement;
    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        localPlayer = GetComponent<GamePlayer>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        
        Move();
        Interaction();
    }
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        rigid.MovePosition(rigid.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void Move()
    {
        if (!isLocalPlayer) return;
        if (!UIManager.Instance.CanMove()) return;
        if (ChatInputFocus.IsFocused || (GameMamager.Instance != null && !GameMamager.Instance.isRoundActive))
        {
            movement = Vector2.zero;
            return;
        }

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
    private void Interaction()
    {
        if (!UIManager.Instance.CanInteract()) return;

        if (Input.GetKeyDown(KeySetting.keys[KeyAction.INTERACT]))
        {
            if (localPlayer.scanner == null) return;

            var type = localPlayer.scanner.CurrentType;            

            if (type == Scanner.ScanTargetType.Player)
            {
                uint targetNetId = localPlayer.scanner.CurrentTargetNetId;
                if (targetNetId != 0)
                {
                    localPlayer.CmdAttack(targetNetId);
                    return;
                }
            }
            else if (type == Scanner.ScanTargetType.Corpse)
            {
                var corpse = localPlayer.scanner.CurrentTargetCorpse;
                if (corpse != null)
                {
                    localPlayer.CmdScanCorpse(corpse.netId);
                }
            }
            else if (type == Scanner.ScanTargetType.Mission)
            {
                var mission = localPlayer.scanner.CurrentMission;
                if (mission != null && localPlayer.CanStartMissionType(mission.MissionType))
                {
                    localPlayer.TryStartMission(mission.MissionType);
                    return;
                }
            }
            else if (type == Scanner.ScanTargetType.Investigation)
            {
                var interact = localPlayer.scanner.CurrentInvestigation;
                if (interact != null)
                {
                    localPlayer.TryStartInvestigation();
                }
            }                                                                               
        }
    }


    public void StopMove()
    {
        movement = Vector2.zero;
    }
}
