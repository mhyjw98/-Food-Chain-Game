using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionListUI : MonoBehaviour
{
    public static MissionListUI Instance { get; private set; }

    [SerializeField] private Transform contentRoot;
    [SerializeField] private MissionListItemUI itemPrefab;

    private readonly List<MissionListItemUI> _items = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RefreshList(IReadOnlyList<MissionSlot> missions)
    {
        foreach (var item in _items)
            Destroy(item.gameObject);
        _items.Clear();

        foreach (var slot in missions)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.SetData(slot.Type, slot.Status);
            _items.Add(item);
        }
    }
}
