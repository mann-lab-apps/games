using System.Collections;
using System.Collections.Generic;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Game10000
{
    public sealed class Game10000Controller : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.10000.best_cleared_stages";
        private const float WrongTapPenalty = 0.5f;
        private const float RunTimeLimit = 60f;
        private readonly List<Button> cellButtons = new List<Button>();
        private readonly List<Image> cellBackgrounds = new List<Image>();
        private readonly List<Image> cellHighlights = new List<Image>();
        private readonly BoardGenerator boardGenerator = new BoardGenerator();

        private BoardData board;
        private Text stageText;
        private Text bestText;
        private Text resultTitleText;
        private Text resultScoreText;
        private Image timerFill;
        private CanvasGroup introCanvasGroup;
        private RectTransform introTilesRoot;
        private GameObject introPanel;
        private GameObject resultPanel;
        private float remainingTime;
        private int stage = 1;
        private int bestClearedStages;
        private bool acceptingInput;
        private bool gameOver;
        private bool timerRunning;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            bestClearedStages = PlayerPrefs.GetInt(BestScoreKey, 0);
            FirebaseTelemetry.Initialize();
            FirebaseTelemetry.LogEvent("app_open");

            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (gameOver || !timerRunning)
            {
                return;
            }

            remainingTime -= Time.deltaTime;
            UpdateTimer();

            if (remainingTime <= 0f)
            {
                EndRun();
            }
        }

        private void StartRun()
        {
            StopAllCoroutines();
            stage = 1;
            remainingTime = RunTimeLimit;
            gameOver = false;
            timerRunning = false;
            resultPanel.SetActive(false);
            GenerateStage();
            acceptingInput = false;
            FirebaseTelemetry.LogEvent("run_start");
            StartCoroutine(PlayOpeningHint());
        }

        private void GenerateStage()
        {
            board = boardGenerator.Generate();
            acceptingInput = true;

            stageText.text = $"Stage {stage}";
            bestText.text = $"Best {bestClearedStages}";
            UpdateTimer();

            for (var i = 0; i < cellButtons.Count; i++)
            {
                var digit = board.GetDigitAtIndex(i);
                var label = cellButtons[i].GetComponentInChildren<Text>();
                label.text = digit.ToString();
                label.color = SketchPalette.Ink;
                cellBackgrounds[i].color = SketchPalette.TilePaper;
                cellHighlights[i].color = Color.clear;
                cellButtons[i].interactable = true;
            }
        }

        private void HandleCellTapped(int index)
        {
            if (!acceptingInput || gameOver)
            {
                return;
            }

            if (board.IsTargetCell(index))
            {
                StartCoroutine(ClearStage());
                return;
            }

            remainingTime = Mathf.Max(0f, remainingTime - WrongTapPenalty);
            FirebaseTelemetry.LogEvent(
                "wrong_tap",
                new Dictionary<string, string>
                {
                    { "stage", stage.ToString() },
                    { "remaining_time", Mathf.CeilToInt(remainingTime).ToString() }
                });
            StartCoroutine(FlashWrong(index));
            UpdateTimer();

            if (remainingTime <= 0f)
            {
                EndRun();
            }
        }

        private IEnumerator ClearStage()
        {
            acceptingInput = false;
            RevealTargets(SketchPalette.CorrectMarker);

            yield return new WaitForSeconds(0.18f);

            if (gameOver)
            {
                yield break;
            }

            stage++;
            var clearedStages = stage - 1;
            FirebaseTelemetry.LogEvent(
                "stage_clear",
                new Dictionary<string, string>
                {
                    { "stage", clearedStages.ToString() },
                    { "remaining_time", Mathf.CeilToInt(remainingTime).ToString() }
                });
            if (clearedStages > bestClearedStages)
            {
                bestClearedStages = clearedStages;
                PlayerPrefs.SetInt(BestScoreKey, bestClearedStages);
                PlayerPrefs.Save();
            }

            GenerateStage();
        }

        private IEnumerator PlayOpeningHint()
        {
            introPanel.SetActive(true);
            introCanvasGroup.alpha = 1f;
            introTilesRoot.localScale = Vector3.one * 0.92f;
            introTilesRoot.anchoredPosition = Vector2.zero;

            yield return new WaitForSeconds(0.18f);

            const float settleDuration = 0.22f;
            var elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / settleDuration);
                introTilesRoot.localScale = Vector3.Lerp(Vector3.one * 0.92f, Vector3.one, progress);
                yield return null;
            }

            yield return new WaitForSeconds(0.42f);

            const float vanishDuration = 0.32f;
            elapsed = 0f;
            while (elapsed < vanishDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / vanishDuration);
                introCanvasGroup.alpha = 1f - progress;
                introTilesRoot.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, progress);
                introTilesRoot.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0f, 90f), progress);
                yield return null;
            }

            introPanel.SetActive(false);
            acceptingInput = true;
            timerRunning = true;
        }

        private IEnumerator FlashWrong(int index)
        {
            var background = cellBackgrounds[index];
            var original = background.color;
            background.color = Color.Lerp(SketchPalette.TilePaper, SketchPalette.WrongMarker, 0.55f);

            yield return new WaitForSeconds(0.1f);

            background.color = original;
        }

        private void EndRun()
        {
            if (gameOver)
            {
                return;
            }

            gameOver = true;
            acceptingInput = false;
            remainingTime = 0f;
            UpdateTimer();
            RevealTargets(SketchPalette.CorrectMarker);

            var clearedStages = Mathf.Max(0, stage - 1);
            if (clearedStages > bestClearedStages)
            {
                bestClearedStages = clearedStages;
                PlayerPrefs.SetInt(BestScoreKey, bestClearedStages);
                PlayerPrefs.Save();
            }

            bestText.text = $"Best {bestClearedStages}";
            resultTitleText.text = "Run Complete";
            resultScoreText.text = $"Cleared {clearedStages}\nBest {bestClearedStages}";
            FirebaseTelemetry.LogEvent(
                "run_end",
                new Dictionary<string, string>
                {
                    { "cleared_stages", clearedStages.ToString() },
                    { "best_cleared_stages", bestClearedStages.ToString() }
                });
            resultPanel.SetActive(true);
        }

        private void RevealTargets(Color color)
        {
            foreach (var targetIndex in board.TargetIndices)
            {
                cellHighlights[targetIndex].color = color;
            }
        }

        private void UpdateTimer()
        {
            var normalized = RunTimeLimit <= 0f ? 0f : Mathf.Clamp01(remainingTime / RunTimeLimit);
            timerFill.fillAmount = normalized;
            timerFill.color = normalized < 0.25f
                ? Color.Lerp(SketchPalette.WarningAmber, SketchPalette.WrongMarker, 0.35f)
                : SketchPalette.WarningAmber;
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            CreateHeader(canvas.transform);
            CreateTimer(canvas.transform);
            CreateBoard(canvas.transform);
            CreateIntroPanel(canvas.transform);
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
            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            rect.offsetMin = new Vector2(32f, -112f);
            rect.offsetMax = new Vector2(-32f, -28f);

            stageText = CreateText(header.transform, "Stage 1", 46, TextAnchor.MiddleLeft);
            var stageRect = stageText.GetComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0f, 0f);
            stageRect.anchorMax = new Vector2(0.5f, 1f);
            stageRect.offsetMin = Vector2.zero;
            stageRect.offsetMax = Vector2.zero;

            bestText = CreateText(header.transform, "Best 0", 34, TextAnchor.MiddleRight);
            var bestRect = bestText.GetComponent<RectTransform>();
            bestRect.anchorMin = new Vector2(0.5f, 0f);
            bestRect.anchorMax = new Vector2(1f, 1f);
            bestRect.offsetMin = Vector2.zero;
            bestRect.offsetMax = Vector2.zero;
        }

        private void CreateTimer(Transform parent)
        {
            var track = new GameObject("Timer Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            var rect = track.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(32f, -134f);
            rect.offsetMax = new Vector2(-32f, -118f);
            track.GetComponent<Image>().color = SketchPalette.TilePaper;
            AddSketchOutline(track.transform);

            var fill = new GameObject("Timer Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            timerFill = fill.GetComponent<Image>();
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = 0;
        }

        private void CreateBoard(Transform parent)
        {
            var boardRoot = new GameObject("Board", typeof(RectTransform), typeof(BoardSizeFitter), typeof(GridLayoutGroup), typeof(BoardGridCellSizer));
            boardRoot.transform.SetParent(parent, false);
            var rect = boardRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -20f);

            var grid = boardRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardData.Size;
            grid.spacing = new Vector2(SketchMetrics.BoardGap, SketchMetrics.BoardGap);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < BoardData.Size * BoardData.Size; i++)
            {
                CreateCell(boardRoot.transform, i);
            }
        }

        private void CreateCell(Transform parent, int index)
        {
            var cell = new GameObject($"Cell {index:00}", typeof(RectTransform), typeof(Image), typeof(Button));
            cell.transform.SetParent(parent, false);

            var background = cell.GetComponent<Image>();
            background.color = SketchPalette.TilePaper;

            var button = cell.GetComponent<Button>();
            var capturedIndex = index;
            button.onClick.AddListener(() => HandleCellTapped(capturedIndex));

            button.colors = SketchUiFactory.ButtonColors();

            var highlight = new GameObject("Marker Highlight", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(cell.transform, false);
            Stretch(
                highlight.GetComponent<RectTransform>(),
                new Vector2(SketchMetrics.MarkerInset, SketchMetrics.MarkerInset),
                new Vector2(-SketchMetrics.MarkerInset, -SketchMetrics.MarkerInset));
            var highlightImage = highlight.GetComponent<Image>();
            highlightImage.color = Color.clear;
            highlightImage.raycastTarget = false;

            var text = CreateText(cell.transform, "0", 44, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var outline = new GameObject("Sketch Outline", typeof(RectTransform), typeof(SketchOutlineGraphic));
            outline.transform.SetParent(cell.transform, false);
            Stretch(outline.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var outlineGraphic = outline.GetComponent<SketchOutlineGraphic>();
            outlineGraphic.color = SketchPalette.Ink;
            outlineGraphic.raycastTarget = false;

            cellButtons.Add(button);
            cellBackgrounds.Add(background);
            cellHighlights.Add(highlightImage);
        }

        private void CreateResultPanel(Transform parent)
        {
            resultPanel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(parent, false);
            var rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 430f);
            rect.anchoredPosition = Vector2.zero;

            resultPanel.GetComponent<Image>().color = new Color(SketchPalette.TilePaper.r, SketchPalette.TilePaper.g, SketchPalette.TilePaper.b, 0.96f);
            AddSketchOutline(resultPanel.transform);

            resultTitleText = CreateText(resultPanel.transform, "Run Complete", 52, TextAnchor.MiddleCenter);
            var titleRect = resultTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.62f);
            titleRect.anchorMax = new Vector2(1f, 0.92f);
            titleRect.offsetMin = new Vector2(24f, 0f);
            titleRect.offsetMax = new Vector2(-24f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "Cleared 0\nBest 0", 38, TextAnchor.MiddleCenter);
            var scoreRect = resultScoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 0.32f);
            scoreRect.anchorMax = new Vector2(1f, 0.62f);
            scoreRect.offsetMin = new Vector2(24f, 0f);
            scoreRect.offsetMax = new Vector2(-24f, 0f);

            var restart = CreateSketchButton(resultPanel.transform, "Again", 36);
            var restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.08f);
            restartRect.anchorMax = new Vector2(0.5f, 0.08f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.sizeDelta = new Vector2(260f, 92f);
            restartRect.anchoredPosition = Vector2.zero;
            restart.onClick.AddListener(StartRun);

            resultPanel.SetActive(false);
        }

        private void CreateIntroPanel(Transform parent)
        {
            introPanel = new GameObject("Opening Target Hint", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            introPanel.transform.SetParent(parent, false);
            Stretch(introPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var image = introPanel.GetComponent<Image>();
            image.color = new Color(SketchPalette.Paper.r, SketchPalette.Paper.g, SketchPalette.Paper.b, 0.9f);

            introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
            introCanvasGroup.blocksRaycasts = true;

            var row = new GameObject("Target Tiles", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(introPanel.transform, false);
            introTilesRoot = row.GetComponent<RectTransform>();
            introTilesRoot.anchorMin = new Vector2(0.5f, 0.5f);
            introTilesRoot.anchorMax = new Vector2(0.5f, 0.5f);
            introTilesRoot.pivot = new Vector2(0.5f, 0.5f);
            introTilesRoot.sizeDelta = new Vector2(640f, 130f);
            introTilesRoot.anchoredPosition = Vector2.zero;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;

            var digits = new[] { "1", "0", "0", "0", "0" };
            foreach (var digit in digits)
            {
                CreateIntroTile(row.transform, digit);
            }

            introPanel.SetActive(false);
        }

        private void CreateIntroTile(Transform parent, string digit)
        {
            var tile = new GameObject($"Hint Tile {digit}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            tile.transform.SetParent(parent, false);
            tile.GetComponent<Image>().color = SketchPalette.TilePaper;
            AddSketchOutline(tile.transform);

            var layout = tile.GetComponent<LayoutElement>();
            layout.preferredWidth = 104f;
            layout.preferredHeight = 104f;

            var text = CreateText(tile.transform, digit, 64, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
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

        private static void AddSketchOutline(Transform parent)
        {
            var outline = new GameObject("Sketch Outline", typeof(RectTransform), typeof(SketchOutlineGraphic));
            outline.transform.SetParent(parent, false);
            Stretch(outline.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var outlineGraphic = outline.GetComponent<SketchOutlineGraphic>();
            outlineGraphic.color = SketchPalette.Ink;
            outlineGraphic.raycastTarget = false;
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

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
