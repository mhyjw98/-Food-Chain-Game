using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LocalVisionLight : NetworkBehaviour
{
    [SerializeField] private GamePlayer localPlayer;
    [SerializeField] private Light2D visionLight;
    [SerializeField] private FOVMaskController2D fovMask;

    [SerializeField] private float fovOffset = 0.1f;
    [SerializeField] private float outerOffset = 0.2f;

    [SerializeField] private float transitionSeconds = 5f;

    private Coroutine _routine;
    public Light2D GetLight() => visionLight;
    public override void OnStartClient()
    {
        if (visionLight) visionLight.gameObject.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        if (visionLight) visionLight.gameObject.SetActive(true);
        if (!fovMask) fovMask = FindObjectOfType<FOVMaskController2D>();
    }

    public void InitFOV(FOVMaskController2D fov)
    {
        if (fovMask == null) fovMask = fov;
    }

    public void TransitionToVision(float targetVision, bool isNight)
    {
        if (!visionLight || !localPlayer || !visionLight.gameObject.activeInHierarchy || !localPlayer.gameObject.activeInHierarchy) return;

        if (_routine != null) StopCoroutine(_routine);       
        _routine = StartCoroutine(TransitionRoutine(targetVision, isNight));
    }

    private IEnumerator TransitionRoutine(float targetVision, bool isNight)
    {
        float startFov = fovMask.viewRadius; 
        float startOuter = visionLight.pointLightOuterRadius;
        float startInner = visionLight.pointLightInnerRadius;

        float endFov = targetVision + fovOffset;
        float endOuter = targetVision + outerOffset;

        var globalLight = fovMask.GetComponent<VisionUIController>().globalLight;
        
        float startIntensity = globalLight.intensity;
        float endIntensity = isNight ? 0.2f : 0.9f;

        float startLocalIntensity = visionLight.intensity;
        float endLocalIntensity = isNight ? 0.8f : 0.1f;

        float t = 0f;
        while (t < transitionSeconds)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / transitionSeconds);

            u = u * u * (3f - 2f * u);

            fovMask.viewRadius = Mathf.Lerp(startFov, endFov, u);
            visionLight.pointLightOuterRadius = Mathf.Lerp(startOuter, endOuter, u);
            visionLight.pointLightInnerRadius = Mathf.Lerp(startInner, targetVision, u);

            globalLight.intensity = Mathf.Lerp(startIntensity, endIntensity, u);
            visionLight.intensity = Mathf.Lerp(startLocalIntensity, endLocalIntensity, u);

            yield return null;
        }

        fovMask.viewRadius = endFov;
        visionLight.pointLightOuterRadius = endOuter;
        visionLight.pointLightInnerRadius = targetVision;
        globalLight.intensity = endIntensity;
        visionLight.intensity = endLocalIntensity;

        _routine = null;
    }
}
