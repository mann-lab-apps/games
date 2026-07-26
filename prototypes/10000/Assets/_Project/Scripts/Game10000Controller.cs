using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Game10000
{
    public sealed class Game10000Controller : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.10000.best_cleared_stages";
        private const float WrongTapPenalty = 0.5f;
        private static readonly Color Paper = new Color32(250, 247, 239, 255);
        private static readonly Color Ink = new Color32(40, 39, 36, 255);
        private static readonly Color TilePaper = new Color32(255, 253, 247, 255);
        private static readonly Color CorrectMarker = new Color32(94, 212, 129, 130);
        private static readonly Color WrongMarker = new Color32(230, 68, 64, 120);
        private static readonly Color Amber = new Color32(238, 168, 64, 255);

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
        private GameObject resultPanel;
        private float stageTimeLimit;
        private float remainingTime;
        private int stage = 1;
        private int bestClearedStages;
        private bool acceptingInput;
        private bool gameOver;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            bestClearedStages = PlayerPrefs.GetInt(BestScoreKey, 0);

            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (gameOver || !acceptingInput)
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
            stage = 1;
            gameOver = false;
            resultPanel.SetActive(false);
            GenerateStage();
        }

        private void GenerateStage()
        {
            board = boardGenerator.Generate();
            stageTimeLimit = StageDifficulty.GetTimeLimit(stage);
            remainingTime = stageTimeLimit;
            acceptingInput = true;

            stageText.text = $"Stage {stage}";
            bestText.text = $"Best {bestClearedStages}";
            UpdateTimer();

            for (var i = 0; i < cellButtons.Count; i++)
            {
                var digit = board.GetDigitAtIndex(i);
                var label = cellButtons[i].GetComponentInChildren<Text>();
                label.text = digit.ToString();
                label.color = Ink;
                cellBackgrounds[i].color = TilePaper;
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
            RevealTargets(CorrectMarker);

            yield return new WaitForSeconds(0.18f);

            stage++;
            var clearedStages = stage - 1;
            if (clearedStages > bestClearedStages)
            {
                bestClearedStages = clearedStages;
                PlayerPrefs.SetInt(BestScoreKey, bestClearedStages);
                PlayerPrefs.Save();
            }

            GenerateStage();
        }

        private IEnumerator FlashWrong(int index)
        {
            var background = cellBackgrounds[index];
            var original = background.color;
            background.color = Color.Lerp(TilePaper, WrongMarker, 0.55f);

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
            RevealTargets(CorrectMarker);

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
            var normalized = stageTimeLimit <= 0f ? 0f : Mathf.Clamp01(remainingTime / stageTimeLimit);
            timerFill.fillAmount = normalized;
            timerFill.color = normalized < 0.25f ? Color.Lerp(Amber, WrongMarker, 0.35f) : Amber;
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            CreateHeader(canvas.transform);
            CreateTimer(canvas.transform);
            CreateBoard(canvas.transform);
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
            background.GetComponent<Image>().color = Paper;
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
            track.GetComponent<Image>().color = new Color32(255, 253, 247, 255);
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
            grid.spacing = new Vector2(5f, 5f);
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
            background.color = TilePaper;

            var button = cell.GetComponent<Button>();
            var capturedIndex = index;
            button.onClick.AddListener(() => HandleCellTapped(capturedIndex));

            var colors = button.colors;
            colors.normalColor = TilePaper;
            colors.highlightedColor = new Color32(255, 250, 229, 255);
            colors.pressedColor = new Color32(244, 235, 208, 255);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var highlight = new GameObject("Marker Highlight", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(cell.transform, false);
            Stretch(highlight.GetComponent<RectTransform>(), new Vector2(8f, 8f), new Vector2(-8f, -8f));
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
            outlineGraphic.color = Ink;
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

            resultPanel.GetComponent<Image>().color = new Color32(255, 253, 247, 245);
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

        private Button CreateSketchButton(Transform parent, string label, int fontSize)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = TilePaper;
            AddSketchOutline(buttonObject.transform);

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = TilePaper;
            colors.highlightedColor = new Color32(255, 250, 229, 255);
            colors.pressedColor = new Color32(238, 225, 193, 255);
            button.colors = colors;

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
            outlineGraphic.color = Ink;
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
            text.color = Ink;
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
