using System;
using System.Collections;
using System.Collections.Generic;
using MannLab.Ads;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.GatherAndShot
{
    public sealed class GatherAndShotController : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.gather_and_shot.best_score";
        private const string OwnedCoinsKey = "mannlab.gather_and_shot.owned_snow_coins";
        private const string RunNumberKey = "mannlab.gather_and_shot.run_number";
        private const string FreeUpgradeClaimedKey = "mannlab.gather_and_shot.free_upgrade_claimed";
        private const string StartFullAmmoNextRunKey = "mannlab.gather_and_shot.start_full_ammo_next_run";
        private const string UpgradeLevelKeyPrefix = "mannlab.gather_and_shot.upgrade.";
        private const float WorldHalfHeight = 6.6f;
        private const float WorldHalfWidth = WorldHalfHeight * 9f / 16f;
        private const float TargetWorldAspect = WorldHalfWidth / WorldHalfHeight;
        private const float WarmthBarWidth = 520f;
        private const float GatherBarWidth = 300f;
        private const float DirectionInputDeadZone = 26f;
        private const float DirectionInputMaxDistance = 180f;
        private const float DirectionGuideFadeSeconds = 0.58f;
        private const float JoystickVisualRadius = 66f;
        private const string ProductionIosInterstitialAdUnitId = "ca-app-pub-4525914685149405/2541126713";
        private const string ProductionAndroidInterstitialAdUnitId = "";
#if MANNLAB_ADMOB_FORCE_TEST_ADS
        private const int GameOverInterstitialInterval = 1;
#else
        private const int GameOverInterstitialInterval = 3;
#endif
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private const string CrashlyticsTestArgument = "--mannlab-force-crashlytics-test";
        private const string CrashlyticsTestEnvironmentVariable = "MANNLAB_FORCE_CRASHLYTICS_TEST";
        private const int CrashlyticsTestTapCount = 7;
        private const float CrashlyticsTestTapWindowSeconds = 2.5f;
        private const float CrashlyticsTestTapZoneSize = 220f;
#endif

        private static readonly Color SnowTint = new Color32(239, 249, 255, 255);
        private static readonly Color PaperBlue = new Color32(218, 238, 242, 255);
        private static readonly Color WarmthColor = new Color32(239, 126, 87, 255);
        private static readonly Color AmmoColor = new Color32(88, 166, 206, 255);
        private static readonly Color RunnerTint = new Color32(236, 94, 123, 255);
        private static readonly Color WalkerTint = new Color32(70, 151, 174, 255);
        private static readonly Color HeavyTint = new Color32(107, 92, 130, 255);

        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<Pickup> pickups = new List<Pickup>();
        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly List<Burst> bursts = new List<Burst>();
        private readonly List<FloatingText> floatingTexts = new List<FloatingText>();
        private readonly List<TrailMark> trailMarks = new List<TrailMark>();
        private readonly List<SpriteRenderer> ammoStackRenderers = new List<SpriteRenderer>();
        private readonly System.Random random = new System.Random(Environment.TickCount);

        private Camera worldCamera;
        private Camera letterboxCamera;
        private Sprite playerSprite;
        private Sprite walkerSprite;
        private Sprite runnerSprite;
        private Sprite heavySprite;
        private Sprite snowballSprite;
        private Sprite gatherRingSprite;
        private Sprite driftSprite;
        private Sprite bigDriftSprite;
        private Sprite puffSprite;
        private SpriteRenderer playerRenderer;
        private SpriteRenderer gatheringRenderer;
        private SpriteRenderer gatherRingRenderer;
        private AudioSource audioSource;
        private AudioClip coinClip;
        private AudioClip hitClip;
        private AudioClip gatherClip;
        private AudioClip upgradeClip;
        private RectTransform joystickBase;
        private RectTransform joystickKnob;
        private RectTransform joystickRoot;
        private CanvasGroup joystickCanvasGroup;
        private CanvasScaler hudScaler;
        private RectTransform gameSquareRoot;
        private Text scoreText;
        private Text bestText;
        private Text ammoText;
        private Text coinText;
        private Text objectiveText;
        private Text missionText;
        private Text weaponText;
        private Text feedbackText;
        private CanvasGroup feedbackGroup;
        private Image warmthFill;
        private RectTransform gatherBack;
        private Image gatherFill;
        private GameObject resultPanel;
        private Text resultCoinText;
        private Text resultStatsText;
        private Text resultUpgradeText;
        private Button upgradeButton;
        private Button doubleCoinButton;
        private Button reviveButton;
        private Button bonusChestButton;
        private GameObject upgradePanel;
        private Text upgradeCoinText;
        private Text upgradeFeedbackText;
        private readonly Button[] upgradeOptionButtons = new Button[GatherAndShotBalance.UpgradeCount];
        private readonly Text[] upgradeOptionLabels = new Text[GatherAndShotBalance.UpgradeCount];
        private Vector2 playerPosition;
        private Vector2 playerVelocity;
        private Vector2 joystickAnchorScreen;
        private Vector2 joystickVector;
        private GatherAndShotGameState state;
        private float warmth;
        private int ammo;
        private int score;
        private int bestScore;
        private int ownedCoins;
        private int runEarnedCoins;
        private int runNumber;
        private int ammoGathered;
        private int pickupsCollected;
        private int bigSnowdriftsCollected;
        private int killsByBasic;
        private int killsByBig;
        private int killsBySplit;
        private int killsByIce;
        private int killsByBurst;
        private int walkersDefeated;
        private int runnersDefeated;
        private int heaviesDefeated;
        private int missionStartScore;
        private int missionStartAmmoGathered;
        private int missionStartBigSnowdrifts;
        private int missionStartWalkersDefeated;
        private int missionStartRunnersDefeated;
        private int missionStartHeaviesDefeated;
        private readonly int[] upgradeLevels = new int[GatherAndShotBalance.UpgradeCount];
        private float elapsedSeconds;
        private float sessionStartedAt;
        private float nextSpawnAt;
        private float nextPickupAt;
        private float nextFireAt;
        private float contactReadyAt;
        private float directionGuideShownAt;
        private float stationaryGatherReadyAt;
        private float gatheringStartedAt;
        private float gatheringUntil;
        private float nextTrailAt;
        private float feedbackUntil;
        private int pendingGatherAmmo;
        private PickupKind gatheringKind;
        private MissionKind currentMission;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool joystickHeld;
        private bool runStarted;
        private bool startWithFullAmmoNextRun;
        private bool freeUpgradeClaimed;
        private bool freeUpgradeAvailable;
        private bool firstActionLogged;
        private bool firstRewardLogged;
        private bool ammoEmptyLogged;
        private bool firstMiniGoalCompleted;
        private bool openingBigDriftSpawned;
        private bool openingWeaponCacheSpawned;
        private bool doubledCoinsThisRun;
        private bool revivedThisRun;
        private bool bonusChestOpenedThisRun;
        private bool firstAutoThrowFeedbackShown;
        private bool freeWorkshopFeedbackShown;
        private int currentWaveStage;
        private WeaponKind activeWeapon;
        private float activeWeaponUntil;
        private float rapidThrowUntil;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int crashlyticsTestTapCount;
        private float crashlyticsTestTapDeadline;
#endif

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                worldCamera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                worldCamera.tag = "MainCamera";
            }

            BuildCameras();

            LoadSprites();
            BuildWorld();
            BuildHud();
            LoadProgression();
            InitializeTelemetryAndAds();
            sessionStartedAt = Time.realtimeSinceStartup;
            StartRun();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (ShouldForceCrashlyticsTestOnLaunch())
            {
                StartCoroutine(ForceCrashlyticsTestAfterStartup());
            }
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (HandleCrashlyticsTestTrigger())
            {
                return;
            }
