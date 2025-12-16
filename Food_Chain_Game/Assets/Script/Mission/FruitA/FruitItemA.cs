using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static BagBase;

public class FruitItemA : MonoBehaviour, IEndDragHandler
{
    public BagItemType itemType;

    [SerializeField] private DragThrowItem _drag;
    [SerializeField] private RectTransform _rect;
    [SerializeField] private float minFallDistance = 300f;
    [SerializeField] private float maxFallDistance = 450f;

    [SerializeField] private float spawnJitterX = 20f;
    [SerializeField] private float spawnJitterY = 10f;

    [SerializeField] private float landingJitterX = 40f;
    [SerializeField] private float landingJitterY = 20f;

    [SerializeField] private float dropDuration = 1f;
    [SerializeField] private float minRotation = -90f;
    [SerializeField] private float maxRotation = 90f;

    private FruitBag _bag;
    private FruitA _mission;
    private bool _cleared;

    private void Update()
    {
        if (_cleared) return;
        if (_bag == null) return;
        if (_drag != null && _drag._isDragging)
            return;

        if (_bag.TryCatchItem(this))
        {
            _cleared = true;
        }
    }
    public void Init(FruitA mission, Vector2 spawnPos)
    {
        _mission = mission;
        _bag = mission != null ? mission.bag : null;

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        Vector2 startPos = spawnPos + new Vector2(
            Random.Range(-spawnJitterX, spawnJitterX),
            Random.Range(-spawnJitterY, spawnJitterY)
        );

        float fallDistance = Random.Range(minFallDistance, maxFallDistance);

        Vector2 endPos = startPos + new Vector2(
            Random.Range(-landingJitterX, landingJitterX),
            -fallDistance + Random.Range(-landingJitterY, landingJitterY)
        );

        _rect.anchoredPosition = startPos;
        _rect.localRotation = Quaternion.identity;

        StartCoroutine(DropCoroutine(startPos, endPos));
    }
       
    IEnumerator DropCoroutine(Vector2 start, Vector2 end)
    {
        // ·£´ý È¸Àü
        float targetZ = Random.Range(minRotation, maxRotation);
        Quaternion rotStart = _rect.localRotation;
        Quaternion rotEnd = Quaternion.Euler(0f, 0f, targetZ);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dropDuration;
            float alpha = Mathf.Clamp01(t);

            float eased = 1f - Mathf.Pow(1f - alpha, 2f);

            _rect.anchoredPosition = Vector2.Lerp(start, end, eased);
            _rect.localRotation = Quaternion.Slerp(rotStart, rotEnd, eased);

            yield return null;
        }

        _rect.anchoredPosition = end;
        _rect.localRotation = rotEnd;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_mission == null) return;
        if (itemType != BagItemType.Fruit) return;

        Vector2 screenPos = eventData.position;
        bool inBag = _mission.IsInBagArea(screenPos);

        if (inBag)
        {
            _mission.OnFruitCollected(this);
            Destroy(gameObject);
        }
    }
}
