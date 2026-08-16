using System;
using System.Collections;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Sitting
{
    public sealed class SittingController : MonoBehaviour
    {
        private const string BestSecondsKey = "mannlab.sitting.best_seconds";
        private static readonly Color DeskTopColor = new Color32(183, 120, 72, 255);
        private static readonly Color DeskFrontColor = new Color32(226, 170, 94, 255);
        private static readonly Color DeskLegColor = new Color32(106, 70, 49, 255);
        private static readonly Color MonitorColor = new Color32(132, 205, 218, 255);
        private static readonly Color ChairColor = new Color32(75, 128, 185, 255);
        private static readonly Color PlayerShirtColor = new Color32(83, 153, 196, 255);
        private static readonly Color PlayerPantsColor = new Color32(78, 84, 96, 255);
        private static readonly Color SkinColor = new Color32(247, 183, 126, 255);
        private static readonly Color VisitorColor = new Color32(217, 74, 67, 255);
        private static readonly Color VisitorGlowColor = new Color32(255, 214, 93, 255);
        private static readonly Color HealthGoodColor = new Color32(75, 178, 99, 255);
        private static readonly Color HealthLowColor = new Color32(236, 143, 49, 255);

        private readonly System.Random random = new System.Random(Environment.TickCount);
        private AudioSource audioSource;
        private AudioClip sitClip;
        private AudioClip caughtClip;
        private AudioClip exhaustedClip;
        private Text timeText;
        private Text bestText;
        private Text statusText;
        private Text riskText;
        private Image healthFill;
        private RectTransform characterRoot;
        private RectTransform characterBody;
        private RectTransform characterHead;
        private RectTransform chairSeat;
        private RectTransform visitorRoot;
        private Image visitorImage;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultScoreText;
        private SittingGameState state;
        private VisitorPhase visitorPhase;
        private float health;
        private float runSeconds;
        private float bestSeconds;
        private float nextVisitorAt;
        private float visitorPhaseEndsAt;
        private float resultAt;
        private float visualPulse;
        private bool wasSitting;
        private SittingGameState lastEndState;

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

            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (state == SittingGameState.GameOver)
            {
                return;
            }

            if (state == SittingGameState.Caught || state == SittingGameState.Exhausted)
            {
                AnimateFailState();
                if (Time.time >= resultAt)
                {
                    ShowResult();
                }

                return;
            }

            var sittingInput = IsPressingPlayArea();
            state = sittingInput ? SittingGameState.Sitting : SittingGameState.Standing;
            if (sittingInput && !wasSitting)
            {
                PlayClip(sitClip);
            }

            wasSitting = sittingInput;
            runSeconds += Time.deltaTime;
            health = SittingBalance.TickHealth(health, sittingInput, Time.deltaTime);
            UpdateVisitor();

            if (SittingBalance.ShouldCatch(sittingInput, visitorPhase))
            {
                EndRun(SittingGameState.Caught);
                return;
            }

            if (SittingBalance.IsExhausted(health))
            {
                EndRun(SittingGameState.Exhausted);
                return;
            }

            UpdateHud();
            UpdateCharacterVisual(sittingInput);
        }

        private void StartRun()
        {
            StopAllCoroutines();
            state = SittingGameState.Standing;
            visitorPhase = VisitorPhase.Empty;
            health = SittingBalance.MaxHealth;
            runSeconds = 0f;
            wasSitting = false;
            visualPulse = 0f;
            nextVisitorAt = Time.time + SittingBalance.NextVisitorGap(random);
            visitorPhaseEndsAt = 0f;
            resultPanel.SetActive(false);
            visitorRoot.gameObject.SetActive(false);
            UpdateHud();
            UpdateCharacterVisual(false);
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
                visitorPhaseEndsAt = Time.time + SittingBalance.VisitorWarningSeconds;
                return;
            }

            if (visitorPhase == VisitorPhase.Warning)
            {
                if (Time.time < visitorPhaseEndsAt)
                {
                    return;
                }

                visitorPhase = VisitorPhase.Passing;
                visitorPhaseEndsAt = Time.time + SittingBalance.VisitorPassingSeconds;
                visitorRoot.gameObject.SetActive(true);
            }

            if (visitorPhase != VisitorPhase.Passing)
            {
                return;
            }

            var progress = 1f - Mathf.Clamp01((visitorPhaseEndsAt - Time.time) / SittingBalance.VisitorPassingSeconds);
            visitorRoot.anchorMin = new Vector2(-0.20f + progress * 1.40f, 0.52f);
            visitorRoot.anchorMax = new Vector2(0.04f + progress * 1.40f, 0.88f);
            visitorImage.color = WithAlpha(VisitorGlowColor, 0.24f);

            if (Time.time < visitorPhaseEndsAt)
            {
                return;
            }

            visitorRoot.gameObject.SetActive(false);
            visitorPhase = VisitorPhase.Empty;
            nextVisitorAt = Time.time + SittingBalance.NextVisitorGap(random);
        }

        private void EndRun(SittingGameState finalState)
        {
            state = finalState;
            lastEndState = finalState;
            resultAt = Time.time + SittingBalance.ResultDelaySeconds;
            visitorRoot.gameObject.SetActive(finalState == SittingGameState.Caught);
            PlayClip(finalState == SittingGameState.Caught ? caughtClip : exhaustedClip);

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
            if (state == SittingGameState.Caught)
            {
                var shake = Mathf.Sin(visualPulse) * 24f;
                characterRoot.anchoredPosition = new Vector2(shake, -80f);
                characterBody.localRotation = Quaternion.Euler(0f, 0f, -8f + Mathf.Sin(visualPulse * 0.8f) * 7f);
                characterHead.localScale = Vector3.one * 1.14f;
                statusText.text = "Caught";
                statusText.color = SketchPalette.WrongMarker;
                riskText.text = "Boss spotted you";
                riskText.color = SketchPalette.WrongMarker;
                return;
            }

            characterRoot.anchoredPosition = new Vector2(0f, -190f);
            characterBody.localRotation = Quaternion.Euler(0f, 0f, 12f);
            characterHead.localScale = Vector3.one * 0.94f;
            statusText.text = "Exhausted";
            statusText.color = SketchPalette.WarningAmber;
            riskText.text = "Stamina empty";
            riskText.color = SketchPalette.WarningAmber;
        }

        private void ShowResult()
        {
            state = SittingGameState.GameOver;
            resultTitleText.text = lastEndState == SittingGameState.Exhausted ? "Collapsed" : "Caught Sitting";
            resultScoreText.text = $"Time {FormatTime(runSeconds)}\nBest {FormatTime(bestSeconds)}";
            resultPanel.SetActive(true);
        }

        private void UpdateHud()
        {
            timeText.text = $"Time {FormatTime(runSeconds)}";
            bestText.text = $"Best {FormatTime(bestSeconds)}";
            healthFill.fillAmount = Mathf.Clamp01(health / SittingBalance.MaxHealth);
            healthFill.color = health < 28f ? HealthLowColor : HealthGoodColor;

            if (state == SittingGameState.Sitting)
            {
                statusText.text = "Sitting";
                statusText.color = SketchPalette.FocusBlue;
            }
            else if (state == SittingGameState.Standing)
            {
                statusText.text = "Standing";
                statusText.color = SketchPalette.Ink;
            }

            switch (visitorPhase)
            {
                case VisitorPhase.Warning:
                    riskText.text = "Footsteps";
                    riskText.color = SketchPalette.WarningAmber;
                    break;
                case VisitorPhase.Passing:
                    riskText.text = "Passing";
                    riskText.color = SketchPalette.WrongMarker;
                    break;
                default:
                    riskText.text = "Clear";
                    riskText.color = SketchPalette.MutedInk;
                    break;
            }
        }

        private void UpdateCharacterVisual(bool sitting)
        {
            var targetY = sitting ? -154f : -78f;
            characterRoot.anchoredPosition = Vector2.Lerp(characterRoot.anchoredPosition, new Vector2(0f, targetY), 0.25f);
            characterBody.localRotation = Quaternion.Lerp(characterBody.localRotation, Quaternion.Euler(0f, 0f, sitting ? -2f : 0f), 0.2f);
            characterHead.localScale = Vector3.Lerp(characterHead.localScale, Vector3.one * (sitting ? 0.98f : 1f), 0.2f);
            chairSeat.anchoredPosition = Vector2.Lerp(chairSeat.anchoredPosition, new Vector2(0f, sitting ? -192f : -214f), 0.2f);
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

        private static void CreateBackground(Transform parent)
        {
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
            SetAnchor(stage.GetComponent<RectTransform>(), 0f, 0.11f, 1f, 0.83f, 44f, 0f, -44f, 0f);

            var wallLine = CreatePanel(stage.transform, "Desk Back Line", SketchPalette.Ink);
            SetAnchor(wallLine.GetComponent<RectTransform>(), 0.04f, 0.64f, 0.96f, 0.655f);

            var window = CreatePanel(stage.transform, "Office Window", WithAlpha(MonitorColor, 0.24f));
            SetAnchor(window.GetComponent<RectTransform>(), 0.08f, 0.68f, 0.30f, 0.91f);
            AddSketchOutline(window.transform);

            var plant = CreatePanel(stage.transform, "Desk Plant", new Color32(106, 159, 92, 255));
            SetAnchor(plant.GetComponent<RectTransform>(), 0.73f, 0.66f, 0.80f, 0.78f);
            AddSketchOutline(plant.transform);

            var desk = CreatePanel(stage.transform, "Desk Front", DeskFrontColor);
            SetAnchor(desk.GetComponent<RectTransform>(), 0.13f, 0.40f, 0.87f, 0.61f);
            AddSketchOutline(desk.transform);

            var deskTop = CreatePanel(stage.transform, "Desk Top", DeskTopColor);
            SetAnchor(deskTop.GetComponent<RectTransform>(), 0.09f, 0.58f, 0.91f, 0.65f);
            AddSketchOutline(deskTop.transform);

            var monitor = CreatePanel(stage.transform, "Monitor", MonitorColor);
            SetAnchor(monitor.GetComponent<RectTransform>(), 0.38f, 0.59f, 0.62f, 0.76f);
            AddSketchOutline(monitor.transform);

            var keyboard = CreatePanel(stage.transform, "Keyboard", SketchPalette.WarmHighlight);
            SetAnchor(keyboard.GetComponent<RectTransform>(), 0.37f, 0.50f, 0.63f, 0.54f);
            AddSketchOutline(keyboard.transform);

            var deskLegLeft = CreatePanel(stage.transform, "Desk Leg Left", DeskLegColor);
            SetAnchor(deskLegLeft.GetComponent<RectTransform>(), 0.22f, 0.25f, 0.26f, 0.40f);

            var deskLegRight = CreatePanel(stage.transform, "Desk Leg Right", DeskLegColor);
            SetAnchor(deskLegRight.GetComponent<RectTransform>(), 0.74f, 0.25f, 0.78f, 0.40f);

            chairSeat = CreatePanel(stage.transform, "Chair Seat", ChairColor).GetComponent<RectTransform>();
            chairSeat.anchorMin = new Vector2(0.5f, 0.23f);
            chairSeat.anchorMax = new Vector2(0.5f, 0.23f);
            chairSeat.pivot = new Vector2(0.5f, 0.5f);
            chairSeat.sizeDelta = new Vector2(360f, 112f);
            chairSeat.anchoredPosition = new Vector2(0f, -214f);
            AddSketchOutline(chairSeat.transform);

            CreateCharacter(stage.transform);
            CreateVisitor(stage.transform);
        }

        private void CreateCharacter(Transform parent)
        {
            var root = new GameObject("Player Back", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            characterRoot = root.GetComponent<RectTransform>();
            characterRoot.anchorMin = new Vector2(0.5f, 0.28f);
            characterRoot.anchorMax = new Vector2(0.5f, 0.28f);
            characterRoot.pivot = new Vector2(0.5f, 0f);
            characterRoot.sizeDelta = new Vector2(320f, 520f);

            var legs = CreatePanel(root.transform, "Legs", PlayerPantsColor);
            SetAnchor(legs.GetComponent<RectTransform>(), 0.30f, 0.00f, 0.70f, 0.32f);
            AddSketchOutline(legs.transform);

            var body = CreatePanel(root.transform, "Body", PlayerShirtColor);
            characterBody = body.GetComponent<RectTransform>();
            SetAnchor(characterBody, 0.20f, 0.26f, 0.80f, 0.73f);
            AddSketchOutline(body.transform);

            var neck = CreatePanel(root.transform, "Neck", SkinColor);
            SetAnchor(neck.GetComponent<RectTransform>(), 0.44f, 0.69f, 0.56f, 0.80f);
            AddSketchOutline(neck.transform);

            var head = CreatePanel(root.transform, "Head", SkinColor);
            characterHead = head.GetComponent<RectTransform>();
            characterHead.anchorMin = new Vector2(0.5f, 0.78f);
            characterHead.anchorMax = new Vector2(0.5f, 0.78f);
            characterHead.pivot = new Vector2(0.5f, 0.5f);
            characterHead.sizeDelta = new Vector2(126f, 126f);
            AddSketchOutline(head.transform);

            var hair = CreatePanel(root.transform, "Hair", SketchPalette.Ink);
            SetAnchor(hair.GetComponent<RectTransform>(), 0.29f, 0.84f, 0.71f, 0.94f);
        }

        private void CreateVisitor(Transform parent)
        {
            var visitor = new GameObject("Passing Person", typeof(RectTransform), typeof(Image));
            visitor.transform.SetParent(parent, false);
            visitorRoot = visitor.GetComponent<RectTransform>();
            visitorImage = visitor.GetComponent<Image>();
            visitorImage.color = WithAlpha(VisitorGlowColor, 0.24f);

            var head = CreatePanel(visitor.transform, "Visitor Head", VisitorColor);
            SetAnchor(head.GetComponent<RectTransform>(), 0.32f, 0.67f, 0.68f, 0.98f);
            AddSketchOutline(head.transform);

            var torso = CreatePanel(visitor.transform, "Visitor Body", VisitorColor);
            SetAnchor(torso.GetComponent<RectTransform>(), 0.18f, 0.18f, 0.82f, 0.70f);
            AddSketchOutline(torso.transform);

            var badge = CreatePanel(visitor.transform, "Visitor Badge", SketchPalette.WarmHighlight);
            SetAnchor(badge.GetComponent<RectTransform>(), 0.58f, 0.48f, 0.75f, 0.60f);
            AddSketchOutline(badge.transform);

            var legs = CreatePanel(visitor.transform, "Visitor Legs", DeskLegColor);
            SetAnchor(legs.GetComponent<RectTransform>(), 0.30f, 0.00f, 0.70f, 0.24f);
            AddSketchOutline(legs.transform);
            visitor.SetActive(false);
        }

        private void CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            SetAnchor(header.GetComponent<RectTransform>(), 0f, 0.84f, 1f, 1f, 42f, -28f, -42f, -20f);

            timeText = CreateText(header.transform, "Time 0:00", 42, TextAnchor.MiddleLeft);
            SetAnchor(timeText.GetComponent<RectTransform>(), 0f, 0.52f, 0.54f, 1f);

            bestText = CreateText(header.transform, "Best 0:00", 30, TextAnchor.MiddleRight);
            bestText.color = SketchPalette.MutedInk;
            SetAnchor(bestText.GetComponent<RectTransform>(), 0.54f, 0.52f, 1f, 1f);

            statusText = CreateText(header.transform, "Standing", 34, TextAnchor.MiddleLeft);
            SetAnchor(statusText.GetComponent<RectTransform>(), 0f, 0.06f, 0.42f, 0.48f);

            riskText = CreateText(header.transform, "Clear", 34, TextAnchor.MiddleRight);
            SetAnchor(riskText.GetComponent<RectTransform>(), 0.58f, 0.06f, 1f, 0.48f);

            var track = CreatePanel(header.transform, "Health Track", SketchPalette.TilePaper);
            SetAnchor(track.GetComponent<RectTransform>(), 0.24f, 0.14f, 0.76f, 0.40f);
            AddSketchOutline(track.transform);

            var fill = CreatePanel(track.transform, "Health Fill", SketchPalette.CorrectMarker);
            Stretch(fill.GetComponent<RectTransform>(), new Vector2(6f, 6f), new Vector2(-6f, -6f));
            healthFill = fill.GetComponent<Image>();
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = 0;
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
            var outlineObject = new GameObject("Sketch Outline", typeof(RectTransform), typeof(Image));
            outlineObject.transform.SetParent(target, false);
            Stretch(outlineObject.GetComponent<RectTransform>(), new Vector2(-4f, -4f), new Vector2(4f, 4f));
            var outlineGraphic = outlineObject.GetComponent<Image>();
            outlineGraphic.color = SketchPalette.Ink;
            outlineGraphic.raycastTarget = false;
            outlineObject.transform.SetAsFirstSibling();
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
