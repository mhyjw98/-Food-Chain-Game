using Mirror;
using Mirror.Examples.Chat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class ContextMenuUI : MonoBehaviour
{
    public static ContextMenuUI CurrentOpen;

    public Button whisperBtn, scanBtn, predictBtn, memoBtn;
    private GamePlayer targetPlayer;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (!IsPointerOverUIElement(gameObject))
            {
                Close();
            }
        }
    }
    public void Setup(GamePlayer player)
    {
        if(CurrentOpen != null && CurrentOpen != this)
            Destroy(CurrentOpen.gameObject);

        CurrentOpen = this;
        targetPlayer = player;
        GamePlayer localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();
        var chatManager = FindObjectOfType<ChatManager>();

        whisperBtn.interactable = (player.netId != localPlayer.netId);
        scanBtn.interactable = (localPlayer.canScan && player.netId != localPlayer.netId && GameMamager.Instance.IsExplorationPhase && localPlayer.maxScanCount > localPlayer.scanCount);
        Debug.Log($"스캔 가능한지 : {localPlayer.canScan}, 본인인지 : {player.netId != localPlayer.netId}, 탐색시간인지 : {GameMamager.Instance.IsExplorationPhase}, 스캔횟수가 남았는지 : {localPlayer.maxScanCount > localPlayer.scanCount}");
        predictBtn.interactable = localPlayer.canPredict;

        // 버튼 동작 설정
        whisperBtn.onClick.AddListener(() => {
            chatManager.SelectWhisperTarget(targetPlayer);
            Close();
        });

        scanBtn.onClick.AddListener(() => {
            localPlayer.CmdScan(targetPlayer.netId);
            Close();
        });

        predictBtn.onClick.AddListener(() => {
            localPlayer.CmdPredict(targetPlayer.netId);
            Close();
        });

        memoBtn.onClick.AddListener(() => {
            PlayerSlot slot = PlayerSlotUI.Instance.GetSlotByPlayer(targetPlayer);
            if (slot != null) slot.BeginEditMemo();
            Close();
        });
    }
    private bool IsPointerOverUIElement(GameObject target)
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var r in results)
        {
            if (r.gameObject == target || 
                r.gameObject.transform.IsChildOf(target.transform) ||r.gameObject.GetComponent<PlayerSlotUI>() != null ||r.gameObject.GetComponent<PlayerSlot>() != null)

                return true;
        }

        return false;
    }
    public void Close()
    {
        if (CurrentOpen == this)
            CurrentOpen = null;

        Destroy(gameObject);
    }
}