#endif

            if (state == GatherAndShotGameState.GameOver)
            {
                ConfigureSquareViewportIfNeeded();
                UpdateFloatingTexts();
                UpdateFeedbackText();
                return;
            }

            ConfigureSquareViewportIfNeeded();
            elapsedSeconds += Time.deltaTime;
            UpdateGrowthMilestones();
            UpdateJoystick();
            UpdateDirectionGuideVisibility();
            UpdateGathering();
            MovePlayer();
            UpdatePickups();
            UpdateEnemies();
            UpdateProjectiles();
            UpdateBursts();
            UpdateTrailMarks();
            UpdateAmmoStackVisuals();
            UpdateFloatingTexts();
            UpdateFeedbackText();
            TryAutoFire();
            TrySpawnEnemy();
            TrySpawnPickup();
            UpdateHud();
        }

        private static void InitializeTelemetryAndAds()
        {
            try
            {
                FirebaseTelemetry.Initialize();
                FirebaseTelemetry.SetContext("game", "gather-and-shot");
                FirebaseTelemetry.LogEvent("app_open");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Gather & Shot] Firebase initialization skipped: {exception.GetType().Name}");
            }

            try
            {
                MannLabAdMob.InitializeGameOverInterstitial(
                    "gather-and-shot",
                    ProductionIosInterstitialAdUnitId,
                    GameOverInterstitialInterval,
                    ProductionAndroidInterstitialAdUnitId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Gather & Shot] AdMob initialization skipped: {exception.GetType().Name}");
            }
        }

        private void LoadProgression()
        {
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            ownedCoins = PlayerPrefs.GetInt(OwnedCoinsKey, 0);
            runNumber = PlayerPrefs.GetInt(RunNumberKey, 0);
            freeUpgradeClaimed = PlayerPrefs.GetInt(FreeUpgradeClaimedKey, 0) == 1;
            startWithFullAmmoNextRun = PlayerPrefs.GetInt(StartFullAmmoNextRunKey, 0) == 1;
            for (var i = 0; i < upgradeLevels.Length; i++)
            {
                upgradeLevels[i] = PlayerPrefs.GetInt($"{UpgradeLevelKeyPrefix}{i}", 0);
            }
        }

        private void SaveProgression()
        {
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.SetInt(OwnedCoinsKey, ownedCoins);
            PlayerPrefs.SetInt(RunNumberKey, runNumber);
            PlayerPrefs.SetInt(FreeUpgradeClaimedKey, freeUpgradeClaimed ? 1 : 0);
            PlayerPrefs.SetInt(StartFullAmmoNextRunKey, startWithFullAmmoNextRun ? 1 : 0);
            for (var i = 0; i < upgradeLevels.Length; i++)
            {
                PlayerPrefs.SetInt($"{UpgradeLevelKeyPrefix}{i}", upgradeLevels[i]);
            }

            PlayerPrefs.Save();
        }

        private void StartRun()
        {
            if (runStarted)
            {
                FirebaseTelemetry.LogEvent(
                    "restart",
                    BuildEventParameters(new Dictionary<string, string>
                    {
                        { "score", score.ToString() },
                        { "best_score", bestScore.ToString() },
                        { "elapsed_seconds", Mathf.FloorToInt(elapsedSeconds).ToString() }
                    }));
            }

            ClearActors();
            runNumber = PlayerPrefs.GetInt(RunNumberKey, runNumber) + 1;
            runStarted = true;
            state = GatherAndShotGameState.Playing;
            playerPosition = Vector2.zero;
            playerVelocity = Vector2.zero;
            joystickVector = Vector2.zero;
            joystickHeld = false;
            warmth = EffectiveMaxWarmth;
            ammo = GatherAndShotBalance.StartingAmmo(GetUpgradeLevel(UpgradeKind.AmmoCapacity), startWithFullAmmoNextRun);
            startWithFullAmmoNextRun = false;
            score = 0;
            runEarnedCoins = 0;
            ammoGathered = 0;
            pickupsCollected = 0;
            bigSnowdriftsCollected = 0;
            killsByBasic = 0;
            killsByBig = 0;
            killsBySplit = 0;
            killsByIce = 0;
            killsByBurst = 0;
            walkersDefeated = 0;
            runnersDefeated = 0;
            heaviesDefeated = 0;
            elapsedSeconds = 0f;
            nextFireAt = 0f;
            nextTrailAt = 0f;
            contactReadyAt = 0f;
            directionGuideShownAt = 0f;
            stationaryGatherReadyAt = Time.time + GatherAndShotBalance.StationaryGatherDelaySeconds;
            firstActionLogged = false;
            firstRewardLogged = false;
            ammoEmptyLogged = false;
            firstMiniGoalCompleted = false;
            openingBigDriftSpawned = false;
            openingWeaponCacheSpawned = false;
            doubledCoinsThisRun = false;
            revivedThisRun = false;
            bonusChestOpenedThisRun = false;
            freeUpgradeAvailable = false;
            firstAutoThrowFeedbackShown = false;
            freeWorkshopFeedbackShown = false;
            currentWaveStage = GatherAndShotBalance.WaveStage(0f);
            activeWeapon = WeaponKind.BasicSnowball;
            activeWeaponUntil = 0f;
            rapidThrowUntil = 0f;
            SetMission(MissionKind.FirstSnowLoop);
            ClearGathering();
            resultPanel.SetActive(false);
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }

            playerRenderer.transform.position = playerPosition;

            for (var i = 0; i < 2; i++)
            {
                SpawnPickup(i == 0 ? PickupKind.Snowdrift : PickupKind.Snowball);
            }

            SpawnOpeningHookEnemies();
            nextSpawnAt = Time.time + 2.6f;
            nextPickupAt = Time.time + 5.2f;
            SaveProgression();
            UpdateHud();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "run_start",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "best_score", bestScore.ToString() },
                    { "start_full_ammo", ammo == EffectiveMaxAmmo ? "true" : "false" }
                }));
            LogWaveStart();
            UpdateJoystickVisual(false);
        }

        private void UpdateGrowthMilestones()
        {
            var waveStage = GatherAndShotBalance.WaveStage(elapsedSeconds);
            if (waveStage != currentWaveStage)
            {
                currentWaveStage = waveStage;
                LogWaveStart();
            }

            if (!openingBigDriftSpawned && elapsedSeconds >= 24f)
            {
                openingBigDriftSpawned = true;
                SpawnPickup(PickupKind.BigSnowdrift);
            }

            if (!openingWeaponCacheSpawned && elapsedSeconds >= 44f)
            {
                openingWeaponCacheSpawned = true;
                SpawnPickup(PickupKind.WeaponCache);
            }

            if (freeUpgradeAvailable
                && !freeUpgradeClaimed
                && !freeWorkshopFeedbackShown
                && elapsedSeconds >= GatherAndShotBalance.FirstFreeUpgradeSeconds)
            {
                freeWorkshopFeedbackShown = true;
                ShowFeedback("FREE WORKSHOP READY", new Color32(88, 166, 206, 255), 2.2f);
            }

            UpdateMissionProgress();
        }

        private void LogWaveStart()
        {
            FirebaseTelemetry.LogEvent(
                "wave_start",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "wave_stage", currentWaveStage.ToString() },
                    { "wave_label", currentWaveStage == 1 ? "walker" : currentWaveStage == 2 ? "runner_intro" : currentWaveStage == 3 ? "heavy_intro" : "mixed_pressure" }
                }));
        }

        private void BuyRecommendedUpgrade()
        {
            BuyUpgrade(GetRecommendedUpgrade(), freeUpgradeAvailable && !freeUpgradeClaimed, "result_button");
        }

        private void OpenUpgradePanel()
        {
            if (upgradePanel == null)
            {
                BuyRecommendedUpgrade();
                return;
            }

            resultPanel.SetActive(false);
            upgradePanel.SetActive(true);
            upgradeFeedbackText.text = freeUpgradeAvailable && !freeUpgradeClaimed
                ? "First upgrade is free"
                : "Spend Snow Coin to change the next run";
            RefreshUpgradePanel();
            FirebaseTelemetry.LogEvent(
                "upgrade_workshop_open",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "source", "result_button" }
                }));
        }

        private void CloseUpgradePanel()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }

            if (state == GatherAndShotGameState.GameOver && resultPanel != null)
            {
                resultPanel.SetActive(true);
                RefreshResultPanel();
            }
        }

        private void BuyUpgradeFromWorkshop(UpgradeKind kind)
        {
            var free = freeUpgradeAvailable && !freeUpgradeClaimed;
            var beforeLevel = GetUpgradeLevel(kind);
            BuyUpgrade(kind, free, "upgrade_workshop");
            if (GetUpgradeLevel(kind) > beforeLevel)
            {
                upgradeFeedbackText.text = UpgradeEffectText(kind, GetUpgradeLevel(kind));
                ShowFeedback(UpgradeEffectText(kind, GetUpgradeLevel(kind)), new Color32(88, 166, 206, 255), 1.8f);
            }

            RefreshUpgradePanel();
        }

        private void RefreshUpgradePanel()
        {
            if (upgradePanel == null || upgradeCoinText == null)
            {
                return;
            }

            upgradeCoinText.text = $"SNOW COIN {ownedCoins}";
            for (var i = 0; i < upgradeOptionButtons.Length; i++)
            {
                var kind = (UpgradeKind)i;
                var level = GetUpgradeLevel(kind);
                var cost = GatherAndShotBalance.UpgradeCost(kind, level);
                var free = freeUpgradeAvailable && !freeUpgradeClaimed;
                var canBuy = free || ownedCoins >= cost;
                if (upgradeOptionLabels[i] != null)
                {
                    upgradeOptionLabels[i].text =
                        $"{GatherAndShotBalance.UpgradeName(kind)}\n"
                        + $"Lv {level} > {level + 1}\n"
                        + UpgradeEffectText(kind, level + 1)
                        + $"\n{(free ? "FREE" : $"{cost} coins")}";
                }

                SetButtonEnabled(upgradeOptionButtons[i], canBuy, canBuy ? SketchPalette.WarmHighlight : (Color)new Color32(210, 214, 214, 210));
            }
        }

        private void BuyUpgrade(UpgradeKind kind, bool free, string source)
        {
            var index = (int)kind;
            var cost = free ? 0 : GatherAndShotBalance.UpgradeCost(kind, upgradeLevels[index]);
            if (!free && ownedCoins < cost)
            {
                RefreshResultPanel();
                return;
            }

            if (!free)
            {
                ownedCoins -= cost;
            }

            upgradeLevels[index]++;
            if (free)
            {
                freeUpgradeClaimed = true;
                freeUpgradeAvailable = false;
            }

            warmth = Mathf.Min(warmth + (kind == UpgradeKind.WarmCoat ? 15f : 0f), EffectiveMaxWarmth);
            ammo = Mathf.Min(ammo, EffectiveMaxAmmo);
            SaveProgression();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "upgrade_purchase",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "upgrade", GatherAndShotBalance.UpgradeName(kind) },
                    { "level", upgradeLevels[index].ToString() },
                    { "cost", cost.ToString() },
                    { "free", free ? "true" : "false" },
                    { "source", source }
                }));
            if (upgradeLevels[index] == 1)
            {
                FirebaseTelemetry.LogEvent(
                    "first_upgrade",
                    BuildEventParameters(new Dictionary<string, string>
                    {
                        { "upgrade", GatherAndShotBalance.UpgradeName(kind) },
                        { "source", source }
                    }));
            }

            PlaySfx(upgradeClip);
            SpawnBurst(playerPosition, 1.35f, 16);
            RefreshResultPanel();
            RefreshUpgradePanel();
            UpdateHud();
        }

        private static string UpgradeEffectText(UpgradeKind kind, int level)
        {
            switch (kind)
            {
                case UpgradeKind.AmmoCapacity:
                    return $"+{level * 2} max snow";
                case UpgradeKind.GatherSpeed:
                    return "Gather faster";
                case UpgradeKind.ThrowRate:
                    return "Auto throw faster";
                case UpgradeKind.SnowballDamage:
                    return $"+{level} damage";
                case UpgradeKind.WarmCoat:
                    return $"+{level * 15} warmth";
                case UpgradeKind.CoinMagnet:
                    return "Wider pickup pull";
                default:
                    return "Upgrade ready";
            }
        }

        private UpgradeKind GetRecommendedUpgrade()
        {
            if (GetUpgradeLevel(UpgradeKind.GatherSpeed) == 0)
            {
                return UpgradeKind.GatherSpeed;
            }

            if (GetUpgradeLevel(UpgradeKind.AmmoCapacity) == 0)
            {
                return UpgradeKind.AmmoCapacity;
            }

            var best = UpgradeKind.ThrowRate;
            var bestLevel = int.MaxValue;
            for (var i = 0; i < upgradeLevels.Length; i++)
            {
                if (upgradeLevels[i] < bestLevel)
                {
                    bestLevel = upgradeLevels[i];
                    best = (UpgradeKind)i;
                }
            }

            return best;
        }

        private int GetUpgradeLevel(UpgradeKind kind)
        {
            return upgradeLevels[(int)kind];
        }

        private void AwardSnowCoins(int amount, string source, Vector2? worldPosition = null)
        {
            if (amount <= 0)
            {
                return;
            }

            runEarnedCoins += amount;
            ownedCoins += amount;
            PlaySfx(coinClip);
            CreateFloatingText($"+{amount} SNOW COIN", worldPosition ?? playerPosition, new Color32(239, 126, 87, 255), 25);
            SaveProgression();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "currency_earned",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "amount", amount.ToString() },
                    { "source", source }
                }));
            if (!firstRewardLogged)
            {
                firstRewardLogged = true;
                FirebaseTelemetry.LogEvent(
                    "first_reward",
                    BuildEventParameters(new Dictionary<string, string>
                    {
                        { "amount", amount.ToString() },
                        { "source", source }
                    }));
            }
        }

        private void LoadSprites()
        {
            playerSprite = LoadSprite("player", new Color32(73, 150, 202, 255), 128);
            walkerSprite = LoadSprite("walker", WalkerTint, 128);
            runnerSprite = LoadSprite("runner", RunnerTint, 128);
            heavySprite = LoadSprite("heavy", HeavyTint, 128);
            snowballSprite = LoadSprite("snowball", Color.white, 96);
            gatherRingSprite = CreateRingSprite("gather_ring", 180, 58, new Color32(90, 176, 218, 185), 96f);
            driftSprite = LoadSprite("snowdrift", new Color32(226, 244, 251, 255), 128);
            bigDriftSprite = LoadSprite("big_snowdrift", new Color32(226, 244, 251, 255), 160);
            puffSprite = LoadSprite("puff", new Color32(238, 249, 255, 210), 96);
        }

        private void BuildCameras()
        {
            letterboxCamera = new GameObject("Letterbox Camera", typeof(Camera)).GetComponent<Camera>();
            letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            letterboxCamera.backgroundColor = Color.black;
            letterboxCamera.cullingMask = 0;
            letterboxCamera.depth = -100f;
            letterboxCamera.orthographic = true;
            letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);

            worldCamera.orthographic = true;
            worldCamera.orthographicSize = WorldHalfHeight;
            worldCamera.aspect = TargetWorldAspect;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = SketchPalette.Paper;
            worldCamera.depth = 0f;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            ConfigureSquareViewportIfNeeded(true);
        }

        private void ConfigureSquareViewportIfNeeded(bool force = false)
        {
            if (!force && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            var width = Mathf.Max(1f, Screen.width);
            var height = Mathf.Max(1f, Screen.height);
            var screenAspect = width / height;
            if (screenAspect >= TargetWorldAspect)
            {
                var normalizedWidth = TargetWorldAspect / screenAspect;
                worldCamera.rect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
                if (hudScaler != null)
                {
                    hudScaler.matchWidthOrHeight = 1f;
                }
            }
            else
            {
                var normalizedHeight = screenAspect / TargetWorldAspect;
                worldCamera.rect = new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
                if (hudScaler != null)
                {
                    hudScaler.matchWidthOrHeight = 0f;
                }
            }

            worldCamera.aspect = TargetWorldAspect;
            ApplySquareHudAnchors();
        }

        private void ApplySquareHudAnchors()
        {
            if (gameSquareRoot == null || worldCamera == null)
            {
                return;
            }

            var rect = worldCamera.rect;
            gameSquareRoot.anchorMin = new Vector2(rect.xMin, rect.yMin);
            gameSquareRoot.anchorMax = new Vector2(rect.xMax, rect.yMax);
            gameSquareRoot.offsetMin = Vector2.zero;
            gameSquareRoot.offsetMax = Vector2.zero;
        }

        private void BuildWorld()
        {
            var bg = new GameObject("Snow Paper", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            bg.sprite = CreateSolidSprite("SnowPaper", 1400, 1400, SnowTint, 96f);
            bg.transform.position = new Vector3(0f, 0f, 8f);
            bg.sortingOrder = -20;

            BuildBoundaryMarkers();

            for (var i = 0; i < 18; i++)
            {
                var stroke = new GameObject("Sketch Snow Line", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                stroke.sprite = CreateSolidSprite("SnowLine", 80 + random.Next(90), 4, PaperBlue, 32f);
                stroke.transform.position = new Vector3(
                    RandomRange(-PlayHalfWidth, PlayHalfWidth),
                    RandomRange(-WorldHalfHeight + 0.5f, WorldHalfHeight - 0.5f),
                    6f);
                stroke.transform.rotation = Quaternion.Euler(0f, 0f, RandomRange(-13f, 13f));
                stroke.sortingOrder = -15;
            }

            playerRenderer = new GameObject("Player", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            playerRenderer.sprite = playerSprite;
            playerRenderer.sortingOrder = 20;
            playerRenderer.transform.localScale = Vector3.one * 0.82f;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            coinClip = CreateToneClip("SnowCoin", 880f, 0.08f, 0.22f);
            hitClip = CreateToneClip("SnowHit", 320f, 0.07f, 0.18f);
            gatherClip = CreateToneClip("SnowGather", 620f, 0.06f, 0.16f);
            upgradeClip = CreateToneClip("SnowUpgrade", 1040f, 0.12f, 0.22f);

            gatheringRenderer = new GameObject("Gathering Snow Cloud", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            gatheringRenderer.sprite = puffSprite;
            gatheringRenderer.sortingOrder = 19;
            gatheringRenderer.transform.localScale = Vector3.zero;
            gatheringRenderer.gameObject.SetActive(false);

            gatherRingRenderer = new GameObject("Stop To Gather Ring", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            gatherRingRenderer.sprite = gatherRingSprite;
            gatherRingRenderer.sortingOrder = 21;
            gatherRingRenderer.transform.localScale = Vector3.zero;
            gatherRingRenderer.gameObject.SetActive(false);

            for (var i = 0; i < 24; i++)
            {
                var stack = new GameObject("Packed Snow Ammo", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                stack.sprite = snowballSprite;
                stack.sortingOrder = 23;
                stack.color = new Color32(255, 255, 255, 230);
                stack.transform.localScale = Vector3.one * 0.18f;
                stack.gameObject.SetActive(false);
                ammoStackRenderers.Add(stack);
            }
        }

        private void BuildHud()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudScaler = canvasObject.GetComponent<CanvasScaler>();
            hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudScaler.referenceResolution = new Vector2(1080f, 1920f);
            hudScaler.matchWidthOrHeight = Screen.width >= Screen.height ? 1f : 0f;

            var safe = SketchUiFactory.CreateSafeAreaRoot(canvasObject.transform);
            gameSquareRoot = CreateRect(safe, "Game Square HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplySquareHudAnchors();

            var top = CreateRect(gameSquareRoot, "Top HUD", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -176f), new Vector2(0f, -18f));
            bestText = CreateText(top, "Best", "BEST 0", 26, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -12f), new Vector2(260f, 44f));
            scoreText = CreateText(top, "Score", "0", 52, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(210f, 62f));
            ammoText = CreateText(top, "Ammo", "SNOW 3/10", 26, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-28f, -14f), new Vector2(280f, 44f));
            coinText = CreateText(top, "Coins", "COIN 0 | BAG 0", 24, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(440f, 38f));

            var warmthBack = CreatePanel(top, "Warmth Back", new Vector2(0.5f, 1f), new Vector2(WarmthBarWidth, 38f), SketchPalette.WarmShadow);
            warmthBack.anchoredPosition = new Vector2(0f, -104f);
            warmthFill = CreateImage(warmthBack, "Warmth Fill", WarmthColor);
            warmthFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            warmthFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            warmthFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            warmthFill.rectTransform.offsetMin = Vector2.zero;
            warmthFill.rectTransform.offsetMax = Vector2.zero;

            gatherBack = CreatePanel(top, "Gather Back", new Vector2(0.5f, 1f), new Vector2(GatherBarWidth, 18f), new Color32(255, 253, 247, 138));
            gatherBack.anchoredPosition = new Vector2(0f, -150f);
            gatherFill = CreateImage(gatherBack, "Gather Fill", AmmoColor);
            gatherFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            gatherFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            gatherFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            gatherFill.rectTransform.offsetMin = Vector2.zero;
            gatherFill.rectTransform.offsetMax = Vector2.zero;
            gatherBack.gameObject.SetActive(false);

            objectiveText = CreateText(gameSquareRoot, "Objective", "Stop to gather snow", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 194f), new Vector2(620f, 48f));
            missionText = CreateText(gameSquareRoot, "Mission", "MISSION", 21, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 106f), new Vector2(660f, 42f));
            weaponText = CreateText(gameSquareRoot, "Weapon", "SNOWBALL", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 146f), new Vector2(440f, 38f));
            feedbackText = CreateText(gameSquareRoot, "Feedback", string.Empty, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 208f), new Vector2(660f, 68f));
            feedbackGroup = feedbackText.gameObject.AddComponent<CanvasGroup>();
            feedbackGroup.alpha = 0f;

            joystickRoot = gameSquareRoot;
            joystickBase = CreatePanel(gameSquareRoot, "Move Direction Guide", new Vector2(0.5f, 0.5f), new Vector2(230f, 230f), new Color32(255, 253, 247, 118));
            joystickCanvasGroup = joystickBase.gameObject.AddComponent<CanvasGroup>();
            joystickCanvasGroup.alpha = 0f;
            joystickCanvasGroup.blocksRaycasts = false;
            joystickCanvasGroup.interactable = false;
            joystickKnob = CreatePanel(joystickBase, "Joystick Knob", new Vector2(0.5f, 0.5f), new Vector2(86f, 86f), new Color32(88, 166, 206, 210));
            joystickBase.gameObject.SetActive(false);

            resultPanel = CreatePanel(gameSquareRoot, "Result Panel", new Vector2(0.5f, 0.5f), new Vector2(640f, 780f), SketchPalette.TilePaper).gameObject;
            CreateText(resultPanel.transform, "Result Title", "RUN COMPLETE", 40, TextAnchor.MiddleCenter, new Vector2(0f, 318f), new Vector2(540f, 56f));
            resultCoinText = CreateText(resultPanel.transform, "Result Coins", "SNOW COIN +0", 44, TextAnchor.MiddleCenter, new Vector2(0f, 248f), new Vector2(560f, 66f));
            resultStatsText = CreateText(resultPanel.transform, "Result Stats", "Kills 0", 23, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(560f, 112f));
            resultUpgradeText = CreateText(resultPanel.transform, "Upgrade Summary", "UPGRADES", 21, TextAnchor.MiddleCenter, new Vector2(0f, 54f), new Vector2(560f, 86f));
            upgradeButton = CreateButton(resultPanel.transform, "Upgrade Button", "Upgrade", new Vector2(0f, -50f), new Vector2(420f, 66f));
            upgradeButton.onClick.AddListener(OpenUpgradePanel);
            doubleCoinButton = CreateButton(resultPanel.transform, "Double Coin Button", "2x Coin", new Vector2(-156f, -136f), new Vector2(276f, 58f));
            doubleCoinButton.onClick.AddListener(() => OfferRewardedReward(RewardedOfferKind.DoubleSnowCoin));
            reviveButton = CreateButton(resultPanel.transform, "Revive Button", "Revive", new Vector2(156f, -136f), new Vector2(276f, 58f));
            reviveButton.onClick.AddListener(() => OfferRewardedReward(RewardedOfferKind.Revive));
            bonusChestButton = CreateButton(resultPanel.transform, "Bonus Chest Button", "Bonus Chest", new Vector2(0f, -216f), new Vector2(420f, 58f));
            bonusChestButton.onClick.AddListener(() => OfferRewardedReward(RewardedOfferKind.BonusChest));
            var again = CreateButton(resultPanel.transform, "Again Button", "Next Run", new Vector2(0f, -300f), new Vector2(300f, 64f));
            again.onClick.AddListener(StartRun);
            resultPanel.SetActive(false);

            upgradePanel = CreatePanel(gameSquareRoot, "Upgrade Workshop", new Vector2(0.5f, 0.5f), new Vector2(680f, 940f), SketchPalette.TilePaper).gameObject;
            CreateText(upgradePanel.transform, "Workshop Title", "SNOW WORKSHOP", 39, TextAnchor.MiddleCenter, new Vector2(0f, 394f), new Vector2(580f, 54f));
            upgradeCoinText = CreateText(upgradePanel.transform, "Workshop Coins", "SNOW COIN 0", 30, TextAnchor.MiddleCenter, new Vector2(0f, 342f), new Vector2(560f, 44f));
            upgradeFeedbackText = CreateText(upgradePanel.transform, "Workshop Feedback", "Choose an upgrade", 23, TextAnchor.MiddleCenter, new Vector2(0f, 294f), new Vector2(600f, 42f));

            for (var i = 0; i < GatherAndShotBalance.UpgradeCount; i++)
            {
                var kind = (UpgradeKind)i;
                var x = i % 2 == 0 ? -168f : 168f;
                var y = 204f - i / 2 * 156f;
                var option = CreateButton(upgradePanel.transform, $"{kind} Option", GatherAndShotBalance.UpgradeName(kind), new Vector2(x, y), new Vector2(300f, 112f), 24);
                option.onClick.AddListener(() => BuyUpgradeFromWorkshop(kind));
                upgradeOptionButtons[i] = option;
                upgradeOptionLabels[i] = option.GetComponentInChildren<Text>();
            }

            var closeWorkshop = CreateButton(upgradePanel.transform, "Workshop Back Button", "Back", new Vector2(-174f, -362f), new Vector2(250f, 62f), 30);
            closeWorkshop.onClick.AddListener(CloseUpgradePanel);
            var nextFromWorkshop = CreateButton(upgradePanel.transform, "Workshop Next Run Button", "Next Run", new Vector2(174f, -362f), new Vector2(250f, 62f), 30);
            nextFromWorkshop.onClick.AddListener(StartRun);
            upgradePanel.SetActive(false);
        }

        private void UpdateJoystick()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    BeginJoystick(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    DragJoystick(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndJoystick();
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginJoystick(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                DragJoystick(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndJoystick();
            }
        }

        private void BeginJoystick(Vector2 screenPosition)
        {
            LogFirstAction("move");
            if (IsGathering)
            {
                CancelGathering();
            }

            joystickAnchorScreen = screenPosition;
            joystickHeld = true;
            UpdateJoystickVisual(true);
            DragJoystick(screenPosition);
        }

        private void DragJoystick(Vector2 screenPosition)
        {
            if (!joystickHeld)
            {
                return;
            }

            if (joystickBase.gameObject.activeSelf)
            {
                PositionJoystickGuideAtScreenPoint(joystickAnchorScreen);
            }

            var delta = screenPosition - joystickAnchorScreen;
            joystickVector = delta.magnitude <= DirectionInputDeadZone
                ? Vector2.zero
                : Vector2.ClampMagnitude(delta, DirectionInputMaxDistance) / DirectionInputMaxDistance;
            if (joystickBase.gameObject.activeSelf)
            {
                joystickKnob.anchoredPosition = joystickVector * JoystickVisualRadius;
            }
        }

        private void EndJoystick()
        {
            joystickHeld = false;
            joystickVector = Vector2.zero;
            stationaryGatherReadyAt = Time.time + GatherAndShotBalance.StationaryGatherDelaySeconds;
            UpdateJoystickVisual(false);
        }

        private void UpdateJoystickVisual(bool held)
        {
            joystickBase.gameObject.SetActive(held);
            joystickCanvasGroup.alpha = held ? 1f : 0f;
            joystickKnob.anchoredPosition = Vector2.zero;
            joystickBase.localScale = held ? Vector3.one * 1.05f : Vector3.one;
            if (held)
            {
                directionGuideShownAt = Time.time;
                PositionJoystickGuideAtScreenPoint(joystickAnchorScreen);
            }
        }

        private void UpdateDirectionGuideVisibility()
        {
            if (!joystickHeld || joystickBase == null || !joystickBase.gameObject.activeSelf)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.time - directionGuideShownAt) / DirectionGuideFadeSeconds);
            joystickCanvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            if (progress >= 1f)
            {
                joystickBase.gameObject.SetActive(false);
            }
        }

        private void MovePlayer()
        {
            if (IsGathering)
            {
                playerVelocity = Vector2.zero;
                playerRenderer.transform.position = playerPosition;
                playerRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 4.5f);
                if (joystickHeld && joystickBase.gameObject.activeSelf)
                {
                    PositionJoystickGuideAtScreenPoint(joystickAnchorScreen);
                }

                return;
            }

            var speed = GatherAndShotBalance.PlayerSpeed(elapsedSeconds);
            playerVelocity = Vector2.Lerp(playerVelocity, joystickVector * speed, Time.deltaTime * 12f);
            playerPosition += playerVelocity * Time.deltaTime;
            playerPosition.x = Mathf.Clamp(playerPosition.x, -PlayHalfWidth + 0.38f, PlayHalfWidth - 0.38f);
            playerPosition.y = Mathf.Clamp(playerPosition.y, -WorldHalfHeight + 0.55f, WorldHalfHeight - 0.55f);
            playerRenderer.transform.position = playerPosition;
            playerRenderer.transform.rotation = Quaternion.identity;
            if (playerVelocity.sqrMagnitude > 0.05f)
            {
                playerRenderer.transform.localScale = new Vector3(playerVelocity.x < -0.05f ? -0.82f : 0.82f, 0.82f, 1f);
                if (Time.time >= nextTrailAt)
                {
                    nextTrailAt = Time.time + 0.13f;
                    SpawnTrailMark(playerPosition - playerVelocity.normalized * 0.22f, new Color32(185, 220, 226, 120), 0.28f);
                }
            }

            if (joystickHeld && joystickBase.gameObject.activeSelf)
            {
                PositionJoystickGuideAtScreenPoint(joystickAnchorScreen);
            }
        }

        private void TrySpawnEnemy()
        {
            if (Time.time < nextSpawnAt || enemies.Count >= GatherAndShotBalance.MaxLiveEnemies(elapsedSeconds))
            {
                return;
            }

            SpawnEnemy(GatherAndShotBalance.RollEnemyKind(random, elapsedSeconds));
            nextSpawnAt = Time.time + GatherAndShotBalance.SpawnGap(elapsedSeconds);
        }

        private void SpawnOpeningHookEnemies()
        {
            SpawnEnemyAt(EnemyKind.Walker, new Vector2(-PlayHalfWidth - 0.25f, 2.1f));
            SpawnEnemyAt(EnemyKind.Walker, new Vector2(PlayHalfWidth + 0.25f, -1.3f));
            SpawnEnemyAt(EnemyKind.Walker, new Vector2(0.4f, WorldHalfHeight + 0.25f));
        }

        private void SpawnEnemy(EnemyKind kind)
        {
            var angle = RandomRange(0f, Mathf.PI * 2f);
            var spawn = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RandomRange(6.2f, 7.8f);
            spawn.x = Mathf.Clamp(spawn.x, -PlayHalfWidth - 0.9f, PlayHalfWidth + 0.9f);
            spawn.y = Mathf.Clamp(spawn.y, -WorldHalfHeight - 0.9f, WorldHalfHeight + 0.9f);

            SpawnEnemyAt(kind, spawn);
        }

        private void SpawnEnemyAt(EnemyKind kind, Vector2 spawn)
        {
            var renderer = new GameObject(kind.ToString(), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = kind == EnemyKind.Runner ? runnerSprite : kind == EnemyKind.Heavy ? heavySprite : walkerSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 12;
            renderer.transform.position = spawn;
            renderer.transform.localScale = Vector3.one * (kind == EnemyKind.Heavy ? 1.28f : kind == EnemyKind.Runner ? 0.70f : 0.82f);

            enemies.Add(new Enemy
            {
                Kind = kind,
                Renderer = renderer,
                Position = spawn,
                Health = GatherAndShotBalance.StartingHealth(kind),
                Seed = RandomRange(0f, 100f),
                NextTrailAt = Time.time + RandomRange(0.05f, 0.22f)
            });
        }

        private void TrySpawnPickup()
        {
            if (Time.time < nextPickupAt || pickups.Count >= GatherAndShotBalance.MaxLivePickups)
            {
                return;
            }

            SpawnPickup(GatherAndShotBalance.RollPickupKind(random, elapsedSeconds));
            nextPickupAt = Time.time + RandomRange(
                GatherAndShotBalance.PickupSpawnGapMin(elapsedSeconds),
                GatherAndShotBalance.PickupSpawnGapMax(elapsedSeconds));
        }

        private void SpawnPickup(PickupKind kind)
        {
            var renderer = new GameObject(PickupName(kind), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = PickupSprite(kind);
            renderer.color = kind == PickupKind.WeaponCache ? (Color)new Color32(168, 225, 255, 255) : Color.white;
            renderer.sortingOrder = kind == PickupKind.BigSnowdrift ? 5 : 4;
            renderer.transform.position = ChoosePickupPosition(kind);
            renderer.transform.localScale = Vector3.one * PickupScale(kind);
            pickups.Add(new Pickup
            {
                Kind = kind,
                Renderer = renderer,
                Position = renderer.transform.position
            });
        }

        private Vector2 ChoosePickupPosition(PickupKind kind)
        {
            if (kind != PickupKind.Snowball && enemies.Count > 0 && random.NextDouble() < 0.68d)
            {
                var enemy = enemies[random.Next(enemies.Count)];
                var angle = RandomRange(0f, Mathf.PI * 2f);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RandomRange(1.1f, 2.1f);
                return ClampToPlayfield(enemy.Position + offset, 0.65f);
            }

            return new Vector2(
                RandomRange(-PlayHalfWidth + 0.4f, PlayHalfWidth - 0.4f),
                RandomRange(-WorldHalfHeight + 0.75f, WorldHalfHeight - 0.75f));
        }

        private Vector2 ClampToPlayfield(Vector2 position, float margin)
        {
            position.x = Mathf.Clamp(position.x, -PlayHalfWidth + margin, PlayHalfWidth - margin);
            position.y = Mathf.Clamp(position.y, -WorldHalfHeight + margin, WorldHalfHeight - margin);
            return position;
        }

        private Sprite PickupSprite(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BigSnowdrift:
                    return bigDriftSprite;
                case PickupKind.Snowdrift:
                    return driftSprite;
                case PickupKind.WeaponCache:
                    return bigDriftSprite;
                default:
                    return snowballSprite;
            }
        }

        private static string PickupName(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BigSnowdrift:
                    return "Big Snowdrift";
                case PickupKind.Snowdrift:
                    return "Snowdrift";
                case PickupKind.WeaponCache:
                    return "Weapon Cache";
                default:
                    return "Snowball Pickup";
            }
        }

        private static float PickupScale(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BigSnowdrift:
                    return 0.95f;
                case PickupKind.Snowdrift:
                    return 0.74f;
                case PickupKind.WeaponCache:
                    return 0.82f;
                default:
                    return 0.48f;
            }
        }

        private void UpdatePickups()
        {
            for (var i = pickups.Count - 1; i >= 0; i--)
            {
                var pickup = pickups[i];
                pickup.Renderer.transform.Rotate(0f, 0f, PickupSpin(pickup.Kind) * Time.deltaTime);
                if (IsGathering || (KindNeedsAmmo(pickup.Kind) && ammo >= EffectiveMaxAmmo))
                {
                    continue;
                }

                if (Vector2.Distance(playerPosition, pickup.Position) <= GatherAndShotBalance.PickupRadius(pickup.Kind, GetUpgradeLevel(UpgradeKind.CoinMagnet)))
                {
                    CollectBonusPickup(pickup.Kind, pickup.Position);
                    Destroy(pickup.Renderer.gameObject);
                    pickups.RemoveAt(i);
                }
            }
        }

        private void CollectBonusPickup(PickupKind kind, Vector2 position)
        {
            var bonusAmmo = GatherAndShotBalance.PickupAmmo(kind);
            ammo = Mathf.Min(EffectiveMaxAmmo, ammo + bonusAmmo);
            pickupsCollected++;
            ammoGathered += bonusAmmo;
            if (kind == PickupKind.BigSnowdrift)
            {
                bigSnowdriftsCollected++;
                ActivateWeapon(WeaponKind.BigSnowball, "big_snowdrift");
            }
            else if (kind == PickupKind.WeaponCache)
            {
                ActivateWeapon(RollWeaponCache(), "weapon_cache");
            }

            AwardSnowCoins(kind == PickupKind.WeaponCache ? 6 : kind == PickupKind.BigSnowdrift ? 5 : 2, $"pickup_{kind}", position);
            SpawnBurst(position, kind == PickupKind.BigSnowdrift ? 1.05f : kind == PickupKind.Snowdrift ? 0.78f : 0.52f, kind == PickupKind.BigSnowdrift ? 10 : 6);
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "bonus_pickup",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "pickup_kind", kind.ToString() },
                    { "bonus_ammo", bonusAmmo.ToString() },
                    { "score", score.ToString() }
                }));
        }

        private static bool KindNeedsAmmo(PickupKind kind)
        {
            return kind != PickupKind.WeaponCache;
        }

        private void BeginStationaryGathering()
        {
            LogFirstAction("gather");
            gatheringKind = PickupKind.Snowball;
            pendingGatherAmmo = GatherAndShotBalance.StationaryGatherAmmo;
            gatheringStartedAt = Time.time;
            gatheringUntil = Time.time + GatherAndShotBalance.StationaryGatherCycleSeconds(elapsedSeconds, GetUpgradeLevel(UpgradeKind.GatherSpeed));
            playerVelocity = Vector2.zero;
            playerRenderer.transform.position = playerPosition;
            SpawnBurst(playerPosition, 0.24f, 2);
            ShowFeedback("STOP: GATHER SNOW", new Color32(88, 166, 206, 255), 1.1f);
            FirebaseTelemetry.LogEvent(
                "gather_start",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "source", "stationary" },
                    { "pending_ammo", pendingGatherAmmo.ToString() },
                    { "score", score.ToString() }
                }));
            UpdateGatheringVisual(0f);
        }

        private void UpdateGathering()
        {
            if (joystickHeld)
            {
                if (IsGathering)
                {
                    CancelGathering();
                }

                return;
            }

            if (ammo >= EffectiveMaxAmmo)
            {
                if (IsGathering)
                {
                    ClearGathering();
                }

                return;
            }

            if (!IsGathering)
            {
                if (Time.time >= stationaryGatherReadyAt && playerVelocity.sqrMagnitude < 0.06f)
                {
                    BeginStationaryGathering();
                }

                return;
            }

            var duration = Mathf.Max(0.01f, gatheringUntil - gatheringStartedAt);
            var progress = Mathf.Clamp01((Time.time - gatheringStartedAt) / duration);
            UpdateGatheringVisual(progress);
            if (Time.time < gatheringUntil)
            {
                return;
            }

            var gathered = pendingGatherAmmo;
            ammo = Mathf.Min(EffectiveMaxAmmo, ammo + pendingGatherAmmo);
            ammoGathered += gathered;
            PlaySfx(gatherClip);
            SpawnBurst(playerPosition, 0.72f, 6);
            CreateFloatingText("+1 SNOW", playerPosition + Vector2.up * 0.5f, new Color32(88, 166, 206, 255), 24);
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "gather_complete",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "source", "stationary" },
                    { "ammo_gathered", gathered.ToString() }
                }));
            if (ammo < EffectiveMaxAmmo)
            {
                BeginStationaryGathering();
            }
            else
            {
                ClearGathering();
            }
        }

        private void UpdateGatheringVisual(float progress)
        {
            if (gatheringRenderer == null)
            {
                return;
            }

            gatheringRenderer.gameObject.SetActive(true);
            gatheringRenderer.transform.position = new Vector3(playerPosition.x, playerPosition.y - 0.06f, 0f);
            const float baseScale = 0.64f;
            gatheringRenderer.transform.localScale = Vector3.one * baseScale * Mathf.Lerp(0.72f, 1.18f, progress);
            var color = Color.white;
            color.a = Mathf.Lerp(0.28f, 0.58f, Mathf.Sin(progress * Mathf.PI));
            gatheringRenderer.color = color;
            if (gatherRingRenderer != null)
            {
                gatherRingRenderer.gameObject.SetActive(true);
                gatherRingRenderer.transform.position = new Vector3(playerPosition.x, playerPosition.y - 0.02f, -0.02f);
                gatherRingRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Time.time * 80f);
                gatherRingRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.42f, progress);
                var ringColor = new Color32(88, 166, 206, 255);
                ringColor.a = (byte)Mathf.RoundToInt(Mathf.Lerp(95f, 230f, progress));
                gatherRingRenderer.color = ringColor;
            }
        }

        private void CancelGathering()
        {
            ClearGathering();
            SpawnBurst(playerPosition, 0.28f, 2);
        }

        private void ClearGathering()
        {
            gatheringStartedAt = 0f;
            gatheringUntil = 0f;
            pendingGatherAmmo = 0;
            gatheringKind = PickupKind.Snowball;
            if (gatheringRenderer != null)
            {
                gatheringRenderer.gameObject.SetActive(false);
            }

            if (gatherRingRenderer != null)
            {
                gatherRingRenderer.gameObject.SetActive(false);
            }

            if (gatherBack != null)
            {
                gatherBack.gameObject.SetActive(false);
            }

            if (playerRenderer != null)
            {
                playerRenderer.transform.rotation = Quaternion.identity;
            }
        }

        private static float PickupSpin(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BigSnowdrift:
                    return 6f;
                case PickupKind.Snowdrift:
                    return 10f;
                default:
                    return -18f;
            }
        }

        private void UpdateEnemies()
        {
            var speedMultiplier = GatherAndShotBalance.EnemySpeedMultiplier(elapsedSeconds);
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                var toPlayer = playerPosition - enemy.Position;
                var direction = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
                var wobble = new Vector2(Mathf.Sin(Time.time * 2.1f + enemy.Seed), Mathf.Cos(Time.time * 1.7f + enemy.Seed)) * 0.16f;
                enemy.Position += (direction + wobble).normalized * GatherAndShotBalance.EnemyBaseSpeed(enemy.Kind) * speedMultiplier * Time.deltaTime;
                enemy.Renderer.transform.position = enemy.Position;
                enemy.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 7f + enemy.Seed) * 3.5f);
                if (enemy.Kind == EnemyKind.Runner && Time.time >= enemy.NextTrailAt)
                {
                    enemy.NextTrailAt = Time.time + 0.16f;
                    SpawnTrailMark(enemy.Position - direction * 0.28f, new Color32(236, 94, 123, 105), 0.22f);
                }

                if (direction.x != 0f)
                {
                    var scale = enemy.Renderer.transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (direction.x < 0f ? -1f : 1f);
                    enemy.Renderer.transform.localScale = scale;
                }

                if (Time.time >= contactReadyAt
                    && Vector2.Distance(playerPosition, enemy.Position) <= GatherAndShotBalance.EnemyContactRadius(enemy.Kind))
                {
                    warmth = GatherAndShotBalance.ApplyContactDamage(warmth, GetUpgradeLevel(UpgradeKind.WarmCoat));
                    contactReadyAt = Time.time + GatherAndShotBalance.ContactCooldownSeconds;
                    playerVelocity = (playerPosition - enemy.Position).normalized * 5.2f;
                    SpawnBurst(playerPosition, 0.95f, 8);
                    UpdateTelemetryContext();
                    if (GatherAndShotBalance.IsGameOver(warmth))
                    {
                        EndRun(RunEndReason.WarmthDepleted);
                        return;
                    }
                }
            }
        }

        private void TryAutoFire()
        {
            if (IsGathering || ammo <= 0 || Time.time < nextFireAt)
            {
                return;
            }

            var target = FindNearestEnemyInRange();
            if (target == null)
            {
                return;
            }

            ammo--;
            if (!firstAutoThrowFeedbackShown)
            {
                firstAutoThrowFeedbackShown = true;
                ShowFeedback("AUTO THROW", new Color32(239, 126, 87, 255), 1.4f);
                CreateFloatingText("AUTO THROW", playerPosition + Vector2.up * 0.72f, new Color32(239, 126, 87, 255), 27);
            }

            if (ammo == 0 && !ammoEmptyLogged)
            {
                ammoEmptyLogged = true;
                FirebaseTelemetry.LogEvent("ammo_empty", BuildEventParameters());
            }

            UpdateTelemetryContext();
            nextFireAt = Time.time + GatherAndShotBalance.FireCooldownSeconds(GetUpgradeLevel(UpgradeKind.ThrowRate), Time.time < rapidThrowUntil);
            if (CurrentWeapon == WeaponKind.SnowBurst)
            {
                DamageEnemiesInRadius(playerPosition, 2.15f, GatherAndShotBalance.SnowballDamage(GetUpgradeLevel(UpgradeKind.SnowballDamage)), WeaponKind.SnowBurst);
                SpawnBurst(playerPosition, 1.65f, 18);
                return;
            }

            var renderer = new GameObject("Snowball Projectile", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = snowballSprite;
            renderer.sortingOrder = 18;
            renderer.transform.position = playerPosition;
            var weapon = CurrentWeapon;
            renderer.color = ProjectileColor(weapon);
            renderer.transform.localScale = Vector3.one * ProjectileScale(weapon);
            projectiles.Add(new Projectile
            {
                Renderer = renderer,
                Position = playerPosition,
                Target = target,
                Direction = (target.Position - playerPosition).normalized,
                Life = weapon == WeaponKind.BigSnowball ? 1.55f : 1.2f,
                Kind = weapon,
                Damage = ProjectileDamage(weapon),
                Speed = ProjectileSpeed(weapon),
                PierceRemaining = weapon == WeaponKind.BigSnowball ? 2 : 0,
                SplitDepth = weapon == WeaponKind.SplitSnowball ? 1 : 0
            });
        }

        private Enemy FindNearestEnemyInRange()
        {
            return FindNearestEnemyInRange(playerPosition, GatherAndShotBalance.FireRange, null);
        }

        private Enemy FindNearestEnemyInRange(Vector2 origin, float range, Enemy excluded)
        {
            Enemy nearest = null;
            var bestDistance = range * range;
            foreach (var enemy in enemies)
            {
                if (enemy == excluded)
                {
                    continue;
                }

                var distance = (enemy.Position - origin).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    nearest = enemy;
                    bestDistance = distance;
                }
            }

            return nearest;
        }

        private void SpawnSplitProjectiles(Vector2 origin, Enemy excluded)
        {
            for (var i = 0; i < 3; i++)
            {
                var target = FindNearestEnemyInRange(origin, 4.4f, excluded);
                var direction = target != null
                    ? (target.Position - origin).normalized
                    : (Vector2)(Quaternion.Euler(0f, 0f, i == 0 ? -28f : i == 1 ? 0f : 28f) * Vector3.up);
                var renderer = new GameObject("Split Snowball", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.sprite = snowballSprite;
                renderer.sortingOrder = 18;
                renderer.color = new Color32(174, 231, 255, 255);
                renderer.transform.position = origin;
                renderer.transform.localScale = Vector3.one * 0.24f;
                projectiles.Add(new Projectile
                {
                    Renderer = renderer,
                    Position = origin,
                    Target = target,
                    Direction = direction,
                    Life = 0.62f,
                    Kind = WeaponKind.SplitSnowball,
                    Damage = 1,
                    Speed = 11.8f,
                    PierceRemaining = 0,
                    SplitDepth = 0
                });
            }
        }

        private void DamageEnemiesInRadius(Vector2 origin, float radius, int damage, WeaponKind weapon, Enemy excluded = null)
        {
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == excluded)
                {
                    continue;
                }

                if (Vector2.Distance(origin, enemy.Position) <= radius)
                {
                    DamageEnemy(enemy, damage, weapon);
                }
            }
        }

        private WeaponKind RollWeaponCache()
        {
            var roll = random.Next(3);
            if (roll == 0)
            {
                return WeaponKind.SplitSnowball;
            }

            return roll == 1 ? WeaponKind.IceShot : WeaponKind.SnowBurst;
        }

        private void ActivateWeapon(WeaponKind weapon, string source)
        {
            if (weapon == WeaponKind.BasicSnowball)
            {
                activeWeapon = weapon;
                activeWeaponUntil = 0f;
                return;
            }

            activeWeapon = weapon;
            activeWeaponUntil = Time.time + GatherAndShotBalance.WeaponDurationSeconds(weapon);
            ShowFeedback(GatherAndShotBalance.WeaponName(weapon).ToUpperInvariant(), new Color32(88, 166, 206, 255), 1.8f);
            FirebaseTelemetry.LogEvent(
                "weapon_unlocked",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "weapon", GatherAndShotBalance.WeaponName(weapon) },
                    { "source", source },
                    { "duration", Mathf.RoundToInt(GatherAndShotBalance.WeaponDurationSeconds(weapon)).ToString() }
                }));
        }

        private void ActivateRapidThrow(float duration, string source)
        {
            rapidThrowUntil = Mathf.Max(rapidThrowUntil, Time.time + duration);
            ShowFeedback("RAPID THROW", new Color32(239, 126, 87, 255), 1.6f);
            FirebaseTelemetry.LogEvent(
                "weapon_unlocked",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "weapon", "Rapid Throw" },
                    { "source", source },
                    { "duration", Mathf.RoundToInt(duration).ToString() }
                }));
        }

        private WeaponKind CurrentWeapon
        {
            get
            {
                if (activeWeapon != WeaponKind.BasicSnowball && Time.time <= activeWeaponUntil)
                {
                    return activeWeapon;
                }

                return GetUpgradeLevel(UpgradeKind.SnowballDamage) >= 2 && runNumber >= 2
                    ? WeaponKind.IceShot
                    : WeaponKind.BasicSnowball;
            }
        }

        private int ProjectileDamage(WeaponKind weapon)
        {
            var baseDamage = GatherAndShotBalance.SnowballDamage(GetUpgradeLevel(UpgradeKind.SnowballDamage));
            switch (weapon)
            {
                case WeaponKind.BigSnowball:
                    return baseDamage + 1;
                case WeaponKind.IceShot:
                    return baseDamage + 1;
                case WeaponKind.SnowBurst:
                    return baseDamage;
                default:
                    return baseDamage;
            }
        }

        private float ProjectileSpeed(WeaponKind weapon)
        {
            switch (weapon)
            {
                case WeaponKind.BigSnowball:
                    return 7.4f;
                case WeaponKind.IceShot:
                    return 15.5f;
                default:
                    return GatherAndShotBalance.BaseProjectileSpeed;
            }
        }

        private static float ProjectileScale(WeaponKind weapon)
        {
            switch (weapon)
            {
                case WeaponKind.BigSnowball:
                    return 0.96f;
                case WeaponKind.IceShot:
                    return 0.34f;
                case WeaponKind.SplitSnowball:
                    return 0.36f;
                default:
                    return 0.38f;
            }
        }

        private static Color ProjectileColor(WeaponKind weapon)
        {
            switch (weapon)
            {
                case WeaponKind.BigSnowball:
                    return new Color32(255, 255, 255, 255);
                case WeaponKind.IceShot:
                    return new Color32(140, 222, 255, 255);
                case WeaponKind.SplitSnowball:
                    return new Color32(205, 243, 255, 255);
                default:
                    return Color.white;
            }
        }

        private void UpdateProjectiles()
        {
            for (var i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                projectile.Life -= Time.deltaTime;
                if (projectile.Target != null)
                {
                    projectile.Direction = Vector2.Lerp(projectile.Direction, (projectile.Target.Position - projectile.Position).normalized, Time.deltaTime * 5f).normalized;
                }

                projectile.Position += projectile.Direction * projectile.Speed * Time.deltaTime;
                projectile.Renderer.transform.position = projectile.Position;
                projectile.Renderer.transform.Rotate(0f, 0f, 540f * Time.deltaTime);
                if (projectile.Kind == WeaponKind.IceShot)
                {
                    SpawnTrailMark(projectile.Position - projectile.Direction * 0.18f, new Color32(140, 222, 255, 120), 0.18f);
                }

                var hit = projectile.Target != null
                    && Vector2.Distance(projectile.Position, projectile.Target.Position) <= GatherAndShotBalance.ProjectileHitRadius;
                if (hit)
                {
                    var target = projectile.Target;
                    DamageEnemy(target, projectile.Damage, projectile.Kind);
                    if (projectile.Kind == WeaponKind.BigSnowball)
                    {
                        DamageEnemiesInRadius(projectile.Position, 0.68f, Math.Max(1, projectile.Damage - 1), projectile.Kind, target);
                    }
                    else if (projectile.Kind == WeaponKind.SplitSnowball && projectile.SplitDepth > 0)
                    {
                        SpawnSplitProjectiles(projectile.Position, target);
                    }
                }

                if (hit && projectile.PierceRemaining > 0)
                {
                    projectile.PierceRemaining--;
                    projectile.Target = FindNearestEnemyInRange(projectile.Position, GatherAndShotBalance.FireRange, projectile.Target);
                    hit = projectile.Target == null;
                }

                if (hit || projectile.Life <= 0f)
                {
                    SpawnBurst(projectile.Position, 0.66f, 6);
                    Destroy(projectile.Renderer.gameObject);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void DamageEnemy(Enemy enemy, int damage, WeaponKind weapon)
        {
            if (enemy == null || !enemies.Contains(enemy))
            {
                return;
            }

            enemy.Health -= Mathf.Max(1, damage);
            PlaySfx(hitClip);
            SpawnBurst(enemy.Position, enemy.Kind == EnemyKind.Heavy ? 1.15f : 0.84f, enemy.Kind == EnemyKind.Heavy ? 10 : 7);
            if (enemy.Health > 0)
            {
                enemy.Position += (enemy.Position - playerPosition).normalized * 0.34f;
                return;
            }

            score++;
            switch (enemy.Kind)
            {
                case EnemyKind.Runner:
                    runnersDefeated++;
                    break;
                case EnemyKind.Heavy:
                    heaviesDefeated++;
                    break;
                default:
                    walkersDefeated++;
                    break;
            }

            AwardKillCredit(weapon);
            AwardSnowCoins(GatherAndShotBalance.EnemyCoinReward(enemy.Kind), $"enemy_{enemy.Kind}", enemy.Position);
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "enemy_defeated",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "enemy_kind", enemy.Kind.ToString() },
                    { "weapon", GatherAndShotBalance.WeaponName(weapon) }
                }));
            Destroy(enemy.Renderer.gameObject);
            enemies.Remove(enemy);
        }

        private void SpawnBurst(Vector2 position, float scale, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var renderer = new GameObject("Snow Puff", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.sprite = puffSprite;
                renderer.sortingOrder = 30;
                renderer.transform.position = position;
                renderer.transform.localScale = Vector3.one * RandomRange(0.18f, 0.36f) * scale;
                bursts.Add(new Burst
                {
                    Renderer = renderer,
                    Position = position,
                    Velocity = new Vector2(RandomRange(-1f, 1f), RandomRange(-1f, 1f)).normalized * RandomRange(0.7f, 2.4f) * scale,
                    Life = RandomRange(0.18f, 0.36f)
                });
            }
        }

        private void OfferRewardedReward(RewardedOfferKind kind)
        {
            FirebaseTelemetry.LogEvent(
                "rewarded_offer_shown",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "offer", kind.ToString() },
                    { "mode", "test_hook" }
                }));

            try
            {
                CompleteRewardedReward(kind, true, "test_hook_completed");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Gather & Shot] Rewarded test hook failed: {exception.GetType().Name}");
                FirebaseTelemetry.LogEvent(
                    "rewarded_offer_completed",
                    BuildEventParameters(new Dictionary<string, string>
                    {
                        { "offer", kind.ToString() },
                        { "success", "false" },
                        { "reason", exception.GetType().Name }
                    }));
                RefreshResultPanel();
            }
        }

        private void CompleteRewardedReward(RewardedOfferKind kind, bool success, string reason)
        {
            if (!success)
            {
                FirebaseTelemetry.LogEvent(
                    "rewarded_offer_completed",
                    BuildEventParameters(new Dictionary<string, string>
                    {
                        { "offer", kind.ToString() },
                        { "success", "false" },
                        { "reason", reason }
                    }));
                return;
            }

            switch (kind)
            {
                case RewardedOfferKind.DoubleSnowCoin:
                    if (!doubledCoinsThisRun)
                    {
                        doubledCoinsThisRun = true;
                        AwardSnowCoins(Mathf.Max(6, runEarnedCoins), "rewarded_2x_coin");
                    }

                    break;
                case RewardedOfferKind.Revive:
                    if (!revivedThisRun)
                    {
                        revivedThisRun = true;
                        ReviveRun();
                    }

                    break;
                case RewardedOfferKind.BonusChest:
                    if (!bonusChestOpenedThisRun)
                    {
                        bonusChestOpenedThisRun = true;
                        AwardSnowCoins(GatherAndShotBalance.BonusChestCoins(runNumber), "rewarded_bonus_chest");
                        startWithFullAmmoNextRun = true;
                    }

                    break;
                case RewardedOfferKind.StartWithFullAmmo:
                    startWithFullAmmoNextRun = true;
                    break;
            }

            SaveProgression();
            FirebaseTelemetry.LogEvent(
                "rewarded_offer_completed",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "offer", kind.ToString() },
                    { "success", "true" },
                    { "reason", reason }
                }));
            RefreshResultPanel();
            UpdateHud();
        }

        private void ReviveRun()
        {
            state = GatherAndShotGameState.Playing;
            warmth = EffectiveMaxWarmth * 0.5f;
            ammo = Mathf.Max(ammo, Mathf.Min(EffectiveMaxAmmo, 3));
            resultPanel.SetActive(false);
            contactReadyAt = Time.time + 1.4f;
            nextSpawnAt = Time.time + 0.8f;
            SpawnBurst(playerPosition, 1.4f, 18);
            UpdateTelemetryContext();
        }

        private void UpdateBursts()
        {
            for (var i = bursts.Count - 1; i >= 0; i--)
            {
                var burst = bursts[i];
                burst.Life -= Time.deltaTime;
                burst.Position += burst.Velocity * Time.deltaTime;
                burst.Renderer.transform.position = burst.Position;
                var color = burst.Renderer.color;
                color.a = Mathf.Clamp01(burst.Life / 0.32f);
                burst.Renderer.color = color;
                if (burst.Life <= 0f)
                {
                    Destroy(burst.Renderer.gameObject);
                    bursts.RemoveAt(i);
                }
            }
        }

        private void UpdateTrailMarks()
        {
            for (var i = trailMarks.Count - 1; i >= 0; i--)
            {
                var mark = trailMarks[i];
                mark.Life -= Time.deltaTime;
                var color = mark.Renderer.color;
                color.a = Mathf.Clamp01(mark.Life / mark.StartLife) * mark.StartAlpha;
                mark.Renderer.color = color;
                mark.Renderer.transform.localScale = Vector3.one * Mathf.Lerp(mark.StartScale * 0.74f, mark.StartScale, Mathf.Clamp01(mark.Life / mark.StartLife));
                if (mark.Life <= 0f)
                {
                    Destroy(mark.Renderer.gameObject);
                    trailMarks.RemoveAt(i);
                }
            }
        }

        private void UpdateAmmoStackVisuals()
        {
            var visible = Mathf.Min(ammo, ammoStackRenderers.Count);
            var radius = IsGathering ? 0.6f : 0.48f;
            for (var i = 0; i < ammoStackRenderers.Count; i++)
            {
                var stack = ammoStackRenderers[i];
                if (stack == null)
                {
                    continue;
                }

                var active = i < visible;
                stack.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var angle = i / Mathf.Max(1f, visible) * Mathf.PI * 2f + Time.time * (IsGathering ? 1.45f : 0.42f);
                var ring = i < 12 ? radius : radius + 0.18f;
                stack.transform.position = new Vector3(
                    playerPosition.x + Mathf.Cos(angle) * ring,
                    playerPosition.y + Mathf.Sin(angle) * ring * 0.58f + 0.08f,
                    -0.08f);
                stack.transform.localScale = Vector3.one * (IsGathering ? 0.22f : 0.18f);
            }
        }

        private void UpdateFloatingTexts()
        {
            for (var i = floatingTexts.Count - 1; i >= 0; i--)
            {
                var item = floatingTexts[i];
                item.Life -= Time.deltaTime;
                item.WorldPosition += Vector2.up * Time.deltaTime * 0.72f;
                var viewport = worldCamera.WorldToViewportPoint(item.WorldPosition);
                var rect = gameSquareRoot.rect;
                item.Text.rectTransform.anchoredPosition = new Vector2((viewport.x - 0.5f) * rect.width, (viewport.y - 0.5f) * rect.height);
                var color = item.Text.color;
                color.a = Mathf.Clamp01(item.Life / item.StartLife);
                item.Text.color = color;
                if (item.Life <= 0f)
                {
                    Destroy(item.Text.gameObject);
                    floatingTexts.RemoveAt(i);
                }
            }
        }

        private void UpdateFeedbackText()
        {
            if (feedbackGroup == null)
            {
                return;
            }

            feedbackGroup.alpha = Time.time < feedbackUntil ? 1f : Mathf.MoveTowards(feedbackGroup.alpha, 0f, Time.deltaTime * 3.5f);
        }

        private void ShowFeedback(string message, Color color, float durationSeconds)
        {
            if (feedbackText == null || feedbackGroup == null)
            {
                return;
            }

            feedbackText.text = message;
            feedbackText.color = color;
            feedbackUntil = Time.time + durationSeconds;
            feedbackGroup.alpha = 1f;
        }

        private void CreateFloatingText(string message, Vector2 worldPosition, Color color, int size)
        {
            if (gameSquareRoot == null)
            {
                return;
            }

            var text = CreateText(gameSquareRoot, "World Popup", message, size, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 40f));
            text.color = color;
            text.raycastTarget = false;
            floatingTexts.Add(new FloatingText
            {
                Text = text,
                WorldPosition = worldPosition,
                Life = 1.08f,
                StartLife = 1.08f
            });
        }

        private void SpawnTrailMark(Vector2 position, Color color, float scale)
        {
            var renderer = new GameObject("Snow Trail Mark", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = puffSprite;
            renderer.sortingOrder = -2;
            renderer.color = color;
            renderer.transform.position = new Vector3(position.x, position.y, 0.12f);
            renderer.transform.localScale = Vector3.one * scale;
            trailMarks.Add(new TrailMark
            {
                Renderer = renderer,
                Life = 0.62f,
                StartLife = 0.62f,
                StartAlpha = color.a,
                StartScale = scale
            });
        }

        private void EndRun(RunEndReason reason)
        {
            state = GatherAndShotGameState.GameOver;
            bestScore = Mathf.Max(bestScore, score);
            if (!freeUpgradeClaimed)
            {
                freeUpgradeAvailable = true;
            }

            AwardSnowCoins(GatherAndShotBalance.WaveCoinReward(elapsedSeconds), "wave_survival");
            SaveProgression();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "run_end",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "score", score.ToString() },
                    { "best_score", bestScore.ToString() },
                    { "ammo", ammo.ToString() },
                    { "warmth", Mathf.RoundToInt(warmth).ToString() },
                    { "elapsed_seconds", Mathf.FloorToInt(elapsedSeconds).ToString() },
                    { "end_reason", reason.ToString() }
                }));
            FirebaseTelemetry.LogEvent(
                "run_end_reason",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "end_reason", reason.ToString() }
                }));
            RefreshResultPanel();
            resultPanel.SetActive(true);
            UpdateHud();
            EndJoystick();
            ClearGathering();
            if (runNumber > 3 && Time.realtimeSinceStartup - sessionStartedAt >= 180f)
            {
                MannLabAdMob.TryShowGameOverInterstitial();
            }
        }

        private void RefreshResultPanel()
        {
            if (resultCoinText == null)
            {
                return;
            }

            var recommended = GetRecommendedUpgrade();
            var recommendedLevel = GetUpgradeLevel(recommended);
            var recommendedCost = GatherAndShotBalance.UpgradeCost(recommended, recommendedLevel);
            var canUpgrade = freeUpgradeAvailable && !freeUpgradeClaimed || ownedCoins >= recommendedCost;
            resultCoinText.text = $"SNOW COIN +{runEarnedCoins}\nBAG {ownedCoins}";
            resultStatsText.text =
                $"Kills {score}   Best {bestScore}   Time {Mathf.FloorToInt(elapsedSeconds)}s\n"
                + $"Ammo gathered {ammoGathered}   Pickups {pickupsCollected}\n"
                + $"Weapons B:{killsByBasic} Big:{killsByBig} Split:{killsBySplit} Ice:{killsByIce} Burst:{killsByBurst}";
            resultUpgradeText.text =
                $"Next: {GatherAndShotBalance.UpgradeName(recommended)} Lv {recommendedLevel + 1} "
                + (freeUpgradeAvailable && !freeUpgradeClaimed ? "FREE" : $"{recommendedCost} coins")
                + $"\nAmmo {GetUpgradeLevel(UpgradeKind.AmmoCapacity)}  Gather {GetUpgradeLevel(UpgradeKind.GatherSpeed)}  Throw {GetUpgradeLevel(UpgradeKind.ThrowRate)}  Damage {GetUpgradeLevel(UpgradeKind.SnowballDamage)}  Coat {GetUpgradeLevel(UpgradeKind.WarmCoat)}  Magnet {GetUpgradeLevel(UpgradeKind.CoinMagnet)}";

            SetButtonLabel(upgradeButton, freeUpgradeAvailable && !freeUpgradeClaimed ? "Free Workshop" : canUpgrade ? "Open Workshop" : $"Need {recommendedCost}");
            SetButtonEnabled(upgradeButton, true, canUpgrade ? SketchPalette.WarmHighlight : (Color)new Color32(238, 238, 226, 230));
            SetButtonEnabled(doubleCoinButton, !doubledCoinsThisRun && runEarnedCoins > 0, SketchPalette.WarmHighlight);
            SetButtonEnabled(reviveButton, !revivedThisRun, SketchPalette.WarmHighlight);
            SetButtonEnabled(bonusChestButton, !bonusChestOpenedThisRun, SketchPalette.WarmHighlight);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void SetButtonEnabled(Button button, bool enabled, Color color)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = enabled;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void UpdateHud()
        {
            scoreText.text = score.ToString();
            bestText.text = $"BEST {bestScore}";
            ammoText.text = IsGathering ? $"SNOW {ammo}/{EffectiveMaxAmmo} +" : $"SNOW {ammo}/{EffectiveMaxAmmo}";
            coinText.text = $"COIN +{runEarnedCoins} | BAG {ownedCoins}";
            objectiveText.text = CurrentObjectiveText();
            missionText.text = CurrentMissionText();
            weaponText.text = CurrentWeaponText();
            warmthFill.rectTransform.sizeDelta = new Vector2(WarmthBarWidth * Mathf.Clamp01(warmth / EffectiveMaxWarmth), 0f);
            if (gatherBack == null || gatherFill == null)
            {
                return;
            }

            gatherBack.gameObject.SetActive(IsGathering);
            if (!IsGathering)
            {
                return;
            }

            var duration = Mathf.Max(0.01f, gatheringUntil - gatheringStartedAt);
            var progress = Mathf.Clamp01((Time.time - gatheringStartedAt) / duration);
            gatherFill.rectTransform.sizeDelta = new Vector2(GatherBarWidth * progress, 0f);
        }

        private string CurrentObjectiveText()
        {
            if (freeUpgradeAvailable && !freeUpgradeClaimed)
            {
                return "Free upgrade ready";
            }

            if (!firstMiniGoalCompleted)
            {
                var killsLeft = Mathf.Max(0, GatherAndShotBalance.FirstMiniGoalKills - score);
                if (elapsedSeconds < 20f)
                {
                    return "Move, stop, auto throw";
                }

                return killsLeft > 0 ? $"Mini goal: defeat {killsLeft}" : "Mini goal complete";
            }

            if (elapsedSeconds < 60f)
            {
                return "Grab the big snowdrift";
            }

            if (elapsedSeconds < 120f)
            {
                return "Runner wave: keep moving";
            }

            if (elapsedSeconds < 240f)
            {
                return "Heavy wave: upgrade damage";
            }

            return "Mixed wave: survive the boss push";
        }

        private string CurrentWeaponText()
        {
            var weapon = CurrentWeapon;
            var rapid = Time.time < rapidThrowUntil ? " + RAPID" : string.Empty;
            if (weapon != WeaponKind.BasicSnowball && Time.time < activeWeaponUntil)
            {
                return $"{GatherAndShotBalance.WeaponName(weapon).ToUpperInvariant()} {Mathf.CeilToInt(activeWeaponUntil - Time.time)}s{rapid}";
            }

            return $"{GatherAndShotBalance.WeaponName(weapon).ToUpperInvariant()}{rapid}";
        }

        private void SetMission(MissionKind mission)
        {
            currentMission = mission;
            missionStartScore = score;
            missionStartAmmoGathered = ammoGathered;
            missionStartBigSnowdrifts = bigSnowdriftsCollected;
            missionStartWalkersDefeated = walkersDefeated;
            missionStartRunnersDefeated = runnersDefeated;
            missionStartHeaviesDefeated = heaviesDefeated;
        }

        private void UpdateMissionProgress()
        {
            if (!IsMissionComplete())
            {
                return;
            }

            var completed = currentMission;
            var reward = GatherAndShotBalance.MissionCoinReward(runNumber) + MissionIndex(completed) * 4;
            AwardSnowCoins(reward, $"mission_{completed}", playerPosition);
            ShowFeedback($"MISSION COMPLETE +{reward}", new Color32(239, 126, 87, 255), 1.8f);
            FirebaseTelemetry.LogEvent(
                "mission_complete",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "mission", completed.ToString() },
                    { "reward_coins", reward.ToString() }
                }));

            if (completed == MissionKind.FirstSnowLoop)
            {
                firstMiniGoalCompleted = true;
                ActivateRapidThrow(8f, "mission_first_snow_loop");
                freeUpgradeAvailable = !freeUpgradeClaimed;
            }

            SetMission(NextMission(completed));
        }

        private bool IsMissionComplete()
        {
            switch (currentMission)
            {
                case MissionKind.FirstSnowLoop:
                    return walkersDefeated - missionStartWalkersDefeated >= GatherAndShotBalance.FirstMiniGoalKills
                        || elapsedSeconds >= GatherAndShotBalance.FirstMiniGoalSurvivalSeconds
                        || bigSnowdriftsCollected > missionStartBigSnowdrifts;
                case MissionKind.GatherSnow:
                    return ammoGathered - missionStartAmmoGathered >= 20;
                case MissionKind.CollectBigSnowdrift:
                    return bigSnowdriftsCollected > missionStartBigSnowdrifts;
                case MissionKind.SurviveRunnerWave:
                    return elapsedSeconds >= 75f && runnersDefeated > missionStartRunnersDefeated;
                case MissionKind.DefeatHeavy:
                    return heaviesDefeated > missionStartHeaviesDefeated;
                default:
                    return false;
            }
        }

        private string CurrentMissionText()
        {
            switch (currentMission)
            {
                case MissionKind.FirstSnowLoop:
                    return $"MISSION: defeat {Mathf.Max(0, GatherAndShotBalance.FirstMiniGoalKills - (walkersDefeated - missionStartWalkersDefeated))} walkers";
                case MissionKind.GatherSnow:
                    return $"MISSION: gather {Mathf.Max(0, 20 - (ammoGathered - missionStartAmmoGathered))} snow";
                case MissionKind.CollectBigSnowdrift:
                    return "MISSION: collect big snowdrift";
                case MissionKind.SurviveRunnerWave:
                    return elapsedSeconds < 60f ? "MISSION: reach runner wave" : "MISSION: beat a runner";
                case MissionKind.DefeatHeavy:
                    return elapsedSeconds < 120f ? "MISSION: reach heavy wave" : "MISSION: defeat 1 heavy";
                default:
                    return "MISSION: survive";
            }
        }

        private static MissionKind NextMission(MissionKind mission)
        {
            switch (mission)
            {
                case MissionKind.FirstSnowLoop:
                    return MissionKind.GatherSnow;
                case MissionKind.GatherSnow:
                    return MissionKind.CollectBigSnowdrift;
                case MissionKind.CollectBigSnowdrift:
                    return MissionKind.SurviveRunnerWave;
                case MissionKind.SurviveRunnerWave:
                    return MissionKind.DefeatHeavy;
                default:
                    return MissionKind.DefeatHeavy;
            }
        }

        private static int MissionIndex(MissionKind mission)
        {
            switch (mission)
            {
                case MissionKind.GatherSnow:
                    return 1;
                case MissionKind.CollectBigSnowdrift:
                    return 2;
                case MissionKind.SurviveRunnerWave:
                    return 3;
                case MissionKind.DefeatHeavy:
                    return 4;
                default:
                    return 0;
            }
        }

        private float EffectiveMaxWarmth => GatherAndShotBalance.MaxWarmth(GetUpgradeLevel(UpgradeKind.WarmCoat));

        private int EffectiveMaxAmmo => GatherAndShotBalance.MaxAmmoForLevel(GetUpgradeLevel(UpgradeKind.AmmoCapacity));

        private void UpdateTelemetryContext()
        {
            FirebaseTelemetry.SetContext("run_number", runNumber.ToString());
            FirebaseTelemetry.SetContext("score", score.ToString());
            FirebaseTelemetry.SetContext("best_score", bestScore.ToString());
            FirebaseTelemetry.SetContext("ammo", ammo.ToString());
            FirebaseTelemetry.SetContext("max_ammo", EffectiveMaxAmmo.ToString());
            FirebaseTelemetry.SetContext("warmth", Mathf.RoundToInt(warmth).ToString());
            FirebaseTelemetry.SetContext("elapsed_seconds", Mathf.FloorToInt(elapsedSeconds).ToString());
            FirebaseTelemetry.SetContext("coins_earned", runEarnedCoins.ToString());
            FirebaseTelemetry.SetContext("owned_coins", ownedCoins.ToString());
            FirebaseTelemetry.SetContext("enemy_count", enemies.Count.ToString());
            FirebaseTelemetry.SetContext("pickup_count", pickups.Count.ToString());
            FirebaseTelemetry.SetContext("game_over", state == GatherAndShotGameState.GameOver ? "true" : "false");
            FirebaseTelemetry.SetContext("gathering", IsGathering ? gatheringKind.ToString() : "none");
            FirebaseTelemetry.SetContext("current_weapon", GatherAndShotBalance.WeaponName(CurrentWeapon));
            FirebaseTelemetry.SetContext("upgrade_levels", UpgradeLevelsText());
        }

        private void LogFirstAction(string action)
        {
            if (firstActionLogged)
            {
                return;
            }

            firstActionLogged = true;
            FirebaseTelemetry.LogEvent(
                "first_action",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "action", action }
                }));
        }

        private Dictionary<string, string> BuildEventParameters(Dictionary<string, string> extras = null)
        {
            var parameters = new Dictionary<string, string>
            {
                { "game", "gather_and_shot" },
                { "run_number", runNumber.ToString() },
                { "session_time", Mathf.FloorToInt(Time.realtimeSinceStartup - sessionStartedAt).ToString() },
                { "survival_time", Mathf.FloorToInt(elapsedSeconds).ToString() },
                { "kills", score.ToString() },
                { "current_ammo", ammo.ToString() },
                { "max_ammo", EffectiveMaxAmmo.ToString() },
                { "warmth", Mathf.RoundToInt(warmth).ToString() },
                { "coins_earned", runEarnedCoins.ToString() },
                { "owned_coins", ownedCoins.ToString() },
                { "upgrade_levels", UpgradeLevelsText() },
                { "current_weapon", GatherAndShotBalance.WeaponName(CurrentWeapon) },
                { "enemy_count", enemies.Count.ToString() }
            };

            if (extras == null)
            {
                return parameters;
            }

            foreach (var pair in extras)
            {
                parameters[pair.Key] = pair.Value;
            }

            return parameters;
        }

        private string UpgradeLevelsText()
        {
            return $"{GetUpgradeLevel(UpgradeKind.AmmoCapacity)}.{GetUpgradeLevel(UpgradeKind.GatherSpeed)}.{GetUpgradeLevel(UpgradeKind.ThrowRate)}.{GetUpgradeLevel(UpgradeKind.SnowballDamage)}.{GetUpgradeLevel(UpgradeKind.WarmCoat)}.{GetUpgradeLevel(UpgradeKind.CoinMagnet)}";
        }

        private void AwardKillCredit(WeaponKind weapon)
        {
            switch (weapon)
            {
                case WeaponKind.BigSnowball:
                    killsByBig++;
                    break;
                case WeaponKind.SplitSnowball:
                    killsBySplit++;
                    break;
                case WeaponKind.IceShot:
                    killsByIce++;
                    break;
                case WeaponKind.SnowBurst:
                    killsByBurst++;
                    break;
                default:
                    killsByBasic++;
                    break;
            }
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private IEnumerator ForceCrashlyticsTestAfterStartup()
        {
            yield return new WaitForSecondsRealtime(3f);
            TriggerCrashlyticsTest("launch_flag");
        }

        private bool HandleCrashlyticsTestTrigger()
        {
            if (!TryReadCrashlyticsTestTap(out var position))
            {
                return false;
            }

            if (position.x > CrashlyticsTestTapZoneSize || position.y < Screen.height - CrashlyticsTestTapZoneSize)
            {
                return false;
            }

            if (Time.unscaledTime > crashlyticsTestTapDeadline)
            {
                crashlyticsTestTapCount = 0;
            }

            crashlyticsTestTapDeadline = Time.unscaledTime + CrashlyticsTestTapWindowSeconds;
            crashlyticsTestTapCount++;

            if (crashlyticsTestTapCount < CrashlyticsTestTapCount)
            {
                return true;
            }

            crashlyticsTestTapCount = 0;
            TriggerCrashlyticsTest("hidden_tap");
            return true;
        }

        private void TriggerCrashlyticsTest(string trigger)
        {
            FirebaseTelemetry.SetContext("crashlytics_test", trigger);
            FirebaseTelemetry.LogEvent(
                "crashlytics_test_trigger",
                new Dictionary<string, string>
                {
                    { "trigger", trigger },
                    { "score", score.ToString() },
                    { "best_score", bestScore.ToString() },
                    { "ammo", ammo.ToString() },
                    { "warmth", Mathf.RoundToInt(warmth).ToString() },
                    { "elapsed_seconds", Mathf.FloorToInt(elapsedSeconds).ToString() }
                });
            FirebaseTelemetry.ForceCrashForTesting();
        }

        private static bool TryReadCrashlyticsTestTap(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = Vector2.zero;
            return false;
        }

        private static bool ShouldForceCrashlyticsTestOnLaunch()
        {
            if (IsTruthy(Environment.GetEnvironmentVariable(CrashlyticsTestEnvironmentVariable)))
            {
                return true;
            }

            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, CrashlyticsTestArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
#endif

        private void ClearActors()
        {
            foreach (var enemy in enemies)
            {
                if (enemy.Renderer != null)
                {
                    Destroy(enemy.Renderer.gameObject);
                }
            }

            foreach (var pickup in pickups)
            {
                if (pickup.Renderer != null)
                {
                    Destroy(pickup.Renderer.gameObject);
                }
            }

            foreach (var projectile in projectiles)
            {
                if (projectile.Renderer != null)
                {
                    Destroy(projectile.Renderer.gameObject);
                }
            }

            foreach (var burst in bursts)
            {
                if (burst.Renderer != null)
                {
                    Destroy(burst.Renderer.gameObject);
                }
            }

            foreach (var item in floatingTexts)
            {
                if (item.Text != null)
                {
                    Destroy(item.Text.gameObject);
                }
            }

            foreach (var mark in trailMarks)
            {
                if (mark.Renderer != null)
                {
                    Destroy(mark.Renderer.gameObject);
                }
            }

            enemies.Clear();
            pickups.Clear();
            projectiles.Clear();
            bursts.Clear();
            floatingTexts.Clear();
            trailMarks.Clear();
        }

        private void BuildBoundaryMarkers()
        {
            var fill = new Color32(200, 229, 235, 96);
            var ink = new Color32(122, 170, 178, 84);
            var halfWidth = PlayHalfWidth;
            var horizontalPixels = Mathf.CeilToInt((halfWidth * 2f + 0.4f) * 96f);
            const float edge = 0.1f;

            CreateWorldBand("North Snowbank", new Vector2(0f, WorldHalfHeight - edge), horizontalPixels, 26, fill, 96f, 0f, -18);
            CreateWorldBand("South Snowbank", new Vector2(0f, -WorldHalfHeight + edge), horizontalPixels, 26, fill, 96f, 0f, -18);
            CreateWorldBand("West Snowbank", new Vector2(-halfWidth + edge, 0f), 26, 1365, fill, 96f, 0f, -18);
            CreateWorldBand("East Snowbank", new Vector2(halfWidth - edge, 0f), 26, 1365, fill, 96f, 0f, -18);

            CreateWorldBand("North Edge Ink", new Vector2(0f, WorldHalfHeight - 0.24f), horizontalPixels - 36, 4, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
            CreateWorldBand("South Edge Ink", new Vector2(0f, -WorldHalfHeight + 0.24f), horizontalPixels - 36, 4, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
            CreateWorldBand("West Edge Ink", new Vector2(-halfWidth + 0.24f, 0f), 4, 1300, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
            CreateWorldBand("East Edge Ink", new Vector2(halfWidth - 0.24f, 0f), 4, 1300, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
        }

        private void CreateWorldBand(string name, Vector2 position, int width, int height, Color color, float pixelsPerUnit, float rotation, int sortingOrder)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(name, width, height, color, pixelsPerUnit);
            renderer.transform.position = new Vector3(position.x, position.y, 7f);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            renderer.sortingOrder = sortingOrder;
        }

        private Sprite LoadSprite(string name, Color fallbackColor, int size)
        {
            var texture = Resources.Load<Texture2D>($"GatherAndShot/{name}");
            if (texture == null)
            {
                return CreateSolidSprite(name, size, size, fallbackColor, 96f);
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        }

        private AudioClip CreateToneClip(string name, float frequency, float durationSeconds, float volume)
        {
            var sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * durationSeconds));
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var fade = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * fade;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private static Sprite CreateSolidSprite(string name, int width, int height, Color color, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.name = name;
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private static Sprite CreateRingSprite(string name, int size, int thickness, Color color, float pixelsPerUnit)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var outer = center - 2f;
            var inner = Mathf.Max(1f, outer - thickness);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var edge = Mathf.Clamp01(Mathf.Min(outer - distance, distance - inner) / 3f);
                    var pixel = color;
                    pixel.a *= edge;
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.name = name;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            panel.anchorMin = anchor;
            panel.anchorMax = anchor;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            AddSketchOutline(panel, 3.25f, 3.2f);
            return panel;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions)
        {
            return CreateText(parent, name, value, size, alignment, new Vector2(0.5f, 0.5f), position, dimensions);
        }

        private Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = anchor;
            text.rectTransform.anchorMax = anchor;
            text.rectTransform.pivot = anchor;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = dimensions;
            text.font = GetDefaultFont();
            text.text = value;
            text.fontSize = size;
            text.color = SketchPalette.Ink;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, size - 12);
            text.resizeTextMaxSize = size;
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 dimensions)
        {
            return CreateButton(parent, name, label, position, dimensions, 38);
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 dimensions, int labelSize)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            button.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().anchoredPosition = position;
            button.GetComponent<RectTransform>().sizeDelta = dimensions;
            button.GetComponent<Image>().color = SketchPalette.WarmHighlight;
            button.colors = SketchUiFactory.ButtonColors();
            AddSketchOutline(button.GetComponent<RectTransform>(), 3.25f, 3.2f);
            CreateText(button.transform, $"{name} Label", label, labelSize, TextAnchor.MiddleCenter, Vector2.zero, dimensions);
            return button;
        }

        private void PositionJoystickGuideAtScreenPoint(Vector2 screenPosition)
        {
            if (joystickRoot == null || joystickBase == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickRoot, screenPosition, null, out var localPoint))
            {
                joystickBase.anchoredPosition = localPoint;
            }
        }

        private float PlayHalfWidth => WorldHalfWidth;

        private bool IsGathering => pendingGatherAmmo > 0;

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void AddSketchOutline(RectTransform target, float thickness, float jitter)
        {
            var outline = new GameObject("Sketch Outline", typeof(RectTransform), typeof(SketchOutlineGraphic));
            outline.transform.SetParent(target, false);

            var rect = outline.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var graphic = outline.GetComponent<SketchOutlineGraphic>();
            graphic.color = SketchPalette.Ink;
            graphic.raycastTarget = false;
            graphic.Thickness = thickness;
            graphic.Jitter = jitter;
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private sealed class Enemy
        {
            public EnemyKind Kind;
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public int Health;
            public float Seed;
            public float NextTrailAt;
        }

        private sealed class Pickup
        {
            public PickupKind Kind;
            public SpriteRenderer Renderer;
            public Vector2 Position;
        }

        private sealed class Projectile
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Direction;
            public Enemy Target;
            public float Life;
            public WeaponKind Kind;
            public int Damage;
            public float Speed;
            public int PierceRemaining;
            public int SplitDepth;
        }

        private sealed class Burst
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
        }

        private sealed class FloatingText
        {
            public Text Text;
            public Vector2 WorldPosition;
            public float Life;
            public float StartLife;
        }

        private sealed class TrailMark
        {
            public SpriteRenderer Renderer;
            public float Life;
            public float StartLife;
            public float StartAlpha;
            public float StartScale;
        }
    }
}
