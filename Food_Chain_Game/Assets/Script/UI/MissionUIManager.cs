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

    public void StartMission(MissionType type, System.Action onComplete, System.Action onClosed)
    {
        if (!_missions.TryGetValue(type, out var prefab))
        {
            Debug.LogError($"[MissionUIManager] 미션 프리팹 없음: {type}");
            return;
        }
        if (_activeInstance != null)
        {
            Destroy(_activeInstance.gameObject);
            _activeInstance = null;
        }

        _activeInstance = Instantiate(prefab, uiRoot);
        _activeInstance.gameObject.SetActive(true);

        _activeInstance.OnMissionCompleted = _ =>
        {
            onComplete?.Invoke();
            onClosed?.Invoke();
            DestroyActive();
        };

        _activeInstance.OnMissionFailed = _ =>
        {
            onClosed?.Invoke();
            DestroyActive();
        };

        _activeInstance.Begin();
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
