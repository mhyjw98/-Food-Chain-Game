using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxSmokeTest : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        await VivoxService.Instance.InitializeAsync();
        await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = "TestUser" });

        await VivoxService.Instance.JoinEchoChannelAsync(
            $"mic_test_{AuthenticationService.Instance.PlayerId}",
            ChatCapability.AudioOnly
        );

        Debug.Log("Vivox Echo joined. Speak and you should hear yourself.");
    }
}
