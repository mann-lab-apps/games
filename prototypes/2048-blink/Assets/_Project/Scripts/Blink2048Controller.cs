using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Game2048Blink
{
    public sealed class Blink2048Controller : MonoBehaviour
    {
        private const string BestTileKey = "mannlab.2048_blink.best_tile";
        private const string BestScoreKey = "mannlab.2048_blink.best_score";
        private const float SwipeThreshold = 72f;

        private readonly Image[] cellBackgrounds = new Image[Blink2048Board.CellCount];
        private readonly Image[] grayOverlays = new Image[Blink2048Board.CellCount];
        private readonly Text[] cellLabels = new Text[Blink2048Board.CellCount];
        private readonly Blink2048Board board = new Blink2048Board();

        private Text scoreText;
        private Text blinkText;
        private Text bestText;
        private Text resultTitleText;
        private Text resultScoreText;
        private GameObject resultPanel;
        private int bestTile;
        private int bestScore;
        private bool gameOver;
        private bool pointerActive;
        private Vector2 pointerStart;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            bestTile = PlayerPrefs.GetInt(BestTileKey, 0);
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (gameOver)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                TryMove(Blink2048Direction.Left);
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                TryMove(Blink2048Direction.Right);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                TryMove(Blink2048Direction.Up);
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                TryMove(Blink2048Direction.Down);
                return;
            }

            ReadPointerSwipe();
        }

        private void StartRun()
        {
            gameOver = false;
            resultPanel.SetActive(false);
            board.StartNew();
            UpdateHeader();
            UpdateBoard();
        }

        private void TryMove(Blink2048Direction direction)
        {
            var result = board.Move(direction);
            if (!result.Moved)
            {
                if (result.GameOver)
                {
                    EndRun();
                }

                return;
            }

            UpdateHeader();
            UpdateBoard(result.SpawnedTileIndex);
            if (result.GameOver)
            {
                EndRun();
            }
        }

        private void EndRun()
        {
            if (gameOver)
            {
                return;
            }

            gameOver = true;
            if (board.HighestTile > bestTile)
            {
                bestTile = board.HighestTile;
                PlayerPrefs.SetInt(BestTileKey, bestTile);
            }

            if (board.Score > bestScore)
            {
                bestScore = board.Score;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
            }

            PlayerPrefs.Save();
            UpdateHeader();
            resultTitleText.text = "Game Over";
            resultScoreText.text = $"Tile {board.HighestTile}\nScore {board.Score}";
            resultPanel.SetActive(true);
        }

        private void ReadPointerSwipe()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pointerActive = true;
                    pointerStart = touch.position;
                    return;
                }

                if (pointerActive && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    pointerActive = false;
                    TrySwipe(touch.position - pointerStart);
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                pointerActive = true;
                pointerStart = Input.mousePosition;
                return;
            }

            if (pointerActive && Input.GetMouseButtonUp(0))
            {
                pointerActive = false;
                TrySwipe((Vector2)Input.mousePosition - pointerStart);
            }
        }

        private void TrySwipe(Vector2 delta)
        {
            if (delta.magnitude < SwipeThreshold)
            {
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                TryMove(delta.x < 0f ? Blink2048Direction.Left : Blink2048Direction.Right);
                return;
            }

            TryMove(delta.y < 0f ? Blink2048Direction.Down : Blink2048Direction.Up);
        }

        private void UpdateHeader()
        {
            scoreText.text = $"Score {board.Score}";
            blinkText.text = board.GrayCrossName;
            bestText.text = $"Best {bestTile}";
        }

        private void UpdateBoard(int spawnedTileIndex = -1)
        {
            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                var value = board.GetValueAtIndex(i);
                var hidden = board.IsHiddenIndex(i);
                var background = cellBackgrounds[i];
                var label = cellLabels[i];
                var gray = grayOverlays[i];

                background.color = hidden && value == 0 ? HiddenEmptyColor() : TileColor(value);
                label.text = value == 0 || hidden ? string.Empty : value.ToString();
                label.color = value >= 128 ? Color.white : SketchPalette.Ink;

                gray.gameObject.SetActive(hidden && value > 0);
                gray.color = i == spawnedTileIndex
                    ? new Color(0.21f, 0.23f, 0.24f, 0.82f)
                    : new Color(0.12f, 0.13f, 0.14f, 0.88f);
            }
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            var safeAreaRoot = SketchUiFactory.CreateSafeAreaRoot(canvas.transform);
            CreateTitle(safeAreaRoot);
            CreateHeader(safeAreaRoot);
            CreateBoard(safeAreaRoot);
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
        }

        private static void CreateTitle(Transform parent)
        {
            var title = CreateText(parent, "2048 Blink", 72, TextAnchor.MiddleCenter);
            var rect = title.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(32f, -116f);
            rect.offsetMax = new Vector2(-32f, -24f);
        }

        private void CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            var rect = header.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(32f, -220f);
            rect.offsetMax = new Vector2(-32f, -132f);

            scoreText = CreateText(header.transform, "Score 0", 38, TextAnchor.MiddleLeft);
            var scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 0f);
            scoreRect.anchorMax = new Vector2(0.34f, 1f);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;

            blinkText = CreateText(header.transform, "Cross 1/4", 42, TextAnchor.MiddleCenter);
            var blinkRect = blinkText.GetComponent<RectTransform>();
            blinkRect.anchorMin = new Vector2(0.28f, 0f);
            blinkRect.anchorMax = new Vector2(0.72f, 1f);
            blinkRect.offsetMin = Vector2.zero;
            blinkRect.offsetMax = Vector2.zero;

            bestText = CreateText(header.transform, "Best 0", 38, TextAnchor.MiddleRight);
            var bestRect = bestText.GetComponent<RectTransform>();
            bestRect.anchorMin = new Vector2(0.66f, 0f);
            bestRect.anchorMax = new Vector2(1f, 1f);
            bestRect.offsetMin = Vector2.zero;
            bestRect.offsetMax = Vector2.zero;
        }

        private void CreateBoard(Transform parent)
        {
            var boardArea = new GameObject("Board Area", typeof(RectTransform));
            boardArea.transform.SetParent(parent, false);
            var areaRect = boardArea.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(48f, 144f);
            areaRect.offsetMax = new Vector2(-48f, -276f);

            var boardRoot = new GameObject(
                "Board",
                typeof(RectTransform),
                typeof(BlinkBoardSizeFitter),
                typeof(GridLayoutGroup),
                typeof(BlinkBoardGridCellSizer));
            boardRoot.transform.SetParent(boardArea.transform, false);
            var rect = boardRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var grid = boardRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Blink2048Board.Size;
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                CreateCell(boardRoot.transform, i);
            }
        }

        private void CreateCell(Transform parent, int index)
        {
            var cell = new GameObject($"Cell {index:00}", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(parent, false);

            var background = cell.GetComponent<Image>();
            background.color = TileColor(0);

            var label = CreateText(cell.transform, string.Empty, 76, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            Stretch(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var gray = new GameObject("Gray Tile Mask", typeof(RectTransform), typeof(Image));
            gray.transform.SetParent(cell.transform, false);
            Stretch(gray.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var grayImage = gray.GetComponent<Image>();
            grayImage.raycastTarget = false;

            AddSketchOutline(cell.transform);

            cellBackgrounds[index] = background;
            cellLabels[index] = label;
            grayOverlays[index] = grayImage;
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

            resultPanel.GetComponent<Image>().color = new Color(SketchPalette.TilePaper.r, SketchPalette.TilePaper.g, SketchPalette.TilePaper.b, 0.97f);
            AddSketchOutline(resultPanel.transform);

            resultTitleText = CreateText(resultPanel.transform, "Game Over", 56, TextAnchor.MiddleCenter);
            var titleRect = resultTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.62f);
            titleRect.anchorMax = new Vector2(1f, 0.92f);
            titleRect.offsetMin = new Vector2(24f, 0f);
            titleRect.offsetMax = new Vector2(-24f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "Tile 0\nScore 0", 40, TextAnchor.MiddleCenter);
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

        private static Button CreateSketchButton(Transform parent, string label, int fontSize)
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
            outlineGraphic.Thickness = 3.2f;
            outlineGraphic.Jitter = 1.9f;
            outlineGraphic.Seed = parent.GetInstanceID();
            outlineGraphic.raycastTarget = false;
        }

        private static Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = SketchPalette.Ink;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Color TileColor(int value)
        {
            switch (value)
            {
                case 0:
                    return new Color32(229, 219, 203, 255);
                case 2:
                    return SketchPalette.TilePaper;
                case 4:
                    return new Color32(239, 222, 183, 255);
                case 8:
                    return new Color32(241, 174, 95, 255);
                case 16:
                    return new Color32(238, 142, 87, 255);
                case 32:
                    return new Color32(226, 103, 86, 255);
                case 64:
                    return new Color32(212, 77, 67, 255);
                case 128:
                    return new Color32(104, 147, 174, 255);
                case 256:
                    return new Color32(76, 128, 162, 255);
                case 512:
                    return new Color32(91, 151, 111, 255);
                case 1024:
                    return new Color32(124, 104, 168, 255);
                default:
                    return new Color32(69, 78, 92, 255);
            }
        }

        private static Color HiddenEmptyColor()
        {
            return new Color32(204, 207, 205, 255);
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
