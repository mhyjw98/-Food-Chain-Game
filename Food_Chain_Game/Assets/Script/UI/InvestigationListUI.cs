using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestigationListUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform content;
    [SerializeField] private InvestigationListItem itemPrefab;

    public void Toggle()
    {
        if (root.activeSelf)
        {
            Close();
            return;
        }

        root.SetActive(true);
        UIManager.RegisterEscape(root, Close);
        Refresh();
    }

    public void Close()
    {
        root.SetActive(false);
        UIManager.UnregisterEscape(root);
    }

    public void Refresh()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (InvestigationLog.Instance == null) return;

        var list = InvestigationLog.Instance.Records;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            var item = Instantiate(itemPrefab, content);
            item.Bind(list[i], OnItemClicked);
        }
    }

    private void OnItemClicked(string recordId)
    {
        if (InvestigationLog.Instance == null || !InvestigationLog.Instance.TryGet(recordId, out var r))
            return;

        InvestigationResultPresenter.Show(r);
    }
}
