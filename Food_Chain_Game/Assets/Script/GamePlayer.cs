using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar]
    public string characterName;

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[GamePlayer] �� ĳ���ʹ� {characterName}");
        GameMamager.Instance.ShowCharacter(characterName);
    }
}
