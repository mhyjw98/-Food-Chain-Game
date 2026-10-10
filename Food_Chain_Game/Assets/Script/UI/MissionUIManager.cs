using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    public static MissionUIManager Instance { get; private set; }

    [SerializeField] private List<BaseMission> missionPrefabs;  
    [SerializeField] private Transform uiRoot;

    private Dictionary<MissionType, BaseMission> _missions;
    private BaseMission _activeInstance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _missions = new Dictionary<MissionType, BaseMission>();

        foreach (var prefab in missionPrefabs)
        {
            if (prefab == null) continue;

            _missions[prefab.MissionType] = prefab;
        }
    }

    public bool HasMission(MissionType type)
    {
        return _missions != null && _missions.ContainsKey(type);
    }

    // onFinished(completed)는 클리어/닫기/실패 중 하나로 UI가 끝날 때 정확히 한 번 호출된다.
    // UI를 열지 못하면 false를 반환하고 onFinished는 호출하지 않는다
    public bool StartMission(MissionType type, System.Action<bool> onFinished, GamePlayer player)
    {
        if (player == null || _missions == null || !_missions.TryGetValue(type, out var prefab))
        {
            Debug.LogError($"[MissionUIManager] 미션 프리팹 없음: {type}");
            return false;
        }
        if (_activeInstance != null)
        {
            Destroy(_activeInstance.gameObject);
            _activeInstance = null;
        }

        var instance = Instantiate(prefab, uiRoot);
        _activeInstance = instance;
        _activeInstance.gameObject.SetActive(true);
        player.progressMission = _activeInstance;

        _activeInstance.OnMissionCompleted = _ => Finish(instance, player, onFinished, true);
        _activeInstance.OnMissionFailed = _ => Finish(instance, player, onFinished, false);

        _activeInstance.Begin();
        return true;
    }

    // 미션 하나당 한 번만 처리한다. Destroy는 프레임 끝에 실행되므로, 그 사이에 같은 미션이 완료/실패를
    // 또 알리면 onFinished가 두 번 불려 서버에 완료와 취소가 함께 전달될 수 있다
    private void Finish(BaseMission instance, GamePlayer player, System.Action<bool> onFinished, bool completed)
    {
        if (instance == null || _activeInstance != instance) return;

        if (player != null)
            player.progressMission = null;

        onFinished?.Invoke(completed);
        DestroyActive();
    }
    private void DestroyActive()
    {
        if (_activeInstance != null)
        {
            Destroy(_activeInstance.gameObject);
            _activeInstance = null;
        }
    }
}
