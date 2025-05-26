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
        string character = player == NetworkClient.localPlayer.GetComponent<GamePlayer>() ? AnimalNameMap.AnimalTypeToName[player.animalType] : "???";

        linkedPlayer = player;
        nicknameText.text = $"{player.nickname} ({character})";
        memoText.text = player.memo;
        // profileImage.sprite = ...;
    }
    public void BeginEditMemo()
    {
        if (isEditingMemo) return;

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
            if (contextMenuInstance != null) Destroy(contextMenuInstance);
            contextMenuInstance = Instantiate(contextMenuPrefab, point);
            contextMenuInstance.GetComponent<ContextMenuUI>().Setup(linkedPlayer);
        }
    }
    public void UpdateNicknameWithAnimal(string updateText)
    {
        nicknameText.text = $"{linkedPlayer.nickname} ({updateText})";
    }
    public void MarkPrediction(uint targetId)
    {
        predictText.text = "¿¹ Ãø";
    }
}
public static class AnimalNameMap
{
    public static readonly Dictionary<AnimalType, string> AnimalTypeToName = new()
    {
        { AnimalType.Lion, "»çÀÚ" },
        { AnimalType.Crocodile, "¾Ç¾î" },
        { AnimalType.Mouse, "Áã" },
        { AnimalType.Rabbit, "Åä³¢" },
        { AnimalType.Deer, "»ç½¿" },
        { AnimalType.Otter, "¼ö´Þ" },
        { AnimalType.Snake, "¹ì" },
        { AnimalType.Mallard, "Ã»µÕ¿À¸®" },
        { AnimalType.Eagle, "µ¶¼ö¸®" },
        { AnimalType.Plover, "¾Ç¾î»õ" },
        { AnimalType.Chameleon, "Ä«¸á·¹¿Â" },
        { AnimalType.Crow, "±î¸¶±Í" },
        { AnimalType.Hyena, "ÇÏÀÌ¿¡³ª" },
        { AnimalType.None, "???" }
    };
}