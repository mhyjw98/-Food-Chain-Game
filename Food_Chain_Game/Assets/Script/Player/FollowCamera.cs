using Cinemachine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : NetworkBehaviour
{
    [SerializeField] private CinemachineVirtualCamera vCam;
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        TryAssignCamera();
    }

    void Update()
    {
        if (vCam == null || vCam.Follow == null)
            TryAssignCamera();
    }

    void TryAssignCamera()
    {
        if (!isLocalPlayer) return;

        vCam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            vCam.Follow = transform;
            Debug.Log("[FollowCamera] VirtualCamera Follow에 로컬 플레이어 연결 완료");
        }
    }
}
