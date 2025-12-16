using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InsectBushB : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image coverImage;
    [SerializeField] private float reopenDelay = 2f;

    private bool _isOpen;
    private Coroutine _reopenRoutine;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (coverImage == null)
            coverImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isOpen) return;
        OpenBush();
    }

    private void OpenBush()
    {
        _isOpen = true;

        if (coverImage != null)
        {
            coverImage.enabled = false;
            coverImage.raycastTarget = false;
        }

        if (_reopenRoutine != null)
            StopCoroutine(_reopenRoutine);

        _reopenRoutine = StartCoroutine(ReopenRoutine());
    }

    private IEnumerator ReopenRoutine()
    {
        yield return new WaitForSeconds(reopenDelay);

        _isOpen = false;

        if (coverImage != null)
        {
            coverImage.enabled = true;
            coverImage.raycastTarget = true;
        }

        _reopenRoutine = null;
    }

    public void initBush()
    {
        if (_reopenRoutine != null)
        {
            StopCoroutine(_reopenRoutine);
            _reopenRoutine = null;
        }

        _isOpen = false;

        if (coverImage != null)
        {
            coverImage.enabled = true;
            coverImage.raycastTarget = true;
        }
    }
}
