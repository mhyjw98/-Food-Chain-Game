using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CharacterData;

public class PlayerSlot : MonoBehaviour, IPointerClickHandler
{
    public static PlayerSlot Instance;

    public Image profileImage;
    public TextMeshProUGUI nicknameText;
    public TMP_InputField memoInput;
    public TextMeshProUGUI memoText;
    public TextMeshProUGUI predictText;

    public GamePlayer linkedPlayer;

    public GameObject contextMenuPrefab;
    private GameObject contextMenuInstance;

    public RectTransform point;
    public bool isEditingMemo = false;
    void Start()
    {
        StartCoroutine(DeActiveInputField());
    }

    IEnumerator DeActiveInputField()
    {
        yield return new WaitForSeconds(0.5f);
        memoInput.gameObject.SetActive(false);
    }
    public void Setup(GamePlayer player)
    {
        if (player == null) return;

        // 내 슬롯에만 동물 이름을 표시한다(다른 플레이어의 역할은 클라이언트가 모른다)
        bool isMine = NetworkClient.localPlayer != null && player.netIdentity == NetworkClient.localPlayer;
        string character = "???";
        if (isMine && AnimalNameMap.AnimalTypeToName.TryGetValue(player.animalType, out string animalName))
            character = animalName;

        linkedPlayer = player;
        nicknameText.text = $"{player.nickname} ({character})";
        memoText.text = player.memo;
        // profileImage.sprite = ...;
    }
    public void BeginEditMemo()
    {
        // 메모 대상이 게임에서 나갔으면 편집하지 않는다
        if (isEditingMemo || linkedPlayer == null) return;

        isEditingMemo = true;
        memoText.gameObject.SetActive(false);
        memoInput.gameObject.SetActive(true);
        memoInput.text = linkedPlayer.memo;
        memoInput.ActivateInputField();

        memoInput.onEndEdit.AddListener(EndEditMemo);
    }
    private void EndEditMemo(string newMemo)
    {
        isEditingMemo = false;
        if (linkedPlayer != null)
            linkedPlayer.memo = newMemo;

        memoText.text = newMemo;
        memoText.gameObject.SetActive(true);
        memoInput.gameObject.SetActive(false);

        memoInput.onEndEdit.RemoveAllListeners();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (linkedPlayer == null || contextMenuPrefab == null) return;

            if (contextMenuInstance != null) Destroy(contextMenuInstance);
            contextMenuInstance = Instantiate(contextMenuPrefab, point);

            var menu = contextMenuInstance.GetComponent<ContextMenuUI>();
            if (menu != null) menu.Setup(linkedPlayer);
        }
    }
    public void UpdateNicknameWithAnimal(string updateText)
    {
        if (linkedPlayer == null) return;

        nicknameText.text = $"{linkedPlayer.nickname} ({updateText})";
    }
    public void MarkPrediction(uint targetId)
    {
        predictText.text = "예 측";
    }
}
public static class AnimalNameMap
{
    public static readonly Dictionary<AnimalType, string> AnimalTypeToName = new()
    {
        { AnimalType.Wolf, "늑대" },
        { AnimalType.Crocodile, "악어" },
        { AnimalType.Chameleon, "카멜레온" },
        { AnimalType.Meerkat, "미어캣" },
        { AnimalType.Zebra, "얼룩말" },
        { AnimalType.Hippo, "하마" },
        { AnimalType.Scorpion, "전갈" },
        { AnimalType.Ostrich, "타조" },
        { AnimalType.Hawk, "매" },
        { AnimalType.Plover, "악어새" },
        { AnimalType.Owl, "올빼미" },
        { AnimalType.Crow, "까마귀" },
        { AnimalType.Hyena, "하이에나" },
        { AnimalType.None, "???" }
    };
}