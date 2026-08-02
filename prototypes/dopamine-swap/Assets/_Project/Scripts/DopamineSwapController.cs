using System;
using System.Collections;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.DopamineSwap
{
    public sealed class DopamineSwapController : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.dopamine_swap.best_score";
        private const float RoundResultSeconds = 0.58f;

        private readonly Button[] cardButtons = new Button[DopamineRoundRules.CardCount];
        private readonly Image[] cardImages = new Image[DopamineRoundRules.CardCount];
        private readonly Text[] cardScoreTexts = new Text[DopamineRoundRules.CardCount];

        private System.Random random;
        private AudioSource audioSource;
        private AudioClip selectClip;
        private AudioClip winClip;
        private AudioClip failClip;
        private Text scoreText;
        private Text bestText;
        private Text roundText;
        private Text phaseText;
        private Text opponentLabelText;
        private Text opponentValueText;
        private Text timerText;
        private Image timerFillImage;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultScoreText;
        private DopamineRound currentRound;
        private int round = 1;
        private int score;
        private int bestScore;
        private float roundEndsAt;
        private bool awaitingChoice;
        private bool runEnded;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            random = new System.Random(Environment.TickCount);
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            selectClip = CreateToneClip("Card Select", 760f, 0.045f, 0.36f);
            winClip = CreateToneClip("Win", 1120f, 0.08f, 0.42f);
            failClip = CreateToneClip("Fail", 180f, 0.16f, 0.48f);

            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (!awaitingChoice || runEnded)
            {
                return;
            }

            var remaining = Mathf.Max(0f, roundEndsAt - Time.time);
            UpdateTimer(remaining, currentRound.TimeLimitSeconds);
            if (remaining <= 0f)
            {
                EndRun("Too Slow", 0);
            }
        }

        private void StartRun()
        {
            StopAllCoroutines();
            round = 1;
            score = 0;
            runEnded = false;
            awaitingChoice = false;
            resultPanel.SetActive(false);
            BeginRound();
        }

        private void BeginRound()
        {
            currentRound = DopamineRoundRules.CreateRound(round, random);
            awaitingChoice = true;
            roundEndsAt = Time.time + currentRound.TimeLimitSeconds;

            UpdateHeader();
            phaseText.text = "Pick Higher";
            opponentLabelText.text = currentRound.RevealsOpponentScore ? "Opponent" : "Opponent Range";
            opponentValueText.text = currentRound.OpponentPrompt;
            timerText.text = $"{currentRound.TimeLimitSeconds:0.0}s";
            UpdateTimer(currentRound.TimeLimitSeconds, currentRound.TimeLimitSeconds);

            for (var i = 0; i < cardButtons.Length; i++)
            {
                var scoreValue = currentRound.PlayerCards[i];
                var buttonIndex = i;
                cardScoreTexts[i].text = scoreValue.ToString();
                cardImages[i].color = SketchPalette.TilePaper;
                cardButtons[i].interactable = true;
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => ChooseCard(buttonIndex));
            }
        }

        private void ChooseCard(int cardIndex)
        {
            if (!awaitingChoice || runEnded)
            {
                return;
            }

            awaitingChoice = false;
            PlayClip(selectClip);
            SetCardsInteractable(false);
            StartCoroutine(ResolveChoice(cardIndex));
        }

        private IEnumerator ResolveChoice(int cardIndex)
        {
            var selectedScore = currentRound.PlayerCards[cardIndex];
            var won = selectedScore > currentRound.OpponentScore;
            opponentLabelText.text = "Opponent";
            opponentValueText.text = currentRound.OpponentScore.ToString();
            cardImages[cardIndex].color = won ? SketchPalette.CorrectMarker : SketchPalette.WrongMarker;

            if (!won)
            {
                yield return new WaitForSeconds(0.18f);
                EndRun("Too Low", selectedScore);
                yield break;
            }

            score += selectedScore;
            phaseText.text = $"+{selectedScore}";
            PlayClip(winClip);
            UpdateHeader();
            yield return new WaitForSeconds(RoundResultSeconds);

            if (!runEnded)
            {
                round++;
                BeginRound();
            }
        }

        private void EndRun(string reason, int selectedScore)
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            awaitingChoice = false;
            SetCardsInteractable(false);
            PlayClip(failClip);

            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }

            UpdateHeader();
            opponentLabelText.text = "Opponent";
            opponentValueText.text = currentRound.OpponentScore.ToString();
            phaseText.text = reason;

            var selectedLine = selectedScore > 0 ? $"You {selectedScore}\n" : string.Empty;
            resultTitleText.text = reason;
            resultScoreText.text = $"{selectedLine}Score {score}\nBest {bestScore}";
            resultPanel.SetActive(true);
        }

        private void SetCardsInteractable(bool interactable)
        {
            for (var i = 0; i < cardButtons.Length; i++)
            {
                cardButtons[i].interactable = interactable;
            }
        }

        private void UpdateHeader()
        {
            scoreText.text = $"Score {score}";
            bestText.text = $"Best {bestScore}";
            roundText.text = $"Round {round}";
        }

        private void UpdateTimer(float remaining, float total)
        {
            timerText.text = $"{remaining:0.0}s";
            timerFillImage.fillAmount = total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
            timerFillImage.color = remaining <= 1f ? SketchPalette.WarningAmber : SketchPalette.FocusBlue;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            CreateHeader(canvas.transform);
            CreateOpponentPanel(canvas.transform);
            CreateCardRow(canvas.transform);
            CreateTimer(canvas.transform);
            CreateResultPanel(canvas.transform);
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
        }

        private void CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            var rect = header.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(36f, -132f);
            rect.offsetMax = new Vector2(-36f, -28f);

            scoreText = CreateText(header.transform, "Score 0", 44, TextAnchor.MiddleLeft);
            SetAnchor(scoreText.GetComponent<RectTransform>(), 0f, 0f, 0.36f, 1f);

            roundText = CreateText(header.transform, "Round 1", 34, TextAnchor.MiddleCenter);
            SetAnchor(roundText.GetComponent<RectTransform>(), 0.32f, 0f, 0.68f, 1f);

            bestText = CreateText(header.transform, "Best 0", 34, TextAnchor.MiddleRight);
            SetAnchor(bestText.GetComponent<RectTransform>(), 0.64f, 0f, 1f, 1f);
        }

        private void CreateOpponentPanel(Transform parent)
        {
            var panel = new GameObject("Opponent Panel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            SetAnchor(panel.GetComponent<RectTransform>(), 0f, 0.48f, 1f, 0.88f, 48f, 0f, -48f, 0f);

            phaseText = CreateText(panel.transform, "Pick Higher", 72, TextAnchor.MiddleCenter);
            SetAnchor(phaseText.GetComponent<RectTransform>(), 0f, 0.72f, 1f, 1f);

            opponentLabelText = CreateText(panel.transform, "Opponent", 34, TextAnchor.MiddleCenter);
            opponentLabelText.color = SketchPalette.MutedInk;
            SetAnchor(opponentLabelText.GetComponent<RectTransform>(), 0f, 0.56f, 1f, 0.72f);

            opponentValueText = CreateText(panel.transform, "62", 160, TextAnchor.MiddleCenter);
            SetAnchor(opponentValueText.GetComponent<RectTransform>(), 0f, 0.08f, 1f, 0.58f);
        }

        private void CreateCardRow(Transform parent)
        {
            var row = new GameObject("Card Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            SetAnchor(row.GetComponent<RectTransform>(), 0f, 0.14f, 1f, 0.44f, 44f, 0f, -44f, 0f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 18f;

            for (var i = 0; i < cardButtons.Length; i++)
            {
                var card = new GameObject($"Card {i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                card.transform.SetParent(row.transform, false);
                card.GetComponent<Image>().color = SketchPalette.TilePaper;
                AddSketchOutline(card.transform);

                var layoutElement = card.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = 300f;
                layoutElement.preferredHeight = 420f;

                var button = card.GetComponent<Button>();
                button.colors = SketchUiFactory.ButtonColors();

                var label = CreateText(card.transform, "100", 98, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                Stretch(label.GetComponent<RectTransform>(), new Vector2(12f, 12f), new Vector2(-12f, -12f));

                cardButtons[i] = button;
                cardImages[i] = card.GetComponent<Image>();
                cardScoreTexts[i] = label;
            }
        }

        private void CreateTimer(Transform parent)
        {
            var timer = new GameObject("Timer", typeof(RectTransform));
            timer.transform.SetParent(parent, false);
            SetAnchor(timer.GetComponent<RectTransform>(), 0f, 0.05f, 1f, 0.12f, 56f, 0f, -56f, 0f);

            var track = new GameObject("Timer Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(timer.transform, false);
            Stretch(track.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            track.GetComponent<Image>().color = SketchPalette.WarmShadow;
            AddSketchOutline(track.transform);

            var fill = new GameObject("Timer Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            Stretch(fill.GetComponent<RectTransform>(), new Vector2(5f, 5f), new Vector2(-5f, -5f));
            timerFillImage = fill.GetComponent<Image>();
            timerFillImage.color = SketchPalette.FocusBlue;
            timerFillImage.type = Image.Type.Filled;
            timerFillImage.fillMethod = Image.FillMethod.Horizontal;
            timerFillImage.fillOrigin = 0;
            timerFillImage.fillAmount = 1f;

            timerText = CreateText(timer.transform, "5.0s", 28, TextAnchor.MiddleCenter);
            timerText.raycastTarget = false;
            Stretch(timerText.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        }

        private void CreateResultPanel(Transform parent)
        {
            resultPanel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(parent, false);
            var rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650f, 470f);
            rect.anchoredPosition = Vector2.zero;

            resultPanel.GetComponent<Image>().color = new Color(SketchPalette.TilePaper.r, SketchPalette.TilePaper.g, SketchPalette.TilePaper.b, 0.97f);
            AddSketchOutline(resultPanel.transform);

            resultTitleText = CreateText(resultPanel.transform, "Run End", 58, TextAnchor.MiddleCenter);
            SetAnchor(resultTitleText.GetComponent<RectTransform>(), 0f, 0.64f, 1f, 0.92f, 34f, 0f, -34f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "Score 0\nBest 0", 40, TextAnchor.MiddleCenter);
            SetAnchor(resultScoreText.GetComponent<RectTransform>(), 0f, 0.30f, 1f, 0.64f, 34f, 0f, -34f, 0f);

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
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void AddSketchOutline(Transform parent)
        {
            var outline = new GameObject("Sketch Outline", typeof(RectTransform), typeof(SketchOutlineGraphic));
            outline.transform.SetParent(parent, false);
            Stretch(outline.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var outlineGraphic = outline.GetComponent<SketchOutlineGraphic>();
            outlineGraphic.color = SketchPalette.Ink;
            outlineGraphic.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetAnchor(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            SetAnchor(rect, minX, minY, maxX, maxY, 0f, 0f, 0f, 0f);
        }

        private static void SetAnchor(RectTransform rect, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static AudioClip CreateToneClip(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 34f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
