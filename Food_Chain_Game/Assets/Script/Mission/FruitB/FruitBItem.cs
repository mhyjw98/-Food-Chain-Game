using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitBItem : MonoBehaviour
{
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform rect;
    [SerializeField] private float dropDistance = 150f;
    [SerializeField] private float dropDuration = 0.4f;
    [SerializeField] private float minRotation = -45f;
    [SerializeField] private float maxRotation = 45f;

    private FruitB _mission;
    private bool _dropped;

    public bool IsDropped => _dropped;

    public void Init(FruitB mission)
    {
        _mission = mission;
        if (rect == null)
            rect = GetComponent<RectTransform>();

        _dropped = false;

        ResetToSpawn();
    }
    public void ResetToSpawn()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (spawnPoint == null)
            return;

        rect.position = spawnPoint.position;
        rect.rotation = spawnPoint.rotation;
    }

    public bool TryHit(Vector2 stoneScreenPos, Camera cam)
    {
        if (_dropped) return false;
        if (rect == null) return false;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(
            rect,
            stoneScreenPos,
            cam
        );

        if (!inside)
            return false;

        _dropped = true;
        StartCoroutine(DropCoroutine());

        if (_mission != null)
            _mission.OnFruitHit(this);

        return true;
    }

    private IEnumerator DropCoroutine()
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, -dropDistance);

        Quaternion rotStart = rect.localRotation;
        float targetZ = Random.Range(minRotation, maxRotation);
        Quaternion rotEnd = Quaternion.Euler(0f, 0f, targetZ);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dropDuration;
            float a = Mathf.Clamp01(t);
            float eased = 1f - Mathf.Pow(1f - a, 2f);

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            rect.localRotation = Quaternion.Slerp(rotStart, rotEnd, eased);

            yield return null;
        }

        rect.anchoredPosition = endPos;
        rect.localRotation = rotEnd;
    }
}
