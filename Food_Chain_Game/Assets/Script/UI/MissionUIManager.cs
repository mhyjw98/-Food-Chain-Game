using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    public static MissionUIManager Instance { get; private set; }

    [SerializeField] private List<BaseMission> missionPrefabs;

    private Dictionary<MissionType, BaseMission> _missions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _missions = new Dictionary<MissionType, BaseMission>();
        foreach (var m in missionPrefabs)
        {
            m.gameObject.SetActive(false);
            _missions[m.MissionType] = m;
        }
    }

    public void StartMission(MissionType type, System.Action onComplete)
    {
        if (!_missions.TryGetValue(type, out var mission))
        {
            Debug.LogError($"[MissionUIManager] 미션 없음: {type}");
            return;
        }

        mission.OnMissionCompleted = _ =>
        {
            onComplete?.Invoke();
        };

        mission.OnMissionFailed = _ =>
        {
            // 실패
        };

        mission.Begin();
    }
}
