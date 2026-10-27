using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 로컬 플레이어의 시야 영역(SpriteMask)을 만든다.
// 빛(Light2D)의 그림자와 어긋나지 않도록, 빛이 쓰는 것과 같은 그림자 오브젝트(ShadowCaster2D)를 벽으로 삼고
// 고정 각도가 아니라 벽의 꼭짓점을 향해 계산한다
[RequireComponent(typeof(SpriteMask))]
public class FOVMaskController2D : MonoBehaviour
{
    [Header("Refs")]
    public float viewRadius = 6f;
    [Tooltip("켜면 벽을 무시하고 원형으로 본다(유령)")]
    public bool ignoreOccluders;

    [Header("Quality")]
    [Tooltip("원형 가장자리를 그리는 기본 간격(도). 작을수록 원이 매끄럽다")]
    [SerializeField, Range(0.5f, 15f)] private float arcStepDegrees = 3f;
    [Tooltip("벽 꼭짓점의 양옆으로 벌려 계산하는 각도(도). 모서리 뒤쪽을 잡기 위한 값")]
    [SerializeField] private float cornerOffsetDegrees = 0.01f;
    [Tooltip("그림자 오브젝트 목록을 다시 찾는 간격(초)")]
    [SerializeField] private float casterRefreshInterval = 0.5f;
    [Tooltip("마스크 그림이 시야 반경보다 얼마나 넉넉해야 하는지(배율)")]
    [SerializeField] private float spriteMargin = 1.05f;
    [SerializeField] private int initialPointCapacity = 512;

    [SerializeField] private Transform target;
    private SpriteMask _mask;
    private Sprite _sprite;

    private Vector2[] _vertices2D;
    private ushort[] _triangles;

    private Vector2[] _worldPoly;
    private int _polyCount;
    private float[] _angles;
    private int _angleCount;

    public Vector2 WorldOrigin { get; private set; }

    // 그림자를 만드는 도형 하나(월드 좌표의 닫힌 다각형)
    private class Caster
    {
        public ShadowCaster2D shadow;
        public Transform transform;
        public Vector2[] local;
        public Vector2[] world;
    }

    private readonly List<Caster> _casters = new();
    private readonly List<Caster> _activeCasters = new();
    private float _nextCasterRefresh;

    // IsVisibleFrom이 마지막으로 벽 목록을 추린 기준(같은 프레임·같은 위치면 다시 추리지 않는다)
    private int _queryFrame = -1;
    private Vector2 _queryOrigin;
    private float _queryRadius;

    void Awake()
    {
        _mask = GetComponent<SpriteMask>();

        if (_mask.sprite == null)
        {
            Debug.LogError("[FOV] SpriteMask.sprite is null. Assign a large square sprite to the SpriteMask.");
            enabled = false;
            return;
        }
        _sprite = Instantiate(_mask.sprite);
        _mask.sprite = _sprite;

        AllocateBuffers(Mathf.Max(64, initialPointCapacity));
    }

    // 캐릭터 이동이 끝난 뒤 매 프레임 계산한다(빛도 매 프레임 그려지므로 같은 위치를 쓴다)
    void LateUpdate()
    {
        if (!target) return;

        if (Time.unscaledTime >= _nextCasterRefresh)
        {
            _nextCasterRefresh = Time.unscaledTime + casterRefreshInterval;
            RefreshCasters();
        }

        BuildMask();
    }

    public void InitLocalPlayer(Transform local)
    {
        target = local;
    }

