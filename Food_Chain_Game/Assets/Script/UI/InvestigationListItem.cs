using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InvestigationListItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timeText;

    private string _recordId;
    private System.Action<string> _onClick;

    public void Bind(InvestigationRecord record, System.Action<string> onClick)
    {
        _recordId = record.recordId;
        _onClick = onClick;

        titleText.text = record.title;
        timeText.text = record.subtitle;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onClick?.Invoke(_recordId));
    }
}
