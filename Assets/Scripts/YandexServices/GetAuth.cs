using UnityEngine;
using YG;
using System;

public class GetAuth : MonoBehaviour
{
    public static string PlayerName { get; private set; } = "Аноним";
    public static bool IsAuthorized { get; private set; } = false;

    public static event Action OnPlayerDataLoaded;

    private void OnEnable() => YG2.onGetSDKData += HandleData;
    private void OnDisable() => YG2.onGetSDKData -= HandleData;

    private void HandleData()
    {
        IsAuthorized = YG2.player.auth;
        PlayerName = IsAuthorized ? YG2.player.name : "Аноним";

        Debug.Log($"[YG2] Авторизация: {IsAuthorized}, Имя: {PlayerName}");

        OnPlayerDataLoaded?.Invoke();
    }

    public void OpenLoginWindow() => YG2.OpenAuthDialog();
}