    // 월드 좌표의 점이 지금 시야 영역 안에 있는지
    public bool ContainsPoint(Vector2 p)
    {
        if (_polyCount < 3) return false;

        bool inside = false;
        for (int i = 0, j = _polyCount - 1; i < _polyCount; j = i++)
        {
            Vector2 a = _worldPoly[i];
            Vector2 b = _worldPoly[j];

            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / ((b.y - a.y) + 1e-6f) + a.x);

            if (intersect) inside = !inside;
        }
        return inside;
    }

    // 다른 위치(예: 시야를 공유하는 파트너)에서 그 점이 보이는지. 내 시야와 같은 벽 판정을 쓴다.
    // 내 시야 마스크(_worldPoly)는 건드리지 않는다
    public bool IsVisibleFrom(Vector2 origin, float radius, Vector2 point)
    {
        Vector2 delta = point - origin;
        float dist = delta.magnitude;
        if (dist > radius) return false;
        if (dist < 0.0001f) return true;

        if (_queryFrame != Time.frameCount || _queryOrigin != origin || !Mathf.Approximately(_queryRadius, radius))
        {
            CollectActiveCasters(origin, radius);
            _queryFrame = Time.frameCount;
            _queryOrigin = origin;
            _queryRadius = radius;
        }

        return CastDistance(origin, delta / dist, radius) >= dist - 0.01f;
    }

    void AllocateBuffers(int capacity)
    {
        _worldPoly = new Vector2[capacity];
        _angles = new float[capacity];
        _vertices2D = new Vector2[capacity + 1];
        _triangles = new ushort[capacity * 3];
    }

    void RefreshCasters()
    {
        _casters.Clear();

        foreach (var shadow in FindObjectsByType<ShadowCaster2D>(FindObjectsSortMode.None))
        {
            if (shadow == null) continue;

            // 그림자 모양은 콜라이더에서 가져오도록 설정돼 있으므로 콜라이더를 먼저 본다
            if (shadow.TryGetComponent(out BoxCollider2D box))
            {
                Vector2 half = box.size * 0.5f;
                Vector2 c = box.offset;
                AddCaster(shadow, new[]
                {
                    c + new Vector2(-half.x, -half.y),
                    c + new Vector2(half.x, -half.y),
                    c + new Vector2(half.x, half.y),
                    c + new Vector2(-half.x, half.y),
                });
            }
            else if (shadow.TryGetComponent(out PolygonCollider2D polygon))
            {
                for (int i = 0; i < polygon.pathCount; i++)
                    AddCaster(shadow, polygon.GetPath(i));
            }
            else
            {
                var path = shadow.shapePath;
                if (path == null) continue;

                var points = new Vector2[path.Length];
                for (int i = 0; i < path.Length; i++) points[i] = path[i];
                AddCaster(shadow, points);
            }
        }
    }

    void AddCaster(ShadowCaster2D shadow, Vector2[] localPoints)
    {
        if (localPoints == null || localPoints.Length < 2) return;

        _casters.Add(new Caster
        {
            shadow = shadow,
            transform = shadow.transform,
            local = localPoints,
            world = new Vector2[localPoints.Length],
        });
    }

    // 이번 프레임에 시야에 걸칠 수 있는 그림자 도형만 추린다
    void CollectActiveCasters(Vector2 origin, float radius)
    {
        _activeCasters.Clear();
        if (ignoreOccluders) return;

        for (int i = 0; i < _casters.Count; i++)
        {
            var caster = _casters[i];
            if (caster.shadow == null || !caster.shadow.isActiveAndEnabled || !caster.shadow.castsShadows) continue;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int k = 0; k < caster.local.Length; k++)
            {
                Vector2 w = caster.transform.TransformPoint(caster.local[k]);
                caster.world[k] = w;
                min = Vector2.Min(min, w);
                max = Vector2.Max(max, w);
            }

            // 도형을 감싸는 사각형이 시야 원 밖이면 건너뛴다
            float dx = Mathf.Max(min.x - origin.x, 0f, origin.x - max.x);
            float dy = Mathf.Max(min.y - origin.y, 0f, origin.y - max.y);
            if (dx * dx + dy * dy > radius * radius) continue;

            _activeCasters.Add(caster);
        }
    }

    void AddAngle(float radians)
    {
        if (_angleCount >= _angles.Length)
        {
            int capacity = _angles.Length * 2;
            Array.Resize(ref _angles, capacity);
            _worldPoly = new Vector2[capacity];
            _vertices2D = new Vector2[capacity + 1];
            _triangles = new ushort[capacity * 3];
        }

        const float TwoPi = Mathf.PI * 2f;
        radians %= TwoPi;
        if (radians < 0f) radians += TwoPi;
        _angles[_angleCount++] = radians;
    }

    void CollectAngles(Vector2 origin, float radius)
    {
        _angleCount = 0;

        float step = Mathf.Clamp(arcStepDegrees, 0.5f, 15f) * Mathf.Deg2Rad;
        int baseCount = Mathf.CeilToInt(Mathf.PI * 2f / step);
        for (int i = 0; i < baseCount; i++)
            AddAngle(i * (Mathf.PI * 2f / baseCount));

        float offset = Mathf.Max(0.0001f, cornerOffsetDegrees) * Mathf.Deg2Rad;
        float radiusSqr = radius * radius;

        for (int c = 0; c < _activeCasters.Count; c++)
        {
            var points = _activeCasters[c].world;
            for (int k = 0; k < points.Length; k++)
            {
                Vector2 a = points[k] - origin;
                Vector2 b = points[(k + 1) % points.Length] - origin;

                // 꼭짓점: 바로 그 방향이 아니라 양옆으로 조금 벌린 두 방향(꼭짓점 앞과 뒤를 각각 잡는다)
                if (a.sqrMagnitude <= radiusSqr)
                {
                    float angle = Mathf.Atan2(a.y, a.x);
                    AddAngle(angle - offset);
                    AddAngle(angle + offset);
                }

                // 벽의 변이 시야 원과 만나는 지점(원 가장자리에서 벽으로 넘어가는 곳)
                Vector2 e = b - a;
                float qa = e.sqrMagnitude;
                if (qa < 1e-10f) continue;

                float qb = 2f * Vector2.Dot(a, e);
                float qc = a.sqrMagnitude - radiusSqr;
                float disc = qb * qb - 4f * qa * qc;
                if (disc < 0f) continue;

                float root = Mathf.Sqrt(disc);
                for (int s = -1; s <= 1; s += 2)
                {
                    float u = (-qb + s * root) / (2f * qa);
                    if (u < 0f || u > 1f) continue;

                    Vector2 hit = a + e * u;
                    float angle = Mathf.Atan2(hit.y, hit.x);
                    AddAngle(angle - offset);
                    AddAngle(angle + offset);
                }
            }
        }

        Array.Sort(_angles, 0, _angleCount);
    }

    // 한 방향으로 빛이 닿는 거리.
    // 빛은 그림자 오브젝트 자체는 비추고 그 뒤부터 가리므로, 처음 만난 도형을 빠져나가는 지점까지를 시야로 본다
    float CastDistance(Vector2 origin, Vector2 dir, float radius)
    {
        float bestEnter = radius;
        float bestStop = radius;

        for (int c = 0; c < _activeCasters.Count; c++)
        {
            var caster = _activeCasters[c];
            var points = caster.world;

            int count = 0;
            float t1 = float.MaxValue;
            float t2 = float.MaxValue;

            for (int k = 0; k < points.Length; k++)
            {
                Vector2 a = points[k] - origin;
                Vector2 e = points[(k + 1) % points.Length] - points[k];

                float denom = dir.x * e.y - dir.y * e.x;
                if (Mathf.Abs(denom) < 1e-9f) continue;

                float t = (a.x * e.y - a.y * e.x) / denom;
                float u = (a.x * dir.y - a.y * dir.x) / denom;

                // 변의 끝점은 다음 변의 시작점이므로 한쪽에서만 센다
                if (t < 0f || u < 0f || u >= 1f) continue;

                count++;
                if (t < t1) { t2 = t1; t1 = t; }
                else if (t < t2) { t2 = t; }
            }

            if (count == 0 || t1 >= bestEnter) continue;

            bestEnter = t1;

            // 짝수 번 만나면 밖에서 들어갔다 나온 것(두 번째가 나가는 지점). 홀수면 도형 안에서 시작한 것
            bool passThrough = !caster.shadow.selfShadows && count % 2 == 0;
            bestStop = passThrough ? t2 : t1;
        }

        return Mathf.Min(bestStop, radius);
    }

    void BuildMask()
    {
        Vector2 origin = target.position;
        float radius = Mathf.Max(0.01f, viewRadius);

        WorldOrigin = origin;
        transform.position = target.position;

        // 마스크 그림(정사각형)이 시야 반경보다 작으면 가장자리가 잘린다. 모자라면 오브젝트를 키워서 맞춘다
        Rect rect = _sprite.rect;
        Vector2 pivot = _sprite.pivot;
        float ppu = _sprite.pixelsPerUnit;

        float halfExtent = Mathf.Min(pivot.x, rect.width - pivot.x, pivot.y, rect.height - pivot.y) / ppu;
        float scale = halfExtent > 0f ? Mathf.Max(1f, radius * Mathf.Max(1f, spriteMargin) / halfExtent) : 1f;

        Transform parent = transform.parent;
        float parentScale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.x) : 1f;
        transform.localScale = new Vector3(scale / parentScale, scale / parentScale, 1f);

        CollectActiveCasters(origin, radius);
        _queryFrame = -1;   // 벽 목록이 내 위치 기준으로 바뀌었으므로 IsVisibleFrom이 다시 추리게 한다
        CollectAngles(origin, radius);

        _polyCount = _angleCount;
        _vertices2D[0] = pivot;

        float pixelsPerWorld = ppu / scale;

        for (int i = 0; i < _polyCount; i++)
        {
            Vector2 dir = new Vector2(Mathf.Cos(_angles[i]), Mathf.Sin(_angles[i]));
            Vector2 local = dir * CastDistance(origin, dir, radius);

            _worldPoly[i] = origin + local;

            Vector2 pixel = pivot + local * pixelsPerWorld;
            pixel.x = Mathf.Clamp(pixel.x, 0f, rect.width);
            pixel.y = Mathf.Clamp(pixel.y, 0f, rect.height);
            _vertices2D[i + 1] = pixel;
        }

        for (int i = 0; i < _polyCount; i++)
        {
            int tri = i * 3;
            _triangles[tri + 0] = 0;
            _triangles[tri + 1] = (ushort)(i + 1);
            _triangles[tri + 2] = (ushort)(i + 1 < _polyCount ? i + 2 : 1);
        }

        // 배열 크기를 매 프레임 바꾸지 않도록, 쓰지 않는 칸은 넓이가 없는 삼각형으로 채운다
        for (int i = _polyCount + 1; i < _vertices2D.Length; i++)
            _vertices2D[i] = pivot;
        for (int i = _polyCount * 3; i < _triangles.Length; i++)
            _triangles[i] = 0;

        _sprite.OverrideGeometry(_vertices2D, _triangles);

        _mask.sprite = _sprite;
    }
}
