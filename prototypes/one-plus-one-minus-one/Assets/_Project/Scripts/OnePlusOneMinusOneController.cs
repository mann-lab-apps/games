using System;
using System.Collections;
using System.Collections.Generic;
using MannLab.Ads;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.OnePlusOneMinusOne
{
    public sealed class OnePlusOneMinusOneController : MonoBehaviour
    {
        private static readonly Color FailureColor = new Color32(217, 93, 85, 255);
        private static readonly Color SuccessColor = new Color32(97, 166, 106, 255);
        private static readonly Color WhitePaperColor = new Color32(255, 255, 252, 255);
        private static readonly Color SlotLockedColor = new Color32(255, 254, 248, 255);
        private static readonly Color SlotHoverColor = new Color32(255, 246, 198, 255);
        private static readonly Color ButtonPaperColor = new Color32(255, 255, 250, 255);
        private static readonly Color DisabledButtonPaperColor = new Color32(202, 200, 190, 255);
        private static readonly Color DisabledButtonInkColor = new Color32(118, 114, 105, 255);
        private static readonly Color FaceInkColor = new Color32(52, 50, 46, 225);
        private static readonly Color EyeDotColor = new Color32(56, 54, 49, 210);
        private static readonly Color StickHatchColor = new Color32(255, 187, 178, 112);
        private const int MaxSticksPerSlot = 3;
        public const int ReleaseRoundSelectPageSize = 12;
        public const int ReleaseRoundClearInterstitialInterval = 10;
        public const int ReleaseInterstitialGraceRoundCount = 5;
        public const int ReleaseInterstitialMaxFailuresBeforeSkip = 3;
        public const int ReleaseMaxEquationRows = 3;
        public const int ReleaseCompactMaxEquationRows = 4;
        public const float ReleaseMinEquationSlotWidth = 104f;
        public const float ReleaseMinEquationSlotHeight = 118f;
        public const float ReleaseCompactMinEquationSlotWidth = 86f;
        public const float ReleaseCompactMinEquationSlotHeight = 98f;
        public const float ReleaseMinRoundSelectPanelWidth = 260f;
        public const float ReleaseMinRoundSelectPanelHeight = 420f;
        public const float ReleaseMinRoundSelectCellWidth = 88f;
        public const float ReleaseMinRoundSelectCellHeight = 58f;
        public const float ReleaseSfxVolume = 0.24f;
        public const float ReleaseSfxCooldownSeconds = 0.045f;
        public static readonly Vector2 ReleaseNativeReferenceResolution = new Vector2(1080f, 1920f);
        public static readonly Vector2 ReleaseWebGlReferenceResolution = new Vector2(720f, 1280f);
        private const float IdleBeatBpm = 96f;
        private static readonly Vector2 DragGhostPointerOffset = new Vector2(0f, 86f);
        private const string HighestUnlockedRoundKey = "OnePlusOneMinusOne.GoalMode.HighestUnlockedRound";
        private const string GameIdentifier = "one-plus-one-minus-one";
        private const string ReleaseAdMobConfigResourceName = "OnePlusOneMinusOneReleaseAdMob";
        private const string ProductionIosInterstitialAdUnitId = "";
        private const string ProductionAndroidInterstitialAdUnitId = "";
#if MANNLAB_ADMOB_FORCE_TEST_ADS
        private const int RoundClearInterstitialInterval = 1;
        private const int InterstitialGraceRoundCount = 0;
#else
        private const int RoundClearInterstitialInterval = ReleaseRoundClearInterstitialInterval;
        private const int InterstitialGraceRoundCount = ReleaseInterstitialGraceRoundCount;
#endif
        private const int AdMobBridgeInterstitialInterval = 1;
        private const int InterstitialMaxFailuresBeforeSkip = ReleaseInterstitialMaxFailuresBeforeSkip;
        private static readonly string[] TargetSuccessMessages = { "Nice.", "It fits.", "Good shape.", "That works." };
        private static readonly string[] EqualitySuccessMessages = { "Balanced.", "It matches.", "Nice balance.", "Both sides agree." };
#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
        private const string CrashlyticsTestArgument = "--mannlab-force-crashlytics-test";
        private const string CrashlyticsTestEnvironmentVariable = "MANNLAB_FORCE_CRASHLYTICS_TEST";
        private const string QaRoundArgumentPrefix = "--mannlab-qa-round=";
        private const string QaUnlockedArgumentPrefix = "--mannlab-qa-unlocked=";
        private const string QaRoundPageArgumentPrefix = "--mannlab-qa-round-page=";
        private const string QaRoundsArgument = "--mannlab-qa-rounds";
        private const string QaFillSampleArgument = "--mannlab-qa-fill-sample";
        private const int CrashlyticsTestTapCount = 7;
        private const float CrashlyticsTestTapWindowSeconds = 2.5f;
        private const float CrashlyticsTestTapZoneSize = 220f;
#endif

        private enum FaceStyle
        {
            Raw,
            One,
            Number,
            Plus,
            Minus,
            Divide,
            Multiply,
            Star,
            Equals
        }

        private enum MouthShape
        {
            Oval,
            Box,
            Triangle
        }

        private enum SfxCue
        {
            Button,
            Pick,
            Drop,
            Rotate,
            Fail,
            Success,
            Finale
        }

        private readonly struct FaceLayout
        {
            public Vector2 LeftEye { get; }
            public Vector2 RightEye { get; }
            public Vector2 LeftEyeSize { get; }
            public Vector2 RightEyeSize { get; }
            public Vector2 MouthPosition { get; }
            public Vector2 MouthSize { get; }
            public float MouthRotation { get; }

            public FaceLayout(
                Vector2 leftEye,
                Vector2 rightEye,
                Vector2 leftEyeSize,
                Vector2 rightEyeSize,
                Vector2 mouthPosition,
                Vector2 mouthSize,
                float mouthRotation)
            {
                LeftEye = leftEye;
                RightEye = rightEye;
                LeftEyeSize = leftEyeSize;
                RightEyeSize = rightEyeSize;
                MouthPosition = mouthPosition;
                MouthSize = mouthSize;
                MouthRotation = mouthRotation;
            }
        }

        public readonly struct EquationLayoutPlan
        {
            public int Rows { get; }
            public int SlotsPerRow { get; }
            public float SlotWidth { get; }
            public float SlotHeight { get; }
            public float Spacing { get; }
            public float RowGap { get; }
            public float SlotAreaWidth { get; }
            public float TotalWidth { get; }
            public float ContentHeight { get; }
            public float TargetWidth { get; }
            public float TargetGap { get; }

            public EquationLayoutPlan(
                int rows,
                int slotsPerRow,
                float slotWidth,
                float slotHeight,
                float spacing,
                float rowGap,
                float slotAreaWidth,
                float totalWidth,
                float contentHeight,
                float targetWidth,
                float targetGap)
            {
                Rows = rows;
                SlotsPerRow = slotsPerRow;
                SlotWidth = slotWidth;
                SlotHeight = slotHeight;
                Spacing = spacing;
                RowGap = rowGap;
                SlotAreaWidth = slotAreaWidth;
                TotalWidth = totalWidth;
                ContentHeight = contentHeight;
                TargetWidth = targetWidth;
                TargetGap = targetGap;
            }
        }

        public readonly struct RoundSelectLayoutPlan
        {
            public float PanelWidth { get; }
            public float PanelHeight { get; }
            public float InnerPadding { get; }
            public float Spacing { get; }
            public float CellWidth { get; }
            public float CellHeight { get; }
            public float GridHeight { get; }

            public RoundSelectLayoutPlan(
                float panelWidth,
                float panelHeight,
                float innerPadding,
                float spacing,
                float cellWidth,
                float cellHeight,
                float gridHeight)
            {
                PanelWidth = panelWidth;
                PanelHeight = panelHeight;
                InnerPadding = innerPadding;
                Spacing = spacing;
                CellWidth = cellWidth;
                CellHeight = cellHeight;
                GridHeight = gridHeight;
            }
        }

        public readonly struct StartupFlowPlan
        {
            public int HighestUnlockedRoundIndex { get; }
            public int StartRoundIndex { get; }
            public bool ShowRoundSelect { get; }

            public StartupFlowPlan(int highestUnlockedRoundIndex, int startRoundIndex, bool showRoundSelect)
            {
                HighestUnlockedRoundIndex = highestUnlockedRoundIndex;
                StartRoundIndex = startRoundIndex;
                ShowRoundSelect = showRoundSelect;
            }
        }

        public readonly struct InterstitialDecisionPlan
        {
            public bool ShouldOffer { get; }
            public string Reason { get; }

            public InterstitialDecisionPlan(bool shouldOffer, string reason)
            {
                ShouldOffer = shouldOffer;
                Reason = reason;
            }
        }

        private readonly List<SlotView> slotViews = new List<SlotView>();
        private readonly List<FriendFaceView> friendFaces = new List<FriendFaceView>();
        private readonly List<RectTransform> bankStickViews = new List<RectTransform>();
        private readonly List<StickPose> bankStickPoses = new List<StickPose>();
        private readonly List<Button> roundSelectButtons = new List<Button>();
        private readonly List<Text> roundSelectLabels = new List<Text>();
        private readonly Dictionary<SfxCue, AudioClip> sfxClips = new Dictionary<SfxCue, AudioClip>();
        private readonly Dictionary<SfxCue, float> sfxLastPlayedAt = new Dictionary<SfxCue, float>();
        private const int RoundSelectPageSize = ReleaseRoundSelectPageSize;
        private string[] slotSymbols;
        private List<StickPose>[] slotStickPoses;
        private int roundIndex;
        private int highestUnlockedRoundIndex;
        private int roundSelectPage;
        private int currentRoundFailureCount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
        private int crashlyticsTestTapCount;
        private float crashlyticsTestTapDeadline;
        private int qaRoundSelectPageOverride = -1;
        private bool qaFillSampleOnLoad;
#endif
        private bool isAdvancing;
        private bool isShaking;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private RectTransform safeRoot;
        private RectTransform stageRoot;
        private RectTransform equationViewport;
        private RectTransform equationRow;
        private HorizontalLayoutGroup equationRowLayout;
        private RectTransform stickBank;
        private RectTransform dragLayer;
        private RectTransform dragGhost;
        private CanvasGroup draggingSourceGroup;
        private int draggingSourceSlotIndex = -1;
        private int draggingSourceStickIndex = -1;
        private int dragHoverSlotIndex = -1;
        private StickPose draggingSourcePose = StickPose.CenterVertical;
        private AudioSource sfxSource;
        private Text headerTitleText;
        private Text roundText;
        private Text stickText;
        private Text tutorialText;
        private Text feedbackText;
        private Text targetText;
        private Text bankHintText;
        private Button checkButton;
        private Button roundPrevButton;
        private Button roundNextButton;
        private Text roundPageText;
        private RectTransform roundSelectOverlay;
        private RectTransform roundSelectPanel;
        private GridLayoutGroup roundSelectGrid;
        private LayoutElement roundSelectGridLayout;
        private LayoutElement equationAreaLayout;
        private LayoutElement tutorialBubbleLayout;
        private LayoutElement tutorialTextLayout;
        private Font font;
        private Coroutine successRoutine;

        private PuzzleRoundData CurrentRound => OnePlusOneMinusOneRules.GoalModeRounds[roundIndex];
        private bool CurrentRoundUsesFixedTarget => !CurrentRound.SampleSolution.Contains("=");

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                   Resources.GetBuiltinResource<Font>("Arial.ttf");
            var savedHighestUnlockedRoundIndex = PlayerPrefs.GetInt(HighestUnlockedRoundKey, 0);
            var startup = CalculateStartupFlowPlan(savedHighestUnlockedRoundIndex);
            highestUnlockedRoundIndex = startup.HighestUnlockedRoundIndex;
            BuildUi();
            InitializeSfx();
            InitializeTelemetryAndAds();
            var shouldShowRoundSelect = startup.ShowRoundSelect;
            var startRoundIndex = startup.StartRoundIndex;
#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
            ApplyQaStartupOverrides(ref startRoundIndex, ref shouldShowRoundSelect);
#endif
            LoadRound(startRoundIndex);
            if (shouldShowRoundSelect)
            {
                ShowRoundSelect();
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
            if (ShouldForceCrashlyticsTestOnLaunch())
            {
                StartCoroutine(ForceCrashlyticsTestAfterStartup());
            }
#endif
            StartCoroutine(BlinkRoutine());
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
            if (HandleCrashlyticsTestTrigger())
            {
                return;
            }
#endif
            UpdateStageLayout();
            AnimateFriends();
        }

        private void BuildUi()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
#if UNITY_WEBGL
            canvasScaler.referenceResolution = ReleaseWebGlReferenceResolution;
#else
            canvasScaler.referenceResolution = ReleaseNativeReferenceResolution;
#endif
            canvasScaler.matchWidthOrHeight = 0.5f;

            var background = CreateRect("Paper Background", canvas.transform);
            Stretch(background);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = WhitePaperColor;

            safeRoot = SketchUiFactory.CreateSafeAreaRoot(canvas.transform, "Goal Mode Safe Area");

            stageRoot = CreateRect("Portrait Stage", safeRoot);
            stageRoot.anchorMin = new Vector2(0.5f, 0.5f);
            stageRoot.anchorMax = new Vector2(0.5f, 0.5f);
            stageRoot.pivot = new Vector2(0.5f, 0.5f);

            var rootLayout = stageRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(36, 36, 22, 22);
            rootLayout.spacing = 16f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            BuildHeader();
            BuildSpeechBubble();
            BuildEquationArea();
            BuildFeedback();
            BuildStickBank();
            BuildFooter();

            dragLayer = CreateRect("Drag Layer", canvas.transform);
            Stretch(dragLayer);
            BuildRoundSelectOverlay();
            UpdateStageLayout();
        }

        private void BuildHeader()
        {
            var header = CreateRect("Header", stageRoot);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;
            var layout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 2f;

            headerTitleText = CreateText("Title", header, string.Empty, 42, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            headerTitleText.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
        }

        private void UpdateStageLayout()
        {
            if (stageRoot == null || safeRoot == null)
            {
                return;
            }

            UpdateWebGlReferenceResolution();
            var safe = safeRoot.rect;
            var width = Mathf.Min(safe.width - 36f, 1040f);
            var portraitHeightLimit = safe.height > safe.width ? 1660f : 1120f;
            var height = Mathf.Min(safe.height - 24f, portraitHeightLimit);

            if (safe.width / Mathf.Max(1f, safe.height) > 0.72f)
            {
                height = Mathf.Min(safe.height - 24f, 1360f);
                width = Mathf.Min(safe.width - 56f, 1040f);
            }

            stageRoot.sizeDelta = new Vector2(Mathf.Max(560f, width), Mathf.Max(820f, height));
            stageRoot.anchoredPosition = Vector2.zero;
            UpdateRoundSelectLayout(safe);
        }

        private void UpdateWebGlReferenceResolution()
        {
#if UNITY_WEBGL
            if (canvasScaler == null)
            {
                return;
            }

            var useMobilePortraitScale = Screen.height >= Screen.width && Screen.width <= 720;
            var targetResolution = useMobilePortraitScale
                ? ReleaseWebGlReferenceResolution
                : ReleaseNativeReferenceResolution;
            if ((canvasScaler.referenceResolution - targetResolution).sqrMagnitude > 0.1f)
            {
                canvasScaler.referenceResolution = targetResolution;
            }
#endif
        }

        private void BuildSpeechBubble()
        {
            var bubble = CreateRect("Tutorial Bubble", stageRoot);
            tutorialBubbleLayout = bubble.gameObject.AddComponent<LayoutElement>();
            tutorialBubbleLayout.preferredHeight = 122f;
            var layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;

            roundText = CreateText("Round Text", bubble, string.Empty, 20, FontStyle.Bold, SketchPalette.MutedInk, TextAnchor.MiddleCenter);
            roundText.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            tutorialText = CreateText("Tutorial Text", bubble, string.Empty, 24, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            tutorialTextLayout = tutorialText.rectTransform.gameObject.AddComponent<LayoutElement>();
            tutorialTextLayout.preferredHeight = 62f;
        }

        private void BuildEquationArea()
        {
            var holder = CreateRect("Equation Area", stageRoot);
            equationAreaLayout = holder.gameObject.AddComponent<LayoutElement>();
            equationAreaLayout.preferredHeight = 344f;

            equationViewport = CreateRect("Equation Viewport", holder);
            equationViewport.anchorMin = new Vector2(0.5f, 0.48f);
            equationViewport.anchorMax = new Vector2(0.5f, 0.48f);
            equationViewport.pivot = new Vector2(0.5f, 0.5f);
            equationViewport.sizeDelta = new Vector2(580f, 260f);
            equationViewport.gameObject.AddComponent<RectMask2D>();

            var scroll = holder.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = equationViewport;
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.inertia = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;
            scroll.enabled = false;

            equationRow = CreateRect("Equation Row", equationViewport);
            equationRow.anchorMin = new Vector2(0.5f, 0.5f);
            equationRow.anchorMax = new Vector2(0.5f, 0.5f);
            equationRow.pivot = new Vector2(0.5f, 0.5f);
            equationRow.anchoredPosition = Vector2.zero;
            equationRow.sizeDelta = new Vector2(580f, 122f);
            scroll.content = equationRow;

            equationRowLayout = equationRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            equationRowLayout.childAlignment = TextAnchor.MiddleCenter;
            equationRowLayout.childControlWidth = true;
            equationRowLayout.childControlHeight = true;
            equationRowLayout.childForceExpandWidth = false;
            equationRowLayout.childForceExpandHeight = false;
            equationRowLayout.spacing = 12f;

            targetText = CreateText("Fixed Target", holder, string.Empty, 42, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            targetText.rectTransform.anchorMin = new Vector2(0.5f, 0.48f);
            targetText.rectTransform.anchorMax = new Vector2(0.5f, 0.48f);
            targetText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            targetText.rectTransform.sizeDelta = new Vector2(116f, 260f);
        }

        private void BuildFeedback()
        {
            var feedback = CreateRect("Feedback Area", stageRoot);
            feedback.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
            var layout = feedback.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;

            feedbackText = CreateText("Feedback Text", feedback, string.Empty, 24, FontStyle.Bold, SketchPalette.MutedInk, TextAnchor.MiddleCenter);
            feedbackText.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

            stickText = CreateText("Stick Text", feedback, string.Empty, 18, FontStyle.Bold, SketchPalette.MutedInk, TextAnchor.MiddleCenter);
            stickText.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        }

        private void BuildStickBank()
        {
            var trayPanel = CreateRect("Stick Bank Panel", stageRoot);
            trayPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 320f;

            bankHintText = CreateText("Bank Hint", trayPanel, string.Empty, 20, FontStyle.Bold, SketchPalette.MutedInk, TextAnchor.UpperCenter);
            bankHintText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bankHintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            bankHintText.rectTransform.pivot = new Vector2(0.5f, 1f);
            bankHintText.rectTransform.offsetMin = new Vector2(20f, -38f);
            bankHintText.rectTransform.offsetMax = new Vector2(-20f, -8f);
            bankHintText.gameObject.SetActive(false);

            stickBank = CreateRect("Stick Bank", trayPanel);
            Stretch(stickBank, 24, 20, 24, 28);
        }

        private void BuildFooter()
        {
            var footer = CreateRect("Footer", stageRoot);
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var roundsButton = CreateCommandButton("Rounds Button", footer, "Rounds", new Vector2(154f, 54f));
            roundsButton.onClick.AddListener(ShowRoundSelect);

            var resetButton = CreateCommandButton("Reset Button", footer, "Reset", new Vector2(154f, 54f));
            resetButton.onClick.AddListener(ResetRound);

            checkButton = CreateCommandButton("Check Button", footer, "Check", new Vector2(154f, 54f));
            checkButton.onClick.AddListener(CheckCurrent);
        }

        private void BuildRoundSelectOverlay()
        {
            roundSelectOverlay = CreateRect("Round Select Overlay", canvas.transform);
            roundSelectButtons.Clear();
            roundSelectLabels.Clear();
            Stretch(roundSelectOverlay);
            var blocker = roundSelectOverlay.gameObject.AddComponent<Image>();
            blocker.color = WhitePaperColor;

            roundSelectPanel = CreatePanel("Round Select Panel", roundSelectOverlay, WhitePaperColor, SketchPalette.Ink, 901);
            roundSelectPanel.anchorMin = new Vector2(0.5f, 0.5f);
            roundSelectPanel.anchorMax = new Vector2(0.5f, 0.5f);
            roundSelectPanel.pivot = new Vector2(0.5f, 0.5f);
            roundSelectPanel.anchoredPosition = Vector2.zero;
            roundSelectPanel.sizeDelta = new Vector2(520f, 760f);

            var layout = roundSelectPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 28, 28);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateText("Round Select Title", roundSelectPanel, "Rounds", 36, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            title.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

            var gridRoot = CreateRect("Round Grid", roundSelectPanel);
            roundSelectGridLayout = gridRoot.gameObject.AddComponent<LayoutElement>();
            roundSelectGridLayout.preferredHeight = 392f;
            roundSelectGrid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            roundSelectGrid.cellSize = new Vector2(140f, 86f);
            roundSelectGrid.spacing = new Vector2(16f, 16f);
            roundSelectGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            roundSelectGrid.constraintCount = 3;
            roundSelectGrid.childAlignment = TextAnchor.UpperCenter;

            for (var i = 0; i < RoundSelectPageSize; i++)
            {
                var pageSlot = i;
                var button = CreateRoundButton(gridRoot, pageSlot, string.Empty, out var label);
                roundSelectButtons.Add(button);
                roundSelectLabels.Add(label);
                button.onClick.AddListener(() => SelectRound(roundSelectPage * RoundSelectPageSize + pageSlot));
            }

            var pager = CreateRect("Round Pager", roundSelectPanel);
            pager.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
            var pagerLayout = pager.gameObject.AddComponent<HorizontalLayoutGroup>();
            pagerLayout.childAlignment = TextAnchor.MiddleCenter;
            pagerLayout.childControlWidth = true;
            pagerLayout.childControlHeight = true;
            pagerLayout.childForceExpandWidth = false;
            pagerLayout.childForceExpandHeight = false;
            pagerLayout.spacing = 12f;

            roundPrevButton = CreateCommandButton("Previous Round Page", pager, "<", new Vector2(86f, 48f));
            roundPrevButton.onClick.AddListener(() => ChangeRoundSelectPage(-1));
            roundPageText = CreateText("Round Page Text", pager, string.Empty, 20, FontStyle.Bold, SketchPalette.MutedInk, TextAnchor.MiddleCenter);
            var pageLayout = roundPageText.rectTransform.gameObject.AddComponent<LayoutElement>();
            pageLayout.preferredWidth = 160f;
            pageLayout.preferredHeight = 48f;
            roundNextButton = CreateCommandButton("Next Round Page", pager, ">", new Vector2(86f, 48f));
            roundNextButton.onClick.AddListener(() => ChangeRoundSelectPage(1));

            var closeButton = CreateCommandButton("Close Round Select", roundSelectPanel, "Close", new Vector2(172f, 54f));
            closeButton.onClick.AddListener(HideRoundSelect);

            UpdateRoundSelectLayout(safeRoot.rect);
            RefreshRoundSelectButtons();
            roundSelectOverlay.gameObject.SetActive(false);
        }

        private void UpdateRoundSelectLayout(Rect safe)
        {
            if (roundSelectPanel == null || roundSelectGrid == null || roundSelectGridLayout == null)
            {
                return;
            }

            var plan = CalculateRoundSelectLayoutPlan(safe.width, safe.height);
            roundSelectPanel.sizeDelta = new Vector2(plan.PanelWidth, plan.PanelHeight);
            roundSelectGrid.spacing = new Vector2(plan.Spacing, plan.Spacing);
            roundSelectGrid.cellSize = new Vector2(plan.CellWidth, plan.CellHeight);
            roundSelectGridLayout.preferredHeight = plan.GridHeight;
        }

        private Button CreateRoundButton(Transform parent, int index, string roundName, out Text label)
        {
            var rect = CreatePanel($"Round {index + 1} Button", parent, ButtonPaperColor, SketchPalette.Ink, index * 19 + 303);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.colors = SketchUiFactory.ButtonColors();
            button.onClick.AddListener(() => PlaySfx(SfxCue.Button));

            label = CreateText("Round Label", rect, $"{index + 1}\n{roundName}", 20, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            label.resizeTextMinSize = 10;
            Stretch(label.rectTransform, 6f, 6f, 6f, 6f);
            return button;
        }

        private void ShowRoundSelect()
        {
            if (roundSelectOverlay != null)
            {
                roundSelectPage = Mathf.Clamp(roundIndex / RoundSelectPageSize, 0, RoundSelectPageCount() - 1);
#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
                if (qaRoundSelectPageOverride >= 0)
                {
                    roundSelectPage = Mathf.Clamp(qaRoundSelectPageOverride, 0, RoundSelectPageCount() - 1);
                }
#endif
                RefreshRoundSelectButtons();
                roundSelectOverlay.gameObject.SetActive(true);
            }
        }

        private void HideRoundSelect()
        {
            if (roundSelectOverlay != null)
            {
                roundSelectOverlay.gameObject.SetActive(false);
            }
        }

        private void SelectRound(int index)
        {
            if (index < 0 || index >= OnePlusOneMinusOneRules.GoalModeRounds.Length)
            {
                return;
            }

            if (index > highestUnlockedRoundIndex)
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "Clear earlier rounds first.";
                return;
            }

            HideRoundSelect();
            LoadRound(index);
        }

        private void ChangeRoundSelectPage(int direction)
        {
            roundSelectPage = Mathf.Clamp(roundSelectPage + direction, 0, RoundSelectPageCount() - 1);
            RefreshRoundSelectButtons();
        }

        private static int RoundSelectPageCount()
        {
            return Mathf.CeilToInt(OnePlusOneMinusOneRules.GoalModeRounds.Length / (float)RoundSelectPageSize);
        }

        private void RefreshRoundSelectButtons()
        {
            roundSelectPage = Mathf.Clamp(roundSelectPage, 0, RoundSelectPageCount() - 1);
            for (var i = 0; i < roundSelectButtons.Count; i++)
            {
                var roundNumber = roundSelectPage * RoundSelectPageSize + i;
                var button = roundSelectButtons[i];
                var label = roundSelectLabels[i];
                var exists = roundNumber < OnePlusOneMinusOneRules.GoalModeRounds.Length;
                button.gameObject.SetActive(exists);
                label.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                var unlocked = roundNumber <= highestUnlockedRoundIndex;
                var cleared = roundNumber < highestUnlockedRoundIndex;
                var current = roundNumber == roundIndex;
                button.interactable = unlocked;
                var statusPrefix = current ? "Now" : cleared ? "Done" : "Next";
                label.text = unlocked
                    ? $"{roundNumber + 1} {statusPrefix}\n{OnePlusOneMinusOneRules.GoalModeRounds[roundNumber].RoundName}"
                    : $"{roundNumber + 1}\nLocked";
                label.color = unlocked ? SketchPalette.Ink : new Color32(120, 112, 99, 190);

                var image = button.targetGraphic as Image;
                if (image != null)
                {
                    image.color = !unlocked
                        ? new Color32(181, 174, 158, 250)
                        : current
                            ? new Color32(255, 244, 190, 255)
                            : cleared
                            ? new Color32(235, 247, 231, 255)
                            : new Color32(246, 252, 238, 255);
                }
            }

            if (roundPageText != null)
            {
                roundPageText.text = $"Page {roundSelectPage + 1} / {RoundSelectPageCount()}";
            }

            if (roundPrevButton != null)
            {
                ApplyCommandButtonState(roundPrevButton, roundSelectPage > 0);
            }

            if (roundNextButton != null)
            {
                ApplyCommandButtonState(roundNextButton, roundSelectPage < RoundSelectPageCount() - 1);
            }
        }

        private void UnlockNextRound()
        {
            var nextUnlocked = Mathf.Min(roundIndex + 1, OnePlusOneMinusOneRules.GoalModeRounds.Length - 1);
            if (nextUnlocked <= highestUnlockedRoundIndex)
            {
                return;
            }

            highestUnlockedRoundIndex = nextUnlocked;
            PlayerPrefs.SetInt(HighestUnlockedRoundKey, highestUnlockedRoundIndex);
            PlayerPrefs.Save();
            RefreshRoundSelectButtons();
        }

        private void LoadRound(int index)
        {
            if (successRoutine != null)
            {
                StopCoroutine(successRoutine);
                successRoutine = null;
            }

            isAdvancing = false;
            roundIndex = Mathf.Clamp(index, 0, OnePlusOneMinusOneRules.GoalModeRounds.Length - 1);
            currentRoundFailureCount = 0;
            slotSymbols = new string[CurrentRound.SlotTypes.Length];
            slotStickPoses = new List<StickPose>[CurrentRound.SlotTypes.Length];
            for (var i = 0; i < slotStickPoses.Length; i++)
            {
                slotStickPoses[i] = new List<StickPose>(MaxSticksPerSlot);
            }
            headerTitleText.text = CurrentRound.RoundName;
            roundText.text = $"{roundIndex + 1} / {OnePlusOneMinusOneRules.GoalModeRounds.Length}";
            var hasTutorial = !string.IsNullOrWhiteSpace(CurrentRound.TutorialMessage);
            tutorialText.gameObject.SetActive(hasTutorial);
            tutorialText.text = hasTutorial ? CurrentRound.TutorialMessage : string.Empty;
            if (tutorialBubbleLayout != null)
            {
                tutorialBubbleLayout.preferredHeight = hasTutorial ? 122f : 48f;
            }

            if (tutorialTextLayout != null)
            {
                tutorialTextLayout.preferredHeight = hasTutorial ? 62f : 0f;
            }

            RebuildSlots();
            RebuildStickBank();
            var sampleWasPlaced = false;
#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
            if (qaFillSampleOnLoad)
            {
                FillCurrentRoundWithSample();
                sampleWasPlaced = true;
            }
#endif
            RefreshUi();
            TrackRoundStart();
            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = sampleWasPlaced ? "Ready to check." : "Drag sticks into boxes.";
        }

        private void RebuildSlots()
        {
            ClearChildren(equationRow);
            slotViews.Clear();
            friendFaces.Clear();

            var slotCount = CurrentRound.SlotTypes.Length;
            equationRowLayout.enabled = false;
            var safeWidth = safeRoot != null && safeRoot.rect.width > 1f ? safeRoot.rect.width : stageRoot.sizeDelta.x;
            var availableWidth = Mathf.Clamp(Mathf.Min(stageRoot.sizeDelta.x, safeWidth) - 72f, 300f, 1040f);
            var usesFixedTarget = CurrentRoundUsesFixedTarget;
            var plan = CalculateEquationLayoutPlan(slotCount, usesFixedTarget, availableWidth);
            if (equationAreaLayout != null)
            {
                equationAreaLayout.preferredHeight = Mathf.Clamp(plan.ContentHeight + 86f, 280f, 520f);
            }

            equationViewport.sizeDelta = new Vector2(plan.SlotAreaWidth, plan.ContentHeight + 12f);
            equationViewport.anchoredPosition = new Vector2(-plan.TotalWidth * 0.5f + plan.SlotAreaWidth * 0.5f, 0f);
            equationRow.sizeDelta = new Vector2(plan.SlotAreaWidth, plan.ContentHeight + 12f);
            equationRow.anchoredPosition = Vector2.zero;
            targetText.rectTransform.sizeDelta = new Vector2(plan.TargetWidth, plan.SlotHeight);
            targetText.gameObject.SetActive(usesFixedTarget);

            for (var i = 0; i < CurrentRound.SlotTypes.Length; i++)
            {
                var slotIndex = i;
                var slot = CreateSlotView(equationRow, CurrentRound.SlotTypes[i], plan.SlotWidth, plan.SlotHeight, i);
                slot.Button.onClick.AddListener(() => TapSlot(slotIndex));
                PositionSlot(slot.Root, slotIndex, slotCount, plan.SlotsPerRow, plan.SlotWidth, plan.SlotHeight, plan.Spacing, plan.RowGap, plan.SlotAreaWidth, plan.ContentHeight);
                slotViews.Add(slot);
            }

            if (usesFixedTarget)
            {
                targetText.fontSize = plan.Rows >= 3 ? 32 : plan.Rows == 2 ? 36 : 42;
                targetText.resizeTextMaxSize = targetText.fontSize;
                var lastRowIndex = plan.Rows - 1;
                var lastRowCount = RowSlotCount(slotCount, plan.SlotsPerRow, lastRowIndex);
                var lastRowWidth = RowWidth(lastRowCount, plan.SlotWidth, plan.Spacing);
                var lastRowY = RowY(lastRowIndex, plan.SlotHeight, plan.RowGap, plan.ContentHeight);
                targetText.rectTransform.anchoredPosition = new Vector2(-plan.TotalWidth * 0.5f + lastRowWidth + plan.TargetGap + plan.TargetWidth * 0.5f, lastRowY);
            }
        }

        public static EquationLayoutPlan CalculateEquationLayoutPlan(int slotCount, bool usesFixedTarget, float availableWidth)
        {
            slotCount = Mathf.Max(1, slotCount);
            availableWidth = Mathf.Clamp(availableWidth, 300f, 1040f);
            var compact = availableWidth < 420f;
            var narrow = availableWidth < 360f;
            var maxRows = compact ? ReleaseCompactMaxEquationRows : ReleaseMaxEquationRows;
            var spacing = narrow ? 6f : slotCount >= 8 ? 8f : 14f;
            var minSlotWidth = narrow ? ReleaseCompactMinEquationSlotWidth : compact ? 96f : ReleaseMinEquationSlotWidth;
            var minSlotHeight = narrow ? ReleaseCompactMinEquationSlotHeight : compact ? 108f : ReleaseMinEquationSlotHeight;
            var targetWidth = usesFixedTarget ? narrow ? 78f : compact ? 88f : 104f : 0f;
            var targetGap = usesFixedTarget ? narrow ? 12f : compact ? 16f : 24f : 0f;
            var rows = 1;
            while (rows < maxRows)
            {
                var slotsPerRow = Mathf.CeilToInt(slotCount / (float)rows);
                var lastRowCount = slotCount - slotsPerRow * (rows - 1);
                var topRowWidth = RowWidth(slotsPerRow, minSlotWidth, spacing);
                var lastRowWidth = RowWidth(lastRowCount, minSlotWidth, spacing) + targetGap + targetWidth;
                if (Mathf.Max(topRowWidth, lastRowWidth) <= availableWidth)
                {
                    break;
                }

                rows++;
            }

            var slotsPerFinalRow = Mathf.CeilToInt(slotCount / (float)rows);
            var idealSlotWidth = slotCount <= 3 ? 185f : slotCount <= 6 ? 142f : 118f;
            if (rows >= 4)
            {
                idealSlotWidth = Mathf.Min(idealSlotWidth, narrow ? 96f : 104f);
            }

            var maxSlotWidth = idealSlotWidth;
            for (var row = 0; row < rows; row++)
            {
                var rowCount = RowSlotCount(slotCount, slotsPerFinalRow, row);
                var reservedTargetWidth = row == rows - 1 ? targetWidth + targetGap : 0f;
                var fitWidth = (availableWidth - reservedTargetWidth - spacing * Mathf.Max(0, rowCount - 1)) / Mathf.Max(1, rowCount);
                maxSlotWidth = Mathf.Min(maxSlotWidth, fitWidth);
            }

            var slotWidth = Mathf.Clamp(maxSlotWidth, minSlotWidth, idealSlotWidth);
            var heightRatio = rows >= 4 ? 1.12f : rows >= 3 ? 1.16f : slotCount <= 3 ? 1.13f : 1.22f;
            var maxSlotHeight = slotCount <= 3 ? 210f : rows >= 4 ? 128f : rows >= 3 ? 148f : 188f;
            var slotHeight = Mathf.Clamp(slotWidth * heightRatio, minSlotHeight, maxSlotHeight);
            var rowGap = rows >= 4 ? 8f : rows >= 3 ? 10f : rows == 2 ? 12f : 18f;
            var contentHeight = rows * slotHeight + (rows - 1) * rowGap;
            var slotAreaWidth = 0f;
            for (var row = 0; row < rows; row++)
            {
                var rowCount = RowSlotCount(slotCount, slotsPerFinalRow, row);
                slotAreaWidth = Mathf.Max(slotAreaWidth, RowWidth(rowCount, slotWidth, spacing));
            }

            var lastRowCountFinal = RowSlotCount(slotCount, slotsPerFinalRow, rows - 1);
            var lastRowWidthFinal = RowWidth(lastRowCountFinal, slotWidth, spacing);
            var totalWidth = Mathf.Max(slotAreaWidth, lastRowWidthFinal + targetGap + targetWidth);
            return new EquationLayoutPlan(
                rows,
                slotsPerFinalRow,
                slotWidth,
                slotHeight,
                spacing,
                rowGap,
                slotAreaWidth,
                totalWidth,
                contentHeight,
                targetWidth,
                targetGap);
        }

        public static RoundSelectLayoutPlan CalculateRoundSelectLayoutPlan(float safeWidth, float safeHeight)
        {
            safeWidth = safeWidth > 1f ? safeWidth : 560f;
            safeHeight = safeHeight > 1f ? safeHeight : 840f;
            var panelWidth = Mathf.Min(520f, Mathf.Max(300f, safeWidth - 40f));
            var panelHeight = Mathf.Min(760f, Mathf.Max(500f, safeHeight - 72f));
            if (safeWidth < 360f)
            {
                panelWidth = Mathf.Max(ReleaseMinRoundSelectPanelWidth, safeWidth - 12f);
            }

            if (safeHeight < 600f)
            {
                panelHeight = Mathf.Max(ReleaseMinRoundSelectPanelHeight, safeHeight - 40f);
            }

            var innerPadding = panelWidth < 320f ? 8f : panelWidth < 360f ? 20f : panelWidth < 420f ? 36f : 60f;
            var innerWidth = panelWidth - innerPadding;
            var spacing = panelWidth < 360f ? 8f : innerWidth < 430f ? 12f : 16f;
            var cellMinWidth = panelWidth < 360f ? ReleaseMinRoundSelectCellWidth : panelWidth < 420f ? ReleaseMinEquationSlotWidth : 112f;
            var cellWidth = Mathf.Clamp((innerWidth - spacing * 2f) / 3f, cellMinWidth, 140f);
            var availableGridHeight = panelHeight - 246f;
            var cellMinHeight = panelHeight < 560f ? ReleaseMinRoundSelectCellHeight : 68f;
            var cellHeight = Mathf.Clamp((availableGridHeight - spacing * 3f) / 4f, cellMinHeight, 86f);
            var gridHeight = cellHeight * 4f + spacing * 3f;
            return new RoundSelectLayoutPlan(
                panelWidth,
                panelHeight,
                innerPadding,
                spacing,
                cellWidth,
                cellHeight,
                gridHeight);
        }

        public static StartupFlowPlan CalculateStartupFlowPlan(int savedHighestUnlockedRoundIndex)
        {
            var highestUnlocked = Mathf.Clamp(
                savedHighestUnlockedRoundIndex,
                0,
                OnePlusOneMinusOneRules.GoalModeRounds.Length - 1);
            var showRoundSelect = highestUnlocked > 0;
            var startRoundIndex = showRoundSelect ? highestUnlocked : 0;
            return new StartupFlowPlan(highestUnlocked, startRoundIndex, showRoundSelect);
        }

        public static InterstitialDecisionPlan CalculateInterstitialDecision(
            int roundIndex,
            int highestUnlockedRoundIndex,
            int failureCount,
            bool forceTestAds = false)
        {
            if (forceTestAds)
            {
                return new InterstitialDecisionPlan(true, "forced_test_ads");
            }

            var roundNumber = roundIndex + 1;
            if (roundIndex < highestUnlockedRoundIndex)
            {
                return new InterstitialDecisionPlan(false, "replay_round");
            }

            if (roundNumber <= ReleaseInterstitialGraceRoundCount)
            {
                return new InterstitialDecisionPlan(false, "early_round");
            }

            if (failureCount >= ReleaseInterstitialMaxFailuresBeforeSkip)
            {
                return new InterstitialDecisionPlan(false, "hard_clear");
            }

            if (roundNumber % ReleaseRoundClearInterstitialInterval != 0)
            {
                return new InterstitialDecisionPlan(false, "cadence");
            }

            return new InterstitialDecisionPlan(true, "round_milestone");
        }

        private static int RowSlotCount(int slotCount, int slotsPerRow, int row)
        {
            var remaining = slotCount - slotsPerRow * row;
            return Mathf.Clamp(remaining, 0, slotsPerRow);
        }

        private static float RowWidth(int slotCount, float slotWidth, float spacing)
        {
            if (slotCount <= 0)
            {
                return 0f;
            }

            return slotCount * slotWidth + Mathf.Max(0, slotCount - 1) * spacing;
        }

        private static float RowY(int row, float slotHeight, float rowGap, float contentHeight)
        {
            return contentHeight * 0.5f - slotHeight * 0.5f - row * (slotHeight + rowGap);
        }

        private static void PositionSlot(
            RectTransform slot,
            int slotIndex,
            int slotCount,
            int slotsPerRow,
            float slotWidth,
            float slotHeight,
            float spacing,
            float rowGap,
            float slotAreaWidth,
            float contentHeight)
        {
            var row = slotIndex / slotsPerRow;
            var col = slotIndex % slotsPerRow;
            var rowCount = RowSlotCount(slotCount, slotsPerRow, row);
            var rowWidth = RowWidth(rowCount, slotWidth, spacing);
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = new Vector2(slotWidth, slotHeight);
            slot.anchoredPosition = new Vector2(-slotAreaWidth * 0.5f + (slotAreaWidth - rowWidth) * 0.5f + slotWidth * 0.5f + col * (slotWidth + spacing), RowY(row, slotHeight, rowGap, contentHeight));
        }

        private void RebuildStickBank()
        {
            ClearChildren(stickBank);
            bankStickViews.Clear();
            ResetBankStickPoses();

            for (var i = 0; i < CurrentRound.StickCount; i++)
            {
                bankStickViews.Add(CreateRawStickView(stickBank, i));
            }

            LayoutBankSticks(CurrentRound.StickCount);
        }

        private void ResetBankStickPoses()
        {
            bankStickPoses.Clear();
            for (var i = 0; i < CurrentRound.StickCount; i++)
            {
                bankStickPoses.Add(StickPose.CenterVertical);
            }
        }

        private void LayoutBankSticks(int visibleCount)
        {
            if (stickBank == null)
            {
                return;
            }

            var count = Mathf.Clamp(visibleCount, 0, bankStickViews.Count);
            if (count <= 0)
            {
                return;
            }

            var dense = count > 12;
            var columns = dense ? Mathf.Min(count, 7) : Mathf.Min(count, 6);
            var rows = Mathf.CeilToInt(count / (float)columns);
            var spacingX = dense ? 104f : 122f;
            var spacingY = dense ? 118f : 142f;
            var stickSize = dense ? new Vector2(96f, 136f) : new Vector2(118f, 156f);
            var startY = (rows - 1) * spacingY * 0.5f;

            for (var i = 0; i < bankStickViews.Count; i++)
            {
                var child = bankStickViews[i];
                if (child == null || i >= count)
                {
                    continue;
                }

                var row = i / columns;
                var col = i % columns;
                var columnsInRow = Mathf.Min(columns, count - row * columns);
                var startX = -(columnsInRow - 1) * spacingX * 0.5f;
                child.anchorMin = new Vector2(0.5f, 0.5f);
                child.anchorMax = new Vector2(0.5f, 0.5f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.sizeDelta = stickSize;
                child.anchoredPosition = new Vector2(startX + col * spacingX, startY - row * spacingY);
            }
        }

        private void TapSlot(int slotIndex)
        {
            if (isAdvancing)
            {
                return;
            }

            if (slotStickPoses[slotIndex].Count <= 0)
            {
                feedbackText.color = SketchPalette.MutedInk;
                feedbackText.text = "Drag a stick here first.";
                PlaySfx(SfxCue.Button);
                StartCoroutine(Bump(slotViews[slotIndex].Root, 0.96f));
                return;
            }

            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = "Tap a stick to rotate.";
            PlaySfx(SfxCue.Button);
            StartCoroutine(Bump(slotViews[slotIndex].Root, 0.98f));
        }

        private void TapDroppedStick(int slotIndex, int stickIndex)
        {
            CycleStickPose(slotIndex, stickIndex);
        }

        private void TapBankStick(int bankIndex)
        {
            if (bankIndex < 0 || bankIndex >= bankStickPoses.Count)
            {
                return;
            }

            bankStickPoses[bankIndex] = NextOutsidePose(bankStickPoses[bankIndex]);
            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = "Turned.";
            PlaySfx(SfxCue.Rotate);
            RefreshUi();
        }

        private bool TryAddStickToSlot(int slotIndex)
        {
            return TryAddStickToSlot(slotIndex, true, DefaultPoseForNewStick(slotIndex));
        }

        private bool TryAddStickToSlot(int slotIndex, bool requireBankStick, StickPose preferredPose)
        {
            if (isAdvancing)
            {
                return false;
            }

            if (requireBankStick && RemainingSticks() <= 0)
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "No sticks left.";
                PlaySfx(SfxCue.Fail);
                StartCoroutine(Bump(slotViews[slotIndex].Root, 0.92f));
                return false;
            }

            if (slotStickPoses[slotIndex].Count >= MaxSticksPerSlot)
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "Box holds 3.";
                PlaySfx(SfxCue.Fail);
                StartCoroutine(Bump(slotViews[slotIndex].Root, 0.92f));
                return false;
            }

            AddPoseToSlot(slotIndex, preferredPose);
            UpdateRecognizedSymbol(slotIndex);

            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = string.IsNullOrEmpty(slotSymbols[slotIndex])
                ? "Shape the sticks."
                : $"{slotSymbols[slotIndex]}";
            PlaySfx(string.IsNullOrEmpty(slotSymbols[slotIndex]) ? SfxCue.Drop : SfxCue.Success);
            StartCoroutine(Bump(slotViews[slotIndex].Root, 1.08f));
            RefreshUi();

            return true;
        }

        private bool TryMoveStickToSlot(int sourceSlotIndex, int sourceStickIndex, int targetSlotIndex, StickPose targetPose)
        {
            if (sourceSlotIndex == targetSlotIndex)
            {
                feedbackText.color = SketchPalette.MutedInk;
                feedbackText.text = "Snapped.";
                PlaySfx(SfxCue.Drop);
                SetStickPoseFromPointer(sourceSlotIndex, sourceStickIndex, targetPose);
                return true;
            }

            if (sourceSlotIndex < 0 || sourceSlotIndex >= slotStickPoses.Length ||
                sourceStickIndex < 0 || sourceStickIndex >= slotStickPoses[sourceSlotIndex].Count)
            {
                return false;
            }

            if (slotStickPoses[targetSlotIndex].Count >= MaxSticksPerSlot)
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "Box holds 3.";
                PlaySfx(SfxCue.Fail);
                StartCoroutine(Bump(slotViews[targetSlotIndex].Root, 0.92f));
                return false;
            }

            var sourcePose = slotStickPoses[sourceSlotIndex][sourceStickIndex];
            RemoveStickFromSlot(sourceSlotIndex, sourceStickIndex);
            var added = TryAddStickToSlot(targetSlotIndex, false, targetPose);
            if (!added)
            {
                slotStickPoses[sourceSlotIndex].Insert(Mathf.Min(sourceStickIndex, slotStickPoses[sourceSlotIndex].Count), sourcePose);
                UpdateRecognizedSymbol(sourceSlotIndex);
            }

            return added;
        }

        private void RemoveStickFromSlot(int slotIndex, int stickIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotStickPoses.Length ||
                stickIndex < 0 || stickIndex >= slotStickPoses[slotIndex].Count)
            {
                return;
            }

            slotStickPoses[slotIndex].RemoveAt(stickIndex);
            NormalizeSingleStickInSlot(slotIndex);
            UpdateRecognizedSymbol(slotIndex);
        }

        private void NormalizeSingleStickInSlot(int slotIndex)
        {
            if (slotStickPoses[slotIndex].Count != 1)
            {
                return;
            }

            var pose = slotStickPoses[slotIndex][0];
            if (pose == StickPose.LeftVertical || pose == StickPose.RightVertical)
            {
                slotStickPoses[slotIndex][0] = StickPose.CenterVertical;
                return;
            }

            if (pose == StickPose.TopHorizontal || pose == StickPose.BottomHorizontal)
            {
                slotStickPoses[slotIndex][0] = StickPose.CenterHorizontal;
            }
        }

        private void ResetRound()
        {
            if (isAdvancing)
            {
                return;
            }

            for (var i = 0; i < slotSymbols.Length; i++)
            {
                slotSymbols[i] = string.Empty;
                slotStickPoses[i].Clear();
            }

            ResetBankStickPoses();
            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = "Place sticks again.";
            PlaySfx(SfxCue.Button);
            FirebaseTelemetry.LogEvent("round_reset", RoundParameters());
            RefreshUi();
        }

        private void CheckCurrent()
        {
            if (isAdvancing)
            {
                return;
            }

            if (!AllSlotsHaveSticks())
            {
                var failureReason = HasWrongShapeForBox() ? "wrong_shape" :
                    HasUnrecognizedShape() ? "unrecognized_shape" : "empty_box";
                TrackCheckFailure(failureReason);
                feedbackText.color = FailureColor;
                feedbackText.text = FriendlyFailureText(failureReason);
                PlaySfx(SfxCue.Fail);
                StartCoroutine(ShakeEquation());
                return;
            }

            if (RemainingSticks() != 0)
            {
                var failureReason = RemainingSticks() > 0 ? "sticks_remain" : "too_many_sticks";
                TrackCheckFailure(failureReason);
                feedbackText.color = FailureColor;
                feedbackText.text = FriendlyFailureText(failureReason);
                PlaySfx(SfxCue.Fail);
                StartCoroutine(ShakeEquation());
                return;
            }

            if (OnePlusOneMinusOneRules.IsRoundSolved(CurrentRound, slotSymbols, out var result, out var reason))
            {
                var shouldOfferInterstitial = ShouldOfferRoundClearInterstitial(out var interstitialReason);
                TrackRoundClear(result.Value);
                feedbackText.color = SuccessColor;
                feedbackText.text = SuccessFeedback();
                PlaySfx(roundIndex >= OnePlusOneMinusOneRules.GoalModeRounds.Length - 1 ? SfxCue.Finale : SfxCue.Success);
                UnlockNextRound();
                TryShowRoundClearInterstitial(shouldOfferInterstitial, interstitialReason);
                successRoutine = StartCoroutine(AdvanceAfterSuccess());
                return;
            }

            TrackCheckFailure(reason);
            feedbackText.color = FailureColor;
            feedbackText.text = FriendlyFailureText(reason);
            PlaySfx(SfxCue.Fail);
            StartCoroutine(ShakeEquation());
        }

        private string SuccessFeedback()
        {
            var messages = Array.IndexOf(slotSymbols, "=") >= 0 ? EqualitySuccessMessages : TargetSuccessMessages;
            return messages[Mathf.Abs(roundIndex * 7 + currentRoundFailureCount * 3) % messages.Length];
        }

        private static string FriendlyFailureText(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "Not yet.";
            }

            if (reason == "wrong_shape")
            {
                return "Shape?";
            }

            if (reason == "unrecognized_shape")
            {
                return "Try another shape.";
            }

            if (reason == "empty_box")
            {
                return "Fill every box.";
            }

            if (reason == "sticks_remain")
            {
                return "Still holding sticks.";
            }

            if (reason == "too_many_sticks")
            {
                return "Too many sticks.";
            }

            if (reason.StartsWith("Result is", StringComparison.Ordinal))
            {
                return "Not yet.";
            }

            if (reason.StartsWith("Cannot divide", StringComparison.Ordinal))
            {
                return "No zero divide.";
            }

            if (reason.IndexOf("not", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Not balanced.";
            }

            if (reason.Contains("Operator"))
            {
                return "Needs a number.";
            }

            if (reason.Contains("="))
            {
                return "Check both sides.";
            }

            return reason;
        }

        private void InitializeTelemetryAndAds()
        {
            try
            {
                FirebaseTelemetry.Initialize();
                FirebaseTelemetry.SetContext("game", GameIdentifier);
                FirebaseTelemetry.SetContext("round_count", OnePlusOneMinusOneRules.GoalModeRounds.Length.ToString());
                FirebaseTelemetry.SetContext("highest_unlocked_round", (highestUnlockedRoundIndex + 1).ToString());
                FirebaseTelemetry.LogEvent("app_open", AppOpenParameters());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[1+1] Firebase initialization skipped: {exception.GetType().Name}");
            }

            try
            {
                MannLabAdMob.InitializeGameOverInterstitial(
                    GameIdentifier,
                    GetConfiguredAdUnitId("ios_interstitial", ProductionIosInterstitialAdUnitId),
                    AdMobBridgeInterstitialInterval,
                    GetConfiguredAdUnitId("android_interstitial", ProductionAndroidInterstitialAdUnitId));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[1+1] AdMob initialization skipped: {exception.GetType().Name}");
            }
        }

        private static string GetConfiguredAdUnitId(string key, string fallback)
        {
            var config = Resources.Load<TextAsset>(ReleaseAdMobConfigResourceName);
            if (config == null || string.IsNullOrWhiteSpace(config.text))
            {
                return fallback;
            }

            var lines = config.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var currentKey = line.Substring(0, separator).Trim();
                if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = line.Substring(separator + 1).Trim();
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }

            return fallback;
        }

        private void InitializeSfx()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = ReleaseSfxVolume;
            sfxClips[SfxCue.Button] = CreateToneClip("1eq1_button", 520f, 620f, 0.055f, 0.42f);
            sfxClips[SfxCue.Pick] = CreateToneClip("1eq1_pick", 620f, 740f, 0.07f, 0.48f);
            sfxClips[SfxCue.Drop] = CreateToneClip("1eq1_drop", 760f, 560f, 0.085f, 0.52f);
            sfxClips[SfxCue.Rotate] = CreateToneClip("1eq1_rotate", 540f, 860f, 0.075f, 0.42f);
            sfxClips[SfxCue.Fail] = CreateToneClip("1eq1_fail", 210f, 150f, 0.11f, 0.38f);
            sfxClips[SfxCue.Success] = CreateToneClip("1eq1_success", 620f, 930f, 0.13f, 0.54f);
            sfxClips[SfxCue.Finale] = CreateToneClip("1eq1_finale", 520f, 1040f, 0.18f, 0.58f);
        }

        private void PlaySfx(SfxCue cue)
        {
            if (sfxSource == null || !sfxClips.TryGetValue(cue, out var clip) || clip == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (sfxLastPlayedAt.TryGetValue(cue, out var lastPlayedAt) &&
                now - lastPlayedAt < ReleaseSfxCooldownSeconds)
            {
                return;
            }

            sfxLastPlayedAt[cue] = now;
            sfxSource.PlayOneShot(clip, 1f);
        }

        private static AudioClip CreateToneClip(string name, float startFrequency, float endFrequency, float duration, float amplitude)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[sampleCount];
            var phase = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)Mathf.Max(1, sampleCount - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency * Mathf.PI * 2f / sampleRate;
                var attack = Mathf.Clamp01(t / 0.08f);
                var release = 1f - Mathf.SmoothStep(0f, 1f, t);
                var envelope = attack * release;
                var softHarmonic = Mathf.Sin(phase * 2f) * 0.18f;
                data[i] = (Mathf.Sin(phase) + softHarmonic) * envelope * amplitude;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void TrackRoundStart()
        {
            FirebaseTelemetry.SetContext("round", (roundIndex + 1).ToString());
            FirebaseTelemetry.SetContext("round_name", CurrentRound.RoundName);
            FirebaseTelemetry.SetContext("stick_count", CurrentRound.StickCount.ToString());
            FirebaseTelemetry.LogEvent("round_start", RoundParameters());
        }

        private void TrackRoundClear(double result)
        {
            var parameters = RoundParameters();
            parameters["result"] = OnePlusOneMinusOneRules.FormatNumber(result);
            parameters["expression"] = string.Join(" ", slotSymbols);
            FirebaseTelemetry.SetContext("last_result", parameters["result"]);
            FirebaseTelemetry.SetContext("last_expression", parameters["expression"]);
            FirebaseTelemetry.LogEvent("round_clear", parameters);
        }

        private void TrackCheckFailure(string reason)
        {
            currentRoundFailureCount++;
            var parameters = RoundParameters();
            parameters["reason"] = reason;
            parameters["expression"] = string.Join(" ", slotSymbols);
            parameters["failure_count"] = currentRoundFailureCount.ToString();
            FirebaseTelemetry.SetContext("last_failure", reason);
            FirebaseTelemetry.LogEvent("round_check_failed", parameters);
        }

        private bool ShouldOfferRoundClearInterstitial(out string reason)
        {
#if MANNLAB_ADMOB_FORCE_TEST_ADS
            var decision = CalculateInterstitialDecision(roundIndex, highestUnlockedRoundIndex, currentRoundFailureCount, true);
#else
            var decision = CalculateInterstitialDecision(roundIndex, highestUnlockedRoundIndex, currentRoundFailureCount);
#endif
            reason = decision.Reason;
            return decision.ShouldOffer;
        }

        private void TryShowRoundClearInterstitial(bool shouldOfferInterstitial, string reason)
        {
            var parameters = RoundParameters();
            parameters["cadence"] = RoundClearInterstitialInterval.ToString();
            parameters["failure_count"] = currentRoundFailureCount.ToString();
            parameters["reason"] = reason;
            parameters["will_show"] = shouldOfferInterstitial ? "true" : "false";
            FirebaseTelemetry.LogEvent("ad_interstitial_opportunity", parameters);

            if (!shouldOfferInterstitial)
            {
                return;
            }

            MannLabAdMob.TryShowGameOverInterstitial();
        }

        private Dictionary<string, string> RoundParameters()
        {
            return new Dictionary<string, string>
            {
                { "game", GameIdentifier },
                { "round", (roundIndex + 1).ToString() },
                { "round_name", CurrentRound.RoundName },
                { "stick_count", CurrentRound.StickCount.ToString() },
                { "slots", CurrentRound.SlotTypes.Length.ToString() },
                { "highest_unlocked_round", (highestUnlockedRoundIndex + 1).ToString() },
                { "is_replay", roundIndex < highestUnlockedRoundIndex ? "true" : "false" }
            };
        }

        private Dictionary<string, string> AppOpenParameters()
        {
            return new Dictionary<string, string>
            {
                { "game", GameIdentifier },
                { "round_count", OnePlusOneMinusOneRules.GoalModeRounds.Length.ToString() },
                { "highest_unlocked_round", (highestUnlockedRoundIndex + 1).ToString() },
                { "app_version", Application.version },
                { "platform", Application.platform.ToString() }
            };
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR || MANNLAB_STORE_CAPTURE
        private IEnumerator ForceCrashlyticsTestAfterStartup()
        {
            yield return null;
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

            crashlyticsTestTapCount++;
            crashlyticsTestTapDeadline = Time.unscaledTime + CrashlyticsTestTapWindowSeconds;
            if (crashlyticsTestTapCount < CrashlyticsTestTapCount)
            {
                return false;
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
                    { "game", GameIdentifier },
                    { "trigger", trigger }
                });
            FirebaseTelemetry.ForceCrashForTesting();
        }

        private static bool TryReadCrashlyticsTestTap(out Vector2 position)
        {
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    return true;
                }
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

        private void ApplyQaStartupOverrides(ref int startRoundIndex, ref bool showRoundSelect)
        {
            qaFillSampleOnLoad = HasQaFlag(QaFillSampleArgument, "qaFillSample");

            if (TryReadQaRound(QaUnlockedArgumentPrefix, "qaUnlocked", out var unlockedRound))
            {
                highestUnlockedRoundIndex = Mathf.Max(
                    highestUnlockedRoundIndex,
                    Mathf.Clamp(unlockedRound - 1, 0, OnePlusOneMinusOneRules.GoalModeRounds.Length - 1));
            }

            if (TryReadQaRound(QaRoundArgumentPrefix, "qaRound", out var roundNumber))
            {
                startRoundIndex = Mathf.Clamp(roundNumber - 1, 0, OnePlusOneMinusOneRules.GoalModeRounds.Length - 1);
                highestUnlockedRoundIndex = Mathf.Max(highestUnlockedRoundIndex, startRoundIndex);
                showRoundSelect = false;
            }

            if (HasQaFlag(QaRoundsArgument, "qaRounds"))
            {
                showRoundSelect = true;
            }

            if (TryReadQaRound(QaRoundPageArgumentPrefix, "qaRoundPage", out var roundPageNumber))
            {
                qaRoundSelectPageOverride = Mathf.Clamp(roundPageNumber - 1, 0, RoundSelectPageCount() - 1);
                showRoundSelect = true;
            }
        }

        private void FillCurrentRoundWithSample()
        {
            var symbols = CurrentRound.SampleSolution.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (symbols.Length != slotStickPoses.Length)
            {
                return;
            }

            for (var i = 0; i < symbols.Length; i++)
            {
                slotStickPoses[i].Clear();
                slotStickPoses[i].AddRange(SamplePosesForSymbol(symbols[i]));
                UpdateRecognizedSymbol(i);
            }

            bankStickPoses.Clear();
            bankStickViews.Clear();
            ClearChildren(stickBank);
        }

        private static StickPose[] SamplePosesForSymbol(string symbol)
        {
            switch (symbol)
            {
                case "1":
                    return new[] { StickPose.CenterVertical };
                case "11":
                    return new[] { StickPose.LeftVertical, StickPose.RightVertical };
                case "111":
                    return new[] { StickPose.LeftVertical, StickPose.CenterVertical, StickPose.RightVertical };
                case "-":
                    return new[] { StickPose.CenterHorizontal };
                case "/":
                    return new[] { StickPose.CenterSlash };
                case "+":
                    return new[] { StickPose.CenterVertical, StickPose.CenterHorizontal };
                case "x":
                case "×":
                    return new[] { StickPose.CenterSlash, StickPose.CenterBackslash };
                case "*":
                    return new[] { StickPose.CenterVertical, StickPose.CenterSlash, StickPose.CenterBackslash };
                case "=":
                    return new[] { StickPose.TopHorizontal, StickPose.BottomHorizontal };
                default:
                    return Array.Empty<StickPose>();
            }
        }

        private static bool TryReadQaRound(string argumentPrefix, string queryKey, out int roundNumber)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith(argumentPrefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(argument.Substring(argumentPrefix.Length), out roundNumber))
                {
                    return true;
                }
            }

            if (TryReadQaQueryValue(queryKey, out var queryValue) &&
                int.TryParse(queryValue, out roundNumber))
            {
                return true;
            }

            roundNumber = 0;
            return false;
        }

        private static bool HasQaFlag(string argument, string queryKey)
        {
            foreach (var current in Environment.GetCommandLineArgs())
            {
                if (string.Equals(current, argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return TryReadQaQueryValue(queryKey, out var queryValue) && IsTruthy(queryValue);
        }

        private static bool TryReadQaQueryValue(string key, out string value)
        {
            var absoluteUrl = Application.absoluteURL;
            var queryStart = absoluteUrl.IndexOf('?', StringComparison.Ordinal);
            if (queryStart < 0)
            {
                value = string.Empty;
                return false;
            }

            var queryEnd = absoluteUrl.IndexOf('#', queryStart + 1);
            var query = queryEnd >= 0
                ? absoluteUrl.Substring(queryStart + 1, queryEnd - queryStart - 1)
                : absoluteUrl.Substring(queryStart + 1);
            var pairs = query.Split('&');
            for (var i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i];
                if (string.IsNullOrEmpty(pair))
                {
                    continue;
                }

                var separator = pair.IndexOf('=');
                var rawKey = separator >= 0 ? pair.Substring(0, separator) : pair;
                if (!string.Equals(Uri.UnescapeDataString(rawKey), key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = separator >= 0
                    ? Uri.UnescapeDataString(pair.Substring(separator + 1))
                    : "1";
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static bool IsTruthy(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   (value == "1" ||
                    value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("yes", StringComparison.OrdinalIgnoreCase));
        }
#endif

        private IEnumerator AdvanceAfterSuccess()
        {
            isAdvancing = true;
            StartCoroutine(Bump(equationRow, 1.04f));
            StartCoroutine(CelebrateSolvedSlots());
            yield return new WaitForSeconds(roundIndex >= OnePlusOneMinusOneRules.GoalModeRounds.Length - 1 ? 1.45f : 1.12f);

            if (roundIndex >= OnePlusOneMinusOneRules.GoalModeRounds.Length - 1)
            {
                isAdvancing = false;
                feedbackText.color = SuccessColor;
                feedbackText.text = "Goal Mode clear!";
                yield break;
            }

            LoadRound(roundIndex + 1);
        }

        private IEnumerator CelebrateSolvedSlots()
        {
            for (var i = 0; i < slotViews.Count; i++)
            {
                if (slotViews[i].Root != null)
                {
                    StartCoroutine(Bump(slotViews[i].Root, 1.045f));
                }

                if (i < slotViews.Count - 1)
                {
                    yield return new WaitForSeconds(0.035f);
                }
            }
        }

        private void RefreshUi()
        {
            var remaining = RemainingSticks();
            stickText.text = $"left {remaining} / total {CurrentRound.StickCount}";
            bankHintText.text = remaining > 0 ? "tap outside sticks to rotate" : "tap placed sticks or drag them outside";
            targetText.gameObject.SetActive(CurrentRoundUsesFixedTarget);
            targetText.text = CurrentRoundUsesFixedTarget
                ? $"= {OnePlusOneMinusOneRules.FormatNumber(CurrentRound.TargetValue)}"
                : string.Empty;

            for (var i = 0; i < slotViews.Count; i++)
            {
                UpdateSlotView(i, slotViews[i]);
            }

            for (var i = 0; i < bankStickViews.Count; i++)
            {
                if (bankStickViews[i] != null)
                {
                    bankStickViews[i].gameObject.SetActive(i < remaining);
                    if (i < remaining)
                    {
                        RebuildBankStickView(bankStickViews[i], i);
                    }
                }
            }

            LayoutBankSticks(remaining);
            UpdateCheckButtonState();
        }

        private int RemainingSticks()
        {
            return bankStickPoses.Count;
        }

        private bool AllSlotsHaveSticks()
        {
            for (var i = 0; i < slotStickPoses.Length; i++)
            {
                if (slotStickPoses[i].Count <= 0 || string.IsNullOrEmpty(slotSymbols[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllSlotsContainAnyStick()
        {
            for (var i = 0; i < slotStickPoses.Length; i++)
            {
                if (slotStickPoses[i].Count <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasUnrecognizedShape()
        {
            for (var i = 0; i < slotStickPoses.Length; i++)
            {
                if (slotStickPoses[i].Count > 0 &&
                    string.IsNullOrEmpty(slotSymbols[i]) &&
                    (string.IsNullOrEmpty(OnePlusOneMinusOneRules.RecognizeToken(slotStickPoses[i])) ||
                     IsPartialTwoStickInProgress(i)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasWrongShapeForBox()
        {
            for (var i = 0; i < slotStickPoses.Length; i++)
            {
                var rawSymbol = OnePlusOneMinusOneRules.RecognizeToken(slotStickPoses[i]);
                if (!string.IsNullOrEmpty(rawSymbol) &&
                    string.IsNullOrEmpty(slotSymbols[i]) &&
                    !IsPartialTwoStickInProgress(i))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPartialTwoStickInProgress(int slotIndex)
        {
            return slotStickPoses[slotIndex].Count == 1 &&
                   string.IsNullOrEmpty(slotSymbols[slotIndex]) &&
                   CountRecognizableTwoStickTokens() > 0;
        }

        private void UpdateCheckButtonState()
        {
            if (checkButton == null)
            {
                return;
            }

            var ready = RemainingSticks() == 0 && AllSlotsContainAnyStick();
            ApplyCommandButtonState(checkButton, ready);
        }

        private void AddPoseToSlot(int slotIndex, StickPose preferredPose)
        {
            var poses = slotStickPoses[slotIndex];
            var nextPose = FilterPoseForTutorial(slotIndex, preferredPose, poses.Count + 1);
            if (poses.Count == 1 &&
                poses[0] == StickPose.CenterVertical &&
                nextPose == StickPose.CenterVertical)
            {
                poses[0] = StickPose.LeftVertical;
                poses.Add(StickPose.RightVertical);
                return;
            }

            if (poses.Count == 1 &&
                poses[0] == StickPose.CenterHorizontal &&
                nextPose == StickPose.CenterHorizontal)
            {
                poses[0] = StickPose.TopHorizontal;
                poses.Add(StickPose.BottomHorizontal);
                return;
            }

            poses.Add(nextPose);
        }

        private void SetStickPoseFromPointer(int slotIndex, int stickIndex, StickPose pose)
        {
            if (slotIndex < 0 || slotIndex >= slotStickPoses.Length ||
                stickIndex < 0 || stickIndex >= slotStickPoses[slotIndex].Count)
            {
                return;
            }

            slotStickPoses[slotIndex][stickIndex] = FilterPoseForTutorial(slotIndex, pose, slotStickPoses[slotIndex].Count);
            UpdateRecognizedSymbol(slotIndex);
            RefreshUi();
        }

        private void CycleStickPose(int slotIndex, int stickIndex)
        {
            var poses = slotStickPoses[slotIndex];
            if (stickIndex < 0 || stickIndex >= poses.Count)
            {
                return;
            }

            var candidates = PhysicalPoseCycle(poses.Count);
            var current = poses[stickIndex];
            var currentIndex = candidates.IndexOf(current);
            poses[stickIndex] = candidates[(currentIndex + 1 + candidates.Count) % candidates.Count];
            UpdateRecognizedSymbol(slotIndex);
            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = string.IsNullOrEmpty(slotSymbols[slotIndex]) ? "?" : slotSymbols[slotIndex];
            PlaySfx(SfxCue.Rotate);
            StartCoroutine(Bump(slotViews[slotIndex].Root, 1.05f));
            RefreshUi();
        }

        private static StickPose NextOutsidePose(StickPose pose)
        {
            return pose == StickPose.CenterVertical ? StickPose.CenterSlash :
                pose == StickPose.CenterSlash ? StickPose.CenterHorizontal :
                pose == StickPose.CenterHorizontal ? StickPose.CenterBackslash :
                StickPose.CenterVertical;
        }

        private StickPose DefaultPoseForNewStick(int slotIndex)
        {
            return StickPose.CenterVertical;
        }

        private StickPose PoseForPointer(int slotIndex, Vector2 screenPosition, StickPose fallback)
        {
            if (slotIndex < 0 || slotIndex >= slotViews.Count)
            {
                return fallback;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(slotViews[slotIndex].Root, screenPosition, null, out var localPoint))
            {
                return fallback;
            }

            var rect = slotViews[slotIndex].Root.rect;
            var normalizedX = localPoint.x / Mathf.Max(1f, rect.width * 0.5f);
            var normalizedY = localPoint.y / Mathf.Max(1f, rect.height * 0.5f);
            var absX = Mathf.Abs(normalizedX);
            var absY = Mathf.Abs(normalizedY);

            if (absX < 0.28f && absY < 0.28f)
            {
                return fallback;
            }

            if (normalizedX < -0.42f && absY > absX * 0.74f)
            {
                return StickPose.LeftVertical;
            }

            if (normalizedX > 0.42f && absY > absX * 0.74f)
            {
                return StickPose.RightVertical;
            }

            if (absX > absY * 1.28f)
            {
                return StickPose.CenterHorizontal;
            }

            if (Mathf.Abs(normalizedX - normalizedY) < 0.46f)
            {
                return StickPose.CenterSlash;
            }

            if (Mathf.Abs(normalizedX + normalizedY) < 0.46f)
            {
                return StickPose.CenterBackslash;
            }

            return StickPose.CenterVertical;
        }

        private StickPose FilterPoseForTutorial(int slotIndex, StickPose pose, int stickCountAfterAction)
        {
            var candidates = PhysicalPoseCycle(stickCountAfterAction);
            return candidates.Contains(pose) ? pose : candidates[0];
        }

        private static List<StickPose> PhysicalPoseCycle(int stickCountAfterAction)
        {
            var poses = new List<StickPose>
            {
                StickPose.CenterVertical,
                StickPose.CenterHorizontal,
                StickPose.CenterSlash,
                StickPose.CenterBackslash
            };

            if (stickCountAfterAction >= 2)
            {
                poses.Add(StickPose.LeftVertical);
                poses.Add(StickPose.RightVertical);
            }

            return poses;
        }

        private int CountRecognizableTwoStickTokens()
        {
            var count = 0;
            if (CanUseTokenInSlot("11"))
            {
                count++;
            }

            if (CanUseTokenInSlot("+"))
            {
                count++;
            }

            if (CanUseTokenInSlot("×"))
            {
                count++;
            }

            if (CanUseTokenInSlot("="))
            {
                count++;
            }

            return count;
        }

        private static void AddPoseCandidate(List<StickPose> poses, StickPose pose)
        {
            if (!poses.Contains(pose))
            {
                poses.Add(pose);
            }
        }

        private static bool CanUseTokenInSlot(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return false;
            }

            try
            {
                OnePlusOneMinusOneRules.GetToken(symbol);
                return true;
            }
            catch (System.ArgumentException)
            {
                return false;
            }
        }

        private void UpdateRecognizedSymbol(int slotIndex)
        {
            var symbol = OnePlusOneMinusOneRules.RecognizeToken(slotStickPoses[slotIndex]);
            if (!CanUseTokenInSlot(symbol))
            {
                slotSymbols[slotIndex] = string.Empty;
                return;
            }

            slotSymbols[slotIndex] = symbol;
        }

        private SlotView CreateSlotView(Transform parent, PuzzleSlotType slotType, float width, float height, int seed)
        {
            var root = CreatePanel($"Slot {seed + 1}", parent, SlotLockedColor, SketchPalette.Ink, seed * 17 + 5);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            button.colors = SketchUiFactory.ButtonColors();

            var recognition = CreateText("Recognition Label", root, "read: -", height >= 250f ? 17 : 13, FontStyle.Bold, SketchPalette.MutedInk, TextAnchor.UpperCenter);
            recognition.rectTransform.anchorMin = new Vector2(0f, 1f);
            recognition.rectTransform.anchorMax = new Vector2(1f, 1f);
            recognition.rectTransform.pivot = new Vector2(0.5f, 1f);
            recognition.rectTransform.offsetMin = new Vector2(10f, height >= 250f ? -38f : -30f);
            recognition.rectTransform.offsetMax = new Vector2(-10f, -8f);

            var stickLayer = CreateRect("Slot Sticks", root);
            var stickTop = height >= 250f ? 72f : 42f;
            var stickBottom = height >= 250f ? 54f : 24f;
            Stretch(stickLayer, width >= 220f ? 42f : 24f, stickTop, width >= 220f ? 42f : 24f, stickBottom);

            var hint = CreateText("Slot Hint", root, "drop", 15, FontStyle.Bold, new Color32(150, 142, 128, 165), TextAnchor.MiddleCenter);
            Stretch(hint.rectTransform, 0, 32f, 0, 0);
            recognition.rectTransform.SetAsLastSibling();

            return new SlotView(root, button, recognition, hint, stickLayer);
        }

        private RectTransform CreateRawStickView(Transform parent, int seed)
        {
            var root = CreateRect($"Raw Stick {seed + 1}", parent);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            root.sizeDelta = new Vector2(118f, 156f);
            root.gameObject.AddComponent<RawStickDragHandler>().Initialize(this, group, -1, seed);
            return root;
        }

        private void RebuildBankStickView(RectTransform root, int bankIndex)
        {
            ClearChildren(root);
            if (bankIndex < 0 || bankIndex >= bankStickPoses.Count)
            {
                return;
            }

            var bankScale = Mathf.Clamp(Mathf.Min(root.sizeDelta.x / 118f, root.sizeDelta.y / 156f), 0.72f, 1f);
            PoseToVisual(bankStickPoses[bankIndex], 108f * bankScale, 134f * bankScale, 40f * bankScale, 25f * bankScale, out var size, out var offset, out var rotation);
            var friend = CreateRect("Raw Stick Body", root);
            friend.anchorMin = new Vector2(0.5f, 0.5f);
            friend.anchorMax = new Vector2(0.5f, 0.5f);
            friend.pivot = new Vector2(0.5f, 0.5f);
            friend.anchoredPosition = offset;
            friend.sizeDelta = size;
            friend.localRotation = Quaternion.Euler(0f, 0f, rotation);
            CreateStickBody(friend, new Color32(255, 253, 244, 255), bankIndex, true);
        }

        private RectTransform CreateFriendVisual(RectTransform parent, PuzzleTokenData token, Vector2 size, int seed)
        {
            var friend = CreateRect($"Friend {token.Symbol}", parent);
            friend.sizeDelta = size;

            var hatch = friend.gameObject.AddComponent<SketchHatchFillGraphic>();
            hatch.BackgroundColor = token.DisplayColor;
            hatch.HatchColor = new Color(token.DisplayColor.r * 0.82f, token.DisplayColor.g * 0.82f, token.DisplayColor.b * 0.82f, 0.42f);
            hatch.Inset = 9f;
            hatch.Spacing = 17f;
            hatch.Thickness = 2.3f;
            hatch.Seed = seed * 29 + 3;
            hatch.raycastTarget = false;

            var outline = CreateRect("Friend Outline", friend);
            Stretch(outline);
            var outlineGraphic = outline.gameObject.AddComponent<SketchOutlineGraphic>();
            outlineGraphic.color = SketchPalette.Ink;
            outlineGraphic.Thickness = 3.2f;
            outlineGraphic.Jitter = 4.2f;
            outlineGraphic.Strokes = 2;
            outlineGraphic.Seed = seed * 31 + 7;
            outlineGraphic.raycastTarget = false;

            var symbol = CreateText("Friend Symbol", friend, token.Symbol, token.Symbol == "11" ? 35 : 43, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            Stretch(symbol.rectTransform, 0, 0, 0, 2);

            var leftEye = CreateDot("Left Eye", friend, new Vector2(-19f, 22f));
            var rightEye = CreateDot("Right Eye", friend, new Vector2(19f, 22f));
            var style = FaceStyleForSymbol(token.Symbol);
            var mouth = CreateMouth(friend, style);
            friendFaces.Add(new FriendFaceView(friend, leftEye, rightEye, mouth, seed, ChatterWeightForStyle(style), TalkWeightForStyle(style)));

            if (token.Symbol == "/")
            {
                friend.localRotation = Quaternion.Euler(0f, 0f, -12f);
            }
            else if (token.Symbol == "+")
            {
                friend.localRotation = Quaternion.Euler(0f, 0f, 2f);
            }
            else if (token.Symbol == "*" || token.Symbol == "x" || token.Symbol == "×")
            {
                friend.localRotation = Quaternion.Euler(0f, 0f, -3f);
            }

            return friend;
        }

        private void UpdateSlotView(int slotIndex, SlotView slot)
        {
            var poses = slotStickPoses[slotIndex];
            var stickCount = poses.Count;
            var rawSymbol = OnePlusOneMinusOneRules.RecognizeToken(poses);
            var allowed = !string.IsNullOrEmpty(slotSymbols[slotIndex]);
            var partial = IsPartialTwoStickInProgress(slotIndex);
            slot.RecognitionText.text = stickCount <= 0 ? string.Empty :
                partial ? "shape" :
                string.IsNullOrEmpty(rawSymbol) ? "?" :
                rawSymbol;
            slot.RecognitionText.gameObject.SetActive(stickCount > 0);
            slot.RecognitionText.color = stickCount > 0 && !allowed && !partial ? FailureColor : SketchPalette.MutedInk;
            slot.HintText.text = "drop";
            slot.HintText.gameObject.SetActive(stickCount <= 0);
            RebuildSlotSticks(slot.SticksRoot, slotIndex);
            ApplySlotBackground(slotIndex, slot);
        }

        private void ApplySlotBackground(int slotIndex, SlotView slot)
        {
            var image = slot.Root.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var allowed = !string.IsNullOrEmpty(slotSymbols[slotIndex]);
            var color = !allowed
                ? SlotLockedColor
                : Color.Lerp(SlotLockedColor, OnePlusOneMinusOneRules.GetToken(slotSymbols[slotIndex]).DisplayColor, 0.22f);
            if (slotIndex == dragHoverSlotIndex)
            {
                color = Color.Lerp(color, SlotHoverColor, 0.58f);
            }

            image.color = color;
        }

        private void RebuildSlotSticks(RectTransform parent, int slotIndex)
        {
            ClearChildren(parent);
            var poses = slotStickPoses[slotIndex];
            var stickCount = poses.Count;
            if (stickCount <= 0)
            {
                return;
            }

            var slotRect = slotViews[slotIndex].Root.rect;
            var visualScale = Mathf.Clamp(Mathf.Min(slotRect.width / 142f, slotRect.height / 188f), 0.56f, 1f);
            var compact = visualScale < 0.82f || CurrentRound.SlotTypes.Length >= 8;
            var longStick = 116f * visualScale;
            var tallStick = 138f * visualScale;
            var thickStick = 40f * visualScale;
            var pairOffset = 30f * visualScale;
            var recognizedSymbol = slotSymbols[slotIndex];
            var visualPairOffset = stickCount >= 3 && recognizedSymbol == "111"
                ? 42f * visualScale
                : pairOffset;

            for (var i = 0; i < poses.Count; i++)
            {
                PoseToVisual(poses[i], longStick, tallStick, thickStick, visualPairOffset, out var size, out var offset, out var rotation);
                CreatePlacedStickView(parent, slotIndex, i, poses[i], size, offset, rotation, !UsesSharedFace(recognizedSymbol));
            }

            if (UsesSharedFace(recognizedSymbol))
            {
                CreateSharedSymbolFace(parent, FaceStyleForSymbol(recognizedSymbol), compact, slotIndex);
            }
        }

        private static void PoseToVisual(
            StickPose pose,
            float longStick,
            float tallStick,
            float thickStick,
            float pairOffset,
            out Vector2 size,
            out Vector2 offset,
            out float rotation)
        {
            size = new Vector2(thickStick, tallStick);
            offset = Vector2.zero;
            rotation = 0f;

            if (pose == StickPose.LeftVertical)
            {
                offset = new Vector2(-pairOffset, 0f);
            }
            else if (pose == StickPose.RightVertical)
            {
                offset = new Vector2(pairOffset, 0f);
            }
            else if (pose == StickPose.CenterHorizontal)
            {
                size = new Vector2(longStick, thickStick);
            }
            else if (pose == StickPose.TopHorizontal)
            {
                size = new Vector2(longStick, thickStick);
                offset = new Vector2(0f, pairOffset * 0.78f);
            }
            else if (pose == StickPose.BottomHorizontal)
            {
                size = new Vector2(longStick, thickStick);
                offset = new Vector2(0f, -pairOffset * 0.78f);
            }
            else if (pose == StickPose.CenterSlash)
            {
                size = new Vector2(longStick, thickStick);
                rotation = 33f;
            }
            else if (pose == StickPose.CenterBackslash)
            {
                size = new Vector2(longStick, thickStick);
                rotation = -33f;
            }
        }

        private void CreatePlacedStickView(
            RectTransform parent,
            int slotIndex,
            int stickIndex,
            StickPose pose,
            Vector2 size,
            Vector2 offset,
            float rotation,
            bool showFace)
        {
            var root = CreateRect($"Placed Stick {slotIndex + 1}-{stickIndex + 1}", parent);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = offset;
            root.sizeDelta = size;
            root.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var stickColor = ColorForPlacedStick(slotIndex, pose);

            CreateStickBody(root, stickColor, slotIndex * 37 + stickIndex * 11 + 71, true, false, true, showFace, slotSymbols[slotIndex]);
            root.gameObject.AddComponent<RawStickDragHandler>().Initialize(this, group, slotIndex, stickIndex);
        }

        private Color ColorForPlacedStick(int slotIndex, StickPose pose)
        {
            Color stickColor = new Color32(255, 253, 247, 255);
            if (!string.IsNullOrEmpty(slotSymbols[slotIndex]))
            {
                var symbol = slotSymbols[slotIndex];
                var tokenColor = BodyColorForSymbol(symbol);
                var mix = IsRepeatedOneSymbol(symbol) ? 0.1f : 0.68f;
                stickColor = Color.Lerp(stickColor, tokenColor, mix);
            }

            if (string.IsNullOrEmpty(slotSymbols[slotIndex]) && pose == StickPose.CenterSlash)
            {
                stickColor = Color.Lerp(stickColor, new Color32(178, 224, 145, 255), 0.18f);
            }

            return stickColor;
        }

        private static Color BodyColorForSymbol(string symbol)
        {
            if (symbol == "+")
            {
                return new Color32(98, 199, 255, 255);
            }

            if (symbol == "-")
            {
                return new Color32(255, 154, 166, 255);
            }

            if (symbol == "/")
            {
                return new Color32(207, 231, 84, 255);
            }

            if (symbol == "×" || symbol == "x" || symbol == "*")
            {
                return symbol == "*"
                    ? new Color32(124, 186, 248, 255)
                    : new Color32(48, 213, 188, 255);
            }

            if (symbol == "=")
            {
                return new Color32(255, 214, 98, 255);
            }

            return OnePlusOneMinusOneRules.GetToken(symbol).DisplayColor;
        }

        private static bool UsesSharedFace(string symbol)
        {
            return symbol == "+";
        }

        private void CreateSharedSymbolFace(RectTransform parent, FaceStyle style, bool compact, int seed)
        {
            var face = CreateRect("Shared Symbol Face", parent);
            face.anchorMin = new Vector2(0.5f, 0.5f);
            face.anchorMax = new Vector2(0.5f, 0.5f);
            face.pivot = new Vector2(0.5f, 0.5f);
            face.anchoredPosition = compact ? new Vector2(0f, -1f) : new Vector2(0f, -2f);
            face.sizeDelta = compact ? new Vector2(46f, 32f) : new Vector2(64f, 42f);
            face.SetAsLastSibling();

            var leftEye = CreateDot("Shared Left Eye", face, compact ? new Vector2(-9f, 6f) : new Vector2(-13f, 9f));
            var rightEye = CreateDot("Shared Right Eye", face, compact ? new Vector2(9f, 7f) : new Vector2(13f, 10f));
            leftEye.sizeDelta = compact ? new Vector2(5f, 5f) : new Vector2(6.2f, 6.2f);
            rightEye.sizeDelta = compact ? new Vector2(5.4f, 5.4f) : new Vector2(6.8f, 6.8f);
            ApplyEyeStyle(style, leftEye, rightEye);

            var mouth = CreateMouth(face, style);
            mouth.anchoredPosition = compact ? new Vector2(0f, -6f) : new Vector2(0f, -8f);
            mouth.sizeDelta = compact ? new Vector2(11f, 8.5f) : new Vector2(14f, 10f);
            ApplyMouthStyle(style, false, mouth);
            mouth.localRotation *= Quaternion.Euler(0f, 0f, 0f);
            friendFaces.Add(new FriendFaceView(face, leftEye, rightEye, mouth, seed * 43 + 191, ChatterWeightForStyle(style), TalkWeightForStyle(style)));
        }

        private void CreateStickBody(
            RectTransform root,
            Color color,
            int seed,
            bool raycastTarget,
            bool registerFace = true,
            bool registerMotion = true,
            bool showFace = true,
            string symbol = "")
        {
            var body = root.gameObject.AddComponent<RoundedStickGraphic>();
            body.BackgroundColor = color;
            body.HatchColor = HatchColorForSymbol(symbol);
            body.OutlineColor = SketchPalette.Ink;
            body.Inset = 5f;
            body.Spacing = 14f;
            body.HatchThickness = 2.1f;
            body.OutlineThickness = 3f;
            body.Jitter = 2.8f;
            body.Seed = seed * 29 + 3;
            body.raycastTarget = raycastTarget;
            var style = FaceStyleForSymbol(symbol);
            body.CornerRadiusRatio = CornerRadiusRatioForStyle(style);

            if (!showFace)
            {
                if (registerMotion)
                {
                    var motionWeight = Mathf.Clamp(root.sizeDelta.y / 116f, 0.55f, 1.15f);
                    root.gameObject.AddComponent<StickBeatMotion>().Initialize(root, root.anchoredPosition, root.localEulerAngles.z, seed, IdleBeatBpm, motionWeight * MotionWeightForStyle(style));
                }

                return;
            }

            var isHorizontalBody = root.sizeDelta.x > root.sizeDelta.y * 1.5f;
            var isTiltedBody = isHorizontalBody && Mathf.Abs(NormalizeAngle(root.localEulerAngles.z)) > 12f;
            var layout = GetFaceLayout(root.sizeDelta, style, isHorizontalBody, isTiltedBody, seed);
            var leftEye = CreateDot("Left Eye", root, layout.LeftEye);
            var rightEye = CreateDot("Right Eye", root, layout.RightEye);
            leftEye.sizeDelta = layout.LeftEyeSize;
            rightEye.sizeDelta = layout.RightEyeSize;
            ApplyEyeStyle(style, leftEye, rightEye);

            var mouth = CreateMouth(root, style);
            mouth.anchoredPosition = layout.MouthPosition;
            mouth.sizeDelta = layout.MouthSize;
            ApplyMouthStyle(style, isHorizontalBody, mouth);
            mouth.localRotation *= Quaternion.Euler(0f, 0f, layout.MouthRotation);
            if (style == FaceStyle.Equals)
            {
                mouth.localRotation = Quaternion.Euler(0f, 0f, seed % 2 == 0 ? -5f : 5f);
            }

            if (registerFace)
            {
                friendFaces.Add(new FriendFaceView(root, leftEye, rightEye, mouth, seed, ChatterWeightForStyle(style), TalkWeightForStyle(style)));
            }

            if (registerMotion)
            {
                var beatWeight = Mathf.Clamp(root.sizeDelta.y / 116f, 0.55f, 1.15f);
                root.gameObject.AddComponent<StickBeatMotion>().Initialize(root, root.anchoredPosition, root.localEulerAngles.z, seed, IdleBeatBpm, beatWeight * MotionWeightForStyle(style));
            }
        }

        private static float MotionWeightForStyle(FaceStyle style)
        {
            if (style == FaceStyle.One)
            {
                return 1.22f;
            }

            if (style == FaceStyle.Raw || style == FaceStyle.Number)
            {
                return 1f;
            }

            if (style == FaceStyle.Minus)
            {
                return 0.72f;
            }

            if (style == FaceStyle.Divide)
            {
                return 1.08f;
            }

            if (style == FaceStyle.Multiply)
            {
                return 1.25f;
            }

            if (style == FaceStyle.Star)
            {
                return 1.34f;
            }

            if (style == FaceStyle.Equals)
            {
                return 0.82f;
            }

            return 1f;
        }

        private static float ChatterWeightForStyle(FaceStyle style)
        {
            if (style == FaceStyle.One)
            {
                return 1.32f;
            }

            if (style == FaceStyle.Raw || style == FaceStyle.Number)
            {
                return 0.95f;
            }

            if (style == FaceStyle.Minus)
            {
                return 0.58f;
            }

            if (style == FaceStyle.Multiply)
            {
                return 1.34f;
            }

            if (style == FaceStyle.Star)
            {
                return 1.46f;
            }

            if (style == FaceStyle.Equals)
            {
                return 0.82f;
            }

            return 1f;
        }

        private static float TalkWeightForStyle(FaceStyle style)
        {
            if (style == FaceStyle.One)
            {
                return 1.42f;
            }

            if (style == FaceStyle.Raw || style == FaceStyle.Number)
            {
                return 0.95f;
            }

            if (style == FaceStyle.Minus)
            {
                return 0.72f;
            }

            if (style == FaceStyle.Multiply)
            {
                return 1.35f;
            }

            if (style == FaceStyle.Star)
            {
                return 1.48f;
            }

            return 1f;
        }

        private void BeginStickDrag(PointerEventData eventData, CanvasGroup sourceGroup, int sourceSlotIndex, int sourceStickIndex)
        {
            if (isAdvancing || sourceSlotIndex < 0 && RemainingSticks() <= 0)
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "No sticks to drag.";
                PlaySfx(SfxCue.Fail);
                return;
            }

            if (sourceSlotIndex >= 0 &&
                (sourceStickIndex < 0 || sourceStickIndex >= slotStickPoses[sourceSlotIndex].Count))
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "No stick there.";
                PlaySfx(SfxCue.Fail);
                return;
            }

            if (sourceSlotIndex < 0 &&
                (sourceStickIndex < 0 || sourceStickIndex >= bankStickPoses.Count))
            {
                feedbackText.color = FailureColor;
                feedbackText.text = "No stick there.";
                PlaySfx(SfxCue.Fail);
                return;
            }

            draggingSourceGroup = sourceGroup;
            draggingSourceSlotIndex = sourceSlotIndex;
            draggingSourceStickIndex = sourceStickIndex;
            draggingSourcePose = sourceSlotIndex >= 0 ? slotStickPoses[sourceSlotIndex][sourceStickIndex] : bankStickPoses[sourceStickIndex];
            draggingSourceGroup.alpha = 0.35f;

            PoseToVisual(draggingSourcePose, 108f, 132f, 36f, 25f, out var ghostSize, out _, out var ghostRotation);
            dragGhost = CreateRect("Dragging Stick", dragLayer);
            dragGhost.sizeDelta = ghostSize;
            dragGhost.localRotation = Quaternion.Euler(0f, 0f, ghostRotation);
            dragGhost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            CreateStickBody(dragGhost, new Color32(255, 253, 247, 245), 997, false, false, false);

            feedbackText.color = SketchPalette.MutedInk;
            feedbackText.text = "Drop into a box.";
            PlaySfx(SfxCue.Pick);
            MoveDragGhost(eventData);
        }

        private void MoveStickDrag(PointerEventData eventData)
        {
            if (dragGhost == null)
            {
                return;
            }

            MoveDragGhost(eventData);
            SetDragHoverSlot(FindSlotAtScreenPosition(eventData.position));
        }

        private void EndStickDrag(PointerEventData eventData)
        {
            if (dragGhost == null)
            {
                SetDragHoverSlot(-1);
                RestoreDraggedSource();
                return;
            }

            var sourceWasBank = draggingSourceSlotIndex < 0;
            var sourceSlotIndex = draggingSourceSlotIndex;
            var sourceStickIndex = draggingSourceStickIndex;
            var sourcePose = draggingSourcePose;
            var dropped = false;
            var targetSlotIndex = FindSlotAtScreenPosition(eventData.position);
            if (targetSlotIndex >= 0)
            {
                var targetPose = draggingSourcePose;
                dropped = draggingSourceSlotIndex >= 0
                    ? TryMoveStickToSlot(draggingSourceSlotIndex, draggingSourceStickIndex, targetSlotIndex, targetPose)
                    : TryAddStickToSlot(targetSlotIndex, true, targetPose);
            }

            Destroy(dragGhost.gameObject);
            dragGhost = null;
            SetDragHoverSlot(-1);
            RestoreDraggedSource();

            if (dropped && sourceWasBank)
            {
                RemoveBankStick(sourceStickIndex);
                RefreshUi();
                return;
            }

            if (!dropped)
            {
                if (!sourceWasBank)
                {
                    RemoveStickFromSlot(sourceSlotIndex, sourceStickIndex);
                    bankStickPoses.Add(NormalizeOutsidePose(sourcePose));
                    feedbackText.color = SketchPalette.MutedInk;
                    feedbackText.text = "Back outside. Tap to rotate.";
                    PlaySfx(SfxCue.Rotate);
                    RefreshUi();
                    return;
                }

                feedbackText.color = FailureColor;
                feedbackText.text = "Drop on a box.";
                PlaySfx(SfxCue.Fail);
                StartCoroutine(ShakeEquation());
                RefreshUi();
            }
        }

        private void RemoveBankStick(int bankIndex)
        {
            if (bankStickPoses.Count <= 0)
            {
                return;
            }

            var index = bankIndex >= 0 && bankIndex < bankStickPoses.Count ? bankIndex : bankStickPoses.Count - 1;
            bankStickPoses.RemoveAt(index);
        }

        private static StickPose NormalizeOutsidePose(StickPose pose)
        {
            if (pose == StickPose.CenterHorizontal ||
                pose == StickPose.TopHorizontal ||
                pose == StickPose.BottomHorizontal)
            {
                return StickPose.CenterHorizontal;
            }

            return pose == StickPose.CenterSlash || pose == StickPose.CenterBackslash ? pose : StickPose.CenterVertical;
        }

        private void MoveDragGhost(PointerEventData eventData)
        {
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, null, out var localPoint))
            {
                dragGhost.anchoredPosition = localPoint + DragGhostPointerOffset;
            }
        }

        private int FindSlotAtScreenPosition(Vector2 screenPosition)
        {
            for (var i = 0; i < slotViews.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slotViews[i].Root, screenPosition, null))
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetDragHoverSlot(int slotIndex)
        {
            if (dragHoverSlotIndex == slotIndex)
            {
                return;
            }

            var previous = dragHoverSlotIndex;
            dragHoverSlotIndex = slotIndex;
            if (previous >= 0 && previous < slotViews.Count)
            {
                ApplySlotBackground(previous, slotViews[previous]);
            }

            if (dragHoverSlotIndex >= 0 && dragHoverSlotIndex < slotViews.Count)
            {
                ApplySlotBackground(dragHoverSlotIndex, slotViews[dragHoverSlotIndex]);
            }
        }

        private void RestoreDraggedSource()
        {
            if (draggingSourceGroup != null)
            {
                draggingSourceGroup.alpha = 1f;
                draggingSourceGroup = null;
            }

            draggingSourceSlotIndex = -1;
            draggingSourceStickIndex = -1;
            draggingSourcePose = StickPose.CenterVertical;
        }

        private IEnumerator ShakeEquation()
        {
            if (isShaking)
            {
                yield break;
            }

            isShaking = true;
            var start = equationRow.anchoredPosition;
            for (var i = 0; i < 8; i++)
            {
                var x = i % 2 == 0 ? -16f : 16f;
                equationRow.anchoredPosition = start + new Vector2(x, 0f);
                yield return new WaitForSeconds(0.035f);
            }

            equationRow.anchoredPosition = start;
            isShaking = false;
        }

        private IEnumerator Bump(RectTransform target, float peakScale)
        {
            var start = target.localScale;
            var peak = Vector3.one * peakScale;
            for (var i = 0; i < 6; i++)
            {
                target.localScale = Vector3.Lerp(start, peak, (i + 1) / 6f);
                yield return null;
            }

            for (var i = 0; i < 8; i++)
            {
                target.localScale = Vector3.Lerp(peak, Vector3.one, (i + 1) / 8f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private IEnumerator BlinkRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(1.6f, 3.4f));
                if (friendFaces.Count <= 0)
                {
                    continue;
                }

                var face = friendFaces[UnityEngine.Random.Range(0, friendFaces.Count)];
                if (face.LeftEye == null || face.RightEye == null)
                {
                    continue;
                }

                var closed = new Vector3(1f, 0.18f, 1f);
                face.LeftEye.localScale = closed;
                face.RightEye.localScale = closed;
                yield return new WaitForSeconds(0.11f);
                face.LeftEye.localScale = Vector3.one;
                face.RightEye.localScale = Vector3.one;
            }
        }

        private void AnimateFriends()
        {
            var time = Time.unscaledTime;
            for (var i = 0; i < friendFaces.Count; i++)
            {
                var face = friendFaces[i];
                if (face.Root == null)
                {
                    continue;
                }

                var chatter = Mathf.Sin(time * 7.5f + face.Seed) * 1.1f * face.ChatterWeight;
                var talk = 1f + Mathf.Max(0f, Mathf.Sin(time * 10.5f + face.Seed * 0.73f)) * 0.18f * face.TalkWeight;
                face.Mouth.anchoredPosition = face.MouthBasePosition + new Vector2(0f, chatter);
                face.Mouth.localScale = new Vector3(1f, talk, 1f);
            }
        }

        private RectTransform CreatePanel(string name, Transform parent, Color fill, Color line, int seed)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = fill;

            var outline = CreateRect("Sketch Outline", rect);
            Stretch(outline);
            var outlineGraphic = outline.gameObject.AddComponent<SketchOutlineGraphic>();
            outlineGraphic.color = line;
            outlineGraphic.Thickness = 3f;
            outlineGraphic.Jitter = 4f;
            outlineGraphic.Strokes = 2;
            outlineGraphic.Seed = seed;
            outlineGraphic.raycastTarget = false;
            return rect;
        }

        private Button CreateCommandButton(string name, Transform parent, string label, Vector2 size)
        {
            var rect = CreatePanel(name, parent, ButtonPaperColor, SketchPalette.Ink, label.GetHashCode());
            rect.sizeDelta = size;
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.colors = ReleaseCommandButtonColors();
            button.onClick.AddListener(() => PlaySfx(SfxCue.Button));

            var text = CreateText("Label", rect, label, 29, FontStyle.Bold, SketchPalette.Ink, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static ColorBlock ReleaseCommandButtonColors()
        {
            var colors = SketchUiFactory.ButtonColors();
            colors.normalColor = ButtonPaperColor;
            colors.highlightedColor = new Color32(255, 251, 239, 255);
            colors.pressedColor = new Color32(244, 239, 225, 255);
            colors.disabledColor = DisabledButtonPaperColor;
            return colors;
        }

        private static void ApplyCommandButtonState(Button button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            button.colors = ReleaseCommandButtonColors();
            button.interactable = enabled;
            if (button.targetGraphic is Image image)
            {
                image.color = enabled ? ButtonPaperColor : DisabledButtonPaperColor;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = enabled ? SketchPalette.Ink : DisabledButtonInkColor;
            }
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.55f));
            text.resizeTextMaxSize = size;
            return text;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Color HatchColorForSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol) || IsRepeatedOneSymbol(symbol))
            {
                return StickHatchColor;
            }

            if (symbol == "+")
            {
                return new Color32(56, 154, 230, 145);
            }

            if (symbol == "-")
            {
                return new Color32(230, 118, 125, 120);
            }

            if (symbol == "/")
            {
                return new Color32(145, 181, 42, 130);
            }

            if (symbol == "×" || symbol == "x" || symbol == "*")
            {
                return symbol == "*"
                    ? new Color32(52, 126, 212, 138)
                    : new Color32(10, 142, 124, 152);
            }

            if (symbol == "=")
            {
                return new Color32(218, 170, 54, 120);
            }

            return StickHatchColor;
        }

        private static FaceStyle FaceStyleForSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol) || IsRepeatedOneSymbol(symbol))
            {
                return FaceStyle.One;
            }

            if (symbol == "+")
            {
                return FaceStyle.Plus;
            }

            if (symbol == "-")
            {
                return FaceStyle.Minus;
            }

            if (symbol == "/")
            {
                return FaceStyle.Divide;
            }

            if (symbol == "×" || symbol == "x")
            {
                return FaceStyle.Multiply;
            }

            if (symbol == "*")
            {
                return FaceStyle.Star;
            }

            if (symbol == "=")
            {
                return FaceStyle.Equals;
            }

            return FaceStyle.Number;
        }

        private static bool IsRepeatedOneSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return false;
            }

            for (var i = 0; i < symbol.Length; i++)
            {
                if (symbol[i] != '1')
                {
                    return false;
                }
            }

            return true;
        }

        private static float CornerRadiusRatioForStyle(FaceStyle style)
        {
            if (style == FaceStyle.One)
            {
                return 0.19f;
            }

            if (style == FaceStyle.Number)
            {
                return 0.18f;
            }

            if (style == FaceStyle.Minus || style == FaceStyle.Divide)
            {
                return 0.2f;
            }

            if (style == FaceStyle.Plus || style == FaceStyle.Multiply || style == FaceStyle.Star || style == FaceStyle.Equals)
            {
                return 0.17f;
            }

            return 0.2f;
        }

        private static FaceLayout GetFaceLayout(Vector2 bodySize, FaceStyle style, bool isHorizontalBody, bool isTiltedBody, int seed)
        {
            var mood = seed % 2 == 0 ? -1f : 1f;
            var wiggle = Mathf.Sin(seed * 2.37f);
            var faceX = isTiltedBody ? bodySize.x * 0.31f : isHorizontalBody ? bodySize.x * 0.24f : mood * 1.2f;
            var eyeY = isTiltedBody ? bodySize.y * 0.2f : isHorizontalBody ? bodySize.y * 0.12f : bodySize.y * 0.3f;
            var eyeGap = isHorizontalBody ? 8f : Mathf.Clamp(bodySize.x * 0.24f, 7.5f, 11f);
            var eyeSize = isHorizontalBody ? 5.4f : Mathf.Clamp(bodySize.x * 0.18f, 5.4f, 7.2f);
            var mouthOffset = isHorizontalBody ? 9f : Mathf.Clamp(bodySize.y * 0.15f, 16f, 21f);
            var mouthSize = isHorizontalBody
                ? new Vector2(19f, 11f)
                : new Vector2(Mathf.Clamp(bodySize.x * 0.48f, 17f, 22f), 11f);
            var leftEyeSize = new Vector2(eyeSize, eyeSize);
            var rightEyeSize = new Vector2(eyeSize, eyeSize);
            var mouthRotation = Mathf.Clamp(wiggle * 8f, -8f, 8f);

            if (style == FaceStyle.One)
            {
                faceX += wiggle * (isHorizontalBody ? 0.35f : 0.25f);
                eyeY += 4f + mood * (isHorizontalBody ? 0.1f : 0.25f);
                eyeGap = isHorizontalBody ? 8f : Mathf.Clamp(bodySize.x * 0.19f, 6.8f, 8f);
                leftEyeSize = Vector2.one * 4.8f;
                rightEyeSize = Vector2.one * 4.8f;
                mouthSize = isHorizontalBody ? new Vector2(8f, 7f) : new Vector2(8.5f, 7.5f);
                mouthOffset = isHorizontalBody ? 8f : Mathf.Clamp(bodySize.y * 0.12f, 13.5f, 15.5f);
                mouthRotation = 0f;
            }
            else if (style == FaceStyle.Raw || style == FaceStyle.Number)
            {
                faceX += wiggle * (isHorizontalBody ? 1f : 0.6f);
                eyeY += mood * (isHorizontalBody ? 0.2f : 0.5f);
                mouthRotation = wiggle * 4f;
            }
            else if (style == FaceStyle.Minus)
            {
                eyeY -= isHorizontalBody ? 1f : 2f;
                eyeGap += 1f;
                leftEyeSize -= Vector2.one * 0.5f;
                rightEyeSize -= Vector2.one * 0.5f;
                mouthSize = new Vector2(Mathf.Max(15f, mouthSize.x * 0.82f), Mathf.Max(7f, mouthSize.y * 0.78f));
                mouthRotation = mood * 3f;
            }
            else if (style == FaceStyle.Divide)
            {
                faceX += isHorizontalBody ? 10f : 2.5f;
                eyeY += isHorizontalBody ? 3f : 2.1f;
                eyeGap = isHorizontalBody ? 6.2f : Mathf.Max(5.8f, eyeGap - 2.2f);
                mouthSize = isHorizontalBody ? new Vector2(8f, 6.8f) : new Vector2(8.2f, 7f);
                leftEyeSize = Vector2.one * 4.4f;
                rightEyeSize = Vector2.one * 5.3f;
                mouthRotation = 18f;
            }
            else if (style == FaceStyle.Plus)
            {
                faceX -= 0.2f;
                eyeY += 3.2f;
                eyeGap = isHorizontalBody ? 8.8f : Mathf.Clamp(bodySize.x * 0.2f, 7.4f, 9f);
                leftEyeSize = Vector2.one * 5f;
                rightEyeSize = Vector2.one * 5f;
                mouthSize = isHorizontalBody ? new Vector2(10.5f, 8.5f) : new Vector2(11f, 8.8f);
                mouthRotation = 0f;
            }
            else if (style == FaceStyle.Multiply)
            {
                faceX += mood * (isHorizontalBody ? 12f : 3.5f);
                eyeY += mood * 2.4f;
                eyeGap = isHorizontalBody ? 5.8f : Mathf.Max(5.8f, eyeGap - 2.4f);
                leftEyeSize = new Vector2(mood > 0f ? 5.8f : 4.3f, mood > 0f ? 3.7f : 5.2f);
                rightEyeSize = new Vector2(mood > 0f ? 4.3f : 5.8f, mood > 0f ? 5.2f : 3.7f);
                mouthSize = isHorizontalBody ? new Vector2(8f, 5.8f) : new Vector2(8.4f, 6.2f);
                mouthRotation = mood * 14f;
            }
            else if (style == FaceStyle.Star)
            {
                faceX += mood * (isHorizontalBody ? 8f : 2.8f);
                eyeY += 2.8f + mood * 1.4f;
                eyeGap = isHorizontalBody ? 7.2f : Mathf.Max(6.4f, eyeGap - 1.1f);
                leftEyeSize = new Vector2(4.6f, 6f);
                rightEyeSize = new Vector2(6f, 4.6f);
                mouthSize = isHorizontalBody ? new Vector2(9.4f, 7.8f) : new Vector2(9.8f, 8.2f);
                mouthRotation = mood * -11f;
            }
            else if (style == FaceStyle.Equals)
            {
                faceX += mood * (isHorizontalBody ? 5f : 2f);
                eyeY += mood * 0.8f;
                eyeGap += 1.5f;
                mouthSize = new Vector2(Mathf.Max(15f, mouthSize.x * 0.86f), Mathf.Max(8f, mouthSize.y * 0.82f));
                mouthRotation = mood * 5f;
            }

            leftEyeSize = new Vector2(Mathf.Max(4.5f, leftEyeSize.x), Mathf.Max(4.5f, leftEyeSize.y));
            rightEyeSize = new Vector2(Mathf.Max(4.5f, rightEyeSize.x), Mathf.Max(4.5f, rightEyeSize.y));

            var leftEye = new Vector2(faceX - eyeGap, eyeY + mood * 0.6f);
            var rightEye = new Vector2(faceX + eyeGap, eyeY - mood * 0.4f);
            var mouthPosition = new Vector2(faceX, eyeY - mouthOffset);
            return new FaceLayout(leftEye, rightEye, leftEyeSize, rightEyeSize, mouthPosition, mouthSize, mouthRotation);
        }

        private static void ApplyFaceStyle(
            FaceStyle style,
            bool isHorizontalBody,
            ref float faceX,
            ref float eyeY,
            ref float eyeGap,
            ref float eyeSize)
        {
            if (style == FaceStyle.Plus)
            {
                eyeSize += isHorizontalBody ? 0.5f : 1f;
                eyeGap += 1f;
                eyeY += isHorizontalBody ? 1f : 2f;
            }
            else if (style == FaceStyle.Minus)
            {
                eyeSize -= 1f;
                eyeGap += 1f;
                eyeY -= isHorizontalBody ? 1f : 3f;
            }
            else if (style == FaceStyle.Divide)
            {
                eyeSize += 0.5f;
                faceX += isHorizontalBody ? 4f : 0f;
                eyeY += isHorizontalBody ? 2f : 1f;
            }
            else if (style == FaceStyle.Multiply)
            {
                eyeSize += isHorizontalBody ? 0.5f : 1f;
                eyeGap -= 1f;
            }
            else if (style == FaceStyle.Star)
            {
                eyeSize += isHorizontalBody ? 0.4f : 0.8f;
                eyeGap -= 0.5f;
                eyeY += 1.4f;
            }
            else if (style == FaceStyle.Equals)
            {
                eyeGap += 2f;
                eyeY -= 1f;
            }

            eyeSize = Mathf.Max(4.5f, eyeSize);
        }

        private static void ApplyEyeStyle(FaceStyle style, RectTransform leftEye, RectTransform rightEye)
        {
            if (style == FaceStyle.Minus)
            {
                leftEye.localScale = new Vector3(1.08f, 0.66f, 1f);
                rightEye.localScale = new Vector3(1.08f, 0.66f, 1f);
            }
            else if (style == FaceStyle.Divide)
            {
                leftEye.anchoredPosition += new Vector2(-1f, 2.4f);
                rightEye.anchoredPosition += new Vector2(1.2f, -1.6f);
                leftEye.localScale = new Vector3(0.82f, 1.18f, 1f);
                rightEye.localScale = new Vector3(1.18f, 0.82f, 1f);
            }
            else if (style == FaceStyle.Plus)
            {
                leftEye.anchoredPosition += new Vector2(0f, 1.2f);
                rightEye.anchoredPosition += new Vector2(0f, 1.2f);
                leftEye.localScale = new Vector3(1f, 1.24f, 1f);
                rightEye.localScale = new Vector3(1f, 1.24f, 1f);
            }
            else if (style == FaceStyle.Multiply)
            {
                leftEye.anchoredPosition += new Vector2(-1.2f, 0.6f);
                rightEye.anchoredPosition += new Vector2(1.1f, -0.8f);
                leftEye.localScale = new Vector3(1.22f, 0.68f, 1f);
                rightEye.localScale = new Vector3(0.76f, 1.18f, 1f);
            }
            else if (style == FaceStyle.Star)
            {
                leftEye.anchoredPosition += new Vector2(-0.4f, 1.4f);
                rightEye.anchoredPosition += new Vector2(1.4f, 0.1f);
                leftEye.localScale = new Vector3(0.86f, 1.22f, 1f);
                rightEye.localScale = new Vector3(1.2f, 0.86f, 1f);
            }
        }

        private static void ApplyMouthStyle(FaceStyle style, bool isHorizontalBody, RectTransform mouth)
        {
            if (style == FaceStyle.One)
            {
                mouth.anchoredPosition += new Vector2(0f, 0.2f);
                mouth.sizeDelta = new Vector2(Mathf.Max(7.5f, mouth.sizeDelta.x), Mathf.Max(6.8f, mouth.sizeDelta.y));
            }
            else if (style == FaceStyle.Raw || style == FaceStyle.Number)
            {
                mouth.anchoredPosition += new Vector2(isHorizontalBody ? 0f : 0.8f, 0.3f);
                mouth.sizeDelta = new Vector2(mouth.sizeDelta.x, Mathf.Max(7f, mouth.sizeDelta.y * 0.86f));
            }
            else if (style == FaceStyle.Plus)
            {
                mouth.sizeDelta = new Vector2(Mathf.Max(10f, mouth.sizeDelta.x), Mathf.Max(8f, mouth.sizeDelta.y));
                mouth.anchoredPosition += new Vector2(0f, -0.4f);
            }
            else if (style == FaceStyle.Minus)
            {
                mouth.sizeDelta = new Vector2(Mathf.Max(14f, mouth.sizeDelta.x * 0.72f), Mathf.Max(7f, mouth.sizeDelta.y * 0.72f));
                mouth.anchoredPosition += new Vector2(0f, 1f);
            }
            else if (style == FaceStyle.Divide)
            {
                mouth.sizeDelta = new Vector2(Mathf.Max(7.8f, mouth.sizeDelta.x), Mathf.Max(6.4f, mouth.sizeDelta.y));
                mouth.anchoredPosition += new Vector2(1f, 1f);
                mouth.localRotation = Quaternion.Euler(0f, 0f, 22f);
            }
            else if (style == FaceStyle.Multiply)
            {
                mouth.sizeDelta = new Vector2(Mathf.Max(7.5f, mouth.sizeDelta.x), Mathf.Max(5.6f, mouth.sizeDelta.y));
                mouth.anchoredPosition += new Vector2(0f, 1.1f);
            }
            else if (style == FaceStyle.Star)
            {
                mouth.sizeDelta = new Vector2(Mathf.Max(8.6f, mouth.sizeDelta.x), Mathf.Max(7.2f, mouth.sizeDelta.y));
                mouth.anchoredPosition += new Vector2(1.2f, 0.3f);
                mouth.localRotation = Quaternion.Euler(0f, 0f, -8f);
            }
            else if (style == FaceStyle.Equals)
            {
                mouth.sizeDelta = new Vector2(Mathf.Max(16f, mouth.sizeDelta.x * 0.8f), Mathf.Max(8f, mouth.sizeDelta.y * 0.78f));
            }
        }

        private RectTransform CreateDot(string name, RectTransform parent, Vector2 position)
        {
            var dot = CreateRect(name, parent);
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.anchoredPosition = position;
            dot.sizeDelta = new Vector2(7f, 7f);
            var image = dot.gameObject.AddComponent<EllipseGraphic>();
            image.color = EyeDotColor;
            image.raycastTarget = false;
            return dot;
        }

        private RectTransform CreateMouth(RectTransform parent, FaceStyle style)
        {
            var mouth = CreateRect("Mouth", parent);
            mouth.anchorMin = new Vector2(0.5f, 0.5f);
            mouth.anchorMax = new Vector2(0.5f, 0.5f);
            mouth.pivot = new Vector2(0.5f, 0.5f);
            mouth.anchoredPosition = new Vector2(0f, -22f);
            mouth.sizeDelta = new Vector2(22f, 12f);

            var outline = CreateRect("Mouth Outline", mouth);
            Stretch(outline);
            var mouthShape = MouthShapeForStyle(style);
            Graphic outlineImage = AddMouthGraphic(outline, mouthShape);
            outlineImage.color = FaceInkColor;
            outlineImage.raycastTarget = false;

            var fill = CreateRect("Mouth Fill", mouth);
            Stretch(fill, 2f, 2f, 2f, 2f);
            Graphic fillImage = AddMouthGraphic(fill, mouthShape);
            fillImage.color = WhitePaperColor;
            fillImage.raycastTarget = false;
            return mouth;
        }

        private static MouthShape MouthShapeForStyle(FaceStyle style)
        {
            if (style == FaceStyle.One || style == FaceStyle.Multiply)
            {
                return MouthShape.Box;
            }

            if (style == FaceStyle.Plus || style == FaceStyle.Divide || style == FaceStyle.Star)
            {
                return MouthShape.Triangle;
            }

            return MouthShape.Oval;
        }

        private static Graphic AddMouthGraphic(RectTransform rect, MouthShape shape)
        {
            if (shape == MouthShape.Box)
            {
                return rect.gameObject.AddComponent<Image>();
            }

            if (shape == MouthShape.Triangle)
            {
                var triangle = rect.gameObject.AddComponent<TriangleGraphic>();
                triangle.PointDown = true;
                return triangle;
            }

            return rect.gameObject.AddComponent<EllipseGraphic>();
        }

        private static void Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private readonly struct SlotView
        {
            public RectTransform Root { get; }
            public Button Button { get; }
            public Text RecognitionText { get; }
            public Text HintText { get; }
            public RectTransform SticksRoot { get; }

            public SlotView(RectTransform root, Button button, Text recognitionText, Text hintText, RectTransform sticksRoot)
            {
                Root = root;
                Button = button;
                RecognitionText = recognitionText;
                HintText = hintText;
                SticksRoot = sticksRoot;
            }
        }

        private sealed class RawStickDragHandler : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            private OnePlusOneMinusOneController controller;
            private CanvasGroup sourceGroup;
            private bool didDrag;

            public void Initialize(OnePlusOneMinusOneController owner, CanvasGroup group, int sourceSlotIndex, int sourceStickIndex)
            {
                controller = owner;
                sourceGroup = group;
                SourceSlotIndex = sourceSlotIndex;
                SourceStickIndex = sourceStickIndex;
            }

            private int SourceSlotIndex { get; set; }
            private int SourceStickIndex { get; set; }

            public void OnPointerDown(PointerEventData eventData)
            {
                didDrag = false;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                didDrag = false;
                controller?.BeginStickDrag(eventData, sourceGroup, SourceSlotIndex, SourceStickIndex);
            }

            public void OnDrag(PointerEventData eventData)
            {
                didDrag = true;
                controller?.MoveStickDrag(eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                controller?.EndStickDrag(eventData);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (didDrag)
                {
                    return;
                }

                if (SourceSlotIndex >= 0)
                {
                    controller?.TapDroppedStick(SourceSlotIndex, SourceStickIndex);
                    return;
                }

                controller?.TapBankStick(SourceStickIndex);
            }
        }

        private sealed class StickBeatMotion : MonoBehaviour
        {
            private RectTransform rect;
            private Vector2 basePosition;
            private float baseRotation;
            private int seed;
            private float bpm;
            private float weight;

            public void Initialize(RectTransform target, Vector2 position, float rotation, int motionSeed, float beatsPerMinute, float motionWeight)
            {
                rect = target;
                basePosition = position;
                baseRotation = rotation;
                seed = motionSeed;
                bpm = beatsPerMinute;
                weight = motionWeight;
            }

            private void Update()
            {
                if (rect == null)
                {
                    return;
                }

                var beat = Time.unscaledTime * bpm / 60f;
                var phase = seed * 0.173f;
                var main = Mathf.Sin((beat + phase) * Mathf.PI * 2f);
                var bounce = Mathf.Max(0f, main);
                var sway = Mathf.Sin((beat * 0.5f + phase) * Mathf.PI * 2f);
                rect.anchoredPosition = basePosition + new Vector2(sway * 0.8f * weight, bounce * 2.4f * weight);
                rect.localRotation = Quaternion.Euler(0f, 0f, baseRotation + sway * 1.8f * weight);
                rect.localScale = new Vector3(1f + bounce * 0.018f * weight, 1f - bounce * 0.012f * weight, 1f);
            }
        }

        private readonly struct FriendFaceView
        {
            public RectTransform Root { get; }
            public RectTransform LeftEye { get; }
            public RectTransform RightEye { get; }
            public RectTransform Mouth { get; }
            public Vector2 MouthBasePosition { get; }
            public int Seed { get; }
            public float ChatterWeight { get; }
            public float TalkWeight { get; }

            public FriendFaceView(
                RectTransform root,
                RectTransform leftEye,
                RectTransform rightEye,
                RectTransform mouth,
                int seed,
                float chatterWeight = 1f,
                float talkWeight = 1f)
            {
                Root = root;
                LeftEye = leftEye;
                RightEye = rightEye;
                Mouth = mouth;
                MouthBasePosition = mouth.anchoredPosition;
                Seed = seed;
                ChatterWeight = chatterWeight;
                TalkWeight = talkWeight;
            }
        }
    }
}
