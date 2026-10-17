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
    [Tooltip("기록 종류 표시. 조력자가 시체를 직접 조사한 기록과 탐색 오브젝트로 탐색한 기록을 구분한다")]
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private string corpseTypeText = "부검 결과";
    [SerializeField] private string playerTypeText = "탐색 결과";

    private string _recordId;
    private System.Action<string> _onClick;

    public void Bind(InvestigationRecord record, System.Action<string> onClick)
    {
        _recordId = record.recordId;
        _onClick = onClick;

        titleText.text = record.title;
        timeText.text = record.subtitle;
        if (typeText != null)
            typeText.text = record.type == InvestigationType.Corpse ? corpseTypeText : playerTypeText;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onClick?.Invoke(_recordId));
    }
}
