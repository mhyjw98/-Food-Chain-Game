using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InsectBugB : MonoBehaviour, IPointerDownHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image icon;

    [Header("Move Settings")]
    [SerializeField] private float minMoveTime = 0.25f;
    [SerializeField] private float maxMoveTime = 0.5f;
    [SerializeField] private float minRestTime = 0.2f;
    [SerializeField] private float maxRestTime = 0.4f;

    [Header("Curve Settings")]
    [SerializeField] private float minArc = 20f;
    [SerializeField] private float maxArc = 60f;
    [SerializeField] private float angleOffset = 0f;

    [Header("Fake Settings")]
    [SerializeField] private float fakePassChance = 0.4f;
    [SerializeField] private float fakeMoveMinTime = 0.15f;
    [SerializeField] private float fakeMoveMaxTime = 0.3f;
  
    private InsectB _mission;
    private RectTransform _playPanel;
    private RectTransform[] _bushPoints;
    private InsectBushB[] _bushSpots;

    private Vector2 _currentPos;
    private int _currentBushIndex = -1;
    private Coroutine _crawlRoutine;
    public void Init(
    InsectB mission,
    RectTransform playPanel,
    RectTransform[] bushPoints,
    InsectBushB[] bushSpots)
    {
        _mission = mission;
        _playPanel = playPanel;
        _bushPoints = bushPoints;
        _bushSpots = bushSpots;

        if (rect == null) rect = GetComponent<RectTransform>();
        if (icon == null) icon = GetComponent<Image>();

        _currentPos = rect.anchoredPosition;

        if (_crawlRoutine != null)
            StopCoroutine(_crawlRoutine);

        _crawlRoutine = StartCoroutine(CrawlRoutine());
    }

    private IEnumerator CrawlRoutine()
    {
        while (_mission != null && !_mission.IsMissionFinished)
        {
            int realIndex = GetNextBushIndex();
            if (realIndex < 0)
                yield break;

            Vector2 realPos = GetBushPosInPanel(_bushPoints[realIndex]);

            bool doFake = (_bushPoints.Length >= 2) && Random.value < fakePassChance;
            if (doFake)
            {
                int fakeIndex = GetRandomBushIndexExcept(realIndex);

                if (fakeIndex >= 0)
                {
                    Vector2 fakePos = GetBushPosInPanel(_bushPoints[fakeIndex]);

                    float fakeTime = Random.Range(fakeMoveMinTime, fakeMoveMaxTime);
                    yield return MoveAlongCurve(_currentPos, fakePos, fakeTime);

                    _currentBushIndex = fakeIndex;
                    _currentPos = fakePos;
                    rect.anchoredPosition = _currentPos;
                }
            }

            float moveTime = Random.Range(minMoveTime, maxMoveTime);
            yield return MoveAlongCurve(_currentPos, realPos, moveTime);

            _currentBushIndex = realIndex;
            _currentPos = realPos;
            rect.anchoredPosition = _currentPos;

            float rest = Random.Range(minRestTime, maxRestTime);
            float timer = 0f;
            while (timer < rest && _mission != null && !_mission.IsMissionFinished)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private int GetNextBushIndex()
    {
        if (_bushPoints == null || _bushPoints.Length == 0)
            return -1;

        if (_bushPoints.Length == 1)
            return 0;

        int idx = _currentBushIndex;

        while (idx == _currentBushIndex)
        {
            idx = Random.Range(0, _bushPoints.Length);
        }
        return idx;
    }

    private int GetRandomBushIndexExcept(int except)
    {
        if (_bushPoints == null || _bushPoints.Length == 0)
            return -1;

        if (_bushPoints.Length == 1)
            return 0;

        int tries = 0;
        int idx = except;
        while (idx == except && tries < 10)
        {
            idx = Random.Range(0, _bushPoints.Length);
            tries++;
        }
        return idx;
    }
    private Vector2 GetBushPosInPanel(RectTransform bush)
    {
        if (bush == null || _playPanel == null)
            return Vector2.zero;

        Vector3 centerLocal = bush.rect.center;
        Vector3 centerWorld = bush.TransformPoint(centerLocal);
        Vector3 panelLocal = _playPanel.InverseTransformPoint(centerWorld);

        return (Vector2)panelLocal;
    }
    private IEnumerator MoveAlongCurve(Vector2 start, Vector2 end, float duration)
    {
        if (duration < 0.05f) duration = 0.05f;

        Vector2 dir = end - start;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;
        else
            dir.Normalize();

        Vector2 perp = new Vector2(-dir.y, dir.x);
        if (Random.value < 0.5f) perp = -perp;

        float arc = Random.Range(minArc, maxArc);
        Vector2 mid = (start + end) * 0.5f;
        Vector2 control = mid + perp * arc;

        float t = 0f;
        Vector2 prevPos = start;

        while (t < 1f && _mission != null && !_mission.IsMissionFinished)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);

            Vector2 p =
                (1 - u) * (1 - u) * start +
                2 * (1 - u) * u * control +
                u * u * end;

            Rect r = _playPanel.rect;
            p.x = Mathf.Clamp(p.x, r.xMin, r.xMax);
            p.y = Mathf.Clamp(p.y, r.yMin, r.yMax);

            _currentPos = p;
            rect.anchoredPosition = _currentPos;

            Vector2 moveDir = p - prevPos;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                angle += angleOffset;

                rect.localRotation = Quaternion.Slerp(
                    rect.localRotation,
                    Quaternion.Euler(0f, 0f, angle),
                    Time.deltaTime * 10f
                );
                prevPos = p;
            }

            yield return null;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_mission == null || _mission.IsMissionFinished)
            return;

        int bushIndex = GetCatchableBushIndex();
        if (bushIndex >= 0)
        {
            _mission.OnBugCaught(this);
            gameObject.SetActive(false);
        }
    }

    private int GetCatchableBushIndex()
    {
        if (_bushPoints == null || _bushSpots == null)
            return -1;

        for (int i = 0; i < _bushPoints.Length && i < _bushSpots.Length; i++)
        {
            if (_bushPoints[i] == null || _bushSpots[i] == null)
                continue;

            if (!_bushSpots[i].IsOpen)
                continue;

            if (IsInsideBushArea(i, _currentPos))
                return i;
        }

        return -1;
    }

    private bool IsInsideBushArea(int i, Vector2 pos)
    {
        if (_bushPoints == null || i < 0 || i >= _bushPoints.Length)
            return false;

        RectTransform bush = _bushPoints[i];
        if (bush == null) return false;

        Rect rect = GetBushRectInPanel(bush);
        return rect.Contains(pos);
    }
    private Rect GetBushRectInPanel(RectTransform bush)
    {
        if (bush == null || _playPanel == null)
            return new Rect(0, 0, 0, 0);

        Vector3[] worldCorners = new Vector3[4];
        bush.GetWorldCorners(worldCorners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector3 local = _playPanel.InverseTransformPoint(worldCorners[i]);
            if (local.x < minX) minX = local.x;
            if (local.x > maxX) maxX = local.x;
            if (local.y < minY) minY = local.y;
            if (local.y > maxY) maxY = local.y;
        }

        return new Rect(
            minX,
            minY,
            maxX - minX,
            maxY - minY
        );
    }
}
