using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeatB : BaseMission
{
    [Header("UI")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    [Header("Spawn")]
    [SerializeField] private RectTransform[] spawnPoints;
    [SerializeField] private MeatBItem piecePrefab;
    [SerializeField] private int spawnCount = 5;

    [Header("Offset")]
    [SerializeField] private float sizeToUpOffsetFactor = 0.25f;
    [SerializeField] private float baseUpOffset = 6f;

    private readonly List<MeatBItem> _spawned = new List<MeatBItem>(16);
    private int _clearedCount;

    private float _remainTime;
    private bool _isFinished;
    public RectTransform PlayPanel => playPanel;
    public bool IsMissionFinished => _isFinished;

    private void Update()
    {
        if (_isFinished) return;

        _remainTime -= Time.deltaTime;
        if (_remainTime < 0f) _remainTime = 0f;

        if (timerText != null)
            timerText.text = _remainTime.ToString("0.0");

        if (_remainTime <= 0f && !_isFinished)
        {
            StartCoroutine(FailCoroutine());
        }
    }
    public override void Begin()
    {
        base.Begin();
        MissionType = MissionType.Meat_B;

        _remainTime = timeLimit;       
        _isFinished = false;
        _clearedCount = 0;

        if (statusText != null)
        {
            statusText.text = "";
        }

        ClearSpawned();
        SpawnPieces();
    }

    public override void End()
    {
        base.End();
        ClearSpawned();
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }

    private void SpawnPieces()
    {
        if (playPanel == null || piecePrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        int count = Mathf.Clamp(spawnCount, 1, spawnPoints.Length);

        List<int> indices = new List<int>(spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++) indices.Add(i);

        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(0, indices.Count);
            int spIndex = indices[pick];
            indices.RemoveAt(pick);

            RectTransform sp = spawnPoints[spIndex];
            if (sp == null) continue;

            MeatBItem piece = Instantiate(piecePrefab, playPanel);
            piece.Init(this);

            RectTransform pr = piece.Rect;

            float h = pr.rect.height;
            float up = baseUpOffset + (h * sizeToUpOffsetFactor);

            Vector3 worldPos = sp.position + new Vector3(0f, up, 0f);
            pr.position = worldPos;

            _spawned.Add(piece);
        }
    }

    public void OnPieceCleared(MeatBItem piece)
    {
        if (_isFinished) return;

        _clearedCount++;

        int target = Mathf.Clamp(spawnCount, 1, spawnPoints.Length);
        if (_clearedCount >= target)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    private IEnumerator CompleteCoroutine()
    {
        _isFinished = true;

        if (statusText != null)
        {
            statusText.text = "성 공";
            statusText.color = Color.green;
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(1.5f);
        Complete();
    }

    private IEnumerator FailCoroutine()
    {
        _isFinished = true;

        if (statusText != null)
        {
            statusText.text = "실 패";
            statusText.color = Color.red;
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(1.5f);
        Fail();
    }
}
