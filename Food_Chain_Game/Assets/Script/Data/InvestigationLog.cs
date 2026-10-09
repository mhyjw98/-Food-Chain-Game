using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestigationLog : MonoBehaviour
{
    public static InvestigationLog Instance;

    private readonly List<InvestigationRecord> _records = new();
    private readonly Dictionary<string, InvestigationRecord> _byId = new();

    private void Awake()
    {
        Instance = this;
    }

    // 씬이 바뀐 뒤 파괴된 오브젝트를 가리키는 참조가 남지 않게 한다
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public IReadOnlyList<InvestigationRecord> Records => _records;

    public void Add(InvestigationRecord record)
    {       
        if (string.IsNullOrEmpty(record.recordId))
            record.recordId = System.Guid.NewGuid().ToString("N");

        if (_byId.ContainsKey(record.recordId)) return;

        _byId.Add(record.recordId, record);
        _records.Add(record);        
    }

    public bool TryGet(string recordId, out InvestigationRecord r)
        => _byId.TryGetValue(recordId, out r);
}
