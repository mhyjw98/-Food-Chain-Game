using System.Collections.Generic;
using UnityEngine;

// 위장 오브젝트: 카멜레온이 가까이 가서 상호작용하면 위장 창(모든 동물 목록)이 열린다.
// 위장 확정(GamePlayer.CmdSetDisguise)은 서버가 카멜레온인지와 이 오브젝트 근처인지를 다시 확인한다
public class DisguiseObject : MonoBehaviour
{
    [Tooltip("서버가 위장을 허용하는 최대 거리. Scanner의 scanRadius보다 조금 크게 잡아 지연 오차를 허용")]
    [SerializeField] private float interactRange = 2f;

    private static readonly List<DisguiseObject> _instances = new();
    public static IReadOnlyList<DisguiseObject> All => _instances;

    private Collider2D _bodyCollider;

    public static bool IsAnyInRange(Vector2 position)
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null && _instances[i].IsInRange(position))
                return true;
        }
        return false;
    }

    public bool IsInRange(Vector2 position)
    {
        Vector2 closest = (_bodyCollider != null && _bodyCollider.enabled)
            ? _bodyCollider.ClosestPoint(position)
            : (Vector2)transform.position;

        return (closest - position).sqrMagnitude <= interactRange * interactRange;
    }

    private void Awake()
    {
        _bodyCollider = GetComponent<Collider2D>();
        _instances.Add(this);
    }

    private void OnDestroy()
    {
        _instances.Remove(this);
    }

    private void OnMouseDown()
    {
        var localPlayer = GamePlayer.LocalPlayer;
        if (localPlayer == null || !IsInRange(localPlayer.transform.position)) return;
        if (UIManager.Instance != null && !UIManager.Instance.CanInteract()) return;

        localPlayer.TryStartDisguise();
    }
}
