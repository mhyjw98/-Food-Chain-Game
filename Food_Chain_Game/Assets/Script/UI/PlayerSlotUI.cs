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

        foreach (var player in players)
        {
            GameObject obj = Instantiate(playerSlotPrefab, slotContainer);
            PlayerSlot slot = obj.GetComponent<PlayerSlot>();
            slot.Setup(player);
            slots.Add(slot);
        }
    }

    public PlayerSlot GetSlotByPlayer(GamePlayer player)
    {
        return slots.Find(slot => slot.linkedPlayer == player);
    }

    public void ClearAll()
    {
        foreach (var slot in slots)
        {
            Destroy(slot.gameObject);
        }
        slots.Clear();
    }
}
