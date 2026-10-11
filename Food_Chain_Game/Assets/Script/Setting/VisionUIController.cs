using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisionUIController : MonoBehaviour
{
    public static VisionUIController Instance { get; private set; }

    public FOVMaskController2D fov;
    public FOVMaskController2D sharedFov;

    [SerializeField] private float interval = 0.05f;

    public Light2D globalLight;
    public GamePlayer localPlayer;
    private float _t;

    private void Awake()
    {
        Instance = this;
    }
    public void SetSharedFov(FOVMaskController2D newShared)
    {
        sharedFov = newShared;
    }
    private void Update()
    {
        if (!fov) return;

        _t += Time.deltaTime;
        if (_t < interval) return;
        _t = 0f;

        for (int i = 0; i < VisionAnchor.All.Count; i++)
        {
            var a = VisionAnchor.All[i];
            if (!a || !a.head || !a.uiVisibility) continue;

            // 캐릭터가 보이는 영역(시야 마스크)과 같은 도형으로 판정한다. 반경과 벽 가림이 모두 들어 있다
            Vector2 head = a.head.position;
            bool visible = fov.ContainsPoint(head) || (sharedFov != null && sharedFov.ContainsPoint(head));

            a.uiVisibility.SetVisible(visible);
        }
    }
}
