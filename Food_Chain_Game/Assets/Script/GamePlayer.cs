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
        Debug.Log($"[GamePlayer] 내 캐릭터는 {characterName}");
        GameMamager.Instance.ShowCharacter(characterName);
    }
}
