using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlySwatter : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private Canvas canvas;

    [Header("Attack Settings")]
    [SerializeField] private float attackDuration = 0.25f;
    [SerializeField] private float recoverDuration = 0.12f;

    [SerializeField] private float backOffset = 80f;
    [SerializeField] private float sideOffsetBack = 30f;
    [SerializeField] private float sideArc = 60f;

    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float minScale = 0.9f;

    [SerializeField] private float maxSwingAngle = -35f;

    private float _nextAttackTime = 0;
    private InsectA _mission;
    private bool _isAttacking;

    public void Init(InsectA mission, RectTransform playPanel)
    {
        _mission = mission;
        this.playPanel = playPanel;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        UpdatePositionByMouse();
    }

    private void Update()
    {
        if (_mission == null || _mission.IsMissionFinished)
            return;

        if (!_isAttacking)
            UpdatePositionByMouse();
    }

    private void UpdatePositionByMouse()
    {
        if (rect == null || playPanel == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playPanel,
            Input.mousePosition,
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        Rect r = playPanel.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, r.xMin, r.xMax);
        localPoint.y = Mathf.Clamp(localPoint.y, r.yMin, r.yMax);

        rect.anchoredPosition = localPoint;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isAttacking) return;
        if (_mission == null || _mission.IsMissionFinished) return;
        if (Time.time < _nextAttackTime) return;
        
        Vector2 localTarget;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playPanel,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out localTarget
        );

        StartCoroutine(AttackCoroutine(localTarget));
    }


    private IEnumerator AttackCoroutine(Vector2 targetPos)
    {
        _isAttacking = true;

        Vector3 baseScale = rect.localScale;
        Quaternion baseRot = rect.localRotation;
        Vector2 startPos = rect.anchoredPosition;

        Vector2 dir = (targetPos - startPos);
        if (dir.sqrMagnitude < 0.0001f)
            dir = new Vector2(0f, -1f);
        else
            dir.Normalize();

        Vector2 side = new Vector2(-dir.y, dir.x);
        if (Random.value < 0.5f)
            side = -side;

        Vector2 backswingPos = startPos - dir * backOffset + side * sideOffsetBack;

        Vector2 control = (backswingPos + targetPos) * 0.5f + side * sideArc;

        float backTime = attackDuration * 0.35f;
        float swingTime = attackDuration - backTime;

        float t = 0f;
        while (t < backTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / backTime);

            rect.anchoredPosition = Vector2.Lerp(startPos, backswingPos, a);

            rect.localScale = Vector3.Lerp(baseScale, baseScale * maxScale, a);

            float angle = Mathf.Lerp(0f, maxSwingAngle * 0.3f, a);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / swingTime);

            Vector2 p =
                (1 - a) * (1 - a) * backswingPos +
                2 * (1 - a) * a * control +
                a * a * targetPos;

            rect.anchoredPosition = p;

            rect.localScale = Vector3.Lerp(baseScale * maxScale, baseScale * minScale, a);

            float angle = Mathf.Lerp(maxSwingAngle * 0.3f, maxSwingAngle, a);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        _mission.OnSwatterHit(targetPos);

        t = 0f;
        Vector3 hitScale = rect.localScale;
        Quaternion hitRot = rect.localRotation;

        while (t < recoverDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / recoverDuration);

            rect.localScale = Vector3.Lerp(hitScale, baseScale, a);
            rect.localRotation = Quaternion.Slerp(hitRot, baseRot, a);

            yield return null;
        }

        rect.anchoredPosition = targetPos;
        rect.localScale = baseScale;
        rect.localRotation = baseRot;

        _nextAttackTime = Time.time + 0.7f;

        _isAttacking = false;
    }
}


