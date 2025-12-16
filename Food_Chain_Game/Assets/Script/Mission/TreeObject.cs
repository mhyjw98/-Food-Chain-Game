using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TreeObject : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private Transform treeTf;

    [SerializeField] private float cooldown = 0.2f;
    [SerializeField] private float shakeAngle = 8f;
    [SerializeField] private float shakeDuration = 0.25f; 
    [SerializeField] private int shakeCycles = 2;

    private float _lastShakeTime;
    private bool _isShaking;
    private Quaternion _originalRotation;

    public Action OnTreeShaken;

    private void Awake()
    {
        if (treeTf == null)
            treeTf = transform;

        _originalRotation = treeTf.localRotation;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryShake();
    }

    public void OnDrag(PointerEventData eventData)
    {
        TryShake();
    }

    private void TryShake()
    {
        if (Time.time - _lastShakeTime < cooldown)
            return;

        _lastShakeTime = Time.time;

        OnTreeShaken?.Invoke();

        if (!_isShaking)
            StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        _isShaking = true;
        _originalRotation = treeTf.localRotation;

        float halfCycleDuration = shakeDuration / (shakeCycles * 2f);

        for (int i = 0; i < shakeCycles; i++)
        {
            yield return RotateToAngle(shakeAngle, halfCycleDuration);
            yield return RotateToAngle(-shakeAngle, halfCycleDuration);
        }

        float t = 0f;
        Quaternion startRot = treeTf.localRotation;
        while (t < 1f)
        {
            t += Time.deltaTime / halfCycleDuration;
            treeTf.localRotation = Quaternion.Slerp(startRot, _originalRotation, t);
            yield return null;
        }

        treeTf.localRotation = _originalRotation;
        _isShaking = false;
    }

    private IEnumerator RotateToAngle(float angle, float duration)
    {
        Quaternion start = treeTf.localRotation;
        Quaternion target = _originalRotation * Quaternion.Euler(0f, 0f, angle);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            treeTf.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }
    }
}
