using UnityEngine;

// 고를 수 있는 의상 목록. 0번은 기본(의상 없음)으로 둔다.
// 서버와 모든 클라이언트가 같은 목록을 써야 번호가 맞으므로 Resources에 하나만 두고 불러온다
[CreateAssetMenu(menuName = "Costume/Costume Catalog", fileName = "CostumeCatalog")]
public class CostumeCatalog : ScriptableObject
{
    public const string ResourcePath = "CostumeCatalog";

    [SerializeField] private CostumeData[] costumes = new CostumeData[0];

    private static CostumeCatalog _instance;
    private static bool _loadFailed;

    public static CostumeCatalog Instance
    {
        get
        {
            if (_instance == null && !_loadFailed)
            {
                _instance = Resources.Load<CostumeCatalog>(ResourcePath);
                if (_instance == null)
                {
                    _loadFailed = true;
                    Debug.LogWarning($"[Costume] Resources/{ResourcePath} 에 CostumeCatalog가 없습니다. 의상 없이 진행합니다.");
                }
            }
            return _instance;
        }
    }

    public int Count => costumes != null ? costumes.Length : 0;

    public static bool IsValidIndex(int index)
    {
        var catalog = Instance;
        return catalog != null && index >= 0 && index < catalog.Count;
    }

    // 범위를 벗어나면 null(기본 모습)
    public static CostumeData Get(int index)
    {
        var catalog = Instance;
        if (catalog == null || index < 0 || index >= catalog.Count) return null;
        return catalog.costumes[index];
    }
}
