using System;
#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif
using UnityEngine;

namespace MannLab.Ads
{
    public static class MannLabAdMob
    {
        public const string IosInterstitialTestAdUnitId = "ca-app-pub-3940256099942544/4411468910";

        private static bool initialized;
        private static string gameKey;
        private static int gameOverInterval = 3;
#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
        private static bool loadingInterstitial;
        private static string interstitialAdUnitId;
        private static InterstitialAd interstitialAd;
#endif

        public static bool IsReady
        {
            get
            {
#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
                return interstitialAd != null && interstitialAd.CanShowAd();
#else
                return false;
#endif
            }
        }

        public static void InitializeGameOverInterstitial(
            string gameIdentifier,
            string productionIosAdUnitId,
            int interstitialEveryGameOvers = 3)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            gameKey = string.IsNullOrWhiteSpace(gameIdentifier) ? "mannlab-game" : gameIdentifier;
            gameOverInterval = Mathf.Max(1, interstitialEveryGameOvers);

#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
            interstitialAdUnitId = ShouldUseTestAds() ? IosInterstitialTestAdUnitId : productionIosAdUnitId;
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.Initialize(status =>
            {
                if (status == null)
                {
                    Debug.LogWarning("[Ads] Google Mobile Ads initialization returned no status.");
                    return;
                }

                Debug.Log("[Ads] Google Mobile Ads initialized.");
                MobileAdsEventExecutor.ExecuteInUpdate(LoadInterstitial);
            });
#else
            Debug.Log("[Ads] Google Mobile Ads SDK not installed. Interstitials are disabled.");
#endif
        }

        public static bool TryShowGameOverInterstitial()
        {
            if (!initialized)
            {
                return false;
            }

#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
            var gameOverCount = IncrementGameOverCount();
            if (gameOverCount % gameOverInterval != 0)
            {
                if (interstitialAd == null && !loadingInterstitial)
                {
                    LoadInterstitial();
                }

                return false;
            }

            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                if (interstitialAd == null && !loadingInterstitial)
                {
                    LoadInterstitial();
                }

                return false;
            }

            interstitialAd.Show();
            Debug.Log("[Ads] Game-over interstitial shown.");
            return true;
#else
            return false;
#endif
        }

#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
        private static void LoadInterstitial()
        {
            if (loadingInterstitial || interstitialAd != null || string.IsNullOrWhiteSpace(interstitialAdUnitId))
            {
                return;
            }

            loadingInterstitial = true;
            var request = new AdRequest();
            InterstitialAd.Load(interstitialAdUnitId, request, (ad, error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() => HandleInterstitialLoaded(ad, error));
            });
        }

        private static void HandleInterstitialLoaded(InterstitialAd ad, LoadAdError error)
        {
            loadingInterstitial = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[Ads] Interstitial failed to load: {error}");
                return;
            }

            DestroyInterstitial();
            interstitialAd = ad;
            RegisterInterstitialEvents(ad);
            Debug.Log("[Ads] Interstitial loaded.");
        }

        private static void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(HandleInterstitialClosed);
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() => HandleInterstitialFailedToOpen(error));
            };
        }

        private static void HandleInterstitialClosed()
        {
            Debug.Log("[Ads] Interstitial closed.");
            DestroyInterstitial();
            LoadInterstitial();
        }

        private static void HandleInterstitialFailedToOpen(AdError error)
        {
            Debug.LogWarning($"[Ads] Interstitial failed to open: {error}");
            DestroyInterstitial();
            LoadInterstitial();
        }

        private static void DestroyInterstitial()
        {
            if (interstitialAd == null)
            {
                return;
            }

            interstitialAd.Destroy();
            interstitialAd = null;
        }
#endif

        private static int IncrementGameOverCount()
        {
            var key = $"mannlab.ads.{gameKey}.game_over_count";
            var count = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, count);
            PlayerPrefs.Save();
            return count;
        }

        private static bool ShouldUseTestAds()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }
    }
}
