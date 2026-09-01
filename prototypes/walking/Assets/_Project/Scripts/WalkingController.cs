using System;
using System.Collections;
using System.Collections.Generic;
using MannLab.Ads;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace MannLab.Games.Walking
{
    public sealed class WalkingController : MonoBehaviour
    {
        public const bool DefaultDebugFootMarkers = false;

        private static readonly Color Paper = new Color32(250, 247, 239, 255);
        private static readonly Color Sky = new Color32(226, 246, 250, 255);
        private static readonly Color Ink = new Color32(40, 39, 36, 255);
        private static readonly Color FadedInk = new Color32(102, 97, 90, 255);
        private static readonly Color Warm = new Color32(247, 181, 71, 255);
        private static readonly Color Green = new Color32(123, 168, 107, 255);
        private static readonly Color Red = new Color32(210, 74, 66, 255);
        private static readonly Color Blue = new Color32(88, 142, 181, 255);
        private static readonly float[] GoalMarkerDistances = { 10f, 25f, 50f, 90f };
        private static Mesh sharedCubeMesh;
        private static Mesh sharedSphereMesh;

        [Header("Debug")]
        [SerializeField] private bool debugFootMarkers = DefaultDebugFootMarkers;

        [Header("Camera")]
        [SerializeField] private float eyeHeight = 1.48f;
        [SerializeField] private float cameraMoveLerp = 5.8f;
        [SerializeField] private float cameraTurnLerp = 8f;
        [SerializeField] private float stepBobStrength = 0.055f;
        [SerializeField] private float thirdPersonDistance = 2.35f;
        [SerializeField] private float thirdPersonHeight = 1.92f;
        [SerializeField] private float thirdPersonLookAhead = 1.02f;
        [SerializeField] private float avatarMoveLerp = 5.4f;
        [SerializeField] private float avatarTurnLerp = 7.2f;

        [Header("World")]
        [SerializeField] private float runDurationSeconds = 30f;
        [SerializeField] private float openFieldHalfWidth = 18f;
        [SerializeField] private float openFieldLength = 180f;
        [SerializeField] private int openFieldObstacleCount = 12;

        private readonly FootRuntime leftFoot = new FootRuntime(WalkingFootSide.Left);
        private readonly FootRuntime rightFoot = new FootRuntime(WalkingFootSide.Right);
        private readonly Dictionary<int, FootRuntime> activeTouches = new Dictionary<int, FootRuntime>();

        private WalkingGameState state;
        private WalkingMaze maze;
        private Camera gameCamera;
        private Transform worldRoot;
        private Transform debugRoot;
        private Transform leftMarker;
        private Transform rightMarker;
        private Transform playerRoot;
        private Transform playerBody;
        private Transform playerBackMark;
        private Transform playerHead;
        private Transform playerFacePatch;
        private Transform playerBeak;
        private Transform playerLeftArm;
        private Transform playerRightArm;
        private Transform playerLeftFoot;
        private Transform playerRightFoot;
        private SpriteRenderer playerSpriteRenderer;
        private Transform cameraBackdropRoot;
        private readonly List<SpriteRenderer> scenicBillboards = new List<SpriteRenderer>();
        private Canvas canvas;
        private Text titleText;
        private Text hintText;
        private Text distanceText;
        private Text bestText;
        private Text stepText;
        private Text resultText;
        private Text leftStatusText;
        private Text rightStatusText;
        private Image leftTouchZone;
        private Image rightTouchZone;
        private Image leftStatusBadge;
        private Image rightStatusBadge;
        private Button restartButton;
        private AudioSource audioSource;
        private AudioClip stepClip;
        private AudioClip bumpClip;
        private AudioClip rewardClip;
        private AudioClip bestClip;
        private GUIStyle hudStyle;
        private GUIStyle smallHudStyle;
        private GUIStyle titleStyle;
        private GUIStyle hintStyle;
        private GUIStyle guideStyle;
        private GUIStyle resultMetricStyle;
        private GUIStyle resultDetailStyle;
        private GUIStyle buttonStyle;
        private Texture2D circleTexture;
        private Texture2D ringTexture;
        private Texture2D iceFieldTexture;
        private Sprite penguinIdleSprite;
        private Sprite penguinLeftStepSprite;
        private Sprite penguinRightStepSprite;
        private Sprite penguinStumbleSprite;
        private Sprite penguinHappySprite;
        private Sprite icebergIntactSprite;
        private Sprite icebergCrackedOneSprite;
        private Sprite icebergCrackedTwoSprite;
        private Sprite icebergBrokenSprite;
        private Sprite polarBackdropSprite;
        private Sprite skyCloudsSprite;
        private Sprite snowPuffSprite;
        private Sprite iceFloeSprite;
        private Sprite iceChipSprite;
        private bool doodleAssetsLoaded;
        private readonly List<StepStamp> stepStamps = new List<StepStamp>();
        private readonly List<FieldObstacle> fieldObstacles = new List<FieldObstacle>();
        private readonly List<GoalMarkerRuntime> goalMarkers = new List<GoalMarkerRuntime>();

        private Vector2 leftFootPosition;
        private Vector2 rightFootPosition;
        private Vector2 bodyPosition;
        private Vector2 previousBodyPosition;
        private Vector2 facing = Vector2.up;
        private Vector2 visualBodyPosition;
        private Vector2 visualFacing = Vector2.up;
        private Vector2 cameraBodyPosition;
        private float distanceMeters;
        private float bestDistanceAtRunStart;
        private float bestMarkerDistanceThisRun;
        private float bestDistanceMeters;
        private float runTimeRemaining;
        private int steps;
        private float bobImpulse;
        private float invalidPulse;
        private float obstacleBumpPulse;
        private float milestonePulse;
        private float bestPassPulse;
        private WalkingFootSide lastLandedSide = WalkingFootSide.Left;
        private float bodyLeanPulse;
        private float suppressMouseInputUntil;
        private bool runStarted;
        private bool bestUpdatedThisRun;
        private bool bestMarkerPassedThisRun;
        private int nextGoalMarkerIndex;
        private int reachedGoalMarkers;
        private int brokenIcebergs;

        private const string BestDistanceKey = "MannLab.Walking.BestDistance";
        private const string ProductionIosInterstitialAdUnitId = "";
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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int crashlyticsTestTapCount;
        private float crashlyticsTestTapDeadline;
#endif

        private enum InputMode
        {
            Idle,
            Placement,
            Return,
            Ignored,
            LandedHold,
            InvalidHold
        }

        private sealed class FootRuntime
        {
            public FootRuntime(WalkingFootSide side)
            {
                Side = side;
            }

            public WalkingFootSide Side { get; }
            public bool NeedsReturn { get; set; }
            public InputMode Mode { get; set; }
            public int FingerId { get; set; } = int.MinValue;
            public Vector2 ScreenPosition { get; set; }
            public Vector2 BestStepScreenPosition { get; set; }
            public WalkingFootPlacement Candidate { get; set; }
            public float StatusPulse { get; set; }
        }

        private sealed class StepStamp
        {
            public Transform Root { get; set; }
            public Material Material { get; set; }
            public Color Color { get; set; }
            public Vector3 BaseScale { get; set; }
            public float Age { get; set; }
        }

        private sealed class FieldObstacle
        {
            public FieldObstacle(Vector2 center, float radius, Transform root, SpriteRenderer renderer)
            {
                Center = center;
                Radius = radius;
                BaseRadius = radius;
                Root = root;
                Renderer = renderer;
            }

            public Vector2 Center { get; }
            public float Radius { get; set; }
            public float BaseRadius { get; }
            public Transform Root { get; }
            public SpriteRenderer Renderer { get; }
            public int Hits { get; set; }
        }

        private sealed class GoalMarkerRuntime
        {
            public GoalMarkerRuntime(float distance, Transform root, Material material, Color baseColor, bool isBest)
            {
                Distance = distance;
                Root = root;
                Material = material;
                BaseColor = baseColor;
                IsBest = isBest;
            }

            public float Distance { get; }
            public Transform Root { get; }
            public Material Material { get; }
            public Color BaseColor { get; }
            public bool IsBest { get; }
            public bool Reached { get; set; }
        }

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            Input.multiTouchEnabled = true;

            bestDistanceMeters = PlayerPrefs.GetFloat(BestDistanceKey, 0f);
            EnsureSceneObjects();
            BuildUi();
            InitializeTelemetryAndAds();
            ResetRun();
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
            HandleInput();
            UpdateRunTimer();
            UpdateCandidates();
            UpdatePlayerAvatar();
            UpdateStepStamps();
            UpdateCamera();
            UpdateBillboards();
            UpdateGoalMarkers();
            UpdateUi();
            UpdateDebugMarkers();
            bobImpulse = Mathf.MoveTowards(bobImpulse, 0f, Time.deltaTime * 4.5f);
            invalidPulse = Mathf.MoveTowards(invalidPulse, 0f, Time.deltaTime * 4f);
            obstacleBumpPulse = Mathf.MoveTowards(obstacleBumpPulse, 0f, Time.deltaTime * 3.2f);
            milestonePulse = Mathf.MoveTowards(milestonePulse, 0f, Time.deltaTime * 3.4f);
            bestPassPulse = Mathf.MoveTowards(bestPassPulse, 0f, Time.deltaTime * 2.4f);
            bodyLeanPulse = Mathf.MoveTowards(bodyLeanPulse, 0f, Time.deltaTime * 3.2f);
            leftFoot.StatusPulse = Mathf.MoveTowards(leftFoot.StatusPulse, 0f, Time.deltaTime * 4f);
            rightFoot.StatusPulse = Mathf.MoveTowards(rightFoot.StatusPulse, 0f, Time.deltaTime * 4f);
        }

        private void ResetRun()
        {
            if (runStarted)
            {
                FirebaseTelemetry.LogEvent(
                    "restart",
                    new Dictionary<string, string>
                    {
                        { "distance_m", distanceMeters.ToString("0.0") },
                        { "best_distance_m", bestDistanceMeters.ToString("0.0") },
                        { "steps", steps.ToString() }
                    });
            }

            maze = null;
            bestDistanceAtRunStart = bestDistanceMeters;
            bestMarkerDistanceThisRun = WalkingRules.BestMarkerDistance(bestDistanceAtRunStart, openFieldLength);
            BuildWorld();

            bodyPosition = new Vector2(0f, 0.4f);
            leftFootPosition = bodyPosition + new Vector2(-WalkingRules.NaturalHalfStance, 0f);
            rightFootPosition = bodyPosition + new Vector2(WalkingRules.NaturalHalfStance, 0f);
            previousBodyPosition = bodyPosition;
            facing = Vector2.up;
            visualBodyPosition = bodyPosition;
            visualFacing = facing;
            cameraBodyPosition = bodyPosition;
            distanceMeters = 0f;
            runTimeRemaining = runDurationSeconds;
            steps = 0;
            brokenIcebergs = 0;
            bestUpdatedThisRun = false;
            bestMarkerPassedThisRun = false;
            nextGoalMarkerIndex = 0;
            reachedGoalMarkers = 0;
            bobImpulse = 0f;
            invalidPulse = 0f;
            obstacleBumpPulse = 0f;
            milestonePulse = 0f;
            bestPassPulse = 0f;
            bodyLeanPulse = 0f;
            lastLandedSide = WalkingFootSide.Left;
            ClearStepStamps();
            ResetFootRuntime(leftFoot);
            ResetFootRuntime(rightFoot);
            activeTouches.Clear();
            runStarted = true;
            state = WalkingGameState.Ready;
            BuildPlayerAvatar();
            UpdatePlayerAvatar(true);
            UpdateCamera(true);
            UpdateUi();
            UpdateDebugMarkers(true);
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "run_start",
                new Dictionary<string, string>
                {
                    { "best_distance_m", bestDistanceMeters.ToString("0.0") },
                    { "duration_s", Mathf.RoundToInt(runDurationSeconds).ToString() }
                });
        }

        private void ResetFootRuntime(FootRuntime foot)
        {
            foot.NeedsReturn = false;
            foot.Mode = InputMode.Idle;
            foot.FingerId = int.MinValue;
            foot.ScreenPosition = Vector2.zero;
            foot.BestStepScreenPosition = Vector2.zero;
            foot.Candidate = default;
            foot.StatusPulse = 0f;
        }

        private void EnsureSceneObjects()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.GetComponent<Camera>();
            }

            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = Sky;
            gameCamera.fieldOfView = 58f;
            gameCamera.nearClipPlane = 0.04f;
            gameCamera.farClipPlane = 180f;
            if (gameCamera.GetComponent<AudioListener>() == null)
            {
                gameCamera.gameObject.AddComponent<AudioListener>();
            }

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            stepClip = CreateTone("Walking Step", 180f, 0.055f, 0.16f);
            bumpClip = CreateTone("Walking Bump", 76f, 0.12f, 0.22f);
            rewardClip = CreateTone("Thumbwaddle Goal", 360f, 0.075f, 0.18f);
            bestClip = CreateTone("Thumbwaddle Best", 520f, 0.11f, 0.20f);

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void LoadDoodleAssets()
        {
            if (doodleAssetsLoaded)
            {
                return;
            }

            doodleAssetsLoaded = true;
            penguinIdleSprite = LoadSprite("Thumbwaddle/penguin_back_idle", 500f);
            penguinLeftStepSprite = LoadSprite("Thumbwaddle/penguin_back_left_step", 500f);
            penguinRightStepSprite = LoadSprite("Thumbwaddle/penguin_back_right_step", 500f);
            penguinStumbleSprite = LoadSprite("Thumbwaddle/penguin_back_stumble", 500f);
            penguinHappySprite = LoadSprite("Thumbwaddle/penguin_back_happy", 500f);
            icebergIntactSprite = LoadSprite("Thumbwaddle/iceberg_intact", 340f);
            icebergCrackedOneSprite = LoadSprite("Thumbwaddle/iceberg_cracked_1", 340f);
            icebergCrackedTwoSprite = LoadSprite("Thumbwaddle/iceberg_cracked_2", 340f);
            icebergBrokenSprite = LoadSprite("Thumbwaddle/iceberg_broken", 340f);
            polarBackdropSprite = LoadSprite("Thumbwaddle/polar_backdrop", 300f);
            skyCloudsSprite = LoadSprite("Thumbwaddle/sky_clouds", 520f);
            snowPuffSprite = LoadSprite("Thumbwaddle/snow_puff", 260f);
            iceFloeSprite = LoadSprite("Thumbwaddle/ice_floe_small", 260f);
            iceChipSprite = LoadSprite("Thumbwaddle/ice_chip", 240f);
            iceFieldTexture = Resources.Load<Texture2D>("Thumbwaddle/ice_field_background");
        }

        private static Sprite LoadSprite(string resourcePath, float pixelsPerUnit)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        private void BuildWorld()
        {
            if (worldRoot != null)
            {
                Destroy(worldRoot.gameObject);
            }

            LoadDoodleAssets();
            worldRoot = new GameObject("Walking World").transform;
            var floorMaterial = CreateMaterial("Paper Floor", new Color32(255, 253, 247, 255));
            var goalMaterial = CreateMaterial("Start Ice Line", new Color32(145, 197, 215, 255));
            var obstacleShadowMaterial = CreateMaterial("Iceberg Snow Shadow", new Color32(218, 232, 234, 255));
            var obstacleMaterial = CreateMaterial("Iceberg Blue Face", new Color32(188, 226, 237, 255));
            var obstacleTopMaterial = CreateMaterial("Iceberg Snow Face", new Color32(252, 254, 250, 255));
            var obstacleStrokeMaterial = CreateMaterial("Iceberg Ink Cracks", new Color32(66, 126, 153, 255));
            var fieldWidth = openFieldHalfWidth * 2f;
            var fieldCenterZ = openFieldLength * 0.5f;
            fieldObstacles.Clear();
            scenicBillboards.Clear();
            goalMarkers.Clear();

            if (iceFieldTexture != null)
            {
                CreateTexturedGround(
                    "Sketch Ice Field",
                    new Vector3(0f, -0.032f, fieldCenterZ),
                    fieldWidth,
                    openFieldLength + 18f,
                    iceFieldTexture,
                    worldRoot);
            }
            else
            {
                CreateCube(
                    "Open Paper Field",
                    new Vector3(0f, -0.035f, fieldCenterZ),
                    new Vector3(fieldWidth, 0.06f, openFieldLength + 18f),
                    floorMaterial,
                    worldRoot);

                CreateCube(
                    "Start Sketch Line",
                    new Vector3(0f, 0.006f, 0.22f),
                    new Vector3(fieldWidth * 0.20f, 0.012f, 0.065f),
                    goalMaterial,
                    worldRoot);
            }

            BuildOpenFieldObstacles(obstacleShadowMaterial, obstacleMaterial, obstacleTopMaterial, obstacleStrokeMaterial);
            BuildFieldDressing();
            BuildDistanceGoalMarkers();
            BuildCameraBackdrop();
            BuildDebugMarkers();
        }

        private void BuildDistanceGoalMarkers()
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject("Thumbwaddle Distance Goals").transform;
            root.SetParent(worldRoot, false);
            var poleMaterial = CreateMaterial("Goal Marker Ink", new Color32(76, 91, 97, 255));
            for (var i = 0; i < GoalMarkerDistances.Length; i++)
            {
                var distance = GoalMarkerDistances[i];
                if (distance >= openFieldLength - 6f)
                {
                    continue;
                }

                var side = i % 2 == 0 ? -1f : 1f;
                var color = i % 3 == 0 ? Warm : i % 3 == 1 ? Blue : Green;
                var material = CreateMaterial("Goal Marker Flag " + i, WithAlpha(color, 235));
                var marker = CreateGoalFlag(root, new Vector3(side * 3.9f, 0f, distance), side, poleMaterial, material, false);
                goalMarkers.Add(new GoalMarkerRuntime(distance, marker, material, color, false));
            }

            var bestDistance = bestMarkerDistanceThisRun;
            if (bestDistance > 0f)
            {
                var bestMaterial = CreateMaterial("Best Marker Warm Ink", new Color32(247, 181, 71, 225));
                var bestRoot = new GameObject("Thumbwaddle Best Marker").transform;
                bestRoot.SetParent(root, false);
                bestRoot.position = new Vector3(0f, 0.012f, bestDistance);
                CreateCube(
                    "Best Marker Ice Line",
                    new Vector3(0f, 0.014f, bestDistance),
                    new Vector3(openFieldHalfWidth * 0.58f, 0.018f, 0.055f),
                    bestMaterial,
                    bestRoot);
                var marker = CreateGoalFlag(root, new Vector3(openFieldHalfWidth - 2.4f, 0f, bestDistance), 1f, poleMaterial, bestMaterial, true);
                goalMarkers.Add(new GoalMarkerRuntime(bestDistance, marker, bestMaterial, Warm, true));
            }
        }

        private static Transform CreateGoalFlag(
            Transform parent,
            Vector3 position,
            float side,
            Material poleMaterial,
            Material flagMaterial,
            bool isBest)
        {
            var flagRoot = new GameObject(isBest ? "Best Flag Doodle" : "Goal Flag Doodle").transform;
            flagRoot.SetParent(parent, false);
            flagRoot.position = position;
            var height = isBest ? 0.68f : 0.48f;
            CreateCube(
                "Goal Flag Pole",
                position + new Vector3(0f, height * 0.5f, 0f),
                new Vector3(0.035f, height, 0.035f),
                poleMaterial,
                flagRoot);
            CreateCube(
                "Goal Flag Cloth",
                position + new Vector3(side * 0.18f, height * 0.78f, 0f),
                new Vector3(0.32f, isBest ? 0.20f : 0.16f, 0.032f),
                flagMaterial,
                flagRoot);
            CreateCube(
                "Goal Flag Base",
                position + new Vector3(0f, 0.018f, 0f),
                new Vector3(0.44f, 0.018f, 0.11f),
                flagMaterial,
                flagRoot);
            return flagRoot;
        }

        private void BuildCameraBackdrop()
        {
            if (cameraBackdropRoot != null)
            {
                Destroy(cameraBackdropRoot.gameObject);
                cameraBackdropRoot = null;
            }

            if (worldRoot == null || skyCloudsSprite == null)
            {
                return;
            }

            cameraBackdropRoot = new GameObject("Thumbwaddle World Sky Doodles").transform;
            cameraBackdropRoot.SetParent(worldRoot, false);

            var random = new System.Random(90817);
            var layers = Mathf.Max(4, Mathf.CeilToInt(openFieldLength / 42f));
            for (var i = 0; i < layers; i++)
            {
                var renderer = CreateWorldSprite("World Cloud Doodle", skyCloudsSprite, cameraBackdropRoot, -20);
                var z = 20f + i * 38f + RandomRange(random, -5f, 7f);
                var x = RandomRange(random, -openFieldHalfWidth * 0.72f, openFieldHalfWidth * 0.72f);
                var y = RandomRange(random, 5.8f, 8.6f);
                var scale = RandomRange(random, 0.95f, 1.55f);
                renderer.color = new Color(1f, 1f, 1f, RandomRange(random, 0.34f, 0.56f));
                renderer.transform.position = new Vector3(x, y, z);
                renderer.transform.localScale = Vector3.one * scale;
                scenicBillboards.Add(renderer);
            }
        }

        private void BuildFieldDressing()
        {
            if (snowPuffSprite == null && iceFloeSprite == null)
            {
                return;
            }

            var random = new System.Random(unchecked(System.DateTime.UtcNow.DayOfYear * 13891 + 411));
            var decorRoot = new GameObject("Thumbwaddle Field Dressing").transform;
            decorRoot.SetParent(worldRoot, false);

            for (var i = 0; i < 7; i++)
            {
                var sprite = i % 3 == 0 ? iceFloeSprite : snowPuffSprite;
                if (sprite == null)
                {
                    continue;
                }

                var x = RandomRange(random, -openFieldHalfWidth + 1.2f, openFieldHalfWidth - 1.2f);
                var z = RandomRange(random, 5f, openFieldLength - 4f);
                if (z < 24f && Mathf.Abs(x) < 3.2f)
                {
                    x += x < 0f ? -4.2f : 4.2f;
                }

                var renderer = CreateWorldSprite("Sketch Snow Field Detail", sprite, decorRoot, -1);
                renderer.transform.position = new Vector3(x, RandomRange(random, 0.07f, 0.22f), z);
                renderer.transform.localScale = Vector3.one * RandomRange(random, 0.26f, 0.58f);
                scenicBillboards.Add(renderer);
            }
        }

        private void BuildOpenFieldObstacles(
            Material obstacleShadowMaterial,
            Material obstacleMaterial,
            Material obstacleTopMaterial,
            Material obstacleStrokeMaterial)
        {
            var random = new System.Random(unchecked(System.DateTime.UtcNow.Millisecond * 92821 + steps * 97));
            var count = Mathf.Max(0, openFieldObstacleCount);
            var usableLength = Mathf.Max(20f, openFieldLength - 18f);

            for (var i = 0; i < count; i++)
            {
                var lane = i / (float)Mathf.Max(1, count - 1);
                var z = 13f + lane * usableLength + RandomRange(random, -2.4f, 2.4f);
                var x = RandomRange(random, -openFieldHalfWidth + 2.4f, openFieldHalfWidth - 2.4f);
                if (WalkingRules.IsWarmupCenterLane(z, x))
                {
                    x += x < 0f ? -3.8f : 3.8f;
                }

                var radius = RandomRange(random, 0.42f, 0.82f);
                var center = new Vector2(x, z);
                var width = radius * RandomRange(random, 1.95f, 2.85f);
                var depth = radius * RandomRange(random, 1.65f, 2.35f);
                var obstacleRoot = CreateIcebergObstacle(
                    center,
                    width,
                    depth,
                    RandomRange(random, 1.12f, 1.58f),
                    RandomRange(random, -18f, 18f),
                    obstacleShadowMaterial,
                    obstacleMaterial,
                    obstacleTopMaterial,
                    obstacleStrokeMaterial,
                    random);
                fieldObstacles.Add(new FieldObstacle(center, radius, obstacleRoot, obstacleRoot.GetComponentInChildren<SpriteRenderer>()));
            }
        }

        private Transform CreateIcebergObstacle(
            Vector2 center,
            float width,
            float depth,
            float height,
            float yaw,
            Material shadowMaterial,
            Material bodyMaterial,
            Material capMaterial,
            Material strokeMaterial,
            System.Random random)
        {
            var root = new GameObject("Sketch Iceberg").transform;
            root.SetParent(worldRoot, false);
            root.position = new Vector3(center.x, 0f, center.y);
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (icebergIntactSprite != null)
            {
                var sprite = CreateWorldSprite("Iceberg Doodle", icebergIntactSprite, root, 0);
                var spriteTransform = sprite.transform;
                spriteTransform.localPosition = new Vector3(0f, height * 0.47f, 0f);
                var spriteScale = width / Mathf.Max(0.01f, sprite.bounds.size.x);
                spriteTransform.localScale = Vector3.one * spriteScale;
                return root;
            }

            var groundWash = CreateEllipsoid(
                "Iceberg Ground Wash",
                Vector3.zero,
                new Vector3(width * 1.22f, 0.024f, depth * 1.08f),
                shadowMaterial,
                root);
            groundWash.transform.localPosition = new Vector3(0.05f, 0.012f, -0.05f);

            CreateIcebergShard(
                "Iceberg Main Peak",
                new Vector3(0f, 0.024f, 0f),
                new Vector3(width * 0.92f, height, depth * 0.82f),
                bodyMaterial,
                root,
                RandomRange(random, -8f, 8f),
                RandomRange(random, -0.10f, 0.12f));
            CreateIcebergShard(
                "Iceberg Side Peak",
                new Vector3(-width * 0.32f, 0.018f, depth * 0.10f),
                new Vector3(width * 0.55f, height * 0.72f, depth * 0.58f),
                bodyMaterial,
                root,
                RandomRange(random, -18f, -8f),
                RandomRange(random, -0.14f, 0.04f));
            CreateIcebergShard(
                "Iceberg Rear Peak",
                new Vector3(width * 0.30f, 0.016f, -depth * 0.12f),
                new Vector3(width * 0.48f, height * 0.58f, depth * 0.50f),
                bodyMaterial,
                root,
                RandomRange(random, 8f, 18f),
                RandomRange(random, -0.02f, 0.16f));

            CreateIcebergShard(
                "Iceberg Snowy Tip",
                new Vector3(-width * 0.02f, height * 0.64f, -depth * 0.02f),
                new Vector3(width * 0.46f, height * 0.25f, depth * 0.40f),
                capMaterial,
                root,
                RandomRange(random, -6f, 6f),
                RandomRange(random, -0.04f, 0.08f));

            CreateIcebergStroke(root, strokeMaterial, new Vector3(-width * 0.26f, height * 0.40f, -depth * 0.36f), new Vector3(width * 0.46f, 0.038f, depth * 0.038f), RandomRange(random, -30f, -14f));
            CreateIcebergStroke(root, strokeMaterial, new Vector3(width * 0.20f, height * 0.52f, depth * 0.30f), new Vector3(width * 0.38f, 0.038f, depth * 0.038f), RandomRange(random, 13f, 30f));
            CreateIcebergStroke(root, strokeMaterial, new Vector3(width * -0.03f, height * 0.72f, depth * -0.30f), new Vector3(width * 0.26f, 0.034f, depth * 0.034f), RandomRange(random, -10f, 10f));
            CreateIcebergStroke(root, strokeMaterial, new Vector3(width * 0.36f, height * 0.28f, depth * -0.05f), new Vector3(width * 0.34f, 0.034f, depth * 0.034f), RandomRange(random, 42f, 58f));
            return root;
        }

        private static void CreateIcebergStroke(Transform obstacle, Material material, Vector3 localPosition, Vector3 localScale, float yaw)
        {
            var stroke = CreateCube("Iceberg Ink Stroke", Vector3.zero, localScale, material, obstacle);
            stroke.transform.localPosition = localPosition;
            stroke.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static GameObject CreateIcebergShard(
            string objectName,
            Vector3 localPosition,
            Vector3 scale,
            Material material,
            Transform parent,
            float yaw,
            float peakLean)
        {
            var shard = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            shard.transform.SetParent(parent, false);
            shard.transform.localPosition = localPosition;
            shard.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            shard.transform.localScale = scale;
            shard.GetComponent<MeshFilter>().sharedMesh = CreateIcebergShardMesh(peakLean);
            var renderer = shard.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return shard;
        }

        private static Mesh CreateIcebergShardMesh(float peakLean)
        {
            var vertices = new[]
            {
                new Vector3(-0.50f, 0f, -0.30f),
                new Vector3(-0.16f, 0f, -0.48f),
                new Vector3(0.34f, 0f, -0.42f),
                new Vector3(0.54f, 0f, -0.05f),
                new Vector3(0.38f, 0f, 0.40f),
                new Vector3(-0.10f, 0f, 0.50f),
                new Vector3(-0.48f, 0f, 0.24f),
                new Vector3(-0.56f, 0f, -0.10f),
                new Vector3(-0.30f, 0.46f, -0.18f),
                new Vector3(-0.08f, 0.54f, -0.30f),
                new Vector3(0.24f, 0.50f, -0.24f),
                new Vector3(0.33f, 0.46f, -0.02f),
                new Vector3(0.24f, 0.52f, 0.25f),
                new Vector3(-0.06f, 0.48f, 0.31f),
                new Vector3(-0.30f, 0.44f, 0.14f),
                new Vector3(-0.34f, 0.46f, -0.06f),
                new Vector3(peakLean, 1f, 0.02f),
            };
            var triangles = new List<int>();
            for (var i = 0; i < 8; i++)
            {
                var next = (i + 1) % 8;
                AddDoubleSidedTriangle(triangles, i, i + 8, next + 8);
                AddDoubleSidedTriangle(triangles, i, next + 8, next);
                AddDoubleSidedTriangle(triangles, i + 8, 16, next + 8);
            }

            var mesh = new Mesh
            {
                name = "Thumbwaddle Iceberg Shard",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDoubleSidedTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(a);
        }

        private void BuildPlayerAvatar()
        {
            if (playerRoot != null)
            {
                Destroy(playerRoot.gameObject);
            }

            playerBody = null;
            playerBackMark = null;
            playerHead = null;
            playerFacePatch = null;
            playerBeak = null;
            playerLeftArm = null;
            playerRightArm = null;
            playerLeftFoot = null;
            playerRightFoot = null;
            playerSpriteRenderer = null;
            playerRoot = new GameObject("Thumbwaddle Player").transform;
            playerRoot.SetParent(worldRoot, false);

            if (penguinIdleSprite != null)
            {
                playerSpriteRenderer = CreateWorldSprite("Penguin Doodle Back", penguinIdleSprite, playerRoot, 0);
                playerSpriteRenderer.transform.localPosition = new Vector3(0f, 0.74f, -0.03f);
                playerSpriteRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                playerSpriteRenderer.transform.localScale = Vector3.one * 1.04f;
                return;
            }

            var bodyMaterial = CreateMaterial("Penguin Ink Body", new Color32(35, 38, 39, 255));
            var patchMaterial = CreateMaterial("Penguin Paper Patch", new Color32(255, 253, 247, 255));
            var beakMaterial = CreateMaterial("Penguin Beak", new Color32(247, 181, 71, 255));
            var footMaterial = CreateMaterial("Penguin Feet", new Color32(236, 143, 58, 255));

            playerBody = CreateEllipsoid(
                "Penguin Body",
                Vector3.zero,
                new Vector3(0.62f, 0.88f, 0.46f),
                bodyMaterial,
                playerRoot).transform;
            playerBackMark = CreateEllipsoid(
                "Penguin Back Patch",
                Vector3.zero,
                new Vector3(0.40f, 0.50f, 0.045f),
                patchMaterial,
                playerRoot).transform;
            playerHead = CreateEllipsoid(
                "Penguin Head",
                Vector3.zero,
                new Vector3(0.46f, 0.42f, 0.44f),
                bodyMaterial,
                playerRoot).transform;
            playerFacePatch = CreateEllipsoid(
                "Penguin Face Patch",
                Vector3.zero,
                new Vector3(0.30f, 0.22f, 0.035f),
                patchMaterial,
                playerRoot).transform;
            playerBeak = CreateEllipsoid(
                "Penguin Beak",
                Vector3.zero,
                new Vector3(0.15f, 0.08f, 0.18f),
                beakMaterial,
                playerRoot).transform;
            playerLeftArm = CreateEllipsoid(
                "Penguin Left Flipper",
                Vector3.zero,
                new Vector3(0.16f, 0.48f, 0.16f),
                bodyMaterial,
                playerRoot).transform;
            playerRightArm = CreateEllipsoid(
                "Penguin Right Flipper",
                Vector3.zero,
                new Vector3(0.16f, 0.48f, 0.16f),
                bodyMaterial,
                playerRoot).transform;
            playerLeftFoot = CreateEllipsoid(
                "Penguin Left Foot",
                Vector3.zero,
                new Vector3(0.27f, 0.07f, 0.45f),
                footMaterial,
                playerRoot).transform;
            playerRightFoot = CreateEllipsoid(
                "Penguin Right Foot",
                Vector3.zero,
                new Vector3(0.27f, 0.07f, 0.45f),
                footMaterial,
                playerRoot).transform;
        }

        private void BuildDebugMarkers()
        {
            if (debugRoot != null)
            {
                Destroy(debugRoot.gameObject);
            }

            debugRoot = new GameObject("Debug Foot Markers").transform;
            debugRoot.SetParent(worldRoot, false);
            leftMarker = CreateDebugMarker("Left Foot Debug", new Color32(88, 142, 181, 255));
            rightMarker = CreateDebugMarker("Right Foot Debug", new Color32(247, 181, 71, 255));
            debugRoot.gameObject.SetActive(debugFootMarkers);
        }

        private Transform CreateDebugMarker(string markerName, Color color)
        {
            var marker = CreateCube(
                markerName,
                Vector3.zero,
                new Vector3(0.32f, 0.025f, 0.32f),
                CreateMaterial(markerName + " Material", color),
                debugRoot);
            marker.name = markerName;
            return marker.transform;
        }

        private void BuildUi()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }

        private Image CreateTouchZone(string zoneName, Vector2 min, Vector2 max)
        {
            var zone = new GameObject(zoneName, typeof(Image)).GetComponent<Image>();
            zone.transform.SetParent(canvas.transform, false);
            zone.color = new Color(1f, 1f, 1f, 0.015f);
            zone.raycastTarget = false;
            Stretch(zone.rectTransform, min, max);
            return zone;
        }

        private Image CreatePanel(string panelName, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchored, Vector2 size, Color color)
        {
            var panel = new GameObject(panelName, typeof(Image)).GetComponent<Image>();
            panel.transform.SetParent(canvas.transform, false);
            panel.color = color;
            panel.raycastTarget = false;
            SetRect(panel.rectTransform, min, max, pivot, anchored, size);
            return panel;
        }

        private Image CreateStatusBadge(string badgeName, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchored)
        {
            var badge = new GameObject(badgeName, typeof(Image)).GetComponent<Image>();
            badge.transform.SetParent(canvas.transform, false);
            badge.color = new Color32(255, 253, 247, 222);
            badge.raycastTarget = false;
            SetRect(badge.rectTransform, min, max, pivot, anchored, new Vector2(220f, 62f));
            return badge;
        }

        private Button CreateButton(string buttonName, string label, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
        {
            var buttonImage = new GameObject(buttonName, typeof(Image), typeof(Button)).GetComponent<Image>();
            buttonImage.transform.SetParent(canvas.transform, false);
            buttonImage.color = Warm;
            SetRect(buttonImage.rectTransform, min, max, pivot, Vector2.zero, size);
            var button = buttonImage.GetComponent<Button>();
            var text = CreateText("Label", buttonImage.transform, label, 34, TextAnchor.MiddleCenter, Ink);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return button;
        }

        private Text CreateText(string textName, Transform parent, string textValue, int fontSize, TextAnchor anchor, Color color)
        {
            var text = new GameObject(textName, typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.text = textValue;
            text.color = color;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return text;
        }

        private void HandleInput()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                suppressMouseInputUntil = Time.unscaledTime + 0.18f;
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            BeginPointer(touch.fingerId, touch.position);
                            break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            MovePointer(touch.fingerId, touch.position);
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            EndPointer(touch.fingerId, touch.position);
                            break;
                    }
                }
            }

            if (Time.unscaledTime < suppressMouseInputUntil)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginPointer(-10, Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                MovePointer(-10, Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndPointer(-10, Input.mousePosition);
            }
        }

        private void BeginPointer(int pointerId, Vector2 screenPosition)
        {
            if (activeTouches.TryGetValue(pointerId, out var previousFoot))
            {
                ReleasePointer(previousFoot, pointerId, false);
            }

            if (state == WalkingGameState.Ready)
            {
                state = WalkingGameState.Playing;
            }
            else if (state == WalkingGameState.Result)
            {
                return;
            }

            if (state != WalkingGameState.Playing)
            {
                return;
            }

            var side = WalkingRules.FootSideForScreenPosition(screenPosition, new Vector2(Screen.width, Screen.height));
            var foot = side == WalkingFootSide.Left ? leftFoot : rightFoot;
            if (foot.Mode != InputMode.Idle)
            {
                return;
            }

            foot.FingerId = pointerId;
            foot.ScreenPosition = screenPosition;
            foot.BestStepScreenPosition = screenPosition;
            activeTouches[pointerId] = foot;

            if (foot.NeedsReturn)
            {
                foot.Mode = WalkingRules.IsReturnGesturePosition(screenPosition, new Vector2(Screen.width, Screen.height))
                    ? InputMode.Return
                    : InputMode.Ignored;
                foot.StatusPulse = 1f;
                return;
            }

            foot.Mode = InputMode.Placement;
            var landed = TryLandFoot(foot);
            if (state == WalkingGameState.Playing)
            {
                foot.Mode = landed ? InputMode.LandedHold : InputMode.InvalidHold;
            }
        }

        private void MovePointer(int pointerId, Vector2 screenPosition)
        {
            if (!activeTouches.TryGetValue(pointerId, out var foot))
            {
                return;
            }

            if (foot.Mode == InputMode.Placement)
            {
                foot.ScreenPosition = screenPosition;
                if (screenPosition.y > foot.BestStepScreenPosition.y)
                {
                    foot.BestStepScreenPosition = screenPosition;
                }
            }
            else if (foot.Mode == InputMode.LandedHold)
            {
                foot.ScreenPosition = screenPosition;
                if (foot.NeedsReturn && WalkingRules.IsReturnGesturePosition(screenPosition, new Vector2(Screen.width, Screen.height)))
                {
                    foot.NeedsReturn = false;
                    foot.Mode = InputMode.Return;
                    foot.StatusPulse = 1f;
                }
            }
        }

        private void EndPointer(int pointerId, Vector2 screenPosition)
        {
            if (!activeTouches.TryGetValue(pointerId, out var foot))
            {
                return;
            }

            ReleasePointer(foot, pointerId, true);
        }

        private void ReleasePointer(FootRuntime foot, int pointerId, bool playFeedback)
        {
            if (foot.Mode == InputMode.Return)
            {
                foot.NeedsReturn = false;
                foot.StatusPulse = 1f;
            }
            else if (playFeedback && foot.Mode == InputMode.Ignored)
            {
                invalidPulse = 1f;
                foot.StatusPulse = 1f;
            }

            foot.Mode = InputMode.Idle;
            foot.FingerId = int.MinValue;
            activeTouches.Remove(pointerId);
        }

        private void UpdateCandidates()
        {
            UpdateCandidate(leftFoot);
            UpdateCandidate(rightFoot);
        }

        private void UpdateCandidate(FootRuntime foot)
        {
            if (foot.Mode != InputMode.Placement)
            {
                foot.Candidate = default;
                return;
            }

            var support = foot.Side == WalkingFootSide.Left ? rightFootPosition : leftFootPosition;
            var candidate = WalkingRules.BuildFootCandidate(
                foot.Side,
                support,
                facing,
                foot.BestStepScreenPosition,
                new Vector2(Screen.width, Screen.height));
            var placement = WalkingRules.ValidateFootPlacement(foot.Side, support, candidate, facing, maze);
            foot.Candidate = placement.IsValid && IsCircleTouchingFieldObstacle(candidate, WalkingRules.FootRadius)
                ? new WalkingFootPlacement(candidate, false, "obstacle")
                : placement;
        }

        private bool TryLandFoot(FootRuntime foot)
        {
            UpdateCandidate(foot);
            if (!foot.Candidate.IsValid)
            {
                if (foot.Candidate.Reason == "obstacle" && DamageFieldObstacleAt(foot.Candidate.Position, WalkingRules.FootRadius))
                {
                    UpdateCandidate(foot);
                    if (foot.Candidate.IsValid)
                    {
                        return TryLandFoot(foot);
                    }
                }

                invalidPulse = foot.Candidate.Reason == "obstacle" ? 0.55f : 1f;
                foot.StatusPulse = 1f;
                PlayBump(0.35f);
                return false;
            }

            previousBodyPosition = bodyPosition;
            var oldBody = bodyPosition;
            var oldFoot = foot.Side == WalkingFootSide.Left ? leftFootPosition : rightFootPosition;
            var proposedLeftFoot = foot.Side == WalkingFootSide.Left ? foot.Candidate.Position : leftFootPosition;
            var proposedRightFoot = foot.Side == WalkingFootSide.Right ? foot.Candidate.Position : rightFootPosition;
            var proposedBody = WalkingRules.BodyCenter(proposedLeftFoot, proposedRightFoot);

            if (IsCircleTouchingFieldObstacle(proposedBody, WalkingRules.BodyRadius))
            {
                var clearedObstacle = DamageFieldObstacleAt(proposedBody, WalkingRules.BodyRadius);
                if (!clearedObstacle || IsCircleTouchingFieldObstacle(proposedBody, WalkingRules.BodyRadius))
                {
                    invalidPulse = 0.58f;
                    foot.StatusPulse = 1f;
                    PlayBump(0.45f);
                    return false;
                }
            }

            if (foot.Side == WalkingFootSide.Left)
            {
                leftFootPosition = foot.Candidate.Position;
            }
            else
            {
                rightFootPosition = foot.Candidate.Position;
            }

            bodyPosition = WalkingRules.BodyCenter(leftFootPosition, rightFootPosition);
            var footForward = WalkingRules.FacingFromFeet(leftFootPosition, rightFootPosition, facing);
            var stepDirection = bodyPosition - oldBody;
            if (stepDirection.sqrMagnitude > 0.0001f)
            {
                var runForward = Vector2.Lerp(stepDirection.normalized, Vector2.up, 0.42f).normalized;
                facing = Vector2.Lerp(footForward, runForward, 0.74f).normalized;
            }
            else
            {
                facing = Vector2.Lerp(footForward, Vector2.up, 0.42f).normalized;
            }

            var traveled = Vector2.Distance(oldBody, bodyPosition);
            distanceMeters += traveled;
            steps++;
            CheckGoalProgress();
            foot.NeedsReturn = true;
            foot.StatusPulse = 1f;
            lastLandedSide = foot.Side;
            bodyLeanPulse = 1f;
            bobImpulse = Mathf.Min(1f, bobImpulse + stepBobStrength + Vector2.Distance(oldFoot, foot.Candidate.Position) * 0.04f);
            CreateStepStamp(foot.Candidate.Position, foot.Side);
            PlayStep();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "step",
                new Dictionary<string, string>
                {
                    { "side", foot.Side.ToString().ToLowerInvariant() },
                    { "distance_m", distanceMeters.ToString("0.0") },
                    { "step_m", traveled.ToString("0.00") },
                    { "steps", steps.ToString() },
                    { "time_remaining_s", Mathf.CeilToInt(runTimeRemaining).ToString() }
                });

            return true;
        }

        private void CheckGoalProgress()
        {
            while (nextGoalMarkerIndex < GoalMarkerDistances.Length
                && distanceMeters >= GoalMarkerDistances[nextGoalMarkerIndex])
            {
                reachedGoalMarkers++;
                milestonePulse = 1f;
                bobImpulse = Mathf.Min(1f, bobImpulse + 0.20f);
                CreateGoalBurst(bodyPosition, false);
                MarkGoalReached(GoalMarkerDistances[nextGoalMarkerIndex], false);
                PlayReward(false);
                nextGoalMarkerIndex++;
            }

            if (!bestMarkerPassedThisRun
                && bestDistanceAtRunStart >= 1f
                && distanceMeters >= bestDistanceAtRunStart)
            {
                bestMarkerPassedThisRun = true;
                bestPassPulse = 1f;
                milestonePulse = 1f;
                bobImpulse = Mathf.Min(1f, bobImpulse + 0.32f);
                CreateGoalBurst(bodyPosition, true);
                MarkGoalReached(bestMarkerDistanceThisRun, true);
                PlayReward(true);
                FirebaseTelemetry.LogEvent(
                    "best_marker_passed",
                    new Dictionary<string, string>
                    {
                        { "distance_m", distanceMeters.ToString("0.0") },
                        { "best_distance_m", bestDistanceAtRunStart.ToString("0.0") },
                        { "steps", steps.ToString() }
                    });
            }
        }

        private void MarkGoalReached(float distance, bool best)
        {
            for (var i = 0; i < goalMarkers.Count; i++)
            {
                var marker = goalMarkers[i];
                if (marker.IsBest != best || Mathf.Abs(marker.Distance - distance) > 0.08f)
                {
                    continue;
                }

                marker.Reached = true;
                return;
            }
        }

        private void UpdateRunTimer()
        {
            if (state != WalkingGameState.Playing)
            {
                return;
            }

            runTimeRemaining = Mathf.Max(0f, runTimeRemaining - Time.deltaTime);
            if (runTimeRemaining <= 0f)
            {
                EndRun();
            }
        }

        private bool IsCircleTouchingFieldObstacle(Vector2 center, float radius)
        {
            for (var i = 0; i < fieldObstacles.Count; i++)
            {
                var obstacle = fieldObstacles[i];
                if (obstacle.Radius <= 0f)
                {
                    continue;
                }

                if (Vector2.Distance(center, obstacle.Center) <= radius + obstacle.Radius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool DamageFieldObstacleAt(Vector2 center, float radius)
        {
            var index = FindTouchedFieldObstacle(center, radius);
            if (index < 0)
            {
                return false;
            }

            var obstacle = fieldObstacles[index];
            obstacle.Hits++;
            obstacleBumpPulse = 1f;
            bodyLeanPulse = Mathf.Max(bodyLeanPulse, 0.62f);
            var cleared = obstacle.Hits >= 3;
            obstacle.Radius = cleared ? 0f : obstacle.BaseRadius * GetIcebergCollisionScale(obstacle.Hits);
            if (obstacle.Root != null)
            {
                var visualScale = GetIcebergVisualScale(obstacle.Hits);
                if (cleared)
                {
                    if (obstacle.Renderer != null)
                    {
                        obstacle.Renderer.sprite = icebergBrokenSprite ?? icebergCrackedTwoSprite ?? icebergCrackedOneSprite ?? icebergIntactSprite;
                    }

                    obstacle.Root.localScale = Vector3.one * visualScale;
                    Destroy(obstacle.Root.gameObject, 0.38f);
                }
                else if (obstacle.Renderer != null)
                {
                    obstacle.Renderer.sprite = obstacle.Hits <= 1
                        ? icebergCrackedOneSprite ?? icebergIntactSprite
                        : icebergCrackedTwoSprite ?? icebergCrackedOneSprite ?? icebergIntactSprite;
                    var wobble = obstacle.Hits % 2 == 0 ? -4f : 4f;
                    obstacle.Root.localScale = Vector3.one * visualScale;
                    obstacle.Root.localRotation *= Quaternion.Euler(0f, wobble, 0f);
                }
                else
                {
                    var wobble = obstacle.Hits % 2 == 0 ? -6f : 6f;
                    obstacle.Root.localScale = Vector3.one * visualScale;
                    obstacle.Root.localRotation *= Quaternion.Euler(0f, wobble, 0f);
                }
            }

            if (cleared)
            {
                fieldObstacles.RemoveAt(index);
                brokenIcebergs++;
                CreateIceChipBurst(obstacle.Center);
                FirebaseTelemetry.LogEvent(
                    "obstacle_chip",
                    new Dictionary<string, string>
                    {
                        { "cleared", "true" },
                        { "hits", obstacle.Hits.ToString() },
                        { "distance_m", distanceMeters.ToString("0.0") }
                    });
                return true;
            }

            fieldObstacles[index] = obstacle;
            FirebaseTelemetry.LogEvent(
                "obstacle_chip",
                new Dictionary<string, string>
                {
                    { "cleared", "false" },
                    { "hits", obstacle.Hits.ToString() },
                    { "distance_m", distanceMeters.ToString("0.0") }
                });
            return false;
        }

        private static float GetIcebergVisualScale(int hits)
        {
            if (hits <= 0)
            {
                return 1f;
            }

            return hits == 1 ? 0.84f : hits == 2 ? 0.68f : 0.46f;
        }

        private static float GetIcebergCollisionScale(int hits)
        {
            if (hits <= 0)
            {
                return 1f;
            }

            return hits == 1 ? 0.56f : 0.24f;
        }

        private int FindTouchedFieldObstacle(Vector2 center, float radius)
        {
            var bestIndex = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < fieldObstacles.Count; i++)
            {
                var obstacle = fieldObstacles[i];
                if (obstacle.Radius <= 0f)
                {
                    continue;
                }

                var distance = Vector2.Distance(center, obstacle.Center);
                if (distance > radius + obstacle.Radius || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = i;
            }

            return bestIndex;
        }

        private void EndRun()
        {
            state = WalkingGameState.Result;
            PlayBump(1f);
            if (distanceMeters > bestDistanceMeters)
            {
                bestDistanceMeters = distanceMeters;
                bestUpdatedThisRun = true;
                PlayerPrefs.SetFloat(BestDistanceKey, bestDistanceMeters);
                PlayerPrefs.Save();
            }

            activeTouches.Clear();
            ResetFootRuntime(leftFoot);
            ResetFootRuntime(rightFoot);
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "run_end",
                new Dictionary<string, string>
                {
                    { "distance_m", distanceMeters.ToString("0.0") },
                    { "best_distance_m", bestDistanceMeters.ToString("0.0") },
                    { "steps", steps.ToString() },
                    { "duration_s", Mathf.RoundToInt(runDurationSeconds).ToString() },
                    { "broken_icebergs", brokenIcebergs.ToString() },
                    { "goal_markers", reachedGoalMarkers.ToString() },
                    { "new_best", bestUpdatedThisRun ? "true" : "false" },
                    { "obstacles_remaining", CountRemainingObstacles().ToString() }
                });
            MannLabAdMob.TryShowGameOverInterstitial();
        }

        private static void InitializeTelemetryAndAds()
        {
            try
            {
                FirebaseTelemetry.Initialize();
                FirebaseTelemetry.SetContext("game", "thumbwaddle");
                FirebaseTelemetry.LogEvent("app_open");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Thumbwaddle] Firebase initialization skipped: {exception.GetType().Name}");
            }

            try
            {
                MannLabAdMob.InitializeGameOverInterstitial(
                    "thumbwaddle",
                    ProductionIosInterstitialAdUnitId,
                    GameOverInterstitialInterval,
                    ProductionAndroidInterstitialAdUnitId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Thumbwaddle] AdMob initialization skipped: {exception.GetType().Name}");
            }
        }

        private void UpdateTelemetryContext()
        {
            FirebaseTelemetry.SetContext("distance_m", distanceMeters.ToString("0.0"));
            FirebaseTelemetry.SetContext("best_distance_m", bestDistanceMeters.ToString("0.0"));
            FirebaseTelemetry.SetContext("steps", steps.ToString());
            FirebaseTelemetry.SetContext("time_remaining_s", Mathf.CeilToInt(runTimeRemaining).ToString());
            FirebaseTelemetry.SetContext("obstacles_remaining", CountRemainingObstacles().ToString());
            FirebaseTelemetry.SetContext("goal_markers", reachedGoalMarkers.ToString());
            FirebaseTelemetry.SetContext("state", state.ToString());
        }

        private int CountRemainingObstacles()
        {
            var count = 0;
            for (var i = 0; i < fieldObstacles.Count; i++)
            {
                if (fieldObstacles[i].Radius > 0f)
                {
                    count++;
                }
            }

            return count;
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
                    { "distance_m", distanceMeters.ToString("0.0") },
                    { "best_distance_m", bestDistanceMeters.ToString("0.0") },
                    { "steps", steps.ToString() },
                    { "time_remaining_s", Mathf.CeilToInt(runTimeRemaining).ToString() }
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

        private void UpdateCamera(bool snap = false)
        {
            if (gameCamera == null)
            {
                return;
            }

            var moveT = snap ? 1f : 1f - Mathf.Exp(-cameraMoveLerp * Time.deltaTime);
            cameraBodyPosition = Vector2.Lerp(cameraBodyPosition, visualBodyPosition, moveT);
            var bob = Mathf.Sin((1f - bobImpulse) * Mathf.PI) * bobImpulse;
            var forward3 = new Vector3(visualFacing.x, 0f, visualFacing.y);
            if (forward3.sqrMagnitude < 0.001f)
            {
                forward3 = Vector3.forward;
            }

            forward3.Normalize();
            var basePosition = new Vector3(cameraBodyPosition.x, 0f, cameraBodyPosition.y);
            var targetPosition = basePosition - forward3 * thirdPersonDistance + Vector3.up * (thirdPersonHeight + bob * 0.42f);
            var lookTarget = basePosition + forward3 * thirdPersonLookAhead + Vector3.up * Mathf.Max(0.82f, eyeHeight * 0.54f);
            gameCamera.transform.position = targetPosition;

            var targetRotation = Quaternion.LookRotation((lookTarget - targetPosition).normalized, Vector3.up);
            gameCamera.transform.rotation = snap
                ? targetRotation
                : Quaternion.Slerp(gameCamera.transform.rotation, targetRotation, 1f - Mathf.Exp(-cameraTurnLerp * Time.deltaTime));
        }

        private void UpdateBillboards()
        {
            if (gameCamera == null)
            {
                return;
            }

            for (var i = 0; i < fieldObstacles.Count; i++)
            {
                var renderer = fieldObstacles[i].Renderer;
                if (renderer == null)
                {
                    continue;
                }

                FaceCameraYaw(renderer.transform, gameCamera);
            }

            for (var i = scenicBillboards.Count - 1; i >= 0; i--)
            {
                var renderer = scenicBillboards[i];
                if (renderer == null)
                {
                    scenicBillboards.RemoveAt(i);
                    continue;
                }

                FaceCameraYaw(renderer.transform, gameCamera);
            }
        }

        private void UpdateGoalMarkers()
        {
            for (var i = 0; i < goalMarkers.Count; i++)
            {
                var marker = goalMarkers[i];
                if (marker.Root == null)
                {
                    continue;
                }

                var reachedScale = marker.Reached ? 1.10f : 1f;
                var pulse = marker.IsBest ? bestPassPulse : marker.Reached ? milestonePulse * 0.18f : 0f;
                marker.Root.localScale = Vector3.one * (reachedScale + pulse * 0.18f);
                if (marker.Material != null)
                {
                    var target = marker.Reached
                        ? Color.Lerp(marker.BaseColor, Paper, marker.IsBest ? 0.08f : 0.28f)
                        : marker.BaseColor;
                    if (marker.IsBest && !marker.Reached)
                    {
                        target = Color.Lerp(Warm, Paper, 0.18f + Pulse01(Time.time * 1.4f) * 0.12f);
                    }

                    SetMaterialColor(marker.Material, target);
                }
            }
        }

        private static void FaceCameraYaw(Transform target, Camera camera)
        {
            var toCamera = camera.transform.position - target.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                return;
            }

            target.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        private void UpdatePlayerAvatar(bool snap = false)
        {
            if (playerRoot == null)
            {
                return;
            }

            var moveT = snap ? 1f : 1f - Mathf.Exp(-avatarMoveLerp * Time.deltaTime);
            var turnT = snap ? 1f : 1f - Mathf.Exp(-avatarTurnLerp * Time.deltaTime);
            visualBodyPosition = Vector2.Lerp(visualBodyPosition, bodyPosition, moveT);
            visualFacing = Vector2.Lerp(visualFacing, facing, turnT);
            if (visualFacing.sqrMagnitude < 0.001f)
            {
                visualFacing = Vector2.up;
            }

            visualFacing.Normalize();
            var forward3 = new Vector3(visualFacing.x, 0f, visualFacing.y);
            var targetRotation = Quaternion.LookRotation(forward3, Vector3.up);
            var rootTarget = new Vector3(visualBodyPosition.x, 0f, visualBodyPosition.y);
            var bodyBob = Mathf.Sin((1f - bobImpulse) * Mathf.PI) * bobImpulse * 0.14f;
            var landedSide = lastLandedSide == WalkingFootSide.Left ? -1f : 1f;
            var lean = landedSide * bodyLeanPulse;

            playerRoot.position = rootTarget;
            playerRoot.rotation = targetRotation;

            if (playerSpriteRenderer != null)
            {
                UpdatePenguinSprite(lean, bodyBob);
                return;
            }

            if (playerBody != null)
            {
                playerBody.localPosition = new Vector3(lean * 0.045f, 0.45f + bodyBob - bodyLeanPulse * 0.025f, 0f);
                playerBody.localRotation = Quaternion.Euler(0f, 0f, -lean * 7f);
                playerBody.localScale = new Vector3(0.62f + bodyLeanPulse * 0.035f, 0.88f - bodyLeanPulse * 0.045f, 0.46f);
            }

            if (playerBackMark != null)
            {
                playerBackMark.localPosition = new Vector3(lean * 0.045f, 0.40f + bodyBob - bodyLeanPulse * 0.025f, -0.238f);
                playerBackMark.localRotation = Quaternion.Euler(0f, 0f, -lean * 7f);
                playerBackMark.localScale = new Vector3(0.24f, 0.16f, 0.035f);
            }

            if (playerHead != null)
            {
                playerHead.localPosition = new Vector3(lean * 0.065f, 1.03f + bodyBob - bodyLeanPulse * 0.018f, 0.02f);
                playerHead.localRotation = Quaternion.Euler(0f, 0f, -lean * 5f);
                playerHead.localScale = new Vector3(0.46f, 0.42f, 0.44f);
            }

            if (playerFacePatch != null)
            {
                playerFacePatch.localPosition = new Vector3(lean * 0.066f, 1.03f + bodyBob - bodyLeanPulse * 0.018f, 0.238f);
                playerFacePatch.localRotation = Quaternion.Euler(0f, 0f, -lean * 5f);
                playerFacePatch.localScale = new Vector3(0.30f, 0.22f, 0.035f);
            }

            if (playerBeak != null)
            {
                playerBeak.localPosition = new Vector3(lean * 0.066f, 0.99f + bodyBob - bodyLeanPulse * 0.018f, 0.305f);
                playerBeak.localRotation = Quaternion.Euler(4f, 0f, -lean * 5f);
                playerBeak.localScale = new Vector3(0.15f, 0.08f, 0.18f);
            }

            if (playerLeftArm != null)
            {
                var swing = Mathf.Clamp01(rightFoot.StatusPulse + bobImpulse * 2.6f);
                playerLeftArm.localPosition = new Vector3(-0.255f + lean * 0.024f, 0.70f + bodyBob * 0.55f, -0.045f + swing * 0.035f);
                playerLeftArm.localRotation = Quaternion.Euler(7f + swing * 10f, 0f, -38f - lean * 3f);
                playerLeftArm.localScale = new Vector3(0.17f, 0.42f, 0.15f);
            }

            if (playerRightArm != null)
            {
                var swing = Mathf.Clamp01(leftFoot.StatusPulse + bobImpulse * 2.6f);
                playerRightArm.localPosition = new Vector3(0.255f + lean * 0.024f, 0.70f + bodyBob * 0.55f, -0.045f + swing * 0.035f);
                playerRightArm.localRotation = Quaternion.Euler(7f + swing * 10f, 0f, 38f - lean * 3f);
                playerRightArm.localScale = new Vector3(0.17f, 0.42f, 0.15f);
            }

            SetAvatarFoot(playerLeftFoot, leftFootPosition, leftFoot.StatusPulse, targetRotation, snap);
            SetAvatarFoot(playerRightFoot, rightFootPosition, rightFoot.StatusPulse, targetRotation, snap);
        }

        private void UpdatePenguinSprite(float lean, float bodyBob)
        {
            var sprite = penguinIdleSprite;
            if (state == WalkingGameState.Result && penguinHappySprite != null)
            {
                sprite = penguinHappySprite;
            }
            else if (Mathf.Max(invalidPulse, obstacleBumpPulse) > 0.05f && penguinStumbleSprite != null)
            {
                sprite = penguinStumbleSprite;
            }
            else if (bodyLeanPulse > 0.05f)
            {
                sprite = lastLandedSide == WalkingFootSide.Left
                    ? penguinLeftStepSprite ?? penguinIdleSprite
                    : penguinRightStepSprite ?? penguinIdleSprite;
            }

            playerSpriteRenderer.sprite = sprite;
            var spriteTransform = playerSpriteRenderer.transform;
            var stumble = Mathf.Max(invalidPulse, obstacleBumpPulse);
            var wobble = stumble > 0.05f ? Mathf.Sin(Time.time * 34f) * stumble * 0.048f : 0f;
            var celebrate = Mathf.Max(bestPassPulse, milestonePulse * 0.45f);
            if (state == WalkingGameState.Result && bestUpdatedThisRun)
            {
                celebrate = Mathf.Max(celebrate, 0.45f + Pulse01(Time.time * 1.3f) * 0.28f);
            }

            var hop = Mathf.Sin((1f - celebrate) * Mathf.PI) * celebrate * 0.08f;
            spriteTransform.localPosition = new Vector3(lean * 0.035f + wobble, 0.74f + bodyBob * 0.75f - bodyLeanPulse * 0.025f + hop, -0.03f);
            spriteTransform.localRotation = Quaternion.Euler(0f, 180f, -lean * 5f - wobble * 120f);
            spriteTransform.localScale = Vector3.one * (1.04f + bodyLeanPulse * 0.045f + celebrate * 0.055f);
        }

        private static void SetAvatarFoot(Transform foot, Vector2 position, float pulse, Quaternion rotation, bool snap)
        {
            if (foot == null)
            {
                return;
            }

            var target = new Vector3(position.x, 0.055f + pulse * 0.035f, position.y);
            var t = snap || pulse > 0.72f ? 1f : 1f - Mathf.Exp(-20f * Time.deltaTime);
            foot.position = snap ? target : Vector3.Lerp(foot.position, target, t);
            foot.rotation = snap ? rotation : Quaternion.Slerp(foot.rotation, rotation, t);
            var spread = 1f + Mathf.Clamp01(pulse) * 0.16f;
            foot.localScale = new Vector3(0.27f * spread, 0.07f, 0.45f * spread);
        }

        private void CreateStepStamp(Vector2 position, WalkingFootSide side)
        {
            if (worldRoot == null)
            {
                return;
            }

            var color = side == WalkingFootSide.Left ? Blue : Warm;
            var material = CreateMaterial("Step Stamp Material", color);
            var root = new GameObject("Step Ink Stamp").transform;
            root.SetParent(worldRoot, false);
            root.position = new Vector3(position.x, 0.075f, position.y);
            var forward3 = new Vector3(facing.x, 0f, facing.y);
            if (forward3.sqrMagnitude < 0.001f)
            {
                forward3 = Vector3.forward;
            }

            root.rotation = Quaternion.LookRotation(forward3.normalized, Vector3.up);
            CreateStampBar(root, material, new Vector3(0f, 0f, 0.22f), new Vector3(0.30f, 0.018f, 0.045f));
            CreateStampBar(root, material, new Vector3(0f, 0f, -0.22f), new Vector3(0.30f, 0.018f, 0.045f));
            CreateStampBar(root, material, new Vector3(-0.17f, 0f, 0f), new Vector3(0.045f, 0.018f, 0.36f));
            CreateStampBar(root, material, new Vector3(0.17f, 0f, 0f), new Vector3(0.045f, 0.018f, 0.36f));
            stepStamps.Add(new StepStamp
            {
                Root = root,
                Material = material,
                Color = color,
                BaseScale = Vector3.one,
                Age = 0f
            });
        }

        private void CreateIceChipBurst(Vector2 position)
        {
            if (worldRoot == null || iceChipSprite == null)
            {
                return;
            }

            for (var i = 0; i < 3; i++)
            {
                var renderer = CreateWorldSprite("Ice Chip Burst", iceChipSprite, worldRoot, 1);
                var angle = (i / 3f) * Mathf.PI * 2f + (Time.time % 1f);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (0.15f + i * 0.06f);
                renderer.transform.position = new Vector3(position.x + offset.x, 0.16f + i * 0.03f, position.y + offset.y);
                renderer.transform.localScale = Vector3.one * (0.24f + i * 0.04f);
                scenicBillboards.Add(renderer);
                stepStamps.Add(new StepStamp
                {
                    Root = renderer.transform,
                    Material = null,
                    Color = Color.white,
                    BaseScale = renderer.transform.localScale,
                    Age = 0.10f * i
                });
            }
        }

        private void CreateGoalBurst(Vector2 position, bool best)
        {
            if (worldRoot == null)
            {
                return;
            }

            var color = best ? Warm : Green;
            var count = best ? 6 : 3;
            for (var i = 0; i < count; i++)
            {
                var material = CreateMaterial((best ? "Best Burst Ink " : "Goal Burst Ink ") + i, color);
                var root = new GameObject(best ? "Best Waddle Burst" : "Goal Waddle Burst").transform;
                root.SetParent(worldRoot, false);
                var angle = (i / (float)count) * Mathf.PI * 2f;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (best ? 0.46f : 0.32f);
                root.position = new Vector3(position.x + offset.x, 0.11f + i * 0.006f, position.y + offset.y);
                root.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                CreateStampBar(root, material, Vector3.zero, new Vector3(best ? 0.20f : 0.14f, 0.014f, 0.035f));
                stepStamps.Add(new StepStamp
                {
                    Root = root,
                    Material = material,
                    Color = color,
                    BaseScale = Vector3.one,
                    Age = i * 0.018f
                });
            }
        }

        private void UpdateStepStamps()
        {
            const float lifetime = 0.38f;
            for (var i = stepStamps.Count - 1; i >= 0; i--)
            {
                var stamp = stepStamps[i];
                if (stamp.Root == null)
                {
                    stepStamps.RemoveAt(i);
                    continue;
                }

                stamp.Age += Time.deltaTime;
                var t = Mathf.Clamp01(stamp.Age / lifetime);
                var baseScale = stamp.BaseScale == Vector3.zero ? Vector3.one : stamp.BaseScale;
                stamp.Root.localScale = baseScale * Mathf.Lerp(0.76f, 1.32f, Mathf.SmoothStep(0f, 1f, t));
                if (stamp.Material != null)
                {
                    SetMaterialColor(stamp.Material, Color.Lerp(stamp.Color, Paper, t * 0.82f));
                }
                else
                {
                    var renderer = stamp.Root.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), t);
                    }
                }
                if (stamp.Age >= lifetime)
                {
                    Destroy(stamp.Root.gameObject);
                    stepStamps.RemoveAt(i);
                }
            }
        }

        private void ClearStepStamps()
        {
            for (var i = 0; i < stepStamps.Count; i++)
            {
                if (stepStamps[i].Root != null)
                {
                    Destroy(stepStamps[i].Root.gameObject);
                }
            }

            stepStamps.Clear();
        }

        private static void CreateStampBar(Transform root, Material material, Vector3 localPosition, Vector3 localScale)
        {
            var bar = CreateCube("Step Stamp Stroke", Vector3.zero, localScale, material, root);
            bar.transform.localPosition = localPosition;
        }

        private void UpdateUi()
        {
            if (distanceText == null)
            {
                return;
            }

            distanceText.text = $"{distanceMeters:0.0} m";
            bestText.text = $"{Mathf.CeilToInt(runTimeRemaining)} s";
            stepText.text = state == WalkingGameState.Playing ? steps.ToString() : string.Empty;

            var ready = state == WalkingGameState.Ready;
            titleText.gameObject.SetActive(false);
            hintText.gameObject.SetActive(false);
            titleText.text = "Thumbwaddle";
            hintText.text = string.Empty;

            resultText.gameObject.SetActive(state == WalkingGameState.Result);
            restartButton.gameObject.SetActive(state == WalkingGameState.Result);
            if (state == WalkingGameState.Result)
            {
                var bestLine = bestUpdatedThisRun ? "New Best!" : $"Best {bestDistanceMeters:0.0} m";
                resultText.text = $"{ResultRating()}\n{distanceMeters:0.0} m\n{bestLine}\n{steps} steps  {brokenIcebergs} ice";
            }

            ApplyFootStatus(leftFoot, leftStatusText, leftStatusBadge, leftTouchZone);
            ApplyFootStatus(rightFoot, rightStatusText, rightStatusBadge, rightTouchZone);
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            var scale = Mathf.Clamp(Screen.width / 720f, 0.8f, 1.55f);
            var margin = 22f * scale;
            var topHeight = 82f * scale;
            if (state == WalkingGameState.Playing)
            {
                DrawInputGuide(scale, steps < 5 || leftFoot.NeedsReturn || rightFoot.NeedsReturn);
            }

            if (state == WalkingGameState.Ready)
            {
                DrawReadyCoach(scale);
                return;
            }

            var aheadOfBestPace = IsAheadOfBestPace();
            var hudTint = aheadOfBestPace
                ? new Color(Warm.r, Warm.g, Warm.b, 0.13f + bestPassPulse * 0.12f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.10f);
            DrawGuiRect(new Rect(margin, margin, Screen.width - margin * 2f, topHeight), new Color(1f, 0.99f, 0.96f, 0.72f));
            DrawGuiRect(new Rect(margin, margin, Screen.width - margin * 2f, topHeight), hudTint);
            var secondsLeft = Mathf.CeilToInt(runTimeRemaining);
            var lastSecondsPulse = secondsLeft <= 5 ? Pulse01(Time.unscaledTime * 4.2f) : 0f;
            var timerColor = secondsLeft <= 5 ? new Color(Red.r, Red.g, Red.b, 0.18f + lastSecondsPulse * 0.12f) : new Color(Warm.r, Warm.g, Warm.b, 0.18f);
            var timerSize = (74f + lastSecondsPulse * 7f) * scale;
            DrawCircle(CenteredRect(new Vector2(Screen.width * 0.5f, margin + topHeight * 0.5f), timerSize, timerSize), timerColor);
            DrawRing(CenteredRect(new Vector2(Screen.width * 0.5f, margin + topHeight * 0.5f), timerSize + 4f * scale, timerSize + 4f * scale), secondsLeft <= 5 ? new Color(Red.r, Red.g, Red.b, 0.62f + lastSecondsPulse * 0.18f) : new Color(Warm.r, Warm.g, Warm.b, 0.52f));
            GUI.Label(new Rect(margin + 20f * scale, margin + 6f * scale, 300f * scale, topHeight), $"{distanceMeters:0.0} m", hudStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 80f * scale, margin + 7f * scale, 160f * scale, topHeight), $"{secondsLeft}s", smallHudStyle);
            GUI.Label(new Rect(Screen.width - margin - 210f * scale, margin + 10f * scale, 190f * scale, topHeight), $"BEST {bestDistanceMeters:0.0}", guideStyle);
            DrawProgressDots(new Rect(margin + 22f * scale, margin + topHeight - 17f * scale, 156f * scale, 9f * scale), scale);

            if (state == WalkingGameState.Playing && (steps < 5 || leftFoot.NeedsReturn || rightFoot.NeedsReturn || leftFoot.Mode != InputMode.Idle || rightFoot.Mode != InputMode.Idle))
            {
                DrawFootSignal(leftFoot, new Rect(margin, Screen.height - 82f * scale, 88f * scale, 42f * scale), scale * 0.82f);
                DrawFootSignal(rightFoot, new Rect(Screen.width - margin - 88f * scale, Screen.height - 82f * scale, 88f * scale, 42f * scale), scale * 0.82f);
            }

            if (state == WalkingGameState.Result)
            {
                DrawResultOverlay(scale);
            }
        }

        private void DrawResultOverlay(float scale)
        {
            var panel = new Rect(Screen.width * 0.14f, Screen.height * 0.235f, Screen.width * 0.72f, 292f * scale);
            DrawGuiRect(panel, new Color(1f, 0.99f, 0.96f, 0.90f));
            DrawGuiRect(new Rect(panel.x, panel.y, panel.width, 7f * scale), bestUpdatedThisRun ? new Color(Warm.r, Warm.g, Warm.b, 0.78f) : new Color(Blue.r, Blue.g, Blue.b, 0.34f));
            GUI.Label(new Rect(panel.x, panel.y + 18f * scale, panel.width, 42f * scale), ResultRating(), titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 72f * scale, panel.width, 62f * scale), $"{distanceMeters:0.0} m", resultMetricStyle);
            var bestLine = bestUpdatedThisRun ? "NEW BEST" : $"BEST {bestDistanceMeters:0.0} m";
            GUI.Label(new Rect(panel.x, panel.y + 138f * scale, panel.width, 30f * scale), bestLine, guideStyle);
            GUI.Label(new Rect(panel.x, panel.y + 176f * scale, panel.width, 32f * scale), $"{steps} steps  {brokenIcebergs} ice  {reachedGoalMarkers} flags", resultDetailStyle);
            var buttonRect = new Rect(panel.x + panel.width * 0.28f, panel.y + panel.height - 66f * scale, panel.width * 0.44f, 50f * scale);
            if (GUI.Button(buttonRect, "Restart", buttonStyle))
            {
                ResetRun();
            }
        }

        private bool IsAheadOfBestPace()
        {
            if (bestDistanceAtRunStart < 1f || state != WalkingGameState.Playing)
            {
                return true;
            }

            var elapsed = Mathf.Clamp(runDurationSeconds - runTimeRemaining, 0.35f, runDurationSeconds);
            var projectedDistance = distanceMeters / elapsed * runDurationSeconds;
            return projectedDistance >= bestDistanceAtRunStart * 0.98f;
        }

        private string ResultRating()
        {
            if (bestUpdatedThisRun)
            {
                return "Best Waddle!";
            }

            if (distanceMeters >= 75f)
            {
                return "Long March";
            }

            if (distanceMeters >= 35f)
            {
                return "Good Slide";
            }

            return "Tiny Waddle";
        }

        private void DrawProgressDots(Rect rect, float scale)
        {
            if (GoalMarkerDistances.Length == 0)
            {
                return;
            }

            var gap = rect.width / Mathf.Max(1, GoalMarkerDistances.Length - 1);
            for (var i = 0; i < GoalMarkerDistances.Length; i++)
            {
                var reached = i < reachedGoalMarkers;
                var color = reached
                    ? new Color(Warm.r, Warm.g, Warm.b, 0.78f)
                    : new Color(Ink.r, Ink.g, Ink.b, 0.18f);
                DrawCircle(CenteredRect(new Vector2(rect.x + gap * i, rect.center.y), 7f * scale, 7f * scale), color);
            }
        }

        private void DrawInputGuide(float scale, bool showLabels)
        {
            var splitTop = Screen.height * 0.16f;
            var returnTop = Screen.height * (1f - WalkingRules.ReturnGestureMaxScreenY);
            var lowHeight = Screen.height - returnTop;
            var leftLowActive = leftFoot.NeedsReturn || leftFoot.Mode == InputMode.Return;
            var rightLowActive = rightFoot.NeedsReturn || rightFoot.Mode == InputMode.Return;
            var leftLowColor = leftLowActive
                ? new Color(Blue.r, Blue.g, Blue.b, 0.085f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.018f);
            var rightLowColor = rightLowActive
                ? new Color(Blue.r, Blue.g, Blue.b, 0.085f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.018f);

            if (leftLowActive)
            {
                DrawGuiRect(new Rect(0f, returnTop, Screen.width * 0.5f, lowHeight), leftLowColor);
            }

            if (rightLowActive)
            {
                DrawGuiRect(new Rect(Screen.width * 0.5f, returnTop, Screen.width * 0.5f, lowHeight), rightLowColor);
            }

            DrawGuiRect(new Rect(Screen.width * 0.5f - 1f * scale, splitTop, 2f * scale, Screen.height - splitTop), new Color(Ink.r, Ink.g, Ink.b, 0.024f));
            DrawGuiRect(new Rect(0f, returnTop, Screen.width, 2f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.026f));

            var suggestedSide = steps % 2 == 0 ? WalkingFootSide.Left : WalkingFootSide.Right;
            DrawTouchGlyph(leftFoot, new Rect(0f, splitTop, Screen.width * 0.5f, returnTop - splitTop), true, showLabels && suggestedSide == WalkingFootSide.Left && !leftFoot.NeedsReturn, scale);
            DrawTouchGlyph(rightFoot, new Rect(Screen.width * 0.5f, splitTop, Screen.width * 0.5f, returnTop - splitTop), true, showLabels && suggestedSide == WalkingFootSide.Right && !rightFoot.NeedsReturn, scale);
            DrawTouchGlyph(leftFoot, new Rect(0f, returnTop, Screen.width * 0.5f, lowHeight), false, showLabels && leftLowActive, scale);
            DrawTouchGlyph(rightFoot, new Rect(Screen.width * 0.5f, returnTop, Screen.width * 0.5f, lowHeight), false, showLabels && rightLowActive, scale);
        }

        private void DrawTouchGlyph(FootRuntime foot, Rect zone, bool stepZone, bool suggested, float scale)
        {
            var phase = Pulse01(Time.unscaledTime * 1.55f + (foot.Side == WalkingFootSide.Left ? 0f : 0.5f));
            var invalid = foot.Mode == InputMode.InvalidHold || (foot.Mode == InputMode.Placement && !foot.Candidate.IsValid);
            var active = stepZone
                ? foot.Mode == InputMode.Placement || (!foot.NeedsReturn && suggested)
                : foot.Mode == InputMode.Return || foot.NeedsReturn || suggested;
            var color = invalid
                ? Red
                : stepZone
                    ? Warm
                    : Blue;
            var alpha = active ? Mathf.Lerp(0.28f, 0.50f, phase) : 0.08f;
            var size = (active ? Mathf.Lerp(30f, 42f, phase) : 24f) * scale;
            var xJitter = invalid ? Mathf.Sin(Time.unscaledTime * 42f) * 5f * scale * Mathf.Clamp01(foot.StatusPulse + 0.25f) : 0f;
            var center = stepZone
                ? new Vector2(zone.center.x + xJitter, Mathf.Lerp(zone.yMin + 76f * scale, zone.yMax - 66f * scale, 0.48f))
                : new Vector2(zone.center.x + xJitter, zone.center.y);
            var rect = CenteredRect(center, size, size);

            if (stepZone)
            {
                DrawRing(Inflate(rect, 7f * scale), new Color(color.r, color.g, color.b, alpha));
                DrawFootprint(center, foot.Side, scale * 0.82f, new Color(color.r, color.g, color.b, alpha * 0.85f));
                if (invalid)
                {
                    DrawGuiRect(new Rect(rect.xMin - 14f * scale, rect.center.y - 3f * scale, rect.width + 28f * scale, 6f * scale), new Color(Red.r, Red.g, Red.b, 0.42f));
                }

                return;
            }

            DrawReturnPocket(center, 116f * scale, 32f * scale, 6f * scale, new Color(color.r, color.g, color.b, alpha));
            DrawThumb(CenteredRect(new Vector2(center.x, center.y - 34f * scale), size * 0.58f, size * 0.68f), new Color(color.r, color.g, color.b, alpha * 0.45f));
        }

        private void DrawReadyCoach(float scale)
        {
            var goalPanel = new Rect(Screen.width * 0.16f, Screen.height * 0.15f, Screen.width * 0.68f, 150f * scale);
            DrawGuiRect(goalPanel, new Color(1f, 0.99f, 0.96f, 0.18f));
            GUI.Label(new Rect(goalPanel.x, goalPanel.y + 10f * scale, goalPanel.width, 42f * scale), "GO FAR IN 30s", smallHudStyle);
            DrawReadyGoalLine(goalPanel, scale);
            DrawReadyPenguinDemo(new Rect(Screen.width * 0.30f, Screen.height * 0.39f, Screen.width * 0.40f, 120f * scale), scale);

            var panel = new Rect(Screen.width * 0.24f, Screen.height * 0.70f, Screen.width * 0.52f, 116f * scale);
            DrawGuiRect(panel, new Color(1f, 0.99f, 0.96f, 0.22f));

            var gap = 16f * scale;
            var laneTop = panel.y + 10f * scale;
            var laneHeight = panel.height - 20f * scale;
            var laneWidth = (panel.width - gap * 3f) * 0.5f;
            var leftLane = new Rect(panel.x + gap, laneTop, laneWidth, laneHeight);
            var rightLane = new Rect(panel.x + gap * 2f + laneWidth, laneTop, laneWidth, laneHeight);
            DrawThumbLoop(leftLane, leftFoot.Side, 0f, scale);
            DrawThumbLoop(rightLane, rightFoot.Side, 0.5f, scale);
        }

        private void DrawReadyPenguinDemo(Rect rect, float scale)
        {
            var loop = Mathf.Repeat(Time.unscaledTime * 0.72f, 1f);
            var side = loop < 0.5f ? -1f : 1f;
            var pulse = Pulse01(loop * 2f);
            var center = new Vector2(rect.center.x + side * Mathf.Lerp(2f, 12f, pulse) * scale, rect.center.y);
            DrawCircle(CenteredRect(center + new Vector2(0f, 16f * scale), 42f * scale, 34f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.72f));
            DrawCircle(CenteredRect(center + new Vector2(0f, -8f * scale), 54f * scale, 64f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.74f));
            DrawCircle(CenteredRect(center + new Vector2(0f, -10f * scale), 24f * scale, 28f * scale), new Color(Paper.r, Paper.g, Paper.b, 0.80f));
            DrawCircle(CenteredRect(center + new Vector2(-13f * scale, -43f * scale), 24f * scale, 10f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.78f));
            DrawCircle(CenteredRect(center + new Vector2(13f * scale, -43f * scale), 24f * scale, 10f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.78f));
            DrawRing(CenteredRect(center + new Vector2(side * 34f * scale, 6f * scale), 18f * scale, 18f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.36f + pulse * 0.24f));
            DrawGuiRect(new Rect(rect.x + rect.width * 0.18f, rect.yMax - 2f * scale, rect.width * 0.64f, 3f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.16f));
        }

        private void DrawReadyGoalLine(Rect panel, float scale)
        {
            var y = panel.y + panel.height * 0.66f;
            var start = panel.x + panel.width * 0.22f;
            var end = panel.x + panel.width * 0.78f;
            DrawGuiRect(new Rect(start, y, end - start, 5f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.22f));
            DrawCircle(CenteredRect(new Vector2(start, y + 2f * scale), 22f * scale, 22f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.60f));
            DrawGuiRect(new Rect(end, y - 34f * scale, 5f * scale, 40f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.42f));
            DrawGuiRect(new Rect(end + 5f * scale, y - 34f * scale, 38f * scale, 20f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.62f));
            if (bestDistanceAtRunStart > 1f)
            {
                var t = Mathf.Clamp01(bestDistanceAtRunStart / Mathf.Max(1f, GoalMarkerDistances[GoalMarkerDistances.Length - 1]));
                var x = Mathf.Lerp(start, end, t);
                DrawRing(CenteredRect(new Vector2(x, y + 2f * scale), 27f * scale, 27f * scale), new Color(Blue.r, Blue.g, Blue.b, 0.46f));
            }
        }

        private void DrawThumbLoop(Rect lane, WalkingFootSide side, float phaseOffset, float scale)
        {
            var topCenter = new Vector2(lane.center.x, lane.y + 31f * scale);
            var bottomCenter = new Vector2(lane.center.x, lane.yMax - 22f * scale);
            var loop = Mathf.Repeat(Time.unscaledTime * 0.82f + phaseOffset, 1f);
            var stampPulse = loop < 0.28f ? 1f - loop / 0.28f : Mathf.Clamp01(1f - (loop - 0.28f) / 0.52f);
            var pocketPulse = loop > 0.34f && loop < 0.78f ? Pulse01((loop - 0.34f) * 2.2f) : 0.15f;

            DrawRing(CenteredRect(topCenter, (34f + stampPulse * 10f) * scale, (34f + stampPulse * 10f) * scale), new Color(Warm.r, Warm.g, Warm.b, 0.28f + stampPulse * 0.26f));
            DrawFootprint(topCenter, side, scale * (0.58f + stampPulse * 0.08f), new Color(Warm.r, Warm.g, Warm.b, 0.30f + stampPulse * 0.35f));
            DrawReturnPocket(bottomCenter, 72f * scale, 18f * scale, 4f * scale, new Color(Blue.r, Blue.g, Blue.b, 0.24f + pocketPulse * 0.22f));

            if (loop >= 0.10f && loop < 0.74f)
            {
                var t = Mathf.InverseLerp(0.10f, 0.74f, loop);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var center = Vector2.Lerp(topCenter, bottomCenter, eased);
                var fade = 1f - Mathf.SmoothStep(0.58f, 0.74f, loop);
                var pullColor = Color.Lerp(Warm, Blue, Mathf.Clamp01(t));
                DrawThumb(CenteredRect(center, 28f * scale, 34f * scale), new Color(pullColor.r, pullColor.g, pullColor.b, 0.72f * fade));
            }
            else if (loop < 0.10f)
            {
                DrawThumb(CenteredRect(topCenter, 30f * scale, 36f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.78f));
            }
        }

        private void DrawFootSignal(FootRuntime foot, Rect rect, float scale)
        {
            DrawGuiRect(rect, FootStatusColor(foot));
            var center = rect.center;
            var dot = CenteredRect(center, 24f * scale, 24f * scale);
            var lineColor = new Color(Ink.r, Ink.g, Ink.b, 0.42f);
            DrawGuiRect(new Rect(rect.x + 14f * scale, center.y - 2f * scale, rect.width - 28f * scale, 4f * scale), lineColor);

            if (foot.Mode == InputMode.InvalidHold || (foot.Mode == InputMode.Placement && !foot.Candidate.IsValid))
            {
                DrawRing(Inflate(dot, 8f * scale), new Color(Red.r, Red.g, Red.b, 0.9f));
                DrawGuiRect(new Rect(center.x - 18f * scale, center.y - 3f * scale, 36f * scale, 6f * scale), new Color(Red.r, Red.g, Red.b, 0.75f));
                return;
            }

            if (foot.Mode == InputMode.Return || foot.NeedsReturn)
            {
                DrawGuiRect(new Rect(center.x - 24f * scale, center.y + 9f * scale, 48f * scale, 5f * scale), new Color(Blue.r, Blue.g, Blue.b, 0.88f));
                DrawCircle(dot, new Color(Blue.r, Blue.g, Blue.b, 0.42f));
                return;
            }

            DrawRing(Inflate(dot, 8f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.72f));
            DrawCircle(dot, new Color(Warm.r, Warm.g, Warm.b, 0.5f));
        }

        private static Color FootStatusColor(FootRuntime foot)
        {
            if (foot.Mode == InputMode.InvalidHold || foot.Mode == InputMode.Placement)
            {
                return foot.Mode != InputMode.InvalidHold && foot.Candidate.IsValid
                    ? new Color(Green.r, Green.g, Green.b, 0.20f)
                    : new Color(Red.r, Red.g, Red.b, 0.24f);
            }

            if (foot.Mode == InputMode.Return)
            {
                return new Color(Blue.r, Blue.g, Blue.b, 0.24f);
            }

            return foot.NeedsReturn
                ? new Color(Warm.r, Warm.g, Warm.b, 0.22f)
                : new Color(1f, 0.99f, 0.96f, 0.46f);
        }

        private void EnsureGuiStyles()
        {
            if (hudStyle != null)
            {
                return;
            }

            hudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(42f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            smallHudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(36f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(44f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f),
                wordWrap = true,
                normal = { textColor = Ink }
            };
            guideStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            resultMetricStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(52f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            resultDetailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(19f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = FadedInk }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f),
                fontStyle = FontStyle.Bold
            };

            EnsureShapeTextures();
        }

        private static void DrawGuiRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawCircle(Rect rect, Color color)
        {
            EnsureShapeTextures();
            DrawGuiTexture(rect, circleTexture, color);
        }

        private void DrawRing(Rect rect, Color color)
        {
            EnsureShapeTextures();
            DrawGuiTexture(rect, ringTexture, color);
        }

        private void DrawThumb(Rect rect, Color color)
        {
            DrawCircle(rect, color);
            var highlight = new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.12f, rect.width * 0.44f, rect.height * 0.26f);
            DrawCircle(highlight, new Color(1f, 1f, 1f, color.a * 0.22f));
        }

        private void DrawFootprint(Vector2 center, WalkingFootSide side, float scale, Color color)
        {
            var mirror = side == WalkingFootSide.Left ? -1f : 1f;
            DrawCircle(CenteredRect(center + new Vector2(-mirror * 2f * scale, 7f * scale), 18f * scale, 34f * scale), color);
            DrawCircle(CenteredRect(center + new Vector2(mirror * 3f * scale, -12f * scale), 10f * scale, 10f * scale), color);
            DrawCircle(CenteredRect(center + new Vector2(-mirror * 6f * scale, -9f * scale), 8f * scale, 8f * scale), color);
            DrawCircle(CenteredRect(center + new Vector2(mirror * 11f * scale, -5f * scale), 7f * scale, 7f * scale), color);
        }

        private static void DrawReturnPocket(Vector2 center, float width, float height, float stroke, Color color)
        {
            var pocket = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            DrawGuiRect(new Rect(pocket.x, pocket.yMax - stroke, pocket.width, stroke), color);
            DrawGuiRect(new Rect(pocket.x, pocket.y, stroke, pocket.height), color);
            DrawGuiRect(new Rect(pocket.xMax - stroke, pocket.y, stroke, pocket.height), color);
        }

        private static void DrawGuiTexture(Rect rect, Texture2D texture, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        private void EnsureShapeTextures()
        {
            if (circleTexture != null && ringTexture != null)
            {
                return;
            }

            circleTexture = CreateCircleTexture("Thumbwaddle Circle", 64, 0f);
            ringTexture = CreateCircleTexture("Thumbwaddle Ring", 64, 0.64f);
        }

        private static Texture2D CreateCircleTexture(string textureName, int size, float innerRadius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var outerAlpha = Mathf.Clamp01((1f - distance) * 10f);
                    var innerAlpha = innerRadius <= 0f ? 1f : Mathf.Clamp01((distance - innerRadius) * 14f);
                    var alpha = (byte)Mathf.RoundToInt(255f * outerAlpha * innerAlpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Rect CenteredRect(Vector2 center, float width, float height)
        {
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private static Rect Inflate(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static float Pulse01(float time)
        {
            return 0.5f + Mathf.Sin(time * Mathf.PI * 2f) * 0.5f;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (max - min) * (float)random.NextDouble();
        }

        private static void ApplyFootStatus(FootRuntime foot, Text label, Image badge, Image zone)
        {
            var color = new Color32(255, 253, 247, 210);
            var zoneColor = new Color(1f, 1f, 1f, 0.02f);

            if (foot.Mode == InputMode.Placement)
            {
                if (foot.Candidate.IsValid)
                {
                    color = WithAlpha(Green, 226);
                    zoneColor = new Color(Green.r, Green.g, Green.b, 0.09f);
                }
                else
                {
                    color = WithAlpha(Red, 226);
                    zoneColor = new Color(Red.r, Red.g, Red.b, 0.11f);
                }
            }
            else if (foot.Mode == InputMode.Return)
            {
                color = WithAlpha(Blue, 226);
                zoneColor = new Color(Blue.r, Blue.g, Blue.b, 0.10f);
            }
            else if (foot.NeedsReturn)
            {
                color = WithAlpha(Warm, 226);
                zoneColor = new Color(Warm.r, Warm.g, Warm.b, 0.11f);
            }

            label.text = string.Empty;
            badge.color = Color.Lerp(color, Color.white, foot.StatusPulse * 0.12f);
            if (zone != null)
            {
                zone.color = zoneColor;
            }
        }

        private void UpdateDebugMarkers(bool snap = false)
        {
            if (debugRoot == null)
            {
                return;
            }

            debugRoot.gameObject.SetActive(debugFootMarkers);
            if (!debugFootMarkers)
            {
                return;
            }

            SetMarker(leftMarker, leftFootPosition, snap);
            SetMarker(rightMarker, rightFootPosition, snap);
        }

        private static void SetMarker(Transform marker, Vector2 position, bool snap)
        {
            if (marker == null)
            {
                return;
            }

            var target = new Vector3(position.x, 0.04f, position.y);
            marker.position = snap ? target : Vector3.Lerp(marker.position, target, 18f * Time.deltaTime);
        }

        private void PlayStep()
        {
            if (audioSource != null && stepClip != null)
            {
                audioSource.PlayOneShot(stepClip, 0.34f);
            }
        }

        private void PlayBump(float volume)
        {
            if (audioSource != null && bumpClip != null)
            {
                audioSource.PlayOneShot(bumpClip, Mathf.Clamp01(volume) * 0.45f);
            }
        }

        private void PlayReward(bool best)
        {
            var clip = best ? bestClip : rewardClip;
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, best ? 0.42f : 0.30f);
            }

#if !UNITY_WEBGL && !UNITY_EDITOR
            if (best)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - t / duration);
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * envelope;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var cube = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshFilter>().sharedMesh = sharedCubeMesh ?? (sharedCubeMesh = CreateUnitCubeMesh());
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return cube;
        }

        private static SpriteRenderer CreateWorldSprite(string objectName, Sprite sprite, Transform parent, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static GameObject CreateTexturedGround(
            string objectName,
            Vector3 position,
            float width,
            float length,
            Texture texture,
            Transform parent)
        {
            var ground = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            ground.transform.SetParent(parent, false);
            ground.transform.position = position;
            ground.GetComponent<MeshFilter>().sharedMesh = CreateGroundQuadMesh(width, length);
            var renderer = ground.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateTexturedMaterial(objectName + " Material", texture);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ground;
        }

        private static Material CreateTexturedMaterial(string materialName, Texture texture)
        {
            var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = materialName,
                mainTexture = texture,
                color = Color.white
            };
            return material;
        }

        private static Mesh CreateGroundQuadMesh(float width, float length)
        {
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;
            var mesh = new Mesh
            {
                name = "Thumbwaddle Textured Ground",
                vertices = new[]
                {
                    new Vector3(-halfWidth, 0f, -halfLength),
                    new Vector3(halfWidth, 0f, -halfLength),
                    new Vector3(halfWidth, 0f, halfLength),
                    new Vector3(-halfWidth, 0f, halfLength),
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateEllipsoid(string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var ellipsoid = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            ellipsoid.name = objectName;
            ellipsoid.transform.SetParent(parent, false);
            ellipsoid.transform.position = position;
            ellipsoid.transform.localScale = scale;
            ellipsoid.GetComponent<MeshFilter>().sharedMesh = sharedSphereMesh ?? (sharedSphereMesh = CreateUnitSphereMesh());
            var renderer = ellipsoid.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ellipsoid;
        }

        private static Mesh CreateUnitCubeMesh()
        {
            var mesh = new Mesh
            {
                name = "Walking Unit Cube"
            };

            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
            };

            mesh.vertices = vertices;
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                1, 2, 6, 1, 6, 5,
                5, 6, 7, 5, 7, 4,
                4, 7, 3, 4, 3, 0,
                3, 7, 6, 3, 6, 2,
                4, 0, 1, 4, 1, 5
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateUnitSphereMesh()
        {
            const int longitudeSegments = 18;
            const int latitudeSegments = 10;
            var vertices = new Vector3[(latitudeSegments + 1) * (longitudeSegments + 1)];
            var triangles = new int[latitudeSegments * longitudeSegments * 6];

            for (var lat = 0; lat <= latitudeSegments; lat++)
            {
                var v = lat / (float)latitudeSegments;
                var theta = v * Mathf.PI;
                var sinTheta = Mathf.Sin(theta);
                var cosTheta = Mathf.Cos(theta);

                for (var lon = 0; lon <= longitudeSegments; lon++)
                {
                    var u = lon / (float)longitudeSegments;
                    var phi = u * Mathf.PI * 2f;
                    var index = lat * (longitudeSegments + 1) + lon;
                    vertices[index] = new Vector3(
                        Mathf.Cos(phi) * sinTheta * 0.5f,
                        cosTheta * 0.5f,
                        Mathf.Sin(phi) * sinTheta * 0.5f);
                }
            }

            var tri = 0;
            for (var lat = 0; lat < latitudeSegments; lat++)
            {
                for (var lon = 0; lon < longitudeSegments; lon++)
                {
                    var current = lat * (longitudeSegments + 1) + lon;
                    var next = current + longitudeSegments + 1;
                    triangles[tri++] = current;
                    triangles[tri++] = next;
                    triangles[tri++] = current + 1;
                    triangles[tri++] = current + 1;
                    triangles[tri++] = next;
                    triangles[tri++] = next + 1;
                }
            }

            var mesh = new Mesh
            {
                name = "Thumbwaddle Rounded Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader =
                Shader.Find("Unlit/Color") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("UI/Default") ??
                Shader.Find("Standard") ??
                Shader.Find("Hidden/Internal-Colored");
            var material = new Material(shader);
            material.name = materialName;
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

            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static Color WithAlpha(Color color, byte alpha)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(color.r * 255f),
                (byte)Mathf.RoundToInt(color.g * 255f),
                (byte)Mathf.RoundToInt(color.b * 255f),
                alpha);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchored, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.one);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
