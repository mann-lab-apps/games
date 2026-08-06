using System.Collections;
using System.Collections.Generic;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Game2048Crash
{
    public sealed class Crash2048Controller : MonoBehaviour
    {
        private const string BestStageKey = "mannlab.2048_crash.best_stage";
        private const float SwipeThreshold = 72f;
        private const float SlideAnimationSeconds = 0.16f;
        private const float CrashAnimationSeconds = 0.2f;
        private const float SpawnAnimationSeconds = 0.14f;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private const int CrashlyticsTestTapCount = 7;
        private const float CrashlyticsTestTapWindowSeconds = 2.5f;
        private const float CrashlyticsTestTapZoneSize = 220f;
#endif
        private readonly Image[] cellBackgrounds = new Image[Crash2048Board.CellCount];
        private readonly Image[] cellHighlights = new Image[Crash2048Board.CellCount];
        private readonly Text[] cellLabels = new Text[Crash2048Board.CellCount];
        private readonly RectTransform[] cellRects = new RectTransform[Crash2048Board.CellCount];
        private readonly SketchHatchFillGraphic[] specialHatches = new SketchHatchFillGraphic[Crash2048Board.CellCount];
        private readonly Crash2048Board board = new Crash2048Board();

        private RectTransform overlayRoot;
        private Text stageText;
        private Text targetText;
        private Text bestText;
        private Text resultTitleText;
        private Text resultScoreText;
        private GameObject resultPanel;
        private int bestStage;
        private bool gameOver;
        private bool inputLocked;
        private bool pointerActive;
        private bool runStarted;
        private Vector2 pointerStart;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int crashlyticsTestTapCount;
        private float crashlyticsTestTapDeadline;
#endif

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            bestStage = PlayerPrefs.GetInt(BestStageKey, 0);
            FirebaseTelemetry.Initialize();
            FirebaseTelemetry.SetContext("game", "2048-crash");
            FirebaseTelemetry.LogEvent("app_open");
            BuildInterface();
            StartRun();
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (HandleCrashlyticsTestTrigger())
            {
                return;
            }
#endif

            if (gameOver || inputLocked)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                TryMove(Crash2048Direction.Left);
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                TryMove(Crash2048Direction.Right);
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                TryMove(Crash2048Direction.Up);
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                TryMove(Crash2048Direction.Down);
                return;
            }

            ReadPointerSwipe();
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
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

            crashlyticsTestTapDeadline = Time.unscaledTime + CrashlyticsTestTapWindowSeconds;
            crashlyticsTestTapCount++;

            if (crashlyticsTestTapCount < CrashlyticsTestTapCount)
            {
                return true;
            }

            crashlyticsTestTapCount = 0;
            FirebaseTelemetry.SetContext("crashlytics_test", "true");
            FirebaseTelemetry.LogEvent(
                "crashlytics_test_trigger",
                new Dictionary<string, string>
                {
                    { "stage", board.Stage.ToString() },
                    { "target_value", board.SpecialValue.ToString() }
                });
            FirebaseTelemetry.ForceCrashForTesting();
            return true;
        }

        private static bool TryReadCrashlyticsTestTap(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = Vector2.zero;
            return false;
        }
