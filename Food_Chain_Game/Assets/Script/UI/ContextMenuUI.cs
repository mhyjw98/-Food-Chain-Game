using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        GamePlayer localPlayer = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<GamePlayer>() : null;
        if (player == null || localPlayer == null || GameMamager.Instance == null)
        {
            Close();
            return;
        }

        var chatManager = FindFirstObjectByType<ChatManager>();

        whisperBtn.interactable = (player.netId != localPlayer.netId);
        scanBtn.interactable = (localPlayer.canScan && player.netId != localPlayer.netId && GameMamager.Instance.IsExplorationPhase && localPlayer.maxScanCount > localPlayer.scanCount);
        Debug.Log($"스캔 가능한지 : {localPlayer.canScan}, 본인인지 : {player.netId != localPlayer.netId}, 탐색시간인지 : {GameMamager.Instance.IsExplorationPhase}, 스캔횟수가 남았는지 : {localPlayer.maxScanCount > localPlayer.scanCount}");
        predictBtn.interactable = localPlayer.canPredict && GameMamager.Instance.IsExplorationPhase;

        // 버튼 동작 설정
        // 메뉴가 열린 사이에 대상이 나갔을 수 있으므로 누를 때 다시 확인한다
        whisperBtn.onClick.AddListener(() => {
            if (chatManager != null && targetPlayer != null) chatManager.SelectWhisperTarget(targetPlayer);
            Close();
        });

        scanBtn.onClick.AddListener(() => {
            if (localPlayer != null && targetPlayer != null) localPlayer.CmdScan(targetPlayer.netId);
            Close();
        });

        predictBtn.onClick.AddListener(() => {
            if (localPlayer != null && targetPlayer != null) localPlayer.CmdSetPredict(targetPlayer.netId);
            Close();
        });

        memoBtn.onClick.AddListener(() => {
            PlayerSlot slot = (PlayerSlotUI.Instance != null && targetPlayer != null) ? PlayerSlotUI.Instance.GetSlotByPlayer(targetPlayer) : null;
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
