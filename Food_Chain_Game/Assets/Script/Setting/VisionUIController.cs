using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VisionUIController : MonoBehaviour
{
    public static VisionUIController Instance { get; private set; }

    public FOVMaskController2D fov;
    public FOVMaskController2D sharedFov;

    [SerializeField] private Transform local;
    [SerializeField] private LayerMask occluderMask;
    [SerializeField] private float interval = 0.05f;

    public Light2D globalLight;
    public GamePlayer localPlayer;
    private float _t;

    private void Awake()
    {
        Instance = this;
        if (!local) local = transform;
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

        var polyMain = fov.WorldPolygon;
        if (polyMain == null || polyMain.Count < 3) return;

        IReadOnlyList<Vector2> polyShared = null;
        if (sharedFov != null)
        {
            var p = sharedFov.WorldPolygon;
            if (p != null && p.Count >= 3) polyShared = p;
        }

        Vector2 eye = local.position;

        for (int i = 0; i < VisionAnchor.All.Count; i++)
        {
            var a = VisionAnchor.All[i];
            if (!a || !a.head || !a.uiVisibility) continue;

            Vector2 head = a.head.position;
            bool visible =
                IsHeadVisible(eye, head, polyMain) ||
                (polyShared != null && IsHeadVisible(eye, head, polyShared));

            a.uiVisibility.SetVisible(visible);
        }
    }

    private bool IsHeadVisible(Vector2 eye, Vector2 head, IReadOnlyList<Vector2> poly)
    {
        int viewRadius = 11;
        if (localPlayer != null && GameMamager.Instance != null)
        {
            if (localPlayer.isAlive && GameMamager.Instance.IsNightPhase)
                viewRadius = 6;
        }
        if ((head - eye).sqrMagnitude > viewRadius * viewRadius)
            return false;

        var hit = Physics2D.Linecast(eye, head, occluderMask);
        if (hit.collider != null) return false;

        return PointInPolygon(head, poly);
    }

    private static bool PointInPolygon(Vector2 p, IReadOnlyList<Vector2> poly)
    {
        bool inside = false;
        int n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[j];

            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / ((b.y - a.y) + 1e-6f) + a.x);

            if (intersect) inside = !inside;
        }
        return inside;
    }
}
