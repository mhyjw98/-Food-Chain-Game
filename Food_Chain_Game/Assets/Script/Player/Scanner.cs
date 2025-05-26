using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public float scanRadius = 3f;
    public LayerMask playerLayer;

    private HashSet<GamePlayer> playersInRange = new();
    public GamePlayer FindValidTarget(GamePlayer attacker)
    {
        Vector2 center = attacker.transform.position;
        Vector2 forward = attacker.GetComponent<PlayerMove>().lastMoveDirection;

        if (forward == Vector2.zero)
        {
            Debug.Log("FindValidTarget : 플레이어 벡터값 0");
            return null;
        }           

        GamePlayer bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (GamePlayer gamePlayer in playersInRange)
        {
            if (gamePlayer == attacker || !gamePlayer.isAlive) continue;

            Vector2 toTarget = ((Vector2)gamePlayer.transform.position - center).normalized;
            float dot = Vector2.Dot(forward.normalized, toTarget);

            if (dot < 0.5f) continue;

            float dist = Vector2.Distance(center, gamePlayer.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestTarget = gamePlayer;
            }
            
        }

        Debug.Log($"Target : {bestTarget == null}");
        return bestTarget;
    }

    public void UpdateKillUI(GamePlayer self)
    {
        if (!GameMamager.Instance.IsNightPhase || self.hasAttacked || !self.isAlive || self.animalType == AnimalType.Snake)
            return;

        Vector2 myForward = self.GetComponent<PlayerMove>().lastMoveDirection;
        if (myForward == Vector2.zero) return;

        GamePlayer closestValidTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var target in playersInRange)
        {
            if (!target || !target.isAlive) continue;

            Vector2 toTarget = ((Vector2)target.transform.position - (Vector2)self.transform.position).normalized;
            float directionX = myForward.x;
            bool isLookAt = directionX > 0 && toTarget.x > 0 || directionX < 0 && toTarget.x < 0;

            if (isLookAt)
            {
                float dist = Vector2.Distance(self.transform.position, target.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestValidTarget = target;
                }
            }
        }
        foreach (var target in playersInRange)
        {
            if (!target) continue;
            target.SetKillUI(target == closestValidTarget);
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out GamePlayer player) && player != GetComponentInParent<GamePlayer>())
        {
            playersInRange.Add(player);
            Debug.Log("플레이어 발견");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out GamePlayer player))
        {
            playersInRange.Remove(player);
            Debug.Log("플레이어 사라짐");
        }
    }
}
