using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public GamePlayer myPlayer;

    public enum ScanTargetType { None, Player, Mission, Corpse, Investigation, Disguise, Predict }

    [Header("Interaction Target")]
    public ScanTargetType currentType = ScanTargetType.None;
    public ScanTargetType CurrentType => currentType;

    public Component currentInteractTarget;
    public Component CurrentInteractTarget => currentInteractTarget;
    public uint targetNetId;
    public uint CurrentTargetNetId => targetNetId;
    public GamePlayer currentTarget;
    public GamePlayer prevTarget;
    public HashSet<GamePlayer> playersInRange = new();

    public MissionObject mission;
    public MissionObject CurrentMission => mission;

    public Corpse corpse;
    public Corpse CurrentTargetCorpse => corpse;
    public HashSet<Corpse> corpsesInRange = new();
    public InvestigationObject investigation;
    public InvestigationObject CurrentInvestigation => investigation;
    public DisguiseObject disguise;
    public DisguiseObject CurrentDisguise => disguise;
    public PredictObject predict;
    public PredictObject CurrentPredict => predict;

    public float scanRadius = 1.2f;
    public LayerMask scanLayers;

    public float updateInterval = 0.1f;
    private float timer = 0f;

    private void Update()
    {
        if (myPlayer == null || !myPlayer.isLocalPlayer) return;       

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            ScanArea();
        }
    }
    private void ScanArea()
    {
        playersInRange.Clear();
        corpsesInRange.Clear();

        currentTarget = null;
        targetNetId = 0;
        mission = null;
        corpse = null;
        investigation = null;
        disguise = null;
        predict = null;

        currentType = ScanTargetType.None;
        currentInteractTarget = null;

        Vector2 center = transform.position;
        float bestDist = Mathf.Infinity;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, scanRadius, scanLayers);

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(center, hit.transform.position);

            if (hit.TryGetComponent(out GamePlayer gp))
            {
                if (myPlayer.isAlive && myPlayer.isPredator && gp != myPlayer && gp.isAlive)
                {
                    playersInRange.Add(gp);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Player;
                        currentInteractTarget = gp;

                        currentTarget = gp;
                        targetNetId = gp.netId;

                        mission = null;
                        corpse = null;
                        investigation = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }

            if (hit.TryGetComponent(out MissionObject obj))
            {
                if (CanInteractMission(obj))
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Mission;
                        currentInteractTarget = obj;

                        mission = obj;

                        currentTarget = null;
                        targetNetId = 0;
                        corpse = null;
                        investigation = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }
            if (hit.TryGetComponent(out Corpse targetCorpse))
            {
                if (myPlayer.isAlive && myPlayer.IsHelper())
                {
                    corpsesInRange.Add(targetCorpse);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Corpse;
                        currentInteractTarget = targetCorpse;

                        corpse = targetCorpse;

                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        investigation = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }
            if (hit.TryGetComponent(out InvestigationObject inv))
            {
                if (myPlayer.isAlive)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Investigation;
                        currentInteractTarget = inv;

                        investigation = inv;

                        corpse = null;
                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }
            // 위장 오브젝트는 카멜레온에게만 상호작용 대상이 된다(역할은 본인만 알므로 다른 플레이어에게 드러나지 않음)
            if (hit.TryGetComponent(out DisguiseObject disguiseObj))
            {
                if (myPlayer.isAlive && myPlayer.CanDisguise)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Disguise;
                        currentInteractTarget = disguiseObj;

                        disguise = disguiseObj;

                        corpse = null;
                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        investigation = null;
                        predict = null;
                    }
                }
            }
            // 승리 예측 오브젝트는 예측 능력이 있는 플레이어(올빼미)에게만 상호작용 대상이 된다
            if (hit.TryGetComponent(out PredictObject predictObj))
            {
                if (myPlayer.isAlive && myPlayer.CanPredict)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Predict;
                        currentInteractTarget = predictObj;

                        predict = predictObj;

                        corpse = null;
                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        investigation = null;
                        disguise = null;
                    }
                }
            }
        }


        UpdateInteractUI(currentInteractTarget);
    }
    private bool CanInteractMission(MissionObject obj)
    {
        if (obj == null) return false;
        if (!myPlayer.CanDoMissions) return false;

        return myPlayer.CanStartMissionType(obj.MissionType);
    }
    public void UpdateInteractUI(Component target)
    {
        if (prevTarget != null && prevTarget != currentTarget)
            prevTarget.SetKillUI(false);

        if(target == null) return;

        if(target is GamePlayer gp)
        {
            if(myPlayer != null && myPlayer.isPredator)
            {
                gp.SetKillUI(true);
            }
            prevTarget = currentTarget;
        }
        else if (target is Corpse corpse)
        {
            // TODO: corpse outline ON
        }
        else if (target is MissionObject missionObj)
        {
            // TODO: missionObj outline ON
        }
        else if (target is InvestigationObject invObj)
        {

        }
    }
}
