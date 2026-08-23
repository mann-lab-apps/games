using System.Collections;
using System.Collections.Generic;
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
        private const float SlideDuration = 0.16f;
        private const float SettleDuration = 0.08f;
        private const float SpawnPopDuration = 0.12f;
        private const float CurtainDuration = 0.22f;

        private readonly Image[] cellBackgrounds = new Image[Blink2048Board.CellCount];
        private readonly Image[] grayOverlays = new Image[Blink2048Board.CellCount];
        private readonly Text[] cellLabels = new Text[Blink2048Board.CellCount];
        private readonly RectTransform[] cellRects = new RectTransform[Blink2048Board.CellCount];
        private readonly Blink2048Board board = new Blink2048Board();

        private RectTransform boardRect;
        private RectTransform animationLayerRect;
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
        private bool isAnimating;
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
            if (gameOver || isAnimating)
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
            isAnimating = false;
            ClearAnimationLayer();
            resultPanel.SetActive(false);
            board.StartNew();
            UpdateHeader();
            UpdateBoard(showCurtains: true);
        }

        private void TryMove(Blink2048Direction direction)
        {
            if (isAnimating)
            {
                return;
            }

            var beforeValues = CaptureBoardValues();
            var beforeHidden = CaptureHiddenCells();
            var result = board.Move(direction);
            if (!result.Moved)
            {
                if (result.GameOver)
                {
                    EndRun();
                }

                return;
            }

            var motions = BuildTileMotions(beforeValues, direction);
            StartCoroutine(AnimateValidMove(beforeValues, beforeHidden, motions, result));
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

        private void UpdateBoard(int spawnedTileIndex = -1, bool showCurtains = true)
        {
            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                var value = board.GetValueAtIndex(i);
                var hidden = showCurtains && board.IsHiddenIndex(i);
                var background = cellBackgrounds[i];
                var label = cellLabels[i];
                var gray = grayOverlays[i];

                background.color = hidden && value == 0 ? HiddenEmptyColor() : TileColor(value);
                label.text = value == 0 || hidden ? string.Empty : value.ToString();
                label.color = LabelColor(value, 1f);

                gray.gameObject.SetActive(hidden && value > 0);
                gray.transform.localScale = Vector3.one;
                gray.color = i == spawnedTileIndex
                    ? new Color(0.21f, 0.23f, 0.24f, 0.82f)
                    : new Color(0.12f, 0.13f, 0.14f, 0.88f);
            }
        }

        private IEnumerator AnimateValidMove(int[] beforeValues, bool[] beforeHidden, List<TileMotion> motions, Blink2048MoveResult result)
        {
            isAnimating = true;
            SyncAnimationLayerToBoard();
            UpdateBoardFromValues(beforeValues, beforeHidden);

            var ghosts = CreateMotionGhosts(motions, beforeHidden);
            UpdateBoardFromValues(new int[Blink2048Board.CellCount], beforeHidden);
            yield return SlideGhosts(ghosts);
            DestroyGhosts(ghosts);

            UpdateHeader();
            UpdateBoardFromValues(CaptureBoardValues(), beforeHidden);
            yield return PopSpawnedTile(result.SpawnedTileIndex, beforeHidden);
            yield return AnimateCurtainTransition(beforeHidden, result.SpawnedTileIndex);

            UpdateBoard(result.SpawnedTileIndex, showCurtains: true);
            isAnimating = false;

            if (result.GameOver)
            {
                EndRun();
            }
        }

        private int[] CaptureBoardValues()
        {
            var result = new int[Blink2048Board.CellCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = board.GetValueAtIndex(i);
            }

            return result;
        }

        private bool[] CaptureHiddenCells()
        {
            var result = new bool[Blink2048Board.CellCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = board.IsHiddenIndex(i);
            }

            return result;
        }

        private void UpdateBoardFromValues(int[] values, bool[] hiddenMask)
        {
            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                var value = values[i];
                var hidden = IsHiddenInMask(hiddenMask, i);
                cellBackgrounds[i].color = hidden && value == 0 ? HiddenEmptyColor() : TileColor(value);
                cellLabels[i].text = value == 0 || hidden ? string.Empty : value.ToString();
                cellLabels[i].color = LabelColor(value, 1f);
                grayOverlays[i].gameObject.SetActive(hidden && value > 0);
                grayOverlays[i].transform.localScale = Vector3.one;
                grayOverlays[i].color = new Color(0.12f, 0.13f, 0.14f, 0.88f);
            }
        }

        private List<TileGhost> CreateMotionGhosts(List<TileMotion> motions, bool[] beforeHidden)
        {
            var ghosts = new List<TileGhost>(motions.Count);
            foreach (var motion in motions)
            {
                var tile = CreateTileVisual(animationLayerRect, motion.SourceValue, IsHiddenInMask(beforeHidden, motion.SourceIndex));
                var rect = tile.GetComponent<RectTransform>();
                rect.sizeDelta = CellSize();
                rect.anchoredPosition = CellPosition(motion.SourceIndex);
                tile.transform.SetAsLastSibling();
                ghosts.Add(new TileGhost(tile, rect, motion));
            }

            return ghosts;
        }

        private IEnumerator SlideGhosts(List<TileGhost> ghosts)
        {
            for (var elapsed = 0f; elapsed < SlideDuration; elapsed += Time.deltaTime)
            {
                var eased = Smooth01(elapsed / SlideDuration);
                foreach (var ghost in ghosts)
                {
                    ghost.Rect.anchoredPosition = Vector2.Lerp(
                        CellPosition(ghost.Motion.SourceIndex),
                        CellPosition(ghost.Motion.TargetIndex),
                        eased);
                }

                yield return null;
            }

            foreach (var ghost in ghosts)
            {
                ghost.Rect.anchoredPosition = CellPosition(ghost.Motion.TargetIndex);
            }

            for (var elapsed = 0f; elapsed < SettleDuration; elapsed += Time.deltaTime)
            {
                var scale = 1f + 0.08f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / SettleDuration));
                foreach (var ghost in ghosts)
                {
                    if (ghost.Motion.Merged)
                    {
                        ghost.Rect.localScale = new Vector3(scale, scale, 1f);
                    }
                }

                yield return null;
            }
        }

        private void DestroyGhosts(List<TileGhost> ghosts)
        {
            foreach (var ghost in ghosts)
            {
                Destroy(ghost.Root);
            }
        }

        private IEnumerator PopSpawnedTile(int spawnedTileIndex, bool[] hiddenMask)
        {
            if (spawnedTileIndex < 0 || IsHiddenInMask(hiddenMask, spawnedTileIndex))
            {
                yield break;
            }

            var target = cellBackgrounds[spawnedTileIndex].transform as RectTransform;
            if (target == null)
            {
                yield break;
            }

            for (var elapsed = 0f; elapsed < SpawnPopDuration; elapsed += Time.deltaTime)
            {
                var t = Smooth01(elapsed / SpawnPopDuration);
                var scale = Mathf.Lerp(0.2f, 1.08f, t);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private IEnumerator AnimateCurtainTransition(bool[] beforeHidden, int spawnedTileIndex)
        {
            var afterHidden = CaptureHiddenCells();
            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                var value = board.GetValueAtIndex(i);
                var wasHidden = IsHiddenInMask(beforeHidden, i);
                var nowHidden = IsHiddenInMask(afterHidden, i);
                var overlay = grayOverlays[i];
                var label = cellLabels[i];

                if (nowHidden && value > 0)
                {
                    overlay.gameObject.SetActive(true);
                    overlay.transform.localScale = Vector3.one;
                    overlay.color = new Color(0.12f, 0.13f, 0.14f, wasHidden ? CurtainAlpha(i == spawnedTileIndex) : 0f);
                }
                else if (wasHidden && value > 0)
                {
                    overlay.gameObject.SetActive(true);
                    overlay.transform.localScale = Vector3.one;
                    overlay.color = new Color(0.12f, 0.13f, 0.14f, CurtainAlpha(false));
                }
                else if (value <= 0)
                {
                    overlay.gameObject.SetActive(false);
                }
                else if (!wasHidden)
                {
                    overlay.gameObject.SetActive(false);
                }

                label.text = value == 0 || wasHidden ? string.Empty : value.ToString();
                label.color = LabelColor(value, wasHidden && !nowHidden ? 0f : 1f);
            }

            for (var elapsed = 0f; elapsed < CurtainDuration; elapsed += Time.deltaTime)
            {
                var t = Smooth01(elapsed / CurtainDuration);
                for (var i = 0; i < Blink2048Board.CellCount; i++)
                {
                    var value = board.GetValueAtIndex(i);
                    var wasHidden = IsHiddenInMask(beforeHidden, i);
                    var nowHidden = IsHiddenInMask(afterHidden, i);
                    if (wasHidden == nowHidden)
                    {
                        continue;
                    }

                    if (value == 0)
                    {
                        cellBackgrounds[i].color = nowHidden
                            ? Color.Lerp(TileColor(0), HiddenEmptyColor(), t)
                            : Color.Lerp(HiddenEmptyColor(), TileColor(0), t);
                        continue;
                    }

                    if (nowHidden)
                    {
                        var alpha = Mathf.Lerp(0f, CurtainAlpha(i == spawnedTileIndex), t);
                        grayOverlays[i].color = new Color(0.12f, 0.13f, 0.14f, alpha);
                        cellLabels[i].color = LabelColor(value, 1f - t);
                        continue;
                    }

                    grayOverlays[i].color = new Color(0.12f, 0.13f, 0.14f, Mathf.Lerp(CurtainAlpha(false), 0f, t));
                    cellLabels[i].text = value.ToString();
                    cellLabels[i].color = LabelColor(value, t);
                }

                yield return null;
            }

            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                grayOverlays[i].transform.localScale = Vector3.one;
            }
        }

        private List<TileMotion> BuildTileMotions(int[] beforeValues, Blink2048Direction direction)
        {
            var motions = new List<TileMotion>();
            for (var line = 0; line < Blink2048Board.Size; line++)
            {
                var indices = LineIndices(direction, line);
                var entries = new List<TileEntry>(Blink2048Board.Size);
                foreach (var index in indices)
                {
                    var value = beforeValues[index];
                    if (value > 0)
                    {
                        entries.Add(new TileEntry(index, value));
                    }
                }

                var targetOffset = 0;
                for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    var targetIndex = indices[targetOffset];
                    if (entryIndex + 1 < entries.Count && entries[entryIndex].Value == entries[entryIndex + 1].Value)
                    {
                        var resultValue = entries[entryIndex].Value * 2;
                        motions.Add(new TileMotion(entries[entryIndex].Index, targetIndex, entries[entryIndex].Value, resultValue, true));
                        motions.Add(new TileMotion(entries[entryIndex + 1].Index, targetIndex, entries[entryIndex + 1].Value, resultValue, true));
                        entryIndex++;
                    }
                    else
                    {
                        motions.Add(new TileMotion(entries[entryIndex].Index, targetIndex, entries[entryIndex].Value, entries[entryIndex].Value, false));
                    }

                    targetOffset++;
                }
            }

            return motions;
        }

        private void SyncAnimationLayerToBoard()
        {
            Canvas.ForceUpdateCanvases();
            animationLayerRect.sizeDelta = boardRect.sizeDelta;
            animationLayerRect.anchoredPosition = boardRect.anchoredPosition;
            animationLayerRect.SetAsLastSibling();
        }

        private Vector2 CellPosition(int index)
        {
            return cellRects[index].anchoredPosition;
        }

        private Vector2 CellSize()
        {
            return cellRects[0].rect.size;
        }

        private void ClearAnimationLayer()
        {
            if (animationLayerRect == null)
            {
                return;
            }

            for (var i = animationLayerRect.childCount - 1; i >= 0; i--)
            {
                Destroy(animationLayerRect.GetChild(i).gameObject);
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
            boardRect = rect;

            var grid = boardRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Blink2048Board.Size;
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < Blink2048Board.CellCount; i++)
            {
                CreateCell(boardRoot.transform, i);
            }

            var animationLayer = new GameObject("Tile Animation Layer", typeof(RectTransform));
            animationLayer.transform.SetParent(boardArea.transform, false);
            animationLayerRect = animationLayer.GetComponent<RectTransform>();
            animationLayerRect.anchorMin = new Vector2(0.5f, 0.5f);
            animationLayerRect.anchorMax = new Vector2(0.5f, 0.5f);
            animationLayerRect.pivot = new Vector2(0.5f, 0.5f);
            animationLayerRect.anchoredPosition = Vector2.zero;
        }

        private void CreateCell(Transform parent, int index)
        {
            var cell = new GameObject($"Cell {index:00}", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(parent, false);
            cellRects[index] = cell.GetComponent<RectTransform>();

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

        private static GameObject CreateTileVisual(Transform parent, int value, bool hidden)
        {
            var tile = new GameObject($"Moving Tile {value}", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            tile.GetComponent<Image>().color = hidden ? new Color(0.12f, 0.13f, 0.14f, CurtainAlpha(false)) : TileColor(value);
            AddSketchOutline(tile.transform);

            if (!hidden)
            {
                var label = CreateText(tile.transform, value.ToString(), 76, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                label.color = LabelColor(value, 1f);
                Stretch(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            }

            return tile;
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

        private static float CurtainAlpha(bool isSpawnedTile)
        {
            return isSpawnedTile ? 0.82f : 0.88f;
        }

        private static bool IsHiddenInMask(bool[] hiddenMask, int index)
        {
            return hiddenMask != null && index >= 0 && index < hiddenMask.Length && hiddenMask[index];
        }

        private static Color LabelColor(int value, float alpha)
        {
            var color = value >= 128 ? Color.white : SketchPalette.Ink;
            color.a = alpha;
            return color;
        }

        private static float Smooth01(float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
        }

        private static int[] LineIndices(Blink2048Direction direction, int line)
        {
            var result = new int[Blink2048Board.Size];
            for (var i = 0; i < Blink2048Board.Size; i++)
            {
                switch (direction)
                {
                    case Blink2048Direction.Left:
                        result[i] = line * Blink2048Board.Size + i;
                        break;
                    case Blink2048Direction.Right:
                        result[i] = line * Blink2048Board.Size + (Blink2048Board.Size - 1 - i);
                        break;
                    case Blink2048Direction.Up:
                        result[i] = i * Blink2048Board.Size + line;
                        break;
                    case Blink2048Direction.Down:
                        result[i] = (Blink2048Board.Size - 1 - i) * Blink2048Board.Size + line;
                        break;
                    default:
                        result[i] = i;
                        break;
                }
            }

            return result;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private readonly struct TileEntry
        {
            public TileEntry(int index, int value)
            {
                Index = index;
                Value = value;
            }

            public int Index { get; }

            public int Value { get; }
        }

        private readonly struct TileMotion
        {
            public TileMotion(int sourceIndex, int targetIndex, int sourceValue, int resultValue, bool merged)
            {
                SourceIndex = sourceIndex;
                TargetIndex = targetIndex;
                SourceValue = sourceValue;
                ResultValue = resultValue;
                Merged = merged;
            }

            public int SourceIndex { get; }

            public int TargetIndex { get; }

            public int SourceValue { get; }

            public int ResultValue { get; }

            public bool Merged { get; }
        }

        private readonly struct TileGhost
        {
            public TileGhost(GameObject root, RectTransform rect, TileMotion motion)
            {
                Root = root;
                Rect = rect;
                Motion = motion;
            }

            public GameObject Root { get; }

            public RectTransform Rect { get; }

            public TileMotion Motion { get; }
        }
    }
}
