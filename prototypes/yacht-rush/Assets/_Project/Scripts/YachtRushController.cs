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

        private const string BestScoreKey = "mannlab.yacht_rush.best_score";
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
        private const string GameKey = "yacht-rush";
        private const string GameOverInterstitialIosAdUnitId = "ca-app-pub-4525914685149405/8278784535";
        private const string GameOverInterstitialAndroidAdUnitId = "";
        private const int GameOverInterstitialInterval = 1;

        private static readonly Vector3 BowlHome = new Vector3(-2.75f, 0.54f, -1.25f);
        private static readonly Vector3 BowlDockLandscape = new Vector3(-8.18f, 0.54f, -0.95f);
        private static readonly Vector3 BowlDockPortrait = new Vector3(-3.85f, 0.54f, -5.34f);
        private static readonly Quaternion CameraRotation = Quaternion.Euler(68f, 0f, 0f);

        private readonly List<DieView> dice = new List<DieView>();
        private readonly Dictionary<YachtRushCategory, ScoreRecord> scores =
            new Dictionary<YachtRushCategory, ScoreRecord>();
        private readonly Dictionary<YachtRushCategory, ScoreButtonView> scoreButtons =
            new Dictionary<YachtRushCategory, ScoreButtonView>();
        private readonly System.Random random = new System.Random(Environment.TickCount);

        private Camera mainCamera;
        private Camera backgroundCamera;
        private Transform bowlRoot;
        private Transform bowlRim;
        private Transform bowlGripHalo;
        private Renderer bowlGripHaloRenderer;
        private Transform tableRoot;
        private PhysicsMaterial dicePhysicsMaterial;
        private PhysicsMaterial tablePhysicsMaterial;
        private Material tableMaterial;
        private AudioSource audioSource;
        private readonly Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        private readonly List<Renderer> twistAccentRenderers = new List<Renderer>();
        private Canvas canvas;
        private Text roundText;
        private Text rollText;
        private Text holdText;
        private Text totalText;
        private Text bestText;
        private Image contractBackground;
        private Text contractNameText;
        private Text contractConditionText;
        private Text contractBonusText;
        private Text contractStateText;
        private Image twistAccentBar;
        private Text chooserTitleText;
        private Text rushIntroText;
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
        private float nextShakeSoundTime;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private int RoundNumber => Mathf.Min(scores.Count + 1, YachtRushRules.RoundCount);
        private int HeldCount => dice.Count(die => die.IsHeld);
        private int MaxRollsThisRound => YachtRushRules.MaxRollsForRule(currentRollRule);
        private bool CanThrow => !isResolvingRoll &&
            scores.Count < YachtRushRules.RoundCount &&
            YachtRushRules.CanThrowWithRule(currentRollRule, rollCount, HeldCount);
        private bool CanScore => !isResolvingRoll && rollCount > 0 && scores.Count < YachtRushRules.RoundCount;

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
                Debug.LogWarning($"[Yacht Rush] Firebase initialization skipped: {exception.GetType().Name}");
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
                Debug.LogWarning($"[Yacht Rush] AdMob initialization skipped: {exception.GetType().Name}");
            }
        }

        private void Update()
        {
            UpdateCameraForScreen();
            UpdateBowlInput();
            UpdateBowlFeedback();
            UpdateHeldDiceVisuals();
            UpdateRushIntroCue();

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
            PlaceUnlockedDiceInBowl();
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
            ChooseRoundModifiers();
            ParkBowl();

            foreach (var die in dice)
            {
                die.IsHeld = false;
                die.SetRushDie(YachtRushRushDie.None, false);
                die.SetValue(random.Next(1, 7));
            }

            ApplyRushDieVisuals();
            PlaceUnlockedDiceInBowl();
            ShowRushIntroCue();
            FirebaseTelemetry.SetContext("round", RoundNumber.ToString());
            FirebaseTelemetry.LogEvent(
                "round_start",
                new Dictionary<string, string>
                {
                    { "round", RoundNumber.ToString() },
                    { "contract", currentContract.ToString() },
                    { "roll_rule", currentRollRule.ToString() },
                    { "rush_die", currentRushDie.ToString() },
                    { "score", scores.Values.Sum(score => score.Total).ToString() }
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

        private void BuildUi()
        {
            var canvasObject = new GameObject("Yacht Rush UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            var topStrip = CreatePanel(root, "Run Stats Strip", Anchor.TopStretch, new Vector2(40f, -116f), new Vector2(-40f, -22f), new Color32(255, 253, 246, 238));
            var statPanel = CreatePanel(topStrip, "Stats", Anchor.Stretch, new Vector2(18f, 12f), new Vector2(-18f, -12f), new Color32(255, 250, 236, 244));
            var statLayout = statPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            statLayout.padding = new RectOffset(10, 10, 8, 8);
            statLayout.spacing = 10f;
            statLayout.childAlignment = TextAnchor.MiddleCenter;
            statLayout.childControlWidth = true;
            statLayout.childControlHeight = true;
            statLayout.childForceExpandWidth = true;
            statLayout.childForceExpandHeight = true;
            roundText = CreateStatText(statPanel, "Round");
            rollText = CreateStatText(statPanel, "Throw");
            holdText = CreateStatText(statPanel, "Hold");
            totalText = CreateStatText(statPanel, "Score");
            bestText = CreateStatText(statPanel, "Best");

            var chooser = CreatePanel(root, "Smart Score Chooser", Anchor.BottomStretch, new Vector2(24f, 22f), new Vector2(-24f, 448f), new Color32(255, 253, 246, 244));
            scoreChooserRect = chooser;

            var twistPanel = CreatePanel(chooser, "Round Twist Banner", Anchor.TopStretch, new Vector2(18f, -132f), new Vector2(-18f, -20f), new Color32(255, 244, 205, 250));
            contractBackground = twistPanel.GetComponent<Image>();
            twistAccentBar = CreateImage(twistPanel, "Twist Accent Bar", Anchor.StretchLeft, new Vector2(0f, 0f), new Vector2(12f, 0f), new Color32(187, 126, 70, 255));
            contractStateText = CreateText(twistPanel, "Twist Type", "ROLL RULE", 10, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopLeft, new Vector2(18f, -25f), new Vector2(220f, -6f));
            contractNameText = CreateText(twistPanel, "Twist Name", "One Shot", 26, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(18f, -65f), new Vector2(-270f, -24f));
            contractNameText.alignment = TextAnchor.MiddleLeft;
            contractConditionText = CreateText(twistPanel, "Twist Effect", "Only one throw. No reroll safety", 14, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(18f, 13f), new Vector2(-270f, 39f));
            contractConditionText.alignment = TextAnchor.MiddleLeft;
            contractBonusText = CreateText(twistPanel, "Twist Badge", "1 THROW", 28, FontStyle.Bold, SketchPalette.Ink, Anchor.StretchRight, new Vector2(-250f, 18f), new Vector2(-18f, -18f));

            chooserTitleText = CreateText(chooser, "Turn Prompt", "Shake to throw", 22, FontStyle.Bold, SketchPalette.Ink, Anchor.TopRight, new Vector2(-560f, -176f), new Vector2(-24f, -140f));

            var grid = new GameObject("Score Choice Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(chooser, false);
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(18f, 18f);
            gridRect.offsetMax = new Vector2(-18f, -188f);
            scoreGridLayout = grid.GetComponent<GridLayoutGroup>();
            scoreGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            scoreGridLayout.constraintCount = 3;
            scoreGridLayout.spacing = new Vector2(12f, 12f);
            scoreGridLayout.cellSize = new Vector2(320f, 88f);
            UpdateScoreChooserLayout();

            foreach (var category in YachtRushRules.Categories)
            {
                scoreButtons[category] = CreateScoreButton(grid.transform, category);
            }

            resultPanel = CreatePanel(root, "Result Panel", Anchor.Center, new Vector2(-260f, -150f), new Vector2(260f, 150f), new Color32(255, 253, 246, 248)).gameObject;
            CreateText(resultPanel.transform, "Result Title", "Run Complete", 34, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(18f, -80f), new Vector2(-18f, -24f));
            resultScoreText = CreateText(resultPanel.transform, "Result Score", "0", 70, FontStyle.Bold, SketchPalette.Ink, Anchor.Center, new Vector2(-130f, -42f), new Vector2(130f, 48f));
            resultMetaText = CreateText(resultPanel.transform, "Result Meta", "Best 0", 18, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(24f, 82f), new Vector2(-24f, 116f));
            var againButton = CreateButton(resultPanel.transform, "Play Again", "Play Again", Anchor.BottomStretch, new Vector2(30f, 24f), new Vector2(-30f, 74f));
            againButton.onClick.AddListener(StartRun);

            rushIntroText = CreateText(root, "Rush Die Intro", "STORM DIE!", 46, FontStyle.Bold, SketchPalette.Ink, Anchor.Center, new Vector2(-320f, 110f), new Vector2(320f, 210f));
            rushIntroText.gameObject.SetActive(false);
        }

        private Text CreateStatText(Transform parent, string label)
        {
            var holder = CreatePanel(parent, label, Anchor.Stretch, Vector2.zero, Vector2.zero, new Color32(248, 245, 232, 255));
            var layout = holder.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 112f;
            layout.flexibleWidth = 1f;
            CreateText(holder, $"{label} Label", label, 11, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopStretch, new Vector2(4f, -21f), new Vector2(-4f, -3f));
            return CreateText(holder, $"{label} Value", "0", 26, FontStyle.Bold, SketchPalette.Ink, Anchor.BottomStretch, new Vector2(4f, 3f), new Vector2(-4f, 34f));
        }

        private ScoreButtonView CreateScoreButton(Transform parent, YachtRushCategory category)
        {
            var button = CreateButton(parent, YachtRushRules.CategoryName(category), string.Empty, Anchor.Stretch, Vector2.zero, Vector2.zero);
            var rect = button.GetComponent<RectTransform>();
            var background = button.GetComponent<Image>();

            var nameText = CreateText(rect, "Name", YachtRushRules.CategoryName(category), 18, FontStyle.Bold, SketchPalette.Ink, Anchor.TopStretch, new Vector2(13f, -31f), new Vector2(-96f, -6f));
            nameText.alignment = TextAnchor.MiddleLeft;
            var hintText = CreateText(rect, "Hint", YachtRushRules.CategoryHint(category), 10, FontStyle.Bold, SketchPalette.MutedInk, Anchor.TopStretch, new Vector2(13f, -52f), new Vector2(-96f, -33f));
            hintText.alignment = TextAnchor.MiddleLeft;
            var detailText = CreateText(rect, "Breakdown", "Base -", 10, FontStyle.Bold, SketchPalette.MutedInk, Anchor.BottomStretch, new Vector2(13f, 8f), new Vector2(-96f, 26f));
            detailText.alignment = TextAnchor.MiddleLeft;
            var totalText = CreateText(rect, "Total", "-", 31, FontStyle.Bold, SketchPalette.Ink, Anchor.StretchRight, new Vector2(-82f, 16f), new Vector2(-14f, -16f));
            var tagText = CreateText(rect, "Tag", string.Empty, 10, FontStyle.Bold, SketchPalette.Ink, Anchor.TopRight, new Vector2(-90f, -25f), new Vector2(-14f, -6f));

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

            if (Input.GetMouseButtonUp(0) && !pointerStartedOverUi && !pointerStartedOnBowl && CanScore)
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
                }
            }
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
            var topInset = Mathf.Clamp((116f * uiScale + 18f) / Mathf.Max(1f, Screen.height), 0.074f, 0.15f);
            var bottomInset = Mathf.Clamp((ScoreChooserHeight() * uiScale + 24f) / Mathf.Max(1f, Screen.height), 0.29f, 0.5f);
            var cameraHeight = Mathf.Clamp(1f - topInset - bottomInset, 0.36f, 0.72f);
            mainCamera.rect = new Rect(0f, bottomInset, 1f, cameraHeight);

            var aspect = Mathf.Max(0.46f, Screen.width / (float)Mathf.Max(1, Mathf.RoundToInt(Screen.height * cameraHeight)));
            mainCamera.orthographic = true;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 60f;
            mainCamera.transform.rotation = CameraRotation;
            mainCamera.transform.position = CameraPositionForTarget(CameraFrameCenter(), 8.8f);
            var minimumCameraSize = IsPortraitLayout() ? 5.75f : 6.65f;
            mainCamera.orthographicSize = Mathf.Clamp(
                Mathf.Max(minimumCameraSize, CameraFrameHalfWidth() / aspect, CameraFrameHalfDepthOnScreen()),
                minimumCameraSize,
                13.2f);

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
                ? -TableHalfWidth - 0.55f
                : Mathf.Min(-TableHalfWidth - 0.35f, CurrentBowlDock().x - 2.6f);
        }

        private static float CameraFrameMaxX()
        {
            return TableHalfWidth + 0.35f;
        }

        private static float CameraFrameMinZ()
        {
            return IsPortraitLayout()
                ? Mathf.Min(-TableHalfDepth - 0.58f, CurrentBowlDock().z - 1.18f)
                : -TableHalfDepth - 1.18f;
        }

        private static float CameraFrameMaxZ()
        {
            return TableHalfDepth + 0.48f;
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
            ShowRushResultCue();
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
            scores[category] = new ScoreRecord(score.BaseScore, score.RushAdjustedScore, score.ContractBonus, score.Total);
            PlayAudioCue(score.ContractBonus > 0 ? "bonus" : "score", score.ContractBonus > 0 ? 0.82f : 0.62f);
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
                    { "rush_die", currentRushDie.ToString() }
                });

            if (scores.Count >= YachtRushRules.RoundCount)
            {
                EndRun();
            }
            else
            {
                PrepareNextRound();
            }
        }

        private void EndRun()
        {
            var total = scores.Values.Sum(score => score.Total);
            var isNewBest = total > bestScore;
            if (total > bestScore)
            {
                bestScore = total;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }

            resultScoreText.text = total.ToString();
            if (resultMetaText != null)
            {
                resultMetaText.text = isNewBest ? "New best" : $"Best {bestScore}";
                resultMetaText.color = isNewBest ? new Color32(72, 116, 75, 255) : SketchPalette.MutedInk;
            }

            resultPanel.SetActive(true);
            FirebaseTelemetry.SetContext("score", total.ToString());
            FirebaseTelemetry.SetContext("best_score", bestScore.ToString());
            FirebaseTelemetry.LogEvent(
                "run_end",
                new Dictionary<string, string>
                {
                    { "score", total.ToString() },
                    { "best_score", bestScore.ToString() }
                });
            MannLabAdMob.TryShowGameOverInterstitial();
            UpdateHudAndScores();
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
            bestScore = Mathf.Max(bestScore, total);
            resultScoreText.text = total.ToString();
            if (resultMetaText != null)
            {
                resultMetaText.text = isNewBest ? "New best" : $"Best {bestScore}";
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

        private void UpdateHudAndScores()
        {
            roundText.text = $"{RoundNumber}/12";
            rollText.text = $"{rollCount}/{MaxRollsThisRound}";
            holdText.text = HeldCount.ToString();
            totalText.text = scores.Values.Sum(score => score.Total).ToString();
            bestText.text = bestScore.ToString();

            var contract = YachtRushRules.GetContract(currentContract);
            var rollRule = YachtRushRules.GetRollRule(currentRollRule);
            var rushDie = YachtRushRules.GetRushDie(currentRushDie);
            var values = CurrentDiceValues();
            var contractState = ContractStateLabel(currentContract);
            contractStateText.text = TwistLabel();
            contractNameText.text = TwistName(contract, rollRule, rushDie);
            contractConditionText.text = TwistEffect(values, contract, contractState);
            contractBonusText.text = TwistBadge(contract, rollRule);
            contractStateText.color = SketchPalette.MutedInk;
            contractNameText.color = SketchPalette.Ink;
            contractConditionText.color = currentTwist == RoundTwist.ContractHand && contractState == "READY"
                ? new Color32(62, 110, 65, 255)
                : SketchPalette.MutedInk;
            contractBonusText.color = currentTwist == RoundTwist.ContractHand && contractState == "READY"
                ? new Color32(62, 110, 65, 255)
                : SketchPalette.Ink;
            UpdateTwistVisualTheme(contractState);

            chooserTitleText.text = TurnPrompt();
            chooserTitleText.color = isResolvingRoll
                ? SketchPalette.MutedInk
                : CanScore && rollCount >= MaxRollsThisRound
                    ? new Color32(62, 92, 59, 255)
                    : SketchPalette.Ink;

            var previews = new Dictionary<YachtRushCategory, YachtRushRoundScorePreview>();
            foreach (var category in YachtRushRules.Categories)
            {
                if (!scores.ContainsKey(category) && CanScore)
                {
                    previews[category] = YachtRushRules.PreviewScore(
                        category,
                        currentContract,
                        currentRollRule,
                        currentRushDie,
                        rushDieIndex,
                        values,
                        Mathf.Max(0, rollCount - 1),
                        lockedBeforeFinalThrow,
                        HeldCount);
                }
            }

            var bestPreview = previews.Count == 0 ? -1 : previews.Values.Max(score => score.Total);
            foreach (var category in YachtRushRules.Categories)
            {
                var view = scoreButtons[category];
                view.Button.gameObject.SetActive(true);

                var isUsed = scores.TryGetValue(category, out var record);
                var preview = previews.ContainsKey(category)
                    ? previews[category]
                    : new YachtRushRoundScorePreview(0, 0, 0, 0, false, Array.Empty<int>());
                var total = isUsed ? record.Total : CanScore ? preview.Total : 0;
                var bonus = isUsed ? record.Bonus : CanScore ? preview.ContractBonus : 0;
                var rushChanged = !isUsed && CanScore && preview.BaseScore != preview.RushAdjustedScore;

                view.Button.interactable = CanScore && !isUsed;
                view.DetailText.text = isUsed
                    ? ScoreDetailText(record.BaseScore, record.RushAdjustedScore, record.Bonus)
                    : CanScore
                        ? ScoreDetailText(preview.BaseScore, preview.RushAdjustedScore, preview.ContractBonus)
                        : "Throw dice to preview";
                view.TotalText.text = isUsed || CanScore ? total.ToString() : "-";
                var isBestPreview = !isUsed && CanScore && total == bestPreview;
                view.TagText.text = isUsed
                    ? "USED"
                    : isBestPreview && bonus > 0
                        ? $"BEST +{bonus}"
                        : isBestPreview
                            ? "BEST"
                            : bonus > 0 ? $"CONTRACT +{bonus}" : rushChanged ? RushDeltaTag(preview) : string.Empty;
                view.TagText.color = isUsed
                    ? SketchPalette.MutedInk
                    : isBestPreview
                        ? SketchPalette.Ink
                        : new Color32(62, 110, 65, 255);
                view.NameText.color = isUsed ? SketchPalette.MutedInk : SketchPalette.Ink;
                view.HintText.color = isUsed ? new Color32(112, 111, 101, 255) : SketchPalette.MutedInk;
                view.DetailText.color = isUsed ? new Color32(112, 111, 101, 255) : SketchPalette.MutedInk;
                view.TotalText.color = isUsed ? SketchPalette.MutedInk : SketchPalette.Ink;
                view.Background.color = isUsed
                    ? new Color32(225, 231, 216, 238)
                    : isBestPreview
                        ? new Color32(255, 242, 196, 250)
                        : bonus > 0
                            ? new Color32(241, 249, 235, 248)
                            : rushChanged
                                ? new Color32(235, 241, 250, 248)
                            : new Color32(255, 253, 246, 238);
            }
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
            if (rushIntroText == null || currentRushDie == YachtRushRushDie.None)
            {
                return;
            }

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
                    ShowRushCue($"DIE {dieNumber} LOCKED", 0.95f);
                    break;
                case YachtRushRushDie.Storm:
                    ShowRushCue("STORM ROLL", 0.85f);
                    break;
                case YachtRushRushDie.Cracked:
                    ShowRushCue("COMBOS CRACK", 0.95f);
                    break;
                case YachtRushRushDie.Mirror:
                    ShowRushCue($"{value} FLIPS TO {7 - value}", 1.05f);
                    break;
                case YachtRushRushDie.Blank:
                    ShowRushCue($"DIE {dieNumber} COUNTS 0", 1.05f);
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

        private string TwistLabel()
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return "CONTRACT HAND";
                case RoundTwist.RollRule:
                    return "ROLL RULE";
                case RoundTwist.RushDie:
                    return "RUSH DIE";
                default:
                    return "ROUND TWIST";
            }
        }

        private string TwistName(YachtRushContractInfo contract, YachtRushRollRuleInfo rollRule, YachtRushRushDieInfo rushDie)
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return contract.Name;
                case RoundTwist.RollRule:
                    return rollRule.Name;
                case RoundTwist.RushDie:
                    return rushDie.Name;
                default:
                    return "Base Yacht";
            }
        }

        private string TwistEffect(IReadOnlyList<int> values, YachtRushContractInfo contract, string contractState)
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return CanScore ? $"{contract.Condition} - {contractState}" : contract.Condition;
                case RoundTwist.RollRule:
                    return RollRuleImpactText();
                case RoundTwist.RushDie:
                    return RushDieImpactText(values);
                default:
                    return "Classic Yacht scoring";
            }
        }

        private string TwistBadge(YachtRushContractInfo contract, YachtRushRollRuleInfo rollRule)
        {
            switch (currentTwist)
            {
                case RoundTwist.ContractHand:
                    return $"+{contract.Bonus}";
                case RoundTwist.RollRule:
                    return RollRuleBadgeText(rollRule.Id);
                case RoundTwist.RushDie:
                    return RushDieBadgeText();
                default:
                    return "CLASSIC";
            }
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
                    return $"DIE {dieNumber} LOCK";
                case YachtRushRushDie.Storm:
                    return $"DIE {dieNumber} STORM";
                case YachtRushRushDie.Cracked:
                    return $"DIE {dieNumber} CRACK";
                case YachtRushRushDie.Mirror:
                    return $"DIE {dieNumber} FLIP";
                case YachtRushRushDie.Blank:
                    var values = CurrentDiceValues();
                    var original = values != null && rushDieIndex >= 0 && rushDieIndex < values.Length ? values[rushDieIndex] : 0;
                    if (CanScore && original > 0)
                    {
                        return $"BLANK -{original}";
                    }

                    return $"DIE {dieNumber} = 0";
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
                        ? $"Die {dieNumber}: locked itself"
                        : $"Die {dieNumber}: auto-locks after throw";
                case YachtRushRushDie.Storm:
                    return $"Die {dieNumber}: launches harder";
                case YachtRushRushDie.Cracked:
                    return $"Die {dieNumber}: combo scores ignore it";
                case YachtRushRushDie.Mirror:
                    if (CanScore && original > 0)
                    {
                        var mirrored = 7 - original;
                        return $"Die {dieNumber}: {original} -> {mirrored} ({Signed(mirrored - original)})";
                    }

                    return $"Die {dieNumber}: flips after landing";
                case YachtRushRushDie.Blank:
                    if (CanScore && original > 0)
                    {
                        return $"One rolled {original} is blanked, so score cards subtract {original}";
                    }

                    return $"Die {dieNumber} will score as 0";
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

        private static string ScoreDetailText(int baseScore, int rushScore, int bonus)
        {
            var rushDelta = rushScore - baseScore;
            if (rushDelta != 0 && bonus > 0)
            {
                return $"B {baseScore}  R {Signed(rushDelta)}  T +{bonus}";
            }

            if (rushDelta != 0)
            {
                return $"Base {baseScore}  Rush {Signed(rushDelta)}";
            }

            if (bonus > 0)
            {
                return $"Base {baseScore}  Twist +{bonus}";
            }

            return $"Base {baseScore}";
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
            if (isResolvingRoll)
            {
                return "Settling";
            }

            if (CanScore)
            {
                if (currentRollRule == YachtRushRollRule.NoHolds)
                {
                    return rollCount < MaxRollsThisRound ? "Choose or throw again - no holds" : "Final choice";
                }

                if (currentRollRule == YachtRushRollRule.MustHold2 && rollCount == 1 && HeldCount < 2)
                {
                    return "Hold 2 dice before throw 2";
                }

                if (rollCount < MaxRollsThisRound)
                {
                    return "Choose or throw again";
                }

                return "Final choice";
            }

            switch (currentTwist)
            {
                case RoundTwist.RollRule:
                    return $"{YachtRushRules.GetRollRule(currentRollRule).Name} - shake to throw";
                case RoundTwist.RushDie:
                    return $"{YachtRushRules.GetRushDie(currentRushDie).Name} - shake to throw";
                case RoundTwist.ContractHand:
                    return $"{YachtRushRules.GetContract(currentContract).Name} - shake to throw";
                default:
                    return "Shake to throw";
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
            currentTwist = RoundTwist.RushDie;
            currentRushDie = RushDieForRound(RoundNumber);
            rushDieIndex = random.Next(YachtRushRules.DiceCount);
            ApplyRushDieVisuals();
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
            Center
        }

        private readonly struct ScoreRecord
        {
            public ScoreRecord(int baseScore, int bonus, int total)
                : this(baseScore, baseScore, bonus, total)
            {
            }

            public ScoreRecord(int baseScore, int rushAdjustedScore, int bonus, int total)
            {
                BaseScore = baseScore;
                RushAdjustedScore = rushAdjustedScore;
                Bonus = bonus;
                Total = total;
            }

            public int BaseScore { get; }
            public int RushAdjustedScore { get; }
            public int Bonus { get; }
            public int Total { get; }
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

        private sealed class DieView
        {
            private readonly Material coreMaterial;
            private readonly Material[] facePanelMaterials;
            private readonly Material rushBadgeMaterial;
            private readonly GameObject rushBadge;
            private readonly Material rushHaloMaterial;
            private readonly GameObject rushHalo;
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
        }
    }
}
