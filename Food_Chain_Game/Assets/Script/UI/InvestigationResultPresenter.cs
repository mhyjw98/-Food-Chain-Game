using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InvestigationResultPresenter
{
    public static void Show(InvestigationRecord r)
    {
        switch (r.type)
        {
            case InvestigationType.Corpse:
                GamePlayUI.Instance.ActiveScanResult(r);
                break;

            case InvestigationType.PlayerIdentity:
                GamePlayUI.Instance.ActivePlayerScanResult(r);
                break;
        }
    }
}
