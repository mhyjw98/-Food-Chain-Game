using Mirror;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class PreySymbiosisAbility : NetworkBehaviour, IAnimalAbility, IPlayerAbility
{
    [SyncVar(hook = nameof(OnPartnerChanged))] public uint partnerNetId;

    [Header("Enable Types")]
    [SerializeField] private AnimalType ostrichType = AnimalType.Ostrich;
    [SerializeField] private AnimalType zebraType = AnimalType.Zebra;

    [SerializeField] private float enterRange = 15f;
    [SerializeField] private float exitRange = 16f;

    [Header("Night Vision Boost")]
    [SerializeField] private float boostedNightVision = 11f;
    [SerializeField] private float proximityPollSeconds = 0.2f;

    // 경계에서 깜빡일 때 추가로 안정화(토글 최소 간격)
    [SerializeField] private float minToggleInterval = 0.35f;

    [Header("Attack Alert")]
    [SerializeField] private bool enablePartnerAttackAlert = true;

    protected GamePlayer local;
    public GamePlayer partner;

    private Coroutine _proximityCo;

    private bool _lastIsNight;
    private float _lastBaseVision;

    private bool _boostApplied;
    private float _lastToggleTime;

    public override void OnStartClient()
    {
        base.OnStartClient();
        local = GetComponent<GamePlayer>();
    }
    public override void OnStopClient()
    {
        base.OnStopClient();
        StopProximityWatcher();
        ResetToBaseVision();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        local = GetComponentInParent<GamePlayer>();
    }
    public void ServerActivate(GamePlayer owner, AnimalType type)
    {
        enabled = (type == ostrichType || type == zebraType);
    }

    [Server]
    public void OnNewDay(int day)
    {
    }
    [Server]
    public void OnBeforeAttack(ref AttackContext ctx)
    {
        if (!enabled) return;
        if (local == null) local = GetComponent<GamePlayer>();
        if (local == null) return;
        if (ctx.attacker == null) return;
        if (ctx.target == null) return;
        if (ctx.target != local) return;

        ctx.killTarget = true;
        ServerNotifyUnderAttack(ctx.attacker, ctx.target);       
    }
    [Server]
    public void ServerClearPartnerBothSides()
    {
        if (!enabled) return;

        uint oldPartner = partnerNetId;
        if (oldPartner == 0) return;

        partnerNetId = 0;

        if (NetworkServer.spawned.TryGetValue(oldPartner, out var otherIdentity) && otherIdentity != null)
        {
            var otherGp = otherIdentity.GetComponent<GamePlayer>();
            if (otherGp != null)
            {
                var otherAbility = otherGp.GetComponent<PreySymbiosisAbility>();
                if (otherAbility != null && otherAbility.partnerNetId == netId)
                    otherAbility.partnerNetId = 0;
            }
        }
    }

    private void OnPartnerChanged(uint oldVal, uint newVal)
    {
        if (!isLocalPlayer) return;
        if (!enabled) return;

        ResolvePartnerClient();

        if (newVal == 0)
        {
            StopProximityWatcher();
            ResetToBaseVision();
            return;
        }

        StartOrStopProximityWatcher();
        ApplyProximityVisionNow(force: true);
    }   

    public void ClientOnDayNightVisionUpdated(bool isNight, float baseVision)
    {
        if (!isLocalPlayer) return;
        if (!enabled) return;

        _lastIsNight = isNight;
        _lastBaseVision = baseVision;

        StartOrStopProximityWatcher();

        ApplyProximityVisionNow(force: true);
    }
    private void StartOrStopProximityWatcher()
    {
        if (!_lastIsNight || partnerNetId == 0)
        {
            StopProximityWatcher();
            ResetToBaseVision();
            return;
        }

        if (_proximityCo == null)
            _proximityCo = StartCoroutine(CoProximityWatcher());
    }

    private void StopProximityWatcher()
    {
        if (_proximityCo != null)
        {
            StopCoroutine(_proximityCo);
            _proximityCo = null;
        }
    }

    private IEnumerator CoProximityWatcher()
    {
        while (true)
        {
            ResolvePartnerClient();

            if (partnerNetId == 0 || partner == null || !partner.isAlive)
            {
                StopProximityWatcher();
                ResetToBaseVision();
                yield break;
            }

            ApplyProximityVisionNow(force: false);
            yield return new WaitForSeconds(proximityPollSeconds);
        }
    }

    private void ApplyProximityVisionNow(bool force)
    {
        if (!_lastIsNight) return;
        if (local == null || local.localVision == null) return;
        if (partner == null) return;

        float dx = local.transform.position.x - partner.transform.position.x;
        float dy = local.transform.position.y - partner.transform.position.y;
        float distSqr = dx * dx + dy * dy;

        float enterSqr = enterRange * enterRange;
        float exitSqr = exitRange * exitRange;

        bool shouldBoost = !_boostApplied ? (distSqr <= enterSqr) : (distSqr <= exitSqr);

        if (!force)
        {
            if (shouldBoost == _boostApplied) return;

            if (Time.time - _lastToggleTime < minToggleInterval)
                return;
        }

        if (shouldBoost != _boostApplied)
            _lastToggleTime = Time.time;

        _boostApplied = shouldBoost;

        float target = shouldBoost ? boostedNightVision : _lastBaseVision;
        local.localVision.TransitionToVision(target, true);
    }
    private void ResetToBaseVision()
    {
        if (!_lastIsNight) return;
        if (local == null || local.localVision == null) return;
        if (!_boostApplied) return;

        _boostApplied = false;
        local.localVision.TransitionToVision(_lastBaseVision, true);
    }
    private void ResolvePartnerClient()
    {
        partner = null;
        if (partnerNetId == 0) return;

        if (NetworkClient.spawned != null &&
            NetworkClient.spawned.TryGetValue(partnerNetId, out var identity) &&
            identity != null)
        {
            partner = identity.GetComponent<GamePlayer>();
        }
    }    
   

    [Server]
    public static void ServerNotifyUnderAttack(GamePlayer attacker, GamePlayer target)
    {
        if (attacker == null || target == null) return;

        var targetAbility = target.GetComponent<PreySymbiosisAbility>();
        if (targetAbility == null) return;
        if (!targetAbility.enablePartnerAttackAlert) return;

        uint partnerId = targetAbility.partnerNetId;
        if (partnerId == 0) return;

        if (NetworkServer.spawned.TryGetValue(partnerId, out var partnerIdentity))
        {
            var partnerGp = partnerIdentity.GetComponent<GamePlayer>();
            if (partnerGp == null) return;

            var partnerAbility = partnerGp.GetComponent<PreySymbiosisAbility>();
            if (partnerAbility == null) return;

            if (partnerGp.connectionToClient != null)
                partnerAbility.TargetRpcUnderAttack(partnerGp.connectionToClient, target.netId, attacker.netId);
        }
    }   

    [TargetRpc]
    private void TargetRpcUnderAttack(NetworkConnectionToClient conn, uint victimNetId, uint attackerNetId)
    {
        if (!isLocalPlayer) return;
        if (!enablePartnerAttackAlert) return;

        var ui = PartnerAttackAlertUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[Symbiosis] PartnerAttackAlertUI.Instance is null. Add PartnerAttackAlertUI in scene.", this);
            return;
        }

        ui.PlayAlert();
    }
   
    [Server]
    public void ServerSetPartner(uint partnerId) => partnerNetId = partnerId;
}
