using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CropsItemB : MonoBehaviour, IPointerDownHandler
{
    public enum PlantState
    {
        Growing,
        Mature,
        Wilted,
        Harvested
    }

    [Header("Visual")]
    [SerializeField] private Image icon;
    [SerializeField] private Sprite growingSprite;
    [SerializeField] private Sprite matureSprite;
    [SerializeField] private Sprite wiltedSprite;

    [Header("Timing")]
    [SerializeField] private float baseGrowTime = 7f;
    [SerializeField] private float baseMatureDuration = 3f;
    [SerializeField] private float baseWiltDuration = 5f;

    private float _growTime;
    private float _matureDuration;
    private float _wiltDuration;

    private CropsB _mission;
    private RectTransform _rect;
    private float _age;
    private PlantState _state = PlantState.Growing;
    private bool _harvested;

    public PlantState State => _state;

    public void Init(CropsB mission, float timeScale)
    {
        _mission = mission;

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _age = 0f;
        _harvested = false;

        _growTime = baseGrowTime * timeScale;
        _matureDuration = baseMatureDuration * timeScale;
        _wiltDuration = baseWiltDuration * timeScale;

        _state = PlantState.Growing;
        UpdateVisual();
    }

    private void Awake()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_mission == null) return;
        if (_harvested) return;
        if (_mission.IsMissionFinished) return;

        _age += Time.deltaTime;

        float tGrowEnd = _growTime;
        float tMatureEnd = _growTime + _matureDuration;
        float tWiltEnd = _growTime + _matureDuration + _wiltDuration;

        PlantState newState;

        if (_age < tGrowEnd)
        {
            newState = PlantState.Growing;
        }
        else if (_age < tMatureEnd)
        {
            newState = PlantState.Mature;
        }
        else
        {
            newState = PlantState.Wilted;
        }

        if (newState != _state)
        {
            _state = newState;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (icon == null) return;

        switch (_state)
        {
            case PlantState.Growing:
                if (growingSprite != null) icon.sprite = growingSprite;
                break;
            case PlantState.Mature:
                if (matureSprite != null) icon.sprite = matureSprite;
                break;
            case PlantState.Wilted:
                if (wiltedSprite != null) icon.sprite = wiltedSprite;
                break;
            case PlantState.Harvested:
                break;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryHarvest();
    }

    public void TryHarvest()
    {
        if (_harvested) return;
        if (_mission == null) return;
        if (_mission.IsMissionFinished) return;

        switch (_state)
        {
            case PlantState.Mature:
                // 수확 성공
                _harvested = true;
                _state = PlantState.Harvested;
                UpdateVisual();
                _mission.OnHarvestSuccess(this);
                break;

            case PlantState.Wilted:
                // 미션 실패
                _harvested = true;
                _state = PlantState.Harvested;
                UpdateVisual();
                _mission.OnHarvestFailure(this);
                break;

            case PlantState.Growing:
                break;
        }
    }
}
