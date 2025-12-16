using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FishNetB : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform netRect;

    [Header("Follow")]
    [SerializeField] private float followSmooth = 20f;

    [Header("Throw Motion")]
    [SerializeField] private float maxThrowDistance = 650f;
    [SerializeField] private float minThrowDistance = 0f;
    [SerializeField] private float throwTime = 0.25f;
    [SerializeField] private float throwArc = 60f;

    [Header("Velocity Mapping")]
    [SerializeField] private float velocitySampleWindow = 0.35f;
    [SerializeField] private float deadSpeed = 700f;
    [SerializeField] private float fullSpeed = 1900f;
    [SerializeField] private float fineExponent = 1.35f;
    [SerializeField] private float fineGain = 1.0f;
    [SerializeField] private AnimationCurve fineCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Drag Gate")]
    [SerializeField] private float minDragToThrow = 8f;

    [Header("Retry")]
    [SerializeField] private float retryDelay = 2f;

    [Header("Catch")]
    [SerializeField] private float schoolHitPadding = 10f;

    private FishB _mission;
    private RectTransform _controlArea;
    private RectTransform _fishingArea;

    private bool _dragging;
    private bool _throwing;
    private bool _cooldown;

    private Vector2 _homePos;
    private Vector2 _targetFollowPos;

    private Vector2 _dragStartLocal;
    private Vector2 _dragReleaseLocal;

    private struct SpeedSample
    {
        public float t;
        public float speed;
    }

    private readonly List<SpeedSample> _speedSamples = new List<SpeedSample>(32);
    private Vector2 _lastDragLocal;
    private float _lastDragTime;
    private float _avgSpeedWindow;

    public void Init(FishB mission, RectTransform controlArea, RectTransform fishingArea)
    {
        _mission = mission;
        _controlArea = controlArea;
        _fishingArea = fishingArea;

        if (netRect == null)
            netRect = GetComponent<RectTransform>();

        _dragging = false;
        _throwing = false;
        _cooldown = false;

        _speedSamples.Clear();
        _avgSpeedWindow = 0f;

        ForceToControlAreaKeepWorld();

        _homePos = ClampInside(_controlArea, netRect, netRect.anchoredPosition);
        _targetFollowPos = _homePos;

        Reset();
    }

    private void Update()
    {
        if (_mission == null || _mission.IsMissionFinished) return;
        if (_throwing) return;

        if (!_dragging && !_cooldown)
        {
            if (TryGetMouseLocalIn(_controlArea, out Vector2 local))
            {
                _targetFollowPos = ClampInside(_controlArea, netRect, local);
            }

            netRect.anchoredPosition = Vector2.Lerp(
                netRect.anchoredPosition,
                _targetFollowPos,
                Time.deltaTime * followSmooth
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_mission == null || _mission.IsMissionFinished) return;
        if (_throwing || _cooldown) return;

        if (!TryScreenToLocal(_controlArea, eventData.position, out Vector2 local))
            return;

        _dragging = true;
        _dragStartLocal = ClampInside(_controlArea, netRect, local);
        _targetFollowPos = _dragStartLocal;
        netRect.anchoredPosition = _dragStartLocal;

        _speedSamples.Clear();
        _lastDragLocal = _dragStartLocal;
        _lastDragTime = Time.unscaledTime;
        _avgSpeedWindow = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        if (_throwing || _cooldown) return;

        if (!TryScreenToLocal(_controlArea, eventData.position, out Vector2 local))
            return;

        _targetFollowPos = ClampInside(_controlArea, netRect, local);
        netRect.anchoredPosition = _targetFollowPos;

        float now = Time.unscaledTime;
        float dt = now - _lastDragTime;

        if (dt > 0.0001f)
        {
            float speed = (local - _lastDragLocal).magnitude / dt;

            _speedSamples.Add(new SpeedSample { t = now, speed = speed });

            float cutoff = now - velocitySampleWindow;
            for (int i = _speedSamples.Count - 1; i >= 0; i--)
            {
                if (_speedSamples[i].t < cutoff)
                    _speedSamples.RemoveAt(i);
                else
                    break;
            }

            float sum = 0f;
            for (int i = 0; i < _speedSamples.Count; i++)
                sum += _speedSamples[i].speed;

            _avgSpeedWindow = (_speedSamples.Count > 0) ? (sum / _speedSamples.Count) : 0f;
        }

        _lastDragLocal = local;
        _lastDragTime = now;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_dragging) return;
        if (_throwing || _cooldown) return;

        _dragging = false;

        if (!TryScreenToLocal(_controlArea, eventData.position, out Vector2 local))
            local = _targetFollowPos;

        _dragReleaseLocal = ClampInside(_controlArea, netRect, local);

        float speedForThrow = _avgSpeedWindow;

        StartCoroutine(ThrowRoutine(_dragStartLocal, _dragReleaseLocal, speedForThrow));
    }

    private IEnumerator ThrowRoutine(Vector2 startLocal, Vector2 releaseLocal, float throwSpeed)
    {
        if (_mission == null) yield break;
        if (_controlArea == null || _fishingArea == null) yield break;

        _throwing = true;
        _cooldown = true;

        Vector2 dragVec = (releaseLocal - startLocal);
        float dragMag = dragVec.magnitude;

        Vector2 dirFishing = Vector2.up;
        if (dragMag >= minDragToThrow)
        {
            Vector3 worldDir = _controlArea.TransformVector(new Vector3(dragVec.x, dragVec.y, 0f));
            Vector3 fishingDir3 = _fishingArea.InverseTransformVector(worldDir);
            Vector2 v = new Vector2(fishingDir3.x, fishingDir3.y);

            if (v.sqrMagnitude > 0.001f)
                dirFishing = v.normalized;
        }

        float dist;

        if (throwSpeed <= deadSpeed)
        {
            dist = minThrowDistance;
        }
        else
        {
            float t = Mathf.InverseLerp(deadSpeed, fullSpeed, throwSpeed);
            t = Mathf.Clamp01(t);

            t = Mathf.Clamp01(t * fineGain);

            float shaped = Mathf.Pow(t, Mathf.Max(1f, fineExponent));

            if (fineCurve != null)
                shaped = Mathf.Clamp01(fineCurve.Evaluate(shaped));

            dist = Mathf.Lerp(minThrowDistance, maxThrowDistance, shaped);
        }

        netRect.SetParent(_fishingArea, worldPositionStays: false);

        Vector2 from = ConvertAnchored(_controlArea, _fishingArea, startLocal);
        netRect.anchoredPosition = from;

        Vector2 landing = from + dirFishing * dist;

        landing = ClampInside(_fishingArea, netRect, landing);

        float tMove = 0f;

        Vector2 d01 = landing - from;
        Vector2 mid = (from + landing) * 0.5f;

        Vector2 perp = new Vector2(-d01.y, d01.x);
        if (perp.sqrMagnitude < 0.001f) perp = Vector2.right;
        perp.Normalize();
        if (Random.value < 0.5f) perp = -perp;

        float arcScaled = throwArc * Mathf.Clamp01(d01.magnitude / 300f);
        Vector2 control = mid + perp * arcScaled;

        while (tMove < 1f && _mission != null && !_mission.IsMissionFinished)
        {
            tMove += Time.deltaTime / Mathf.Max(0.01f, throwTime);
            float u = Mathf.Clamp01(tMove);

            Vector2 p =
                (1 - u) * (1 - u) * from +
                2 * (1 - u) * u * control +
                u * u * landing;

            netRect.anchoredPosition = p;
            yield return null;
        }

        netRect.anchoredPosition = landing;

        bool hit = CheckHitSchool(landing);

        if (hit)
        {
            _throwing = false;
            _cooldown = false;
            _mission.OnNetSuccess();
            yield break;
        }

        _throwing = false;

        yield return new WaitForSeconds(retryDelay);

        if (_mission == null || _mission.IsMissionFinished) yield break;

        Reset();
        _cooldown = false;
    }


    public void Reset()
    {
        StopAllCoroutines();

        _dragging = false;
        _throwing = false;
        _cooldown = false;

        _speedSamples.Clear();
        _avgSpeedWindow = 0f;

        ForceToControlAreaKeepWorld();

        netRect.anchoredPosition = _homePos;
        _targetFollowPos = _homePos;
    }

    private void ForceToControlAreaKeepWorld()
    {
        if (_controlArea == null || netRect == null) return;

        RectTransform currentParent = netRect.parent as RectTransform;

        if (currentParent == _controlArea)
            return;

        Vector2 localInControl = netRect.anchoredPosition;

        if (currentParent != null)
            localInControl = ConvertAnchored(currentParent, _controlArea, netRect.anchoredPosition);

        netRect.SetParent(_controlArea, worldPositionStays: false);
        netRect.anchoredPosition = localInControl;
    }

    private bool CheckHitSchool(Vector2 landingInFishingArea)
    {
        if (_mission == null) return false;

        FishSchoolB school = _mission.GetComponentInChildren<FishSchoolB>();
        if (school == null) school = FindObjectOfType<FishSchoolB>();

        if (school == null) return false;

        Rect r = school.GetSchoolRectInFishingArea();
        r.xMin -= schoolHitPadding;
        r.xMax += schoolHitPadding;
        r.yMin -= schoolHitPadding;
        r.yMax += schoolHitPadding;

        return r.Contains(landingInFishingArea);
    }

    private static bool TryGetMouseLocalIn(RectTransform area, out Vector2 local)
    {
        local = Vector2.zero;
        if (area == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area,
            Input.mousePosition,
            null,
            out local
        );
    }

    private static bool TryScreenToLocal(RectTransform area, Vector2 screen, out Vector2 local)
    {
        local = Vector2.zero;
        if (area == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area,
            screen,
            null,
            out local
        );
    }

    private static Vector2 ClampInside(RectTransform area, RectTransform item, Vector2 p)
    {
        Rect r = area.rect;
        Vector2 half = item.rect.size * 0.5f;

        p.x = Mathf.Clamp(p.x, r.xMin + half.x, r.xMax - half.x);
        p.y = Mathf.Clamp(p.y, r.yMin + half.y, r.yMax - half.y);
        return p;
    }

    private static Vector2 ConvertAnchored(RectTransform fromArea, RectTransform toArea, Vector2 fromLocal)
    {
        Vector3 world = fromArea.TransformPoint(fromLocal);
        Vector3 toLocal3 = toArea.InverseTransformPoint(world);
        return new Vector2(toLocal3.x, toLocal3.y);
    }
}
