using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public GamePlayer myPlayer;

    public enum ScanTargetType { None, Player, Mission, Corpse, Investigation }

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

    public float scanRadius = 1.2f;
    public LayerMask scanLayers;

    public float updateInterval = 0.1f;
    private float timer = 0f;

    private void Update()
    {
        if(!myPlayer.isLocalPlayer) return;       

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
                if (myPlayer.isPredator && gp != myPlayer && gp.isAlive)
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
                    }
                }
            }
            if (hit.TryGetComponent(out Corpse targetCorpse))
            {
                if (myPlayer.IsHelper())
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
                    }
                }
            }
            if (hit.TryGetComponent(out InvestigationObject inv))
            {
                if (myPlayer.IsHelper())
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
                    }
                }
            }
        }

        
        UpdateInteractUI(currentInteractTarget);
    }
    private bool CanInteractMission(MissionObject obj)
    {
        if (obj == null) return false;
        if (!myPlayer.isAlive && myPlayer.animalType != AnimalType.Badger) return false;

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
