using System.Collections;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.DrumDuel
{
    public sealed class DrumDuelController : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.drum_duel.best_stage";
        private const float TimingWindowSeconds = 0.14f;
        private const float StageSettleSeconds = 0.34f;
        private const float TickFlashSeconds = 0.12f;
        private const float PlayerInputTailSeconds = 0.2f;

        private readonly Image[] computerTickImages = new Image[RhythmPattern.TickCount];
        private readonly Image[] playerTickImages = new Image[RhythmPattern.TickCount];
        private readonly bool[] playerHits = new bool[RhythmPattern.TickCount];

        private AudioSource audioSource;
        private AudioClip countInClip;
        private AudioClip computerHitClip;
        private AudioClip playerHitClip;
        private AudioClip failClip;
        private Text stageText;
        private Text bestText;
        private Text bpmText;
        private Text phaseText;
        private Text patternText;
        private Text resultTitleText;
        private Text resultScoreText;
        private Button tapPadButton;
        private Image tapPadImage;
        private GameObject resultPanel;
        private RhythmPattern currentPattern;
        private float currentTickDuration;
        private float playerInputStartedAt;
        private int stage = 1;
        private int bestStage;
        private bool acceptingInput;
        private bool runEnded;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            bestStage = PlayerPrefs.GetInt(BestScoreKey, 0);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            countInClip = CreatePercussionClip("Count Tick", 620f, 0.045f, 0.42f);
            computerHitClip = CreatePercussionClip("Computer Hat", 880f, 0.07f, 0.55f);
            playerHitClip = CreatePercussionClip("Player Hat", 1320f, 0.055f, 0.45f);
            failClip = CreatePercussionClip("Miss", 180f, 0.16f, 0.5f);

            BuildInterface();
            StartRun();
        }

        private void StartRun()
        {
            StopAllCoroutines();
            stage = 1;
            runEnded = false;
            acceptingInput = false;
            resultPanel.SetActive(false);
            StartCoroutine(PlayStage());
        }

        private IEnumerator PlayStage()
        {
            acceptingInput = false;
            currentPattern = RhythmStageLibrary.PatternForStage(stage);
            currentTickDuration = RhythmStageLibrary.TickDurationForStage(stage);
            ClearTickRows();
            UpdateHeader();

            yield return PlayCountIn();

            phaseText.text = "Listen";
            patternText.text = currentPattern.ToPulseString();
            yield return new WaitForSeconds(StageSettleSeconds);

            for (var i = 0; i < RhythmPattern.TickCount; i++)
            {
                PulseTick(computerTickImages[i], currentPattern.HasHitAt(i));
                if (currentPattern.HasHitAt(i))
                {
                    PlayClip(computerHitClip);
                }

                yield return new WaitForSeconds(currentTickDuration);
            }

            phaseText.text = "Your turn";
            patternText.text = "tap the same beats";
            ClearPlayerHits();
            playerInputStartedAt = Time.time;
            acceptingInput = true;

            for (var i = 0; i < RhythmPattern.TickCount; i++)
            {
                PulseTick(playerTickImages[i], false);
                yield return new WaitForSeconds(currentTickDuration);
            }

            yield return new WaitForSeconds(PlayerInputTailSeconds);
            acceptingInput = false;

            if (!runEnded && MissingRequiredHit())
            {
                EndRun("Missed beat");
                yield break;
            }

            if (!runEnded)
            {
                yield return ClearStage();
            }
        }

        private IEnumerator PlayCountIn()
        {
            phaseText.text = "Ready";
            patternText.text = "1 2 3 4";

            for (var i = 0; i < RhythmPattern.TickCount; i++)
            {
                PulseTick(computerTickImages[i], false);
                PlayClip(countInClip);
                yield return new WaitForSeconds(currentTickDuration);
            }

            ClearTickRows();
        }

        private IEnumerator ClearStage()
        {
            phaseText.text = "Clear";
            patternText.text = "nice timing";
            stage++;
            var clearedStage = stage - 1;
            if (clearedStage > bestStage)
            {
                bestStage = clearedStage;
                PlayerPrefs.SetInt(BestScoreKey, bestStage);
                PlayerPrefs.Save();
            }

            UpdateHeader();
            yield return new WaitForSeconds(0.42f);

            if (!runEnded)
            {
                StartCoroutine(PlayStage());
            }
        }

        private void HandleTap()
        {
            if (!acceptingInput || runEnded)
            {
                return;
            }

            PlayClip(playerHitClip);
            StartCoroutine(FlashTapPad(SketchPalette.WarmHighlight));

            var elapsed = Time.time - playerInputStartedAt;
            var nearestTick = Mathf.RoundToInt(elapsed / currentTickDuration);
            if (nearestTick < 0 || nearestTick >= RhythmPattern.TickCount)
            {
                EndRun("Out of time");
                return;
            }

            var tickTime = nearestTick * currentTickDuration;
            var offset = Mathf.Abs(elapsed - tickTime);
            if (offset > TimingWindowSeconds)
            {
                EndRun("Off beat");
                return;
            }

            if (!currentPattern.HasHitAt(nearestTick))
            {
                EndRun("Extra hit");
                return;
            }

            if (playerHits[nearestTick])
            {
                EndRun("Double hit");
                return;
            }

            playerHits[nearestTick] = true;
            playerTickImages[nearestTick].color = SketchPalette.CorrectMarker;
        }

        private bool MissingRequiredHit()
        {
            for (var i = 0; i < RhythmPattern.TickCount; i++)
            {
                if (currentPattern.HasHitAt(i) && !playerHits[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void EndRun(string reason)
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            acceptingInput = false;
            StopAllCoroutines();
            PlayClip(failClip);
            StartCoroutine(FlashTapPad(SketchPalette.WrongMarker));

            var clearedStages = Mathf.Max(0, stage - 1);
            if (clearedStages > bestStage)
            {
                bestStage = clearedStages;
                PlayerPrefs.SetInt(BestScoreKey, bestStage);
                PlayerPrefs.Save();
            }

            UpdateHeader();
            phaseText.text = reason;
            patternText.text = currentPattern.ToPulseString();
            resultTitleText.text = "Run End";
            resultScoreText.text = $"Cleared {clearedStages}\nBest {bestStage}";
            resultPanel.SetActive(true);
        }

        private void ClearPlayerHits()
        {
            for (var i = 0; i < playerHits.Length; i++)
            {
                playerHits[i] = false;
                playerTickImages[i].color = SketchPalette.TilePaper;
            }
        }

        private void ClearTickRows()
        {
            for (var i = 0; i < RhythmPattern.TickCount; i++)
            {
                computerTickImages[i].color = SketchPalette.TilePaper;
                playerTickImages[i].color = SketchPalette.TilePaper;
                playerHits[i] = false;
            }
        }

        private void PulseTick(Image image, bool hit)
        {
            image.color = hit ? SketchPalette.WarningAmber : SketchPalette.WarmShadow;
            StartCoroutine(FadeTick(image, hit ? SketchPalette.CorrectMarker : SketchPalette.TilePaper));
        }

        private IEnumerator FadeTick(Image image, Color finalColor)
        {
            yield return new WaitForSeconds(TickFlashSeconds);
            if (image.color != SketchPalette.CorrectMarker && image.color != SketchPalette.WrongMarker)
            {
                image.color = finalColor;
            }
        }

        private IEnumerator FlashTapPad(Color color)
        {
            if (tapPadImage == null)
            {
                yield break;
            }

            tapPadImage.color = Color.Lerp(SketchPalette.TilePaper, color, 0.55f);
            yield return new WaitForSeconds(0.08f);
            tapPadImage.color = SketchPalette.TilePaper;
        }

        private void UpdateHeader()
        {
            stageText.text = $"Stage {stage}";
            bestText.text = $"Best {bestStage}";
            bpmText.text = $"{Mathf.RoundToInt(RhythmStageLibrary.BeatsPerMinuteForStage(stage))} BPM";
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
            CreateRhythmPanel(canvas.transform);
            CreateTapPad(canvas.transform);
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
            rect.offsetMin = new Vector2(36f, -130f);
            rect.offsetMax = new Vector2(-36f, -30f);

            stageText = CreateText(header.transform, "Stage 1", 46, TextAnchor.MiddleLeft);
            SetAnchor(stageText.GetComponent<RectTransform>(), 0f, 0f, 0.34f, 1f);

            bpmText = CreateText(header.transform, "80 BPM", 32, TextAnchor.MiddleCenter);
            SetAnchor(bpmText.GetComponent<RectTransform>(), 0.33f, 0f, 0.67f, 1f);

            bestText = CreateText(header.transform, "Best 0", 34, TextAnchor.MiddleRight);
            SetAnchor(bestText.GetComponent<RectTransform>(), 0.66f, 0f, 1f, 1f);
        }

        private void CreateRhythmPanel(Transform parent)
        {
            var panel = new GameObject("Rhythm Panel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.38f);
            rect.anchorMax = new Vector2(1f, 0.88f);
            rect.offsetMin = new Vector2(44f, 0f);
            rect.offsetMax = new Vector2(-44f, 0f);

            phaseText = CreateText(panel.transform, "Listen", 76, TextAnchor.MiddleCenter);
            SetAnchor(phaseText.GetComponent<RectTransform>(), 0f, 0.72f, 1f, 1f);

            patternText = CreateText(panel.transform, "x . . .", 34, TextAnchor.MiddleCenter);
            patternText.color = SketchPalette.MutedInk;
            SetAnchor(patternText.GetComponent<RectTransform>(), 0f, 0.62f, 1f, 0.76f);

            CreateTickRow(panel.transform, "Computer", 0.36f, 0.58f, computerTickImages);
            CreateTickRow(panel.transform, "You", 0.08f, 0.30f, playerTickImages);
        }

        private void CreateTickRow(Transform parent, string label, float minY, float maxY, Image[] output)
        {
            var labelText = CreateText(parent, label, 30, TextAnchor.MiddleLeft);
            labelText.color = SketchPalette.MutedInk;
            SetAnchor(labelText.GetComponent<RectTransform>(), 0f, minY, 0.22f, maxY);

            var row = new GameObject($"{label} Tick Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            SetAnchor(row.GetComponent<RectTransform>(), 0.24f, minY, 1f, maxY);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 18f;

            for (var i = 0; i < RhythmPattern.TickCount; i++)
            {
                var tick = new GameObject($"Tick {i + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                tick.transform.SetParent(row.transform, false);
                tick.GetComponent<Image>().color = SketchPalette.TilePaper;
                AddSketchOutline(tick.transform);

                var layoutElement = tick.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = 140f;
                layoutElement.preferredHeight = 116f;

                var text = CreateText(tick.transform, (i + 1).ToString(), 44, TextAnchor.MiddleCenter);
                text.raycastTarget = false;
                Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

                output[i] = tick.GetComponent<Image>();
            }
        }

        private void CreateTapPad(Transform parent)
        {
            tapPadButton = CreateSketchButton(parent, "TAP", 82);
            var rect = tapPadButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(760f, 320f);
            rect.anchoredPosition = new Vector2(0f, 78f);
            tapPadButton.onClick.AddListener(HandleTap);
            tapPadImage = tapPadButton.GetComponent<Image>();
        }

        private void CreateResultPanel(Transform parent)
        {
            resultPanel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(parent, false);
            var rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 430f);
            rect.anchoredPosition = Vector2.zero;

            resultPanel.GetComponent<Image>().color = new Color(SketchPalette.TilePaper.r, SketchPalette.TilePaper.g, SketchPalette.TilePaper.b, 0.97f);
            AddSketchOutline(resultPanel.transform);

            resultTitleText = CreateText(resultPanel.transform, "Run End", 54, TextAnchor.MiddleCenter);
            SetAnchor(resultTitleText.GetComponent<RectTransform>(), 0f, 0.62f, 1f, 0.92f, 30f, 0f, -30f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "Cleared 0\nBest 0", 38, TextAnchor.MiddleCenter);
            SetAnchor(resultScoreText.GetComponent<RectTransform>(), 0f, 0.32f, 1f, 0.62f, 30f, 0f, -30f, 0f);

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

        private static AudioClip CreatePercussionClip(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            var noiseSeed = 0.37f;

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 42f);
                noiseSeed = Mathf.Repeat(noiseSeed * 17.17f + 0.31f, 1f);
                var noise = (noiseSeed * 2f - 1f) * 0.55f;
                var tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.45f;
                samples[i] = (tone + noise) * envelope * volume;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
