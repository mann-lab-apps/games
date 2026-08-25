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
        public static string DiagnosticSummary { get; private set; } = "ads: not initialized";
#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
        private static bool loadingInterstitial;
        private static bool showInterstitialWhenLoaded;
        private static bool usingTestAds;
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

        public static bool IsLoading
        {
            get
            {
#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS
                return loadingInterstitial;
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
            try
            {
                usingTestAds = ShouldUseTestAds();
                interstitialAdUnitId = usingTestAds ? IosInterstitialTestAdUnitId : productionIosAdUnitId;
                Debug.Log(
                    $"[Ads] Game-over interstitial configured. mode={(usingTestAds ? "test" : "production")}, interval={gameOverInterval}.");
                SetDiagnosticSummary($"ads: configured mode={(usingTestAds ? "test" : "production")} interval={gameOverInterval}");
                MobileAds.SetiOSAppPauseOnBackground(true);
                MobileAds.Initialize(status =>
                {
                    if (status == null)
                    {
                        Debug.LogWarning("[Ads] Google Mobile Ads initialization returned no status.");
                        SetDiagnosticSummary("ads: init returned no status");
                        return;
                    }

                    Debug.Log("[Ads] Google Mobile Ads initialized.");
                    SetDiagnosticSummary("ads: initialized, loading");
                    MobileAdsEventExecutor.ExecuteInUpdate(LoadInterstitial);
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Ads] Google Mobile Ads initialization failed: {exception.GetType().Name}");
                SetDiagnosticSummary($"ads: init failed {exception.GetType().Name}");
            }
#else
            Debug.Log("[Ads] Google Mobile Ads SDK not installed. Interstitials are disabled.");
            SetDiagnosticSummary("ads: SDK not installed");
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
            Debug.Log(
                $"[Ads] Game-over interstitial check. count={gameOverCount}, interval={gameOverInterval}, ready={IsReady}, loading={loadingInterstitial}.");
            SetDiagnosticSummary($"ads: gameover count={gameOverCount} ready={IsReady} loading={loadingInterstitial}");

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
                Debug.LogWarning("[Ads] Game-over interstitial was due, but no loaded ad is ready yet.");
                SetDiagnosticSummary($"ads: due, not ready loading={loadingInterstitial}");
                showInterstitialWhenLoaded = true;
                if (interstitialAd == null && !loadingInterstitial)
                {
                    LoadInterstitial();
                }

                return false;
            }

            try
            {
                return TryShowLoadedInterstitial();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Ads] Failed to show interstitial: {exception.GetType().Name}");
                SetDiagnosticSummary($"ads: show failed {exception.GetType().Name}");
                DestroyInterstitial();
                LoadInterstitial();
                return false;
            }
#else
            return false;
#endif
        }

        public static bool TryShowInterstitialForTesting()
        {
#if MANNLAB_ADMOB_GOOGLE_MOBILE_ADS && MANNLAB_ADMOB_FORCE_TEST_ADS
            if (!initialized)
            {
                SetDiagnosticSummary("ads: manual show before init");
                return false;
            }

            SetDiagnosticSummary($"ads: manual show ready={IsReady} loading={loadingInterstitial}");
            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                showInterstitialWhenLoaded = true;
                if (interstitialAd == null && !loadingInterstitial)
                {
                    LoadInterstitial();
                }

                return false;
            }

            return TryShowLoadedInterstitial();
#else
            SetDiagnosticSummary("ads: manual show only in AdMob test build");
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

            try
            {
                Debug.Log($"[Ads] Loading interstitial. mode={(usingTestAds ? "test" : "production")}.");
                SetDiagnosticSummary($"ads: loading mode={(usingTestAds ? "test" : "production")}");
                loadingInterstitial = true;
                var request = new AdRequest();
                InterstitialAd.Load(interstitialAdUnitId, request, (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() => HandleInterstitialLoaded(ad, error));
                });
            }
            catch (Exception exception)
            {
                loadingInterstitial = false;
                Debug.LogWarning($"[Ads] Failed to request interstitial: {exception.GetType().Name}");
                SetDiagnosticSummary($"ads: request failed {exception.GetType().Name}");
            }
        }

        private static void HandleInterstitialLoaded(InterstitialAd ad, LoadAdError error)
        {
            loadingInterstitial = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[Ads] Interstitial failed to load: {error}");
                SetDiagnosticSummary($"ads: load failed {ShortError(error)}");
                return;
            }

            DestroyInterstitial();
            interstitialAd = ad;
            RegisterInterstitialEvents(ad);
            Debug.Log("[Ads] Interstitial loaded.");
            SetDiagnosticSummary($"ads: loaded pending={showInterstitialWhenLoaded}");
            if (showInterstitialWhenLoaded)
            {
                showInterstitialWhenLoaded = false;
                TryShowLoadedInterstitial();
            }
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
            SetDiagnosticSummary("ads: closed, loading next");
            DestroyInterstitial();
            LoadInterstitial();
        }

        private static void HandleInterstitialFailedToOpen(AdError error)
        {
            Debug.LogWarning($"[Ads] Interstitial failed to open: {error}");
            SetDiagnosticSummary($"ads: open failed {ShortError(error)}");
            showInterstitialWhenLoaded = false;
            DestroyInterstitial();
            LoadInterstitial();
        }

        private static bool TryShowLoadedInterstitial()
        {
            try
            {
                interstitialAd.Show();
                Debug.Log("[Ads] Game-over interstitial shown.");
                SetDiagnosticSummary("ads: shown");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Ads] Failed to show interstitial: {exception.GetType().Name}");
                SetDiagnosticSummary($"ads: show failed {exception.GetType().Name}");
                showInterstitialWhenLoaded = false;
                DestroyInterstitial();
                LoadInterstitial();
                return false;
            }
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

        private static string ShortError(object error)
        {
            if (error == null)
            {
                return "unknown";
            }

            var text = error.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return error.GetType().Name;
            }

            return text.Length > 96 ? text.Substring(0, 96) : text;
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
#if MANNLAB_ADMOB_FORCE_TEST_ADS
            return true;
#else
            return Application.isEditor || Debug.isDebugBuild;
#endif
        }

        private static void SetDiagnosticSummary(string value)
        {
            DiagnosticSummary = string.IsNullOrWhiteSpace(value) ? "ads: unknown" : value;
        }
    }
}
