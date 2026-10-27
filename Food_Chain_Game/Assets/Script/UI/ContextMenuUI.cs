using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContextMenuUI : MonoBehaviour
{
    public static ContextMenuUI CurrentOpen;

    // 플레이어 탐색과 승리 예측은 각각 탐색 오브젝트(InvestigationObject), 예측 오브젝트(PredictObject)에서만 한다
    public Button whisperBtn, memoBtn;
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
        UIManager.RegisterEscape(this, Close);
        GamePlayer localPlayer = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<GamePlayer>() : null;
        if (player == null || localPlayer == null || GameMamager.Instance == null)
        {
            Close();
            return;
        }

        var chatManager = FindFirstObjectByType<ChatManager>();

        // 산 사람과 유령 사이에는 귓속말을 할 수 없다
        whisperBtn.interactable = player.netId != localPlayer.netId && player.isAlive == localPlayer.isAlive;

        // 버튼 동작 설정
        // 메뉴가 열린 사이에 대상이 나갔을 수 있으므로 누를 때 다시 확인한다
        whisperBtn.onClick.AddListener(() => {
            if (chatManager != null && targetPlayer != null) chatManager.SelectWhisperTarget(targetPlayer);
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

        UIManager.UnregisterEscape(this);
        Destroy(gameObject);
    }

    // 다른 메뉴가 열리며 바로 Destroy된 경우에도 ESC 목록에서 빠지게 한다
    private void OnDestroy()
    {
        UIManager.UnregisterEscape(this);
    }
}
