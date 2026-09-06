using System;
using System.Collections.Generic;
using System.Collections;
using MannLab.Ads;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.SensitiveBarista
{
    public sealed class SensitiveBaristaController : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.sensitive_barista.best_score";
        private const float CupCenterX = 0f;
        private const float CupRadius = 0.68f;
        private const float CupBottomY = 0.37f;
        private const float CupTopY = 2.16f;
        private const float PieceLifetime = 7.5f;
        private const float WasteLifetime = 1.15f;
        private const int MaxVisibleIcePieces = 24;
        private const float ParticlePlaneZ = -0.17f;
        private const float WorldPrimaryLine = 0.058f;
        private const float WorldSecondaryLine = 0.028f;
        private const float WorldDoodleLine = 0.018f;
        private const int CupPhysicsLayer = 6;
        private const int LiquidPhysicsLayer = 7;
        private const int IcePhysicsLayer = 8;
        private const string ProductionIosInterstitialAdUnitId = "ca-app-pub-4525914685149405/3848794363";
        private const string ProductionAndroidInterstitialAdUnitId = "";
#if MANNLAB_ADMOB_FORCE_TEST_ADS
        private const int RunCompleteInterstitialInterval = 1;
#else
        private const int RunCompleteInterstitialInterval = 2;
#endif
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private const string CrashlyticsTestArgument = "--mannlab-force-crashlytics-test";
        private const string CrashlyticsTestEnvironmentVariable = "MANNLAB_FORCE_CRASHLYTICS_TEST";
        private const int CrashlyticsTestTapCount = 7;
        private const float CrashlyticsTestTapWindowSeconds = 2.5f;
        private const float CrashlyticsTestTapZoneSize = 220f;
#endif

        private static readonly Vector3 CameraPortraitPosition = new Vector3(0f, 1.48f, -10f);
        private static readonly Vector3 CameraWidePosition = new Vector3(0f, 1.44f, -10f);
        private static readonly Color IceColor = new Color32(186, 234, 255, 215);
        private static readonly Color ShotColor = new Color32(93, 53, 36, 255);
        private static readonly Color WaterColor = new Color32(96, 187, 233, 210);
        private static readonly Color MilkColor = new Color32(255, 226, 166, 245);
        private static readonly Color SyrupColor = new Color32(224, 103, 144, 255);
        private static readonly Color CafeWallColor = new Color32(246, 241, 229, 255);
        private static readonly Color CafeCounterColor = new Color32(241, 230, 204, 255);
        private static readonly Color InkLineColor = new Color32(42, 39, 35, 255);
        private static readonly BaristaIngredient[] LiquidLayerOrder =
        {
            BaristaIngredient.Syrup,
            BaristaIngredient.Shot,
            BaristaIngredient.Milk,
            BaristaIngredient.Water
        };

        private readonly List<PouredPiece> pieces = new List<PouredPiece>();
        private readonly List<LiquidDrop> liquidDrops = new List<LiquidDrop>();
        private readonly Dictionary<BaristaIngredient, DispenserView> dispenserViews =
            new Dictionary<BaristaIngredient, DispenserView>();
        private readonly Dictionary<BaristaIngredient, IngredientButtonView> ingredientButtons =
            new Dictionary<BaristaIngredient, IngredientButtonView>();
        private readonly Dictionary<BaristaIngredient, Transform> fillLayers =
            new Dictionary<BaristaIngredient, Transform>();
        private readonly Dictionary<BaristaIngredient, Transform> fillEdges =
            new Dictionary<BaristaIngredient, Transform>();
        private Camera mainCamera;
        private Canvas canvas;
        private Transform cupCenter;
        private Transform pourRoot;
        private readonly Dictionary<BaristaIngredient, Transform> nozzles =
            new Dictionary<BaristaIngredient, Transform>();
        private readonly Dictionary<BaristaIngredient, Material> ingredientMaterials =
            new Dictionary<BaristaIngredient, Material>();
        private readonly Dictionary<BaristaIngredient, PhysicsMaterial2D> particlePhysicsMaterials =
            new Dictionary<BaristaIngredient, PhysicsMaterial2D>();
        private Material cupMaterial;
        private Material capacityLineMaterial;
        private Material iceOutlineMaterial;
        private Mesh liquidParticleMesh;
        private Mesh waterParticleVisualMesh;
        private Mesh shotParticleVisualMesh;
        private Mesh milkParticleVisualMesh;
        private Mesh syrupParticleVisualMesh;
        private Renderer capacityLineRenderer;
        private Text roundText;
        private Text scoreText;
        private Text bestText;
        private Text orderText;
        private Text moodTagText;
        private Text difficultyText;
        private Text fillText;
        private Text activeIngredientText;
        private Text wasteFeedbackText;
        private Text runMetaText;
        private RectTransform orderPanel;
        private RectTransform recipePanel;
        private Text recipeText;
        private Button recipeButton;
        private Image recipeButtonImage;
        private Button submitButton;
        private Image submitButtonImage;
        private Text submitButtonText;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultScoreText;
        private Text resultDetailText;
        private Text nextButtonText;
        private TextMesh cupStatusText;
        private BaristaOrder[] runOrders;
        private BaristaOrder currentOrder;
        private IngredientAmounts amounts;
        private float wasteAmount;
        private float nextPourAt;
        private float unitFeedbackUntil;
        private float nextTrailingDripAt;
        private float wasteFeedbackUntil;
        private float roundStartedAt;
        private int trailingDripsRemaining;
        private int roundNumber;
        private int runScore;
        private int bestScore;
        private bool roundSubmitted;
        private bool isPouring;
        private BaristaIngredient activeIngredient;
        private BaristaIngredient feedbackIngredient;
        private BaristaIngredient trailingIngredient;
        private string captureMode;
        private int captureOrderIndex = -1;
        private static bool telemetryAndAdsInitialized;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int crashlyticsTestTapCount;
        private float crashlyticsTestTapDeadline;
#endif
        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            InitializeTelemetryAndAds();
            Physics.gravity = new Vector3(0f, -17.5f, 0f);
            Physics.defaultSolverIterations = 14;
            Physics.defaultSolverVelocityIterations = 8;
            Physics.defaultContactOffset = 0.01f;
            Physics2D.gravity = new Vector2(0f, -11.5f);
            Physics2D.velocityIterations = 8;
            Physics2D.positionIterations = 6;
            Physics2D.IgnoreLayerCollision(LiquidPhysicsLayer, LiquidPhysicsLayer, false);
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            captureMode = CaptureQueryValue("capture");
            captureOrderIndex = CaptureQueryInt("captureOrder", -1);

            BuildWorld();
            BuildUi();
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
            UpdateCameraForScreen();
            UpdatePouring();
            UpdatePieces();
            UpdateLiquidDrops();
            UpdateCupFill();
            UpdateFeedbackVisuals();
            UpdateHud();
        }

        private void StartRun()
        {
            var seed = Environment.TickCount ^ Mathf.RoundToInt(Time.realtimeSinceStartup * 1000f);
            runOrders = SensitiveBaristaRules.CreateRunOrders(SensitiveBaristaRules.RoundCount, seed);
            runScore = 0;
            roundNumber = 1;
            FirebaseTelemetry.LogEvent("run_start");
            StartRound();
        }

        private void StartRound()
        {
            ClearCup();
            currentOrder = runOrders != null && roundNumber - 1 < runOrders.Length
                ? runOrders[roundNumber - 1]
                : SensitiveBaristaRules.OrderForRound(roundNumber);
            if (!string.IsNullOrEmpty(captureMode) && captureOrderIndex >= 0)
            {
                currentOrder = SensitiveBaristaRules.Orders[
                    Mathf.Clamp(captureOrderIndex, 0, SensitiveBaristaRules.Orders.Length - 1)];
            }

            amounts = new IngredientAmounts(0f, 0f, 0f, 0f, 0f);
            wasteAmount = 0f;
            roundSubmitted = false;
            isPouring = false;
            unitFeedbackUntil = 0f;
            trailingDripsRemaining = 0;
            roundStartedAt = Time.time;
            wasteFeedbackUntil = 0f;
            resultPanel.SetActive(false);
            recipePanel.gameObject.SetActive(false);
            orderText.text = currentOrder.CustomerLine;
            moodTagText.text = MoodTagsFor(currentOrder);
            activeIngredientText.text = string.Empty;
            recipeText.text = SensitiveBaristaRules.RecipeMemoFor(currentOrder);
            UpdateHud();
            ApplyCaptureModeIfNeeded();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "round_start",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "round", roundNumber.ToString() },
                    { "order", currentOrder.MemoName }
                }));
        }

        private void ApplyCaptureModeIfNeeded()
        {
            if (string.IsNullOrEmpty(captureMode) || captureMode == "ready")
            {
                return;
            }

            if (captureMode != "mix" && captureMode != "result")
            {
                return;
            }

            BuildCaptureDrink();
            if (captureMode == "result")
            {
                SubmitRound();
            }

            UpdateCupFill();
            UpdateHud();
        }

        private void BuildCaptureDrink()
        {
            ClearCup();
            wasteAmount = 0f;
            isPouring = false;
            trailingDripsRemaining = 0;

            var target = currentOrder.Target;
            var scale = target.Total <= 0f ? 0f : currentOrder.IdealTotal / target.Total;
            amounts = new IngredientAmounts(
                target.Ice * scale,
                target.Shot * scale,
                target.Water * scale,
                target.Milk * scale,
                target.Syrup * scale);
            SpawnCaptureIcePieces(amounts.Ice);
        }

        private void SpawnCaptureIcePieces(float iceAmount)
        {
            var count = Mathf.Clamp(Mathf.RoundToInt(iceAmount / 4.2f), 0, 9);
            if (count <= 0)
            {
                return;
            }

            var liquidTotal = LiquidIngredientTotal(amounts);
            var surfaceY = liquidTotal > 2f ? LiquidSurfaceYFor(amounts) + 0.025f : CupBottomY + 0.22f;
            for (var index = 0; index < count; index += 1)
            {
                var piece = CreatePhysicsParticleObject(BaristaIngredient.Ice);
                piece.name = "Capture Ice Piece";
                piece.transform.SetParent(pourRoot, true);
                piece.transform.localScale = ScaleForIngredient(BaristaIngredient.Ice);
                var xT = count <= 1 ? 0.5f : index / (float)(count - 1);
                var yOffset = Mathf.Sin(index * 1.73f) * 0.035f + UnityEngine.Random.Range(-0.01f, 0.03f);
                var y = Mathf.Clamp(surfaceY + yOffset, CupBottomY + 0.2f, CupTopY - 0.14f);
                var halfWidth = Mathf.Max(0.12f, CupWidthAtY(y) * 0.43f);
                var x = Mathf.Lerp(-halfWidth, halfWidth, xT) + UnityEngine.Random.Range(-0.045f, 0.045f);
                piece.transform.position = new Vector3(CupCenterX + x, y, ParticlePlaneZ);
                piece.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-22f, 22f));
                piece.GetComponent<Renderer>().sharedMaterial = ingredientMaterials[BaristaIngredient.Ice];
                piece.GetComponent<Renderer>().sortingOrder = ParticleSortingOrderFor(BaristaIngredient.Ice);

                var body = piece.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.linearVelocity = new Vector2(UnityEngine.Random.Range(-0.04f, 0.04f), UnityEngine.Random.Range(-0.03f, 0.04f));
                    body.angularVelocity = UnityEngine.Random.Range(-8f, 8f);
                }

                pieces.Add(new PouredPiece(
                    piece,
                    body,
                    BaristaIngredient.Ice,
                    0f,
                    Time.time,
                    piece.transform.position,
                    piece.transform.position,
                    piece.transform.rotation,
                    piece.transform.rotation,
                    PieceLifetime));
            }
        }

        private void ClearCurrentDrink()
        {
            if (roundSubmitted)
            {
                return;
            }

            ClearCup();
            amounts = new IngredientAmounts(0f, 0f, 0f, 0f, 0f);
            wasteAmount = 0f;
            isPouring = false;
            trailingDripsRemaining = 0;
            activeIngredientText.text = string.Empty;
            wasteFeedbackText.text = string.Empty;
            wasteFeedbackUntil = 0f;
            UpdateHud();
            FirebaseTelemetry.LogEvent(
                "clear_cup",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "round", roundNumber.ToString() }
                }));
        }

        private void SubmitRound()
        {
            if (roundSubmitted)
            {
                return;
            }

            if (amounts.Total < SensitiveBaristaRules.MinimumPlayableAmount)
            {
                activeIngredientText.text = "add a little first";
                return;
            }

            isPouring = false;
            roundSubmitted = true;
            var score = SensitiveBaristaRules.Score(currentOrder, amounts, wasteAmount, roundNumber);
            runScore += score.RoundScore;
            var isFinalRound = roundNumber >= SensitiveBaristaRules.RoundCount;
            var isNewBest = isFinalRound && runScore > bestScore;
            if (isFinalRound && runScore > bestScore)
            {
                bestScore = runScore;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }

            resultPanel.SetActive(true);
            resultTitleText.text = isFinalRound ? "Run Complete" : score.Comment;
            resultScoreText.text = isFinalRound
                ? $"{runScore}"
                : $"+{score.RoundScore}";
            var runScoreLine = isFinalRound
                ? $"Total Score {runScore} / {SensitiveBaristaRules.RoundCount * 100}"
                : $"Round +{score.RoundScore}   Run Score {runScore}";
            resultDetailText.text =
                $"{currentOrder.MemoName}\n" +
                $"{runScoreLine}\n" +
                $"Balance {score.BalanceGrade}   Volume {score.VolumeGrade}   Clean {score.CleanlinessGrade}\n" +
                $"{JudgeNudge(score, currentOrder, amounts)}\n" +
                $"Mix: {score.ActualRatio}\n" +
                $"Hint: {currentOrder.MemoRatio}\n" +
                $"{VolumeSummary(amounts.Total)}   Waste {Mathf.RoundToInt(wasteAmount)}" +
                (isFinalRound
                    ? $"\nAverage {Mathf.RoundToInt(runScore / (float)SensitiveBaristaRules.RoundCount)}   {(isNewBest ? "New Best" : $"Best {bestScore}")}"
                    : string.Empty);
            nextButtonText.text = isFinalRound ? "New Run" : "Next Order";
            FirebaseTelemetry.LogEvent(
                isFinalRound ? "run_complete" : "round_submit",
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "round", roundNumber.ToString() },
                    { "order", currentOrder.MemoName },
                    { "round_score", score.RoundScore.ToString() },
                    { "run_score", runScore.ToString() },
                    { "best_score", bestScore.ToString() },
                    { "fill", Mathf.RoundToInt(amounts.Total).ToString() },
                    { "waste", Mathf.RoundToInt(wasteAmount).ToString() },
                    { "balance_grade", score.BalanceGrade },
                    { "volume_grade", score.VolumeGrade },
                    { "clean_grade", score.CleanlinessGrade }
                }));
            UpdateTelemetryContext();
            if (isFinalRound)
            {
                MannLabAdMob.TryShowGameOverInterstitial();
            }

            UpdateHud();
        }

        private void ContinueAfterResult()
        {
            if (!roundSubmitted)
            {
                return;
            }

            if (roundNumber >= SensitiveBaristaRules.RoundCount)
            {
                StartRun();
                return;
            }

            roundNumber += 1;
            StartRound();
        }

        private static void InitializeTelemetryAndAds()
        {
            if (telemetryAndAdsInitialized)
            {
                return;
            }

            telemetryAndAdsInitialized = true;
            try
            {
                FirebaseTelemetry.Initialize();
                FirebaseTelemetry.SetContext("game", "too-picky-coffee");
                FirebaseTelemetry.LogEvent("app_open");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Too Picky Coffee] Firebase initialization skipped: {exception.GetType().Name}");
            }

            try
            {
                MannLabAdMob.InitializeGameOverInterstitial(
                    "too-picky-coffee",
                    ProductionIosInterstitialAdUnitId,
                    RunCompleteInterstitialInterval,
                    ProductionAndroidInterstitialAdUnitId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Too Picky Coffee] AdMob initialization skipped: {exception.GetType().Name}");
            }
        }

        private void UpdateTelemetryContext()
        {
            FirebaseTelemetry.SetContext("round", roundNumber.ToString());
            FirebaseTelemetry.SetContext("run_score", runScore.ToString());
            FirebaseTelemetry.SetContext("best_score", bestScore.ToString());
            FirebaseTelemetry.SetContext("order", currentOrder.MemoName ?? string.Empty);
            FirebaseTelemetry.SetContext("fill", Mathf.RoundToInt(amounts.Total).ToString());
            FirebaseTelemetry.SetContext("waste", Mathf.RoundToInt(wasteAmount).ToString());
        }

        private Dictionary<string, string> BuildEventParameters(Dictionary<string, string> extra = null)
        {
            var parameters = new Dictionary<string, string>
            {
                { "round", roundNumber.ToString() },
                { "run_score", runScore.ToString() },
                { "best_score", bestScore.ToString() },
                { "order", currentOrder.MemoName ?? string.Empty },
                { "ice", Mathf.RoundToInt(amounts.Ice).ToString() },
                { "shot", Mathf.RoundToInt(amounts.Shot).ToString() },
                { "water", Mathf.RoundToInt(amounts.Water).ToString() },
                { "milk", Mathf.RoundToInt(amounts.Milk).ToString() },
                { "syrup", Mathf.RoundToInt(amounts.Syrup).ToString() },
                { "total", Mathf.RoundToInt(amounts.Total).ToString() }
            };

            if (extra == null)
            {
                return parameters;
            }

            foreach (var pair in extra)
            {
                parameters[pair.Key] = pair.Value;
            }

            return parameters;
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
                BuildEventParameters(new Dictionary<string, string>
                {
                    { "trigger", trigger }
                }));
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
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value == "1" ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }
#endif

        private static string CaptureQueryValue(string key)
        {
            var url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var queryStart = url.IndexOf('?');
            if (queryStart < 0 || queryStart >= url.Length - 1)
            {
                return string.Empty;
            }

            var hashStart = url.IndexOf('#', queryStart + 1);
            var queryLength = hashStart < 0 ? url.Length - queryStart - 1 : hashStart - queryStart - 1;
            var query = url.Substring(queryStart + 1, queryLength);
            var pairs = query.Split('&');
            for (var index = 0; index < pairs.Length; index += 1)
            {
                var pair = pairs[index];
                if (pair.Length == 0)
                {
                    continue;
                }

                var separator = pair.IndexOf('=');
                var pairKey = separator < 0 ? pair : pair.Substring(0, separator);
                if (!string.Equals(Uri.UnescapeDataString(pairKey), key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return separator < 0
                    ? string.Empty
                    : Uri.UnescapeDataString(pair.Substring(separator + 1)).ToLowerInvariant();
            }

            return string.Empty;
        }

        private static int CaptureQueryInt(string key, int fallback)
        {
            var value = CaptureQueryValue(key);
            return int.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private void BuildWorld()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.GetComponent<Camera>();
            }

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = SketchPalette.Paper;
            mainCamera.orthographic = true;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 80f;
            mainCamera.fieldOfView = 36f;

            ingredientMaterials[BaristaIngredient.Ice] = CreateMaterial("Ice", IceColor, 0.86f);
            ingredientMaterials[BaristaIngredient.Shot] = CreateMaterial("Shot", ShotColor, 1f);
            ingredientMaterials[BaristaIngredient.Water] = CreateMaterial("Water", WaterColor, 0.86f);
            ingredientMaterials[BaristaIngredient.Milk] = CreateMaterial("Milk", MilkColor, 0.96f);
            ingredientMaterials[BaristaIngredient.Syrup] = CreateMaterial("Syrup", SyrupColor, 0.96f);
            particlePhysicsMaterials[BaristaIngredient.Ice] = new PhysicsMaterial2D("Ice Particle Physics") { friction = 0.42f, bounciness = 0.015f };
            particlePhysicsMaterials[BaristaIngredient.Shot] = new PhysicsMaterial2D("Shot Particle Physics") { friction = 0.08f, bounciness = 0.03f };
            particlePhysicsMaterials[BaristaIngredient.Water] = new PhysicsMaterial2D("Water Particle Physics") { friction = 0.02f, bounciness = 0.04f };
            particlePhysicsMaterials[BaristaIngredient.Milk] = new PhysicsMaterial2D("Milk Particle Physics") { friction = 0.12f, bounciness = 0.015f };
            particlePhysicsMaterials[BaristaIngredient.Syrup] = new PhysicsMaterial2D("Syrup Particle Physics") { friction = 0.6f, bounciness = 0f };
            liquidParticleMesh = CreateCircleMesh2D("Shared Liquid Particle Mesh", 8);
            waterParticleVisualMesh = CreateCircleMesh2D("Shared Water Visual Particle Mesh", 8, 0.64f);
            shotParticleVisualMesh = CreateCircleMesh2D("Shared Shot Visual Particle Mesh", 8, 0.62f);
            milkParticleVisualMesh = CreateCircleMesh2D("Shared Milk Visual Particle Mesh", 8, 0.63f);
            syrupParticleVisualMesh = CreateCircleMesh2D("Shared Syrup Visual Particle Mesh", 8, 0.6f);
            cupMaterial = CreateMaterial("Cup Glass", new Color32(205, 228, 230, 255), 0.22f);
            iceOutlineMaterial = CreateMaterial("Ice Sketch Ink", new Color32(57, 99, 116, 255), 0.46f);

            var tableMaterial = CreateMaterial("Warm Table", CafeCounterColor, 1f);
            var inkMaterial = CreateMaterial("Ink", InkLineColor, 1f);
            var metalMaterial = CreateMaterial("Nozzle Metal", new Color32(126, 125, 118, 255), 1f);

            var lightObject = new GameObject("Key Light", typeof(Light));
            lightObject.transform.position = new Vector3(-3.2f, 5f, -3.8f);
            lightObject.transform.rotation = Quaternion.Euler(50f, -34f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color32(255, 249, 235, 255);

            var fillObject = new GameObject("Soft Fill", typeof(Light));
            fillObject.transform.position = new Vector3(3f, 3.4f, 2f);
            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 0.92f;
            fill.range = 8f;

            BuildCafeBackdrop(inkMaterial);

            var table = CreatePrimitiveObject(PrimitiveType.Cube);
            table.name = "Counter Physics Plane";
            table.transform.position = new Vector3(0f, -0.1f, 0.32f);
            table.transform.localScale = new Vector3(8.2f, 0.12f, 2.4f);
            table.GetComponent<Renderer>().sharedMaterial = tableMaterial;

            cupCenter = new GameObject("Cup Center").transform;
            cupCenter.position = new Vector3(CupCenterX, 0f, 0.1f);
            pourRoot = new GameObject("Pour Pieces").transform;

            BuildSinglePourNozzle(metalMaterial, inkMaterial);
            BuildCup(inkMaterial);
            UpdateCameraForScreen();
        }

        private void BuildCafeBackdrop(Material inkMaterial)
        {
            var wallMaterial = CreateMaterial("Cafe Wall Paper", CafeWallColor, 1f);
            var counterFaceMaterial = CreateMaterial("Counter Face Paper", CafeCounterColor, 1f);
            var faintInkMaterial = CreateMaterial("Faint Table Ink", new Color32(116, 99, 78, 255), 0.2f);

            CreateSketchCube("Cafe Back Wall", new Vector3(0f, 2.42f, 2.8f), new Vector3(8.8f, 3.9f, 0.08f), wallMaterial);
            CreateSketchCube("Counter Face", new Vector3(0f, 0.16f, 2.65f), new Vector3(8.8f, 0.7f, 0.08f), counterFaceMaterial);
            CreateSketchCube("Counter Top Ink Line Left", new Vector3(-2.0f, 0.455f, 2.58f), new Vector3(2.2f, 0.014f, 0.06f), inkMaterial);
            CreateSketchCube("Counter Top Ink Line Right", new Vector3(2.0f, 0.455f, 2.58f), new Vector3(2.2f, 0.014f, 0.06f), inkMaterial);
            CreateSketchCube("Counter Bottom Soft Line", new Vector3(0f, -0.18f, 2.58f), new Vector3(6.6f, 0.014f, 0.06f), faintInkMaterial);

            for (var index = 0; index < 3; index += 1)
            {
                var x = -2.2f + index * 2.2f;
                var line = CreateSketchCube(
                    $"Counter Pencil Grain {index + 1}",
                    new Vector3(x, 0.14f + Mathf.Sin(index * 1.7f) * 0.05f, 2.54f),
                    new Vector3(0.28f + (index % 2) * 0.08f, WorldDoodleLine * 0.7f, 0.026f),
                    faintInkMaterial);
                line.transform.rotation = Quaternion.Euler(0f, 0f, -1f + index * 0.6f);
            }
        }

        private void BuildCup(Material inkMaterial)
        {
            var glass = new GameObject("Clean Glass Cup", typeof(MeshFilter), typeof(MeshRenderer));
            glass.GetComponent<MeshFilter>().mesh = CreateFrontCupMesh("Clean Glass Cup Mesh");
            glass.GetComponent<MeshRenderer>().sharedMaterial = cupMaterial;
            cupStatusText = CreateWorldLabel(
                "Cup Status Amount",
                "0 / 100",
                new Vector3(CupCenterX, CupBottomY - 0.28f, -0.18f),
                0.024f,
                SketchPalette.MutedInk);
            cupStatusText.gameObject.SetActive(false);

            var cupTopY = CupTopY - 0.02f;
            var cupFootY = CupBottomY + 0.12f;
            CreateLine2D("Cup Top Rim", new Vector2(CupCenterX - CupRadius * 1.04f, cupTopY), new Vector2(CupCenterX + CupRadius * 1.04f, cupTopY), 0.028f, inkMaterial, -0.18f);
            CreateLine2D("Cup Bottom Rim", new Vector2(CupCenterX - CupRadius * 0.64f, cupFootY), new Vector2(CupCenterX + CupRadius * 0.64f, cupFootY), 0.022f, inkMaterial, -0.205f);
            CreateLine2D("Cup Inner Floor", new Vector2(CupCenterX - CupRadius * 0.56f, cupFootY + 0.035f), new Vector2(CupCenterX + CupRadius * 0.56f, cupFootY + 0.035f), 0.012f, inkMaterial, -0.206f);
            CreateLine2D("Cup Left Side", new Vector2(CupCenterX - CupRadius * 1.02f, cupTopY), new Vector2(CupCenterX - CupRadius * 0.64f, cupFootY), 0.026f, inkMaterial, -0.18f);
            CreateLine2D("Cup Right Side", new Vector2(CupCenterX + CupRadius * 1.02f, cupTopY), new Vector2(CupCenterX + CupRadius * 0.64f, cupFootY), 0.026f, inkMaterial, -0.18f);

            capacityLineMaterial = CreateMaterial("Capacity Warning Line", SketchPalette.WarningAmber, 0.72f);
            var capacityLine = CreateLine2D(
                "Cup Capacity Line",
                new Vector2(CupCenterX - CupRadius * 0.82f, CupBottomY + (CupTopY - CupBottomY) * 0.8f),
                new Vector2(CupCenterX + CupRadius * 0.82f, CupBottomY + (CupTopY - CupBottomY) * 0.8f),
                0.012f,
                capacityLineMaterial,
                -0.19f);
            capacityLineRenderer = capacityLine.GetComponent<Renderer>();

            var baseDisc = CreatePrimitiveObject(PrimitiveType.Cylinder);
            baseDisc.name = "Cup Base";
            baseDisc.transform.position = new Vector3(CupCenterX, CupBottomY + 0.02f, 0.1f);
            baseDisc.transform.localScale = new Vector3(CupRadius * 0.72f, 0.08f, CupRadius * 0.72f);
            baseDisc.GetComponent<Renderer>().sharedMaterial = inkMaterial;
            baseDisc.GetComponent<Renderer>().enabled = false;
            var baseCollider = baseDisc.GetComponent<Collider>();
            if (baseCollider != null)
            {
                baseCollider.material = CreatePhysicsMaterial("Cup Base Physics", 0.72f, 0.18f);
            }

            var wallCenterY = (CupBottomY + CupTopY) * 0.5f;
            var wallHeight = CupTopY - CupBottomY;
            CreateCupWall("Cup Wall North", new Vector3(CupCenterX, wallCenterY, CupRadius + 0.1f), new Vector3(CupRadius * 1.6f, wallHeight, 0.08f), inkMaterial);
            CreateCupWall("Cup Wall South", new Vector3(CupCenterX, wallCenterY, -CupRadius + 0.1f), new Vector3(CupRadius * 1.6f, wallHeight, 0.08f), inkMaterial);
            CreateCupWall("Cup Wall West", new Vector3(CupCenterX - CupRadius, wallCenterY, 0.1f), new Vector3(0.08f, wallHeight, CupRadius * 1.6f), inkMaterial);
            CreateCupWall("Cup Wall East", new Vector3(CupCenterX + CupRadius, wallCenterY, 0.1f), new Vector3(0.08f, wallHeight, CupRadius * 1.6f), inkMaterial);
            BuildCupPhysics2D();
            BuildCupFillLayers();
        }

        private void BuildCupPhysics2D()
        {
            var leftTop = new Vector2(CupCenterX - CupRadius * 1.02f, CupTopY - 0.04f);
            var rightTop = new Vector2(CupCenterX + CupRadius * 1.02f, CupTopY - 0.04f);
            var leftBottom = new Vector2(CupCenterX - CupRadius * 0.62f, CupBottomY + 0.13f);
            var rightBottom = new Vector2(CupCenterX + CupRadius * 0.62f, CupBottomY + 0.13f);
            var material = new PhysicsMaterial2D("Cup 2D Physics")
            {
                friction = 0.35f,
                bounciness = 0.02f
            };

            CreateCupEdge2D("Cup 2D Left Wall", leftBottom, leftTop, material);
            CreateCupEdge2D("Cup 2D Right Wall", rightBottom, rightTop, material);
            CreateCupEdge2D("Cup 2D Bottom", leftBottom + new Vector2(0.02f, 0f), rightBottom - new Vector2(0.02f, 0f), material);
            CreateCupBottomPlug2D("Cup 2D Bottom Plug", new Vector2(CupCenterX, CupBottomY + 0.025f), new Vector2(CupRadius * 1.42f, 0.09f), material);
        }

        private static void CreateCupEdge2D(string name, Vector2 start, Vector2 end, PhysicsMaterial2D material)
        {
            var edgeObject = new GameObject(name, typeof(EdgeCollider2D));
            edgeObject.layer = CupPhysicsLayer;
            var edge = edgeObject.GetComponent<EdgeCollider2D>();
            edge.points = new[] { start, end };
            edge.edgeRadius = 0.032f;
            edge.sharedMaterial = material;
        }

        private static void CreateCupBottomPlug2D(string name, Vector2 center, Vector2 size, PhysicsMaterial2D material)
        {
            var plugObject = new GameObject(name, typeof(BoxCollider2D));
            plugObject.layer = CupPhysicsLayer;
            plugObject.transform.position = new Vector3(center.x, center.y, 0f);
            var collider = plugObject.GetComponent<BoxCollider2D>();
            collider.size = size;
            collider.sharedMaterial = material;
        }

        private void BuildCupFillLayers()
        {
            foreach (var ingredient in SensitiveBaristaRules.Ingredients)
            {
                if (ingredient == BaristaIngredient.Ice)
                {
                    continue;
                }

                var ingredientColor = IngredientColor(ingredient);
                var layer = new GameObject(
                    $"{SensitiveBaristaRules.IngredientName(ingredient)} Fill Layer",
                    typeof(MeshFilter),
                    typeof(MeshRenderer));
                layer.name = $"{SensitiveBaristaRules.IngredientName(ingredient)} Fill Layer";
                layer.transform.position = Vector3.zero;
                layer.transform.localScale = Vector3.one;
                layer.GetComponent<MeshFilter>().mesh = CreateLiquidLayerMesh(
                    $"{SensitiveBaristaRules.IngredientName(ingredient)} Fill Mesh",
                    CupBottomY + 0.08f,
                    CupBottomY + 0.081f);
                var layerRenderer = layer.GetComponent<Renderer>();
                layerRenderer.sharedMaterial = CreateMaterial(
                    $"{SensitiveBaristaRules.IngredientName(ingredient)} Fill",
                    ingredientColor,
                    ingredient == BaristaIngredient.Milk ? 0.84f : 0.76f);
                layerRenderer.sortingOrder = ParticleSortingOrderFor(ingredient);
                layerRenderer.enabled = false;
                fillLayers[ingredient] = layer.transform;

                var edge = CreatePrimitiveObject(PrimitiveType.Cube);
                edge.name = $"{SensitiveBaristaRules.IngredientName(ingredient)} Fill Ink Edge";
                edge.transform.position = new Vector3(0f, CupBottomY, -0.175f);
                edge.transform.localScale = new Vector3(CupRadius * 1.22f, WorldDoodleLine, 0.035f);
                edge.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                    $"{SensitiveBaristaRules.IngredientName(ingredient)} Fill Edge",
                    Color.Lerp(ingredientColor, InkLineColor, 0.36f),
                    ingredient == BaristaIngredient.Milk ? 0.42f : 0.55f);
                edge.GetComponent<Renderer>().sortingOrder = ParticleSortingOrderFor(ingredient) + 1;
                edge.GetComponent<Renderer>().enabled = false;
                Destroy(edge.GetComponent<Collider>());
                fillEdges[ingredient] = edge.transform;
            }
        }

        private void CreateCupWall(string name, Vector3 position, Vector3 scale, Material material)
        {
            var wall = CreatePrimitiveObject(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = material;
            var renderer = wall.GetComponent<Renderer>();
            renderer.enabled = false;
            var collider = wall.GetComponent<Collider>();
            if (collider != null)
            {
                collider.material = CreatePhysicsMaterial($"{name} Physics", 0.6f, 0.12f);
            }
        }

        private void BuildSinglePourNozzle(Material metalMaterial, Material inkMaterial)
        {
            var guideMaterial = CreateMaterial("Pour Guide Paper", new Color32(255, 253, 247, 246), 0.12f);
            var guideCenter = new Vector3(CupCenterX, CupTopY + 0.24f, 0.08f);
            var pointPosition = new Vector3(CupCenterX, CupTopY + 0.08f, 0f);

            var guide = CreateSketchCube("Single Pour Guide Hit Area", guideCenter, new Vector3(0.72f, 0.22f, 0.08f), guideMaterial);
            guide.GetComponent<Renderer>().enabled = false;
            var guideCollider = guide.GetComponent<Collider>();
            if (guideCollider != null)
            {
                guideCollider.isTrigger = true;
            }

            CreateLine2D("Pour Nozzle Rail", new Vector2(CupCenterX - 0.22f, CupTopY + 0.22f), new Vector2(CupCenterX + 0.22f, CupTopY + 0.22f), 0.02f, metalMaterial, -0.14f);

            var commonNozzle = CreatePrimitiveObject(PrimitiveType.Cylinder);
            commonNozzle.name = "Common Pour Nozzle";
            commonNozzle.transform.position = pointPosition + new Vector3(0f, 0.08f, -0.04f);
            commonNozzle.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            commonNozzle.transform.localScale = new Vector3(0.055f, 0.18f, 0.055f);
            commonNozzle.GetComponent<Renderer>().sharedMaterial = metalMaterial;
            Destroy(commonNozzle.GetComponent<Collider>());

            var labels = new[]
            {
                new ButtonSpec(BaristaIngredient.Ice, "I"),
                new ButtonSpec(BaristaIngredient.Shot, "S"),
                new ButtonSpec(BaristaIngredient.Water, "W"),
                new ButtonSpec(BaristaIngredient.Milk, "M"),
                new ButtonSpec(BaristaIngredient.Syrup, "Y")
            };

            for (var index = 0; index < labels.Length; index += 1)
            {
                var spec = labels[index];
                var swatchPosition = guideCenter + new Vector3(0f, -0.15f, -0.12f);
                var body = CreateSketchCube(
                    $"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Pour Feedback Pivot",
                    swatchPosition,
                    new Vector3(0.1f, 0.04f, 0.035f),
                    guideMaterial);
                body.GetComponent<Renderer>().enabled = false;

                var activeMark = CreateSketchCube(
                    $"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Pour Active Mark",
                    pointPosition + new Vector3(0f, 0.24f, -0.14f),
                    new Vector3(0.12f, 0.035f, 0.04f),
                    ingredientMaterials[spec.Ingredient]);
                var activeRenderer = activeMark.GetComponent<Renderer>();
                activeRenderer.enabled = false;

                var nozzle = new GameObject($"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Common Nozzle Pivot").transform;
                nozzle.position = commonNozzle.transform.position;
                nozzle.localScale = commonNozzle.transform.localScale;

                var stream = CreatePrimitiveObject(PrimitiveType.Cube);
                stream.name = $"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Common Stream";
                stream.transform.position = pointPosition + new Vector3(0f, -0.18f, -0.16f);
                stream.transform.localScale = new Vector3(0.035f, 0.36f, 0.035f);
                stream.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                    $"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Common Stream Material",
                    IngredientColor(spec.Ingredient),
                    spec.Ingredient == BaristaIngredient.Ice ? 0.34f : 0.62f);
                stream.GetComponent<Renderer>().enabled = false;
                Destroy(stream.GetComponent<Collider>());

                var streamPieces = new Transform[3];
                var streamRenderers = new Renderer[3];
                for (var pieceIndex = 0; pieceIndex < streamPieces.Length; pieceIndex += 1)
                {
                    var primitive = spec.Ingredient == BaristaIngredient.Ice ? PrimitiveType.Cube : PrimitiveType.Sphere;
                    var blob = CreatePrimitiveObject(primitive);
                    blob.name = $"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Stream Blob {pieceIndex + 1}";
                    blob.transform.position = pointPosition + new Vector3(0f, -0.14f - pieceIndex * 0.16f, -0.18f);
                    blob.transform.localScale = Vector3.one * (spec.Ingredient == BaristaIngredient.Ice ? 0.09f : 0.07f);
                    blob.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                        $"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Stream Blob Material {pieceIndex + 1}",
                        IngredientColor(spec.Ingredient),
                        spec.Ingredient == BaristaIngredient.Ice ? 0.86f : 0.9f);
                    blob.GetComponent<Renderer>().enabled = false;
                    Destroy(blob.GetComponent<Collider>());
                    streamPieces[pieceIndex] = blob.transform;
                    streamRenderers[pieceIndex] = blob.GetComponent<Renderer>();
                }

                var point = new GameObject($"{SensitiveBaristaRules.IngredientName(spec.Ingredient)} Common Pour Point").transform;
                point.position = pointPosition;
                nozzles[spec.Ingredient] = point;
                dispenserViews[spec.Ingredient] = new DispenserView(
                    body.transform,
                    nozzle.transform,
                    stream.transform,
                    stream.GetComponent<Renderer>(),
                    streamPieces,
                    streamRenderers,
                    activeRenderer,
                    body.GetComponent<Renderer>(),
                    body.transform.position,
                    nozzle.transform.position,
                    body.transform.localScale,
                    nozzle.transform.localScale);
            }
        }

        private void BuildUi()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.55f;

            var root = SketchUiFactory.CreateSafeAreaRoot(canvas.transform, "Safe Area");
            roundText = CreateText(root, "Round", "ROUND 1/10", 29, FontStyle.Bold, SketchPalette.Ink, Anchor.TopLeft, new Vector2(28f, -82f), new Vector2(300f, -28f));
            scoreText = CreateText(root, "Score", "RUN SCORE 0", 32, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(312f, -84f), new Vector2(-312f, -28f));
            bestText = CreateText(root, "Best", "BEST 0", 29, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopRight, new Vector2(-300f, -82f), new Vector2(-28f, -28f));
            runMetaText = CreateText(root, "Run Meta", "", 16, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopStretch, new Vector2(360f, -108f), new Vector2(-360f, -84f));
            runMetaText.gameObject.SetActive(false);

            BuildCustomerOrderBubble(root);

            fillText = CreateText(root, "Fill", "0 / 100", 20, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopRight, new Vector2(-230f, -132f), new Vector2(-32f, -98f));
            fillText.gameObject.SetActive(false);
            wasteFeedbackText = CreateText(root, "Waste Feedback", "", 30, FontStyle.Bold, SketchPalette.WarningAmber, Anchor.TopStretch, new Vector2(320f, -340f), new Vector2(-320f, -290f));
            activeIngredientText = CreateText(root, "Active Ingredient", "", 18, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(150f, 176f), new Vector2(-150f, 202f));

            BuildRecipePanel(root);
            BuildIngredientButtons(root);
            BuildCommandButtons(root);
            BuildResultPanel(root);
        }

        private void BuildCustomerOrderBubble(RectTransform root)
        {
            orderPanel = CreatePanel(root, "Order Ticket", Anchor.TopStretch, new Vector2(180f, -244f), new Vector2(-180f, -134f), new Color32(255, 253, 247, 238));
            CreateText(orderPanel, "Order Label", "SENSITIVE ORDER", 13, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopLeft, new Vector2(20f, -28f), new Vector2(190f, -8f));
            orderText = CreateText(orderPanel, "Order Text", "", 28, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, new Vector2(28f, 26f), new Vector2(-28f, -30f));
            moodTagText = CreateText(orderPanel, "Mood Tags", "", 14, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(24f, 6f), new Vector2(-24f, 28f));
            moodTagText.alignment = TextAnchor.MiddleCenter;
            difficultyText = CreateText(orderPanel, "Difficulty", "", 1, FontStyle.Normal, new Color(0f, 0f, 0f, 0f), Anchor.TopRight, Vector2.zero, Vector2.zero);
            difficultyText.gameObject.SetActive(false);
        }

        private void BuildRecipePanel(RectTransform root)
        {
            recipePanel = CreatePanel(root, "Recipe Ticket Back", Anchor.TopStretch, new Vector2(180f, -548f), new Vector2(-180f, -278f), new Color32(255, 253, 247, 248));
            CreateText(recipePanel, "Memo Title", "Recipe Card", 32, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(24f, -60f), new Vector2(-24f, -16f));
            recipeText = CreateText(recipePanel, "Memo Text", SensitiveBaristaRules.RecipeMemo(), 27, FontStyle.Bold, SketchPalette.MutedInk, Anchor.Stretch, new Vector2(34f, 30f), new Vector2(-34f, -76f));
            recipeText.alignment = TextAnchor.UpperLeft;
            recipePanel.gameObject.SetActive(false);
        }

        private void BuildIngredientButtons(RectTransform root)
        {
            var labels = new[]
            {
                new ButtonSpec(BaristaIngredient.Ice, "Ice"),
                new ButtonSpec(BaristaIngredient.Shot, "Shot"),
                new ButtonSpec(BaristaIngredient.Water, "Water"),
                new ButtonSpec(BaristaIngredient.Milk, "Milk"),
                new ButtonSpec(BaristaIngredient.Syrup, "Syrup")
            };

            const float width = 136f;
            const float gap = 9f;
            var totalWidth = labels.Length * width + (labels.Length - 1) * gap;
            var startX = -totalWidth * 0.5f;
            for (var index = 0; index < labels.Length; index += 1)
            {
                var minX = startX + index * (width + gap);
                var button = CreateButton(
                    root,
                    $"{SensitiveBaristaRules.IngredientName(labels[index].Ingredient)} Button",
                    labels[index].Label,
                    Anchor.BottomCenter,
                    new Vector2(minX, 90f),
                    new Vector2(minX + width, 160f));
                var buttonText = button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.fontSize = 29;
                    buttonText.resizeTextMinSize = 21;
                    buttonText.rectTransform.offsetMin = Vector2.zero;
                    buttonText.rectTransform.offsetMax = Vector2.zero;
                }

                var buttonRect = button.GetComponent<RectTransform>();
                AttachHoldEvents(button.gameObject, labels[index].Ingredient);
                ingredientButtons[labels[index].Ingredient] = new IngredientButtonView(
                    button.transform,
                    button.GetComponent<Image>(),
                    button.transform.localScale);
            }
        }

        private void BuildCommandButtons(RectTransform root)
        {
            recipeButton = CreateButton(root, "Recipe Button", "Recipe", Anchor.BottomLeft, new Vector2(32f, 18f), new Vector2(222f, 76f));
            recipeButtonImage = recipeButton.GetComponent<Image>();
            recipeButton.onClick.AddListener(() => recipePanel.gameObject.SetActive(!recipePanel.gameObject.activeSelf));

            var restartButton = CreateButton(root, "Restart Button", "Clear Cup", Anchor.BottomCenter, new Vector2(-130f, 18f), new Vector2(130f, 76f));
            restartButton.onClick.AddListener(ClearCurrentDrink);

            submitButton = CreateButton(root, "Submit Button", "Taste", Anchor.BottomRight, new Vector2(-286f, 18f), new Vector2(-32f, 76f));
            submitButtonImage = submitButton.GetComponent<Image>();
            submitButtonText = submitButton.GetComponentInChildren<Text>();
            submitButton.onClick.AddListener(SubmitRound);
        }

        private void BuildResultPanel(RectTransform root)
        {
            resultPanel = CreatePanel(root, "Result Panel", Anchor.Center, new Vector2(-390f, -330f), new Vector2(390f, 330f), new Color32(255, 253, 247, 248)).gameObject;
            resultTitleText = CreateText(resultPanel.transform, "Result Title", "Result", 36, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(30f, -88f), new Vector2(-30f, -24f));
            resultScoreText = CreateText(resultPanel.transform, "Result Score", "+0", 56, FontStyle.Bold, SketchPalette.Ink, Anchor.TopRight, new Vector2(-214f, -154f), new Vector2(-42f, -90f));
            resultDetailText = CreateText(resultPanel.transform, "Result Detail", "", 22, FontStyle.Bold, SketchPalette.MutedInk, Anchor.Stretch, new Vector2(38f, 116f), new Vector2(-38f, -198f));
            resultDetailText.alignment = TextAnchor.UpperLeft;

            var nextButton = CreateButton(resultPanel.transform, "Next Button", "Next Order", Anchor.BottomStretch, new Vector2(42f, 34f), new Vector2(-42f, 108f));
            nextButtonText = nextButton.GetComponentInChildren<Text>();
            nextButton.onClick.AddListener(ContinueAfterResult);
            resultPanel.SetActive(false);
        }

        private void AttachHoldEvents(GameObject buttonObject, BaristaIngredient ingredient)
        {
            var trigger = buttonObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => BeginPour(ingredient));
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => EndPour());
            trigger.triggers.Add(up);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => EndPour());
            trigger.triggers.Add(exit);

            var cancel = new EventTrigger.Entry { eventID = EventTriggerType.Cancel };
            cancel.callback.AddListener(_ => EndPour());
            trigger.triggers.Add(cancel);
        }

        private void BeginPour(BaristaIngredient ingredient)
        {
            if (roundSubmitted)
            {
                return;
            }

            activeIngredient = ingredient;
            activeIngredientText.text = string.Empty;

            if (IsTapUnitIngredient(ingredient))
            {
                SpawnTapBurst(ingredient);
                isPouring = false;
                feedbackIngredient = ingredient;
                unitFeedbackUntil = Time.time + 0.24f;
                nextPourAt = float.PositiveInfinity;
                return;
            }

            SpawnPourTick(ingredient);
            isPouring = true;
            trailingDripsRemaining = 0;
            nextPourAt = Time.time + PourIntervalFor(ingredient);
        }

        private void EndPour()
        {
            if (!isPouring)
            {
                return;
            }

            isPouring = false;
            if (IsTrailingFlowIngredient(activeIngredient))
            {
                trailingIngredient = activeIngredient;
                trailingDripsRemaining = activeIngredient == BaristaIngredient.Water ? 3 : 2;
                nextTrailingDripAt = Time.time + PourIntervalFor(activeIngredient) * 0.85f;
            }

            activeIngredientText.text = string.Empty;
        }

        private void UpdatePouring()
        {
            if (!roundSubmitted && trailingDripsRemaining > 0 && Time.time >= nextTrailingDripAt)
            {
                SpawnIngredient(trailingIngredient);
                trailingDripsRemaining -= 1;
                nextTrailingDripAt = Time.time + PourIntervalFor(trailingIngredient) * 1.15f;
            }

            if (!isPouring || roundSubmitted || Time.time < nextPourAt)
            {
                return;
            }

            nextPourAt = Time.time + PourIntervalFor(activeIngredient);
            SpawnPourTick(activeIngredient);
        }

        private void SpawnPourTick(BaristaIngredient ingredient)
        {
            var count = ParticleCountPerPourTick(ingredient);
            for (var index = 0; index < count; index += 1)
            {
                SpawnIngredient(ingredient);
            }
        }

        private void SpawnIngredient(BaristaIngredient ingredient)
        {
            if (!nozzles.TryGetValue(ingredient, out var nozzle))
            {
                return;
            }

            if (ingredient != BaristaIngredient.Ice)
            {
                var liquidAmount = LiquidVolumeForPourUnit(ingredient);
                amounts = amounts.Add(ingredient, liquidAmount);
                SpawnLiquidDropVisual(ingredient, nozzle.position);
                return;
            }

            var piece = CreatePhysicsParticleObject(ingredient);
            piece.name = $"{SensitiveBaristaRules.IngredientName(ingredient)} Piece";
            piece.transform.SetParent(pourRoot, true);
            var startPosition = nozzle.position + RandomJitter(
                SpawnJitterFor(ingredient),
                ingredient == BaristaIngredient.Ice ? 0.03f : 0.015f);
            var startRotation = ingredient == BaristaIngredient.Ice
                ? Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-28f, 28f))
                : Quaternion.identity;
            piece.transform.position = new Vector3(startPosition.x, startPosition.y, ParticlePlaneZ);
            piece.transform.rotation = startRotation;
            var particleScale = ScaleForIngredient(ingredient);
            piece.transform.localScale = particleScale;
            var amount = VolumeForParticle(ingredient, particleScale);
            amounts = amounts.Add(ingredient, amount);
            var pieceRenderer = piece.GetComponent<Renderer>();
            pieceRenderer.sharedMaterial = ingredientMaterials[ingredient];
            pieceRenderer.sortingOrder = ParticleSortingOrderFor(ingredient);

            var body = piece.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = InitialVelocity2DFor(ingredient);
                body.angularVelocity = UnityEngine.Random.Range(-90f, 90f) * AngularVelocityFactorFor(ingredient);
            }

            pieces.Add(new PouredPiece(
                piece,
                body,
                ingredient,
                amount,
                Time.time,
                piece.transform.position,
                piece.transform.position,
                startRotation,
                startRotation,
                PieceLifetime));
        }

        private void SpawnLiquidDropVisual(BaristaIngredient ingredient, Vector3 nozzlePosition)
        {
            var drop = new GameObject($"{SensitiveBaristaRules.IngredientName(ingredient)} Drop", typeof(MeshFilter), typeof(MeshRenderer));
            drop.transform.SetParent(pourRoot, true);
            var start = nozzlePosition + RandomJitter(SpawnJitterFor(ingredient) * 0.75f, 0.012f);
            start.z = ParticlePlaneZ - 0.015f;
            var targetY = Mathf.Min(LiquidSurfaceYFor(amounts) + UnityEngine.Random.Range(0.02f, 0.12f), CupTopY - 0.12f);
            var end = new Vector3(
                CupCenterX + UnityEngine.Random.Range(-CupRadius * 0.36f, CupRadius * 0.36f),
                targetY,
                ParticlePlaneZ - 0.015f);
            var scale = VisualDropScaleFor(ingredient);
            drop.transform.position = start;
            drop.transform.localScale = scale;
            var meshFilter = drop.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = VisualMeshForLiquid(ingredient);
            var renderer = drop.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(
                $"{SensitiveBaristaRules.IngredientName(ingredient)} Drop Material",
                IngredientColor(ingredient),
                ParticleAlphaFor(ingredient));
            renderer.sortingOrder = ParticleSortingOrderFor(ingredient) + 8;
            liquidDrops.Add(new LiquidDrop(drop, ingredient, Time.time, start, end, scale, DropDurationFor(ingredient)));
        }

        private void DestroyOldestParticleOfIngredient(BaristaIngredient ingredient)
        {
            for (var index = 0; index < pieces.Count; index += 1)
            {
                if (pieces[index].Ingredient != ingredient)
                {
                    continue;
                }

                if (pieces[index].GameObject != null)
                {
                    Destroy(pieces[index].GameObject);
                }

                pieces.RemoveAt(index);
                return;
            }
        }

        private void SpawnTapBurst(BaristaIngredient ingredient)
        {
            var count = ingredient == BaristaIngredient.Shot ? 10 : 6;
            for (var index = 0; index < count; index += 1)
            {
                SpawnIngredient(ingredient);
            }
        }

        private GameObject CreatePhysicsParticleObject(BaristaIngredient ingredient)
        {
            var piece = ingredient == BaristaIngredient.Ice
                ? CreateIcePieceObject()
                : CreateLiquidParticleObject(ingredient);
            piece.layer = ingredient == BaristaIngredient.Ice ? IcePhysicsLayer : LiquidPhysicsLayer;
            var body = piece.AddComponent<Rigidbody2D>();
            body.gravityScale = GravityScaleFor(ingredient);
            body.linearDamping = DragForIngredient(ingredient);
            body.angularDamping = ingredient == BaristaIngredient.Ice ? 1.4f : 6f;
            body.mass = MassForIngredient(ingredient);
            body.collisionDetectionMode = ingredient == BaristaIngredient.Ice
                ? CollisionDetectionMode2D.Continuous
                : CollisionDetectionMode2D.Discrete;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            return piece;
        }

        private GameObject CreateLiquidParticleObject(BaristaIngredient ingredient)
        {
            var piece = new GameObject($"{SensitiveBaristaRules.IngredientName(ingredient)} Particle", typeof(MeshFilter), typeof(MeshRenderer));
            piece.GetComponent<MeshFilter>().sharedMesh = VisualMeshForLiquid(ingredient);
            var collider = piece.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.sharedMaterial = particlePhysicsMaterials[ingredient];
            return piece;
        }

        private Mesh VisualMeshForLiquid(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Water:
                    return waterParticleVisualMesh;
                case BaristaIngredient.Shot:
                    return shotParticleVisualMesh;
                case BaristaIngredient.Milk:
                    return milkParticleVisualMesh;
                case BaristaIngredient.Syrup:
                    return syrupParticleVisualMesh;
                default:
                    return liquidParticleMesh;
            }
        }

        private int CountVisibleIcePieces()
        {
            var count = 0;
            for (var index = 0; index < pieces.Count; index += 1)
            {
                if (pieces[index].Ingredient == BaristaIngredient.Ice && pieces[index].GameObject != null)
                {
                    count += 1;
                }
            }

            return count;
        }

        private void UpdatePieces()
        {
            for (var index = pieces.Count - 1; index >= 0; index -= 1)
            {
                var piece = pieces[index];
                if (piece.GameObject == null)
                {
                    pieces.RemoveAt(index);
                    continue;
                }

                var age = Time.time - piece.SpawnedAt;
                var transform = piece.GameObject.transform;
                var body = piece.Body;
                if (body != null)
                {
                    body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, MaxVelocityFor(piece.Ingredient));
                    KeepParticleInsideCup(body, piece.Ingredient, transform);
                    if (piece.Ingredient == BaristaIngredient.Ice)
                    {
                        ApplyIceBuoyancyForce(body, transform, amounts);
                    }
                    else
                    {
                        ApplyLiquidSettlingForce(body, piece.Ingredient, transform.position);
                    }
                }

                if (!piece.Wasted && IsClearlyWaste2D(transform.position))
                {
                    piece.Wasted = true;
                    piece.WasteAt = Time.time;
                    if (piece.Counted)
                    {
                        amounts = amounts.Add(piece.Ingredient, -piece.Amount);
                        piece.Counted = false;
                    }

                    wasteAmount += piece.Amount;
                    ShowWasteFeedback(piece.Amount);
                    pieces[index] = piece;
                }

                if (piece.Wasted)
                {
                    FadeParticle(piece.GameObject, Mathf.Clamp01(1f - (Time.time - piece.WasteAt) / WasteLifetime));
                    if (Time.time - piece.WasteAt > WasteLifetime)
                    {
                        Destroy(piece.GameObject);
                        pieces.RemoveAt(index);
                    }

                    continue;
                }

                _ = age;
            }
        }

        private void UpdateLiquidDrops()
        {
            for (var index = liquidDrops.Count - 1; index >= 0; index -= 1)
            {
                var drop = liquidDrops[index];
                if (drop.GameObject == null)
                {
                    liquidDrops.RemoveAt(index);
                    continue;
                }

                var t = Mathf.Clamp01((Time.time - drop.SpawnedAt) / drop.Duration);
                var arc = Mathf.Sin(t * Mathf.PI) * 0.04f;
                drop.GameObject.transform.position = Vector3.Lerp(drop.StartPosition, drop.EndPosition, t) + Vector3.up * arc;
                drop.GameObject.transform.localScale = drop.BaseScale * Mathf.Lerp(1f, 0.82f, t);
                var renderer = drop.GameObject.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    var color = renderer.sharedMaterial.color;
                    color.a = ParticleAlphaFor(drop.Ingredient) * Mathf.Clamp01(1f - t * 0.72f);
                    renderer.sharedMaterial.color = color;
                    if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        renderer.sharedMaterial.SetColor("_BaseColor", color);
                    }
                }

                if (t >= 1f)
                {
                    Destroy(drop.GameObject);
                    liquidDrops.RemoveAt(index);
                }
            }
        }

        private void FadeParticle(GameObject piece, float alpha)
        {
            var renderer = piece.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            var color = renderer.sharedMaterial.color;
            color.a = alpha * ParticleAlphaFor(IngredientForObject(piece));
            renderer.sharedMaterial.color = color;
            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                renderer.sharedMaterial.SetColor("_BaseColor", color);
            }
        }

        private BaristaIngredient IngredientForObject(GameObject pieceObject)
        {
            for (var index = 0; index < pieces.Count; index += 1)
            {
                if (pieces[index].GameObject == pieceObject)
                {
                    return pieces[index].Ingredient;
                }
            }

            return BaristaIngredient.Water;
        }

        private static bool IsLiquidIngredient(BaristaIngredient ingredient)
        {
            return ingredient == BaristaIngredient.Shot ||
                ingredient == BaristaIngredient.Water ||
                ingredient == BaristaIngredient.Milk ||
                ingredient == BaristaIngredient.Syrup;
        }

        private static bool IsInsideCup2D(Vector3 position)
        {
            if (position.y < CupBottomY + 0.08f || position.y > CupTopY + 0.04f)
            {
                return false;
            }

            var halfWidth = CupWidthAtY(position.y) * 0.48f;
            return Mathf.Abs(position.x - CupCenterX) <= halfWidth;
        }

        private GameObject CreateIcePieceObject()
        {
            var piece = new GameObject("Ice Piece", typeof(MeshFilter), typeof(MeshRenderer));
            piece.GetComponent<MeshFilter>().mesh = CreateIceShardMesh("Ice Shard Mesh");
            piece.GetComponent<MeshRenderer>().sharedMaterial = ingredientMaterials[BaristaIngredient.Ice];
            var collider = piece.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(-0.5f, -0.38f),
                new Vector2(0.46f, -0.42f),
                new Vector2(0.52f, 0.36f),
                new Vector2(-0.42f, 0.48f)
            };
            collider.sharedMaterial = particlePhysicsMaterials[BaristaIngredient.Ice];
            return piece;
        }

        private void AddIceSketchDetail(GameObject piece)
        {
            var offsets = new[]
            {
                new Vector3(0f, 0.48f, -0.08f),
                new Vector3(0f, -0.46f, -0.08f),
                new Vector3(-0.46f, 0f, -0.08f),
                new Vector3(0.48f, 0f, -0.08f)
            };
            var scales = new[]
            {
                new Vector3(0.86f, 0.06f, 0.09f),
                new Vector3(0.74f, 0.05f, 0.09f),
                new Vector3(0.055f, 0.72f, 0.09f),
                new Vector3(0.05f, 0.66f, 0.09f)
            };

            for (var index = 0; index < offsets.Length; index += 1)
            {
                var line = CreatePrimitiveObject(PrimitiveType.Cube);
                line.name = $"Ice Sketch Edge {index + 1}";
                line.transform.SetParent(piece.transform, false);
                line.transform.localPosition = offsets[index];
                line.transform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? UnityEngine.Random.Range(-3f, 3f) : UnityEngine.Random.Range(-8f, 8f));
                line.transform.localScale = scales[index];
                line.GetComponent<Renderer>().sharedMaterial = iceOutlineMaterial;
                Destroy(line.GetComponent<Collider>());
            }

            var highlight = CreatePrimitiveObject(PrimitiveType.Cube);
            highlight.name = "Ice Sketch Highlight";
            highlight.transform.SetParent(piece.transform, false);
            highlight.transform.localPosition = new Vector3(-0.12f, 0.08f, -0.1f);
            highlight.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(8f, 18f));
            highlight.transform.localScale = new Vector3(0.08f, 0.62f, 0.08f);
            highlight.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Ice Highlight", new Color32(255, 255, 247, 255), 0.5f);
            Destroy(highlight.GetComponent<Collider>());
        }

        private Vector3 TargetPositionForIngredient(
            BaristaIngredient ingredient,
            IngredientAmounts previousAmounts,
            IngredientAmounts nextAmounts)
        {
            if (ingredient == BaristaIngredient.Ice)
            {
                var visibleIndex = Mathf.Clamp(CountVisibleIcePieces(), 0, MaxVisibleIcePieces - 1);
                var row = visibleIndex < 4 ? 0 : 1;
                var t = (visibleIndex * 0.6180339f) % 1f;
                var x = Mathf.Lerp(-0.43f, 0.43f, t) + UnityEngine.Random.Range(-0.045f, 0.045f);
                var y = CupBottomY + 0.2f + row * 0.12f + UnityEngine.Random.Range(-0.018f, 0.04f);
                return new Vector3(CupCenterX + Mathf.Clamp(x, -0.48f, 0.48f), Mathf.Min(y, CupTopY - 0.22f), -0.2f);
            }

            var surfaceBefore = LiquidSurfaceYFor(previousAmounts);
            var surfaceAfter = LiquidSurfaceYFor(nextAmounts);
            var xOffset = ingredient == BaristaIngredient.Syrup
                ? UnityEngine.Random.Range(-0.12f, 0.12f)
                : UnityEngine.Random.Range(-0.18f, 0.18f);
            return new Vector3(CupCenterX + xOffset, Mathf.Lerp(surfaceBefore, surfaceAfter, 0.72f), -0.2f);
        }

        private static Vector2 InitialVelocity2DFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return new Vector2(UnityEngine.Random.Range(-0.32f, 0.32f), UnityEngine.Random.Range(-0.6f, -0.2f));
                case BaristaIngredient.Shot:
                    return new Vector2(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-1.9f, -1.3f));
                case BaristaIngredient.Water:
                    return new Vector2(UnityEngine.Random.Range(-0.34f, 0.34f), UnityEngine.Random.Range(-1.7f, -1.05f));
                case BaristaIngredient.Milk:
                    return new Vector2(UnityEngine.Random.Range(-0.22f, 0.22f), UnityEngine.Random.Range(-1.36f, -0.88f));
                case BaristaIngredient.Syrup:
                    return new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-1.0f, -0.62f));
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float SpawnJitterFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.16f;
                case BaristaIngredient.Water:
                    return 0.09f;
                case BaristaIngredient.Milk:
                    return 0.065f;
                case BaristaIngredient.Shot:
                    return 0.045f;
                case BaristaIngredient.Syrup:
                    return 0.025f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float GravityScaleFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 1.08f;
                case BaristaIngredient.Shot:
                    return 1.16f;
                case BaristaIngredient.Water:
                    return 0.95f;
                case BaristaIngredient.Milk:
                    return 1.02f;
                case BaristaIngredient.Syrup:
                    return 1.22f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float DragForIngredient(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.28f;
                case BaristaIngredient.Shot:
                    return 0.18f;
                case BaristaIngredient.Water:
                    return 0.035f;
                case BaristaIngredient.Milk:
                    return 0.26f;
                case BaristaIngredient.Syrup:
                    return 0.9f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float AngularVelocityFactorFor(BaristaIngredient ingredient)
        {
            return ingredient == BaristaIngredient.Ice ? 1f : 0.08f;
        }

        private static float MaxVelocityFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 3.2f;
                case BaristaIngredient.Water:
                    return 3.9f;
                case BaristaIngredient.Shot:
                    return 3.7f;
                case BaristaIngredient.Milk:
                    return 3.35f;
                case BaristaIngredient.Syrup:
                    return 2.5f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float ParticleAlphaFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.74f;
                case BaristaIngredient.Water:
                    return 0.72f;
                case BaristaIngredient.Milk:
                    return 0.88f;
                case BaristaIngredient.Syrup:
                    return 0.92f;
                case BaristaIngredient.Shot:
                    return 0.9f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static int ParticleSortingOrderFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Syrup:
                    return 1;
                case BaristaIngredient.Shot:
                    return 2;
                case BaristaIngredient.Water:
                    return 3;
                case BaristaIngredient.Milk:
                    return 4;
                case BaristaIngredient.Ice:
                    return 20;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static void ApplyLiquidSettlingForce(Rigidbody2D body, BaristaIngredient ingredient, Vector3 position)
        {
            if (!IsLiquidIngredient(ingredient) || !IsInsideCup2D(position) || body.linearVelocity.sqrMagnitude > 0.72f)
            {
                return;
            }

            var desiredY = LayerPreferenceYFor(ingredient);
            var offset = Mathf.Clamp(desiredY - position.y, -0.34f, 0.34f);
            var force = offset * SettlingStrengthFor(ingredient);
            body.AddForce(new Vector2(0f, force), ForceMode2D.Force);
        }

        private static void ApplyIceBuoyancyForce(Rigidbody2D body, Transform transform, IngredientAmounts amounts)
        {
            var position = transform.position;
            if (LiquidIngredientTotal(amounts) < 2f || !IsInsideCup2D(position))
            {
                body.gravityScale = GravityScaleFor(BaristaIngredient.Ice);
                return;
            }

            var surfaceY = LiquidSurfaceYFor(amounts);
            if (position.y > surfaceY + 0.22f)
            {
                body.gravityScale = GravityScaleFor(BaristaIngredient.Ice);
                return;
            }

            body.gravityScale = position.y < surfaceY + 0.04f ? 0.14f : 0.55f;

            var targetY = Mathf.Clamp(surfaceY - 0.035f, CupBottomY + 0.22f, CupTopY - 0.12f);
            var offset = Mathf.Clamp(targetY - position.y, -0.16f, 0.62f);
            var targetVelocityY = Mathf.Clamp(offset * 7.2f, -0.45f, 2.35f);
            var correction = (targetVelocityY - body.linearVelocity.y) * 3.8f;
            body.AddForce(new Vector2(0f, correction * body.mass), ForceMode2D.Force);

            if (offset > 0.34f && body.linearVelocity.y < 0.45f)
            {
                var correctedY = Mathf.Lerp(position.y, targetY, Time.deltaTime * 2.6f);
                transform.position = new Vector3(position.x, correctedY, ParticlePlaneZ);
                body.linearVelocity = new Vector2(body.linearVelocity.x * 0.92f, Mathf.Max(body.linearVelocity.y, 0.35f));
            }

            body.angularVelocity *= 0.97f;
        }

        private static float LayerPreferenceYFor(BaristaIngredient ingredient)
        {
            var usableBottom = CupBottomY + 0.18f;
            var usableTop = CupTopY - 0.18f;
            switch (ingredient)
            {
                case BaristaIngredient.Syrup:
                    return Mathf.Lerp(usableBottom, usableTop, 0.08f);
                case BaristaIngredient.Shot:
                    return Mathf.Lerp(usableBottom, usableTop, 0.22f);
                case BaristaIngredient.Milk:
                    return Mathf.Lerp(usableBottom, usableTop, 0.54f);
                case BaristaIngredient.Water:
                    return Mathf.Lerp(usableBottom, usableTop, 0.66f);
                default:
                    return Mathf.Lerp(usableBottom, usableTop, 0.45f);
            }
        }

        private static float SettlingStrengthFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Syrup:
                    return 0.18f;
                case BaristaIngredient.Shot:
                    return 0.14f;
                case BaristaIngredient.Milk:
                    return 0.09f;
                case BaristaIngredient.Water:
                    return 0.08f;
                default:
                    return 0f;
            }
        }

        private static void KeepParticleInsideCup(Rigidbody2D body, BaristaIngredient ingredient, Transform transform)
        {
            var floorY = CupBottomY + (ingredient == BaristaIngredient.Ice ? 0.15f : 0.11f);
            var position = transform.position;
            var nearCup = Mathf.Abs(position.x - CupCenterX) <= CupRadius * 1.18f &&
                position.y <= CupTopY + 0.08f;
            if (!nearCup)
            {
                return;
            }

            var clampedY = Mathf.Clamp(position.y, floorY, CupTopY - 0.04f);
            var margin = ingredient == BaristaIngredient.Ice ? 0.055f : 0.025f;
            var halfWidth = Mathf.Max(0.08f, CupWidthAtY(clampedY) * 0.5f - margin);
            var clampedX = Mathf.Clamp(position.x, CupCenterX - halfWidth, CupCenterX + halfWidth);
            if (Mathf.Approximately(clampedX, position.x) && Mathf.Approximately(clampedY, position.y))
            {
                return;
            }

            transform.position = new Vector3(clampedX, clampedY, ParticlePlaneZ);
            var velocity = body.linearVelocity;
            if (!Mathf.Approximately(clampedX, position.x))
            {
                velocity.x *= -0.16f;
            }

            if (!Mathf.Approximately(clampedY, position.y))
            {
                velocity.y = Mathf.Abs(velocity.y) * (ingredient == BaristaIngredient.Ice ? 0.16f : 0.08f);
            }

            body.linearVelocity = Vector2.ClampMagnitude(velocity, MaxVelocityFor(ingredient));
            body.angularVelocity *= ingredient == BaristaIngredient.Ice ? 0.38f : 0.12f;
        }

        private static bool IsClearlyWaste2D(Vector3 position)
        {
            return position.y < CupBottomY - 0.34f ||
                position.y > CupTopY + 0.62f ||
                Mathf.Abs(position.x - CupCenterX) > CupRadius * 1.65f;
        }

        private static float LiquidSurfaceYFor(IngredientAmounts amounts)
        {
            var liquidTotal = LiquidIngredientTotal(amounts);
            if (liquidTotal <= 0.05f)
            {
                return CupBottomY + 0.08f;
            }

            var liquidHeight = Mathf.Clamp01(liquidTotal / SensitiveBaristaRules.CupCapacity) * (CupTopY - CupBottomY - 0.18f);
            return CupBottomY + 0.08f + liquidHeight;
        }

        private static float LiquidIngredientTotal(IngredientAmounts amounts)
        {
            return amounts.Shot + amounts.Water + amounts.Milk + amounts.Syrup;
        }

        private static float IceDisplacementVolume(float iceAmount)
        {
            return Mathf.Max(0f, iceAmount) * 0.68f;
        }

        private static float FallDurationFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.2f;
                case BaristaIngredient.Shot:
                    return 0.18f;
                case BaristaIngredient.Water:
                    return 0.16f;
                case BaristaIngredient.Milk:
                    return 0.22f;
                case BaristaIngredient.Syrup:
                    return 0.3f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private bool IsInsideCup(Vector3 position)
        {
            var flat = new Vector2(position.x - cupCenter.position.x, position.z - cupCenter.position.z);
            return flat.magnitude <= CupRadius * 0.92f &&
                position.y >= CupBottomY &&
                position.y <= CupTopY;
        }

        private bool IsClearlyWaste(Vector3 position)
        {
            var flat = new Vector2(position.x - cupCenter.position.x, position.z - cupCenter.position.z);
            return flat.magnitude > CupRadius * 1.55f || position.y < 0.04f || position.y > CupTopY + 1.2f;
        }

        private void DestroyOldestCountedPiece()
        {
            for (var index = 0; index < pieces.Count; index += 1)
            {
                if (pieces[index].GameObject == null)
                {
                    pieces.RemoveAt(index);
                    return;
                }

                if (!pieces[index].Wasted && IsInsideCup2D(pieces[index].GameObject.transform.position))
                {
                    continue;
                }

                Destroy(pieces[index].GameObject);
                pieces.RemoveAt(index);
                return;
            }
        }

        private void ClearCup()
        {
            for (var index = pieces.Count - 1; index >= 0; index -= 1)
            {
                if (pieces[index].GameObject != null)
                {
                    Destroy(pieces[index].GameObject);
                }
            }

            pieces.Clear();

            for (var index = liquidDrops.Count - 1; index >= 0; index -= 1)
            {
                if (liquidDrops[index].GameObject != null)
                {
                    Destroy(liquidDrops[index].GameObject);
                }
            }

            liquidDrops.Clear();
        }

        private void UpdateHud()
        {
            if (roundText == null)
            {
                return;
            }

            roundText.text = $"ROUND {roundNumber}/{SensitiveBaristaRules.RoundCount}";
            scoreText.text = $"RUN SCORE {runScore}";
            bestText.text = $"BEST {bestScore}";
            if (runMetaText != null)
            {
                var average = roundNumber <= 1 ? 0 : Mathf.RoundToInt(runScore / (float)(roundNumber - 1));
                var left = Mathf.Max(0, SensitiveBaristaRules.RoundCount - roundNumber);
                runMetaText.text = string.Empty;
            }

            difficultyText.text = string.Empty;
            var fillLabel = $"{Mathf.RoundToInt(amounts.Total)} / {Mathf.RoundToInt(SensitiveBaristaRules.CupCapacity)}";
            fillText.text = fillLabel;
            fillText.color = amounts.Total >= SensitiveBaristaRules.CupCapacity * 0.8f ? SketchPalette.WarningAmber : SketchPalette.MutedInk;
            if (cupStatusText != null)
            {
                cupStatusText.text = fillLabel;
                cupStatusText.color = fillText.color;
            }
            var canSubmit = !roundSubmitted && amounts.Total >= SensitiveBaristaRules.MinimumPlayableAmount;
            if (submitButton != null)
            {
                submitButton.interactable = canSubmit;
            }

            if (submitButtonImage != null)
            {
                submitButtonImage.color = canSubmit
                    ? Color.Lerp(SketchPalette.TilePaper, SketchPalette.WarningAmber, 0.22f)
                    : (Color)new Color32(233, 229, 218, 210);
            }

            if (submitButtonText != null)
            {
                submitButtonText.text = canSubmit ? "Taste" : "Add";
                submitButtonText.color = canSubmit ? SketchPalette.Ink : SketchPalette.MutedInk;
            }

            if (recipeButtonImage != null)
            {
                recipeButtonImage.color = recipePanel != null && recipePanel.gameObject.activeSelf
                    ? Color.Lerp(SketchPalette.TilePaper, new Color32(133, 203, 255, 255), 0.22f)
                    : SketchPalette.TilePaper;
            }
        }

        private void UpdateCupFill()
        {
            foreach (var ingredient in SensitiveBaristaRules.Ingredients)
            {
                if (!fillLayers.TryGetValue(ingredient, out var layer))
                {
                    continue;
                }

                var renderer = layer.GetComponent<Renderer>();
                renderer.enabled = false;
                if (fillEdges.TryGetValue(ingredient, out var edge))
                {
                    var edgeRenderer = edge.GetComponent<Renderer>();
                    edgeRenderer.enabled = false;
                }
            }

            var bottomY = CupBottomY + 0.13f;
            var maxTopY = CupTopY - 0.08f;
            var usableHeight = maxTopY - bottomY;

            foreach (var ingredient in LiquidLayerOrder)
            {
                var amount = amounts[ingredient];
                if (amount <= 0.05f || !fillLayers.TryGetValue(ingredient, out var layer))
                {
                    continue;
                }

                var topY = Mathf.Min(maxTopY, bottomY + amount / SensitiveBaristaRules.CupCapacity * usableHeight);
                SetLiquidLayerMesh(layer, bottomY, topY);
                var renderer = layer.GetComponent<Renderer>();
                renderer.enabled = true;
                if (fillEdges.TryGetValue(ingredient, out var edge))
                {
                    edge.position = new Vector3(0f, topY, -0.176f);
                    edge.localScale = new Vector3(CupWidthAtY(topY) * 0.88f, WorldDoodleLine * 0.55f, 0.035f);
                    var edgeRenderer = edge.GetComponent<Renderer>();
                    edgeRenderer.enabled = true;
                }

                bottomY = topY;
                if (bottomY >= maxTopY)
                {
                    break;
                }
            }
        }

        private void UpdateFeedbackVisuals()
        {
            foreach (var ingredient in SensitiveBaristaRules.Ingredients)
            {
                var active =
                    !roundSubmitted &&
                    ((isPouring && activeIngredient == ingredient) ||
                    (Time.time < unitFeedbackUntil && feedbackIngredient == ingredient));
                if (ingredientButtons.TryGetValue(ingredient, out var buttonView))
                {
                    buttonView.Transform.localScale = Vector3.Lerp(
                        buttonView.Transform.localScale,
                        buttonView.BaseScale * (active ? 1.08f : 1f),
                        Time.deltaTime * 18f);
                    buttonView.Image.color = active
                        ? Color.Lerp(SketchPalette.TilePaper, IngredientColor(ingredient), 0.42f)
                        : SketchPalette.TilePaper;
                }

                if (dispenserViews.TryGetValue(ingredient, out var dispenserView))
                {
                    var pulse = active ? 1f + Mathf.Sin(Time.time * 26f) * 0.035f : 1f;
                    if (active)
                    {
                        dispenserView.Body.localScale = Vector3.Lerp(
                            dispenserView.Body.localScale,
                            dispenserView.BaseBodyScale * 1.08f,
                            Time.deltaTime * 14f);
                        dispenserView.Nozzle.localScale = dispenserView.BaseNozzleScale * pulse;
                        dispenserView.Nozzle.position = dispenserView.BaseNozzlePosition +
                            Vector3.down * (0.035f + Mathf.Sin(Time.time * 32f) * 0.018f);
                    }
                    else if (!isPouring)
                    {
                        dispenserView.Body.localScale = Vector3.Lerp(
                            dispenserView.Body.localScale,
                            dispenserView.BaseBodyScale,
                            Time.deltaTime * 14f);
                        dispenserView.Nozzle.localScale = dispenserView.BaseNozzleScale;
                        dispenserView.Nozzle.position = Vector3.Lerp(
                            dispenserView.Nozzle.position,
                            dispenserView.BaseNozzlePosition,
                            Time.deltaTime * 14f);
                    }

                    if (dispenserView.StreamRenderer != null)
                    {
                        dispenserView.StreamRenderer.enabled = false;
                        dispenserView.Stream.localScale = new Vector3(
                            0.07f + Mathf.Sin(Time.time * 34f) * 0.012f,
                            0.62f + Mathf.Sin(Time.time * 22f) * 0.04f,
                            0.045f);
                    }

                    if (dispenserView.ActiveRenderer != null)
                    {
                        dispenserView.ActiveRenderer.enabled = active;
                    }

                    for (var index = 0; index < dispenserView.StreamPieces.Length; index += 1)
                    {
                        var renderer = dispenserView.StreamPieceRenderers[index];
                        var piece = dispenserView.StreamPieces[index];
                        if (renderer == null || piece == null)
                        {
                            continue;
                        }

                        renderer.enabled = false;
                        var fallPhase = Mathf.Repeat(Time.time * StreamVisualSpeedFor(ingredient) + index * 0.28f, 1f);
                        var baseX = dispenserView.BaseNozzlePosition.x;
                        piece.position = new Vector3(
                            baseX + Mathf.Sin(Time.time * 9f + index) * StreamVisualWobbleFor(ingredient),
                            dispenserView.BaseNozzlePosition.y - 0.28f - fallPhase * 0.86f,
                            -0.18f);
                        var size = ingredient == BaristaIngredient.Ice
                            ? 0.11f + Mathf.Sin(Time.time * 8f + index) * 0.014f
                            : 0.075f + Mathf.Sin(Time.time * 13f + index) * 0.017f;
                        piece.localScale = Vector3.one * size;
                    }

                    dispenserView.Body.position = Vector3.Lerp(dispenserView.Body.position, dispenserView.BaseBodyPosition, Time.deltaTime * 14f);
                }
            }

            if (orderPanel != null)
            {
                var t = Mathf.Clamp01((Time.time - roundStartedAt) / 0.34f);
                var scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.035f;
                orderPanel.localScale = Vector3.one * scale;
            }

            if (!isPouring && Time.time >= unitFeedbackUntil && trailingDripsRemaining <= 0 && activeIngredientText != null)
            {
                activeIngredientText.text = string.Empty;
            }

            if (wasteFeedbackText != null)
            {
                var remaining = wasteFeedbackUntil - Time.time;
                var color = SketchPalette.WarningAmber;
                color.a = Mathf.Clamp01(remaining / 0.55f);
                wasteFeedbackText.color = color;
                if (remaining <= 0f)
                {
                    wasteFeedbackText.text = string.Empty;
                }
            }

            if (capacityLineRenderer != null && capacityLineMaterial != null)
            {
                var overWarning = amounts.Total >= SensitiveBaristaRules.CupCapacity * 0.8f;
                var warningColor = SketchPalette.WarningAmber;
                warningColor.a = overWarning ? 0.62f + Mathf.Sin(Time.time * 8f) * 0.22f : 0.72f;
                capacityLineMaterial.color = warningColor;
                capacityLineRenderer.sharedMaterial = capacityLineMaterial;
            }
        }

        private void ShowWasteFeedback(float amount)
        {
            if (wasteFeedbackText == null)
            {
                return;
            }

            wasteFeedbackText.text = $"WASTE +{Mathf.CeilToInt(amount)}";
            wasteFeedbackUntil = Time.time + 0.7f;
        }

        private static string MoodTagsFor(BaristaOrder order)
        {
            var tags = new List<string>();
            AddMoodTag(tags, IngredientMood(order.PrimaryIngredient));
            AddMoodTag(tags, IngredientMood(order.SecondaryIngredient));
            if (order.Target.Ice / Math.Max(1f, order.Target.Total) >= 0.22f)
            {
                AddMoodTag(tags, "COLD");
            }

            if (order.Target.Milk / Math.Max(1f, order.Target.Total) >= 0.42f)
            {
                AddMoodTag(tags, "SOFT");
            }

            if (order.Target.Water / Math.Max(1f, order.Target.Total) >= 0.42f)
            {
                AddMoodTag(tags, "LIGHT");
            }

            if (order.Target.Shot / Math.Max(1f, order.Target.Total) >= 0.32f)
            {
                AddMoodTag(tags, "RICH");
            }

            if (order.Target.Syrup / Math.Max(1f, order.Target.Total) >= 0.14f)
            {
                AddMoodTag(tags, "SWEET");
            }

            AddMoodTag(tags, order.IdealTotal < 70f ? "SHORT" : order.IdealTotal > 84f ? "FULL" : "BALANCED");
            return string.Join("  /  ", tags.GetRange(0, Math.Min(tags.Count, 4)));
        }

        private static void AddMoodTag(List<string> tags, string tag)
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        private static string IngredientMood(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return "COLD";
                case BaristaIngredient.Shot:
                    return "RICH";
                case BaristaIngredient.Water:
                    return "LIGHT";
                case BaristaIngredient.Milk:
                    return "SOFT";
                case BaristaIngredient.Syrup:
                    return "SWEET";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static string VolumeSummary(float total)
        {
            var percent = Mathf.RoundToInt(total / SensitiveBaristaRules.CupCapacity * 100f);
            if (percent >= 140)
            {
                return $"Fill {percent}% (way too full)";
            }

            if (percent > 108)
            {
                return $"Fill {percent}% (too full)";
            }

            if (percent < 65)
            {
                return $"Fill {percent}% (too small)";
            }

            return $"Fill {percent}%";
        }

        private static string JudgeNudge(BaristaScore score, BaristaOrder order, IngredientAmounts actual)
        {
            if (score.RoundScore >= 92)
            {
                return "Read the mood cleanly.";
            }

            var fillRatio = SensitiveBaristaRules.CupCapacity <= 0f
                ? 1f
                : actual.Total / SensitiveBaristaRules.CupCapacity;
            if (fillRatio >= 1.35f)
            {
                return "Good idea, but pour a much smaller cup.";
            }

            if (fillRatio > 1.08f)
            {
                return "Too full. Keep the same idea, less volume.";
            }

            if (actual.Total < order.IdealTotal * 0.68f)
            {
                return "Too small. Build the cup a bit higher.";
            }

            if (score.WastePenalty >= 8f)
            {
                return "Clean pour matters as much as taste.";
            }

            if (score.MissingPenalty >= 8f)
            {
                return "One key note barely showed up.";
            }

            if (score.SyrupPenalty >= 7f)
            {
                return "Sweetness started taking over.";
            }

            var missingHint = MissingIngredientHint(order, actual);
            if (!string.IsNullOrEmpty(missingHint))
            {
                return missingHint;
            }

            if (score.TotalScore < 10f)
            {
                return "The cup size missed the mood.";
            }

            if (score.RatioScore < 42f)
            {
                return "The idea was there, but the balance drifted.";
            }

            return "Close. One smaller adjustment would land it.";
        }

        private static string MissingIngredientHint(BaristaOrder order, IngredientAmounts actual)
        {
            foreach (var ingredient in SensitiveBaristaRules.Ingredients)
            {
                var target = order.Target[ingredient];
                if (target <= 0f)
                {
                    continue;
                }

                if (actual[ingredient] < target * 0.32f)
                {
                    return $"Needs more {SensitiveBaristaRules.IngredientName(ingredient).ToLowerInvariant()}.";
                }
            }

            if (order.Target.Syrup <= 0f && actual.Syrup > actual.Total * 0.05f)
            {
                return "This order wanted syrup out.";
            }

            return string.Empty;
        }

        private void UpdateCameraForScreen()
        {
            if (mainCamera == null)
            {
                return;
            }

            var aspect = Screen.width <= 0 || Screen.height <= 0 ? 9f / 16f : (float)Screen.width / Screen.height;
            var wideOffset = Mathf.Clamp01((aspect - 0.65f) / 0.55f);
            mainCamera.transform.position = Vector3.Lerp(CameraPortraitPosition, CameraWidePosition, wideOffset);
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = Mathf.Lerp(3.08f, 2.82f, wideOffset);
        }

        private Vector3 ScaleForIngredient(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return new Vector3(
                        UnityEngine.Random.Range(0.13f, 0.18f),
                        UnityEngine.Random.Range(0.11f, 0.16f),
                        UnityEngine.Random.Range(0.035f, 0.055f));
                case BaristaIngredient.Syrup:
                    return Vector3.one * UnityEngine.Random.Range(0.05f, 0.064f);
                case BaristaIngredient.Shot:
                    return Vector3.one * UnityEngine.Random.Range(0.058f, 0.074f);
                case BaristaIngredient.Water:
                    return Vector3.one * UnityEngine.Random.Range(0.045f, 0.058f);
                case BaristaIngredient.Milk:
                    return Vector3.one * UnityEngine.Random.Range(0.052f, 0.067f);
                default:
                    return Vector3.one * 0.05f;
            }
        }

        private static int ParticleCountPerPourTick(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 1;
                case BaristaIngredient.Water:
                case BaristaIngredient.Milk:
                    return 1;
                case BaristaIngredient.Syrup:
                case BaristaIngredient.Shot:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float VolumeForParticle(BaristaIngredient ingredient, Vector3 scale)
        {
            var frontArea = Mathf.Max(0.0001f, Mathf.Abs(scale.x * scale.y));
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return frontArea * 146f;
                case BaristaIngredient.Syrup:
                    return frontArea * 245f;
                case BaristaIngredient.Shot:
                    return frontArea * 375f;
                case BaristaIngredient.Water:
                    return frontArea * 260f;
                case BaristaIngredient.Milk:
                    return frontArea * 195f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float LiquidVolumeForPourUnit(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Syrup:
                    return 0.9f;
                case BaristaIngredient.Shot:
                    return 1.65f;
                case BaristaIngredient.Water:
                    return 0.72f;
                case BaristaIngredient.Milk:
                    return 0.72f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static Vector3 VisualDropScaleFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Syrup:
                    return Vector3.one * UnityEngine.Random.Range(0.058f, 0.074f);
                case BaristaIngredient.Shot:
                    return Vector3.one * UnityEngine.Random.Range(0.062f, 0.08f);
                case BaristaIngredient.Water:
                    return Vector3.one * UnityEngine.Random.Range(0.05f, 0.064f);
                case BaristaIngredient.Milk:
                    return Vector3.one * UnityEngine.Random.Range(0.058f, 0.074f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float DropDurationFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Syrup:
                    return 0.34f;
                case BaristaIngredient.Milk:
                    return 0.28f;
                case BaristaIngredient.Shot:
                    return 0.22f;
                case BaristaIngredient.Water:
                    return 0.2f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private float PourIntervalFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.2f;
                case BaristaIngredient.Shot:
                    return 0.48f;
                case BaristaIngredient.Water:
                    return 0.032f;
                case BaristaIngredient.Milk:
                    return 0.047f;
                case BaristaIngredient.Syrup:
                    return 0.38f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private float MassForIngredient(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.14f;
                case BaristaIngredient.Syrup:
                    return 0.105f;
                case BaristaIngredient.Shot:
                    return 0.088f;
                case BaristaIngredient.Milk:
                    return 0.068f;
                case BaristaIngredient.Water:
                    return 0.045f;
                default:
                    return 0.055f;
            }
        }

        private float LinearDampingForIngredient(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.05f;
                case BaristaIngredient.Syrup:
                    return 0.48f;
                case BaristaIngredient.Milk:
                    return 0.16f;
                case BaristaIngredient.Water:
                    return 0.04f;
                default:
                    return 0.08f;
            }
        }

        private static float StreamVisualSpeedFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 3.2f;
                case BaristaIngredient.Shot:
                    return 6.4f;
                case BaristaIngredient.Water:
                    return 8.2f;
                case BaristaIngredient.Milk:
                    return 5.4f;
                case BaristaIngredient.Syrup:
                    return 2.5f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private static float StreamVisualWobbleFor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return 0.045f;
                case BaristaIngredient.Syrup:
                    return 0.015f;
                case BaristaIngredient.Milk:
                    return 0.026f;
                default:
                    return 0.02f;
            }
        }

        private static bool IsTapUnitIngredient(BaristaIngredient ingredient)
        {
            return ingredient == BaristaIngredient.Shot ||
                ingredient == BaristaIngredient.Syrup;
        }

        private static bool IsTrailingFlowIngredient(BaristaIngredient ingredient)
        {
            return ingredient == BaristaIngredient.Water || ingredient == BaristaIngredient.Milk;
        }

        private Color IngredientColor(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return IceColor;
                case BaristaIngredient.Shot:
                    return ShotColor;
                case BaristaIngredient.Water:
                    return WaterColor;
                case BaristaIngredient.Milk:
                    return MilkColor;
                case BaristaIngredient.Syrup:
                    return SyrupColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        private Vector3 RandomJitter(float horizontal, float vertical)
        {
            return new Vector3(
                UnityEngine.Random.Range(-horizontal, horizontal),
                UnityEngine.Random.Range(-vertical, vertical),
                UnityEngine.Random.Range(-horizontal, horizontal));
        }

        private GameObject CreateSketchCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var gameObject = CreatePrimitiveObject(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(gameObject.GetComponent<Collider>());
            return gameObject;
        }

        private GameObject CreateFlatOval(string name, Material material, int segments)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gameObject.GetComponent<MeshFilter>().mesh = CreateDiscMesh(name + " Mesh", segments);
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        private GameObject CreateFrontOvalRing(
            string name,
            float y,
            float outerX,
            float outerY,
            float innerX,
            float innerY,
            Material material)
        {
            var ring = CreateOvalRing(name, material, outerX, outerY, innerX, innerY);
            ring.transform.position = new Vector3(0f, y, -0.16f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return ring;
        }

        private GameObject CreateOvalRing(string name, Material material, float outerX, float outerZ, float innerX, float innerZ)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gameObject.GetComponent<MeshFilter>().mesh = CreateOvalRingMesh(name + " Mesh", outerX, outerZ, innerX, innerZ, 48);
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        private GameObject CreateLine2D(string name, Vector2 start, Vector2 end, float thickness, Material material, float z)
        {
            var center = (start + end) * 0.5f;
            var delta = end - start;
            var line = CreatePrimitiveObject(PrimitiveType.Cube);
            line.name = name;
            line.transform.position = new Vector3(center.x, center.y, z);
            line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            line.transform.localScale = new Vector3(delta.magnitude, thickness, 0.04f);
            line.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(line.GetComponent<Collider>());
            return line;
        }

        private TextMesh CreateWorldLabel(string name, string value, Vector3 position, float characterSize, Color color)
        {
            var label = new GameObject(name, typeof(TextMesh), typeof(MeshRenderer)).GetComponent<TextMesh>();
            label.text = value;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 36;
            label.characterSize = characterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            label.transform.position = position;
            label.transform.rotation = Quaternion.identity;
            var renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 2;
            }

            return label;
        }

        private static Mesh CreateFrontCupMesh(string name)
        {
            var vertices = new[]
            {
                new Vector3(CupCenterX - CupRadius * 0.64f, CupBottomY + 0.12f, -0.14f),
                new Vector3(CupCenterX + CupRadius * 0.64f, CupBottomY + 0.12f, -0.14f),
                new Vector3(CupCenterX + CupRadius * 1.03f, CupTopY - 0.04f, -0.14f),
                new Vector3(CupCenterX - CupRadius * 1.03f, CupTopY - 0.04f, -0.14f)
            };
            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateIceShardMesh(string name)
        {
            var left = UnityEngine.Random.Range(-0.5f, -0.42f);
            var right = UnityEngine.Random.Range(0.42f, 0.52f);
            var topLeft = UnityEngine.Random.Range(0.34f, 0.5f);
            var topRight = UnityEngine.Random.Range(0.32f, 0.48f);
            var bottomLeft = UnityEngine.Random.Range(-0.48f, -0.34f);
            var bottomRight = UnityEngine.Random.Range(-0.48f, -0.32f);
            var vertices = new[]
            {
                new Vector3(left, bottomLeft, 0f),
                new Vector3(right, bottomRight, 0f),
                new Vector3(UnityEngine.Random.Range(0.36f, 0.54f), topRight, 0f),
                new Vector3(UnityEngine.Random.Range(-0.54f, -0.36f), topLeft, 0f)
            };
            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCircleMesh2D(string name, int segments, float radius = 0.5f)
        {
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < segments; index += 1)
            {
                var angle = (Mathf.PI * 2f * index) / segments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            for (var index = 0; index < segments; index += 1)
            {
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 1;
                triangles[triangleIndex + 2] = index == segments - 1 ? 1 : index + 2;
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateLiquidLayerMesh(string name, float bottomY, float topY)
        {
            var mesh = new Mesh { name = name };
            ApplyLiquidLayerMesh(mesh, bottomY, topY);
            return mesh;
        }

        private static void SetLiquidLayerMesh(Transform layer, float bottomY, float topY)
        {
            var meshFilter = layer.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }

            ApplyLiquidLayerMesh(meshFilter.mesh, bottomY, Mathf.Max(bottomY + 0.001f, topY));
        }

        private static void ApplyLiquidLayerMesh(Mesh mesh, float bottomY, float topY)
        {
            const int columns = 10;
            var bottomWidth = CupWidthAtY(bottomY);
            var topWidth = CupWidthAtY(topY);
            var vertices = new Vector3[(columns + 1) * 2];
            var triangles = new int[columns * 6];
            var wave = Mathf.Min(0.018f, (topY - bottomY) * 0.22f);

            for (var index = 0; index <= columns; index += 1)
            {
                var t = index / (float)columns;
                var bottomX = Mathf.Lerp(CupCenterX - bottomWidth * 0.5f, CupCenterX + bottomWidth * 0.5f, t);
                var topX = Mathf.Lerp(CupCenterX - topWidth * 0.5f, CupCenterX + topWidth * 0.5f, t);
                var surfaceWave = Mathf.Sin(Time.time * 2.8f + index * 0.9f) * wave;
                vertices[index * 2] = new Vector3(bottomX, bottomY, -0.13f);
                vertices[index * 2 + 1] = new Vector3(topX, topY + surfaceWave, -0.13f);
            }

            for (var index = 0; index < columns; index += 1)
            {
                var triangleIndex = index * 6;
                var bottom = index * 2;
                var top = bottom + 1;
                var nextBottom = bottom + 2;
                var nextTop = bottom + 3;
                triangles[triangleIndex] = bottom;
                triangles[triangleIndex + 1] = top;
                triangles[triangleIndex + 2] = nextBottom;
                triangles[triangleIndex + 3] = top;
                triangles[triangleIndex + 4] = nextTop;
                triangles[triangleIndex + 5] = nextBottom;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static float CupWidthAtY(float y)
        {
            var height01 = Mathf.InverseLerp(CupBottomY, CupTopY, y);
            return Mathf.Lerp(CupRadius * 1.28f, CupRadius * 1.88f, height01);
        }

        private static Mesh CreateTaperedCupMesh(string name, int segments)
        {
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            var height = CupTopY - CupBottomY;
            const float bottomRadius = CupRadius * 0.78f;
            const float topRadius = CupRadius * 1.08f;

            for (var index = 0; index < segments; index += 1)
            {
                var angle = (Mathf.PI * 2f * index) / segments;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                vertices[index * 2] = new Vector3(cos * bottomRadius, CupBottomY, 0.1f + sin * bottomRadius);
                vertices[index * 2 + 1] = new Vector3(cos * topRadius, CupBottomY + height, 0.1f + sin * topRadius);
            }

            for (var index = 0; index < segments; index += 1)
            {
                var next = (index + 1) % segments;
                var triangleIndex = index * 6;
                var bottom = index * 2;
                var top = bottom + 1;
                var nextBottom = next * 2;
                var nextTop = nextBottom + 1;
                triangles[triangleIndex] = bottom;
                triangles[triangleIndex + 1] = top;
                triangles[triangleIndex + 2] = nextBottom;
                triangles[triangleIndex + 3] = top;
                triangles[triangleIndex + 4] = nextTop;
                triangles[triangleIndex + 5] = nextBottom;
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateDiscMesh(string name, int segments)
        {
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < segments; index += 1)
            {
                var angle = (Mathf.PI * 2f * index) / segments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            }

            for (var index = 0; index < segments; index += 1)
            {
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index == segments - 1 ? 1 : index + 2;
                triangles[triangleIndex + 2] = index + 1;
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOvalRingMesh(string name, float outerX, float outerZ, float innerX, float innerZ, int segments)
        {
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];

            for (var index = 0; index < segments; index += 1)
            {
                var angle = (Mathf.PI * 2f * index) / segments;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                vertices[index * 2] = new Vector3(cos * outerX, 0f, sin * outerZ);
                vertices[index * 2 + 1] = new Vector3(cos * innerX, 0f, sin * innerZ);
            }

            for (var index = 0; index < segments; index += 1)
            {
                var next = (index + 1) % segments;
                var triangleIndex = index * 6;
                var outer = index * 2;
                var inner = outer + 1;
                var nextOuter = next * 2;
                var nextInner = nextOuter + 1;
                triangles[triangleIndex] = outer;
                triangles[triangleIndex + 1] = inner;
                triangles[triangleIndex + 2] = nextOuter;
                triangles[triangleIndex + 3] = inner;
                triangles[triangleIndex + 4] = nextInner;
                triangles[triangleIndex + 5] = nextOuter;
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private GameObject CreatePrimitiveObject(PrimitiveType primitive)
        {
            var gameObject = GameObject.CreatePrimitive(primitive);
            return gameObject;
        }

        private static void AddSketchOutline(RectTransform parent, float thickness, float jitter, int strokes)
        {
            var outlineObject = new GameObject("Sketch Outline", typeof(RectTransform), typeof(SketchOutlineGraphic));
            outlineObject.transform.SetParent(parent, false);
            var rect = outlineObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var outline = outlineObject.GetComponent<SketchOutlineGraphic>();
            outline.color = new Color32(54, 48, 42, 118);
            outline.Thickness = thickness;
            outline.Jitter = jitter;
            outline.Strokes = strokes;
            outline.Seed = Mathf.Abs(parent.name.GetHashCode());
            outline.raycastTarget = false;
        }

        private Material CreateMaterial(string name, Color color, float alpha)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("UI/Default") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Hidden/Internal-Colored") ??
                Shader.Find("Standard");
            var material = shader == null ? new Material(Graphic.defaultGraphicMaterial) : new Material(shader);
            material.name = name;
            color.a = alpha;
            material.color = color;
            material.mainTexture = Texture2D.whiteTexture;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            }

            if (alpha < 0.99f)
            {
                if (material.HasProperty("_Mode"))
                {
                    material.SetFloat("_Mode", 3f);
                }

                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                }

                if (material.HasProperty("_SrcBlend"))
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                }

                if (material.HasProperty("_DstBlend"))
                {
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }

                if (material.HasProperty("_ZWrite"))
                {
                    material.SetInt("_ZWrite", 0);
                }

                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            else
            {
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 0f);
                }

                if (material.HasProperty("_ZWrite"))
                {
                    material.SetInt("_ZWrite", 1);
                }

                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }

            return material;
        }

        private PhysicsMaterial CreatePhysicsMaterial(string name, float friction, float bounciness)
        {
            var material = new PhysicsMaterial(name)
            {
                dynamicFriction = friction,
                staticFriction = friction,
                bounciness = bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return material;
        }

        private RectTransform CreatePanel(Transform parent, string name, Anchor anchor, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(58, 47, 38, 38);
            outline.effectDistance = new Vector2(2f, -2f);
            return rect;
        }

        private Button CreateButton(Transform parent, string name, string label, Anchor anchor, Vector2 offsetMin, Vector2 offsetMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = gameObject.GetComponent<Image>();
            image.color = SketchPalette.TilePaper;
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(58, 47, 38, 40);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            AddSketchOutline(rect, 2.8f, 2.4f, 2);
            var button = gameObject.GetComponent<Button>();
            button.colors = SketchUiFactory.ButtonColors();
            CreateText(rect, "Label", label, 28, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, Vector2.zero, Vector2.zero);
            return button;
        }

        private void AddUiSwatch(Transform parent, Color color, Anchor anchor, Vector2 offsetMin, Vector2 offsetMax)
        {
            var gameObject = new GameObject("Sketch Swatch", typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = gameObject.GetComponent<Image>();
            color.a = Mathf.Min(color.a, 0.82f);
            image.color = color;
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(58, 47, 38, 35);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, Anchor anchor, Vector2 offsetMin, Vector2 offsetMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentForAnchor(anchor);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 9);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static TextAnchor TextAlignmentForAnchor(Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft:
                case Anchor.BottomLeft:
                case Anchor.StretchLeft:
                    return TextAnchor.MiddleLeft;
                case Anchor.TopRight:
                case Anchor.StretchRight:
                    return TextAnchor.MiddleRight;
                default:
                    return TextAnchor.MiddleCenter;
            }
        }

        private static void ApplyAnchor(RectTransform rect, Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    break;
                case Anchor.TopStretch:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = Vector2.one;
                    break;
                case Anchor.BottomStretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = new Vector2(1f, 0f);
                    break;
                case Anchor.StretchRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    break;
                case Anchor.StretchLeft:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    break;
                case Anchor.TopLeft:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    break;
                case Anchor.TopRight:
                    rect.anchorMin = Vector2.one;
                    rect.anchorMax = Vector2.one;
                    break;
                case Anchor.BottomLeft:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    break;
                case Anchor.BottomRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    break;
                case Anchor.BottomCenter:
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    break;
                case Anchor.Center:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
            }
        }

        private enum Anchor
        {
            Stretch,
            TopStretch,
            BottomStretch,
            StretchLeft,
            StretchRight,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            BottomCenter,
            Center
        }

        private readonly struct ButtonSpec
        {
            public ButtonSpec(BaristaIngredient ingredient, string label)
            {
                Ingredient = ingredient;
                Label = label;
            }

            public BaristaIngredient Ingredient { get; }
            public string Label { get; }
        }

        private readonly struct IngredientButtonView
        {
            public IngredientButtonView(Transform transform, Image image, Vector3 baseScale)
            {
                Transform = transform;
                Image = image;
                BaseScale = baseScale;
            }

            public Transform Transform { get; }
            public Image Image { get; }
            public Vector3 BaseScale { get; }
        }

        private readonly struct DispenserView
        {
            public DispenserView(
                Transform body,
                Transform nozzle,
                Transform stream,
                Renderer streamRenderer,
                Transform[] streamPieces,
                Renderer[] streamPieceRenderers,
                Renderer activeRenderer,
                Renderer renderer,
                Vector3 baseBodyPosition,
                Vector3 baseNozzlePosition,
                Vector3 baseBodyScale,
                Vector3 baseNozzleScale)
            {
                Body = body;
                Nozzle = nozzle;
                Stream = stream;
                StreamRenderer = streamRenderer;
                StreamPieces = streamPieces;
                StreamPieceRenderers = streamPieceRenderers;
                ActiveRenderer = activeRenderer;
                Renderer = renderer;
                BaseBodyPosition = baseBodyPosition;
                BaseNozzlePosition = baseNozzlePosition;
                BaseBodyScale = baseBodyScale;
                BaseNozzleScale = baseNozzleScale;
            }

            public Transform Body { get; }
            public Transform Nozzle { get; }
            public Transform Stream { get; }
            public Renderer StreamRenderer { get; }
            public Transform[] StreamPieces { get; }
            public Renderer[] StreamPieceRenderers { get; }
            public Renderer ActiveRenderer { get; }
            public Renderer Renderer { get; }
            public Vector3 BaseBodyPosition { get; }
            public Vector3 BaseNozzlePosition { get; }
            public Vector3 BaseBodyScale { get; }
            public Vector3 BaseNozzleScale { get; }
        }

        private struct PouredPiece
        {
            public PouredPiece(
                GameObject gameObject,
                Rigidbody2D body,
                BaristaIngredient ingredient,
                float amount,
                float spawnedAt,
                Vector3 startPosition,
                Vector3 targetPosition,
                Quaternion startRotation,
                Quaternion targetRotation,
                float fallDuration)
            {
                GameObject = gameObject;
                Body = body;
                Ingredient = ingredient;
                Amount = amount;
                SpawnedAt = spawnedAt;
                StartPosition = startPosition;
                TargetPosition = targetPosition;
                StartRotation = startRotation;
                TargetRotation = targetRotation;
                FallDuration = fallDuration;
                Counted = true;
                Wasted = false;
                WasteAt = 0f;
            }

            public GameObject GameObject { get; }
            public Rigidbody2D Body { get; }
            public BaristaIngredient Ingredient { get; }
            public float Amount { get; }
            public float SpawnedAt { get; }
            public Vector3 StartPosition { get; }
            public Vector3 TargetPosition { get; }
            public Quaternion StartRotation { get; }
            public Quaternion TargetRotation { get; }
            public float FallDuration { get; }
            public bool Counted { get; set; }
            public bool Wasted { get; set; }
            public float WasteAt { get; set; }
        }

        private readonly struct LiquidDrop
        {
            public LiquidDrop(
                GameObject gameObject,
                BaristaIngredient ingredient,
                float spawnedAt,
                Vector3 startPosition,
                Vector3 endPosition,
                Vector3 baseScale,
                float duration)
            {
                GameObject = gameObject;
                Ingredient = ingredient;
                SpawnedAt = spawnedAt;
                StartPosition = startPosition;
                EndPosition = endPosition;
                BaseScale = baseScale;
                Duration = duration;
            }

            public GameObject GameObject { get; }
            public BaristaIngredient Ingredient { get; }
            public float SpawnedAt { get; }
            public Vector3 StartPosition { get; }
            public Vector3 EndPosition { get; }
            public Vector3 BaseScale { get; }
            public float Duration { get; }
        }
    }
}
