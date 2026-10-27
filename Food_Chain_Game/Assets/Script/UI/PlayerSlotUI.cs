using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSlotUI : MonoBehaviour
{
    public static PlayerSlotUI Instance;

    public Transform slotContainer;
    public GameObject playerSlotPrefab;

    private readonly List<PlayerSlot> slots = new();

    private void Awake()
    {
        Instance = this;
    }

    public void CreateSlots(List<GamePlayer> players)
    {
        ClearAll();

        if (players == null) return;

        GamePlayer localPlayer = null;
        if (NetworkClient.localPlayer != null)
            localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();

        // 로컬플레이어 슬롯 생성 후
        if (localPlayer != null && players.Contains(localPlayer))
        {
            CreateSlot(localPlayer);
        }
        foreach (var player in players) // 나머지 슬롯 생성
        {
            if (player == null || player == localPlayer) continue;
            CreateSlot(player);
        }
    }

    private void CreateSlot(GamePlayer player)
    {
        if (playerSlotPrefab == null || slotContainer == null) return;

        GameObject obj = Instantiate(playerSlotPrefab, slotContainer);
        PlayerSlot slot = obj.GetComponent<PlayerSlot>();
        if (slot == null) return;

        slot.Setup(player);
        slots.Add(slot);
    }

    public PlayerSlot GetSlotByPlayer(GamePlayer player)
    {
        if (player == null) return null;

        return slots.Find(slot => slot != null && slot.linkedPlayer == player);
    }

    public void ClearAll()
    {
        foreach (var slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slots.Clear();
    }
}
