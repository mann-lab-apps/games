using System;
using System.Collections.Generic;
using System.Linq;
using MannLab.Ads;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.YachtRush
{
    public sealed class YachtRushController : MonoBehaviour
    {
        private enum RoundTwist
        {
            ContractHand,
            RollRule,
            RushDie
        }

        private enum DeckAssetKind
        {
            Sail,
            Anchor,
            Cargo,
            Compass,
            HullPatch,
            Harbor,
            Storm
        }

        private const string BestScoreKey = "mannlab.yacht_sailing.best_distance";
        private const float TableHalfWidth = 5.7f;
        private const float TableHalfDepth = 4.05f;
        private const float DiceSize = 0.46f;
        private const float SettledVelocity = 0.24f;
        private const float SettledAngularVelocity = 0.72f;
        private const float RequiredStableSeconds = 0.18f;
        private const float SnapVelocity = 0.18f;
        private const float SnapAngularVelocity = 0.56f;
        private const float StrongFrictionVelocity = 0.95f;
        private const float PlayMinX = -4.75f;
        private const float PlayMaxX = 4.75f;
        private const float PlayMinZ = -1.9f;
        private const float PlayMaxZ = 2.55f;
        private const float DiceRestY = 0.28f;
        private const float DiceMinSpacing = 1f;
        private const float BowlRadiusX = 1.5f;
        private const float BowlRadiusZ = 0.72f;
        private const float BowlDiceSpacing = 0.62f;
        private const float RushIntroSeconds = 1.25f;
        private const string GameKey = "yacht-sailing";
        private const string GameOverInterstitialIosAdUnitId = "ca-app-pub-4525914685149405/8278784535";
        private const string GameOverInterstitialAndroidAdUnitId = "";
        private const int GameOverInterstitialInterval = 1;

        private static readonly Vector3 BowlHome = new Vector3(-2.75f, 0.54f, -1.25f);
        private static readonly Vector3 BowlDockLandscape = new Vector3(-8.35f, 0.54f, -0.15f);
        private static readonly Vector3 BowlDockPortrait = new Vector3(-4.55f, 0.54f, -4.25f);
        private static readonly Quaternion CameraRotation = Quaternion.Euler(68f, 0f, 0f);
        private static readonly string[] CrewCouncilLines =
        {
            "Five dice become monthly ship resources.",
            "Spend resources to open stronger voyage strategies.",
            "Plan the month before the sea changes.",
            "The crew debates the voyage. Dice settle the command.",
            "Throw onto the deck, then choose the captain's order."
        };
        private static readonly VoyageStrategy[] StrategyCatalog =
        {
            VoyageStrategy.TailwindRun,
            VoyageStrategy.PatchTheHull,
            VoyageStrategy.StockTheHold,
            VoyageStrategy.RallyTheCrew,
            VoyageStrategy.PortBargain,
            VoyageStrategy.ReadTheStars,
            VoyageStrategy.SafePassage,
            VoyageStrategy.LongVoyage,
            VoyageStrategy.RepairConvoy,
            VoyageStrategy.TradeRoute,
            VoyageStrategy.FullDeck,
            VoyageStrategy.CaptainsGambit
        };
        private static readonly YachtRushCategory[] StrategyTokenCategories =
        {
            YachtRushCategory.Ones,
            YachtRushCategory.Twos,
            YachtRushCategory.Threes,
            YachtRushCategory.Fours,
            YachtRushCategory.Fives,
            YachtRushCategory.Sixes,
            YachtRushCategory.FourOfAKind,
            YachtRushCategory.FullHouse,
            YachtRushCategory.SmallStraight,
            YachtRushCategory.LargeStraight,
            YachtRushCategory.Yacht,
            YachtRushCategory.Chance
        };

        private readonly List<DieView> dice = new List<DieView>();
        private readonly Dictionary<YachtRushCategory, ScoreRecord> scores =
            new Dictionary<YachtRushCategory, ScoreRecord>();
        private readonly Dictionary<YachtRushCategory, ScoreButtonView> scoreButtons =
            new Dictionary<YachtRushCategory, ScoreButtonView>();
        private readonly Dictionary<YachtRushCategory, CommandTokenView> commandTokens =
            new Dictionary<YachtRushCategory, CommandTokenView>();
        private readonly Dictionary<Collider, YachtRushCategory> commandTokenColliders =
            new Dictionary<Collider, YachtRushCategory>();
        private readonly Dictionary<VoyageDeckZone, Renderer> voyageZoneRenderers =
            new Dictionary<VoyageDeckZone, Renderer>();
        private readonly List<Button> captainOrderButtons = new List<Button>();
        private readonly List<Text> captainOrderButtonLabels = new List<Text>();
        private readonly List<Image> captainOrderArtPanels = new List<Image>();
        private readonly List<Text> captainOrderArtLabels = new List<Text>();
        private readonly System.Random random = new System.Random(Environment.TickCount);

        private Camera mainCamera;
        private Camera backgroundCamera;
        private Transform bowlRoot;
        private Transform bowlRim;
        private Transform bowlGripHalo;
        private Renderer bowlGripHaloRenderer;
        private Transform tableRoot;
        private Transform deckRoot;
        private PhysicsMaterial dicePhysicsMaterial;
        private PhysicsMaterial tablePhysicsMaterial;
        private Material tableMaterial;
        private AudioSource audioSource;
        private readonly Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        private readonly List<Renderer> twistAccentRenderers = new List<Renderer>();
        private readonly Dictionary<DeckAssetKind, DeckAssetView> deckAssets =
            new Dictionary<DeckAssetKind, DeckAssetView>();
        private readonly Dictionary<int, DeckAssetView> resourceStations =
            new Dictionary<int, DeckAssetView>();
        private readonly Dictionary<int, TextMesh> resourceStationCountTexts =
            new Dictionary<int, TextMesh>();
        private readonly Dictionary<int, Text> resourceStockTexts =
            new Dictionary<int, Text>();
        private readonly Dictionary<int, Text> resourceStockStatusTexts =
            new Dictionary<int, Text>();
        private readonly HashSet<VoyageStrategy> usedLimitedStrategies = new HashSet<VoyageStrategy>();
        private Transform voyageShipMarker;
        private Canvas canvas;
        private Text roundText;
        private Text rollText;
        private Text holdText;
        private Text totalText;
        private Text crewText;
        private Text tradeText;
        private Text chartText;
        private Text bestText;
        private Image contractBackground;
        private Text contractNameText;
        private Text contractConditionText;
        private Text contractBonusText;
        private Text contractStateText;
        private Image twistAccentBar;
        private RectTransform harborMapRect;
        private RectTransform harborMapTrackRect;
        private Image harborRouteFill;
        private Image harborMapRouteFill;
        private Image harborStartPort;
        private Image harborDestinationPort;
        private RectTransform harborYachtMarker;
        private Text harborRouteText;
        private Text harborFeedbackText;
        private Text runGoalToastText;
        private GameObject commandTooltipPanel;
        private Text commandTooltipTitleText;
        private Text commandTooltipDetailText;
        private Text commandTooltipEffectText;
        private GameObject commandHelpPanel;
        private Text commandHelpTitleText;
        private Text commandHelpBodyText;
        private Button commandHelpChooseButton;
        private VoyageStrategy? commandHelpStrategy;
        private Text chooserTitleText;
        private Text voyageStatusText;
        private Text rushIntroText;
        private Text resultTitleText;
        private RectTransform scoreChooserRect;
        private GridLayoutGroup scoreGridLayout;
        private GameObject resultPanel;
        private Text resultScoreText;
        private Text resultMetaText;
        private YachtRushContract currentContract;
        private YachtRushRollRule currentRollRule;
        private YachtRushRushDie currentRushDie;
        private RoundTwist currentTwist;
        private int rushDieIndex;
        private int rollCount;
        private int lockedBeforeFinalThrow;
        private int bestScore;
        private int routeProgress;
        private int windResource;
        private int hull;
        private int supplies;
        private int crewResource;
        private int contractScore;
        private int chartResource;
        private int resolvedMonths;
        private bool hasPendingTurnResult;
        private CaptainOrder[] currentOrders = Array.Empty<CaptainOrder>();
        private VoyageDieLanding[] pendingLandings = Array.Empty<VoyageDieLanding>();
        private int[] pendingResourceCounts = new int[6];
        private VoyageStrategyPreview[] currentStrategyPreviews = Array.Empty<VoyageStrategyPreview>();
        private GameObject captainOrderPanel;
        private bool isDraggingBowl;
        private bool isResolvingRoll;
        private bool pointerStartedOverUi;
        private bool pointerStartedOnBowl;
        private float stableSeconds;
        private Vector2 dragStartScreen;
        private Vector2 lastPointerScreen;
        private Vector2 pointerVelocity;
        private float bowlShake;
        private float bowlFeedbackPulse;
        private float rushIntroTimer;
        private float harborFeedbackTimer;
        private float harborPulseTimer;
        private float runGoalToastTimer;
        private float nextShakeSoundTime;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool hasShownRunGoalToast;
        private string currentCrewCouncilLine;

        private int RoundNumber => Mathf.Min(resolvedMonths + 1, YachtRushRules.RoundCount);
        private int HeldCount => dice.Count(die => die.IsHeld);
        private int MaxRollsThisRound => 1;
        private bool CanThrow => !isResolvingRoll &&
            !hasPendingTurnResult &&
            resolvedMonths < YachtRushRules.RoundCount &&
            rollCount == 0;
        private bool CanScore => false;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            Physics.gravity = new Vector3(0f, -16f, 0f);
            Physics.defaultSolverIterations = 14;
            Physics.defaultSolverVelocityIterations = 8;
            Physics.defaultContactOffset = 0.01f;
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

            BuildWorld();
            BuildUi();
            InitializeTelemetryAndAds();
            StartRun();
            ApplyWebCaptureStateIfRequested();
        }

        private static void InitializeTelemetryAndAds()
        {
            try
            {
                FirebaseTelemetry.Initialize();
                FirebaseTelemetry.SetContext("game", GameKey);
                FirebaseTelemetry.LogEvent("app_open");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Yacht Sailing] Firebase initialization skipped: {exception.GetType().Name}");
            }

            try
            {
                MannLabAdMob.InitializeGameOverInterstitial(
                    GameKey,
                    GameOverInterstitialIosAdUnitId,
                    GameOverInterstitialInterval,
                    GameOverInterstitialAndroidAdUnitId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Yacht Sailing] AdMob initialization skipped: {exception.GetType().Name}");
            }
        }

        private void Update()
        {
            UpdateCameraForScreen();
            UpdateBowlInput();
            UpdateBowlFeedback();
            UpdateHeldDiceVisuals();
            UpdateRushIntroCue();
            UpdateHarborUiEffects();
            UpdateDeckAssetFeedback();
            ApplyBowlOverlapToCommandTokens();
            UpdateCommandTooltip();

            if (isResolvingRoll)
            {
                KeepRollingDiceInPlayArea();
                ApplyTableDiceFriction();
                UpdateRollSettlement();
            }
        }

        private void StartRun()
        {
            StopAllCoroutines();
            scores.Clear();
            rollCount = 0;
            lockedBeforeFinalThrow = 0;
            stableSeconds = 0f;
            resolvedMonths = 0;
            hasPendingTurnResult = false;
            pendingLandings = Array.Empty<VoyageDieLanding>();
            currentOrders = YachtRushRules.CoreOrders();
            pendingResourceCounts = new int[6];
            currentStrategyPreviews = Array.Empty<VoyageStrategyPreview>();
            usedLimitedStrategies.Clear();
            routeProgress = 0;
            windResource = 0;
            hull = YachtRushRules.HarborStartingHull;
            supplies = YachtRushRules.HarborStartingSupplies;
            crewResource = 0;
            contractScore = 0;
            chartResource = 0;
            ChooseRoundModifiers();
            resultPanel.SetActive(false);
            ParkBowl();

            for (var index = 0; index < dice.Count; index += 1)
            {
                dice[index].IsHeld = false;
                dice[index].SetRushDie(YachtRushRushDie.None, false);
                dice[index].SetValue((index % 6) + 1);
            }

            ApplyRushDieVisuals();
            PulseResourceStations(CurrentResourceCounts());
            PlaceUnlockedDiceInBowl();
            if (captainOrderPanel != null)
            {
                captainOrderPanel.SetActive(false);
            }

            ShowRunGoalToast();
            ShowRushIntroCue();
            FirebaseTelemetry.SetContext("round", RoundNumber.ToString());
            FirebaseTelemetry.SetContext("score", "0");
            FirebaseTelemetry.LogEvent("run_start");
            UpdateHudAndScores();
        }

        private void PrepareNextRound()
        {
            rollCount = 0;
            lockedBeforeFinalThrow = 0;
            stableSeconds = 0f;
            bowlShake = 0f;
            hasPendingTurnResult = false;
            pendingLandings = Array.Empty<VoyageDieLanding>();
            currentOrders = YachtRushRules.CoreOrders();
            pendingResourceCounts = new int[6];
            currentStrategyPreviews = Array.Empty<VoyageStrategyPreview>();
            ChooseRoundModifiers();
            ParkBowl();

            foreach (var die in dice)
            {
                die.IsHeld = false;
                die.SetRushDie(YachtRushRushDie.None, false);
                die.SetValue(random.Next(1, 7));
            }

            ApplyRushDieVisuals();
            PulseResourceStations(CurrentResourceCounts());
            PlaceUnlockedDiceInBowl();
            if (captainOrderPanel != null)
            {
                captainOrderPanel.SetActive(false);
            }

            ShowRushIntroCue();
            FirebaseTelemetry.SetContext("round", RoundNumber.ToString());
            FirebaseTelemetry.LogEvent(
                "round_start",
                new Dictionary<string, string>
                {
                    { "month", RoundNumber.ToString() },
                    { "roll_rule", "crew_resource" },
                    { "score", FinalHarborScore().ToString() },
                    { "route", routeProgress.ToString() },
                    { "hull", hull.ToString() },
                    { "supplies", supplies.ToString() },
                    { "contract_score", contractScore.ToString() }
                });
            UpdateHudAndScores();
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

            mainCamera.transform.position = new Vector3(0f, 6.8f, -7.6f);
            mainCamera.transform.rotation = Quaternion.Euler(54f, 0f, 0f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = SketchPalette.Paper;
            mainCamera.fieldOfView = 39f;
            backgroundCamera = new GameObject("Paper Backdrop Camera", typeof(Camera)).GetComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = SketchPalette.Paper;
            backgroundCamera.cullingMask = 0;
            backgroundCamera.depth = -20f;
            UpdateCameraForScreen(true);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.36f;
            audioSource.spatialBlend = 0f;
            CreateAudioClips();

            var lightObject = new GameObject("Soft Directional Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 12f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color32(255, 247, 225, 255);

            tableRoot = new GameObject("Sketch Table").transform;
            CreatePhysicsMaterials();
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Large Throw Table";
            table.transform.SetParent(tableRoot, false);
            table.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            table.transform.localScale = new Vector3(TableHalfWidth * 2f, 0.16f, TableHalfDepth * 2f);
            tableMaterial = CreateMaterial("Table Paper", new Color32(235, 240, 224, 255), 0.34f);
            table.GetComponent<Renderer>().material = tableMaterial;
            table.GetComponent<Collider>().sharedMaterial = tablePhysicsMaterial;

            CreateWall("North Rail", new Vector3(0f, 0.034f, TableHalfDepth - 0.08f), new Vector3(TableHalfWidth * 2f, 0.035f, 0.24f));
            CreateWall("South Rail", new Vector3(0f, 0.034f, -TableHalfDepth + 0.08f), new Vector3(TableHalfWidth * 2f, 0.035f, 0.24f));
            CreateWall("West Rail", new Vector3(-TableHalfWidth + 0.08f, 0.034f, 0f), new Vector3(0.24f, 0.035f, TableHalfDepth * 2f));
            CreateWall("East Rail", new Vector3(TableHalfWidth - 0.08f, 0.034f, 0f), new Vector3(0.24f, 0.035f, TableHalfDepth * 2f));
            CreateGuard("North Play Guard", new Vector3(0f, 0.58f, PlayMaxZ), new Vector3((PlayMaxX - PlayMinX) + 0.8f, 1.15f, 0.18f));
            CreateGuard("South Play Guard", new Vector3(0f, 0.58f, PlayMinZ), new Vector3((PlayMaxX - PlayMinX) + 0.8f, 1.15f, 0.18f));
            CreateGuard("West Play Guard", new Vector3(PlayMinX, 0.58f, 0f), new Vector3(0.18f, 1.15f, (PlayMaxZ - PlayMinZ) + 0.8f));
            CreateGuard("East Play Guard", new Vector3(PlayMaxX, 0.58f, 0f), new Vector3(0.18f, 1.15f, (PlayMaxZ - PlayMinZ) + 0.8f));
            CreateSketchLines();
            CreateTwistBoardAccents();
            CreateShipDeckAssets();
            CreateCommandCouncilTokens();
            BuildBowl();

            for (var index = 0; index < YachtRushRules.DiceCount; index += 1)
            {
                dice.Add(new DieView(index, CreateDie(index)));
            }
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(tableRoot, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material = CreateMaterial("Ink Rail", new Color32(74, 61, 48, 255), 0.38f);
            Destroy(wall.GetComponent<Collider>());
        }

        private void CreateGuard(string name, Vector3 position, Vector3 scale)
        {
            var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = name;
            guard.transform.SetParent(tableRoot, false);
            guard.transform.localPosition = position;
            guard.transform.localScale = scale;
            guard.GetComponent<Renderer>().enabled = false;
            guard.GetComponent<Collider>().sharedMaterial = tablePhysicsMaterial;
        }

        private void CreatePhysicsMaterials()
        {
            tablePhysicsMaterial = new PhysicsMaterial("Warm Paper Table Physics")
            {
                dynamicFriction = 0.82f,
                staticFriction = 0.9f,
                bounciness = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            dicePhysicsMaterial = new PhysicsMaterial("Soft Dice Physics")
            {
                dynamicFriction = 0.58f,
                staticFriction = 0.66f,
                bounciness = 0.04f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        private void BuildBowl()
        {
            bowlRoot = new GameObject("Korean Low Bowl").transform;
            bowlRoot.position = CurrentBowlDock();
            bowlRoot.rotation = Quaternion.Euler(-2f, -8f, 0f);

            var shadow = CreateFlatOval("Bowl Ground Shadow", CreateMaterial("Bowl Ground Shadow", new Color32(214, 205, 188, 255), 0.74f));
            shadow.transform.SetParent(bowlRoot, false);
            shadow.transform.localPosition = new Vector3(0.1f, -0.24f, -0.07f);
            shadow.transform.localScale = new Vector3(4.38f, 0.012f, 1.96f);

            var body = CreateBowlWall("Clay Wood Bowl Body", CreateMaterial("Clay Wood Body", new Color32(159, 112, 82, 255), 0.5f));
            body.transform.SetParent(bowlRoot, false);
            body.transform.localPosition = Vector3.zero;

            var outerLine = CreateOvalRing("Ink Bowl Outer Line", CreateMaterial("Bowl Ink Lip", SketchPalette.Ink, 0.58f), 2.22f, 1.18f, 2.02f, 1.02f);
            outerLine.transform.SetParent(bowlRoot, false);
            outerLine.transform.localPosition = new Vector3(0f, 0.295f, 0f);

            bowlRim = CreateOvalRing("Celadon Rim", CreateMaterial("Celadon Rim", new Color32(139, 170, 144, 255), 0.42f), 2.02f, 1.02f, 1.5f, 0.67f).transform;
            bowlRim.name = "Celadon Rim";
            bowlRim.SetParent(bowlRoot, false);
            bowlRim.localPosition = new Vector3(0f, 0.305f, 0f);

            var inner = CreateFlatOval("Rice Glaze Bowl Interior", CreateMaterial("Rice Glaze Interior", new Color32(232, 239, 219, 255), 0.5f));
            inner.name = "Rice Glaze Bowl Interior";
            inner.transform.SetParent(bowlRoot, false);
            inner.transform.localPosition = new Vector3(0f, 0.292f, 0.01f);
            inner.transform.localScale = new Vector3(3.08f, 0.024f, 1.4f);

            var innerShadow = CreateFlatOval("Bowl Inner Wash", CreateMaterial("Soft Inner Wash", new Color32(154, 178, 152, 255), 0.62f));
            innerShadow.name = "Bowl Inner Wash";
            innerShadow.transform.SetParent(bowlRoot, false);
            innerShadow.transform.localPosition = new Vector3(0.08f, 0.312f, 0.05f);
            innerShadow.transform.localScale = new Vector3(2.08f, 0.014f, 0.78f);

            var innerLine = CreateOvalRing("Thin Glaze Line", CreateMaterial("Thin Glaze Line", new Color32(181, 125, 74, 255), 0.64f), 1.58f, 0.7f, 1.52f, 0.65f);
            innerLine.transform.SetParent(bowlRoot, false);
            innerLine.transform.localPosition = new Vector3(0.02f, 0.326f, 0.02f);

            var foot = CreateOvalRing("Bowl Foot Ring", CreateMaterial("Bowl Foot Ring", new Color32(101, 70, 51, 255), 0.54f), 1.34f, 0.58f, 0.96f, 0.35f);
            foot.transform.SetParent(bowlRoot, false);
            foot.transform.localPosition = new Vector3(0f, -0.278f, -0.02f);

            var hitbox = new GameObject("Bowl Drag Hitbox", typeof(BoxCollider));
            hitbox.transform.SetParent(bowlRoot, false);
            hitbox.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            hitbox.transform.localScale = new Vector3(4.35f, 0.3f, 2.34f);
            hitbox.GetComponent<BoxCollider>().isTrigger = true;

            bowlGripHalo = CreateOvalRing("Bowl Grip Cue", CreateMaterial("Bowl Grip Cue", new Color32(255, 239, 175, 255), 0.5f), 2.35f, 1.28f, 2.23f, 1.16f).transform;
            bowlGripHalo.SetParent(bowlRoot, false);
            bowlGripHalo.localPosition = new Vector3(0f, 0.355f, 0f);
            bowlGripHaloRenderer = bowlGripHalo.GetComponent<Renderer>();
            bowlGripHaloRenderer.enabled = false;
        }

        private GameObject CreateDie(int index)
        {
            var die = new GameObject($"Die {index + 1}", typeof(BoxCollider), typeof(Rigidbody));
            die.name = $"Die {index + 1}";
            var collider = die.GetComponent<BoxCollider>();
            collider.size = Vector3.one * DiceSize;
            collider.sharedMaterial = dicePhysicsMaterial;

            var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Die Core";
            core.transform.SetParent(die.transform, false);
            core.transform.localScale = Vector3.one * DiceSize;
            core.GetComponent<Renderer>().material = CreateMaterial("Rice Paper Die", new Color32(255, 253, 246, 255), 0.3f);
            Destroy(core.GetComponent<Collider>());

            var rigidbody = die.GetComponent<Rigidbody>();
            rigidbody.mass = 0.7f;
            rigidbody.linearDamping = 0.42f;
            rigidbody.angularDamping = 0.55f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            AddDieFacePanels(die.transform);
            AddFacePips(die.transform);
            AddDieOutline(die.transform);
            AddRushDieBadge(die.transform);
            return die;
        }

        private void AddDieFacePanels(Transform die)
        {
            var material = CreateMaterial("Die Face Wash", new Color32(246, 250, 236, 255), 0.48f);
            AddDieFacePanel(die, Vector3.up, material);
            AddDieFacePanel(die, Vector3.down, material);
            AddDieFacePanel(die, Vector3.forward, material);
            AddDieFacePanel(die, Vector3.back, material);
            AddDieFacePanel(die, Vector3.right, material);
            AddDieFacePanel(die, Vector3.left, material);
        }

        private void AddDieFacePanel(Transform die, Vector3 normal, Material material)
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Soft Face Inset";
            panel.transform.SetParent(die, false);
            panel.transform.localPosition = normal * (DiceSize * 0.51f);
            panel.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            panel.transform.localScale = new Vector3(DiceSize * 0.74f, 0.007f, DiceSize * 0.74f);
            panel.GetComponent<Renderer>().material = material;
            Destroy(panel.GetComponent<Collider>());
        }

        private void AddFacePips(Transform die)
        {
            AddPipsForFace(die, 1, Vector3.up, Vector3.forward);
            AddPipsForFace(die, 6, Vector3.down, Vector3.forward);
            AddPipsForFace(die, 2, Vector3.forward, Vector3.up);
            AddPipsForFace(die, 5, Vector3.back, Vector3.up);
            AddPipsForFace(die, 3, Vector3.right, Vector3.up);
            AddPipsForFace(die, 4, Vector3.left, Vector3.up);
        }

        private void AddPipsForFace(Transform die, int value, Vector3 normal, Vector3 faceUp)
        {
            var right = Vector3.Cross(faceUp, normal).normalized;
            var up = faceUp.normalized;
            var faceCenter = normal * (DiceSize * 0.525f);
            var positions = PipPositions(value);

            foreach (var position in positions)
            {
                var pip = CreateFlatOval($"Pip {value}", CreateMaterial("Ink Pip", SketchPalette.Ink, 0.48f));
                pip.transform.SetParent(die, false);
                pip.transform.localScale = new Vector3(0.066f, 0.008f, 0.066f);
                pip.transform.localPosition = faceCenter + right * (position.x * DiceSize * 0.26f) + up * (position.y * DiceSize * 0.26f);
                pip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            }
        }

        private void AddVoyageFaceLabel(Transform die, int value, Vector3 normal, Vector3 faceUp)
        {
            var label = new GameObject($"Face {value} Voyage Label", typeof(TextMesh));
            label.transform.SetParent(die, false);
            label.transform.localPosition = FaceSymbolPosition(normal, faceUp, new Vector2(0.21f, -0.22f));
            label.transform.localRotation = Quaternion.LookRotation(normal.normalized, faceUp.normalized);

            var text = label.GetComponent<TextMesh>();
            text.text = VoyageFaceShortName(value);
            text.fontSize = 48;
            text.characterSize = 0.022f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = VoyageFaceColor(value);
        }

        private static string VoyageFaceShortName(int value)
        {
            switch (Mathf.Clamp(value, 1, 6))
            {
                case 1:
                    return "W";
                case 2:
                    return "H";
                case 3:
                    return "S";
                case 4:
                    return "C";
                case 5:
                    return "T";
                default:
                    return "Ch";
            }
        }

        private void AddVoyageIconForFace(Transform die, int value, Vector3 normal, Vector3 faceUp)
        {
            var ink = CreateMaterial($"Die {value} Voyage Icon Ink", VoyageFaceColor(value), 0.52f);
            switch (value)
            {
                case 1:
                    AddFaceSymbolBlock(die, "Wind Mark A", normal, faceUp, new Vector2(-0.16f, -0.18f), new Vector2(0.16f, 0.024f), -22f, ink);
                    AddFaceSymbolBlock(die, "Wind Mark B", normal, faceUp, new Vector2(0f, -0.2f), new Vector2(0.15f, 0.024f), -22f, ink);
                    AddFaceSymbolBlock(die, "Wind Mark C", normal, faceUp, new Vector2(0.15f, -0.18f), new Vector2(0.13f, 0.024f), -22f, ink);
                    break;
                case 2:
                    AddFaceSymbolBlock(die, "Hull Plank", normal, faceUp, new Vector2(-0.12f, -0.17f), new Vector2(0.22f, 0.12f), -10f, ink);
                    AddFaceSymbolBlock(die, "Hull Stitch", normal, faceUp, new Vector2(-0.12f, -0.17f), new Vector2(0.018f, 0.16f), -10f, CreateMaterial("Hull Stitch Ink", SketchPalette.Ink, 0.58f));
                    break;
                case 3:
                    AddFaceSymbolBlock(die, "Supply Crate", normal, faceUp, new Vector2(-0.14f, -0.16f), new Vector2(0.17f, 0.13f), -8f, ink);
                    AddFaceSymbolBlock(die, "Supply Strap", normal, faceUp, new Vector2(-0.14f, -0.16f), new Vector2(0.18f, 0.018f), -8f, CreateMaterial("Supply Strap Ink", SketchPalette.Ink, 0.58f));
                    break;
                case 4:
                    AddFaceSymbolOval(die, "Crew Head A", normal, faceUp, new Vector2(-0.15f, -0.16f), new Vector2(0.08f, 0.08f), ink);
                    AddFaceSymbolOval(die, "Crew Head B", normal, faceUp, new Vector2(0.02f, -0.16f), new Vector2(0.08f, 0.08f), ink);
                    AddFaceSymbolBlock(die, "Crew Bench", normal, faceUp, new Vector2(-0.07f, -0.23f), new Vector2(0.27f, 0.035f), 0f, ink);
                    break;
                case 5:
                    AddFaceSymbolOval(die, "Trade Coin A", normal, faceUp, new Vector2(-0.16f, -0.17f), new Vector2(0.09f, 0.09f), ink);
                    AddFaceSymbolOval(die, "Trade Coin B", normal, faceUp, new Vector2(0.02f, -0.18f), new Vector2(0.08f, 0.08f), ink);
                    AddFaceSymbolBlock(die, "Trade Ledger", normal, faceUp, new Vector2(0.16f, -0.16f), new Vector2(0.13f, 0.024f), 0f, CreateMaterial("Trade Ledger Ink", SketchPalette.Ink, 0.58f));
                    break;
                case 6:
                    AddFaceSymbolOval(die, "Chart Compass Ring", normal, faceUp, new Vector2(-0.06f, -0.17f), new Vector2(0.15f, 0.15f), ink);
                    AddFaceSymbolBlock(die, "Chart Needle A", normal, faceUp, new Vector2(-0.06f, -0.17f), new Vector2(0.22f, 0.018f), 35f, CreateMaterial("Chart Needle Ink A", SketchPalette.Ink, 0.58f));
                    AddFaceSymbolBlock(die, "Chart Needle B", normal, faceUp, new Vector2(-0.06f, -0.17f), new Vector2(0.018f, 0.22f), 35f, CreateMaterial("Chart Needle Ink B", SketchPalette.Ink, 0.58f));
                    break;
            }
        }

        private void AddFaceSymbolBlock(
            Transform die,
            string name,
            Vector3 normal,
            Vector3 faceUp,
            Vector2 center,
            Vector2 size,
            float yaw,
            Material material)
        {
            var symbol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            symbol.name = name;
            symbol.transform.SetParent(die, false);
            symbol.transform.localPosition = FaceSymbolPosition(normal, faceUp, center);
            symbol.transform.localRotation = FaceSymbolRotation(normal, faceUp, yaw);
            symbol.transform.localScale = new Vector3(size.x * DiceSize, 0.009f, size.y * DiceSize);
            symbol.GetComponent<Renderer>().material = material;
            Destroy(symbol.GetComponent<Collider>());
        }

        private void AddFaceSymbolOval(
            Transform die,
            string name,
            Vector3 normal,
            Vector3 faceUp,
            Vector2 center,
            Vector2 size,
            Material material)
        {
            var symbol = CreateFlatOval(name, material);
            symbol.transform.SetParent(die, false);
            symbol.transform.localPosition = FaceSymbolPosition(normal, faceUp, center);
            symbol.transform.localRotation = FaceSymbolRotation(normal, faceUp, 0f);
            symbol.transform.localScale = new Vector3(size.x * DiceSize, 0.008f, size.y * DiceSize);
        }

        private void AddFaceSymbolTriangle(
            Transform die,
            string name,
            Vector3 normal,
            Vector3 faceUp,
            Vector2 center,
            Vector2 size,
            float yaw,
            Material material)
        {
            var symbol = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            symbol.transform.SetParent(die, false);
            symbol.transform.localPosition = FaceSymbolPosition(normal, faceUp, center);
            symbol.transform.localRotation = FaceSymbolRotation(normal, faceUp, yaw);
            symbol.transform.localScale = new Vector3(size.x * DiceSize, 0.008f, size.y * DiceSize);
            symbol.GetComponent<MeshFilter>().mesh = CreateTriangleMesh(name + " Mesh");
            symbol.GetComponent<MeshRenderer>().material = material;
        }

        private static Vector3 FaceSymbolPosition(Vector3 normal, Vector3 faceUp, Vector2 center)
        {
            var right = Vector3.Cross(faceUp, normal).normalized;
            var up = faceUp.normalized;
            return normal * (DiceSize * 0.535f) + right * (center.x * DiceSize) + up * (center.y * DiceSize);
        }

        private static Quaternion FaceSymbolRotation(Vector3 normal, Vector3 faceUp, float yaw)
        {
            return Quaternion.LookRotation(faceUp.normalized, normal.normalized) * Quaternion.Euler(0f, yaw, 0f);
        }

        private static Color VoyageFaceColor(int value)
        {
            switch (Mathf.Clamp(value, 1, 6))
            {
                case 1:
                    return new Color32(113, 154, 132, 255);
                case 2:
                    return new Color32(121, 169, 143, 255);
                case 3:
                    return new Color32(181, 126, 72, 255);
                case 4:
                    return new Color32(91, 119, 154, 255);
                case 5:
                    return new Color32(143, 194, 214, 255);
                default:
                    return new Color32(236, 222, 148, 255);
            }
        }

        private static Vector2[] PipPositions(int value)
        {
            switch (value)
            {
                case 1:
                    return new[] { Vector2.zero };
                case 2:
                    return new[] { new Vector2(-1f, 1f), new Vector2(1f, -1f) };
                case 3:
                    return new[] { new Vector2(-1f, 1f), Vector2.zero, new Vector2(1f, -1f) };
                case 4:
                    return new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
                case 5:
                    return new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-1f, -1f), new Vector2(1f, -1f) };
                default:
                    return new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
            }
        }

        private void AddDieOutline(Transform die)
        {
            var material = CreateMaterial("Ink Edge", new Color32(42, 37, 31, 255), 0.6f);
            var half = DiceSize * 0.58f;
            var thickness = 0.035f;
            var edgeIndex = 0;

            for (var axis = 0; axis < 3; axis += 1)
            {
                for (var first = -1; first <= 1; first += 2)
                {
                    for (var second = -1; second <= 1; second += 2)
                    {
                        var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        edge.name = $"Sketch Edge {edgeIndex + 1}";
                        edge.transform.SetParent(die, false);
                        edge.transform.localPosition = EdgePosition(axis, first * half, second * half);
                        edge.transform.localScale = EdgeScale(axis, DiceSize * 1.16f, thickness);
                        edge.GetComponent<Renderer>().material = material;
                        Destroy(edge.GetComponent<Collider>());
                        edgeIndex += 1;
                    }
                }
            }
        }

        private void AddRushDieBadge(Transform die)
        {
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Rush Die Badge";
            badge.transform.SetParent(die, false);
            badge.transform.localPosition = new Vector3(0f, DiceSize * 0.63f, -DiceSize * 0.36f);
            badge.transform.localScale = new Vector3(DiceSize * 0.96f, 0.026f, DiceSize * 0.15f);
            badge.GetComponent<Renderer>().material = CreateMaterial("Rush Die Badge", new Color32(65, 116, 154, 255), 0.4f);
            Destroy(badge.GetComponent<Collider>());
            badge.SetActive(false);

            var halo = CreateOvalRing("Rush Die Halo", CreateMaterial("Rush Die Halo", new Color32(65, 116, 154, 255), 0.42f), DiceSize * 0.57f, DiceSize * 0.57f, DiceSize * 0.39f, DiceSize * 0.39f);
            halo.transform.SetParent(die, false);
            halo.transform.localPosition = new Vector3(0f, DiceSize * 0.64f, 0f);
            halo.SetActive(false);

            for (var index = 0; index < 3; index += 1)
            {
                var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mark.name = $"Hazard Glyph {index + 1}";
                mark.transform.SetParent(die, false);
                mark.transform.localPosition = new Vector3(0f, DiceSize * 0.675f + index * 0.002f, 0f);
                mark.transform.localScale = new Vector3(DiceSize * 0.52f, 0.018f, DiceSize * 0.055f);
                mark.GetComponent<Renderer>().material = CreateMaterial("Hazard Glyph Ink", SketchPalette.Ink, 0.5f);
                Destroy(mark.GetComponent<Collider>());
                mark.SetActive(false);
            }
        }

        private static Vector3 EdgePosition(int axis, float first, float second)
        {
            switch (axis)
            {
                case 0:
                    return new Vector3(0f, first, second);
                case 1:
                    return new Vector3(first, 0f, second);
                default:
                    return new Vector3(first, second, 0f);
            }
        }

        private static Vector3 EdgeScale(int axis, float length, float thickness)
        {
            switch (axis)
            {
                case 0:
                    return new Vector3(length, thickness, thickness);
                case 1:
                    return new Vector3(thickness, length, thickness);
                default:
                    return new Vector3(thickness, thickness, length);
            }
        }

        private void CreateSketchLines()
        {
            var material = CreateMaterial("Table Sketch Lines", new Color32(62, 89, 76, 255), 0.64f);
            for (var index = 0; index < 11; index += 1)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"Table Brush Line {index + 1}";
                line.transform.SetParent(tableRoot, false);
                line.transform.localPosition = new Vector3(-TableHalfWidth + 1.12f + index * 0.9f, 0.028f, 0.05f);
                line.transform.localRotation = Quaternion.Euler(0f, 1.2f - index % 3 * 1.2f, 0f);
                line.transform.localScale = new Vector3(0.014f, 0.014f, TableHalfDepth * 1.62f);
                line.GetComponent<Renderer>().material = material;
                Destroy(line.GetComponent<Collider>());
            }
        }

        private void CreateTwistBoardAccents()
        {
            twistAccentRenderers.Clear();
            var material = CreateMaterial("Twist Board Accent", new Color32(187, 126, 70, 255), 0.48f);
            AddTwistAccent("Twist North Rail", new Vector3(0f, 0.018f, TableHalfDepth - 0.42f), new Vector3(TableHalfWidth * 1.55f, 0.014f, 0.06f), Quaternion.identity, material);
            AddTwistAccent("Twist South Rail", new Vector3(0f, 0.018f, -TableHalfDepth + 0.42f), new Vector3(TableHalfWidth * 1.55f, 0.014f, 0.06f), Quaternion.identity, material);
            AddTwistAccent("Twist West Rail", new Vector3(-TableHalfWidth + 0.42f, 0.018f, 0f), new Vector3(0.06f, 0.014f, TableHalfDepth * 1.45f), Quaternion.identity, material);
            AddTwistAccent("Twist East Rail", new Vector3(TableHalfWidth - 0.42f, 0.018f, 0f), new Vector3(0.06f, 0.014f, TableHalfDepth * 1.45f), Quaternion.identity, material);

            for (var index = 0; index < 3; index += 1)
            {
                AddTwistAccent(
                    $"Twist Brush Tick {index + 1}",
                    new Vector3(-TableHalfWidth + 0.92f + index * 0.36f, 0.024f, TableHalfDepth - 0.78f),
                    new Vector3(0.055f, 0.014f, 0.42f),
                    Quaternion.Euler(0f, -22f, 0f),
                    material);
            }
        }

        private void AddTwistAccent(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accent.name = name;
            accent.transform.SetParent(tableRoot, false);
            accent.transform.localPosition = position;
            accent.transform.localRotation = rotation;
            accent.transform.localScale = scale;
            var renderer = accent.GetComponent<Renderer>();
            renderer.material = material;
            twistAccentRenderers.Add(renderer);
            Destroy(accent.GetComponent<Collider>());
        }

        private void CreateShipDeckAssets()
        {
            deckAssets.Clear();
            voyageZoneRenderers.Clear();
            deckRoot = new GameObject("Ship Deck Action Board").transform;
            deckRoot.SetParent(tableRoot, false);
            deckRoot.localPosition = Vector3.zero;

            var ink = CreateMaterial("Deck Asset Ink", SketchPalette.Ink, 0.6f);
            var line = CreateMaterial("Deck Route Ink", new Color32(92, 112, 94, 255), 0.55f);
            var parchment = CreateMaterial("Deck Parchment", new Color32(247, 242, 221, 255), 0.42f);
            var sailWash = CreateMaterial("Deck Sail Wash", new Color32(251, 247, 225, 255), 0.36f);
            var cargoWash = CreateMaterial("Deck Cargo Wash", new Color32(181, 142, 99, 255), 0.5f);
            var celadon = CreateMaterial("Deck Celadon Wash", new Color32(139, 170, 144, 255), 0.45f);
            var seaBlue = CreateMaterial("Deck Sea Wash", new Color32(177, 205, 213, 255), 0.5f);
            var warning = CreateMaterial("Deck Hazard Wash", new Color32(154, 105, 96, 255), 0.5f);
            var deckLine = CreateMaterial("Deck Board Line", new Color32(102, 85, 65, 255), 0.56f);

            AddDeckBlock(deckRoot, "Deck Bow Rail", new Vector3(0f, 0.046f, 2.25f), new Vector3(8.7f, 0.022f, 0.055f), Quaternion.identity, deckLine);
            AddDeckBlock(deckRoot, "Deck Stern Rail", new Vector3(0f, 0.046f, -2.62f), new Vector3(7.9f, 0.022f, 0.055f), Quaternion.identity, deckLine);
            AddDeckBlock(deckRoot, "Deck Port Rope", new Vector3(-4.45f, 0.046f, -0.12f), new Vector3(0.052f, 0.022f, 4.5f), Quaternion.Euler(0f, -3f, 0f), deckLine);
            AddDeckBlock(deckRoot, "Deck Starboard Rope", new Vector3(4.45f, 0.046f, -0.12f), new Vector3(0.052f, 0.022f, 4.5f), Quaternion.Euler(0f, 3f, 0f), deckLine);

            for (var index = 0; index < 6; index += 1)
            {
                AddDeckBlock(
                    deckRoot,
                    $"Deck Cross Plank {index + 1}",
                    new Vector3(-3.45f + index * 1.38f, 0.044f, -0.12f),
                    new Vector3(0.032f, 0.018f, 4.16f),
                    Quaternion.Euler(0f, index % 2 == 0 ? -2f : 2f, 0f),
                    new Color32(83, 111, 92, 180));
            }

            for (var index = 0; index < 8; index += 1)
            {
                AddDeckBlock(
                    deckRoot,
                    $"Chart Route Dash {index + 1}",
                    new Vector3(-3.6f + index * 0.96f, 0.052f, -0.78f + Mathf.Sin(index * 0.85f) * 0.18f),
                    new Vector3(0.46f, 0.018f, 0.04f),
                    Quaternion.Euler(0f, 6f + Mathf.Sin(index) * 15f, 0f),
                    line);
            }

            AddDeckBlock(deckRoot, "Upper Route Line", new Vector3(0f, 0.075f, 3.58f), new Vector3(8.35f, 0.026f, 0.06f), Quaternion.identity, line);
            AddDeckBlock(deckRoot, "Start Port Sketch", new Vector3(-4.24f, 0.09f, 3.58f), new Vector3(0.52f, 0.04f, 0.34f), Quaternion.Euler(0f, -8f, 0f), parchment);
            AddDeckBlock(deckRoot, "Far Island Sketch", new Vector3(4.08f, 0.09f, 3.58f), new Vector3(0.62f, 0.04f, 0.38f), Quaternion.Euler(0f, 10f, 0f), celadon);
            AddDeckBlock(deckRoot, "Reef Mark A", new Vector3(1.35f, 0.068f, -1.04f), new Vector3(0.38f, 0.02f, 0.045f), Quaternion.Euler(0f, 28f, 0f), warning);
            AddDeckBlock(deckRoot, "Reef Mark B", new Vector3(1.64f, 0.068f, -1.12f), new Vector3(0.32f, 0.02f, 0.045f), Quaternion.Euler(0f, -16f, 0f), warning);

            voyageShipMarker = new GameObject("Voyage Ship Marker").transform;
            voyageShipMarker.SetParent(deckRoot, false);
            voyageShipMarker.localPosition = new Vector3(-4.06f, 0.14f, 3.58f);
            AddDeckTriangle(voyageShipMarker, "Ship Paper Sail", new Vector3(0.08f, 0.014f, 0.06f), new Vector3(0.52f, 1f, 0.58f), Quaternion.Euler(0f, 14f, 0f), sailWash);
            AddDeckBlock(voyageShipMarker, "Ship Ink Hull", new Vector3(0f, 0f, -0.18f), new Vector3(0.58f, 0.03f, 0.08f), Quaternion.Euler(0f, 4f, 0f), ink);
            AddDeckBlock(voyageShipMarker, "Ship Mast", new Vector3(-0.04f, 0.018f, 0.04f), new Vector3(0.035f, 0.024f, 0.5f), Quaternion.identity, ink);

            var compass = new GameObject("Compass Asset").transform;
            compass.SetParent(deckRoot, false);
            compass.localPosition = new Vector3(-7.12f, 0.068f, 2.86f);
            AddDeckRing(compass, "Compass Ring", Vector3.zero, new Vector3(0.82f, 1f, 0.82f), ink, 0.48f, 0.48f, 0.38f, 0.38f);
            AddDeckRing(compass, "Compass Wash", new Vector3(0f, -0.002f, 0f), new Vector3(0.74f, 1f, 0.74f), parchment, 0.44f, 0.44f, 0.18f, 0.18f);
            AddDeckBlock(compass, "Compass Needle", Vector3.zero, new Vector3(0.06f, 0.02f, 0.68f), Quaternion.Euler(0f, -32f, 0f), new Color32(68, 99, 120, 255));
            AddDeckBlock(compass, "Compass Cross", Vector3.zero, new Vector3(0.48f, 0.016f, 0.04f), Quaternion.Euler(0f, 12f, 0f), ink);
            AddDeckLabel(compass, "SCOUT", new Vector3(0f, 0.034f, -0.62f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.Compass, compass);

            var sail = new GameObject("Sail Asset").transform;
            sail.SetParent(deckRoot, false);
            sail.localPosition = new Vector3(-7.16f, 0.072f, 1.28f);
            AddDeckBlock(sail, "Sail Mast", new Vector3(-0.1f, 0f, 0f), new Vector3(0.055f, 0.035f, 1.15f), Quaternion.identity, ink);
            AddDeckTriangle(sail, "Open Sail Cloth", new Vector3(0.1f, 0.01f, 0.02f), new Vector3(0.98f, 1f, 1.08f), Quaternion.Euler(0f, -8f, 0f), sailWash);
            AddDeckBlock(sail, "Sail Ink Edge", new Vector3(0.18f, 0.028f, -0.1f), new Vector3(0.055f, 0.022f, 1.05f), Quaternion.Euler(0f, -27f, 0f), ink);
            AddDeckLabel(sail, "SAIL", new Vector3(0f, 0.034f, -0.72f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.Sail, sail);

            var cargo = new GameObject("Cargo Asset").transform;
            cargo.SetParent(deckRoot, false);
            cargo.localPosition = new Vector3(-7.08f, 0.09f, -0.3f);
            AddDeckBlock(cargo, "Cargo Crate A", new Vector3(-0.24f, 0f, -0.05f), new Vector3(0.5f, 0.15f, 0.42f), Quaternion.Euler(0f, 5f, 0f), cargoWash);
            AddDeckBlock(cargo, "Cargo Crate B", new Vector3(0.28f, 0.014f, 0.05f), new Vector3(0.46f, 0.16f, 0.4f), Quaternion.Euler(0f, -9f, 0f), cargoWash);
            AddDeckBlock(cargo, "Cargo Strap A", new Vector3(-0.24f, 0.092f, -0.05f), new Vector3(0.48f, 0.02f, 0.04f), Quaternion.Euler(0f, 5f, 0f), ink);
            AddDeckBlock(cargo, "Cargo Strap B", new Vector3(0.28f, 0.104f, 0.05f), new Vector3(0.04f, 0.02f, 0.4f), Quaternion.Euler(0f, -9f, 0f), ink);
            AddDeckLabel(cargo, "CARGO", new Vector3(0f, 0.064f, -0.62f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.Cargo, cargo);

            var anchor = new GameObject("Anchor Asset").transform;
            anchor.SetParent(deckRoot, false);
            anchor.localPosition = new Vector3(7.12f, 0.072f, 1.58f);
            AddDeckBlock(anchor, "Anchor Stem", Vector3.zero, new Vector3(0.07f, 0.026f, 0.86f), Quaternion.identity, ink);
            AddDeckBlock(anchor, "Anchor Crown", new Vector3(0f, 0.004f, -0.42f), new Vector3(0.72f, 0.026f, 0.065f), Quaternion.identity, ink);
            AddDeckBlock(anchor, "Anchor Left Fluke", new Vector3(-0.24f, 0.006f, -0.34f), new Vector3(0.42f, 0.026f, 0.06f), Quaternion.Euler(0f, 35f, 0f), ink);
            AddDeckBlock(anchor, "Anchor Right Fluke", new Vector3(0.24f, 0.006f, -0.34f), new Vector3(0.42f, 0.026f, 0.06f), Quaternion.Euler(0f, -35f, 0f), ink);
            AddDeckRing(anchor, "Anchor Top Ring", new Vector3(0f, 0.008f, 0.49f), Vector3.one, ink, 0.28f, 0.28f, 0.18f, 0.18f);
            AddDeckLabel(anchor, "ANCHOR", new Vector3(0f, 0.04f, -0.72f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.Anchor, anchor);

            var patch = new GameObject("Hull Patch Asset").transform;
            patch.SetParent(deckRoot, false);
            patch.localPosition = new Vector3(7.08f, 0.068f, -0.08f);
            AddDeckBlock(patch, "Patch Cloth", Vector3.zero, new Vector3(0.98f, 0.03f, 0.56f), Quaternion.Euler(0f, -8f, 0f), celadon);
            AddDeckBlock(patch, "Patch Thread A", Vector3.zero, new Vector3(0.84f, 0.018f, 0.035f), Quaternion.Euler(0f, 16f, 0f), ink);
            AddDeckBlock(patch, "Patch Thread B", Vector3.zero, new Vector3(0.035f, 0.018f, 0.48f), Quaternion.Euler(0f, -8f, 0f), ink);
            AddDeckLabel(patch, "REPAIR", new Vector3(0f, 0.042f, -0.58f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.HullPatch, patch);

            var harbor = new GameObject("Harbor Asset").transform;
            harbor.SetParent(deckRoot, false);
            harbor.localPosition = new Vector3(7.08f, 0.072f, -1.72f);
            AddDeckBlock(harbor, "Lighthouse Base", new Vector3(0f, 0f, -0.18f), new Vector3(0.52f, 0.11f, 0.32f), Quaternion.identity, parchment);
            AddDeckBlock(harbor, "Lighthouse Tower", new Vector3(0f, 0.05f, 0.14f), new Vector3(0.35f, 0.22f, 0.58f), Quaternion.identity, parchment);
            AddDeckBlock(harbor, "Lighthouse Cap", new Vector3(0f, 0.17f, 0.48f), new Vector3(0.58f, 0.06f, 0.08f), Quaternion.identity, ink);
            AddDeckBlock(harbor, "Harbor Beam", new Vector3(-0.44f, 0.065f, 0.38f), new Vector3(0.74f, 0.018f, 0.05f), Quaternion.Euler(0f, -16f, 0f), seaBlue);
            AddDeckLabel(harbor, "HARBOR", new Vector3(0f, 0.058f, -0.72f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.Harbor, harbor);

            var storm = new GameObject("Storm Hazard Asset").transform;
            storm.SetParent(deckRoot, false);
            storm.localPosition = new Vector3(-7.02f, 0.076f, -1.94f);
            AddDeckRing(storm, "Storm Cloud A", new Vector3(-0.22f, 0f, 0.04f), new Vector3(0.82f, 1f, 0.56f), warning, 0.48f, 0.34f, 0.02f, 0.02f);
            AddDeckRing(storm, "Storm Cloud B", new Vector3(0.18f, 0.004f, 0.05f), new Vector3(0.78f, 1f, 0.5f), seaBlue, 0.44f, 0.32f, 0.02f, 0.02f);
            AddDeckBlock(storm, "Storm Slash A", new Vector3(-0.18f, 0.038f, -0.34f), new Vector3(0.52f, 0.018f, 0.055f), Quaternion.Euler(0f, -32f, 0f), ink);
            AddDeckBlock(storm, "Storm Slash B", new Vector3(0.24f, 0.042f, -0.18f), new Vector3(0.42f, 0.018f, 0.055f), Quaternion.Euler(0f, -32f, 0f), ink);
            AddDeckLabel(storm, "HAZARD", new Vector3(0f, 0.05f, -0.7f), 0.15f);
            RegisterDeckAsset(DeckAssetKind.Storm, storm);
        }

        private void CreateCrewResourceStations(
            Material ink,
            Material parchment,
            Material sailWash,
            Material cargoWash,
            Material celadon,
            Material seaBlue)
        {
            resourceStations.Clear();
            resourceStationCountTexts.Clear();

            var positions = new[]
            {
                new Vector3(-4.1f, 0.086f, 2.82f),
                new Vector3(-2.45f, 0.086f, 2.82f),
                new Vector3(-0.78f, 0.086f, 2.82f),
                new Vector3(0.9f, 0.086f, 2.82f),
                new Vector3(2.58f, 0.086f, 2.82f),
                new Vector3(4.18f, 0.086f, 2.82f)
            };

            for (var face = 1; face <= 6; face += 1)
            {
                CreateCrewResourceStation(face, positions[face - 1], ink, parchment, sailWash, cargoWash, celadon, seaBlue);
            }
        }

        private void CreateCrewResourceStation(
            int face,
            Vector3 localPosition,
            Material ink,
            Material parchment,
            Material sailWash,
            Material cargoWash,
            Material celadon,
            Material seaBlue)
        {
            var root = new GameObject($"{face} {YachtRushRules.CrewResourceName(face)} Resource Station").transform;
            root.SetParent(deckRoot, false);
            root.localPosition = localPosition;

            var faceColor = VoyageFaceColor(face);
            var wash = CreateMaterial($"Resource {face} Wash", new Color(faceColor.r, faceColor.g, faceColor.b, 0.32f), 0.4f);
            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = "Crew Resource Plaque";
            plaque.transform.SetParent(root, false);
            plaque.transform.localPosition = Vector3.zero;
            plaque.transform.localScale = new Vector3(1.38f, 0.035f, 0.62f);
            plaque.GetComponent<Renderer>().material = wash;
            Destroy(plaque.GetComponent<Collider>());

            AddDeckBlock(root, "Resource Ink Base", new Vector3(0f, 0.035f, -0.31f), new Vector3(1.18f, 0.018f, 0.04f), Quaternion.identity, ink);
            AddDeckBlock(root, "Resource Accent", new Vector3(-0.58f, 0.04f, 0f), new Vector3(0.045f, 0.02f, 0.5f), Quaternion.identity, CreateMaterial($"Resource {face} Accent", faceColor, 0.48f));
            AddResourceStationIcon(root, face, ink, parchment, sailWash, cargoWash, celadon, seaBlue);

            var title = CreateCommandTokenText(
                root,
                "Resource Title",
                $"{face} {ResourceCode(face)}",
                new Vector3(-0.2f, 0.066f, 0.1f),
                0.046f,
                SketchPalette.Ink,
                TextAnchor.MiddleCenter);
            title.alignment = TextAlignment.Center;

            var count = CreateCommandTokenText(
                root,
                "Resource Count",
                "0",
                new Vector3(0.42f, 0.068f, -0.16f),
                0.046f,
                SketchPalette.MutedInk,
                TextAnchor.MiddleCenter);
            count.gameObject.SetActive(false);
            resourceStationCountTexts[face] = count;
            resourceStations[face] = new DeckAssetView(root);
        }

        private void AddResourceStationIcon(
            Transform root,
            int face,
            Material ink,
            Material parchment,
            Material sailWash,
            Material cargoWash,
            Material celadon,
            Material seaBlue)
        {
            var iconRoot = new GameObject("Resource Icon").transform;
            iconRoot.SetParent(root, false);
            iconRoot.localPosition = new Vector3(-0.46f, 0.05f, -0.03f);
            iconRoot.localScale = Vector3.one * 0.7f;

            switch (face)
            {
                case 1:
                    AddDeckBlock(iconRoot, "Wind Stroke A", new Vector3(-0.08f, 0f, 0.12f), new Vector3(0.4f, 0.018f, 0.035f), Quaternion.Euler(0f, -18f, 0f), sailWash);
                    AddDeckBlock(iconRoot, "Wind Stroke B", new Vector3(0.04f, 0.012f, -0.06f), new Vector3(0.5f, 0.018f, 0.035f), Quaternion.Euler(0f, -18f, 0f), ink);
                    break;
                case 2:
                    AddDeckBlock(iconRoot, "Hull Plank", Vector3.zero, new Vector3(0.52f, 0.08f, 0.24f), Quaternion.Euler(0f, -8f, 0f), celadon);
                    AddDeckBlock(iconRoot, "Hull Stitch", Vector3.zero, new Vector3(0.04f, 0.018f, 0.34f), Quaternion.Euler(0f, -8f, 0f), ink);
                    break;
                case 3:
                    AddDeckBlock(iconRoot, "Supply Crate", Vector3.zero, new Vector3(0.36f, 0.1f, 0.26f), Quaternion.Euler(0f, -8f, 0f), cargoWash);
                    AddDeckBlock(iconRoot, "Supply Strap", Vector3.zero, new Vector3(0.34f, 0.018f, 0.035f), Quaternion.Euler(0f, -8f, 0f), ink);
                    break;
                case 4:
                    AddDeckRing(iconRoot, "Crew Head A", new Vector3(-0.12f, 0.022f, 0.08f), Vector3.one, seaBlue, 0.12f, 0.12f, 0.04f, 0.04f);
                    AddDeckRing(iconRoot, "Crew Head B", new Vector3(0.1f, 0.022f, 0.08f), Vector3.one, seaBlue, 0.12f, 0.12f, 0.04f, 0.04f);
                    AddDeckBlock(iconRoot, "Crew Bench", new Vector3(0f, 0.026f, -0.12f), new Vector3(0.42f, 0.02f, 0.09f), Quaternion.identity, seaBlue);
                    break;
                case 5:
                    AddDeckBlock(iconRoot, "Trade Crate", new Vector3(-0.1f, 0f, -0.02f), new Vector3(0.3f, 0.1f, 0.24f), Quaternion.Euler(0f, -8f, 0f), seaBlue);
                    AddDeckRing(iconRoot, "Trade Coin", new Vector3(0.18f, 0.025f, 0.08f), Vector3.one, ink, 0.12f, 0.12f, 0.06f, 0.06f);
                    break;
                case 6:
                    AddDeckRing(iconRoot, "Chart Compass", Vector3.zero, Vector3.one, parchment, 0.24f, 0.24f, 0.1f, 0.1f);
                    AddDeckBlock(iconRoot, "Chart Needle", Vector3.zero, new Vector3(0.04f, 0.018f, 0.44f), Quaternion.Euler(0f, -28f, 0f), ink);
                    break;
            }
        }

        private void RegisterDeckAsset(DeckAssetKind kind, Transform root)
        {
            deckAssets[kind] = new DeckAssetView(root);
            root.gameObject.SetActive(false);
        }

        private void CreateCommandCouncilTokens()
        {
            commandTokens.Clear();
            commandTokenColliders.Clear();

            var placements = new[]
            {
                new CommandTokenPlacement(YachtRushCategory.Ones, new Vector3(-5.25f, 0.18f, 5.2f), DeckAssetKind.Sail),
                new CommandTokenPlacement(YachtRushCategory.Twos, new Vector3(-1.75f, 0.18f, 5.2f), DeckAssetKind.Cargo),
                new CommandTokenPlacement(YachtRushCategory.Threes, new Vector3(1.75f, 0.18f, 5.2f), DeckAssetKind.HullPatch),
                new CommandTokenPlacement(YachtRushCategory.Fours, new Vector3(5.25f, 0.18f, 5.2f), DeckAssetKind.Anchor),
                new CommandTokenPlacement(YachtRushCategory.Fives, new Vector3(8.85f, 0.18f, 2.85f), DeckAssetKind.Harbor),
                new CommandTokenPlacement(YachtRushCategory.Sixes, new Vector3(8.85f, 0.18f, 0.92f), DeckAssetKind.Compass),
                new CommandTokenPlacement(YachtRushCategory.FourOfAKind, new Vector3(8.85f, 0.18f, -1.02f), DeckAssetKind.Sail),
                new CommandTokenPlacement(YachtRushCategory.FullHouse, new Vector3(8.85f, 0.18f, -2.95f), DeckAssetKind.HullPatch),
                new CommandTokenPlacement(YachtRushCategory.SmallStraight, new Vector3(-5.25f, 0.18f, -6.34f), DeckAssetKind.Cargo),
                new CommandTokenPlacement(YachtRushCategory.LargeStraight, new Vector3(-1.75f, 0.18f, -6.34f), DeckAssetKind.Harbor),
                new CommandTokenPlacement(YachtRushCategory.Yacht, new Vector3(1.75f, 0.18f, -6.34f), DeckAssetKind.Compass),
                new CommandTokenPlacement(YachtRushCategory.Chance, new Vector3(5.25f, 0.18f, -6.34f), DeckAssetKind.Anchor)
            };

            foreach (var placement in placements)
            {
                commandTokens[placement.Category] = CreateCommandToken(placement.Category, placement.Position, placement.Kind);
            }
        }

        private CommandTokenView CreateCommandToken(YachtRushCategory category, Vector3 position, DeckAssetKind kind)
        {
            var strategy = StrategyForTokenCategory(category);
            var preview = YachtRushRules.PreviewVoyageStrategy(strategy, new int[6]);
            var root = new GameObject(preview.Name + " Strategy Token").transform;
            root.SetParent(tableRoot, false);
            root.localPosition = position;

            var baseMaterial = CreateMaterial(preview.Name + " Token Face", new Color32(248, 244, 231, 255), 0.5f);
            var inkMaterial = CreateMaterial(preview.Name + " Token Ink", new Color32(45, 37, 29, 255), 0.62f);
            var railMaterial = CreateMaterial(preview.Name + " Token Rail", new Color32(68, 53, 36, 235), 0.5f);
            var accentMaterial = CreateMaterial(preview.Name + " Token Accent", new Color32(250, 246, 232, 255), 0.48f);
            var statusMaterial = CreateMaterial(preview.Name + " Token Status", new Color32(136, 129, 113, 255), 0.48f);

            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = preview.Name + " Strategy Plaque";
            plaque.transform.SetParent(root, false);
            plaque.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            plaque.transform.localScale = new Vector3(2.36f, 0.08f, 1.28f);
            var background = plaque.GetComponent<Renderer>();
            background.material = baseMaterial;
            var collider = plaque.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.08f, 5.6f, 1.26f);
            commandTokenColliders[collider] = category;

            var hitbox = new GameObject(preview.Name + " Token Hitbox", typeof(BoxCollider));
            hitbox.transform.SetParent(root, false);
            hitbox.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            var hitboxCollider = hitbox.GetComponent<BoxCollider>();
            hitboxCollider.isTrigger = true;
            hitboxCollider.size = new Vector3(3.45f, 0.32f, 1.42f);
            commandTokenColliders[hitboxCollider] = category;

            Renderer iconBacking = null;
            var statusStrip = AddDeckBlock(root, preview.Name + " Token Status Strip", new Vector3(0.5f, 0.076f, -0.52f), new Vector3(2.18f, 0.024f, 0.06f), Quaternion.identity, statusMaterial);
            AddDeckBlock(root, preview.Name + " Token Top Rail", new Vector3(0.5f, 0.067f, 0.66f), new Vector3(2.34f, 0.024f, 0.045f), Quaternion.identity, railMaterial);
            AddDeckBlock(root, preview.Name + " Token Bottom Rail", new Vector3(0.5f, 0.067f, -0.66f), new Vector3(2.34f, 0.024f, 0.045f), Quaternion.identity, railMaterial);
            AddStrategyTokenIcon(root, strategy, accentMaterial, inkMaterial);

            var name = CreateCommandTokenText(root, "Name", ShortStrategyName(preview.Name), new Vector3(0.5f, 0.112f, 0.1f), 0.06f, SketchPalette.Ink, TextAnchor.MiddleCenter);
            var detail = CreateCommandTokenText(root, "Detail", string.Empty, new Vector3(0.5f, 0.114f, -0.24f), 0.033f, new Color32(84, 70, 52, 255), TextAnchor.MiddleCenter);
            var value = CreateCommandTokenText(root, "Value", string.Empty, new Vector3(1.28f, 0.116f, -0.02f), 0.064f, SketchPalette.Ink, TextAnchor.MiddleCenter);
            var tag = CreateCommandTokenText(root, "Tag", "LOCKED", new Vector3(0.5f, 0.118f, -0.34f), 0.029f, new Color32(62, 110, 65, 255), TextAnchor.MiddleCenter);

            return new CommandTokenView(root, background, iconBacking, statusStrip, name, detail, value, tag);
        }

        private void AddStrategyTokenIcon(Transform parent, VoyageStrategy strategy, Material accentMaterial, Material inkMaterial)
        {
            if (AddGeneratedStrategyTokenIcon(parent, strategy))
            {
                return;
            }

            var iconRoot = new GameObject("Strategy Token Mark").transform;
            iconRoot.SetParent(parent, false);
            iconRoot.localPosition = new Vector3(-1.26f, 0.108f, -0.02f);
            iconRoot.localScale = Vector3.one * 1.04f;

            switch (strategy)
            {
                case VoyageStrategy.TailwindRun:
                case VoyageStrategy.LongVoyage:
                    AddDeckTriangle(iconRoot, "Strategy Sail", new Vector3(0.08f, 0.014f, 0.08f), new Vector3(0.36f, 1f, 0.46f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Strategy Mast", new Vector3(-0.06f, 0.02f, 0f), new Vector3(0.035f, 0.024f, 0.58f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Strategy Route", new Vector3(0.16f, 0.048f, -0.3f), new Vector3(0.44f, 0.018f, 0.035f), Quaternion.Euler(0f, -16f, 0f), inkMaterial);
                    break;
                case VoyageStrategy.PatchTheHull:
                case VoyageStrategy.RepairConvoy:
                    AddDeckBlock(iconRoot, "Strategy Patch", Vector3.zero, new Vector3(0.52f, 0.1f, 0.3f), Quaternion.Euler(0f, -12f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Strategy Patch Stitch A", new Vector3(-0.16f, 0.064f, 0.02f), new Vector3(0.035f, 0.018f, 0.34f), Quaternion.Euler(0f, 8f, 0f), inkMaterial);
                    AddDeckBlock(iconRoot, "Strategy Patch Stitch B", new Vector3(0.16f, 0.064f, -0.02f), new Vector3(0.035f, 0.018f, 0.34f), Quaternion.Euler(0f, 8f, 0f), inkMaterial);
                    break;
                case VoyageStrategy.StockTheHold:
                    AddDeckBlock(iconRoot, "Strategy Crate A", new Vector3(-0.12f, 0f, 0.04f), new Vector3(0.36f, 0.12f, 0.26f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Strategy Crate B", new Vector3(0.16f, 0.012f, -0.08f), new Vector3(0.32f, 0.1f, 0.22f), Quaternion.Euler(0f, 9f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Strategy Crate Strap", new Vector3(0.02f, 0.075f, 0f), new Vector3(0.48f, 0.018f, 0.035f), Quaternion.Euler(0f, 18f, 0f), inkMaterial);
                    break;
                case VoyageStrategy.RallyTheCrew:
                    AddDeckRing(iconRoot, "Strategy Crew A", new Vector3(-0.16f, 0.034f, 0.13f), Vector3.one * 0.38f, inkMaterial, 0.13f, 0.13f, 0.045f, 0.045f);
                    AddDeckRing(iconRoot, "Strategy Crew B", new Vector3(0.12f, 0.034f, 0.13f), Vector3.one * 0.38f, inkMaterial, 0.13f, 0.13f, 0.045f, 0.045f);
                    AddDeckBlock(iconRoot, "Strategy Crew Table", new Vector3(0f, 0.014f, -0.12f), new Vector3(0.54f, 0.03f, 0.2f), Quaternion.identity, accentMaterial);
                    break;
                case VoyageStrategy.PortBargain:
                case VoyageStrategy.TradeRoute:
                    AddDeckBlock(iconRoot, "Strategy Dock", new Vector3(0.12f, 0.018f, -0.18f), new Vector3(0.5f, 0.028f, 0.05f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Strategy Cargo", new Vector3(-0.12f, 0f, 0.04f), new Vector3(0.32f, 0.11f, 0.24f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckRing(iconRoot, "Strategy Coin", new Vector3(0.22f, 0.035f, 0.12f), Vector3.one * 0.44f, inkMaterial, 0.12f, 0.12f, 0.055f, 0.055f);
                    break;
                case VoyageStrategy.ReadTheStars:
                case VoyageStrategy.SafePassage:
                    AddDeckRing(iconRoot, "Strategy Compass Ring", Vector3.zero, Vector3.one * 0.72f, inkMaterial, 0.25f, 0.25f, 0.14f, 0.14f);
                    AddDeckBlock(iconRoot, "Strategy Compass Needle", Vector3.zero, new Vector3(0.42f, 0.018f, 0.035f), Quaternion.Euler(0f, 35f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Strategy Safe Line", new Vector3(0.1f, 0.054f, -0.28f), new Vector3(0.56f, 0.016f, 0.03f), Quaternion.Euler(0f, -12f, 0f), inkMaterial);
                    break;
                case VoyageStrategy.FullDeck:
                    for (var index = 0; index < 5; index += 1)
                    {
                        AddDeckBlock(iconRoot, $"Strategy Full Deck {index + 1}", new Vector3(-0.24f + index * 0.12f, 0.012f, Mathf.Sin(index) * 0.13f), new Vector3(0.1f, 0.06f, 0.16f), Quaternion.Euler(0f, index * 11f, 0f), accentMaterial);
                    }

                    AddDeckRing(iconRoot, "Strategy Full Compass", new Vector3(0.26f, 0.04f, 0.02f), Vector3.one * 0.36f, inkMaterial, 0.1f, 0.1f, 0.05f, 0.05f);
                    break;
                case VoyageStrategy.CaptainsGambit:
                    AddDeckRing(iconRoot, "Strategy Wheel", Vector3.zero, Vector3.one * 0.82f, inkMaterial, 0.28f, 0.28f, 0.18f, 0.18f);
                    AddDeckBlock(iconRoot, "Strategy Wheel Spoke A", Vector3.zero, new Vector3(0.5f, 0.018f, 0.035f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Strategy Wheel Spoke B", Vector3.zero, new Vector3(0.035f, 0.018f, 0.5f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Strategy Bold Mark", new Vector3(0.28f, 0.028f, -0.24f), new Vector3(0.28f, 0.018f, 0.04f), Quaternion.Euler(0f, -18f, 0f), accentMaterial);
                    break;
                default:
                    AddDeckBlock(iconRoot, "Strategy Block", Vector3.zero, new Vector3(0.38f, 0.1f, 0.28f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    break;
            }
        }

        private bool AddGeneratedStrategyTokenIcon(Transform parent, VoyageStrategy strategy)
        {
            var texture = Resources.Load<Texture2D>($"StrategyTokens/{StrategyTokenAssetName(strategy)}");
            if (texture == null)
            {
                return false;
            }

            var icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            icon.name = "Generated Strategy Token Art";
            icon.transform.SetParent(parent, false);
            icon.transform.localPosition = new Vector3(-1.28f, 0.122f, 0f);
            icon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            icon.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            var collider = icon.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            icon.GetComponent<Renderer>().material = CreateTextureMaterial($"Token Art {strategy}", texture);
            return true;
        }

        private static string StrategyTokenAssetName(VoyageStrategy strategy)
        {
            switch (strategy)
            {
                case VoyageStrategy.TailwindRun:
                    return "tailwind_run";
                case VoyageStrategy.StockTheHold:
                    return "stock_the_hold";
                case VoyageStrategy.PatchTheHull:
                    return "patch_the_hull";
                case VoyageStrategy.RallyTheCrew:
                    return "rally_the_crew";
                case VoyageStrategy.PortBargain:
                    return "port_bargain";
                case VoyageStrategy.ReadTheStars:
                    return "read_the_stars";
                case VoyageStrategy.SafePassage:
                    return "safe_passage";
                case VoyageStrategy.RepairConvoy:
                    return "repair_convoy";
                case VoyageStrategy.LongVoyage:
                    return "long_voyage";
                case VoyageStrategy.TradeRoute:
                    return "trade_route";
                case VoyageStrategy.FullDeck:
                    return "full_deck";
                case VoyageStrategy.CaptainsGambit:
                    return "captains_gambit";
                default:
                    return "captains_gambit";
            }
        }

        private void AddCommandTokenIcon(Transform parent, YachtRushCategory category, DeckAssetKind kind, Material accentMaterial, Material inkMaterial)
        {
            var iconRoot = new GameObject("Command Icon").transform;
            iconRoot.SetParent(parent, false);
            iconRoot.localPosition = new Vector3(-1.24f, 0.096f, -0.02f);
            iconRoot.localScale = Vector3.one * 1.24f;

            switch (category)
            {
                case YachtRushCategory.Ones:
                    AddDeckTriangle(iconRoot, "Tiny Sail", new Vector3(0.08f, 0.014f, 0.06f), new Vector3(0.34f, 1f, 0.42f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Tiny Mast", new Vector3(-0.04f, 0.018f, 0f), new Vector3(0.035f, 0.024f, 0.5f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Tailwind Line", new Vector3(0.18f, 0.028f, -0.26f), new Vector3(0.34f, 0.018f, 0.035f), Quaternion.Euler(0f, -12f, 0f), inkMaterial);
                    break;
                case YachtRushCategory.Twos:
                    AddDeckBlock(iconRoot, "Trade Crate", new Vector3(-0.08f, 0f, 0f), new Vector3(0.32f, 0.11f, 0.24f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckRing(iconRoot, "Trade Coin A", new Vector3(0.2f, 0.03f, 0.14f), Vector3.one * 0.46f, inkMaterial, 0.13f, 0.13f, 0.06f, 0.06f);
                    AddDeckRing(iconRoot, "Trade Coin B", new Vector3(0.32f, 0.035f, -0.05f), Vector3.one * 0.42f, inkMaterial, 0.12f, 0.12f, 0.055f, 0.055f);
                    break;
                case YachtRushCategory.Threes:
                    AddDeckBlock(iconRoot, "Hull Patch", new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.1f, 0.3f), Quaternion.Euler(0f, -12f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Patch Stitch A", new Vector3(-0.16f, 0.064f, 0.02f), new Vector3(0.035f, 0.018f, 0.34f), Quaternion.Euler(0f, 8f, 0f), inkMaterial);
                    AddDeckBlock(iconRoot, "Patch Stitch B", new Vector3(0.16f, 0.064f, -0.02f), new Vector3(0.035f, 0.018f, 0.34f), Quaternion.Euler(0f, 8f, 0f), inkMaterial);
                    break;
                case YachtRushCategory.Fours:
                    AddDeckTriangle(iconRoot, "Full Sail Cloth", new Vector3(0.08f, 0.014f, 0.04f), new Vector3(0.42f, 1f, 0.5f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Full Sail Mast", new Vector3(-0.08f, 0.02f, 0f), new Vector3(0.035f, 0.024f, 0.58f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Sail Gust", new Vector3(0.2f, 0.046f, -0.3f), new Vector3(0.42f, 0.016f, 0.035f), Quaternion.Euler(0f, -16f, 0f), inkMaterial);
                    break;
                case YachtRushCategory.Fives:
                    AddDeckBlock(iconRoot, "Trade Crate Main", new Vector3(-0.12f, 0f, 0.02f), new Vector3(0.34f, 0.11f, 0.24f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Harbor Pier Tiny", new Vector3(0.22f, 0.018f, -0.18f), new Vector3(0.42f, 0.028f, 0.05f), Quaternion.identity, inkMaterial);
                    AddDeckRing(iconRoot, "Trade Coin", new Vector3(0.22f, 0.035f, 0.12f), Vector3.one * 0.44f, inkMaterial, 0.12f, 0.12f, 0.055f, 0.055f);
                    break;
                case YachtRushCategory.Sixes:
                    AddDeckRing(iconRoot, "Crew Vote Head A", new Vector3(-0.16f, 0.034f, 0.12f), Vector3.one * 0.38f, inkMaterial, 0.13f, 0.13f, 0.045f, 0.045f);
                    AddDeckRing(iconRoot, "Crew Vote Head B", new Vector3(0.12f, 0.034f, 0.12f), Vector3.one * 0.38f, inkMaterial, 0.13f, 0.13f, 0.045f, 0.045f);
                    AddDeckBlock(iconRoot, "Crew Vote Table", new Vector3(0f, 0.014f, -0.12f), new Vector3(0.52f, 0.03f, 0.2f), Quaternion.identity, accentMaterial);
                    break;
                case YachtRushCategory.FourOfAKind:
                    AddDeckRing(iconRoot, "Tiny Storm Cloud", Vector3.zero, Vector3.one * 0.78f, accentMaterial, 0.34f, 0.22f, 0.02f, 0.02f);
                    AddDeckBlock(iconRoot, "Tiny Storm Slash", new Vector3(0f, 0.038f, -0.22f), new Vector3(0.36f, 0.018f, 0.044f), Quaternion.Euler(0f, -32f, 0f), inkMaterial);
                    AddDeckBlock(iconRoot, "Breakwater Bar", new Vector3(0f, 0.05f, 0.25f), new Vector3(0.46f, 0.018f, 0.04f), Quaternion.identity, inkMaterial);
                    break;
                case YachtRushCategory.FullHouse:
                    AddDeckBlock(iconRoot, "Supply Chain Crate", new Vector3(-0.18f, 0f, 0.1f), new Vector3(0.28f, 0.11f, 0.22f), Quaternion.Euler(0f, 8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Supply Chain Patch", new Vector3(0.15f, 0.006f, -0.08f), new Vector3(0.3f, 0.09f, 0.22f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Supply Chain Link", new Vector3(0f, 0.075f, 0.02f), new Vector3(0.5f, 0.018f, 0.034f), Quaternion.Euler(0f, 20f, 0f), inkMaterial);
                    break;
                case YachtRushCategory.SmallStraight:
                    AddDeckRing(iconRoot, "Safe Compass", new Vector3(-0.12f, 0.02f, 0.08f), Vector3.one * 0.54f, inkMaterial, 0.18f, 0.18f, 0.1f, 0.1f);
                    AddDeckBlock(iconRoot, "Safe Patch", new Vector3(0.2f, 0f, -0.1f), new Vector3(0.28f, 0.09f, 0.22f), Quaternion.Euler(0f, -10f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Safe Route", new Vector3(0.02f, 0.054f, -0.26f), new Vector3(0.58f, 0.016f, 0.03f), Quaternion.Euler(0f, -12f, 0f), inkMaterial);
                    break;
                case YachtRushCategory.LargeStraight:
                    AddDeckTriangle(iconRoot, "Full Sail A", new Vector3(-0.08f, 0.014f, 0.06f), new Vector3(0.34f, 1f, 0.42f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckTriangle(iconRoot, "Full Sail B", new Vector3(0.18f, 0.018f, -0.06f), new Vector3(0.28f, 1f, 0.34f), Quaternion.Euler(0f, 14f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Full Sail Mast", new Vector3(-0.05f, 0.02f, 0f), new Vector3(0.035f, 0.024f, 0.56f), Quaternion.identity, inkMaterial);
                    break;
                case YachtRushCategory.Yacht:
                    AddDeckTriangle(iconRoot, "Grand Voyage Sail", new Vector3(-0.1f, 0.014f, 0.1f), new Vector3(0.34f, 1f, 0.42f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Grand Route", new Vector3(0.12f, 0.05f, -0.18f), new Vector3(0.58f, 0.018f, 0.04f), Quaternion.Euler(0f, -12f, 0f), inkMaterial);
                    AddDeckBlock(iconRoot, "Grand Harbor", new Vector3(0.32f, 0.028f, 0.14f), new Vector3(0.16f, 0.08f, 0.22f), Quaternion.identity, accentMaterial);
                    break;
                case YachtRushCategory.Chance:
                    AddDeckRing(iconRoot, "Captain Wheel", Vector3.zero, Vector3.one * 0.82f, inkMaterial, 0.28f, 0.28f, 0.18f, 0.18f);
                    AddDeckBlock(iconRoot, "Wheel Spoke A", Vector3.zero, new Vector3(0.5f, 0.018f, 0.035f), Quaternion.identity, inkMaterial);
                    AddDeckBlock(iconRoot, "Wheel Spoke B", Vector3.zero, new Vector3(0.035f, 0.018f, 0.5f), Quaternion.identity, inkMaterial);
                    break;
                default:
                    AddDeckBlock(iconRoot, "Tiny Cargo", Vector3.zero, new Vector3(0.34f, 0.12f, 0.28f), Quaternion.Euler(0f, -8f, 0f), accentMaterial);
                    AddDeckBlock(iconRoot, "Tiny Cargo Strap", Vector3.zero, new Vector3(0.32f, 0.018f, 0.035f), Quaternion.Euler(0f, -8f, 0f), inkMaterial);
                    break;
            }
        }

        private TextMesh CreateCommandTokenText(Transform parent, string name, string value, Vector3 localPosition, float characterSize, Color color, TextAnchor anchor)
        {
            var label = new GameObject(name, typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = label.GetComponent<TextMesh>();
            text.text = value;
            text.fontSize = 48;
            text.characterSize = characterSize;
            text.anchor = anchor;
            text.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
            text.color = color;
            return text;
        }

        private Renderer AddDeckBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            return AddDeckBlock(parent, name, localPosition, localScale, localRotation, CreateMaterial(name + " Material", color, 0.48f));
        }

        private Renderer AddDeckBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            block.transform.localRotation = localRotation;
            var renderer = block.GetComponent<Renderer>();
            renderer.material = material;
            Destroy(block.GetComponent<Collider>());
            return renderer;
        }

        private void AddDeckRing(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, float outerX, float outerZ, float innerX, float innerZ)
        {
            var ring = CreateOvalRing(name, material, outerX, outerZ, innerX, innerZ);
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = localPosition;
            ring.transform.localScale = localScale;
        }

        private void AddDeckTriangle(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            var triangle = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            triangle.transform.SetParent(parent, false);
            triangle.transform.localPosition = localPosition;
            triangle.transform.localScale = localScale;
            triangle.transform.localRotation = localRotation;
            triangle.GetComponent<MeshFilter>().mesh = CreateTriangleMesh(name + " Mesh");
            triangle.GetComponent<MeshRenderer>().material = material;
        }

        private void AddDeckLabel(Transform parent, string value, Vector3 localPosition, float size)
        {
            var label = new GameObject(value + " Deck Label", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = label.GetComponent<TextMesh>();
            text.text = string.Empty;
            text.fontSize = 32;
            text.characterSize = size * 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color32(64, 55, 45, 90);
        }

        private void CreateVoyageZone(VoyageDeckZone zone, string title, string subtitle, Vector3 localPosition, Vector3 localScale, Color fill)
        {
            var root = new GameObject(title + " Landing Zone").transform;
            root.SetParent(deckRoot, false);
            root.localPosition = Vector3.zero;

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = title + " Zone Wash";
            panel.transform.SetParent(root, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localScale = localScale;
            var renderer = panel.GetComponent<Renderer>();
            renderer.material = CreateMaterial(title + " Zone Material", fill, 0.34f);
            voyageZoneRenderers[zone] = renderer;
            Destroy(panel.GetComponent<Collider>());

            AddDeckBlock(root, title + " Zone Left Ink", localPosition + new Vector3(-localScale.x * 0.52f, 0.012f, 0f), new Vector3(0.04f, 0.014f, localScale.z * 0.94f), Quaternion.identity, new Color32(59, 50, 40, 155));
            AddDeckBlock(root, title + " Zone Right Ink", localPosition + new Vector3(localScale.x * 0.52f, 0.012f, 0f), new Vector3(0.04f, 0.014f, localScale.z * 0.94f), Quaternion.identity, new Color32(59, 50, 40, 88));
            AddZoneText(root, title, localPosition + new Vector3(0f, 0.04f, 1.9f), 0.048f, SketchPalette.Ink);
            AddZoneText(root, subtitle, localPosition + new Vector3(0f, 0.045f, 1.62f), 0.032f, new Color32(77, 67, 56, 190));
        }

        private void AddZoneText(Transform parent, string value, Vector3 localPosition, float characterSize, Color color)
        {
            var label = new GameObject(value + " Zone Text", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = label.GetComponent<TextMesh>();
            text.text = value;
            text.fontSize = 48;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Yacht Sailing UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var root = SketchUiFactory.CreateSafeAreaRoot(canvas.transform, "Safe Area");
            var topStrip = CreatePanel(root, "Run Stats Strip", Anchor.TopStretch, new Vector2(44f, -124f), new Vector2(-44f, -18f), new Color32(255, 253, 246, 242));
            var statPanel = CreatePanel(topStrip, "Stats", Anchor.Stretch, new Vector2(14f, 12f), new Vector2(-14f, -12f), new Color32(255, 250, 236, 244));
            var statLayout = statPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            statLayout.padding = new RectOffset(10, 10, 8, 8);
            statLayout.spacing = 12f;
            statLayout.childAlignment = TextAnchor.MiddleCenter;
            statLayout.childControlWidth = true;
            statLayout.childControlHeight = true;
            statLayout.childForceExpandWidth = true;
            statLayout.childForceExpandHeight = true;
            roundText = CreateStatText(statPanel, "Month");
            rollText = CreateStatText(statPanel, "Distance");
            bestText = CreateStatText(statPanel, "Best");

            var commandHelpButton = CreateButton(root, "Command Help Button", "?", Anchor.TopRight, new Vector2(-98f, -186f), new Vector2(-48f, -136f));
            commandHelpButton.onClick.AddListener(() => ShowCommandHelp(null));
            BuildCommandHelpModal(root);
            BuildResourceStockPanel(root);
            voyageStatusText = CreateText(root, "Voyage Status", "Roll dice, inspect a deck token, then choose.", 14, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(70f, 12f), new Vector2(-70f, 42f));
            voyageStatusText.alignment = TextAnchor.MiddleCenter;
            voyageStatusText.gameObject.SetActive(false);
            BuildCaptainOrderPanel(root);

            var twistPanel = CreatePanel(root, "Crew Council Banner", Anchor.TopStretch, new Vector2(40f, -154f), new Vector2(-40f, -104f), new Color32(235, 244, 224, 226));
            contractBackground = twistPanel.GetComponent<Image>();
            twistAccentBar = CreateImage(twistPanel, "Twist Accent Bar", Anchor.StretchLeft, new Vector2(0f, 0f), new Vector2(12f, 0f), new Color32(187, 126, 70, 255));
            contractStateText = CreateText(twistPanel, "Twist Type", "COUNCIL", 8, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopLeft, new Vector2(18f, -18f), new Vector2(118f, -4f));
            contractNameText = CreateText(twistPanel, "Twist Name", "Crew dice decides the course", 17, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(128f, -32f), new Vector2(-430f, -4f));
            contractNameText.alignment = TextAnchor.MiddleLeft;
            contractConditionText = CreateText(twistPanel, "Twist Effect", "Use the dice meanings to choose a token.", 10, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(128f, 4f), new Vector2(-430f, 20f));
            contractConditionText.alignment = TextAnchor.MiddleLeft;
            contractBonusText = CreateText(twistPanel, "Twist Badge", "DAY 1", 17, FontStyle.Bold, SketchPalette.Ink, Anchor.StretchRight, new Vector2(-410f, 8f), new Vector2(-320f, -8f));
            var routeTrack = CreateImage(twistPanel, "Harbor Route Track", Anchor.BottomStretch, new Vector2(18f, 6f), new Vector2(-430f, 10f), new Color32(70, 58, 45, 70));
            harborRouteFill = CreateImage(routeTrack.transform, "Harbor Route Fill", Anchor.StretchLeft, Vector2.zero, new Vector2(0f, 0f), new Color32(73, 130, 87, 210));
            harborRouteText = CreateText(twistPanel, "Harbor Route Text", "0 nm to Far Sea", 8, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(18f, 14f), new Vector2(118f, 30f));
            harborRouteText.alignment = TextAnchor.MiddleLeft;
            harborMapRect = CreatePanel(twistPanel, "Parchment Route Map", Anchor.StretchRight, new Vector2(-300f, 8f), new Vector2(-18f, -8f), new Color32(255, 250, 232, 210));
            var mapTrack = CreateImage(harborMapRect, "Map Ink Route", Anchor.BottomStretch, new Vector2(34f, 15f), new Vector2(-34f, 20f), new Color32(70, 58, 45, 110));
            harborMapTrackRect = mapTrack.rectTransform;
            harborMapRouteFill = CreateImage(mapTrack.transform, "Map Route Fill", Anchor.StretchLeft, Vector2.zero, Vector2.zero, new Color32(62, 110, 65, 225));
            harborStartPort = CreateImage(harborMapRect, "Start Port Dot", Anchor.BottomLeft, new Vector2(28f, 11f), new Vector2(46f, 29f), new Color32(74, 61, 48, 230));
            harborDestinationPort = CreateImage(harborMapRect, "Destination Harbor Dot", Anchor.BottomLeft, new Vector2(236f, 11f), new Vector2(260f, 35f), new Color32(74, 61, 48, 230));
            var marker = CreateImage(mapTrack.transform, "Yacht Marker", Anchor.Center, new Vector2(-11f, -8f), new Vector2(11f, 8f), new Color32(255, 248, 211, 255));
            harborYachtMarker = marker.rectTransform;
            CreateText(marker.transform, "Yacht Marker Icon", ">", 12, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, Vector2.zero, Vector2.zero);
            CreateText(harborMapRect, "Start Label", "PORT", 7, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomLeft, new Vector2(18f, 29f), new Vector2(70f, 43f));
            CreateText(harborMapRect, "End Label", "FAR SEA", 7, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomLeft, new Vector2(210f, 29f), new Vector2(282f, 43f));
            twistPanel.gameObject.SetActive(false);

            chooserTitleText = CreateText(root, "Turn Prompt", "Roll supplies", 21, FontStyle.Bold, SketchPalette.Ink, Anchor.TopRight, new Vector2(-580f, -198f), new Vector2(-48f, -162f));
            chooserTitleText.gameObject.SetActive(false);

            resultPanel = CreatePanel(root, "Result Panel", Anchor.Center, new Vector2(-260f, -150f), new Vector2(260f, 150f), new Color32(255, 253, 246, 248)).gameObject;
            resultTitleText = CreateText(resultPanel.transform, "Result Title", "Voyage Complete", 34, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(18f, -80f), new Vector2(-18f, -24f));
            resultScoreText = CreateText(resultPanel.transform, "Result Score", "0", 54, FontStyle.Bold, SketchPalette.Ink, Anchor.Center, new Vector2(-220f, -42f), new Vector2(220f, 48f));
            resultMetaText = CreateText(resultPanel.transform, "Result Meta", "Best 0", 18, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(24f, 82f), new Vector2(-24f, 116f));
            var againButton = CreateButton(resultPanel.transform, "Play Again", "Play Again", Anchor.BottomStretch, new Vector2(30f, 24f), new Vector2(-30f, 74f));
            againButton.onClick.AddListener(StartRun);

            rushIntroText = CreateText(root, "Hazard Intro", "STORM HAZARD!", 46, FontStyle.Bold, SketchPalette.Ink, Anchor.Center, new Vector2(-320f, 110f), new Vector2(320f, 210f));
            rushIntroText.gameObject.SetActive(false);
            harborFeedbackText = CreateText(root, "Harbor Change Toast", "Route +14", 25, FontStyle.Bold, SketchPalette.Ink, Anchor.Center, new Vector2(-360f, -90f), new Vector2(360f, -34f));
            harborFeedbackText.gameObject.SetActive(false);
            runGoalToastText = CreateText(root, "Run Goal Toast", "Plan a 12-Month Voyage", 28, FontStyle.Bold, SketchPalette.Ink, Anchor.Center, new Vector2(-390f, 244f), new Vector2(390f, 316f));
            runGoalToastText.text = "Roll resources. Pick a voyage strategy. Survive 12 months.";
            runGoalToastText.gameObject.SetActive(false);
        }

        private void BuildCaptainOrderPanel(Transform root)
        {
            captainOrderButtons.Clear();
            captainOrderButtonLabels.Clear();
            captainOrderArtPanels.Clear();
            captainOrderArtLabels.Clear();
            captainOrderPanel = CreatePanel(root, "Voyage Strategies", Anchor.BottomStretch, new Vector2(36f, 18f), new Vector2(-36f, 402f), new Color32(255, 251, 236, 248)).gameObject;
            var layout = captainOrderPanel.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = new Vector2(12f, 10f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.cellSize = new Vector2(246f, 112f);

            for (var index = 0; index < 12; index += 1)
            {
                var button = CreateButton(captainOrderPanel.transform, $"Voyage Strategy {index + 1}", string.Empty, Anchor.Stretch, Vector2.zero, Vector2.zero);
                var image = button.GetComponent<Image>();
                image.color = new Color32(250, 245, 230, 255);
                var artPanel = CreateImage(button.transform, "Resource Band", Anchor.TopStretch, new Vector2(8f, -22f), new Vector2(-8f, -8f), new Color32(96, 122, 92, 255));
                var artLabel = CreateText(artPanel.transform, "Resource Band Label", "SAIL PLAN", 9, FontStyle.Bold, Color.white, Anchor.Stretch, new Vector2(4f, 0f), new Vector2(-4f, 0f));
                artLabel.alignment = TextAnchor.MiddleCenter;
                artLabel.resizeTextMinSize = 7;

                var label = CreateText(button.transform, "Label", "Strategy", 14, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, new Vector2(12f, 8f), new Vector2(-12f, -28f));
                label.alignment = TextAnchor.MiddleLeft;
                label.supportRichText = true;
                label.resizeTextMinSize = 11;
                var captured = index;
                button.onClick.AddListener(() => ChooseCaptainOrder(captured));
                captainOrderButtons.Add(button);
                captainOrderButtonLabels.Add(label);
                captainOrderArtPanels.Add(artPanel);
                captainOrderArtLabels.Add(artLabel);
            }

            captainOrderPanel.SetActive(false);
        }

        private Text CreateStatText(Transform parent, string label)
        {
            var holder = CreatePanel(parent, label, Anchor.Stretch, Vector2.zero, Vector2.zero, new Color32(248, 245, 232, 255));
            var layout = holder.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 128f;
            layout.flexibleWidth = 1f;
            var labelText = CreateText(holder, $"{label} Label", label.ToUpperInvariant(), 9, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopStretch, new Vector2(8f, -26f), new Vector2(-8f, -8f));
            labelText.alignment = TextAnchor.MiddleCenter;
            var valueText = CreateText(holder, $"{label} Value", "0", 25, FontStyle.Bold, SketchPalette.Ink, Anchor.BottomStretch, new Vector2(8f, 8f), new Vector2(-8f, 58f));
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.resizeTextMinSize = 18;
            return valueText;
        }

        private void BuildResourceLegend(Transform root)
        {
            var legend = CreatePanel(root, "Crew Resource Key", Anchor.TopStretch, new Vector2(60f, -190f), new Vector2(-120f, -138f), new Color32(255, 250, 236, 238));
            var layout = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (var face = 1; face <= 6; face += 1)
            {
                var cell = CreatePanel(legend, $"Resource Key {face}", Anchor.Stretch, Vector2.zero, Vector2.zero, new Color(VoyageFaceColor(face).r, VoyageFaceColor(face).g, VoyageFaceColor(face).b, 0.22f));
                var cellLayout = cell.gameObject.AddComponent<LayoutElement>();
                cellLayout.minWidth = 116f;
                cellLayout.flexibleWidth = 1f;
                var text = CreateText(cell, $"Resource Key {face} Label", $"{face}  {YachtRushRules.CrewResourceName(face)}", 15, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, new Vector2(6f, 3f), new Vector2(-6f, -3f));
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextMinSize = 11;
            }
        }

        private void BuildCommandHelpModal(Transform root)
        {
            commandHelpPanel = CreatePanel(root, "Command Help Modal", Anchor.Center, new Vector2(-430f, -330f), new Vector2(430f, 330f), new Color32(255, 251, 236, 255)).gameObject;
            commandHelpTitleText = CreateText(commandHelpPanel.transform, "Command Help Title", "How to Play", 31, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(26f, -70f), new Vector2(-26f, -20f));
            commandHelpTitleText.alignment = TextAnchor.MiddleLeft;
            commandHelpBodyText = CreateText(commandHelpPanel.transform, "Command Help Body", CommandGuideText(null), 15, FontStyle.Bold, SketchPalette.MutedInk, Anchor.Stretch, new Vector2(28f, 92f), new Vector2(-28f, -90f));
            commandHelpBodyText.alignment = TextAnchor.UpperLeft;
            commandHelpBodyText.resizeTextMinSize = 10;
            commandHelpBodyText.lineSpacing = 0.92f;
            commandHelpChooseButton = CreateButton(commandHelpPanel.transform, "Command Help Choose", "Choose", Anchor.BottomRight, new Vector2(-330f, 24f), new Vector2(-186f, 74f));
            commandHelpChooseButton.onClick.AddListener(ConfirmStrategyFromHelp);
            var closeButton = CreateButton(commandHelpPanel.transform, "Command Help Close", "Close", Anchor.BottomRight, new Vector2(-172f, 24f), new Vector2(-28f, 74f));
            closeButton.onClick.AddListener(HideCommandHelp);
            commandHelpPanel.SetActive(false);
        }

        private void BuildResourceStockPanel(Transform root)
        {
            resourceStockTexts.Clear();
            resourceStockStatusTexts.Clear();
            var panel = CreatePanel(root, "Resource Ledger", Anchor.TopStretch, new Vector2(60f, -236f), new Vector2(-118f, -134f), new Color32(255, 250, 236, 248));
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (var face = 1; face <= 6; face += 1)
            {
                var cellButton = CreateButton(panel, $"Resource {face}", string.Empty, Anchor.Stretch, Vector2.zero, Vector2.zero);
                var cell = cellButton.GetComponent<RectTransform>();
                cellButton.GetComponent<Image>().color = StockCellColor(face);
                var cellLayout = cellButton.gameObject.AddComponent<LayoutElement>();
                cellLayout.minWidth = 116f;
                cellLayout.flexibleWidth = 1f;
                var label = CreateText(cell, $"Resource {face} Label", $"{face} {YachtRushRules.CrewResourceName(face).ToUpperInvariant()}", 14, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(8f, -28f), new Vector2(-8f, -6f));
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextMinSize = 10;
                var count = CreateText(cell, $"Resource {face} Count", "0", 30, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, new Vector2(8f, 24f), new Vector2(-8f, -30f));
                count.alignment = TextAnchor.MiddleCenter;
                count.resizeTextMinSize = 18;
                var status = CreateText(cell, $"Resource {face} Status", "LOW", 11, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(8f, 5f), new Vector2(-8f, 24f));
                status.alignment = TextAnchor.MiddleCenter;
                status.resizeTextMinSize = 8;
                var captured = face;
                cellButton.onClick.AddListener(() => ShowResourceHelp(captured));
                resourceStockTexts[face] = count;
                resourceStockStatusTexts[face] = status;
            }
        }

        private void ShowCommandHelp(YachtRushCategory? category)
        {
            if (commandHelpPanel == null)
            {
                return;
            }

            ConfigureCommandHelpPanel(true);
            commandHelpTitleText.text = category.HasValue
                ? YachtRushRules.GetHarborAction(category.Value).Name
                : "How to Play";
            commandHelpBodyText.text = CommandGuideText(category);
            commandHelpStrategy = null;
            if (commandHelpChooseButton != null)
            {
                commandHelpChooseButton.gameObject.SetActive(false);
            }

            commandHelpPanel.SetActive(true);
        }

        private void ConfigureCommandHelpPanel(bool compact)
        {
            if (commandHelpPanel == null)
            {
                return;
            }

            var rect = commandHelpPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.offsetMin = compact ? new Vector2(-360f, -220f) : new Vector2(-430f, -330f);
                rect.offsetMax = compact ? new Vector2(360f, 220f) : new Vector2(430f, 330f);
            }

            if (commandHelpBodyText != null)
            {
                commandHelpBodyText.fontSize = compact ? 16 : 15;
                commandHelpBodyText.resizeTextMinSize = compact ? 12 : 10;
                commandHelpBodyText.lineSpacing = compact ? 1f : 0.92f;
            }
        }

        private void ShowResourceHelp(int face)
        {
            if (commandHelpPanel == null)
            {
                return;
            }

            ConfigureCommandHelpPanel(false);
            var safeFace = Mathf.Clamp(face, 1, 6);
            var current = CurrentResourceCounts()[safeFace - 1];
            commandHelpTitleText.text = $"{ResourceCode(safeFace)} Resource";
            commandHelpBodyText.text = ResourceGuideText(safeFace, current);
            commandHelpStrategy = null;
            if (commandHelpChooseButton != null)
            {
                commandHelpChooseButton.gameObject.SetActive(false);
            }

            commandHelpPanel.SetActive(true);
        }

        private void ShowStrategyHelp(VoyageStrategy strategy, VoyageStrategyPreview preview, bool isBest)
        {
            if (commandHelpPanel == null)
            {
                return;
            }

            ConfigureCommandHelpPanel(false);
            var state = IsLimitedStrategyUsed(strategy)
                    ? "USED"
                : !hasPendingTurnResult
                    ? "CATALOG"
                    : isBest
                    ? "BEST"
                    : preview.IsAvailable
                        ? "OPEN"
                        : "LOCKED";
            commandHelpTitleText.text = $"{preview.Name}  /  {state}";
            commandHelpBodyText.text =
                $"{StrategyUseLimitLine(strategy)}\n\n" +
                $"{StrategyUsedLine(strategy)}" +
                $"Cost: {ResourceCostSummary(preview.ResourceCost)}\n" +
                $"Your resources: {ResourceCostSummary(CurrentResourceCounts())}\n" +
                $"{MissingResourceLine(preview)}\n\n" +
                $"{ResourceRoleLine(preview.ResourceCost)}\n\n" +
                $"Effect: {StrategyEffectFormula(preview)}\n\n" +
                "Top HUD tracks Month, Distance, and Best.\n" +
                "When chosen: spend the listed resources, gain the effect, then Month +1.\n" +
                "No automatic monthly drain. HULL 0 or FOOD 0 ends the voyage.\n" +
                "No free exchange: deck tokens are the only conversions.\n\n" +
                StrategyRationale(strategy);
            commandHelpStrategy = strategy;
            if (commandHelpChooseButton != null)
            {
                var canChoose = hasPendingTurnResult && preview.IsAvailable && !IsLimitedStrategyUsed(strategy);
                commandHelpChooseButton.gameObject.SetActive(canChoose);
                commandHelpChooseButton.interactable = canChoose;
            }

            commandHelpPanel.SetActive(true);
        }

        private void ConfirmStrategyFromHelp()
        {
            if (!commandHelpStrategy.HasValue || !hasPendingTurnResult)
            {
                return;
            }

            var strategy = commandHelpStrategy.Value;
            var index = Array.FindIndex(currentStrategyPreviews, item => item.Strategy == strategy && item.IsAvailable);
            if (index < 0)
            {
                return;
            }

            HideCommandHelp();
            ChooseCaptainOrder(index);
        }

        private void HideCommandHelp()
        {
            commandHelpStrategy = null;
            if (commandHelpPanel != null)
            {
                commandHelpPanel.SetActive(false);
            }
        }

        private ScoreButtonView CreateScoreButton(Transform parent, YachtRushCategory category)
        {
            var action = YachtRushRules.GetHarborAction(category);
            var button = CreateButton(parent, action.Name, string.Empty, Anchor.Stretch, Vector2.zero, Vector2.zero);
            var rect = button.GetComponent<RectTransform>();
            var background = button.GetComponent<Image>();

            var nameText = CreateText(rect, "Name", action.Name, 17, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(13f, -30f), new Vector2(-100f, -6f));
            nameText.alignment = TextAnchor.MiddleLeft;
            var hintText = CreateText(rect, "Hint", action.Pattern, 9, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopStretch, new Vector2(13f, -51f), new Vector2(-100f, -32f));
            hintText.alignment = TextAnchor.MiddleLeft;
            var detailText = CreateText(rect, "Breakdown", "Roll supplies to preview command", 9, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(13f, 8f), new Vector2(-92f, 35f));
            detailText.alignment = TextAnchor.MiddleLeft;
            var totalText = CreateText(rect, "Total", "-", 29, FontStyle.Bold, SketchPalette.Ink, Anchor.StretchRight, new Vector2(-84f, 18f), new Vector2(-14f, -18f));
            var tagText = CreateText(rect, "Tag", string.Empty, 9, FontStyle.Bold, SketchPalette.Ink, Anchor.TopRight, new Vector2(-96f, -25f), new Vector2(-14f, -6f));

            button.onClick.AddListener(() => ScoreCategory(category));

            return new ScoreButtonView
            {
                Button = button,
                Background = background,
                NameText = nameText,
                HintText = hintText,
                DetailText = detailText,
                TotalText = totalText,
                TagText = tagText
            };
        }

        private void UpdateBowlInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                pointerStartedOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                pointerStartedOnBowl = false;
                if (!pointerStartedOverUi && CanThrow)
                {
                    if (IsPointerOnBowl(Input.mousePosition))
                    {
                        pointerStartedOnBowl = true;
                        isDraggingBowl = true;
                        dragStartScreen = Input.mousePosition;
                        lastPointerScreen = Input.mousePosition;
                        pointerVelocity = Vector2.zero;
                        bowlShake = 0f;
                        PlaceUnlockedDiceInBowl();
                        PlayAudioCue("grab", 0.72f);
                    }
                }
            }

            if (isDraggingBowl && Input.GetMouseButton(0))
            {
                var current = (Vector2)Input.mousePosition;
                var delta = current - lastPointerScreen;
                pointerVelocity = delta / Mathf.Max(Time.deltaTime, 0.016f);
                bowlShake += Mathf.Min(delta.magnitude, 42f);
                lastPointerScreen = current;

                var plane = new Plane(Vector3.up, new Vector3(0f, BowlHome.y, 0f));
                var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out var enter))
                {
                    bowlRoot.position = ClampBowlDragPoint(ray.GetPoint(enter));
                    bowlRoot.rotation = Quaternion.Euler(-2f, -8f, 0f);
                }

                ShakeDiceInBowl();
            }

            if (isDraggingBowl && Input.GetMouseButtonUp(0))
            {
                isDraggingBowl = false;
                ThrowUnlockedDice(pointerVelocity, bowlShake);
                return;
            }

            if (Input.GetMouseButtonUp(0) && !pointerStartedOverUi && !pointerStartedOnBowl)
            {
                if (CanScore)
                {
                    DieView die = null;
                    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out var hit, 100f))
                    {
                        die = dice.FirstOrDefault(item => item.Transform == hit.transform || hit.transform.IsChildOf(item.Transform));
                    }

                    die ??= FindDieNearPointer(Input.mousePosition);
                    if (die != null)
                    {
                        ToggleDieHold(die);
                        return;
                    }
                }

                var command = FindCommandToken(Input.mousePosition);
                if (command.HasValue)
                {
                    SelectOrInspectStrategyToken(command.Value);
                }
            }
        }

        private YachtRushCategory? FindCommandToken(Vector2 screenPosition)
        {
            if (mainCamera == null)
            {
                return null;
            }

            var ray = mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out var hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) &&
                commandTokenColliders.TryGetValue(hit.collider, out var category))
            {
                return category;
            }

            return null;
        }

        private void SelectOrInspectStrategyToken(YachtRushCategory category)
        {
            var strategy = StrategyForTokenCategory(category);
            var preview = FindStrategyPreview(strategy);
            var bestStrategy = BestOpenStrategy();
            if (!hasPendingTurnResult)
            {
                ShowStrategyHelp(strategy, preview, false);
                return;
            }

            ShowStrategyHelp(strategy, preview, bestStrategy.HasValue && bestStrategy.Value == strategy);
        }

        private Vector3 ClampBowlDragPoint(Vector3 point)
        {
            var portrait = IsPortraitLayout();
            var minX = portrait ? -TableHalfWidth - 0.45f : -TableHalfWidth - 3.35f;
            var maxX = portrait ? TableHalfWidth + 0.45f : TableHalfWidth + 0.52f;
            var minZ = portrait ? -TableHalfDepth - 1.35f : -TableHalfDepth - 0.72f;
            var maxZ = TableHalfDepth + 0.18f;

            point.x = Mathf.Clamp(point.x, minX, maxX);
            point.y = BowlHome.y;
            point.z = Mathf.Clamp(point.z, minZ, maxZ);
            return point;
        }

        private DieView FindDieNearPointer(Vector2 screenPosition)
        {
            var bestDistance = float.MaxValue;
            DieView bestDie = null;
            var threshold = IsPortraitLayout() ? 64f : 46f;
            threshold *= Mathf.Clamp(Screen.dpi / 220f, 1f, 1.45f);

            foreach (var die in dice)
            {
                if (die.Transform.parent == bowlRoot)
                {
                    continue;
                }

                var screenPoint = mainCamera.WorldToScreenPoint(die.Transform.position);
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                var distance = Vector2.Distance(screenPosition, screenPoint);
                if (distance < threshold && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestDie = die;
                }
            }

            return bestDie;
        }

        private void UpdateBowlFeedback()
        {
            if (bowlGripHaloRenderer == null || bowlGripHalo == null)
            {
                return;
            }

            var hovering = CanThrow && !pointerStartedOverUi && IsPointerOnBowl(Input.mousePosition);
            var active = isDraggingBowl || hovering || CanThrow;
            bowlGripHaloRenderer.enabled = active;
            if (!active)
            {
                return;
            }

            bowlFeedbackPulse = Mathf.Repeat(bowlFeedbackPulse + Time.deltaTime * (isDraggingBowl ? 3.8f : 1.25f), Mathf.PI * 2f);
            var pulse = Mathf.Sin(bowlFeedbackPulse) * 0.018f;
            var gripScale = isDraggingBowl ? 1.045f : hovering ? 1.025f : 1f;
            bowlGripHalo.localScale = Vector3.one * (gripScale + pulse);

            var material = bowlGripHaloRenderer.sharedMaterial;
            material.color = isDraggingBowl
                ? new Color32(255, 221, 111, 255)
                : hovering
                    ? new Color32(255, 234, 154, 255)
                    : new Color32(255, 247, 203, 255);
        }

        private bool IsPointerOnBowl(Vector2 screenPosition)
        {
            var ray = mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out var hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) &&
                hit.transform.IsChildOf(bowlRoot))
            {
                return true;
            }

            var plane = new Plane(Vector3.up, new Vector3(0f, BowlHome.y, 0f));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            var local = bowlRoot.InverseTransformPoint(ray.GetPoint(enter));
            var normalized = new Vector2(local.x / (BowlRadiusX * 1.45f), local.z / (BowlRadiusZ * 1.55f));
            return normalized.sqrMagnitude <= 1f;
        }

        private void UpdateCameraForScreen(bool force = false)
        {
            if (!force && lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            var uiScale = CanvasScaleEstimate();
            var topInset = Mathf.Clamp((138f * uiScale + 10f) / Mathf.Max(1f, Screen.height), 0.08f, 0.13f);
            var bottomInset = Mathf.Clamp((34f * uiScale + 8f) / Mathf.Max(1f, Screen.height), 0.018f, 0.055f);
            var cameraHeight = Mathf.Clamp(1f - topInset - bottomInset, 0.68f, 0.86f);
            mainCamera.rect = new Rect(0f, bottomInset, 1f, cameraHeight);

            var aspect = Mathf.Max(0.46f, Screen.width / (float)Mathf.Max(1, Mathf.RoundToInt(Screen.height * cameraHeight)));
            mainCamera.orthographic = true;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 60f;
            mainCamera.transform.rotation = CameraRotation;
            mainCamera.transform.position = CameraPositionForTarget(CameraFrameCenter(), 8.8f);
            var minimumCameraSize = IsPortraitLayout() ? 7.35f : 7.8f;
            mainCamera.orthographicSize = Mathf.Clamp(
                Mathf.Max(minimumCameraSize, CameraFrameHalfWidth() / aspect, CameraFrameHalfDepthOnScreen()),
                minimumCameraSize,
                15.4f);

            UpdateScoreChooserLayout();
            if (bowlRoot != null && !isDraggingBowl)
            {
                ParkBowl();
            }
        }

        private void UpdateScoreChooserLayout()
        {
            if (scoreChooserRect == null || scoreGridLayout == null)
            {
                return;
            }

            var compactPortrait = Screen.width < Screen.height;
            var columns = compactPortrait ? 3 : 4;
            var referenceWidth = Screen.width / Mathf.Max(0.1f, CanvasScaleEstimate());
            var availableGridWidth = Mathf.Max(640f, referenceWidth - 84f);
            var cellWidth = Mathf.Floor((availableGridWidth - scoreGridLayout.spacing.x * (columns - 1)) / columns);
            scoreChooserRect.offsetMax = new Vector2(-24f, ScoreChooserHeight());
            scoreGridLayout.constraintCount = columns;
            scoreGridLayout.cellSize = compactPortrait
                ? new Vector2(Mathf.Clamp(cellWidth, 202f, 286f), 82f)
                : new Vector2(Mathf.Clamp(cellWidth, 226f, 286f), 88f);
        }

        private static float ScoreChooserHeight()
        {
            var compactPortrait = Screen.width < Screen.height;
            return compactPortrait ? 620f : 500f;
        }

        private static float CanvasScaleEstimate()
        {
            var widthScale = Mathf.Max(0.1f, Screen.width / 1080f);
            var heightScale = Mathf.Max(0.1f, Screen.height / 1920f);
            return Mathf.Sqrt(widthScale * heightScale);
        }

        private void PlaceUnlockedDiceInBowl()
        {
            var looseDice = YachtRushRules.ShouldRerollHeldDice(currentRollRule)
                ? dice.ToArray()
                : dice.Where(die => !die.IsHeld).ToArray();
            for (var order = 0; order < looseDice.Length; order += 1)
            {
                var die = looseDice[order];
                var slot = BowlSlot(order, looseDice.Length);
                var rotation = BowlDiceRotation(order);
                die.Rigidbody.isKinematic = true;
                die.Rigidbody.linearVelocity = Vector3.zero;
                die.Rigidbody.angularVelocity = Vector3.zero;
                if (YachtRushRules.ShouldRerollHeldDice(currentRollRule))
                {
                    die.IsHeld = false;
                }

                die.Transform.SetParent(bowlRoot, false);
                die.Transform.localPosition = slot;
                die.Transform.localRotation = rotation;
                die.BowlVelocity = Vector3.zero;
                die.UpdateHoldRing();
            }
        }

        private void ShakeDiceInBowl()
        {
            var looseDice = dice.Where(die => !die.IsHeld && die.Transform.parent == bowlRoot).ToArray();
            var dt = Mathf.Max(Time.deltaTime, 0.016f);
            var dragForce = new Vector3(
                Mathf.Clamp(pointerVelocity.x * 0.0009f, -4f, 4f),
                0f,
                Mathf.Clamp(pointerVelocity.y * 0.0009f, -4f, 4f));
            var inputPower = Mathf.Clamp01(pointerVelocity.magnitude / 1250f);
            var damping = inputPower > 0.035f ? 0.5f : 0.16f;
            if (inputPower > 0.08f && Time.unscaledTime >= nextShakeSoundTime)
            {
                PlayAudioCue("shake", Mathf.Lerp(0.18f, 0.52f, inputPower));
                nextShakeSoundTime = Time.unscaledTime + Mathf.Lerp(0.18f, 0.07f, inputPower);
            }

            for (var order = 0; order < looseDice.Length; order += 1)
            {
                var die = looseDice[order];
                var wave = bowlShake * 0.1f + order * 1.71f;
                var swirl = new Vector3(Mathf.Cos(wave), 0f, Mathf.Sin(wave * 0.84f)) * (0.62f * inputPower);
                die.BowlVelocity += (dragForce + swirl) * dt;
                die.BowlVelocity *= Mathf.Pow(damping, dt);
                die.Transform.localPosition += die.BowlVelocity * dt;
                KeepDieInsideBowl(die);
            }

            ResolveBowlDiceSpacing(looseDice);

            foreach (var die in looseDice)
            {
                die.Transform.localRotation = Quaternion.Euler(
                    die.BowlVelocity.z * -220f * dt,
                    pointerVelocity.x * 0.018f * dt,
                    die.BowlVelocity.x * 220f * dt) * die.Transform.localRotation;
            }
        }

        private Vector3 BowlSlot(int order, int count)
        {
            if (count <= 1)
            {
                return new Vector3(0f, 0.45f, 0.02f);
            }

            var spread = count >= 5 ? 0.62f : count == 4 ? 0.72f : 0.82f;
            var x = (order - (count - 1) * 0.5f) * spread;
            var z = order % 2 == 0 ? -0.18f : 0.22f;
            if (count <= 3)
            {
                z *= 0.72f;
            }

            return new Vector3(x, 0.38f + (order % 2) * 0.02f, z);
        }

        private Quaternion BowlDiceRotation(int order)
        {
            var faceTilt = new[]
            {
                new Vector3(7f, -12f, 4f),
                new Vector3(-5f, 15f, -7f),
                new Vector3(6f, 4f, 10f),
                new Vector3(-8f, -18f, 5f),
                new Vector3(4f, 22f, -9f)
            };
            var tilt = faceTilt[order % faceTilt.Length];
            return Quaternion.Euler(tilt.x, tilt.y + order * 18f, tilt.z);
        }

        private void KeepDieInsideBowl(DieView die)
        {
            var position = die.Transform.localPosition;
            position.y = 0.38f;
            var normalized = new Vector2(position.x / BowlRadiusX, position.z / BowlRadiusZ);
            var magnitude = normalized.magnitude;
            if (magnitude > 1f)
            {
                normalized /= magnitude;
                position.x = normalized.x * BowlRadiusX;
                position.z = normalized.y * BowlRadiusZ;

                var normal = new Vector3(normalized.x / BowlRadiusX, 0f, normalized.y / BowlRadiusZ).normalized;
                die.BowlVelocity = Vector3.Reflect(die.BowlVelocity, normal) * 0.58f;
            }

            die.Transform.localPosition = position;
        }

        private void ResolveBowlDiceSpacing(DieView[] looseDice)
        {
            for (var pass = 0; pass < 4; pass += 1)
            {
                for (var first = 0; first < looseDice.Length; first += 1)
                {
                    for (var second = first + 1; second < looseDice.Length; second += 1)
                    {
                        SeparateBowlDice(looseDice[first], looseDice[second], first + second + pass);
                    }
                }

                foreach (var die in looseDice)
                {
                    KeepDieInsideBowl(die);
                }
            }
        }

        private void SeparateBowlDice(DieView first, DieView second, int seed)
        {
            var firstPosition = first.Transform.localPosition;
            var secondPosition = second.Transform.localPosition;
            var delta = new Vector2(secondPosition.x - firstPosition.x, secondPosition.z - firstPosition.z);
            var distance = delta.magnitude;
            if (distance >= BowlDiceSpacing)
            {
                return;
            }

            if (distance < 0.001f)
            {
                var angle = seed * 1.19f;
                delta = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = 1f;
            }

            var direction = delta / distance;
            var push = (BowlDiceSpacing - distance) * 0.5f;
            firstPosition.x -= direction.x * push;
            firstPosition.z -= direction.y * push;
            secondPosition.x += direction.x * push;
            secondPosition.z += direction.y * push;
            first.Transform.localPosition = firstPosition;
            second.Transform.localPosition = secondPosition;
            var impulse = new Vector3(direction.x, 0f, direction.y) * push * 3.2f;
            first.BowlVelocity -= impulse;
            second.BowlVelocity += impulse;
        }

        private void ThrowUnlockedDice(Vector2 releaseVelocity, float shake)
        {
            if (!CanThrow)
            {
                PlayAudioCue("hold", 0.22f);
                UpdateHudAndScores();
                ParkBowl();
                return;
            }

            if (rollCount == MaxRollsThisRound - 1)
            {
                lockedBeforeFinalThrow = HeldCount;
            }

            rollCount += 1;
            isResolvingRoll = true;
            stableSeconds = 0f;
            PlayAudioCue("throw", 0.9f);
            FirebaseTelemetry.LogEvent(
                "dice_throw",
                new Dictionary<string, string>
                {
                    { "round", RoundNumber.ToString() },
                    { "throw", rollCount.ToString() },
                    { "max_throw", MaxRollsThisRound.ToString() },
                    { "held", HeldCount.ToString() },
                    { "shake", Mathf.RoundToInt(shake).ToString() }
                });

            var throwForward = Mathf.Clamp(releaseVelocity.y / 620f, -1.4f, 1.8f);
            var throwRight = Mathf.Clamp(releaseVelocity.x / 520f, -2.6f, 3.6f);
            var basePower = Mathf.Clamp(4.3f + shake * 0.01f + releaseVelocity.magnitude * 0.0014f, 4.3f, 7.8f);

            for (var index = 0; index < dice.Count; index += 1)
            {
                var die = dice[index];
                if (die.IsHeld && !YachtRushRules.ShouldRerollHeldDice(currentRollRule))
                {
                    continue;
                }

                die.IsHeld = false;
                die.Transform.SetParent(null, true);
                die.Rigidbody.isKinematic = false;
                die.Rigidbody.linearVelocity = Vector3.zero;
                die.Rigidbody.angularVelocity = Vector3.zero;
                var bowlCarry = bowlRoot.TransformDirection(die.BowlVelocity);
                var stormBoost = index == rushDieIndex && currentRushDie == YachtRushRushDie.Storm ? 1.8f : 1f;
                die.Rigidbody.AddForce(
                    (new Vector3(throwRight + (index - 2) * 0.42f, 2.4f + index * 0.12f, basePower + throwForward) + bowlCarry * 1.15f) * stormBoost,
                    ForceMode.VelocityChange);
                die.Rigidbody.AddTorque(((UnityEngine.Random.onUnitSphere * (5.8f + shake * 0.015f)) + bowlCarry * 2.4f) * stormBoost, ForceMode.VelocityChange);
                die.BowlVelocity = Vector3.zero;
                die.UpdateHoldRing();
            }

            ParkBowl();
            UpdateHudAndScores();
        }

        private void ParkBowl()
        {
            bowlRoot.position = CurrentBowlDock();
            bowlRoot.rotation = Quaternion.Euler(-2f, -8f, 0f);
            bowlShake = 0f;
        }

        private static bool IsPortraitLayout()
        {
            return Screen.width < Screen.height;
        }

        private static Vector3 CurrentBowlDock()
        {
            return IsPortraitLayout() ? BowlDockPortrait : BowlDockLandscape;
        }

        private static Vector3 CameraFrameCenter()
        {
            return new Vector3((CameraFrameMinX() + CameraFrameMaxX()) * 0.5f, 0f, (CameraFrameMinZ() + CameraFrameMaxZ()) * 0.5f);
        }

        private static Vector3 CameraPositionForTarget(Vector3 target, float cameraY)
        {
            var forward = CameraRotation * Vector3.forward;
            var distance = (cameraY - target.y) / -forward.y;
            var position = target - forward * distance;
            position.y = cameraY;
            return position;
        }

        private static float CameraFrameHalfWidth()
        {
            return (CameraFrameMaxX() - CameraFrameMinX()) * 0.5f;
        }

        private static float CameraFrameHalfDepthOnScreen()
        {
            var halfDepth = (CameraFrameMaxZ() - CameraFrameMinZ()) * 0.5f;
            return halfDepth * Mathf.Sin(68f * Mathf.Deg2Rad);
        }

        private static float CameraFrameMinX()
        {
            return IsPortraitLayout()
                ? Mathf.Min(-8.4f, CurrentBowlDock().x - 1.9f)
                : Mathf.Min(-8.6f, CurrentBowlDock().x - 2.35f);
        }

        private static float CameraFrameMaxX()
        {
            return 11f;
        }

        private static float CameraFrameMinZ()
        {
            return IsPortraitLayout()
                ? Mathf.Min(-7.75f, CurrentBowlDock().z - 1.18f)
                : -7.75f;
        }

        private static float CameraFrameMaxZ()
        {
            return 6.75f;
        }

        private void UpdateRollSettlement()
        {
            var unlockedDice = dice.Where(die => !die.IsHeld).ToArray();
            var allSlow = unlockedDice.All(die =>
                die.Rigidbody.linearVelocity.magnitude <= SettledVelocity &&
                die.Rigidbody.angularVelocity.magnitude <= SettledAngularVelocity);

            stableSeconds = allSlow ? stableSeconds + Time.deltaTime : 0f;
            if (stableSeconds < RequiredStableSeconds)
            {
                return;
            }

            foreach (var die in unlockedDice)
            {
                die.SetValue(ReadTopFace(die.Transform));
                if (die.Index == rushDieIndex && currentRushDie == YachtRushRushDie.Anchor)
                {
                    die.IsHeld = true;
                }

                die.Rigidbody.linearVelocity = Vector3.zero;
                die.Rigidbody.angularVelocity = Vector3.zero;
                die.Rigidbody.isKinematic = true;
                SnapDieToReadableRest(die);
                die.UpdateHoldRing();
            }

            ResolveSettledDiceLayout();
            isResolvingRoll = false;
            PlayAudioCue("settle", 0.72f);
            ResolvePendingVoyageTurn();
            UpdateHudAndScores();
        }

        private void KeepRollingDiceInPlayArea()
        {
            foreach (var die in dice)
            {
                if (die.IsHeld || die.Transform.parent == bowlRoot)
                {
                    continue;
                }

                var position = die.Transform.position;
                var velocity = die.Rigidbody.linearVelocity;
                var clamped = false;

                if (position.x < PlayMinX + DiceSize * 0.5f)
                {
                    position.x = PlayMinX + DiceSize * 0.5f;
                    velocity.x = Mathf.Abs(velocity.x) * 0.38f;
                    clamped = true;
                }
                else if (position.x > PlayMaxX - DiceSize * 0.5f)
                {
                    position.x = PlayMaxX - DiceSize * 0.5f;
                    velocity.x = -Mathf.Abs(velocity.x) * 0.38f;
                    clamped = true;
                }

                if (position.z < PlayMinZ + DiceSize * 0.5f)
                {
                    position.z = PlayMinZ + DiceSize * 0.5f;
                    velocity.z = Mathf.Abs(velocity.z) * 0.38f;
                    clamped = true;
                }
                else if (position.z > PlayMaxZ - DiceSize * 0.5f)
                {
                    position.z = PlayMaxZ - DiceSize * 0.5f;
                    velocity.z = -Mathf.Abs(velocity.z) * 0.38f;
                    clamped = true;
                }

                if (position.y < DiceRestY)
                {
                    position.y = DiceRestY;
                    velocity.y = Mathf.Max(0f, velocity.y);
                    clamped = true;
                }

                if (clamped)
                {
                    die.Rigidbody.position = position;
                    die.Rigidbody.linearVelocity = velocity;
                    die.Rigidbody.angularVelocity *= 0.65f;
                }
            }
        }

        private void ApplyTableDiceFriction()
        {
            foreach (var die in dice)
            {
                if (die.IsHeld || die.Transform.parent == bowlRoot || die.Rigidbody.isKinematic)
                {
                    continue;
                }

                var linearSpeed = die.Rigidbody.linearVelocity.magnitude;
                var angularSpeed = die.Rigidbody.angularVelocity.magnitude;
                if (linearSpeed <= SnapVelocity && angularSpeed <= SnapAngularVelocity && die.Transform.position.y <= DiceRestY + 0.18f)
                {
                    die.Rigidbody.linearVelocity = Vector3.zero;
                    die.Rigidbody.angularVelocity = Vector3.zero;
                    die.Rigidbody.Sleep();
                    continue;
                }

                if (linearSpeed <= StrongFrictionVelocity && die.Transform.position.y <= DiceRestY + 0.22f)
                {
                    var damping = Mathf.Pow(0.04f, Time.deltaTime);
                    die.Rigidbody.linearVelocity *= damping;
                    die.Rigidbody.angularVelocity *= Mathf.Pow(0.07f, Time.deltaTime);
                }
            }
        }

        private void ResolveSettledDiceLayout()
        {
            var tableDice = dice.Where(die => die.Transform.parent != bowlRoot).ToArray();
            for (var pass = 0; pass < 18; pass += 1)
            {
                foreach (var die in tableDice)
                {
                    ClampDieRestPosition(die);
                }

                for (var first = 0; first < tableDice.Length; first += 1)
                {
                    for (var second = first + 1; second < tableDice.Length; second += 1)
                    {
                        SeparateDice(tableDice[first], tableDice[second], first + second + pass);
                    }
                }
            }

            foreach (var die in tableDice)
            {
                ClampDieRestPosition(die);
                SnapDieToReadableRest(die);
            }
        }

        private void SeparateDice(DieView first, DieView second, int seed)
        {
            var firstPosition = first.Transform.position;
            var secondPosition = second.Transform.position;
            var delta = new Vector2(secondPosition.x - firstPosition.x, secondPosition.z - firstPosition.z);
            var distance = delta.magnitude;
            if (distance >= DiceMinSpacing)
            {
                return;
            }

            if (distance < 0.001f)
            {
                var angle = seed * 1.37f;
                delta = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = 1f;
            }

            var direction = delta / distance;
            var push = (DiceMinSpacing - distance) * 0.5f;
            firstPosition.x -= direction.x * push;
            firstPosition.z -= direction.y * push;
            secondPosition.x += direction.x * push;
            secondPosition.z += direction.y * push;
            first.Transform.position = firstPosition;
            second.Transform.position = secondPosition;
        }

        private void ClampDieRestPosition(DieView die)
        {
            var position = die.Transform.position;
            position.x = Mathf.Clamp(position.x, PlayMinX + DiceSize * 0.55f, PlayMaxX - DiceSize * 0.55f);
            position.y = DiceRestY;
            position.z = Mathf.Clamp(position.z, PlayMinZ + DiceSize * 0.55f, PlayMaxZ - DiceSize * 0.55f);
            die.Transform.position = position;
        }

        private void SnapDieToReadableRest(DieView die)
        {
            var position = die.Transform.position;
            position.y = DiceRestY;
            die.Transform.position = position;

            var forward = Vector3.ProjectOnPlane(die.Transform.forward, Vector3.up);
            var yaw = forward.sqrMagnitude > 0.001f
                ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg
                : die.Index * 21f;
            die.Transform.rotation = StableRotationForValue(die.Value, yaw);
        }

        private static Quaternion StableRotationForValue(int value, float yaw)
        {
            var faceUp = Quaternion.FromToRotation(FaceNormalForValue(value), Vector3.up);
            return Quaternion.AngleAxis(yaw, Vector3.up) * faceUp;
        }

        private static Vector3 FaceNormalForValue(int value)
        {
            switch (Mathf.Clamp(value, 1, 6))
            {
                case 2:
                    return Vector3.forward;
                case 3:
                    return Vector3.right;
                case 4:
                    return Vector3.left;
                case 5:
                    return Vector3.back;
                case 6:
                    return Vector3.down;
                default:
                    return Vector3.up;
            }
        }

        private int ReadTopFace(Transform die)
        {
            var bestValue = 1;
            var bestDot = -1f;
            var faces = new[]
            {
                (Value: 1, Normal: Vector3.up),
                (Value: 6, Normal: Vector3.down),
                (Value: 2, Normal: Vector3.forward),
                (Value: 5, Normal: Vector3.back),
                (Value: 3, Normal: Vector3.right),
                (Value: 4, Normal: Vector3.left)
            };

            foreach (var face in faces)
            {
                var dot = Vector3.Dot(die.TransformDirection(face.Normal), Vector3.up);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestValue = face.Value;
                }
            }

            return bestValue;
        }

        private void UpdateHeldDiceVisuals()
        {
            foreach (var die in dice)
            {
                die.UpdateHoldRing();
            }
        }

        private void ResolvePendingVoyageTurn()
        {
            pendingResourceCounts = YachtRushRules.CountCrewResources(CurrentDiceValues());
            AddCrewResourceRoll(pendingResourceCounts);
            currentStrategyPreviews = YachtRushRules.AllVoyageStrategyPreviews(CurrentResourceCounts());
            hasPendingTurnResult = true;
            if (captainOrderPanel != null)
            {
                captainOrderPanel.SetActive(false);
            }

            PulseResourceStations(CurrentResourceCounts());
            if (voyageStatusText != null)
            {
                voyageStatusText.text = "Inspect a token, then choose an OPEN plan.";
            }

            FirebaseTelemetry.LogEvent(
                "voyage_resource_roll",
                new Dictionary<string, string>
                {
                    { "month", RoundNumber.ToString() },
                    { "resources", string.Join("-", pendingResourceCounts.Select(count => count.ToString())) },
                    { "dice", string.Join("-", CurrentDiceValues().Select(value => value.ToString())) }
                });
        }

        private void ChooseCaptainOrder(int index)
        {
            if (!hasPendingTurnResult || currentStrategyPreviews.Length == 0)
            {
                return;
            }

            var preview = currentStrategyPreviews[Mathf.Clamp(index, 0, currentStrategyPreviews.Length - 1)];
            if (!preview.IsAvailable || IsLimitedStrategyUsed(preview.Strategy))
            {
                PlayAudioCue("error", 0.2f);
                return;
            }

            var resourceRoll = pendingResourceCounts.ToArray();
            var resourceCost = preview.ResourceCost.ToArray();
            SpendCrewResources(resourceCost);
            var nextState = YachtRushRules.ApplyVoyageStrategy(
                CurrentHarborState(),
                preview,
                resolvedMonths,
                out var supplyUpkeep,
                out var stormDamage);
            routeProgress = nextState.RouteProgress;
            hull = nextState.Hull;
            supplies = nextState.Supplies;
            contractScore = nextState.ContractScore;
            resolvedMonths += 1;
            if (IsLimitedVoyageStrategy(preview.Strategy))
            {
                usedLimitedStrategies.Add(preview.Strategy);
            }

            hasPendingTurnResult = false;
            pendingResourceCounts = new int[6];
            currentStrategyPreviews = Array.Empty<VoyageStrategyPreview>();
            PulseResourceStations(CurrentResourceCounts());

            if (captainOrderPanel != null)
            {
                captainOrderPanel.SetActive(false);
            }

            var summary = $"{preview.Name}: Spend {ResourceCostSummary(resourceCost)} -> {preview.Effect}";

            ShowHarborFeedback(new HarborYachtActionEffect(
                preview.DistanceDelta,
                preview.HullDelta,
                preview.SupplyDelta,
                preview.GoldDelta,
                0,
                summary));
            PlayAudioCue("score", 0.62f);
            FirebaseTelemetry.LogEvent(
                "voyage_strategy_resolved",
                new Dictionary<string, string>
                {
                    { "month", resolvedMonths.ToString() },
                    { "strategy", preview.Strategy.ToString() },
                    { "distance_delta", preview.DistanceDelta.ToString() },
                    { "hull_delta", preview.HullDelta.ToString() },
                    { "supply_delta", preview.SupplyDelta.ToString() },
                    { "gold_delta", preview.GoldDelta.ToString() },
                    { "resources", string.Join("-", resourceRoll.Select(count => count.ToString())) },
                    { "route", routeProgress.ToString() },
                    { "hull", hull.ToString() },
                    { "supplies", supplies.ToString() },
                    { "gold", contractScore.ToString() }
                });

            var runResult = YachtRushRules.EvaluateVoyageRun(CurrentHarborState(), resolvedMonths);
            if (runResult.IsComplete)
            {
                EndRun(runResult);
            }
            else
            {
                PrepareNextRound();
            }
        }

        private void ScoreCategory(YachtRushCategory category)
        {
            if (!CanScore || scores.ContainsKey(category))
            {
                return;
            }

            var values = CurrentDiceValues();
            var scoredRound = RoundNumber;
            var score = YachtRushRules.PreviewScore(
                category,
                currentContract,
                currentRollRule,
                currentRushDie,
                rushDieIndex,
                values,
                Mathf.Max(0, rollCount - 1),
                lockedBeforeFinalThrow,
                HeldCount);
            var harborEffect = YachtRushRules.PreviewHarborAction(category, score, currentRushDie);
            if (!harborEffect.IsAvailable)
            {
                ShowHarborFeedback(harborEffect);
                ShowCommandHelp(category);
                PulseCommandToken(category, new Color32(154, 82, 58, 255));
                PlayAudioCue("hold", 0.24f);
                UpdateHudAndScores();
                return;
            }

            var harborState = YachtRushRules.ApplyHarborAction(CurrentHarborState(), harborEffect);
            routeProgress = harborState.RouteProgress;
            hull = harborState.Hull;
            supplies = harborState.Supplies;
            contractScore = harborState.ContractScore;
            ShowHarborFeedback(harborEffect);
            PulseCommandToken(category, new Color32(255, 232, 146, 255));
            PulseDeckAssetsForAction(category, harborEffect);

            scores[category] = new ScoreRecord(
                score.BaseScore,
                score.RushAdjustedScore,
                score.ContractBonus,
                score.Total,
                harborEffect.RouteDelta,
                harborEffect.HullDelta,
                harborEffect.SuppliesDelta,
                harborEffect.ContractScoreDelta);
            PlayAudioCue(score.ContractBonus > 0 || harborEffect.ContractScoreDelta > 0 ? "bonus" : "score", score.ContractBonus > 0 ? 0.82f : 0.62f);
            FirebaseTelemetry.LogEvent(
                "score_recorded",
                new Dictionary<string, string>
                {
                    { "round", scoredRound.ToString() },
                    { "category", category.ToString() },
                    { "base_score", score.BaseScore.ToString() },
                    { "rush_score", score.RushAdjustedScore.ToString() },
                    { "contract_bonus", score.ContractBonus.ToString() },
                    { "total_score", score.Total.ToString() },
                    { "run_score", scores.Values.Sum(record => record.Total).ToString() },
                    { "dice", string.Join("-", values) },
                    { "effective_dice", string.Join("-", score.EffectiveDice) },
                    { "contract", currentContract.ToString() },
                    { "roll_rule", currentRollRule.ToString() },
                    { "rush_die", currentRushDie.ToString() },
                    { "harbor_action", YachtRushRules.GetHarborAction(category).Name },
                    { "route", routeProgress.ToString() },
                    { "hull", hull.ToString() },
                    { "supplies", supplies.ToString() },
                    { "contract_score", contractScore.ToString() }
                });

            var result = YachtRushRules.EvaluateHarborRun(CurrentHarborState(), scores.Count);
            if (result.IsComplete)
            {
                EndRun(result);
            }
            else
            {
                PrepareNextRound();
            }
        }

        private void EndRun(HarborYachtRunResult? resolvedResult = null)
        {
            var result = resolvedResult ?? YachtRushRules.EvaluateVoyageRun(CurrentHarborState(), resolvedMonths);
            var total = FinalHarborScore();
            var isNewBest = routeProgress > bestScore;
            if (routeProgress > bestScore)
            {
                bestScore = routeProgress;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = isNewBest ? "New Best Voyage" : result.Title;
                resultTitleText.color = result.IsSuccess ? new Color32(62, 110, 65, 255) : SketchPalette.Ink;
            }

            resultScoreText.text = $"{routeProgress} nm";
            if (resultMetaText != null)
            {
                resultMetaText.text = $"Resources {ResourceStockSummary()}  Score {total}" +
                    (isNewBest ? "  New best distance" : $"  Best {bestScore} nm");
                resultMetaText.color = isNewBest ? new Color32(72, 116, 75, 255) : SketchPalette.MutedInk;
            }

            resultPanel.SetActive(true);
            FirebaseTelemetry.SetContext("score", total.ToString());
            FirebaseTelemetry.SetContext("best_distance", bestScore.ToString());
            FirebaseTelemetry.LogEvent(
                "run_end",
                new Dictionary<string, string>
                {
                    { "score", total.ToString() },
                    { "best_distance", bestScore.ToString() },
                    { "result", result.Title },
                    { "route", routeProgress.ToString() },
                    { "hull", hull.ToString() },
                    { "supplies", supplies.ToString() },
                    { "gold", contractScore.ToString() },
                    { "resources", ResourceStockSummary() }
                });
            MannLabAdMob.TryShowGameOverInterstitial();
            UpdateHudAndScores();
        }

        private HarborYachtState CurrentHarborState()
        {
            return new HarborYachtState(RoundNumber, routeProgress, hull, supplies, contractScore);
        }

        private int[] CurrentResourceCounts()
        {
            return new[]
            {
                Mathf.Max(0, windResource),
                Mathf.Max(0, hull),
                Mathf.Max(0, supplies),
                Mathf.Max(0, crewResource),
                Mathf.Max(0, contractScore),
                Mathf.Max(0, chartResource)
            };
        }

        private void AddCrewResourceRoll(IReadOnlyList<int> resourceCounts)
        {
            if (resourceCounts == null || resourceCounts.Count != 6)
            {
                return;
            }

            windResource += resourceCounts[0];
            hull = Mathf.Clamp(hull + resourceCounts[1], 0, YachtRushRules.HarborMaxHull);
            supplies = Mathf.Clamp(supplies + resourceCounts[2], 0, YachtRushRules.HarborMaxSupplies);
            crewResource += resourceCounts[3];
            contractScore += resourceCounts[4];
            chartResource += resourceCounts[5];
        }

        private void SpendCrewResources(IReadOnlyList<int> cost)
        {
            if (cost == null || cost.Count != 6)
            {
                return;
            }

            windResource = Mathf.Max(0, windResource - cost[0]);
            hull = Mathf.Max(0, hull - cost[1]);
            supplies = Mathf.Max(0, supplies - cost[2]);
            crewResource = Mathf.Max(0, crewResource - cost[3]);
            contractScore = Mathf.Max(0, contractScore - cost[4]);
            chartResource = Mathf.Max(0, chartResource - cost[5]);
        }

        private int FinalHarborScore()
        {
            return routeProgress +
                Mathf.Max(0, windResource) +
                Mathf.Max(0, hull) +
                Mathf.Max(0, supplies) * 2 +
                Mathf.Max(0, crewResource) +
                contractScore * 2 +
                Mathf.Max(0, chartResource) * 2;
        }

        private void ApplyWebCaptureStateIfRequested()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var shot = WebCaptureShotNumber();
            if (shot <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(BestScoreKey, 188);
            bestScore = 188;
            scores.Clear();
            routeProgress = 26;
            windResource = 3;
            hull = 16;
            supplies = 7;
            crewResource = 2;
            contractScore = 18;
            chartResource = 2;
            resultPanel.SetActive(false);
            isDraggingBowl = false;
            isResolvingRoll = false;
            pointerStartedOverUi = false;
            pointerStartedOnBowl = false;
            bowlShake = 0f;
            stableSeconds = 0f;
            lockedBeforeFinalThrow = 0;
            currentContract = YachtRushContract.None;
            currentRollRule = YachtRushRollRule.Classic;
            currentRushDie = YachtRushRushDie.Storm;
            currentTwist = RoundTwist.RushDie;
            rushDieIndex = 2;
            rollCount = 0;

            switch (shot)
            {
                case 1:
                    currentContract = YachtRushContract.None;
                    currentRollRule = YachtRushRollRule.Classic;
                    currentRushDie = YachtRushRushDie.Storm;
                    currentTwist = RoundTwist.RushDie;
                    rushDieIndex = 1;
                    SetCaptureDiceInBowl(new[] { 1, 2, 3, 4, 5 });
                    break;
                case 2:
                    currentContract = YachtRushContract.None;
                    currentRollRule = YachtRushRollRule.Classic;
                    currentRushDie = YachtRushRushDie.Storm;
                    currentTwist = RoundTwist.RushDie;
                    rushDieIndex = 4;
                    rollCount = 1;
                    SetCaptureDiceInBowl(new[] { 2, 2, 4, 5, 6 }, true);
                    break;
                case 3:
                    currentContract = YachtRushContract.None;
                    currentRollRule = YachtRushRollRule.Classic;
                    currentRushDie = YachtRushRushDie.Mirror;
                    currentTwist = RoundTwist.RushDie;
                    rushDieIndex = 3;
                    rollCount = 1;
                    SetCaptureDiceRolling(new[] { 6, 5, 4, 3, 2 });
                    break;
                case 4:
                    currentContract = YachtRushContract.None;
                    currentRollRule = YachtRushRollRule.Classic;
                    currentRushDie = YachtRushRushDie.Blank;
                    currentTwist = RoundTwist.RushDie;
                    rushDieIndex = 2;
                    rollCount = 2;
                    SeedRecordedScores(3);
                    SetCaptureDiceOnTable(new[] { 6, 6, 6, 4, 2 });
                    break;
                case 5:
                    currentContract = YachtRushContract.None;
                    currentRollRule = YachtRushRollRule.Classic;
                    currentRushDie = YachtRushRushDie.Cracked;
                    currentTwist = RoundTwist.RushDie;
                    rushDieIndex = 0;
                    rollCount = 2;
                    SeedRecordedScores(7);
                    SetCaptureDiceOnTable(new[] { 1, 2, 3, 5, 6 }, 2);
                    break;
                default:
                    currentContract = YachtRushContract.None;
                    currentRollRule = YachtRushRollRule.Classic;
                    currentRushDie = YachtRushRushDie.Anchor;
                    currentTwist = RoundTwist.RushDie;
                    rushDieIndex = 0;
                    SeedCompleteScores();
                    SetCaptureDiceOnTable(new[] { 5, 5, 5, 5, 5 });
                    ShowCaptureResult(scores.Values.Sum(score => score.Total), true);
                    break;
            }

            ApplyRushDieVisuals();
            UpdateHudAndScores();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static int WebCaptureShotNumber()
        {
            var url = Application.absoluteURL;
            if (string.IsNullOrWhiteSpace(url))
            {
                return 0;
            }

            var marker = "captureShot=";
            var start = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return 0;
            }

            start += marker.Length;
            var end = url.IndexOf('&', start);
            var value = end >= 0 ? url.Substring(start, end - start) : url.Substring(start);
            return int.TryParse(value, out var shot) ? shot : 0;
        }
#endif

        private void SetCaptureDiceInBowl(int[] values, bool lively = false)
        {
            ParkBowl();
            for (var index = 0; index < dice.Count; index += 1)
            {
                var die = dice[index];
                die.IsHeld = false;
                die.SetValue(values[index % values.Length]);
                die.Rigidbody.isKinematic = true;
                die.Rigidbody.linearVelocity = Vector3.zero;
                die.Rigidbody.angularVelocity = Vector3.zero;
                die.Transform.SetParent(bowlRoot, false);
                die.Transform.localPosition = BowlSlot(index, dice.Count) + (lively ? new Vector3(Mathf.Sin(index * 1.7f) * 0.09f, 0.03f, Mathf.Cos(index * 1.2f) * 0.08f) : Vector3.zero);
                die.Transform.localRotation = BowlDiceRotation(index) * Quaternion.Euler(lively ? 20f + index * 7f : 0f, lively ? index * 18f : 0f, lively ? -14f + index * 5f : 0f);
                die.BowlVelocity = Vector3.zero;
                die.UpdateHoldRing();
            }
        }

        private void SetCaptureDiceRolling(int[] values)
        {
            ParkBowl();
            var positions = CaptureTablePositions();
            for (var index = 0; index < dice.Count; index += 1)
            {
                var die = dice[index];
                die.IsHeld = false;
                die.SetValue(values[index % values.Length]);
                die.Transform.SetParent(null, true);
                die.Transform.position = positions[index] + new Vector3(0f, 0.25f + index * 0.03f, 0f);
                die.Transform.rotation = StableRotationForValue(die.Value, index * 31f) * Quaternion.Euler(18f + index * 5f, 0f, -12f + index * 3f);
                die.Rigidbody.isKinematic = false;
                die.Rigidbody.linearVelocity = new Vector3(0.4f - index * 0.08f, 0f, -0.5f + index * 0.13f);
                die.Rigidbody.angularVelocity = new Vector3(1.8f + index * 0.2f, 2.2f - index * 0.1f, 1.4f);
                die.UpdateHoldRing();
            }

            isResolvingRoll = true;
            stableSeconds = -1.2f;
        }

        private void SetCaptureDiceOnTable(int[] values, int heldCount = 0)
        {
            ParkBowl();
            var positions = CaptureTablePositions();
            for (var index = 0; index < dice.Count; index += 1)
            {
                var die = dice[index];
                die.IsHeld = index < heldCount;
                die.SetValue(values[index % values.Length]);
                die.Transform.SetParent(null, true);
                die.Transform.position = positions[index];
                die.Transform.rotation = StableRotationForValue(die.Value, index * 23f - 18f);
                die.Rigidbody.isKinematic = true;
                die.Rigidbody.linearVelocity = Vector3.zero;
                die.Rigidbody.angularVelocity = Vector3.zero;
                die.UpdateHoldRing();
            }

            ResolveSettledDiceLayout();
        }

        private static Vector3[] CaptureTablePositions()
        {
            return new[]
            {
                new Vector3(-1.6f, DiceRestY, 1.05f),
                new Vector3(-0.45f, DiceRestY, 1.38f),
                new Vector3(0.65f, DiceRestY, 1.06f),
                new Vector3(1.75f, DiceRestY, 1.42f),
                new Vector3(2.85f, DiceRestY, 1.08f)
            };
        }

        private void SeedRecordedScores(int count)
        {
            scores.Clear();
            var totals = new[] { 4, 8, 9, 12, 15, 18, 24, 25, 30, 40, 0, 22 };
            for (var index = 0; index < Mathf.Min(count, YachtRushRules.Categories.Length); index += 1)
            {
                var category = YachtRushRules.Categories[index];
                scores[category] = new ScoreRecord(totals[index], 0, totals[index]);
            }
        }

        private void SeedCompleteScores()
        {
            scores.Clear();
            var records = new[]
            {
                new ScoreRecord(3, 0, 3),
                new ScoreRecord(8, 0, 8),
                new ScoreRecord(9, 0, 9),
                new ScoreRecord(12, 0, 12),
                new ScoreRecord(15, 0, 15),
                new ScoreRecord(24, 10, 34),
                new ScoreRecord(28, 10, 38),
                new ScoreRecord(25, 0, 25),
                new ScoreRecord(30, 0, 30),
                new ScoreRecord(40, 0, 40),
                new ScoreRecord(0, 5, 5),
                new ScoreRecord(22, 0, 22)
            };

            for (var index = 0; index < YachtRushRules.Categories.Length; index += 1)
            {
                scores[YachtRushRules.Categories[index]] = records[index];
            }
        }

        private void ShowCaptureResult(int total, bool isNewBest)
        {
            routeProgress = YachtRushRules.HarborTargetRoute;
            bestScore = Mathf.Max(bestScore, routeProgress);
            windResource = Mathf.Max(windResource, 6);
            hull = Mathf.Max(hull, 12);
            supplies = Mathf.Max(supplies, 5);
            crewResource = Mathf.Max(crewResource, 4);
            contractScore = Mathf.Max(contractScore, 52);
            chartResource = Mathf.Max(chartResource, 4);
            if (resultTitleText != null)
            {
                resultTitleText.text = "New Best Voyage";
                resultTitleText.color = new Color32(62, 110, 65, 255);
            }

            resultScoreText.text = $"{routeProgress} nm";
            if (resultMetaText != null)
            {
                resultMetaText.text = $"Resources {ResourceStockSummary()}  Score {total}" +
                    (isNewBest ? "  New best distance" : $"  Best {bestScore} nm");
                resultMetaText.color = isNewBest ? new Color32(72, 116, 75, 255) : SketchPalette.MutedInk;
            }

            resultPanel.SetActive(true);
        }

        private void ToggleDieHold(DieView die)
        {
            if (!CanScore || isDraggingBowl)
            {
                return;
            }

            if (!YachtRushRules.CanHold(currentRollRule) ||
                (currentRushDie == YachtRushRushDie.Anchor && die.Index == rushDieIndex && die.IsHeld))
            {
                PlayAudioCue("hold", 0.22f);
                UpdateHudAndScores();
                return;
            }

            die.IsHeld = !die.IsHeld;
            PlayAudioCue(die.IsHeld ? "hold" : "grab", 0.42f);
            die.Rigidbody.isKinematic = true;
            die.Rigidbody.linearVelocity = Vector3.zero;
            die.Rigidbody.angularVelocity = Vector3.zero;
            die.Transform.SetParent(null, true);
            SnapDieToReadableRest(die);

            UpdateHudAndScores();
        }

        private void UpdateCaptainOrderButtons()
        {
            UpdateStrategyTokens();
            if (captainOrderPanel == null || captainOrderButtonLabels.Count == 0)
            {
                return;
            }

            captainOrderPanel.SetActive(false);
            return;
#pragma warning disable CS0162
            for (var index = 0; index < captainOrderButtonLabels.Count; index += 1)
            {
                captainOrderButtons[index].gameObject.SetActive(true);
                captainOrderButtons[index].interactable = hasPendingTurnResult &&
                    index < currentStrategyPreviews.Length &&
                    currentStrategyPreviews[index].IsAvailable;
                var image = captainOrderButtons[index].GetComponent<Image>();

                if (!hasPendingTurnResult)
                {
                    var idlePreview = YachtRushRules.PreviewVoyageStrategy(StrategyCatalog[index], new int[6]);
                    captainOrderButtonLabels[index].text = StrategyIdleCardText(idlePreview);
                    if (image != null)
                    {
                        image.color = MutedStockCellColor(PrimaryCostFace(idlePreview.ResourceCost));
                    }

                    UpdateStrategyCardBand(index, idlePreview, false, false);

                    continue;
                }

                if (index >= currentStrategyPreviews.Length)
                {
                    captainOrderButtons[index].gameObject.SetActive(false);
                    continue;
                }

                var preview = currentStrategyPreviews[index];
                captainOrderButtonLabels[index].text = StrategyPreviewCardText(preview, index == 0 && preview.IsAvailable);
                if (image != null)
                {
                    image.color = StrategyCardColor(preview, index);
                }

                UpdateStrategyCardBand(index, preview, preview.IsAvailable, index == 0 && preview.IsAvailable);
            }
#pragma warning restore CS0162
        }

        private void UpdateStrategyTokens()
        {
            if (commandTokens.Count == 0)
            {
                return;
            }

            var bestStrategy = BestOpenStrategy();
            foreach (var item in commandTokens)
            {
                var category = item.Key;
                var token = item.Value;
                var strategy = StrategyForTokenCategory(category);
                var preview = FindStrategyPreview(strategy);
                var isUsed = IsLimitedStrategyUsed(strategy);
                var isBest = !isUsed && bestStrategy.HasValue && bestStrategy.Value == strategy;
                var isOpen = hasPendingTurnResult && preview.IsAvailable && !isUsed;
                var face = PrimaryCostFace(preview.ResourceCost);
                var baseColor = isBest
                    ? (Color)new Color32(255, 249, 224, 255)
                    : isOpen
                        ? (Color)new Color32(255, 253, 246, 255)
                        : (Color)new Color32(234, 231, 219, 255);
                var accentColor = isBest
                    ? (Color)new Color32(255, 245, 205, 255)
                    : isOpen
                        ? (Color)new Color32(252, 248, 236, 255)
                        : (Color)new Color32(224, 221, 211, 255);
                var statusColor = isBest
                    ? (Color)new Color32(214, 149, 34, 255)
                    : isOpen
                        ? (Color)new Color32(76, 132, 82, 255)
                        : (Color)new Color32(135, 130, 118, 255);

                token.Background.material.color = baseColor;
                if (token.IconBacking != null)
                {
                    token.IconBacking.material.color = accentColor;
                }

                if (token.StatusStrip != null)
                {
                    token.StatusStrip.material.color = statusColor;
                }

                token.Root.gameObject.SetActive(resultPanel == null || !resultPanel.activeSelf);
                token.Root.localScale = Vector3.one * (isBest ? 1.06f : isOpen ? 1.02f : 1f);
                token.NameText.text = ShortStrategyName(preview.Name);
                token.DetailText.text = string.Empty;
                token.ValueText.text = string.Empty;
                token.TagText.text = isUsed ? "USED" : isBest ? "BEST" : isOpen ? "OPEN" : "LOCKED";
                token.NameText.color = SketchPalette.Ink;
                token.DetailText.color = Color.clear;
                token.ValueText.color = Color.clear;
                token.TagText.color = isUsed
                    ? new Color32(105, 98, 84, 255)
                    : isBest
                    ? new Color32(132, 76, 17, 255)
                    : isOpen
                        ? new Color32(45, 104, 51, 255)
                        : new Color32(105, 98, 84, 255);
            }
        }

        private VoyageStrategyPreview FindStrategyPreview(VoyageStrategy strategy)
        {
            if (hasPendingTurnResult)
            {
                foreach (var preview in currentStrategyPreviews)
                {
                    if (preview.Strategy == strategy)
                    {
                        return preview;
                    }
                }
            }

            return YachtRushRules.PreviewVoyageStrategy(strategy, CurrentResourceCounts());
        }

        private VoyageStrategy? BestOpenStrategy()
        {
            if (!hasPendingTurnResult)
            {
                return null;
            }

            foreach (var preview in currentStrategyPreviews)
            {
                if (preview.IsAvailable && !IsLimitedStrategyUsed(preview.Strategy))
                {
                    return preview.Strategy;
                }
            }

            return null;
        }

        private static VoyageStrategy StrategyForTokenCategory(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return VoyageStrategy.TailwindRun;
                case YachtRushCategory.Twos:
                    return VoyageStrategy.StockTheHold;
                case YachtRushCategory.Threes:
                    return VoyageStrategy.PatchTheHull;
                case YachtRushCategory.Fours:
                    return VoyageStrategy.RallyTheCrew;
                case YachtRushCategory.Fives:
                    return VoyageStrategy.PortBargain;
                case YachtRushCategory.Sixes:
                    return VoyageStrategy.ReadTheStars;
                case YachtRushCategory.FourOfAKind:
                    return VoyageStrategy.SafePassage;
                case YachtRushCategory.FullHouse:
                    return VoyageStrategy.RepairConvoy;
                case YachtRushCategory.SmallStraight:
                    return VoyageStrategy.LongVoyage;
                case YachtRushCategory.LargeStraight:
                    return VoyageStrategy.TradeRoute;
                case YachtRushCategory.Yacht:
                    return VoyageStrategy.FullDeck;
                case YachtRushCategory.Chance:
                    return VoyageStrategy.CaptainsGambit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        private static string ShortStrategyName(string name)
        {
            switch (name)
            {
                case "Tailwind Run":
                    return "Tailwind";
                case "Patch the Hull":
                    return "Patch Hull";
                case "Stock the Hold":
                    return "Stock Hold";
                case "Rally the Crew":
                    return "Crew Rally";
                case "Port Bargain":
                    return "Port Deal";
                case "Read the Stars":
                    return "Star Map";
                case "Captain's Gambit":
                    return "Gambit";
                default:
                    return name;
            }
        }

        private static string StrategyTokenValue(VoyageStrategyPreview preview)
        {
            if (preview.DistanceDelta != 0)
            {
                return $"+{preview.DistanceDelta}";
            }

            if (preview.HullDelta != 0)
            {
                return $"H+{preview.HullDelta}";
            }

            if (preview.SupplyDelta != 0)
            {
                return $"F+{preview.SupplyDelta}";
            }

            if (preview.GoldDelta != 0)
            {
                return $"G+{preview.GoldDelta}";
            }

            return "GO";
        }

        private static string MissingResourceLine(VoyageStrategyPreview preview)
        {
            if (preview.IsAvailable)
            {
                return "Status: enough resources to choose this token.";
            }

            return $"Missing: {preview.Condition}";
        }

        private static string ResourceRoleLine(IReadOnlyList<int> cost)
        {
            if (cost == null || cost.Count != 6)
            {
                return "Resource role: choose a plan to turn resources into voyage progress.";
            }

            var roles = new List<string>(4);
            if (cost[0] > 0)
            {
                roles.Add("SAIL -> Distance");
            }

            if (cost[1] > 0)
            {
                roles.Add("HULL -> survival / repair");
            }

            if (cost[2] > 0)
            {
                roles.Add("FOOD -> survival / longer routes");
            }

            if (cost[3] > 0)
            {
                roles.Add("CREW -> combo support");
            }

            if (cost[4] > 0)
            {
                roles.Add("GOLD -> score / port value");
            }

            if (cost[5] > 0)
            {
                roles.Add("MAP -> special routes");
            }

            return roles.Count == 0
                ? "Resource role: fallback command from current dice."
                : "This token converts: " + string.Join("  |  ", roles);
        }

        private static string StrategyRationale(VoyageStrategy strategy)
        {
            switch (strategy)
            {
                case VoyageStrategy.TailwindRun:
                    return "Two sail results mean the crew can commit to speed.";
                case VoyageStrategy.PatchTheHull:
                    return "Hull becomes spare boards and repair labor.";
                case VoyageStrategy.StockTheHold:
                    return "Food keeps the voyage alive and pays for longer routes.";
                case VoyageStrategy.RallyTheCrew:
                    return "Crew turns mixed resources into stronger combo plans.";
                case VoyageStrategy.PortBargain:
                    return "Gold turns into port leverage, score, and extra stores.";
                case VoyageStrategy.ReadTheStars:
                    return "Map opens safer and longer routes.";
                case VoyageStrategy.SafePassage:
                    return "Sail, hull, and map together make a cautious route.";
                case VoyageStrategy.LongVoyage:
                    return "Sail, food, and map combine into a longer push.";
                case VoyageStrategy.RepairConvoy:
                    return "Hull, food, and crew support a repair-heavy month.";
                case VoyageStrategy.TradeRoute:
                    return "Food, gold, and map point toward profitable routes.";
                case VoyageStrategy.FullDeck:
                    return "Many resource types let the ship run as a complete plan.";
                case VoyageStrategy.CaptainsGambit:
                    return "Three of one resource lets the captain force a bold order.";
                default:
                    return "The crew turns resources into a voyage command.";
            }
        }

        private void UpdateStrategyCardBand(int index, VoyageStrategyPreview preview, bool isOpen, bool isPick)
        {
            if (index < 0 || index >= captainOrderArtPanels.Count)
            {
                return;
            }

            var face = PrimaryCostFace(preview.ResourceCost);
            var bandColor = isPick
                ? (Color)new Color32(103, 72, 24, 255)
                : isOpen
                    ? StrongStockColor(face)
                    : DimStockColor(face);
            captainOrderArtPanels[index].color = bandColor;

            if (index < captainOrderArtLabels.Count && captainOrderArtLabels[index] != null)
            {
                captainOrderArtLabels[index].text = isPick ? "BEST OPEN" : isOpen ? "OPEN" : "LOCKED";
            }
        }

        private static string StrategyIdleCardText(VoyageStrategyPreview preview)
        {
            return $"<size=16><b>{preview.Name}</b></size>\n" +
                $"<size=12>{preview.Condition}</size>\n" +
                $"<size=12>{StrategyEffectFormula(preview)}</size>";
        }

        private static string StrategyPreviewCardText(VoyageStrategyPreview preview, bool isPick)
        {
            var effect = StrategyEffectFormula(preview);
            var state = isPick ? "PICK" : preview.IsAvailable ? "OPEN" : "LOCKED";
            var stateColor = isPick ? "#72510F" : preview.IsAvailable ? "#3E6E41" : "#665F55";
            var requirement = preview.IsAvailable
                ? $"Spend {ResourceCostSummary(preview.ResourceCost)}"
                : preview.Condition;
            return $"<size=16><b>{preview.Name}</b></size>  <size=10><color={stateColor}>{state}</color></size>\n" +
                $"<size=12>{requirement}</size>\n" +
                $"<size=13>{effect}</size>";
        }

        private static string CondensedHaveLine(string have)
        {
            if (string.IsNullOrEmpty(have) || have == "No resources")
            {
                return "Have none";
            }

            return have.Replace("  ", " ");
        }

        private static string ResourceCostSummary(IReadOnlyList<int> cost)
        {
            if (cost == null || cost.Count != 6)
            {
                return "none";
            }

            var parts = new List<string>(6);
            for (var face = 1; face <= 6; face += 1)
            {
                var amount = cost[face - 1];
                if (amount > 0)
                {
                    parts.Add($"{ResourceCode(face)} x{amount}");
                }
            }

            return parts.Count == 0 ? "none" : string.Join(", ", parts);
        }

        private static string StrategyEffectFormula(VoyageStrategyPreview preview)
        {
            var parts = new List<string>(6);
            if (preview.DistanceDelta != 0)
            {
                parts.Add($"{Signed(preview.DistanceDelta)} nm");
            }

            AppendSigned(parts, "HULL", preview.HullDelta);
            AppendSigned(parts, "FOOD", preview.SupplyDelta);
            AppendSigned(parts, "GOLD", preview.GoldDelta);

            return parts.Count == 0 ? "hold" : string.Join("  ", parts);
        }

        private static string ResourceCode(int face)
        {
            switch (face)
            {
                case 1:
                    return "SAIL";
                case 2:
                    return "HULL";
                case 3:
                    return "FOOD";
                case 4:
                    return "CREW";
                case 5:
                    return "GOLD";
                case 6:
                    return "MAP";
                default:
                    return "?";
            }
        }

        private static Color StrategyCardColor(VoyageStrategyPreview preview, int index)
        {
            if (preview.IsAvailable && index == 0)
            {
                return new Color32(255, 236, 162, 255);
            }

            var primaryFace = PrimaryCostFace(preview.ResourceCost);
            var baseColor = preview.IsAvailable
                ? StockCellColor(primaryFace)
                : MutedStockCellColor(primaryFace);
            return new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        }

        private static Color StrategyTokenOpenColor(int face)
        {
            var strong = StrongStockColor(face);
            return new Color(
                Mathf.Lerp(strong.r, 1f, 0.28f),
                Mathf.Lerp(strong.g, 1f, 0.28f),
                Mathf.Lerp(strong.b, 1f, 0.28f),
                1f);
        }

        private static Color StrategyAccentColor(VoyageStrategyPreview preview)
        {
            return StrongStockColor(PrimaryCostFace(preview.ResourceCost));
        }

        private static Color MutedTokenAccentColor(int face)
        {
            var strong = StrongStockColor(face);
            return new Color(
                Mathf.Lerp(strong.r, 1f, 0.48f),
                Mathf.Lerp(strong.g, 1f, 0.48f),
                Mathf.Lerp(strong.b, 1f, 0.48f),
                1f);
        }

        private static Color LockedTokenAccentColor(int face)
        {
            var muted = MutedStockCellColor(face);
            return new Color(
                Mathf.Lerp(muted.r, 0.62f, 0.18f),
                Mathf.Lerp(muted.g, 0.62f, 0.18f),
                Mathf.Lerp(muted.b, 0.62f, 0.18f),
                1f);
        }

        private static int PrimaryCostFace(IReadOnlyList<int> cost)
        {
            if (cost == null || cost.Count != 6)
            {
                return 0;
            }

            for (var face = 1; face <= 6; face += 1)
            {
                if (cost[face - 1] > 0)
                {
                    return face;
                }
            }

            return 0;
        }

        private static Color StockCellColor(int face)
        {
            switch (face)
            {
                case 1:
                    return new Color32(255, 244, 181, 255);
                case 2:
                    return new Color32(219, 235, 246, 255);
                case 3:
                    return new Color32(219, 240, 221, 255);
                case 4:
                    return new Color32(247, 224, 218, 255);
                case 5:
                    return new Color32(255, 232, 151, 255);
                case 6:
                    return new Color32(219, 242, 244, 255);
                default:
                    return new Color32(248, 245, 232, 255);
            }
        }

        private static Color MutedStockCellColor(int face)
        {
            switch (face)
            {
                case 1:
                    return new Color32(246, 241, 213, 255);
                case 2:
                    return new Color32(232, 239, 244, 255);
                case 3:
                    return new Color32(232, 242, 233, 255);
                case 4:
                    return new Color32(245, 234, 231, 255);
                case 5:
                    return new Color32(247, 239, 216, 255);
                case 6:
                    return new Color32(231, 243, 244, 255);
                default:
                    return new Color32(224, 221, 211, 255);
            }
        }

        private static Color StrongStockColor(int face)
        {
            switch (face)
            {
                case 1:
                    return new Color32(184, 156, 35, 255);
                case 2:
                    return new Color32(63, 133, 178, 255);
                case 3:
                    return new Color32(59, 150, 72, 255);
                case 4:
                    return new Color32(184, 82, 67, 255);
                case 5:
                    return new Color32(178, 127, 25, 255);
                case 6:
                    return new Color32(46, 148, 150, 255);
                default:
                    return new Color32(82, 76, 66, 255);
            }
        }

        private static Color DimStockColor(int face)
        {
            switch (face)
            {
                case 1:
                    return new Color32(178, 166, 103, 255);
                case 2:
                    return new Color32(128, 154, 170, 255);
                case 3:
                    return new Color32(126, 166, 130, 255);
                case 4:
                    return new Color32(176, 133, 127, 255);
                case 5:
                    return new Color32(174, 150, 91, 255);
                case 6:
                    return new Color32(117, 165, 166, 255);
                default:
                    return new Color32(116, 109, 97, 255);
            }
        }

        private string ResourceStockSummary()
        {
            return ResourceCostSummary(CurrentResourceCounts());
        }

        private bool IsLimitedStrategyUsed(VoyageStrategy strategy)
        {
            return IsLimitedVoyageStrategy(strategy) && usedLimitedStrategies.Contains(strategy);
        }

        private static bool IsLimitedVoyageStrategy(VoyageStrategy strategy)
        {
            switch (strategy)
            {
                case VoyageStrategy.SafePassage:
                case VoyageStrategy.LongVoyage:
                case VoyageStrategy.RepairConvoy:
                case VoyageStrategy.TradeRoute:
                case VoyageStrategy.FullDeck:
                case VoyageStrategy.CaptainsGambit:
                    return true;
                default:
                    return false;
            }
        }

        private static string StrategyUseLimitLine(VoyageStrategy strategy)
        {
            return IsLimitedVoyageStrategy(strategy)
                ? "LIMITED strategy: powerful combo plan. Use once during this 12-month voyage."
                : "BASIC command: repeatable monthly plan.";
        }

        private string StrategyUsedLine(VoyageStrategy strategy)
        {
            return IsLimitedStrategyUsed(strategy)
                ? "Already used: this combo cannot be chosen again.\n\n"
                : string.Empty;
        }

        private static string ResourceStockStatusLabel(int face, int count)
        {
            if (count >= 2)
            {
                switch (face)
                {
                    case 1:
                        return "DIST READY";
                    case 2:
                        return "SURVIVE READY";
                    case 3:
                        return "SURVIVE READY";
                    case 4:
                        return "COMBO READY";
                    case 5:
                        return "SCORE READY";
                    case 6:
                        return "SPECIAL READY";
                }
            }

            if (count == 1)
            {
                return "SAVED x1";
            }

            switch (face)
            {
                case 1:
                    return "DIST LOW";
                case 2:
                    return "HULL RISK";
                case 3:
                    return "FOOD RISK";
                case 4:
                    return "COMBO LOCKED";
                case 5:
                    return "SCORE LOW";
                case 6:
                    return "SPECIAL LOCKED";
                default:
                    return "NO STOCK";
            }
        }

        private static string ResourceGuideText(int face, int current)
        {
            return $"Current resource: {ResourceCode(face)} x{current}\n\n" +
                "Gain\n" +
                $"{ResourceGainLine(face)}\n\n" +
                "Spend\n" +
                $"{ResourceSpendLine(face)}\n\n" +
                "Enough resource\n" +
                $"{ResourceBenefitLine(face)}\n\n" +
                "Shortage impact\n" +
                $"{ResourcePenaltyLine(face)}\n\n" +
                "Exchange\n" +
                "No free exchange. Spend resources only through deck tokens.";
        }

        private static string ResourceGainLine(int face)
        {
            return $"Roll a {face}. Each matching die adds +1 {ResourceCode(face)}.";
        }

        private static string ResourceSpendLine(int face)
        {
            switch (face)
            {
                case 1:
                    return "Spent by Tailwind Run, Safe Passage, Long Voyage, and route plans.";
                case 2:
                    return "Spent by Patch the Hull, Safe Passage, and Repair Convoy.";
                case 3:
                    return "Spent by Stock the Hold, Long Voyage, Repair Convoy, and Trade Route.";
                case 4:
                    return "Spent by Rally the Crew, Repair Convoy, Full Deck, and Captain's Gambit.";
                case 5:
                    return "Spent by Port Bargain, Trade Route, Full Deck, and Captain's Gambit.";
                case 6:
                    return "Spent by Read the Stars, Safe Passage, Long Voyage, and map plans.";
                default:
                    return "Spent by deck tokens.";
            }
        }

        private static string ResourceBenefitLine(int face)
        {
            switch (face)
            {
                case 1:
                    return "Distance plans open. More SAIL usually means a better distance score.";
                case 2:
                    return "Repair plans open and the voyage stays alive while HULL is above 0.";
                case 3:
                    return "Supply plans and longer routes open. The voyage stays alive while FOOD is above 0.";
                case 4:
                    return "Crew combo plans open, including mixed and limited strategies.";
                case 5:
                    return "Port plans convert stored value into Gold score and trade rewards.";
                case 6:
                    return "Special route plans open, including safer and longer voyages.";
                default:
                    return "More resource opens stronger plans.";
            }
        }

        private static string ResourcePenaltyLine(int face)
        {
            switch (face)
            {
                case 1:
                    return "No direct damage, but distance scoring stays low because SAIL plans are weak or locked.";
                case 2:
                    return "Survival risk: HULL-spending plans are harder to choose without sinking.";
                case 3:
                    return "Survival risk: FOOD-spending plans are harder to choose without starving.";
                case 4:
                    return "Strategy lock: mixed crew combo plans stay unavailable.";
                case 5:
                    return "Score pressure: port trade and Gold scoring plans stay weak or locked.";
                case 6:
                    return "Strategy lock: special safe-route plans stay unavailable.";
                default:
                    return "Important plans stay locked.";
            }
        }

        private void PulseResourceStations(IReadOnlyList<int> resourceCounts)
        {
            for (var face = 1; face <= 6; face += 1)
            {
                var count = resourceCounts != null && resourceCounts.Count >= face ? resourceCounts[face - 1] : 0;
                if (resourceStockTexts.TryGetValue(face, out var stockText) && stockText != null)
                {
                    stockText.text = count.ToString();
                    stockText.color = count > 0 ? SketchPalette.Ink : SketchPalette.MutedInk;
                }

                if (resourceStockStatusTexts.TryGetValue(face, out var statusText) && statusText != null)
                {
                    statusText.text = ResourceStockStatusLabel(face, count);
                    statusText.color = count >= 2
                        ? StrongStockColor(face)
                        : count == 1
                            ? SketchPalette.MutedInk
                            : new Color32(146, 76, 57, 255);
                }

                if (resourceStationCountTexts.TryGetValue(face, out var countText) && countText != null)
                {
                    countText.text = count.ToString();
                    countText.color = count > 0 ? VoyageFaceColor(face) : SketchPalette.MutedInk;
                    countText.gameObject.SetActive(hasPendingTurnResult || count > 0);
                }

                if (resourceStations.TryGetValue(face, out var station))
                {
                    if (count > 0)
                    {
                        station.Pulse(0.85f, VoyageFaceColor(face));
                    }
                }
            }
        }

        private static string ResourceRollSummary(IReadOnlyList<int> resourceCounts)
        {
            if (resourceCounts == null || resourceCounts.Count != 6)
            {
                return "Roll dice to gather this month's resources.";
            }

            var parts = new List<string>(6);
            for (var face = 1; face <= 6; face += 1)
            {
                if (resourceCounts[face - 1] > 0)
                {
                    parts.Add($"{YachtRushRules.CrewResourceName(face)} x{resourceCounts[face - 1]}");
                }
            }

            return "Choose an OPEN strategy.";
        }

        private void PulseLandingZones(IEnumerable<VoyageDieLanding> landings)
        {
            foreach (var item in voyageZoneRenderers)
            {
                if (item.Value != null)
                {
                    item.Value.transform.localScale = new Vector3(item.Value.transform.localScale.x, 0.025f, item.Value.transform.localScale.z);
                }
            }

            foreach (var zone in landings.Select(landing => landing.Zone).Distinct())
            {
                if (voyageZoneRenderers.TryGetValue(zone, out var renderer) && renderer != null)
                {
                    renderer.transform.localScale = new Vector3(renderer.transform.localScale.x, 0.05f, renderer.transform.localScale.z);
                }
            }
        }

        private static string LandingSummary(IEnumerable<VoyageDieLanding> landings)
        {
            var groups = landings
                .GroupBy(landing => landing.Zone)
                .OrderBy(group => group.Key == VoyageDeckZone.Overboard ? 99 : (int)group.Key)
                .Select(ZoneReadingText);
            return string.Join("  |  ", groups) + "  -> Pick one Captain Order";
        }

        private static string ZoneReadingText(IGrouping<VoyageDeckZone, VoyageDieLanding> group)
        {
            var sum = group.Sum(landing => landing.Value);
            switch (group.Key)
            {
                case VoyageDeckZone.Sail:
                    return $"Sail +{sum} Distance";
                case VoyageDeckZone.Repair:
                    return $"Repair +{group.Sum(landing => Mathf.Max(1, landing.Value / 2))} Hull";
                case VoyageDeckZone.Supply:
                    return $"Supply +{group.Sum(landing => Mathf.Max(1, landing.Value / 2)) + group.Count()} Stores";
                case VoyageDeckZone.Trade:
                    return $"Trade +{sum} Discovery";
                case VoyageDeckZone.Storm:
                    return $"Storm -{group.Sum(landing => Mathf.Max(1, (landing.Value + 1) / 2))} Hull";
                case VoyageDeckZone.Overboard:
                    return $"Overboard -{group.Count()} Dice";
                default:
                    return $"Deck {sum}";
            }
        }

        private static string ZoneLabel(VoyageDeckZone zone)
        {
            switch (zone)
            {
                case VoyageDeckZone.Sail:
                    return "Sail";
                case VoyageDeckZone.Repair:
                    return "Repair";
                case VoyageDeckZone.Supply:
                    return "Supply";
                case VoyageDeckZone.Trade:
                    return "Trade";
                case VoyageDeckZone.Storm:
                    return "Storm";
                case VoyageDeckZone.Overboard:
                    return "Overboard";
                default:
                    return "Deck";
            }
        }

        private void UpdateHudAndScores()
        {
            roundText.text = $"{RoundNumber}/12";
            rollText.text = $"{routeProgress} nm";
            bestText.text = $"{bestScore} nm";
            rollText.color = routeProgress > bestScore ? new Color32(62, 110, 65, 255) : SketchPalette.Ink;
            bestText.color = bestScore <= 0 ? SketchPalette.MutedInk : SketchPalette.Ink;
            if (harborRouteFill != null)
            {
                var fill = Mathf.Clamp01(routeProgress / (float)YachtRushRules.HarborTargetRoute);
                harborRouteFill.rectTransform.anchorMax = new Vector2(fill, 1f);
                harborRouteFill.rectTransform.offsetMax = Vector2.zero;
                if (harborMapRouteFill != null)
                {
                    harborMapRouteFill.rectTransform.anchorMax = new Vector2(fill, 1f);
                    harborMapRouteFill.rectTransform.offsetMax = Vector2.zero;
                }

                if (harborYachtMarker != null && harborMapTrackRect != null)
                {
                    harborYachtMarker.anchorMin = new Vector2(fill, 0.5f);
                    harborYachtMarker.anchorMax = new Vector2(fill, 0.5f);
                    harborYachtMarker.offsetMin = new Vector2(-11f, -8f);
                    harborYachtMarker.offsetMax = new Vector2(11f, 8f);
                }

                if (voyageShipMarker != null)
                {
                    voyageShipMarker.localPosition = Vector3.Lerp(
                        new Vector3(-4.06f, 0.14f, 3.58f),
                        new Vector3(4.08f, 0.14f, 3.58f),
                        fill);
                    voyageShipMarker.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-8f, 12f, fill), 0f);
                }
            }

            if (harborRouteText != null)
            {
                harborRouteText.text = $"Start Port -> {routeProgress} nm sailed -> open record";
            }

            if (voyageStatusText != null)
            {
                voyageStatusText.text = TurnPrompt();
            }

            UpdateCaptainOrderButtons();
        }

        private void UpdateCommandToken(
            YachtRushCategory category,
            HarborYachtActionInfo action,
            ScoreRecord record,
            HarborYachtActionEffect harborEffect,
            bool isUsed,
            bool isBestPreview,
            bool rushChanged)
        {
            if (!commandTokens.TryGetValue(category, out var token))
            {
                return;
            }

            var canSelect = CanScore && !isUsed && harborEffect.IsAvailable;
            token.NameText.text = CommandTokenTitle(category);
            token.DetailText.text = isUsed
                ? ShortRecordedEffect(record)
                : !CanScore || !harborEffect.IsAvailable
                    ? TokenConditionGlyph(category)
                    : TokenPreviewLine(harborEffect);
            token.ValueText.text = isUsed
                ? "DONE"
                : canSelect
                    ? VoyageCommandBadgeText(harborEffect)
                    : string.Empty;
            token.TagText.text = isUsed
                ? "COMPLETED"
                : !CanScore
                    ? string.Empty
                    : !harborEffect.IsAvailable
                        ? "NEED"
                        : isBestPreview
                            ? "BEST"
                            : CommandTagText(harborEffect);

            var muted = new Color32(102, 98, 86, 255);
            var locked = new Color32(116, 105, 91, 255);
            var active = new Color32(42, 38, 32, 255);
            token.NameText.color = isUsed ? muted : active;
            token.DetailText.color = !canSelect ? locked : new Color32(76, 68, 56, 255);
            token.ValueText.color = isUsed ? muted : active;
            token.TagText.color = isBestPreview
                ? new Color32(62, 110, 65, 255)
                : canSelect
                    ? TokenStateColor(harborEffect, rushChanged)
                    : muted;

            token.Background.material.color = isUsed
                ? new Color32(224, 224, 210, 245)
                : isBestPreview
                    ? new Color32(255, 240, 181, 255)
                    : CanScore && !harborEffect.IsAvailable
                        ? new Color32(236, 226, 202, 245)
                        : canSelect
                            ? new Color32(250, 246, 226, 255)
                            : new Color32(246, 236, 211, 245);
            token.Root.localScale = Vector3.one * (isBestPreview ? 1.04f : 1f);
        }

        private void UpdateCommandTooltip()
        {
            if (commandTooltipPanel == null)
            {
                return;
            }

            commandTooltipPanel.SetActive(false);
        }

        private void ShowCommandTooltip(YachtRushCategory category)
        {
            if (commandTooltipPanel == null)
            {
                return;
            }

            var action = YachtRushRules.GetHarborAction(category);
            var values = CurrentDiceValues();
            var score = YachtRushRules.PreviewScore(
                category,
                currentContract,
                currentRollRule,
                currentRushDie,
                rushDieIndex,
                values,
                Mathf.Max(0, rollCount - 1),
                lockedBeforeFinalThrow,
                HeldCount);
            var effect = YachtRushRules.PreviewHarborAction(category, score, currentRushDie);

            commandTooltipTitleText.text = action.Name;
            commandTooltipDetailText.text = $"Need: {action.Pattern}";
            commandTooltipEffectText.text = effect.IsAvailable
                ? $"{CommandRationale(category)}  {effect.Summary}"
                : $"Locked: {effect.LockedReason}";
            commandTooltipEffectText.color = effect.IsAvailable ? SketchPalette.Ink : new Color32(154, 82, 58, 255);
            commandTooltipPanel.SetActive(true);
        }

        private void ShowBestCommandReader()
        {
            var values = CurrentDiceValues();
            var bestCategory = YachtRushRules.Categories
                .Where(category => !scores.ContainsKey(category))
                .Select(category =>
                {
                    var preview = YachtRushRules.PreviewScore(
                        category,
                        currentContract,
                        currentRollRule,
                        currentRushDie,
                        rushDieIndex,
                        values,
                        Mathf.Max(0, rollCount - 1),
                        lockedBeforeFinalThrow,
                        HeldCount);
                    var effect = YachtRushRules.PreviewHarborAction(category, preview, currentRushDie);
                    return new { Category = category, Effect = effect };
                })
                .Where(item => item.Effect.IsAvailable)
                .OrderByDescending(item => EffectPriority(item.Effect))
                .FirstOrDefault();

            if (bestCategory == null)
            {
                ShowCommandReaderIdle();
                return;
            }

            ShowCommandTooltip(bestCategory.Category);
        }

        private void ShowCommandReaderIdle()
        {
            if (commandTooltipPanel == null)
            {
                return;
            }

            commandTooltipPanel.SetActive(false);
        }

        private void ApplyBowlOverlapToCommandTokens()
        {
            if (bowlRoot == null || commandTokens.Count == 0)
            {
                return;
            }

            foreach (var token in commandTokens.Values)
            {
                token.Root.gameObject.SetActive(resultPanel == null || !resultPanel.activeSelf);
            }
        }

        private bool IsCommandTokenCoveredByBowl(CommandTokenView token)
        {
            var bowlPosition = bowlRoot.localPosition;
            var tokenPosition = token.Root.localPosition;
            return Mathf.Abs(bowlPosition.x - tokenPosition.x) < BowlRadiusX * 1.35f &&
                Mathf.Abs(bowlPosition.z - tokenPosition.z) < BowlRadiusZ * 1.55f;
        }

        private string ContractStateLabel(YachtRushContract contract)
        {
            if (!CanScore)
            {
                return "TARGET";
            }

            var values = CurrentDiceValues();
            var preview = YachtRushRules.PreviewScore(
                YachtRushCategory.Chance,
                contract,
                currentRollRule,
                currentRushDie,
                rushDieIndex,
                values,
                Mathf.Max(0, rollCount - 1),
                lockedBeforeFinalThrow,
                HeldCount);
            return preview.ContractSatisfied ? "READY" : "PENDING";
        }

        private void ShowRushIntroCue()
        {
            if (rushIntroText == null)
            {
                return;
            }

            if (currentRushDie == YachtRushRushDie.None)
            {
                PulseDeckAsset(DeckAssetKind.Compass, 0.9f);
                PulseDeckAsset(DeckAssetKind.Sail, 0.9f);
                ShowRushCue("CREW RESOURCE ROLL", RushIntroSeconds);
                return;
            }

            PulseDeckAsset(HazardDeckAsset(currentRushDie), 1.15f);
            ShowRushCue($"{YachtRushRules.GetRushDie(currentRushDie).Name.ToUpperInvariant()}!", RushIntroSeconds);
        }

        private void ShowRushResultCue()
        {
            if (rushIntroText == null || currentRushDie == YachtRushRushDie.None || rushDieIndex < 0 || rushDieIndex >= dice.Count)
            {
                return;
            }

            var dieNumber = rushDieIndex + 1;
            var value = dice[rushDieIndex].Value;
            switch (currentRushDie)
            {
                case YachtRushRushDie.Anchor:
                    PulseDeckAsset(DeckAssetKind.Anchor, 0.9f);
                    ShowRushCue($"ANCHOR LOCKED DIE {dieNumber}", 0.95f);
                    break;
                case YachtRushRushDie.Storm:
                    PulseDeckAsset(DeckAssetKind.Storm, 0.9f);
                    ShowRushCue("STORM TOSS", 0.85f);
                    break;
                case YachtRushRushDie.Cracked:
                    PulseDeckAsset(DeckAssetKind.Cargo, 0.9f);
                    ShowRushCue("CARGO CRACKED", 0.95f);
                    break;
                case YachtRushRushDie.Mirror:
                    PulseDeckAsset(DeckAssetKind.Compass, 0.9f);
                    ShowRushCue($"CURRENT {value} -> {7 - value}", 1.05f);
                    break;
                case YachtRushRushDie.Blank:
                    PulseDeckAsset(DeckAssetKind.Storm, 0.75f);
                    ShowRushCue($"FOG BLANKS DIE {dieNumber}", 1.05f);
                    break;
            }
        }

        private void ShowRushCue(string message, float seconds)
        {
            rushIntroTimer = Mathf.Max(0.1f, seconds);
            rushIntroText.text = message;
            rushIntroText.gameObject.SetActive(true);
            UpdateRushIntroCue();
        }

        private void UpdateRushIntroCue()
        {
            if (rushIntroText == null || rushIntroTimer <= 0f)
            {
                return;
            }

            rushIntroTimer -= Time.deltaTime;
            var progress = Mathf.Clamp01(1f - rushIntroTimer / RushIntroSeconds);
            var alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((progress - 0.58f) / 0.42f));
            var accent = RushDieAccentColor(currentRushDie);
            rushIntroText.color = new Color(accent.r, accent.g, accent.b, alpha);
            rushIntroText.rectTransform.localScale = Vector3.one * (1.08f - Mathf.Sin(progress * Mathf.PI) * 0.08f);

            if (rushIntroTimer <= 0f)
            {
                rushIntroText.gameObject.SetActive(false);
                rushIntroText.rectTransform.localScale = Vector3.one;
            }
        }

        private void ShowRunGoalToast()
        {
            if (runGoalToastText == null || hasShownRunGoalToast)
            {
                return;
            }

            hasShownRunGoalToast = true;
            runGoalToastTimer = 2f;
            runGoalToastText.color = SketchPalette.Ink;
            runGoalToastText.gameObject.SetActive(true);
        }

        private void ShowHarborFeedback(HarborYachtActionEffect effect)
        {
            var summary = CompactHarborEffectText(effect);
            if (string.IsNullOrEmpty(summary))
            {
                summary = effect.Summary;
            }

            if (harborFeedbackText == null || string.IsNullOrEmpty(summary))
            {
                return;
            }

            harborFeedbackTimer = 1.25f;
            harborPulseTimer = effect.RouteDelta > 0 ? 0.75f : Mathf.Max(harborPulseTimer, 0.24f);
            harborFeedbackText.text = summary;
            harborFeedbackText.color = effect.HazardDelta < 0
                ? new Color32(154, 82, 58, 255)
                : effect.ContractScoreDelta > 0
                    ? new Color32(62, 110, 65, 255)
                    : SketchPalette.Ink;
            harborFeedbackText.rectTransform.localScale = Vector3.one * 1.06f;
            harborFeedbackText.gameObject.SetActive(true);
        }

        private void UpdateHarborUiEffects()
        {
            if (harborFeedbackText != null && harborFeedbackTimer > 0f)
            {
                harborFeedbackTimer -= Time.deltaTime;
                var alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(harborFeedbackTimer / 0.32f));
                var color = harborFeedbackText.color;
                color.a = alpha;
                harborFeedbackText.color = color;
                harborFeedbackText.rectTransform.localScale = Vector3.one * (1f + Mathf.Clamp01(harborFeedbackTimer) * 0.06f);
                if (harborFeedbackTimer <= 0f)
                {
                    harborFeedbackText.gameObject.SetActive(false);
                    harborFeedbackText.rectTransform.localScale = Vector3.one;
                }
            }

            if (runGoalToastText != null && runGoalToastTimer > 0f)
            {
                runGoalToastTimer -= Time.deltaTime;
                var alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(runGoalToastTimer / 0.45f));
                var color = runGoalToastText.color;
                color.a = alpha;
                runGoalToastText.color = color;
                if (runGoalToastTimer <= 0f)
                {
                    runGoalToastText.gameObject.SetActive(false);
                }
            }

            if (harborYachtMarker != null)
            {
                if (harborPulseTimer > 0f)
                {
                    harborPulseTimer -= Time.deltaTime;
                    var pulse = 1f + Mathf.Sin((1f - Mathf.Clamp01(harborPulseTimer / 0.75f)) * Mathf.PI) * 0.22f;
                    harborYachtMarker.localScale = Vector3.one * pulse;
                    if (voyageShipMarker != null)
                    {
                        voyageShipMarker.localScale = Vector3.one * (1f + (pulse - 1f) * 0.72f);
                    }
                }
                else
                {
                    harborYachtMarker.localScale = Vector3.one;
                    if (voyageShipMarker != null)
                    {
                        voyageShipMarker.localScale = Vector3.one;
                    }
                }
            }
        }

        private void PulseDeckAssetsForAction(YachtRushCategory category, HarborYachtActionEffect effect)
        {
            foreach (var asset in DeckAssetsForAction(category))
            {
                PulseDeckAsset(asset, effect.HazardDelta < 0 ? 1.05f : 0.88f);
            }

            if (effect.RouteDelta > 0)
            {
                PulseDeckAsset(DeckAssetKind.Harbor, 0.74f);
            }

            if (effect.HullDelta > 0)
            {
                PulseDeckAsset(DeckAssetKind.HullPatch, 0.74f);
            }

            if (effect.SuppliesDelta > 0)
            {
                PulseDeckAsset(DeckAssetKind.Cargo, 0.74f);
            }

            if (effect.HazardDelta < 0)
            {
                PulseDeckAsset(HazardDeckAsset(currentRushDie), 1f);
            }
        }

        private void PulseDeckAsset(DeckAssetKind kind, float seconds)
        {
            if (deckAssets.TryGetValue(kind, out var asset))
            {
                asset.Pulse(seconds, RushDieAccentColor(currentRushDie));
            }
        }

        private void PulseCommandToken(YachtRushCategory category, Color color)
        {
            if (!commandTokens.TryGetValue(category, out var token))
            {
                return;
            }

            token.Root.localScale = Vector3.one * 1.12f;
            token.Background.material.color = color;
        }

        private void UpdateDeckAssetFeedback()
        {
            foreach (var asset in deckAssets.Values)
            {
                asset.Update(Time.deltaTime);
            }

            foreach (var station in resourceStations.Values)
            {
                station.Update(Time.deltaTime);
            }
        }

        private static DeckAssetKind[] DeckAssetsForAction(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return new[] { DeckAssetKind.Sail };
                case YachtRushCategory.Twos:
                    return new[] { DeckAssetKind.Cargo };
                case YachtRushCategory.Threes:
                    return new[] { DeckAssetKind.HullPatch };
                case YachtRushCategory.Fours:
                    return new[] { DeckAssetKind.Sail };
                case YachtRushCategory.Fives:
                    return new[] { DeckAssetKind.Cargo, DeckAssetKind.Harbor };
                case YachtRushCategory.Sixes:
                    return new[] { DeckAssetKind.Anchor, DeckAssetKind.Sail };
                case YachtRushCategory.FourOfAKind:
                    return new[] { DeckAssetKind.Compass, DeckAssetKind.Anchor };
                case YachtRushCategory.FullHouse:
                    return new[] { DeckAssetKind.Cargo, DeckAssetKind.HullPatch };
                case YachtRushCategory.SmallStraight:
                    return new[] { DeckAssetKind.Compass, DeckAssetKind.HullPatch };
                case YachtRushCategory.LargeStraight:
                    return new[] { DeckAssetKind.Compass, DeckAssetKind.Sail };
                case YachtRushCategory.Yacht:
                    return new[] { DeckAssetKind.Sail, DeckAssetKind.Harbor };
                case YachtRushCategory.Chance:
                    return new[] { DeckAssetKind.Anchor, DeckAssetKind.Compass, DeckAssetKind.Cargo };
                default:
                    return new[] { DeckAssetKind.Compass };
            }
        }

        private static DeckAssetKind HazardDeckAsset(YachtRushRushDie rushDie)
        {
            switch (rushDie)
            {
                case YachtRushRushDie.Anchor:
                    return DeckAssetKind.Anchor;
                case YachtRushRushDie.Storm:
                case YachtRushRushDie.Blank:
                    return DeckAssetKind.Storm;
                case YachtRushRushDie.Cracked:
                    return DeckAssetKind.Cargo;
                case YachtRushRushDie.Mirror:
                    return DeckAssetKind.Compass;
                default:
                    return DeckAssetKind.Storm;
            }
        }

        private string TwistLabel()
        {
            return "VOYAGE";
        }

        private string TwistName(YachtRushContractInfo contract, YachtRushRollRuleInfo rollRule, YachtRushRushDieInfo rushDie)
        {
            return "Roll supplies, pick a monthly command";
        }

        private string TwistEffect(IReadOnlyList<int> values, YachtRushContractInfo contract, string contractState)
        {
            return string.IsNullOrEmpty(currentCrewCouncilLine)
                ? "Use 1 Sail, 2 Hull, 3 Food, 4 Crew, 5 Gold, 6 Map."
                : currentCrewCouncilLine;
        }

        private string TwistBadge(YachtRushContractInfo contract, YachtRushRollRuleInfo rollRule)
        {
            return $"Month {RoundNumber}";
        }

        private string RollRuleBadgeText(YachtRushRollRule rollRule)
        {
            switch (rollRule)
            {
                case YachtRushRollRule.OneShot:
                    return "1 THROW";
                case YachtRushRollRule.NoHolds:
                    return "NO HOLD";
                case YachtRushRollRule.MustHold2:
                    return $"{HeldCount}/2 HELD";
                case YachtRushRollRule.RerollAll:
                    return "ALL DICE";
                case YachtRushRollRule.SafeHarbor:
                    return "2 THROWS";
                default:
                    return $"{MaxRollsThisRound} THROWS";
            }
        }

        private string RushDieBadgeText()
        {
            var dieNumber = rushDieIndex + 1;
            switch (currentRushDie)
            {
                case YachtRushRushDie.Anchor:
                    return $"DIE {dieNumber} ANCHOR";
                case YachtRushRushDie.Storm:
                    return $"DIE {dieNumber} STORM";
                case YachtRushRushDie.Cracked:
                    return $"DIE {dieNumber} CARGO";
                case YachtRushRushDie.Mirror:
                    return $"DIE {dieNumber} CURRENT";
                case YachtRushRushDie.Blank:
                    var values = CurrentDiceValues();
                    var original = values != null && rushDieIndex >= 0 && rushDieIndex < values.Length ? values[rushDieIndex] : 0;
                    if (CanScore && original > 0)
                    {
                        return $"BLANK -{original}";
                    }

                    return $"DIE {dieNumber} FOG";
                default:
                    return "NORMAL";
            }
        }

        private string RollRuleImpactText()
        {
            switch (currentRollRule)
            {
                case YachtRushRollRule.OneShot:
                    return rollCount == 0 ? "Only one throw. No reroll safety" : "No throws left. Choose a score";
                case YachtRushRollRule.NoHolds:
                    return "Hold is disabled. Every die stays live";
                case YachtRushRollRule.MustHold2:
                    if (rollCount == 1 && HeldCount < 2)
                    {
                        var needed = 2 - HeldCount;
                        return needed == 1 ? "Hold 1 more die to throw again" : "Hold 2 dice to throw again";
                    }

                    return "Second throw requires 2 held dice";
                case YachtRushRollRule.RerollAll:
                    return HeldCount > 0 ? "Next throw rerolls held dice too" : "Every throw rerolls all 5 dice";
                case YachtRushRollRule.SafeHarbor:
                    return "Only 2 throws. Contract pays more";
                default:
                    return "3 throws. Hold any dice";
            }
        }

        private string RushDieImpactText(IReadOnlyList<int> values)
        {
            var dieNumber = rushDieIndex + 1;
            var original = values != null && rushDieIndex >= 0 && rushDieIndex < values.Count ? values[rushDieIndex] : 0;
            switch (currentRushDie)
            {
                case YachtRushRushDie.Anchor:
                    return dice.Count > rushDieIndex && dice[rushDieIndex].IsHeld
                        ? $"Die {dieNumber}: anchor locked"
                        : $"Die {dieNumber}: anchor locks after throw";
                case YachtRushRushDie.Storm:
                    return $"Die {dieNumber}: storm toss, Hull -2";
                case YachtRushRushDie.Cracked:
                    return $"Die {dieNumber}: cracked cargo, Hull -1";
                case YachtRushRushDie.Mirror:
                    if (CanScore && original > 0)
                    {
                        var mirrored = 7 - original;
                        return $"Die {dieNumber}: {original} -> {mirrored} ({Signed(mirrored - original)})";
                    }

                    return $"Die {dieNumber}: current flips after landing";
                case YachtRushRushDie.Blank:
                    if (CanScore && original > 0)
                    {
                        return $"Fog blanks die {dieNumber}: score -{original}";
                    }

                    return $"Die {dieNumber}: fog may score 0";
                default:
                    return "All dice score normally";
            }
        }

        private void UpdateTwistVisualTheme(string contractState)
        {
            var accent = TwistAccentColor();
            if (contractBackground != null)
            {
                contractBackground.color = TwistBannerColor(contractState);
            }

            if (twistAccentBar != null)
            {
                twistAccentBar.color = accent;
            }

            if (tableMaterial != null)
            {
                tableMaterial.color = TwistTableColor(contractState);
            }

            foreach (var renderer in twistAccentRenderers)
            {
                if (renderer != null)
                {
                    renderer.material.color = accent;
                }
            }
        }

        private Color TwistBannerColor(string contractState)
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return contractState == "READY"
                        ? new Color32(224, 246, 218, 252)
                        : new Color32(236, 247, 226, 250);
                case RoundTwist.RushDie:
                    return new Color32(224, 239, 248, 250);
                case RoundTwist.RollRule:
                default:
                    return new Color32(255, 239, 190, 250);
            }
        }

        private Color TwistTableColor(string contractState)
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return contractState == "READY"
                        ? new Color32(226, 240, 216, 255)
                        : new Color32(232, 240, 222, 255);
                case RoundTwist.RushDie:
                    return new Color32(224, 237, 236, 255);
                case RoundTwist.RollRule:
                default:
                    return new Color32(239, 235, 211, 255);
            }
        }

        private Color TwistAccentColor()
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return new Color32(72, 128, 76, 255);
                case RoundTwist.RushDie:
                    return RushDieAccentColor(currentRushDie);
                case RoundTwist.RollRule:
                default:
                    return new Color32(188, 123, 58, 255);
            }
        }

        private static Color RushDieAccentColor(YachtRushRushDie rushDie)
        {
            switch (rushDie)
            {
                case YachtRushRushDie.Anchor:
                    return new Color32(50, 82, 128, 255);
                case YachtRushRushDie.Storm:
                    return new Color32(83, 102, 154, 255);
                case YachtRushRushDie.Cracked:
                    return new Color32(150, 93, 63, 255);
                case YachtRushRushDie.Mirror:
                    return new Color32(45, 130, 117, 255);
                case YachtRushRushDie.Blank:
                    return new Color32(110, 110, 104, 255);
                default:
                    return new Color32(65, 116, 154, 255);
            }
        }

        private static Color TokenAccentColor(DeckAssetKind kind)
        {
            switch (kind)
            {
                case DeckAssetKind.Sail:
                    return new Color32(232, 224, 184, 255);
                case DeckAssetKind.Anchor:
                    return new Color32(102, 119, 142, 255);
                case DeckAssetKind.Cargo:
                    return new Color32(181, 142, 99, 255);
                case DeckAssetKind.Compass:
                    return new Color32(154, 178, 152, 255);
                case DeckAssetKind.HullPatch:
                    return new Color32(139, 170, 144, 255);
                case DeckAssetKind.Harbor:
                    return new Color32(177, 205, 213, 255);
                case DeckAssetKind.Storm:
                    return new Color32(154, 105, 96, 255);
                default:
                    return new Color32(230, 221, 190, 255);
            }
        }

        private static Color TokenStateColor(HarborYachtActionEffect effect, bool rushChanged)
        {
            if (effect.HazardDelta < 0 || rushChanged)
            {
                return new Color32(154, 82, 58, 255);
            }

            if (effect.ContractScoreDelta > 0)
            {
                return new Color32(65, 116, 154, 255);
            }

            if (effect.HullDelta > 0)
            {
                return new Color32(62, 110, 65, 255);
            }

            if (effect.SuppliesDelta > 0)
            {
                return new Color32(150, 105, 60, 255);
            }

            return new Color32(62, 110, 65, 255);
        }

        private static string ScoreDetailText(int baseScore, int rushScore, int bonus)
        {
            var rushDelta = rushScore - baseScore;
            if (rushDelta != 0 && bonus > 0)
            {
                return $"Score {rushScore}  Hazard {Signed(rushDelta)}  Order +{bonus}";
            }

            if (rushDelta != 0)
            {
                return $"Score {rushScore}  Hazard {Signed(rushDelta)}";
            }

            if (bonus > 0)
            {
                return $"Score {baseScore}  Order +{bonus}";
            }

            return $"Score {baseScore}";
        }

        private static string HarborScoreDetailText(YachtRushRoundScorePreview preview, HarborYachtActionEffect effect)
        {
            var prefix = preview.BaseScore == preview.RushAdjustedScore
                ? $"Power {preview.RushAdjustedScore}"
                : $"Power {preview.RushAdjustedScore} ({Signed(preview.RushAdjustedScore - preview.BaseScore)})";
            var effectText = CompactHarborEffectText(effect);
            return string.IsNullOrEmpty(effectText) ? prefix : $"{prefix}  {effectText}";
        }

        private static string VoyageCommandDetailText(YachtRushCategory category, HarborYachtActionEffect effect)
        {
            if (!effect.IsAvailable)
            {
                return $"Need {CommandConditionHint(category)}";
            }

            var detail = CompactHarborEffectText(effect);
            return string.IsNullOrEmpty(detail) ? "Hold course" : detail;
        }

        private static string TokenConditionGlyph(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "1W  1W  4Sa";
                case YachtRushCategory.Twos:
                    return "2Su  2Su  5T";
                case YachtRushCategory.Threes:
                    return "3R  3R  6C";
                case YachtRushCategory.Fours:
                    return "4Sa  4Sa  6C";
                case YachtRushCategory.Fives:
                    return "5T  5T  2Su";
                case YachtRushCategory.Sixes:
                    return "6C  6C  pair";
                case YachtRushCategory.FourOfAKind:
                    return "low pair / high pair";
                case YachtRushCategory.FullHouse:
                    return "2Su  3R  5T";
                case YachtRushCategory.SmallStraight:
                    return "1W  3R  6C";
                case YachtRushCategory.LargeStraight:
                    return "1W 2Su 3R 4Sa";
                case YachtRushCategory.Yacht:
                    return "2Su 3R 4Sa 5T 6C";
                case YachtRushCategory.Chance:
                    return "any dice";
                default:
                    return "dice";
            }
        }

        private static string TokenPreviewLine(HarborYachtActionEffect effect)
        {
            if (!effect.IsAvailable)
            {
                return "locked";
            }

            if (effect.RouteDelta > 0)
            {
                return $"Dist +{effect.RouteDelta}";
            }

            if (effect.HullDelta > 0)
            {
                return $"Hull +{effect.HullDelta}";
            }

            if (effect.SuppliesDelta > 0)
            {
                return $"Supply +{effect.SuppliesDelta}";
            }

            if (effect.ContractScoreDelta > 0)
            {
                return $"Gold +{effect.ContractScoreDelta}";
            }

            return "hold course";
        }

        private static string CommandRationale(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "Wind fills the sail.";
                case YachtRushCategory.Twos:
                    return "Supplies meet trade goods.";
                case YachtRushCategory.Threes:
                    return "Tools and crew repair the hull.";
                case YachtRushCategory.Fours:
                    return "Crew raises enough sail.";
                case YachtRushCategory.Fives:
                    return "Trade goods need supplies.";
                case YachtRushCategory.Sixes:
                    return "Crew consensus steadies the ship.";
                case YachtRushCategory.FourOfAKind:
                    return "Low and high watches balance risk.";
                case YachtRushCategory.FullHouse:
                    return "Cargo, repair, and trade form a chain.";
                case YachtRushCategory.SmallStraight:
                    return "Wind, repair, and crew make safe water.";
                case YachtRushCategory.LargeStraight:
                    return "A prepared route opens the sea.";
                case YachtRushCategory.Yacht:
                    return "Every resource backs the voyage.";
                case YachtRushCategory.Chance:
                    return "The captain can always choose.";
                default:
                    return "The crew agrees.";
            }
        }

        private static string CommandGuideText(YachtRushCategory? focus)
        {
            return "Survive 12 months. Sail as far as possible.\n\n" +
                "Start: SAIL 0, HULL 18, FOOD 8, CREW 0, GOLD 0, MAP 0.\n\n" +
                "1. Roll 5 dice -> add matching resources.\n" +
                "2. Pick 1 deck token -> spend its listed resources and gain its effect.\n" +
                "3. Month +1. There is no automatic monthly drain.\n\n" +
                "Basic commands can repeat. Limited combo strategies can be used once.\n\n" +
                "HULL 0 or FOOD 0 ends the voyage.\n" +
                "SAIL moves the ship. GOLD and Distance build the record.\n" +
                "CREW and MAP unlock stronger limited strategies.\n\n" +
                "Tap resource boxes or deck tokens for details.";
        }

        private static string ShortRecordedEffect(ScoreRecord record)
        {
            if (record.RouteDelta > 0)
            {
                return $"Dist +{record.RouteDelta}";
            }

            if (record.HullDelta > 0)
            {
                return $"Hull +{record.HullDelta}";
            }

            if (record.SuppliesDelta > 0)
            {
                return $"Supply +{record.SuppliesDelta}";
            }

            if (record.ContractDelta > 0)
            {
                return $"Discovery +{record.ContractDelta}";
            }

            return "completed";
        }

        private static string VoyageCommandBadgeText(HarborYachtActionEffect effect)
        {
            if (!effect.IsAvailable)
            {
                return "-";
            }

            if (effect.RouteDelta > 0)
            {
                return $"+{effect.RouteDelta}";
            }

            if (effect.ContractScoreDelta > 0)
            {
                return $"+{effect.ContractScoreDelta}";
            }

            if (effect.HullDelta > 0)
            {
                return $"+{effect.HullDelta}";
            }

            if (effect.SuppliesDelta > 0)
            {
                return $"+{effect.SuppliesDelta}";
            }

            return "GO";
        }

        private static string CommandTagText(HarborYachtActionEffect effect)
        {
            if (effect.HazardDelta < 0)
            {
                return "RISK";
            }

            if (effect.ContractScoreDelta > 0)
            {
                return "GOLD";
            }

            if (effect.HullDelta > 0)
            {
                return "REPAIR";
            }

            if (effect.SuppliesDelta > 0)
            {
                return "SUPPLY";
            }

            return "OPEN";
        }

        private static int EffectPriority(HarborYachtActionEffect effect)
        {
            if (!effect.IsAvailable)
            {
                return -1;
            }

            return effect.RouteDelta * 3 +
                effect.ContractScoreDelta * 2 +
                effect.HullDelta * 2 +
                effect.SuppliesDelta -
                Math.Abs(Math.Min(0, effect.HazardDelta)) * 4;
        }

        private static string HarborRecordDetailText(ScoreRecord record)
        {
            var parts = new List<string>(4);
            AppendSigned(parts, "Dist", record.RouteDelta);
            AppendSigned(parts, "H", record.HullDelta);
            AppendSigned(parts, "S", record.SuppliesDelta);
            AppendSigned(parts, "Gold", record.ContractDelta);
            return parts.Count == 0 ? ScoreDetailText(record.BaseScore, record.RushAdjustedScore, record.Bonus) : string.Join("  ", parts);
        }

        private static string CompactHarborEffectText(HarborYachtActionEffect effect)
        {
            var parts = new List<string>(4);
            AppendSigned(parts, "Dist", effect.RouteDelta);
            AppendSigned(parts, "H", effect.HullDelta);
            AppendSigned(parts, "S", effect.SuppliesDelta);
            AppendSigned(parts, "Gold", effect.ContractScoreDelta);
            return string.Join("  ", parts);
        }

        private static void AppendSigned(ICollection<string> parts, string label, int value)
        {
            if (value == 0)
            {
                return;
            }

            parts.Add($"{label} {Signed(value)}");
        }

        private string RushDeltaTag(YachtRushRoundScorePreview preview)
        {
            var delta = preview.RushAdjustedScore - preview.BaseScore;
            switch (currentRushDie)
            {
                case YachtRushRushDie.Blank:
                    return $"BLANK {Signed(delta)}";
                case YachtRushRushDie.Mirror:
                    return $"FLIP {Signed(delta)}";
                case YachtRushRushDie.Cracked:
                    return $"CRACK {Signed(delta)}";
                default:
                    return $"RUSH {Signed(delta)}";
            }
        }

        private static string Signed(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }

        private string TurnPrompt()
        {
            if (hasPendingTurnResult)
            {
                return "Inspect a token, then choose an OPEN plan.";
            }

            if (isResolvingRoll)
            {
                return "Reading crew resources";
            }

            if (resultPanel != null && resultPanel.activeSelf)
            {
                return "Voyage complete";
            }

            return CanThrow ? "Roll dice to gather this month's resources" : "Preparing next month";
        }

        private static string ActionDeckLabel(YachtRushCategory category)
        {
            var assets = DeckAssetsForAction(category);
            return string.Join(" + ", assets.Select(DeckAssetDisplayName));
        }

        private static string CommandConditionHint(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "1 Wind + 1 Wind + 4 Sail";
                case YachtRushCategory.Twos:
                    return "2 Supply + 2 Supply + 5 Trade";
                case YachtRushCategory.Threes:
                    return "3 Repair + 3 Repair + 6 Crew";
                case YachtRushCategory.Fours:
                    return "4 Sail + 4 Sail + 6 Crew";
                case YachtRushCategory.Fives:
                    return "5 Trade + 5 Trade + 2 Supply";
                case YachtRushCategory.Sixes:
                    return "6 Crew + 6 Crew + any pair";
                case YachtRushCategory.FourOfAKind:
                    return "low pair + high pair";
                case YachtRushCategory.FullHouse:
                    return "2 Supply + 3 Repair + 5 Trade";
                case YachtRushCategory.SmallStraight:
                    return "1 Wind + 3 Repair + 6 Crew";
                case YachtRushCategory.LargeStraight:
                    return "1-2-3-4 route";
                case YachtRushCategory.Yacht:
                    return "2-3-4-5-6 route";
                case YachtRushCategory.Chance:
                    return "any dice";
                default:
                    return "monthly command";
            }
        }

        private static string CommandTokenTitle(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "Tailwind";
                case YachtRushCategory.Twos:
                    return "Stock Up";
                case YachtRushCategory.Threes:
                    return "Patch Hull";
                case YachtRushCategory.Fours:
                    return "Full Sail";
                case YachtRushCategory.Fives:
                    return "Harbor Trade";
                case YachtRushCategory.Sixes:
                    return "Crew Vote";
                case YachtRushCategory.FourOfAKind:
                    return "Watch";
                case YachtRushCategory.FullHouse:
                    return "Supply Chain";
                case YachtRushCategory.SmallStraight:
                    return "Safe Passage";
                case YachtRushCategory.LargeStraight:
                    return "Open Sea";
                case YachtRushCategory.Yacht:
                    return "Grand Voyage";
                case YachtRushCategory.Chance:
                    return "Captain Call";
                default:
                    return "Command";
            }
        }

        private static string CommandIconName(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "SAIL";
                case YachtRushCategory.Twos:
                    return "SUPPLY";
                case YachtRushCategory.Threes:
                    return "REPAIR";
                case YachtRushCategory.Fours:
                    return "SAIL";
                case YachtRushCategory.Fives:
                    return "TRADE";
                case YachtRushCategory.Sixes:
                    return "CREW";
                case YachtRushCategory.FourOfAKind:
                    return "WATCH";
                case YachtRushCategory.FullHouse:
                    return "CHAIN";
                case YachtRushCategory.SmallStraight:
                    return "SAFE";
                case YachtRushCategory.LargeStraight:
                    return "OPEN SEA";
                case YachtRushCategory.Yacht:
                    return "VOYAGE";
                case YachtRushCategory.Chance:
                    return "CAPTAIN";
                default:
                    return "TOKEN";
            }
        }

        private static string DeckAssetDisplayName(DeckAssetKind kind)
        {
            switch (kind)
            {
                case DeckAssetKind.Sail:
                    return "Sail";
                case DeckAssetKind.Anchor:
                    return "Anchor";
                case DeckAssetKind.Cargo:
                    return "Cargo";
                case DeckAssetKind.Compass:
                    return "Compass";
                case DeckAssetKind.HullPatch:
                    return "Patch";
                case DeckAssetKind.Harbor:
                    return "Harbor";
                case DeckAssetKind.Storm:
                    return "Storm";
                default:
                    return "Deck";
            }
        }

        private int[] CurrentDiceValues()
        {
            return dice.Select(die => Mathf.Clamp(die.Value, 1, 6)).ToArray();
        }

        private YachtRushContract NextContract()
        {
            var contracts = YachtRushRules.Contracts;
            YachtRushContract next;
            do
            {
                next = contracts[random.Next(contracts.Length)].Id;
            } while (contracts.Length > 1 && next == currentContract);

            return next;
        }

        private YachtRushRollRule NextRollRule()
        {
            var rollRules = YachtRushRules.RollRules
                .Where(item => item.Id != YachtRushRollRule.Classic)
                .Where(item => item.Id != YachtRushRollRule.SafeHarbor)
                .ToArray();
            YachtRushRollRule next;
            do
            {
                next = rollRules[random.Next(rollRules.Length)].Id;
            } while (rollRules.Length > 1 && next == currentRollRule);

            return next;
        }

        private YachtRushRushDie NextRushDie()
        {
            var rushDice = YachtRushRules.RushDice;
            YachtRushRushDie next;
            do
            {
                next = rushDice[random.Next(rushDice.Length)].Id;
            } while (rushDice.Length > 1 && next == currentRushDie);

            return next;
        }

        private void ChooseRoundModifiers()
        {
            currentContract = YachtRushContract.None;
            currentRollRule = YachtRushRollRule.Classic;
            currentTwist = RoundTwist.ContractHand;
            currentRushDie = YachtRushRushDie.None;
            rushDieIndex = random.Next(YachtRushRules.DiceCount);
            currentCrewCouncilLine = CrewCouncilLines[random.Next(CrewCouncilLines.Length)];
            ApplyRushDieVisuals();
        }

        private YachtRushContract ContractForRound(int roundNumber)
        {
            var contracts = YachtRushRules.Contracts;
            if (contracts.Length == 0)
            {
                return YachtRushContract.None;
            }

            return contracts[(Mathf.Max(1, roundNumber) - 1) % contracts.Length].Id;
        }

        private YachtRushRushDie RushDieForRound(int roundNumber)
        {
            var showcase = new[]
            {
                YachtRushRushDie.Storm,
                YachtRushRushDie.Blank,
                YachtRushRushDie.Mirror,
                YachtRushRushDie.Anchor,
                YachtRushRushDie.Cracked
            };

            if (roundNumber >= 1 && roundNumber <= showcase.Length)
            {
                return showcase[roundNumber - 1];
            }

            return NextRushDie();
        }

        private void ApplyRushDieVisuals()
        {
            for (var index = 0; index < dice.Count; index += 1)
            {
                var isRushDie = currentTwist == RoundTwist.RushDie &&
                    currentRushDie != YachtRushRushDie.None &&
                    index == rushDieIndex;
                dice[index].SetRushDie(currentRushDie, isRushDie);
                dice[index].UpdateHoldRing();
            }
        }

        private void CreateAudioClips()
        {
            audioClips["grab"] = CreateToneClip("Bowl Grab", 320f, 240f, 0.055f, 0.18f, 0.05f);
            audioClips["hold"] = CreateToneClip("Die Hold", 460f, 560f, 0.05f, 0.15f, 0.02f);
            audioClips["shake"] = CreateToneClip("Bowl Shake", 160f, 220f, 0.045f, 0.11f, 0.45f);
            audioClips["throw"] = CreateToneClip("Dice Throw", 140f, 92f, 0.12f, 0.24f, 0.28f);
            audioClips["settle"] = CreateToneClip("Dice Settle", 520f, 310f, 0.075f, 0.16f, 0.06f);
            audioClips["score"] = CreateToneClip("Score Mark", 420f, 630f, 0.09f, 0.16f, 0.03f);
            audioClips["bonus"] = CreateToneClip("Bonus Mark", 520f, 820f, 0.12f, 0.18f, 0.02f);
        }

        private static AudioClip CreateToneClip(string name, float startFrequency, float endFrequency, float seconds, float volume, float noise)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
            var data = new float[sampleCount];
            var seededNoise = new System.Random(name.Length * 7919);
            var phase = 0f;

            for (var index = 0; index < sampleCount; index += 1)
            {
                var t = index / (float)sampleCount;
                var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(1f - t)) * Mathf.Pow(1f - t, 0.65f);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency / sampleRate;
                var tone = Mathf.Sin(phase * Mathf.PI * 2f);
                var hiss = ((float)seededNoise.NextDouble() * 2f - 1f) * noise;
                data[index] = Mathf.Clamp((tone * (1f - noise * 0.35f) + hiss) * envelope * volume, -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlayAudioCue(string cue, float volumeScale = 1f)
        {
            if (audioSource == null || !audioClips.TryGetValue(cue, out var clip))
            {
                return;
            }

            audioSource.pitch = cue == "shake" ? 0.92f + Mathf.Clamp01(bowlShake / 700f) * 0.18f : 1f;
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private Material CreateMaterial(string name, Color color, float roughness)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Hidden/Internal-Colored");
            var material = shader == null ? new Material(Graphic.defaultGraphicMaterial) : new Material(shader);
            material.name = name;
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

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", Mathf.Clamp01(1f - roughness));
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 1f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            return material;
        }

        private Material CreateUnderlayMaterial(string name, Color color, float roughness)
        {
            var material = CreateMaterial(name, color, roughness);
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry - 100;
            return material;
        }

        private Material CreateTextureMaterial(string name, Texture2D texture)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Transparent") ??
                Shader.Find("Standard") ??
                Shader.Find("Sprites/Default");
            var material = shader == null ? new Material(Graphic.defaultGraphicMaterial) : new Material(shader);
            material.name = name;
            material.mainTexture = texture;
            material.color = Color.white;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private GameObject CreateFlatOval(string name, Material material)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gameObject.GetComponent<MeshFilter>().mesh = CreateDiscMesh(name + " Mesh", 28);
            gameObject.GetComponent<MeshRenderer>().material = material;
            return gameObject;
        }

        private Image CreateImage(Transform parent, string name, Anchor anchor, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private GameObject CreateOvalRing(string name, Material material, float outerX, float outerZ, float innerX, float innerZ)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gameObject.GetComponent<MeshFilter>().mesh = CreateOvalRingMesh(name + " Mesh", outerX, outerZ, innerX, innerZ, 40);
            gameObject.GetComponent<MeshRenderer>().material = material;
            return gameObject;
        }

        private GameObject CreateBowlWall(string name, Material material)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gameObject.GetComponent<MeshFilter>().mesh = CreateBowlWallMesh(name + " Mesh", 40);
            gameObject.GetComponent<MeshRenderer>().material = material;
            return gameObject;
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

        private static Mesh CreateTriangleMesh(string name)
        {
            var mesh = new Mesh
            {
                name = name,
                vertices = new[]
                {
                    new Vector3(-0.42f, 0f, -0.48f),
                    new Vector3(-0.42f, 0f, 0.48f),
                    new Vector3(0.46f, 0f, -0.34f)
                },
                triangles = new[] { 0, 1, 2 }
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

        private static Mesh CreateBowlWallMesh(string name, int segments)
        {
            const int rings = 3;
            var vertices = new Vector3[segments * rings];
            var triangles = new int[segments * (rings - 1) * 6];

            for (var index = 0; index < segments; index += 1)
            {
                var angle = (Mathf.PI * 2f * index) / segments;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                vertices[index * rings] = new Vector3(cos * 1.98f, 0.28f, sin * 1.02f);
                vertices[index * rings + 1] = new Vector3(cos * 1.76f, -0.02f, sin * 0.82f);
                vertices[index * rings + 2] = new Vector3(cos * 1.22f, -0.31f, sin * 0.48f);
            }

            for (var index = 0; index < segments; index += 1)
            {
                var next = (index + 1) % segments;
                for (var ring = 0; ring < rings - 1; ring += 1)
                {
                    var triangleIndex = (index * (rings - 1) + ring) * 6;
                    var top = index * rings + ring;
                    var bottom = top + 1;
                    var nextTop = next * rings + ring;
                    var nextBottom = nextTop + 1;
                    triangles[triangleIndex] = top;
                    triangles[triangleIndex + 1] = bottom;
                    triangles[triangleIndex + 2] = nextTop;
                    triangles[triangleIndex + 3] = nextTop;
                    triangles[triangleIndex + 4] = bottom;
                    triangles[triangleIndex + 5] = nextBottom;
                }
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
            outline.effectColor = new Color32(58, 47, 38, 36);
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
            var button = gameObject.GetComponent<Button>();
            button.colors = SketchUiFactory.ButtonColors();
            if (!string.IsNullOrEmpty(label))
            {
                CreateText(rect, "Label", label, 22, FontStyle.Bold, SketchPalette.Ink, Anchor.Stretch, Vector2.zero, Vector2.zero);
            }

            return button;
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
            text.resizeTextMinSize = Mathf.Max(9, size - 7);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static TextAnchor TextAlignmentForAnchor(Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft:
                case Anchor.BottomLeft:
                    return TextAnchor.MiddleLeft;
                case Anchor.TopRight:
                case Anchor.StretchRight:
                case Anchor.BottomRight:
                    return TextAnchor.MiddleRight;
                default:
                    return TextAnchor.MiddleCenter;
            }
        }

        private void ApplyAnchor(RectTransform rect, Anchor anchor)
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
                case Anchor.StretchLeft:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    break;
                case Anchor.StretchRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
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
                case Anchor.Center:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
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
            Center
        }

        private readonly struct ScoreRecord
        {
            public ScoreRecord(int baseScore, int bonus, int total)
                : this(baseScore, baseScore, bonus, total)
            {
            }

            public ScoreRecord(int baseScore, int rushAdjustedScore, int bonus, int total)
                : this(baseScore, rushAdjustedScore, bonus, total, 0, 0, 0, 0)
            {
            }

            public ScoreRecord(
                int baseScore,
                int rushAdjustedScore,
                int bonus,
                int total,
                int routeDelta,
                int hullDelta,
                int suppliesDelta,
                int contractDelta)
            {
                BaseScore = baseScore;
                RushAdjustedScore = rushAdjustedScore;
                Bonus = bonus;
                Total = total;
                RouteDelta = routeDelta;
                HullDelta = hullDelta;
                SuppliesDelta = suppliesDelta;
                ContractDelta = contractDelta;
            }

            public int BaseScore { get; }
            public int RushAdjustedScore { get; }
            public int Bonus { get; }
            public int Total { get; }
            public int RouteDelta { get; }
            public int HullDelta { get; }
            public int SuppliesDelta { get; }
            public int ContractDelta { get; }
        }

        private sealed class ScoreButtonView
        {
            public Button Button { get; set; }
            public Image Background { get; set; }
            public Text NameText { get; set; }
            public Text HintText { get; set; }
            public Text DetailText { get; set; }
            public Text TotalText { get; set; }
            public Text TagText { get; set; }
        }

        private readonly struct CommandTokenPlacement
        {
            public CommandTokenPlacement(YachtRushCategory category, Vector3 position, DeckAssetKind kind)
            {
                Category = category;
                Position = position;
                Kind = kind;
            }

            public YachtRushCategory Category { get; }
            public Vector3 Position { get; }
            public DeckAssetKind Kind { get; }
        }

        private sealed class CommandTokenView
        {
            public CommandTokenView(
                Transform root,
                Renderer background,
                Renderer iconBacking,
                Renderer statusStrip,
                TextMesh nameText,
                TextMesh detailText,
                TextMesh valueText,
                TextMesh tagText)
            {
                Root = root;
                Background = background;
                IconBacking = iconBacking;
                StatusStrip = statusStrip;
                NameText = nameText;
                DetailText = detailText;
                ValueText = valueText;
                TagText = tagText;
            }

            public Transform Root { get; }
            public Renderer Background { get; }
            public Renderer IconBacking { get; }
            public Renderer StatusStrip { get; }
            public TextMesh NameText { get; }
            public TextMesh DetailText { get; }
            public TextMesh ValueText { get; }
            public TextMesh TagText { get; }
        }

        private sealed class DeckAssetView
        {
            private readonly Transform root;
            private readonly Renderer[] renderers;
            private readonly Color[] baseColors;
            private readonly Vector3 baseScale;
            private float pulseTimer;
            private float pulseDuration;
            private Color pulseColor;

            public DeckAssetView(Transform root)
            {
                this.root = root;
                renderers = root.GetComponentsInChildren<Renderer>();
                baseColors = renderers.Select(item => item.material.color).ToArray();
                baseScale = root.localScale;
                pulseColor = new Color32(255, 232, 146, 255);
            }

            public void Pulse(float seconds, Color color)
            {
                pulseDuration = Mathf.Max(0.1f, seconds);
                pulseTimer = pulseDuration;
                pulseColor = color;
            }

            public void Update(float deltaTime)
            {
                if (pulseTimer <= 0f)
                {
                    root.localScale = baseScale;
                    for (var index = 0; index < renderers.Length; index += 1)
                    {
                        if (renderers[index] != null)
                        {
                            renderers[index].material.color = baseColors[index];
                        }
                    }

                    return;
                }

                pulseTimer -= deltaTime;
                var progress = Mathf.Clamp01(1f - pulseTimer / pulseDuration);
                var wave = Mathf.Sin(progress * Mathf.PI);
                root.localScale = baseScale * (1f + wave * 0.12f);

                for (var index = 0; index < renderers.Length; index += 1)
                {
                    if (renderers[index] == null)
                    {
                        continue;
                    }

                    renderers[index].material.color = Color.Lerp(baseColors[index], pulseColor, wave * 0.36f);
                }
            }
        }

        private sealed class DieView
        {
            private readonly Material coreMaterial;
            private readonly Material[] facePanelMaterials;
            private readonly Material rushBadgeMaterial;
            private readonly GameObject rushBadge;
            private readonly Material rushHaloMaterial;
            private readonly GameObject rushHalo;
            private readonly GameObject[] hazardGlyphs;
            private readonly Material[] hazardGlyphMaterials;
            private readonly Color baseColor;
            private readonly Color baseFaceColor;
            private readonly Color heldColor = new Color32(216, 237, 198, 255);
            private readonly Color heldFaceColor = new Color32(228, 244, 211, 255);

            public DieView(int index, GameObject gameObject)
            {
                Index = index;
                GameObject = gameObject;
                Transform = gameObject.transform;
                Rigidbody = gameObject.GetComponent<Rigidbody>();
                var renderer = Transform.Find("Die Core").GetComponent<Renderer>();
                var facePanelRenderers = Transform
                    .GetComponentsInChildren<Renderer>()
                    .Where(item => item.gameObject.name == "Soft Face Inset")
                    .ToArray();
                coreMaterial = renderer.material;
                facePanelMaterials = facePanelRenderers.Select(item => item.material).ToArray();
                rushBadge = Transform.Find("Rush Die Badge")?.gameObject;
                rushBadgeMaterial = rushBadge == null ? null : rushBadge.GetComponent<Renderer>().material;
                rushHalo = Transform.Find("Rush Die Halo")?.gameObject;
                rushHaloMaterial = rushHalo == null ? null : rushHalo.GetComponent<Renderer>().material;
                hazardGlyphs = Enumerable.Range(1, 3)
                    .Select(item => Transform.Find($"Hazard Glyph {item}")?.gameObject)
                    .Where(item => item != null)
                    .ToArray();
                hazardGlyphMaterials = hazardGlyphs
                    .Select(item => item.GetComponent<Renderer>().material)
                    .ToArray();
                baseColor = coreMaterial.color;
                baseFaceColor = facePanelMaterials.Length > 0 ? facePanelMaterials[0].color : baseColor;
                Value = (index % 6) + 1;
            }

            public int Index { get; }
            public int Value { get; private set; }
            public bool IsHeld { get; set; }
            public bool IsRushDie { get; private set; }
            public YachtRushRushDie RushDie { get; private set; }
            public Vector3 BowlVelocity { get; set; }
            public GameObject GameObject { get; }
            public Transform Transform { get; }
            public Rigidbody Rigidbody { get; }

            public void SetValue(int value)
            {
                Value = Mathf.Clamp(value, 1, 6);
            }

            public void SetRushDie(YachtRushRushDie rushDie, bool isRushDie)
            {
                RushDie = rushDie;
                IsRushDie = isRushDie;
                if (rushBadge != null)
                {
                    rushBadge.SetActive(isRushDie);
                }

                if (rushHalo != null)
                {
                    rushHalo.SetActive(isRushDie);
                }

                if (rushBadgeMaterial != null)
                {
                    rushBadgeMaterial.color = RushDieColor(rushDie);
                }

                if (rushHaloMaterial != null)
                {
                    rushHaloMaterial.color = RushDieColor(rushDie);
                }

                foreach (var glyph in hazardGlyphs)
                {
                    glyph.SetActive(isRushDie);
                }

                foreach (var material in hazardGlyphMaterials)
                {
                    material.color = RushDieGlyphColor(rushDie);
                }

                ApplyHazardGlyph(rushDie);
            }

            public void UpdateHoldRing()
            {
                coreMaterial.color = IsHeld ? heldColor : IsRushDie ? RushDieCoreColor(RushDie) : baseColor;
                foreach (var facePanelMaterial in facePanelMaterials)
                {
                    facePanelMaterial.color = IsHeld ? heldFaceColor : IsRushDie ? RushDieFaceColor(RushDie) : baseFaceColor;
                }
            }

            private static Color RushDieColor(YachtRushRushDie rushDie)
            {
                switch (rushDie)
                {
                    case YachtRushRushDie.Anchor:
                        return new Color32(59, 88, 130, 255);
                    case YachtRushRushDie.Storm:
                        return new Color32(92, 116, 150, 255);
                    case YachtRushRushDie.Cracked:
                        return new Color32(126, 94, 77, 255);
                    case YachtRushRushDie.Mirror:
                        return new Color32(62, 123, 112, 255);
                    case YachtRushRushDie.Blank:
                        return new Color32(126, 126, 118, 255);
                    default:
                        return SketchPalette.Ink;
                }
            }

            private static Color RushDieCoreColor(YachtRushRushDie rushDie)
            {
                var badge = RushDieColor(rushDie);
                return Color.Lerp(new Color32(245, 242, 232, 255), badge, 0.46f);
            }

            private static Color RushDieFaceColor(YachtRushRushDie rushDie)
            {
                var badge = RushDieColor(rushDie);
                return Color.Lerp(new Color32(246, 250, 236, 255), badge, 0.24f);
            }

            private static Color RushDieGlyphColor(YachtRushRushDie rushDie)
            {
                return rushDie == YachtRushRushDie.Blank
                    ? new Color32(255, 253, 246, 255)
                    : SketchPalette.Ink;
            }

            private void ApplyHazardGlyph(YachtRushRushDie rushDie)
            {
                if (hazardGlyphs.Length == 0)
                {
                    return;
                }

                for (var index = 0; index < hazardGlyphs.Length; index += 1)
                {
                    var transform = hazardGlyphs[index].transform;
                    transform.localPosition = new Vector3(0f, DiceSize * 0.675f + index * 0.002f, 0f);
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(DiceSize * 0.52f, 0.018f, DiceSize * 0.055f);
                }

                switch (rushDie)
                {
                    case YachtRushRushDie.Anchor:
                        SetGlyph(0, new Vector3(0f, DiceSize * 0.675f, -DiceSize * 0.04f), new Vector3(DiceSize * 0.48f, 0.018f, DiceSize * 0.055f), Quaternion.identity);
                        SetGlyph(1, new Vector3(0f, DiceSize * 0.68f, DiceSize * 0.04f), new Vector3(DiceSize * 0.06f, 0.018f, DiceSize * 0.44f), Quaternion.identity);
                        SetGlyph(2, new Vector3(0f, DiceSize * 0.685f, -DiceSize * 0.16f), new Vector3(DiceSize * 0.34f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, 18f, 0f));
                        break;
                    case YachtRushRushDie.Storm:
                        SetGlyph(0, new Vector3(-DiceSize * 0.13f, DiceSize * 0.675f, DiceSize * 0.13f), new Vector3(DiceSize * 0.5f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, -26f, 0f));
                        SetGlyph(1, new Vector3(DiceSize * 0.04f, DiceSize * 0.68f, 0f), new Vector3(DiceSize * 0.62f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, -26f, 0f));
                        SetGlyph(2, new Vector3(DiceSize * 0.15f, DiceSize * 0.685f, -DiceSize * 0.13f), new Vector3(DiceSize * 0.42f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, -26f, 0f));
                        break;
                    case YachtRushRushDie.Cracked:
                        SetGlyph(0, new Vector3(-DiceSize * 0.05f, DiceSize * 0.675f, DiceSize * 0.1f), new Vector3(DiceSize * 0.46f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, 47f, 0f));
                        SetGlyph(1, new Vector3(DiceSize * 0.08f, DiceSize * 0.68f, -DiceSize * 0.08f), new Vector3(DiceSize * 0.36f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, -34f, 0f));
                        SetGlyph(2, new Vector3(-DiceSize * 0.16f, DiceSize * 0.685f, -DiceSize * 0.12f), new Vector3(DiceSize * 0.2f, 0.018f, DiceSize * 0.05f), Quaternion.Euler(0f, 18f, 0f));
                        break;
                    case YachtRushRushDie.Mirror:
                        SetGlyph(0, new Vector3(0f, DiceSize * 0.675f, DiceSize * 0.1f), new Vector3(DiceSize * 0.48f, 0.018f, DiceSize * 0.05f), Quaternion.identity);
                        SetGlyph(1, new Vector3(0f, DiceSize * 0.68f, -DiceSize * 0.1f), new Vector3(DiceSize * 0.48f, 0.018f, DiceSize * 0.05f), Quaternion.identity);
                        SetGlyph(2, new Vector3(0f, DiceSize * 0.685f, 0f), new Vector3(DiceSize * 0.05f, 0.018f, DiceSize * 0.42f), Quaternion.identity);
                        break;
                    case YachtRushRushDie.Blank:
                        SetGlyph(0, new Vector3(0f, DiceSize * 0.675f, 0f), new Vector3(DiceSize * 0.56f, 0.02f, DiceSize * 0.08f), Quaternion.Euler(0f, 45f, 0f));
                        SetGlyph(1, new Vector3(0f, DiceSize * 0.68f, 0f), new Vector3(DiceSize * 0.56f, 0.02f, DiceSize * 0.08f), Quaternion.Euler(0f, -45f, 0f));
                        SetGlyph(2, new Vector3(0f, DiceSize * 0.685f, 0f), new Vector3(DiceSize * 0.18f, 0.02f, DiceSize * 0.18f), Quaternion.identity);
                        break;
                }
            }

            private void SetGlyph(int index, Vector3 position, Vector3 scale, Quaternion rotation)
            {
                if (index < 0 || index >= hazardGlyphs.Length)
                {
                    return;
                }

                var transform = hazardGlyphs[index].transform;
                transform.localPosition = position;
                transform.localScale = scale;
                transform.localRotation = rotation;
            }
        }
    }
}
