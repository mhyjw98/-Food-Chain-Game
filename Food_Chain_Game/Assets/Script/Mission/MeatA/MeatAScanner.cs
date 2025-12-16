using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MeatAScanner : MonoBehaviour, IPointerDownHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private RectTransform scanLineRect;
    [SerializeField] private Image scanLineImage;
    [SerializeField] private TextMeshProUGUI messageText; 

    [Header("Line")]
    [SerializeField] private float lineThickness = 6f;

    [Header("Scan")]
    [SerializeField] private float scanDuration = 3f;
    [SerializeField] private int verticalBins = 20;
    [SerializeField] private float minCoverageRatio = 0.90f;
    [SerializeField] private float leadTolerance = 0.18f;
    [SerializeField] private float maxProgressStep = 0.12f;

    [Header("UI")]
    [SerializeField] private float cancelLockTime = 0.5f;

    private Canvas _canvas;
    private MeatA _mission;
    private RectTransform _playPanel;
    private RectTransform _meatArea;

    private bool _scanning;
    private bool _locked;

    private float _elapsed;

    private bool _dirTopToBottom;
    private bool[] _visited;
    private int _visitedCount;

    private float _prevProgress;
    private bool _hasPrevProgress;

    public void Init(MeatA mission, RectTransform playPanel, RectTransform meatArea, Canvas canvas)
    {
        _mission = mission;
        _playPanel = playPanel;
        _meatArea = meatArea;
        _canvas = canvas;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (scanLineRect != null)
        {
            if (scanLineImage == null)
                scanLineImage = scanLineRect.GetComponent<Image>();

            if (scanLineImage != null)
                scanLineImage.color = Color.red;

            SetupLineSizeToMeat();
        }

        if (_visited == null || _visited.Length != Mathf.Max(4, verticalBins))
            _visited = new bool[Mathf.Max(4, verticalBins)];

        ResetInternal();
        SetMessage("");
    }

    private void Update()
    {
        if (_mission == null || _mission.IsMissionFinished) return;

        FollowMouseFreeInsidePlayPanel();

        if (!_scanning) return;

        _elapsed += Time.deltaTime;

        float remain = Mathf.Max(0f, scanDuration - _elapsed);
        int count = Mathf.Clamp(Mathf.CeilToInt(remain), 0, 3);
        SetMessage(count.ToString());

        if (!TryGetLineCenterYInMeat(out float centerY, out float meatMinY, out float meatMaxY))
        {
            if (_elapsed >= scanDuration)
                FinishScan();
            return;
        }

        float progress = ComputeProgress(centerY, meatMinY, meatMaxY, _dirTopToBottom);

        if (_hasPrevProgress)
        {
            float dp = Mathf.Abs(progress - _prevProgress);
            if (dp > maxProgressStep)
            {
                CancelScan("너무 빠름");
                return;
            }
        }
        _prevProgress = progress;
        _hasPrevProgress = true;

        float expected = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, scanDuration));
        if (progress > expected + leadTolerance)
        {
            CancelScan("너무 빠름");
            return;
        }

        int idx = Mathf.Clamp(Mathf.FloorToInt(progress * verticalBins), 0, verticalBins - 1);
        if (!_visited[idx])
        {
            _visited[idx] = true;
            _visitedCount++;
        }

        if (_elapsed >= scanDuration)
        {
            FinishScan();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_mission == null || _mission.IsMissionFinished) return;
        if (_locked) return;
        if (_scanning) return;

        if (!TryGetLineCenterYInMeat(out float centerY, out float meatMinY, out float meatMaxY))
        {
            StartScan(defaultTopToBottom: true);
            return;
        }

        float distToTop = Mathf.Abs(meatMaxY - centerY);
        float distToBottom = Mathf.Abs(centerY - meatMinY);

        bool topToBottom = distToTop <= distToBottom;

        StartScan(topToBottom);
    }

    private void StartScan(bool defaultTopToBottom)
    {
        _scanning = true;
        _elapsed = 0f;

        _dirTopToBottom = defaultTopToBottom;

        _visitedCount = 0;
        for (int i = 0; i < _visited.Length; i++)
            _visited[i] = false;

        _hasPrevProgress = false;
        _prevProgress = 0f;

        SetupLineSizeToMeat();
        SetMessage("3");

        // AudioManager.Instance.PlayUISfx(...);
    }

    private void FinishScan()
    {
        _scanning = false;

        float coverage = (float)_visitedCount / Mathf.Max(1, verticalBins);

        if (coverage >= minCoverageRatio)
        {
            SetMessage("안 전");

            // AudioManager.Instance.PlayEffectSfx(...);

            _mission.OnScanSuccess();
            return;
        }

        CancelScan("스캔 부족");
    }

    private void CancelScan(string msg)
    {
        _scanning = false;
        ResetInternal();
        SetMessage(msg);

        StartCoroutine(LockRoutine());
    }

    private IEnumerator LockRoutine()
    {
        _locked = true;
        yield return new WaitForSecondsRealtime(cancelLockTime);
        _locked = false;
        SetMessage("");
    }

    private void ResetInternal()
    {
        _elapsed = 0f;
        _hasPrevProgress = false;
        _prevProgress = 0f;

        _visitedCount = 0;
        if (_visited != null)
        {
            for (int i = 0; i < _visited.Length; i++)
                _visited[i] = false;
        }
    }

    private void FollowMouseFreeInsidePlayPanel()
    {
        if (_playPanel == null || rect == null) return;

        Camera cam = null;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = _canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _playPanel,
                Input.mousePosition,
                cam,
                out Vector2 local))
            return;

        local = ClampInside(_playPanel, rect, local);
        rect.anchoredPosition = local;
    }

    private void SetupLineSizeToMeat()
    {
        if (_meatArea == null || scanLineRect == null) return;

        float w = _meatArea.rect.width;
        scanLineRect.sizeDelta = new Vector2(w, lineThickness);
    }

    private bool TryGetLineCenterYInMeat(out float centerY, out float meatMinY, out float meatMaxY)
    {
        centerY = 0f;

        meatMinY = 0f;
        meatMaxY = 0f;

        if (_meatArea == null || scanLineRect == null) return false;

        meatMinY = _meatArea.rect.yMin;
        meatMaxY = _meatArea.rect.yMax;

        Vector3 worldCenter = scanLineRect.TransformPoint(scanLineRect.rect.center);
        Vector3 local = _meatArea.InverseTransformPoint(worldCenter);

        Vector2 p = new Vector2(local.x, local.y);
        if (!_meatArea.rect.Contains(p)) return false;

        centerY = local.y;
        return true;
    }

    private static float ComputeProgress(float centerY, float meatMinY, float meatMaxY, bool topToBottom)
    {
        float p = topToBottom
            ? Mathf.InverseLerp(meatMaxY, meatMinY, centerY)   // 위 0 아래 1
            : Mathf.InverseLerp(meatMinY, meatMaxY, centerY);  // 아래 0 위 1

        return Mathf.Clamp01(p);
    }

    private void SetMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
    }

    private static Vector2 ClampInside(RectTransform area, RectTransform item, Vector2 p)
    {
        Rect r = area.rect;

        Vector2 size = item.rect.size;
        Vector2 pivot = item.pivot;

        float left = size.x * pivot.x;
        float right = size.x * (1f - pivot.x);
        float bottom = size.y * pivot.y;
        float top = size.y * (1f - pivot.y);

        p.x = Mathf.Clamp(p.x, r.xMin + left, r.xMax - right);
        p.y = Mathf.Clamp(p.y, r.yMin + bottom, r.yMax - top);

        return p;
    }
}
