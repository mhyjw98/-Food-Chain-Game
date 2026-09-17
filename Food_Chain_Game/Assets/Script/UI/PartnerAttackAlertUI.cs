using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartnerAttackAlertUI : MonoBehaviour
{
    public static PartnerAttackAlertUI Instance { get; private set; }

    [SerializeField] private Image borderImage;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private int flashCount = 3;

    private Coroutine _co;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayAlert()
    {
        if (borderImage == null) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFlash());
    }

    private IEnumerator CoFlash()
    {
        borderImage.gameObject.SetActive(true);

        for (int i = 0; i < flashCount; i++)
        {
            borderImage.enabled = true;
            yield return new WaitForSeconds(flashDuration * 0.5f);

            borderImage.enabled = false;
            yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        borderImage.enabled = false;
        _co = null;
    }
}
