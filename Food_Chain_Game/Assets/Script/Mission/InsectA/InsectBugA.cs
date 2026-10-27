using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsectBugA : MonoBehaviour
{
    public enum InsectState
    {
        Flying,
        Landed
    }

    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private RectTransform playPanel;

    [Header("Move Settings")]
    [SerializeField] private float minFlyDuration = 0.4f;
    [SerializeField] private float maxFlyDuration = 1.0f;
    [SerializeField] private float minArc = 120f;
    [SerializeField] private float maxArc = 260f;

    [Header("Landing Settings")]
    [SerializeField] private float minSitTime = 0.5f;
    [SerializeField] private float maxSitTime = 1.5f;
    [SerializeField] private float fakeLandDistance = 50f;
    [SerializeField] private float maxNoLandTime = 10f;
    [SerializeField] private float landJitter = 10f;

    [Header("Hit Settings")]
    [SerializeField] private float hitRadius = 50f;

    private InsectA _mission;
    private RectTransform[] _landTargets;
    private InsectState _state = InsectState.Flying;
    private float _spawnTime;

    private Vector2 _currentPos;
    private float _lastRealLandTime;

    public bool IsLanded => _state == InsectState.Landed;

    public void Init(InsectA mission, RectTransform playPanel, RectTransform[] landTargets)
    {
        _mission = mission;
        this.playPanel = playPanel;
        _landTargets = landTargets;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        Rect r = playPanel.rect;
        float x = Random.Range(r.xMin, r.xMax);
        float y = Random.Range(r.yMin, r.yMax);
        _currentPos = new Vector2(x, y);
        rect.anchoredPosition = _currentPos;

        _state = InsectState.Flying;
        _lastRealLandTime = Time.time;
        _spawnTime = Time.time;

        StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        while (_mission != null && !_mission.IsMissionFinished)
        {
            bool forceRealLand = (Time.time - _lastRealLandTime >= maxNoLandTime);

            bool doRealLand = forceRealLand || Random.value < 0.4f;
            bool doFakeLand = !doRealLand && Random.value < 0.4f;

            if (doRealLand && _landTargets != null && _landTargets.Length > 0)
            {
                RectTransform target = _landTargets[Random.Range(0, _landTargets.Length)];

                Vector2 targetPos = GetLandingPosInPanel(target);

                if (landJitter > 0f)
                    targetPos += Random.insideUnitCircle * landJitter;

                float duration = Random.Range(minFlyDuration, maxFlyDuration);
                yield return MoveAlongCurve(_currentPos, targetPos, duration);

                _state = InsectState.Landed;
                _currentPos = targetPos;
                rect.anchoredPosition = _currentPos;
                _lastRealLandTime = Time.time;

                float elapsed = Time.time - _spawnTime;

                float sitTime;
                if (elapsed < 5f)
                {
                    sitTime = Random.Range(0.1f, 0.2f);
                }
                else
                {
                    sitTime = Random.Range(minSitTime, maxSitTime);
                }

                float sitTimer = 0f;

                while (sitTimer < sitTime && _mission != null && !_mission.IsMissionFinished)
                {
                    sitTimer += Time.deltaTime;
                    yield return null;
                }

                _state = InsectState.Flying;
            }
            else
            {
                Vector2 endPos;

                if (doFakeLand && _landTargets != null && _landTargets.Length > 0)
                {
                    RectTransform target = _landTargets[Random.Range(0, _landTargets.Length)];
                    Vector2 tPos = GetLandingPosInPanel(target);

                    Vector2 dir = (tPos - _currentPos);
                    if (dir.sqrMagnitude < 0.0001f)
                        dir = Vector2.right;
                    else
                        dir.Normalize();

                    float dist = Random.Range(fakeLandDistance * 0.5f, fakeLandDistance);
                    endPos = _currentPos + dir * dist;
                }
                else
                {
                    Rect r = playPanel.rect;
                    float x = Random.Range(r.xMin, r.xMax);
                    float y = Random.Range(r.yMin, r.yMax);
                    endPos = new Vector2(x, y);
                }

                float duration = Random.Range(minFlyDuration, maxFlyDuration);
                yield return MoveAlongCurve(_currentPos, endPos, duration);

                _state = InsectState.Flying;
                _currentPos = endPos;
                rect.anchoredPosition = _currentPos;
            }
        }
    }
    private Vector2 GetLandingPosInPanel(RectTransform target)
    {
        if (target == null || playPanel == null)
            return Vector2.zero;

        Vector3 centerLocal = target.rect.center;
        Vector3 centerWorld = target.TransformPoint(centerLocal);

        Vector3 panelLocal = playPanel.InverseTransformPoint(centerWorld);

        return (Vector2)panelLocal;
    }
    private IEnumerator MoveAlongCurve(Vector2 start, Vector2 end, float duration)
    {
        Vector2 dir = (end - start);
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;
        else
            dir.Normalize();

        Vector2 perp = new Vector2(-dir.y, dir.x);

        float arc = Random.Range(minArc, maxArc);
        float sign = (Random.value < 0.5f) ? -1f : 1f;

        Vector2 mid = (start + end) * 0.5f;
        Vector2 control = mid + perp * arc * sign;

        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        Vector2 prevPos = start;

        while (t < 1f && _mission != null && !_mission.IsMissionFinished)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);

            Vector2 p =
                (1 - u) * (1 - u) * start +
                2 * (1 - u) * u * control +
                u * u * end;

            Rect r = playPanel.rect;
            p.x = Mathf.Clamp(p.x, r.xMin, r.xMax);
            p.y = Mathf.Clamp(p.y, r.yMin, r.yMax);

            _currentPos = p;
            rect.anchoredPosition = _currentPos;

            Vector2 moveDir = p - prevPos;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                angle -= 90f;

                float rotateLerpSpeed = 15f;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
                rect.localRotation = Quaternion.Slerp(
                    rect.localRotation,
                    targetRot,
                    Time.deltaTime * rotateLerpSpeed
                );

                prevPos = p;
            }

            yield return null;
        }
    }


    public bool TryHit(Vector2 swatterLocalPos)
    {
        if (!IsLanded)
            return false;

        float dist = Vector2.Distance(_currentPos, swatterLocalPos);
        if (dist <= hitRadius)
        {
            gameObject.SetActive(false);
            return true;
        }

        return false;
    }
}
