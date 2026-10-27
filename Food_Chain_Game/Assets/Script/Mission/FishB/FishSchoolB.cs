using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishSchoolB : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform schoolShadow;
    [SerializeField] private RectTransform[] soloShadows;

    [Header("School Move")]
    [SerializeField] private float schoolMoveMinTime = 2.5f;
    [SerializeField] private float schoolMoveMaxTime = 4.5f;
    [SerializeField] private float schoolArcMin = 30f;
    [SerializeField] private float schoolArcMax = 90f;
    [SerializeField] private float schoolWobbleAmp = 6f;
    [SerializeField] private float schoolWobbleFreq = 2.0f;

    [Header("Solo Move")]
    [SerializeField] private float soloMoveMinTime = 1.2f;
    [SerializeField] private float soloMoveMaxTime = 2.2f;
    [SerializeField] private float soloArcMin = 20f;
    [SerializeField] private float soloArcMax = 70f;

    private FishB _mission;
    private RectTransform _fishingArea;

    private Coroutine _schoolRoutine;
    private Coroutine[] _soloRoutines;

    public void Init(FishB mission, RectTransform fishingArea)
    {
        _mission = mission;
        _fishingArea = fishingArea;

        StopAll();

        if (schoolShadow != null)
            _schoolRoutine = StartCoroutine(SchoolRoutine());

        if (soloShadows != null && soloShadows.Length > 0)
        {
            _soloRoutines = new Coroutine[soloShadows.Length];
            for (int i = 0; i < soloShadows.Length; i++)
            {
                if (soloShadows[i] == null) continue;
                int idx = i;
                _soloRoutines[i] = StartCoroutine(SoloRoutine(soloShadows[idx]));
            }
        }
    }

    private void StopAll()
    {
        if (_schoolRoutine != null) StopCoroutine(_schoolRoutine);
        _schoolRoutine = null;

        if (_soloRoutines != null)
        {
            for (int i = 0; i < _soloRoutines.Length; i++)
            {
                if (_soloRoutines[i] != null) StopCoroutine(_soloRoutines[i]);
            }
        }
        _soloRoutines = null;
    }

    public Rect GetSchoolRectInFishingArea()
    {
        if (schoolShadow == null || _fishingArea == null) return new Rect(0, 0, 0, 0);

        Vector3[] wc = new Vector3[4];
        schoolShadow.GetWorldCorners(wc);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector3 local = _fishingArea.InverseTransformPoint(wc[i]);
            if (local.x < minX) minX = local.x;
            if (local.x > maxX) maxX = local.x;
            if (local.y < minY) minY = local.y;
            if (local.y > maxY) maxY = local.y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private IEnumerator SchoolRoutine()
    {
        RectTransform rt = schoolShadow;
        Vector2 cur = rt.anchoredPosition;

        while (_mission != null && !_mission.IsMissionFinished)
        {
            Vector2 target = RandomPointInside(_fishingArea, rt);
            float dur = Random.Range(schoolMoveMinTime, schoolMoveMaxTime);
            float arc = Random.Range(schoolArcMin, schoolArcMax);

            Vector2 start = cur;
            Vector2 end = target;

            Vector2 dir = (end - start);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            if (perp.sqrMagnitude < 0.001f) perp = Vector2.up;
            perp.Normalize();
            if (Random.value < 0.5f) perp = -perp;

            Vector2 mid = (start + end) * 0.5f;
            Vector2 control = mid + perp * arc;

            float t = 0f;
            while (t < 1f && _mission != null && !_mission.IsMissionFinished)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                float u = Mathf.Clamp01(t);

                Vector2 p =
                    (1 - u) * (1 - u) * start +
                    2 * (1 - u) * u * control +
                    u * u * end;

                float wobble = Mathf.Sin(Time.time * schoolWobbleFreq) * schoolWobbleAmp;
                p.y += wobble;

                p = ClampInside(_fishingArea, rt, p);

                cur = p;
                rt.anchoredPosition = cur;

                yield return null;
            }
        }
    }

    private IEnumerator SoloRoutine(RectTransform solo)
    {
        Vector2 cur = solo.anchoredPosition;

        while (_mission != null && !_mission.IsMissionFinished)
        {
            Vector2 target = RandomPointInside(_fishingArea, solo);
            float dur = Random.Range(soloMoveMinTime, soloMoveMaxTime);
            float arc = Random.Range(soloArcMin, soloArcMax);

            Vector2 start = cur;
            Vector2 end = target;

            Vector2 dir = (end - start);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            if (perp.sqrMagnitude < 0.001f) perp = Vector2.up;
            perp.Normalize();
            if (Random.value < 0.5f) perp = -perp;

            Vector2 mid = (start + end) * 0.5f;
            Vector2 control = mid + perp * arc;

            float t = 0f;
            while (t < 1f && _mission != null && !_mission.IsMissionFinished)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                float u = Mathf.Clamp01(t);

                Vector2 p =
                    (1 - u) * (1 - u) * start +
                    2 * (1 - u) * u * control +
                    u * u * end;

                p = ClampInside(_fishingArea, solo, p);

                cur = p;
                solo.anchoredPosition = cur;

                yield return null;
            }
        }
    }

    private static Vector2 RandomPointInside(RectTransform area, RectTransform item)
    {
        Rect r = area.rect;
        Vector2 half = item.rect.size * 0.5f;

        float x = Random.Range(r.xMin + half.x, r.xMax - half.x);
        float y = Random.Range(r.yMin + half.y, r.yMax - half.y);
        return new Vector2(x, y);
    }

    private static Vector2 ClampInside(RectTransform area, RectTransform item, Vector2 p)
    {
        Rect r = area.rect;
        Vector2 half = item.rect.size * 0.5f;

        p.x = Mathf.Clamp(p.x, r.xMin + half.x, r.xMax - half.x);
        p.y = Mathf.Clamp(p.y, r.yMin + half.y, r.yMax - half.y);
        return p;
    }
}
