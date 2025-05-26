using Cinemachine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : NetworkBehaviour
{
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer) return;

        var vCam = FindObjectOfType<CinemachineVirtualCamera>();
        vCam.Follow = transform;
        //vCam.enabled = true;
    }
}