#endif

        private void StartRun()
        {
            StopAllCoroutines();
            if (runStarted)
            {
                FirebaseTelemetry.LogEvent(
                    "restart",
                    new Dictionary<string, string>
                    {
                        { "stage", board.Stage.ToString() },
                        { "best_stage", bestStage.ToString() }
                    });
            }

            runStarted = true;
            gameOver = false;
            inputLocked = false;
            resultPanel.SetActive(false);
            board.StartNew();
            UpdateHeader();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "run_start",
                new Dictionary<string, string>
                {
                    { "target_value", board.SpecialValue.ToString() },
                    { "best_stage", bestStage.ToString() }
                });
            UpdateBoard();
        }

        private void TryMove(Crash2048Direction direction)
        {
            if (inputLocked)
            {
                return;
            }

            var previousSpecialValue = board.SpecialValue;
            var result = board.Move(direction);
            if (!result.Moved)
            {
                if (result.GameOver)
                {
                    EndRun();
                }

                return;
            }

            if (result.SpecialCrashed)
            {
                FirebaseTelemetry.LogEvent(
                    "special_crash",
                    new Dictionary<string, string>
                    {
                        { "stage", board.Stage.ToString() },
                        { "crashed_value", previousSpecialValue.ToString() },
                        { "next_target_value", board.SpecialValue.ToString() }
                    });
            }

            UpdateTelemetryContext();
            StartCoroutine(AnimateMove(result, previousSpecialValue));
        }

        private IEnumerator AnimateMove(Crash2048MoveResult result, int previousSpecialValue)
        {
            inputLocked = true;
            UpdateHeader();

            var hiddenNormalIndices = new bool[Crash2048Board.CellCount];
            foreach (var motion in result.Motions)
            {
                hiddenNormalIndices[motion.ToIndex] = true;
            }

            if (result.SpawnedTileIndex >= 0)
            {
                hiddenNormalIndices[result.SpawnedTileIndex] = true;
            }

            UpdateBoard(hiddenNormalIndices, result.SpecialCrashed ? result.NewSpecialIndex : -1);
            Canvas.ForceUpdateCanvases();

            var motionViews = new TileMotionView[result.Motions.Length];
            for (var i = 0; i < result.Motions.Length; i++)
            {
                var motion = result.Motions[i];
                var view = CreateTileOverlay(motion.FromIndex, motion.FromValue, false);
                motionViews[i] = new TileMotionView(
                    view,
                    view.GetComponent<RectTransform>(),
                    view.GetComponent<CanvasGroup>(),
                    view.GetComponentInChildren<Text>(),
                    CellPosition(motion.FromIndex),
                    CellPosition(motion.ToIndex),
                    motion);
            }

            GameObject crashOverlay = null;
            RectTransform crashRect = null;
            CanvasGroup crashGroup = null;
            if (result.SpecialCrashed && result.PreviousSpecialIndex >= 0)
            {
                crashOverlay = CreateTileOverlay(result.PreviousSpecialIndex, previousSpecialValue, true);
                crashRect = crashOverlay.GetComponent<RectTransform>();
                crashGroup = crashOverlay.GetComponent<CanvasGroup>();
            }

            var elapsed = 0f;
            while (elapsed < SlideAnimationSeconds)
            {
                elapsed += Time.deltaTime;
                var progress = Smooth(Mathf.Clamp01(elapsed / SlideAnimationSeconds));
                foreach (var view in motionViews)
                {
                    view.Rect.anchoredPosition = Vector2.Lerp(view.Start, view.End, progress);
                }

                yield return null;
            }

            foreach (var view in motionViews)
            {
                view.Rect.anchoredPosition = view.End;
                view.Text.text = view.Motion.ToValue.ToString();
            }

            if (result.SpecialCrashed)
            {
                yield return AnimateCrash(result, crashRect, crashGroup, motionViews);
            }

            foreach (var view in motionViews)
            {
                Destroy(view.GameObject);
            }

            if (crashOverlay != null)
            {
                Destroy(crashOverlay);
            }

            var hiddenSpawnIndex = new bool[Crash2048Board.CellCount];
            if (result.SpawnedTileIndex >= 0)
            {
                hiddenSpawnIndex[result.SpawnedTileIndex] = true;
            }

            UpdateBoard(hiddenSpawnIndex, result.SpecialCrashed ? result.NewSpecialIndex : -1);
            yield return AnimateSpawns(result);

            UpdateBoard();

            inputLocked = false;
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
            inputLocked = false;
            if (board.Stage > bestStage)
            {
                bestStage = board.Stage;
                PlayerPrefs.SetInt(BestStageKey, bestStage);
                PlayerPrefs.Save();
            }

            UpdateHeader();
            UpdateTelemetryContext();
            FirebaseTelemetry.LogEvent(
                "run_end",
                new Dictionary<string, string>
                {
                    { "stage", board.Stage.ToString() },
                    { "best_stage", bestStage.ToString() },
                    { "target_value", board.SpecialValue.ToString() }
                });
            resultTitleText.text = "Game Over";
            resultScoreText.text = $"Stage {board.Stage}\nBest {bestStage}";
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
                TryMove(delta.x < 0f ? Crash2048Direction.Left : Crash2048Direction.Right);
                return;
            }

            TryMove(delta.y < 0f ? Crash2048Direction.Down : Crash2048Direction.Up);
        }

        private void UpdateHeader()
        {
            stageText.text = $"Stage {board.Stage}";
            targetText.text = $"Crash {board.SpecialValue}";
            bestText.text = $"Best {bestStage}";
        }

        private void UpdateTelemetryContext()
        {
            FirebaseTelemetry.SetContext("stage", board.Stage.ToString());
            FirebaseTelemetry.SetContext("target_value", board.SpecialValue.ToString());
            FirebaseTelemetry.SetContext("special_index", board.SpecialIndex.ToString());
            FirebaseTelemetry.SetContext("best_stage", bestStage.ToString());
            FirebaseTelemetry.SetContext("game_over", gameOver ? "true" : "false");
        }

        private IEnumerator AnimateCrash(Crash2048MoveResult result, RectTransform crashRect, CanvasGroup crashGroup, TileMotionView[] motionViews)
        {
            if (result.PreviousSpecialIndex >= 0)
            {
                cellHighlights[result.PreviousSpecialIndex].color = new Color(1f, 0.84f, 0.25f, 0.42f);
            }

            var elapsed = 0f;
            while (elapsed < CrashAnimationSeconds)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / CrashAnimationSeconds);
                var punch = Mathf.Sin(progress * Mathf.PI);
                if (crashRect != null)
                {
                    crashRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.24f, punch);
                    crashRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-7f, 8f, punch));
                }

                if (crashGroup != null)
                {
                    crashGroup.alpha = 1f - progress;
                }

                foreach (var view in motionViews)
                {
                    if (!view.Motion.CrashedSpecial && !view.Motion.Merged)
                    {
                        continue;
                    }

                    if (view.Motion.CrashedSpecial)
                    {
                        view.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.42f, progress);
                        view.Group.alpha = 1f - progress;
                        continue;
                    }

                    view.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, punch);
                }

                yield return null;
            }

            if (result.PreviousSpecialIndex >= 0)
            {
                cellHighlights[result.PreviousSpecialIndex].color = Color.clear;
            }
        }

        private IEnumerator AnimateSpawns(Crash2048MoveResult result)
        {
            var spawnCount = 0;
            if (result.SpawnedTileIndex >= 0)
            {
                spawnCount++;
            }

            if (result.SpecialCrashed && result.NewSpecialIndex >= 0)
            {
                spawnCount++;
            }

            if (spawnCount == 0)
            {
                yield break;
            }

            var spawnViews = new GameObject[spawnCount];
            var spawnRects = new RectTransform[spawnCount];
            var spawnGroups = new CanvasGroup[spawnCount];
            var cursor = 0;

            if (result.SpecialCrashed && result.NewSpecialIndex >= 0)
            {
                var special = CreateTileOverlay(result.NewSpecialIndex, board.SpecialValue, true);
                spawnViews[cursor] = special;
                spawnRects[cursor] = special.GetComponent<RectTransform>();
                spawnGroups[cursor] = special.GetComponent<CanvasGroup>();
                cursor++;
            }

            if (result.SpawnedTileIndex >= 0)
            {
                var tile = CreateTileOverlay(result.SpawnedTileIndex, result.SpawnedTileValue, false);
                spawnViews[cursor] = tile;
                spawnRects[cursor] = tile.GetComponent<RectTransform>();
                spawnGroups[cursor] = tile.GetComponent<CanvasGroup>();
            }

            foreach (var rect in spawnRects)
            {
                rect.localScale = Vector3.one * 0.62f;
            }

            foreach (var group in spawnGroups)
            {
                group.alpha = 0f;
            }

            var elapsed = 0f;
            while (elapsed < SpawnAnimationSeconds)
            {
                elapsed += Time.deltaTime;
                var progress = Smooth(Mathf.Clamp01(elapsed / SpawnAnimationSeconds));
                for (var i = 0; i < spawnRects.Length; i++)
                {
                    spawnRects[i].localScale = Vector3.one * Mathf.Lerp(0.62f, 1f, progress);
                    spawnGroups[i].alpha = progress;
                }

                yield return null;
            }

            foreach (var view in spawnViews)
            {
                Destroy(view);
            }
        }

        private void UpdateBoard(bool[] hiddenNormalIndices = null, int hiddenSpecialIndex = -1)
        {
            for (var i = 0; i < Crash2048Board.CellCount; i++)
            {
                var label = cellLabels[i];
                var background = cellBackgrounds[i];
                cellHighlights[i].color = Color.clear;
                specialHatches[i].gameObject.SetActive(false);

                if (board.IsSpecialAtIndex(i))
                {
                    if (i == hiddenSpecialIndex)
                    {
                        label.text = string.Empty;
                        background.color = TileColor(0);
                        continue;
                    }

                    label.text = board.SpecialValue.ToString();
                    label.color = SketchPalette.Ink;
                    background.color = SketchPalette.HatchPaper;
                    specialHatches[i].gameObject.SetActive(true);
                    continue;
                }

                var value = board.GetValueAtIndex(i);
                var hidden = hiddenNormalIndices != null && hiddenNormalIndices[i];
                label.text = value == 0 || hidden ? string.Empty : value.ToString();
                label.color = value >= 128 ? Color.white : SketchPalette.Ink;
                background.color = hidden ? TileColor(0) : TileColor(value);
            }
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            CreateTitle(canvas.transform);
            CreateHeader(canvas.transform);
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
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            background.GetComponent<Image>().color = SketchPalette.Paper;
        }

        private static void CreateTitle(Transform parent)
        {
            var title = CreateText(parent, "2048 Crash", 72, TextAnchor.MiddleCenter);
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

            stageText = CreateText(header.transform, "Stage 0", 40, TextAnchor.MiddleLeft);
            var stageRect = stageText.GetComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0f, 0f);
            stageRect.anchorMax = new Vector2(0.33f, 1f);
            stageRect.offsetMin = Vector2.zero;
            stageRect.offsetMax = Vector2.zero;

            targetText = CreateText(header.transform, "Crash 2", 46, TextAnchor.MiddleCenter);
            var targetRect = targetText.GetComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0.28f, 0f);
            targetRect.anchorMax = new Vector2(0.72f, 1f);
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;

            bestText = CreateText(header.transform, "Best 0", 40, TextAnchor.MiddleRight);
            var bestRect = bestText.GetComponent<RectTransform>();
            bestRect.anchorMin = new Vector2(0.67f, 0f);
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
                typeof(CrashBoardSizeFitter),
                typeof(GridLayoutGroup),
                typeof(CrashBoardGridCellSizer));
            boardRoot.transform.SetParent(boardArea.transform, false);
            var rect = boardRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var grid = boardRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Crash2048Board.Size;
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < Crash2048Board.CellCount; i++)
            {
                CreateCell(boardRoot.transform, i);
            }

            var overlay = new GameObject("Tile Overlay", typeof(RectTransform));
            overlay.transform.SetParent(boardArea.transform, false);
            overlayRoot = overlay.GetComponent<RectTransform>();
            Stretch(overlayRoot, Vector2.zero, Vector2.zero);
            overlayRoot.SetAsLastSibling();
        }

        private void CreateCell(Transform parent, int index)
        {
            var cell = new GameObject($"Cell {index:00}", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(parent, false);

            var background = cell.GetComponent<Image>();
            background.color = TileColor(0);

            var specialHatch = CreateSpecialHatch(cell.transform, index);
            specialHatch.gameObject.SetActive(false);

            var highlight = new GameObject("Crash Highlight", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(cell.transform, false);
            Stretch(highlight.GetComponent<RectTransform>(), new Vector2(8f, 8f), new Vector2(-8f, -8f));
            var highlightImage = highlight.GetComponent<Image>();
            highlightImage.color = Color.clear;
            highlightImage.raycastTarget = false;

            var label = CreateText(cell.transform, string.Empty, 76, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            Stretch(label.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            AddSketchOutline(cell.transform);

            cellBackgrounds[index] = background;
            cellHighlights[index] = highlightImage;
            cellLabels[index] = label;
            cellRects[index] = cell.GetComponent<RectTransform>();
            specialHatches[index] = specialHatch;
        }

        private GameObject CreateTileOverlay(int index, int value, bool special)
        {
            var overlay = new GameObject($"Moving Tile {value}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(overlayRoot, false);

            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = CellSize(index);
            rect.anchoredPosition = CellPosition(index);

            overlay.GetComponent<Image>().color = special ? SketchPalette.HatchPaper : TileColor(value);
            if (special)
            {
                CreateSpecialHatch(overlay.transform, index + 37);
            }

            AddSketchOutline(overlay.transform);

            var text = CreateText(overlay.transform, value.ToString(), 76, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            text.color = special ? SketchPalette.Ink : value >= 128 ? Color.white : SketchPalette.Ink;
            Stretch(text.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            return overlay;
        }

        private static SketchHatchFillGraphic CreateSpecialHatch(Transform parent, int seed)
        {
            var hatchObject = new GameObject("Sketch Hatch Fill", typeof(RectTransform), typeof(SketchHatchFillGraphic));
            hatchObject.transform.SetParent(parent, false);
            Stretch(hatchObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var hatch = hatchObject.GetComponent<SketchHatchFillGraphic>();
            hatch.BackgroundColor = SketchPalette.HatchPaper;
            hatch.HatchColor = SketchPalette.HatchBlue;
            hatch.Inset = 10f;
            hatch.Spacing = 17f;
            hatch.Thickness = 2.4f;
            hatch.Jitter = 2.2f;
            hatch.Strokes = 2;
            hatch.Seed = seed * 17 + 23;
            hatch.raycastTarget = false;
            return hatch;
        }

        private Vector2 CellPosition(int index)
        {
            var corners = new Vector3[4];
            cellRects[index].GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) * 0.5f;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPoint, null, out var localPoint);
            return localPoint;
        }

        private Vector2 CellSize(int index)
        {
            return cellRects[index].rect.size;
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

            resultScoreText = CreateText(resultPanel.transform, "Stage 0\nBest 0", 40, TextAnchor.MiddleCenter);
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

        private static Color TileColor(int value)
        {
            switch (value)
            {
                case 0:
                    return new Color32(232, 225, 211, 255);
                case 2:
                    return new Color32(248, 245, 236, 255);
                case 4:
                    return new Color32(238, 231, 205, 255);
                case 8:
                    return new Color32(240, 183, 120, 255);
                case 16:
                    return new Color32(230, 145, 101, 255);
                case 32:
                    return new Color32(219, 109, 97, 255);
                case 64:
                    return new Color32(206, 82, 78, 255);
                case 128:
                    return new Color32(117, 156, 185, 255);
                case 256:
                    return new Color32(70, 134, 158, 255);
                case 512:
                    return new Color32(92, 122, 97, 255);
                case 1024:
                    return new Color32(116, 93, 142, 255);
                default:
                    return new Color32(54, 67, 88, 255);
            }
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private readonly struct TileMotionView
        {
            public TileMotionView(
                GameObject gameObject,
                RectTransform rect,
                CanvasGroup group,
                Text text,
                Vector2 start,
                Vector2 end,
                Crash2048TileMotion motion)
            {
                GameObject = gameObject;
                Rect = rect;
                Group = group;
                Text = text;
                Start = start;
                End = end;
                Motion = motion;
            }

            public GameObject GameObject { get; }

            public RectTransform Rect { get; }

            public CanvasGroup Group { get; }

            public Text Text { get; }

            public Vector2 Start { get; }

            public Vector2 End { get; }

            public Crash2048TileMotion Motion { get; }
        }
    }
}
