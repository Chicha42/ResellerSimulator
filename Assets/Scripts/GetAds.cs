using UnityEngine;
using YG;
using System;

public class GetAds : MonoBehaviour
{
    public static event Action<string> OnRewardEarned;
    public static event Action<bool> OnInterstitialToggle;

    public void ShowInterstitialAd()
    {
        Debug.Log("[YG2] Показ межстраничной рекламы...");
        YG2.InterstitialAdvShow();
    }

    public void ShowRewardedAd(string rewardID)
    {
        YG2.RewardedAdvShow(rewardID, () =>
        {
            Debug.Log($"[YG2] Награда получена: {rewardID}");
            OnRewardEarned?.Invoke(rewardID);
        });
    }

    private void OnEnable()
    {
        YG2.onOpenInterAdv += HandleOpen;
        YG2.onCloseInterAdv += HandleClose;
    }
    private void OnDisable()
    {
        YG2.onOpenInterAdv -= HandleOpen;
        YG2.onCloseInterAdv -= HandleClose;
    }

    private void HandleOpen() => OnInterstitialToggle?.Invoke(true);
    private void HandleClose() => OnInterstitialToggle?.Invoke(false);
}