using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public GamePlayer myPlayer;
    public uint targetNetId;
    public uint CurrentTargetNetId => targetNetId;

    public GamePlayer prevTarget;
    public GamePlayer currentTarget;
    public HashSet<GamePlayer> playersInRange = new();    

    public float scanRadius = 1.2f;
    public LayerMask playerLayer;

    public float updateInterval = 0.1f;
    private float timer = 0f;

    private void Update()
    {
        if(!myPlayer.isLocalPlayer) return;
        if(!myPlayer.isPredator) return;
        if(GameMamager.Instance == null) return;
        if(!GameMamager.Instance.IsNightPhase) return;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            ScanArea();
            UpdateKillUI();
        }
    }
    private void ScanArea()
    {
        playersInRange.Clear();
        currentTarget = null;
        targetNetId = 0;

        Vector2 center = transform.position;
        float closestDist = Mathf.Infinity;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, scanRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out GamePlayer gp)) continue;
            if (gp == myPlayer) continue;
            if (!gp.isAlive) continue;

            playersInRange.Add(gp);

            float dist = Vector2.Distance(center, gp.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                currentTarget = gp;
                targetNetId = gp.netId;
            }
        }       
    }
    public void UpdateKillUI()
    {
        if (prevTarget != null && prevTarget != currentTarget)
            prevTarget.SetKillUI(false);

        foreach (var player in playersInRange)
        {
            if (player == currentTarget)
            {
                player.SetKillUI(true);
            }
        }

        prevTarget = currentTarget;
    }
    //public GamePlayer FindValidTarget(GamePlayer attacker)
    //{
    //    Vector2 center = attacker.transform.position;
    //    float radius = scanRadius;

    //    Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, playerLayer);
    //    if (hits.Length == 0) return null;

    //    GamePlayer bestTarget = null;
    //    float closestDist = Mathf.Infinity;

    //    foreach (var hit in hits)
    //    {
    //        if (!hit.TryGetComponent(out GamePlayer target)) continue;
    //        if (target == attacker) continue;
    //        if (!target.isAlive) continue;

    //        float dist = Vector2.Distance(center, target.transform.position);
    //        if (dist < closestDist)
    //        {
    //            closestDist = dist;
    //            bestTarget = target;
    //        }
    //    }

    //    return bestTarget;
    //}

    //public void UpdateKillUI(GamePlayer self)
    //{
    //    if (!GameMamager.Instance.IsNightPhase || self.hasAttacked || !self.isAlive || self.animalType == AnimalType.Snake)
    //        return;

    //    Vector2 myForward = self.GetComponent<PlayerMove>().lastMoveDirection;
    //    if (myForward == Vector2.zero) return;

    //    GamePlayer closestValidTarget = null;
    //    float closestDistance = float.MaxValue;

    //    foreach (var target in playersInRange)
    //    {
    //        if (!target || !target.isAlive) continue;

    //        Vector2 toTarget = ((Vector2)target.transform.position - (Vector2)self.transform.position).normalized;
    //        float directionX = myForward.x;
    //        bool isLookAt = directionX > 0 && toTarget.x > 0 || directionX < 0 && toTarget.x < 0;

    //        if (isLookAt)
    //        {
    //            float dist = Vector2.Distance(self.transform.position, target.transform.position);
    //            if (dist < closestDistance)
    //            {
    //                closestDistance = dist;
    //                closestValidTarget = target;
    //            }
    //        }
    //    }
    //    foreach (var target in playersInRange)
    //    {
    //        if (!target) continue;
    //        target.SetKillUI(target == closestValidTarget);
    //    }
    //}
}
