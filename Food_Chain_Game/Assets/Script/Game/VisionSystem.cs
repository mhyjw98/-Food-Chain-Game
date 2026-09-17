using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisionSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D globalLight;

    public AnimationCurve globalIntensityCurve =
        new AnimationCurve(
            new Keyframe(0f, 1.0f),
            new Keyframe(0.5f, 0.12f),
            new Keyframe(1f, 1.0f));

    public AnimationCurve preyRadiusCurve =
        new AnimationCurve(
            new Keyframe(0f, 16f),
            new Keyframe(0.5f, 7f),
            new Keyframe(1f, 16f));

    [Header("Role Multipliers")]
    public float predatorRadiusMultiplier = 0.80f;
    public float squirrelRadiusMultiplier = 1.0f;

    [Header("Smoothing")]
    public float smoothSeconds = 0.25f;

    private Light2D _localPlayerLight;
    private GamePlayer _localGamePlayer;

    private float _globalVel;
    private float _radiusVel;

    private float _t01;
    private float dayCycleSeconds = 40f;

    void Update()
    {
        BindLocalOnce();

        // 시간 진행
        if (dayCycleSeconds <= 1f) dayCycleSeconds = 1f;
        _t01 += Time.deltaTime / dayCycleSeconds;
        if (_t01 > 1f) _t01 -= 1f;

        // 곡선 평가
        float targetGlobal = globalIntensityCurve.Evaluate(_t01);
        float targetRadius = preyRadiusCurve.Evaluate(_t01);

        // 역할 보정
        if (_localGamePlayer != null)
        {
            bool isPred = _localGamePlayer.isPredator;
            bool isSquirrel = (_localGamePlayer.animalType == AnimalType.Squirrel); // enum 맞춰 수정

            if (isSquirrel)
            {
                targetRadius *= squirrelRadiusMultiplier; // 기본 1.0
            }
            else if (isPred)
            {
                targetRadius *= predatorRadiusMultiplier;
            }
        }

        // 부드럽게 반영
        if (globalLight)
            globalLight.intensity = Mathf.SmoothDamp(globalLight.intensity, targetGlobal, ref _globalVel, smoothSeconds);

        if (_localPlayerLight)
        {
            float current = _localPlayerLight.pointLightOuterRadius;
            _localPlayerLight.pointLightOuterRadius =
                Mathf.SmoothDamp(current, targetRadius, ref _radiusVel, smoothSeconds);
        }
    }

    private void BindLocalOnce()
    {
        if (_localPlayerLight != null && _localGamePlayer != null) return;

        var local = NetworkClient.localPlayer;
        if (!local) return;

        _localGamePlayer = local.GetComponent<GamePlayer>();

        var lv = local.GetComponent<LocalVisionLight>();
        if (lv != null) _localPlayerLight = lv.GetLight();
    }

    // 디버그용: 시간을 특정 위치로 점프
    [ContextMenu("Set Midnight (t=0.5)")]
    public void SetMidnight() => _t01 = 0.5f;

    [ContextMenu("Set Noon (t=0)")]
    public void SetNoon() => _t01 = 0f;
}
