using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteMask))]
public class FOVMaskController2D : MonoBehaviour
{
    [Header("Refs")]
    public LayerMask occluderMask;
    public float viewRadius = 6f;

    [Header("Quality")]
    [Range(30, 720)] public int rayCount = 240;
    public float updateInterval = 0.05f;

    [SerializeField] private Transform target;
    private SpriteMask _mask;
    private Sprite _sprite;

    private Vector2[] _vertices2D;
    private ushort[] _triangles;

    private Vector2[] _worldPoly;
    public IReadOnlyList<Vector2> WorldPolygon => _worldPoly;
    public Vector2 WorldOrigin { get; private set; }

    private float _t;

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

        AllocateBuffers();
    }

    void Update()
    {
        if (!target) return;

        _t += Time.deltaTime;
        if (_t < updateInterval) return;
        _t = 0f;

        BuildMask();
    }
    public void InitLocalPlayer(Transform local)
    {
        target = local;
    }
    void AllocateBuffers()
    {
        _worldPoly = new Vector2[rayCount];

        int vCount = rayCount + 1;
        _vertices2D = new Vector2[vCount];

        _triangles = new ushort[rayCount * 3];
        for (int i = 0; i < rayCount; i++)
        {
            int tri = i * 3;
            _triangles[tri + 0] = 0;
            _triangles[tri + 1] = (ushort)(i + 1);
            _triangles[tri + 2] = (ushort)((i + 2) <= rayCount ? (i + 2) : 1);
        }
    }

    void BuildMask()
    {
        transform.position = target.position;

        Rect rect = _sprite.rect;
        Vector2 pivot = _sprite.pivot;
        float ppu = _sprite.pixelsPerUnit;

        _vertices2D[0] = pivot;

        float step = 360f / rayCount;
        Vector2 origin = target.position;

        for (int i = 0; i < rayCount; i++)
        {
            float rad = (step * i) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewRadius, occluderMask);

            Vector2 worldPoint = hit.collider ? hit.point : origin + dir * viewRadius;

            WorldOrigin = origin;
            _worldPoly[i] = worldPoint;

            Vector2 localWorld = worldPoint - origin;

            Vector2 pixel = pivot + localWorld * ppu;

            pixel.x = Mathf.Clamp(pixel.x, 0f, rect.width);
            pixel.y = Mathf.Clamp(pixel.y, 0f, rect.height);

            _vertices2D[i + 1] = pixel;
        }

        _sprite.OverrideGeometry(_vertices2D, _triangles);

        _mask.sprite = _sprite;
    }

    void OnValidate()
    {
        // 실행중 변경시
        if (rayCount < 30) rayCount = 30;
        if (Application.isPlaying)
            AllocateBuffers();
    }
}
