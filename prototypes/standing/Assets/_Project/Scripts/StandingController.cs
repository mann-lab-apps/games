using System;
using System.Collections;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Standing
{
    public sealed class StandingController : MonoBehaviour
    {
        private const string BestSecondsKey = "mannlab.standing.best_seconds";
        private static readonly Color DeskTopColor = new Color32(194, 132, 72, 255);
        private static readonly Color DeskFrontColor = new Color32(239, 188, 107, 255);
        private static readonly Color DeskLegColor = new Color32(122, 78, 49, 255);
        private static readonly Color MonitorColor = new Color32(107, 194, 215, 255);
        private static readonly Color PlayerShirtColor = new Color32(54, 143, 205, 255);
        private static readonly Color PlayerPantsColor = new Color32(74, 83, 101, 255);
        private static readonly Color SkinColor = new Color32(248, 189, 132, 255);
        private static readonly Color VisitorColor = new Color32(196, 79, 117, 255);
        private static readonly Color HealthGoodColor = new Color32(47, 183, 97, 255);
        private static readonly Color HealthWarnColor = new Color32(238, 181, 56, 255);
        private static readonly Color HealthLowColor = new Color32(238, 96, 56, 255);
        private static readonly Color[] PasserTints =
        {
            Color.white,
            new Color32(220, 242, 255, 255),
            new Color32(255, 230, 219, 255),
            new Color32(226, 246, 224, 255),
            new Color32(238, 228, 255, 255)
        };

        private const float PasserStartX = -0.28f;
        private const float PasserTravelX = 1.32f;
        private const float PasserWidth = 0.24f;
        private const float DetectionCarpetLeftX = 0.02f;
        private const float DetectionCarpetRightX = 0.98f;
        private const float DetectionCarpetBottomY = 0.60f;
        private const float DetectionCarpetTopY = 0.78f;
        private const int EmployeeStandingPose = 0;
        private const int EmployeeSittingPose = 1;
        private const int EmployeeCaughtPose = 2;
        private const int EmployeeExhaustedPose = 3;

        private readonly System.Random random = new System.Random(Environment.TickCount);
        private AudioSource audioSource;
        private AudioClip sitClip;
        private AudioClip caughtClip;
        private AudioClip exhaustedClip;
        private Text timeText;
        private Text bestText;
        private Image healthFill;
        private RectTransform healthFillRect;
        private RectTransform characterRoot;
        private RectTransform characterBody;
        private RectTransform characterHead;
        private Image employeeArt;
        private RectTransform visitorRoot;
        private Image visitorImage;
        private Image passerArt;
        private Sprite[] employeePoseSprites;
        private Sprite[] customerWalkFrames;
        private Sprite[] phonePasserFrames;
        private Sprite customerFallbackSprite;
        private Sprite phonePasserFallbackSprite;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultScoreText;
        private Texture2D customerTexture;
        private Texture2D employeePoseTexture;
        private Texture2D customerWalkTexture;
        private Texture2D phonePasserTexture;
        private Texture2D deskTexture;
        private Texture2D carpetTexture;
        private Texture2D lobbyTexture;
        private bool usingGeneratedLobby;
        private StandingGameState state;
        private VisitorPhase visitorPhase;
        private float health;
        private float runSeconds;
        private float bestSeconds;
        private int clearedCustomers;
        private float nextVisitorAt;
        private float passerStartedAt;
        private float currentPasserProgress;
        private float currentPasserWalkSpeed;
        private float resultAt;
        private float visualPulse;
        private int currentPasserFrameOffset;
        private int currentPasserDirection;
        private Color currentPasserTint;
        private bool wasSitting;
        private bool currentPasserIsCustomer;
        private StandingGameState lastEndState;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            sitClip = CreateToneClip("Sit", 420f, 0.045f, 0.25f);
            caughtClip = CreateToneClip("Caught", 920f, 0.12f, 0.42f);
            exhaustedClip = CreateToneClip("Exhausted", 150f, 0.20f, 0.45f);
            bestSeconds = PlayerPrefs.GetFloat(BestSecondsKey, 0f);

            LoadArtAssets();
            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (state == StandingGameState.GameOver)
            {
                return;
            }

            if (state == StandingGameState.Caught || state == StandingGameState.Exhausted)
            {
                AnimateFailState();
                if (Time.time >= resultAt)
                {
                    ShowResult();
                }

                return;
            }

            var sittingInput = IsPressingPlayArea();
            state = sittingInput ? StandingGameState.Sitting : StandingGameState.Standing;
            if (sittingInput && !wasSitting)
            {
                PlayClip(sitClip);
            }

            wasSitting = sittingInput;
            runSeconds += Time.deltaTime;
            health = StandingBalance.TickHealth(health, sittingInput, Time.deltaTime);
            UpdateVisitor();

            if (StandingBalance.ShouldCatch(sittingInput, visitorPhase, currentPasserIsCustomer))
            {
                EndRun(StandingGameState.Caught);
                return;
            }

            if (StandingBalance.IsExhausted(health))
            {
                EndRun(StandingGameState.Exhausted);
                return;
            }

            UpdateHud();
            UpdateCharacterVisual(sittingInput);
        }

        private void StartRun()
        {
            StopAllCoroutines();
            state = StandingGameState.Standing;
            visitorPhase = VisitorPhase.Empty;
            health = StandingBalance.MaxHealth;
            runSeconds = 0f;
            clearedCustomers = 0;
            currentPasserIsCustomer = false;
            wasSitting = false;
            visualPulse = 0f;
            currentPasserProgress = 0f;
            currentPasserWalkSpeed = StandingBalance.MinVisitorWalkSpeed;
            currentPasserFrameOffset = 0;
            currentPasserDirection = 1;
            currentPasserTint = Color.white;
            nextVisitorAt = Time.time + StandingBalance.NextVisitorGap(random);
            resultPanel.SetActive(false);
            visitorRoot.gameObject.SetActive(false);
            UpdateHud();
            UpdateCharacterVisual(false);
        }

        private void LoadArtAssets()
        {
            employeePoseTexture = LoadFirstTexture(
                "Standing/employee_poses_v2",
                "Standing/employee_poses_clean",
                "Standing/employee_poses_sheet");
            customerWalkTexture = LoadFirstTexture(
                "Standing/customer_walk_sheet_v3",
                "Standing/customer_male_walk_sheet",
                "Standing/customer");
            employeePoseSprites = CreateGridFrames(employeePoseTexture, 2, 2);
            phonePasserTexture = LoadFirstTexture(
                "Standing/phone_passer_female_walk_sheet",
                "Standing/phone_passer_walk_sheet_v2",
                "Standing/phone_passer_walk_sheet_v1");
            customerTexture = customerWalkTexture ?? Resources.Load<Texture2D>("Standing/customer");
            customerWalkFrames = CreateWalkFrames(customerWalkTexture);
            phonePasserFrames = CreateWalkFrames(phonePasserTexture);
            customerFallbackSprite = CreateFullSprite(customerTexture);
            phonePasserFallbackSprite = CreateFullSprite(phonePasserTexture);
            deskTexture = LoadFirstTexture("Standing/service_desk_doodle", "Standing/desk");
            carpetTexture = Resources.Load<Texture2D>("Standing/detection_carpet_runner");

            var generatedLobby = LoadFirstTexture(
                "Standing/lobby_background_doodle_v2",
                "Standing/lobby_background_doodle_v1");
            lobbyTexture = generatedLobby ?? Resources.Load<Texture2D>("Standing/lobby");
            usingGeneratedLobby = generatedLobby != null;
        }

        private void UpdateVisitor()
        {
            if (visitorPhase == VisitorPhase.Empty)
            {
                if (Time.time < nextVisitorAt)
                {
                    return;
                }

                visitorPhase = VisitorPhase.Warning;
                currentPasserIsCustomer = random.NextDouble() < StandingBalance.CustomerChance;
                currentPasserWalkSpeed = StandingBalance.NextVisitorWalkSpeed(random);
                currentPasserProgress = 0f;
                currentPasserFrameOffset = random.Next(0, 6);
                currentPasserDirection = random.Next(0, 2) == 0 ? 1 : -1;
                currentPasserTint = PasserTints[random.Next(PasserTints.Length)];
                passerStartedAt = Time.time;
                UpdatePasserArt();
                PositionPasser(0f);
                visitorRoot.gameObject.SetActive(true);
                return;
            }

            if (visitorPhase == VisitorPhase.Warning)
            {
                currentPasserProgress = Mathf.Clamp01(currentPasserProgress + currentPasserWalkSpeed * Time.deltaTime);
                PositionPasser(currentPasserProgress);
                UpdatePasserWalkFrame();
                visitorPhase = IsCurrentPasserOnCarpet()
                    ? VisitorPhase.Passing
                    : VisitorPhase.Warning;
                if (currentPasserProgress < 1f)
                {
                    return;
                }
            }

            if (visitorPhase == VisitorPhase.Passing)
            {
                currentPasserProgress = Mathf.Clamp01(currentPasserProgress + currentPasserWalkSpeed * Time.deltaTime);
                PositionPasser(currentPasserProgress);
                UpdatePasserWalkFrame();
                visitorPhase = IsCurrentPasserOnCarpet()
                    ? VisitorPhase.Passing
                    : VisitorPhase.Warning;
            }

            if (currentPasserProgress < 1f)
            {
                return;
            }

            visitorRoot.gameObject.SetActive(false);
            visitorPhase = VisitorPhase.Empty;
            if (currentPasserIsCustomer)
            {
                clearedCustomers++;
            }

            nextVisitorAt = Time.time + StandingBalance.NextVisitorGap(random);
        }

        private void UpdatePasserArt()
        {
            if (passerArt == null)
            {
                return;
            }

            passerArt.sprite = currentPasserIsCustomer || phonePasserFrames == null
                ? customerFallbackSprite
                : phonePasserFallbackSprite;
            passerArt.color = currentPasserTint;
            passerArt.GetComponent<RectTransform>().localScale = new Vector3(currentPasserDirection, 1f, 1f);
            SetPasserFrame(0);
        }

        private void PositionPasser(float progress)
        {
            var travel = Mathf.Clamp01(progress) * PasserTravelX;
            var x = currentPasserDirection > 0
                ? PasserStartX + travel
                : PasserStartX + PasserTravelX - travel;
            visitorRoot.anchorMin = new Vector2(x, 0.48f);
            visitorRoot.anchorMax = new Vector2(x + PasserWidth, 0.82f);
            visitorRoot.offsetMin = Vector2.zero;
            visitorRoot.offsetMax = Vector2.zero;
        }

        private bool IsCurrentPasserOnCarpet()
        {
            var centerX = visitorRoot.anchorMin.x + PasserWidth * 0.5f;
            return StandingBalance.IsVisitorInDetectionZone(centerX);
        }

        private void UpdatePasserWalkFrame()
        {
            var frame = currentPasserFrameOffset + Mathf.FloorToInt((Time.time - passerStartedAt) * 7.5f);
            SetPasserFrame(frame);
        }

        private void EndRun(StandingGameState finalState)
        {
            state = finalState;
            lastEndState = finalState;
            resultAt = Time.time + StandingBalance.ResultDelaySeconds;
            visitorRoot.gameObject.SetActive(finalState == StandingGameState.Caught);
            PlayClip(finalState == StandingGameState.Caught ? caughtClip : exhaustedClip);

            if (runSeconds > bestSeconds)
            {
                bestSeconds = runSeconds;
                PlayerPrefs.SetFloat(BestSecondsKey, bestSeconds);
                PlayerPrefs.Save();
            }

            UpdateHud();
            AnimateFailState();
        }

        private void AnimateFailState()
        {
            visualPulse += Time.deltaTime * 18f;
            if (state == StandingGameState.Caught)
            {
                SetEmployeePose(EmployeeCaughtPose);
                var shake = Mathf.Sin(visualPulse) * 24f;
                characterRoot.anchoredPosition = new Vector2(shake, -80f);
                characterBody.localRotation = Quaternion.Euler(0f, 0f, -8f + Mathf.Sin(visualPulse * 0.8f) * 7f);
                characterHead.localScale = Vector3.one * 1.14f;
                return;
            }

            SetEmployeePose(EmployeeExhaustedPose);
            characterRoot.anchoredPosition = new Vector2(0f, -190f);
            characterBody.localRotation = Quaternion.Euler(0f, 0f, 12f);
            characterHead.localScale = Vector3.one * 0.94f;
        }

        private void ShowResult()
        {
            state = StandingGameState.GameOver;
            resultTitleText.text = lastEndState == StandingGameState.Exhausted ? "Collapsed" : "Customer Saw You";
            resultScoreText.text = $"Time {FormatTime(runSeconds)}\nBest {FormatTime(bestSeconds)}\nCustomers {clearedCustomers}";
            resultPanel.SetActive(true);
        }

        private void UpdateHud()
        {
            timeText.text = $"Time {FormatTime(runSeconds)}";
            bestText.text = $"Best {FormatTime(bestSeconds)}";
            var healthRatio = Mathf.Clamp01(health / StandingBalance.MaxHealth);
            healthFillRect.anchorMin = Vector2.zero;
            healthFillRect.anchorMax = new Vector2(healthRatio, 1f);
            healthFillRect.offsetMin = Vector2.zero;
            healthFillRect.offsetMax = Vector2.zero;
            healthFillRect.localScale = Vector3.one;
            healthFill.color = healthRatio < 0.28f
                ? HealthLowColor
                : healthRatio < 0.58f
                    ? HealthWarnColor
                    : HealthGoodColor;
        }

        private void UpdateCharacterVisual(bool sitting)
        {
            SetEmployeePose(sitting ? EmployeeSittingPose : EmployeeStandingPose);
            var targetY = sitting ? -92f : -28f;
            characterRoot.anchoredPosition = Vector2.Lerp(characterRoot.anchoredPosition, new Vector2(0f, targetY), 0.25f);
            characterBody.localRotation = Quaternion.Lerp(characterBody.localRotation, Quaternion.Euler(0f, 0f, sitting ? -2f : 0f), 0.2f);
            characterHead.localScale = Vector3.Lerp(characterHead.localScale, Vector3.one * (sitting ? 0.98f : 1f), 0.2f);
        }

        private bool IsPressingPlayArea()
        {
            if (resultPanel.activeSelf)
            {
                return false;
            }

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                return touch.phase != TouchPhase.Canceled && touch.phase != TouchPhase.Ended;
            }

            return Input.GetMouseButton(0);
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            var safeAreaRoot = SketchUiFactory.CreateSafeAreaRoot(canvas.transform);
            CreateOfficeScene(safeAreaRoot);
            CreateHeader(safeAreaRoot);
            CreateResultPanel(safeAreaRoot);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Game Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private void CreateBackground(Transform parent)
        {
            if (lobbyTexture != null)
            {
                var lobby = CreateRawImage(parent, "Lobby Art", lobbyTexture, Color.white);
                Stretch(lobby.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
                return;
            }

            var background = new GameObject("Paper Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            background.GetComponent<Image>().color = SketchPalette.Paper;

            var floor = new GameObject("Floor Shadow", typeof(RectTransform), typeof(Image));
            floor.transform.SetParent(parent, false);
            SetAnchor(floor.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.40f);
            floor.GetComponent<Image>().color = WithAlpha(SketchPalette.WarmShadow, 0.48f);
        }

        private void CreateOfficeScene(Transform parent)
        {
            var stage = new GameObject("Office Stage", typeof(RectTransform));
            stage.transform.SetParent(parent, false);
            SetAnchor(stage.GetComponent<RectTransform>(), 0f, 0.10f, 1f, 0.82f, 56f, 0f, -56f, 0f);

            if (!usingGeneratedLobby)
            {
                var wallLine = CreatePanel(stage.transform, "Desk Back Line", SketchPalette.Ink);
                SetAnchor(wallLine.GetComponent<RectTransform>(), 0.06f, 0.67f, 0.94f, 0.678f);

                var window = CreatePanel(stage.transform, "Office Window", WithAlpha(MonitorColor, 0.24f));
                SetAnchor(window.GetComponent<RectTransform>(), 0.08f, 0.71f, 0.30f, 0.91f);
                AddSketchOutline(window.transform);

                var plant = CreatePanel(stage.transform, "Desk Plant", new Color32(106, 159, 92, 255));
                SetAnchor(plant.GetComponent<RectTransform>(), 0.73f, 0.64f, 0.80f, 0.76f);
                AddSketchOutline(plant.transform);
            }

            CreateDetectionCarpet(stage.transform);
            CreateVisitor(stage.transform);

            if (deskTexture != null)
            {
                var deskArt = CreateRawImage(stage.transform, "Desk Art", deskTexture, Color.white);
                SetAnchor(deskArt.GetComponent<RectTransform>(), 0.02f, 0.31f, 0.98f, 0.66f);
            }
            else
            {
                var desk = CreatePanel(stage.transform, "Desk Front", DeskFrontColor);
                SetAnchor(desk.GetComponent<RectTransform>(), 0.15f, 0.44f, 0.85f, 0.58f);
                AddSketchOutline(desk.transform);

                var deskTop = CreatePanel(stage.transform, "Desk Top", DeskTopColor);
                SetAnchor(deskTop.GetComponent<RectTransform>(), 0.11f, 0.57f, 0.89f, 0.63f);
                AddSketchOutline(deskTop.transform);

                var monitor = CreatePanel(stage.transform, "Monitor", MonitorColor);
                SetAnchor(monitor.GetComponent<RectTransform>(), 0.39f, 0.60f, 0.61f, 0.75f);
                AddSketchOutline(monitor.transform);

                var keyboard = CreatePanel(stage.transform, "Keyboard", SketchPalette.WarmHighlight);
                SetAnchor(keyboard.GetComponent<RectTransform>(), 0.38f, 0.49f, 0.62f, 0.53f);
                AddSketchOutline(keyboard.transform);

                var deskLegLeft = CreatePanel(stage.transform, "Desk Leg Left", DeskLegColor);
                SetAnchor(deskLegLeft.GetComponent<RectTransform>(), 0.24f, 0.31f, 0.27f, 0.44f);

                var deskLegRight = CreatePanel(stage.transform, "Desk Leg Right", DeskLegColor);
                SetAnchor(deskLegRight.GetComponent<RectTransform>(), 0.73f, 0.31f, 0.76f, 0.44f);
            }

            CreateCharacter(stage.transform);
        }

        private void CreateDetectionCarpet(Transform parent)
        {
            if (carpetTexture == null)
            {
                return;
            }

            var carpet = CreateRawImage(parent, "Detection Carpet Runner", carpetTexture, Color.white);
            carpet.GetComponent<RawImage>().raycastTarget = false;
            SetAnchor(
                carpet.GetComponent<RectTransform>(),
                DetectionCarpetLeftX,
                DetectionCarpetBottomY,
                DetectionCarpetRightX,
                DetectionCarpetTopY);
        }

        private void CreateCharacter(Transform parent)
        {
            var root = new GameObject("Player Back", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            characterRoot = root.GetComponent<RectTransform>();
            characterRoot.anchorMin = new Vector2(0.5f, 0.23f);
            characterRoot.anchorMax = new Vector2(0.5f, 0.23f);
            characterRoot.pivot = new Vector2(0.5f, 0f);
            characterRoot.sizeDelta = new Vector2(250f, 420f);

            if (employeePoseSprites != null && employeePoseSprites.Length >= 4)
            {
                characterRoot.sizeDelta = new Vector2(315f, 470f);
                employeeArt = CreateSpriteImage(root.transform, "Employee Pose Art", employeePoseSprites[EmployeeStandingPose], Color.white).GetComponent<Image>();
                var artRect = employeeArt.GetComponent<RectTransform>();
                Stretch(artRect, Vector2.zero, Vector2.zero);
                characterBody = artRect;
                characterHead = artRect;
                SetEmployeePose(EmployeeStandingPose);
                return;
            }

            var legs = CreatePanel(root.transform, "Legs", PlayerPantsColor);
            SetAnchor(legs.GetComponent<RectTransform>(), 0.32f, 0.00f, 0.68f, 0.30f);
            AddSketchOutline(legs.transform);

            var body = CreatePanel(root.transform, "Body", PlayerShirtColor);
            characterBody = body.GetComponent<RectTransform>();
            SetAnchor(characterBody, 0.22f, 0.26f, 0.78f, 0.72f);
            AddSketchOutline(body.transform);

            var neck = CreatePanel(root.transform, "Neck", SkinColor);
            SetAnchor(neck.GetComponent<RectTransform>(), 0.44f, 0.68f, 0.56f, 0.79f);
            AddSketchOutline(neck.transform);

            var head = CreatePanel(root.transform, "Head", SkinColor);
            characterHead = head.GetComponent<RectTransform>();
            characterHead.anchorMin = new Vector2(0.5f, 0.78f);
            characterHead.anchorMax = new Vector2(0.5f, 0.78f);
            characterHead.pivot = new Vector2(0.5f, 0.5f);
            characterHead.sizeDelta = new Vector2(110f, 110f);
            AddSketchOutline(head.transform);

            var hair = CreatePanel(root.transform, "Hair", SketchPalette.Ink);
            SetAnchor(hair.GetComponent<RectTransform>(), 0.29f, 0.84f, 0.71f, 0.93f);
        }

        private void CreateVisitor(Transform parent)
        {
            var visitor = new GameObject("Passing Person", typeof(RectTransform), typeof(Image));
            visitor.transform.SetParent(parent, false);
            visitorRoot = visitor.GetComponent<RectTransform>();
            visitorImage = visitor.GetComponent<Image>();
            visitorImage.color = Color.clear;
            visitorImage.raycastTarget = false;

            if (customerFallbackSprite != null)
            {
                passerArt = CreateSpriteImage(visitor.transform, "Passer Art", customerFallbackSprite, Color.white).GetComponent<Image>();
                Stretch(passerArt.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
                visitor.SetActive(false);
                return;
            }

            var head = CreatePanel(visitor.transform, "Customer Head", SkinColor);
            SetAnchor(head.GetComponent<RectTransform>(), 0.32f, 0.67f, 0.68f, 0.98f);
            AddSketchOutline(head.transform);

            var torso = CreatePanel(visitor.transform, "Customer Body", VisitorColor);
            SetAnchor(torso.GetComponent<RectTransform>(), 0.18f, 0.18f, 0.82f, 0.70f);
            AddSketchOutline(torso.transform);

            var bag = CreatePanel(visitor.transform, "Customer Bag", SketchPalette.WarmHighlight);
            SetAnchor(bag.GetComponent<RectTransform>(), 0.00f, 0.30f, 0.24f, 0.58f);
            AddSketchOutline(bag.transform);

            var badge = CreatePanel(visitor.transform, "Customer Badge", SketchPalette.WarmHighlight);
            SetAnchor(badge.GetComponent<RectTransform>(), 0.58f, 0.48f, 0.75f, 0.60f);
            AddSketchOutline(badge.transform);

            var legs = CreatePanel(visitor.transform, "Customer Legs", DeskLegColor);
            SetAnchor(legs.GetComponent<RectTransform>(), 0.30f, 0.00f, 0.70f, 0.24f);
            AddSketchOutline(legs.transform);
            visitor.SetActive(false);
        }

        private void CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            SetAnchor(header.GetComponent<RectTransform>(), 0f, 0.84f, 1f, 1f, 42f, -28f, -42f, -20f);

            timeText = CreateText(header.transform, "Time 0:00", 38, TextAnchor.MiddleLeft);
            SetAnchor(timeText.GetComponent<RectTransform>(), 0f, 0.58f, 0.36f, 1f);

            bestText = CreateText(header.transform, "Best 0", 32, TextAnchor.MiddleRight);
            bestText.color = SketchPalette.MutedInk;
            SetAnchor(bestText.GetComponent<RectTransform>(), 0.64f, 0.58f, 1f, 1f);

            var track = CreatePanel(header.transform, "Health Track", SketchPalette.TilePaper);
            SetAnchor(track.GetComponent<RectTransform>(), 0.26f, 0.10f, 0.74f, 0.52f);
            AddSketchOutline(track.transform);

            var fill = CreatePanel(track.transform, "Health Fill", HealthGoodColor);
            healthFillRect = fill.GetComponent<RectTransform>();
            Stretch(healthFillRect, Vector2.zero, Vector2.zero);
            healthFillRect.pivot = new Vector2(0f, 0.5f);
            healthFill = fill.GetComponent<Image>();
        }

        private void CreateResultPanel(Transform parent)
        {
            resultPanel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(parent, false);
            var rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(680f, 470f);
            rect.anchoredPosition = Vector2.zero;
            resultPanel.GetComponent<Image>().color = new Color(SketchPalette.TilePaper.r, SketchPalette.TilePaper.g, SketchPalette.TilePaper.b, 0.97f);
            AddSketchOutline(resultPanel.transform);

            resultTitleText = CreateText(resultPanel.transform, "Run End", 58, TextAnchor.MiddleCenter);
            SetAnchor(resultTitleText.GetComponent<RectTransform>(), 0f, 0.64f, 1f, 0.92f, 36f, 0f, -36f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "Time 0:00\nBest 0:00", 40, TextAnchor.MiddleCenter);
            SetAnchor(resultScoreText.GetComponent<RectTransform>(), 0f, 0.30f, 1f, 0.64f, 36f, 0f, -36f, 0f);

            var restart = CreateSketchButton(resultPanel.transform, "Again", 36);
            var restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.08f);
            restartRect.anchorMax = new Vector2(0.5f, 0.08f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.sizeDelta = new Vector2(270f, 92f);
            restartRect.anchoredPosition = Vector2.zero;
            restart.onClick.AddListener(StartRun);
            resultPanel.SetActive(false);
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static GameObject CreateRawImage(Transform parent, string name, Texture texture, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            return imageObject;
        }

        private static GameObject CreateSpriteImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return imageObject;
        }

        private static Texture2D LoadFirstTexture(params string[] resourcePaths)
        {
            foreach (var resourcePath in resourcePaths)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        private void SetPasserFrame(int frameIndex)
        {
            if (passerArt == null)
            {
                return;
            }

            var frames = currentPasserIsCustomer ? customerWalkFrames : phonePasserFrames;
            if (frames == null || frames.Length == 0)
            {
                passerArt.sprite = currentPasserIsCustomer ? customerFallbackSprite : phonePasserFallbackSprite;
                return;
            }

            passerArt.sprite = frames[Mathf.Abs(frameIndex) % frames.Length];
        }

        private void SetEmployeePose(int poseIndex)
        {
            if (employeeArt == null || employeePoseSprites == null || employeePoseSprites.Length == 0)
            {
                return;
            }

            employeeArt.sprite = employeePoseSprites[Mathf.Clamp(poseIndex, 0, employeePoseSprites.Length - 1)];
        }

        private static Sprite CreateFullSprite(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Sprite[] CreateWalkFrames(Texture2D texture)
        {
            return CreateGridFrames(texture, 3, 2);
        }

        private static Sprite[] CreateGridFrames(Texture2D texture, int columns, int rows)
        {
            if (texture == null || columns <= 0 || rows <= 0 || texture.width < columns || texture.height < rows)
            {
                return null;
            }

            var frameWidth = texture.width / (float)columns;
            var frameHeight = texture.height / (float)rows;
            var frames = new Sprite[columns * rows];
            for (var frame = 0; frame < frames.Length; frame++)
            {
                var column = frame % columns;
                var rowFromTop = frame / columns;
                var y = (rows - rowFromTop - 1) * frameHeight;
                frames[frame] = Sprite.Create(
                    texture,
                    new Rect(column * frameWidth, y, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            return frames;
        }

        private Button CreateSketchButton(Transform parent, string label, int fontSize)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = SketchPalette.TilePaper;
            AddSketchOutline(buttonObject.transform);

            var button = buttonObject.GetComponent<Button>();
            button.colors = SketchUiFactory.ButtonColors();

            var text = CreateText(buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text CreateText(Transform parent, string value, int size, TextAnchor alignment)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = SketchPalette.Ink;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, size / 2);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void AddSketchOutline(Transform target)
        {
            var outline = target.gameObject.GetComponent<Outline>() ?? target.gameObject.AddComponent<Outline>();
            outline.effectColor = SketchPalette.Ink;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetAnchor(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float left = 0f,
            float bottom = 0f,
            float right = 0f,
            float top = 0f)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static string FormatTime(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private static AudioClip CreateToneClip(string name, float frequency, float durationSeconds, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
