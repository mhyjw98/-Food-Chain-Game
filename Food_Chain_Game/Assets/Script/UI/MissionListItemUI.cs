using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionListItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI statusText;

    private MissionType _type;

    public MissionType Type => _type;

    public void SetData(MissionType type, MissionStatus status)
    {
        _type = type;
        missionNameText.text = MissionData.GetName(type);
        statusText.text = GetStatusText(status);
        statusText.color = GetStatusColor(status);
    }

    private string GetStatusText(MissionStatus status)
    {
        switch (status)
        {
            case MissionStatus.NotStarted: return "미완료";
            case MissionStatus.InProgress: return "진행중";
            case MissionStatus.Completed: return "완료";
            default: return "";
        }
    }

    private Color GetStatusColor(MissionStatus status)
    {
        switch (status)
        {
            case MissionStatus.NotStarted: return Color.gray;
            case MissionStatus.InProgress: return Color.yellow;
            case MissionStatus.Completed: return Color.green;
            default: return Color.white;
        }
    }
}
