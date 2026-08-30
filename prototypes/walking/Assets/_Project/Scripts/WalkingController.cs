using System.Collections.Generic;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace MannLab.Games.Walking
{
    public sealed class WalkingController : MonoBehaviour
    {
        public const bool DefaultDebugFootMarkers = false;

        private static readonly Color Paper = new Color32(250, 247, 239, 255);
        private static readonly Color Ink = new Color32(40, 39, 36, 255);
        private static readonly Color FadedInk = new Color32(102, 97, 90, 255);
        private static readonly Color Warm = new Color32(247, 181, 71, 255);
        private static readonly Color Green = new Color32(123, 168, 107, 255);
        private static readonly Color Red = new Color32(210, 74, 66, 255);
        private static readonly Color Blue = new Color32(88, 142, 181, 255);

        [Header("Debug")]
        [SerializeField] private bool debugFootMarkers = DefaultDebugFootMarkers;

        [Header("Camera")]
        [SerializeField] private float eyeHeight = 1.48f;
        [SerializeField] private float cameraMoveLerp = 9.5f;
        [SerializeField] private float cameraTurnLerp = 8f;
        [SerializeField] private float stepBobStrength = 0.055f;

        [Header("World")]
        [SerializeField] private float wallHeight = 2.75f;
        [SerializeField] private float wallInset = 0.02f;

        private readonly FootRuntime leftFoot = new FootRuntime(WalkingFootSide.Left);
        private readonly FootRuntime rightFoot = new FootRuntime(WalkingFootSide.Right);
        private readonly Dictionary<int, FootRuntime> activeTouches = new Dictionary<int, FootRuntime>();

        private WalkingGameState state;
        private WalkingMaze maze;
        private Camera gameCamera;
        private Transform worldRoot;
        private Transform debugRoot;
        private Transform leftMarker;
        private Transform rightMarker;
        private Canvas canvas;
        private Text titleText;
        private Text hintText;
        private Text distanceText;
        private Text bestText;
        private Text stepText;
        private Text resultText;
        private Text leftStatusText;
        private Text rightStatusText;
        private Image leftTouchZone;
        private Image rightTouchZone;
        private Image leftStatusBadge;
        private Image rightStatusBadge;
        private Button restartButton;
        private AudioSource audioSource;
        private AudioClip stepClip;
        private AudioClip bumpClip;
        private GUIStyle hudStyle;
        private GUIStyle smallHudStyle;
        private GUIStyle titleStyle;
        private GUIStyle hintStyle;
        private GUIStyle guideStyle;
        private GUIStyle buttonStyle;

        private Vector2 leftFootPosition;
        private Vector2 rightFootPosition;
        private Vector2 bodyPosition;
        private Vector2 previousBodyPosition;
        private Vector2 facing = Vector2.up;
        private Vector2 cameraBodyPosition;
        private float distanceMeters;
        private float bestDistanceMeters;
        private int steps;
        private float bobImpulse;
        private float invalidPulse;

        private const string BestDistanceKey = "MannLab.Walking.BestDistance";

        private enum InputMode
        {
            Idle,
            Placement,
            Return,
            Ignored
        }

        private sealed class FootRuntime
        {
            public FootRuntime(WalkingFootSide side)
            {
                Side = side;
            }

            public WalkingFootSide Side { get; }
            public bool NeedsReturn { get; set; }
            public InputMode Mode { get; set; }
            public int FingerId { get; set; } = int.MinValue;
            public Vector2 ScreenPosition { get; set; }
            public Vector2 BestStepScreenPosition { get; set; }
            public WalkingFootPlacement Candidate { get; set; }
            public float StatusPulse { get; set; }
        }

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            Input.multiTouchEnabled = true;

            bestDistanceMeters = PlayerPrefs.GetFloat(BestDistanceKey, 0f);
            EnsureSceneObjects();
            BuildUi();
            ResetRun();
        }

        private void Update()
        {
            HandleInput();
            UpdateCandidates();
            UpdateCamera();
            UpdateUi();
            UpdateDebugMarkers();
            bobImpulse = Mathf.MoveTowards(bobImpulse, 0f, Time.deltaTime * 4.5f);
            invalidPulse = Mathf.MoveTowards(invalidPulse, 0f, Time.deltaTime * 4f);
            leftFoot.StatusPulse = Mathf.MoveTowards(leftFoot.StatusPulse, 0f, Time.deltaTime * 4f);
            rightFoot.StatusPulse = Mathf.MoveTowards(rightFoot.StatusPulse, 0f, Time.deltaTime * 4f);
        }

        private void ResetRun()
        {
            var seed = unchecked(System.DateTime.UtcNow.Millisecond * 73856093 ^ Random.Range(1, int.MaxValue));
            maze = WalkingMaze.Generate(WalkingRules.MazeCellColumns, WalkingRules.MazeCellRows, seed, WalkingRules.TileSize);
            BuildWorld();

            bodyPosition = maze.GridToWorld(2, 2);
            leftFootPosition = bodyPosition + new Vector2(-WalkingRules.NaturalHalfStance, 0f);
            rightFootPosition = bodyPosition + new Vector2(WalkingRules.NaturalHalfStance, 0f);
            previousBodyPosition = bodyPosition;
            cameraBodyPosition = bodyPosition;
            facing = Vector2.up;
            distanceMeters = 0f;
            steps = 0;
            bobImpulse = 0f;
            invalidPulse = 0f;
            ResetFootRuntime(leftFoot);
            ResetFootRuntime(rightFoot);
            activeTouches.Clear();
            state = WalkingGameState.Ready;
            UpdateCamera(true);
            UpdateUi();
            UpdateDebugMarkers(true);
        }

        private void ResetFootRuntime(FootRuntime foot)
        {
            foot.NeedsReturn = false;
            foot.Mode = InputMode.Idle;
            foot.FingerId = int.MinValue;
            foot.ScreenPosition = Vector2.zero;
            foot.BestStepScreenPosition = Vector2.zero;
            foot.Candidate = default;
            foot.StatusPulse = 0f;
        }

        private void EnsureSceneObjects()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.GetComponent<Camera>();
            }

            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = Paper;
            gameCamera.fieldOfView = 66f;
            gameCamera.nearClipPlane = 0.04f;
            gameCamera.farClipPlane = 95f;
            if (gameCamera.GetComponent<AudioListener>() == null)
            {
                gameCamera.gameObject.AddComponent<AudioListener>();
            }

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            stepClip = CreateTone("Walking Step", 180f, 0.055f, 0.16f);
            bumpClip = CreateTone("Walking Bump", 76f, 0.12f, 0.22f);

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void BuildWorld()
        {
            if (worldRoot != null)
            {
                Destroy(worldRoot.gameObject);
            }

            worldRoot = new GameObject("Walking World").transform;
            var wallMaterial = CreateMaterial("Ink Wall", Ink);
            var wallEdgeMaterial = CreateMaterial("Wall Edge", new Color32(33, 32, 30, 255));
            var floorMaterial = CreateMaterial("Paper Floor", new Color32(255, 253, 247, 255));
            var lineMaterial = CreateMaterial("Floor Ink Lines", new Color32(186, 181, 170, 255));
            var startMaterial = CreateMaterial("Start Wash", new Color32(240, 246, 226, 255));

            var floorCenter = new Vector3(0f, -0.035f, 0f);
            var floorSize = new Vector3(
                maze.GridWidth * maze.TileSize,
                0.06f,
                maze.GridHeight * maze.TileSize);
            CreateCube("Paper Floor", floorCenter, floorSize, floorMaterial, worldRoot);

            foreach (var tile in maze.SolidTiles())
            {
                var world = maze.GridToWorld(tile.x, tile.y);
                var wall = CreateCube(
                    "Ink Wall",
                    new Vector3(world.x, wallHeight * 0.5f, world.y),
                    new Vector3(maze.TileSize - wallInset, wallHeight, maze.TileSize - wallInset),
                    wallMaterial,
                    worldRoot);
                wall.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                CreateCube(
                    "Wall Top Sketch",
                    new Vector3(world.x, wallHeight + 0.012f, world.y),
                    new Vector3(maze.TileSize * 0.92f, 0.025f, maze.TileSize * 0.92f),
                    wallEdgeMaterial,
                    wall.transform);
            }

            for (var y = 0; y < maze.GridHeight; y++)
            {
                for (var x = 0; x < maze.GridWidth; x++)
                {
                    if (maze.IsSolidGrid(x, y))
                    {
                        continue;
                    }

                    var world = maze.GridToWorld(x, y);
                    var material = x <= 4 && y <= 4 ? startMaterial : lineMaterial;
                    if (!maze.IsSolidGrid(x, y + 1))
                    {
                        CreateCube(
                            "Floor Direction Line",
                            new Vector3(world.x, 0.012f, world.y + maze.TileSize * 0.26f),
                            new Vector3(0.08f, 0.018f, maze.TileSize * 0.46f),
                            material,
                            worldRoot);
                    }

                    if (!maze.IsSolidGrid(x + 1, y))
                    {
                        CreateCube(
                            "Floor Cross Line",
                            new Vector3(world.x + maze.TileSize * 0.26f, 0.014f, world.y),
                            new Vector3(maze.TileSize * 0.46f, 0.018f, 0.055f),
                            lineMaterial,
                            worldRoot);
                    }
                }
            }

            BuildDebugMarkers();
        }

        private void BuildDebugMarkers()
        {
            if (debugRoot != null)
            {
                Destroy(debugRoot.gameObject);
            }

            debugRoot = new GameObject("Debug Foot Markers").transform;
            debugRoot.SetParent(worldRoot, false);
            leftMarker = CreateDebugMarker("Left Foot Debug", new Color32(88, 142, 181, 255));
            rightMarker = CreateDebugMarker("Right Foot Debug", new Color32(247, 181, 71, 255));
            debugRoot.gameObject.SetActive(debugFootMarkers);
        }

        private Transform CreateDebugMarker(string markerName, Color color)
        {
            var marker = CreateCube(
                markerName,
                Vector3.zero,
                new Vector3(0.32f, 0.025f, 0.32f),
                CreateMaterial(markerName + " Material", color),
                debugRoot);
            marker.name = markerName;
            return marker.transform;
        }

        private void BuildUi()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }

        private Image CreateTouchZone(string zoneName, Vector2 min, Vector2 max)
        {
            var zone = new GameObject(zoneName, typeof(Image)).GetComponent<Image>();
            zone.transform.SetParent(canvas.transform, false);
            zone.color = new Color(1f, 1f, 1f, 0.015f);
            zone.raycastTarget = false;
            Stretch(zone.rectTransform, min, max);
            return zone;
        }

        private Image CreatePanel(string panelName, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchored, Vector2 size, Color color)
        {
            var panel = new GameObject(panelName, typeof(Image)).GetComponent<Image>();
            panel.transform.SetParent(canvas.transform, false);
            panel.color = color;
            panel.raycastTarget = false;
            SetRect(panel.rectTransform, min, max, pivot, anchored, size);
            return panel;
        }

        private Image CreateStatusBadge(string badgeName, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchored)
        {
            var badge = new GameObject(badgeName, typeof(Image)).GetComponent<Image>();
            badge.transform.SetParent(canvas.transform, false);
            badge.color = new Color32(255, 253, 247, 222);
            badge.raycastTarget = false;
            SetRect(badge.rectTransform, min, max, pivot, anchored, new Vector2(220f, 62f));
            return badge;
        }

        private Button CreateButton(string buttonName, string label, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
        {
            var buttonImage = new GameObject(buttonName, typeof(Image), typeof(Button)).GetComponent<Image>();
            buttonImage.transform.SetParent(canvas.transform, false);
            buttonImage.color = Warm;
            SetRect(buttonImage.rectTransform, min, max, pivot, Vector2.zero, size);
            var button = buttonImage.GetComponent<Button>();
            var text = CreateText("Label", buttonImage.transform, label, 34, TextAnchor.MiddleCenter, Ink);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return button;
        }

        private Text CreateText(string textName, Transform parent, string textValue, int fontSize, TextAnchor anchor, Color color)
        {
            var text = new GameObject(textName, typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.text = textValue;
            text.color = color;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return text;
        }

        private void HandleInput()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            BeginPointer(touch.fingerId, touch.position);
                            break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            MovePointer(touch.fingerId, touch.position);
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            EndPointer(touch.fingerId, touch.position);
                            break;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginPointer(-10, Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                MovePointer(-10, Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndPointer(-10, Input.mousePosition);
            }
        }

        private void BeginPointer(int pointerId, Vector2 screenPosition)
        {
            if (state == WalkingGameState.Ready)
            {
                state = WalkingGameState.Playing;
            }
            else if (state == WalkingGameState.Result)
            {
                return;
            }

            if (state != WalkingGameState.Playing)
            {
                return;
            }

            var foot = screenPosition.x <= Screen.width * 0.5f ? leftFoot : rightFoot;
            if (foot.Mode != InputMode.Idle)
            {
                return;
            }

            foot.FingerId = pointerId;
            foot.ScreenPosition = screenPosition;
            foot.BestStepScreenPosition = screenPosition;
            activeTouches[pointerId] = foot;

            if (foot.NeedsReturn)
            {
                foot.Mode = WalkingRules.IsReturnGesturePosition(screenPosition, new Vector2(Screen.width, Screen.height))
                    ? InputMode.Return
                    : InputMode.Ignored;
                foot.StatusPulse = 1f;
                return;
            }

            foot.Mode = InputMode.Placement;
        }

        private void MovePointer(int pointerId, Vector2 screenPosition)
        {
            if (!activeTouches.TryGetValue(pointerId, out var foot))
            {
                return;
            }

            if (foot.Mode == InputMode.Placement)
            {
                foot.ScreenPosition = screenPosition;
                if (screenPosition.y > foot.BestStepScreenPosition.y)
                {
                    foot.BestStepScreenPosition = screenPosition;
                }
            }
        }

        private void EndPointer(int pointerId, Vector2 screenPosition)
        {
            if (!activeTouches.TryGetValue(pointerId, out var foot))
            {
                return;
            }

            if (foot.Mode == InputMode.Placement)
            {
                foot.ScreenPosition = screenPosition;
                TryLandFoot(foot);
            }
            else if (foot.Mode == InputMode.Return)
            {
                foot.NeedsReturn = false;
                foot.StatusPulse = 1f;
            }
            else if (foot.Mode == InputMode.Ignored)
            {
                invalidPulse = 1f;
                foot.StatusPulse = 1f;
            }

            foot.Mode = InputMode.Idle;
            foot.FingerId = int.MinValue;
            activeTouches.Remove(pointerId);
        }

        private void UpdateCandidates()
        {
            UpdateCandidate(leftFoot);
            UpdateCandidate(rightFoot);
        }

        private void UpdateCandidate(FootRuntime foot)
        {
            if (foot.Mode != InputMode.Placement)
            {
                foot.Candidate = default;
                return;
            }

            var support = foot.Side == WalkingFootSide.Left ? rightFootPosition : leftFootPosition;
            var candidate = WalkingRules.BuildFootCandidate(
                foot.Side,
                support,
                facing,
                foot.BestStepScreenPosition,
                new Vector2(Screen.width, Screen.height));
            foot.Candidate = WalkingRules.ValidateFootPlacement(foot.Side, support, candidate, facing, maze);
        }

        private void TryLandFoot(FootRuntime foot)
        {
            UpdateCandidate(foot);
            if (!foot.Candidate.IsValid)
            {
                invalidPulse = 1f;
                foot.StatusPulse = 1f;
                PlayBump(0.35f);
                return;
            }

            previousBodyPosition = bodyPosition;
            var oldBody = bodyPosition;
            var oldFoot = foot.Side == WalkingFootSide.Left ? leftFootPosition : rightFootPosition;

            if (foot.Side == WalkingFootSide.Left)
            {
                leftFootPosition = foot.Candidate.Position;
            }
            else
            {
                rightFootPosition = foot.Candidate.Position;
            }

            bodyPosition = WalkingRules.BodyCenter(leftFootPosition, rightFootPosition);
            var footForward = WalkingRules.FacingFromFeet(leftFootPosition, rightFootPosition, facing);
            var stepDirection = bodyPosition - oldBody;
            if (stepDirection.sqrMagnitude > 0.0001f)
            {
                facing = Vector2.Lerp(footForward, stepDirection.normalized, 0.74f).normalized;
            }
            else
            {
                facing = footForward;
            }

            var traveled = Vector2.Distance(oldBody, bodyPosition);
            distanceMeters += traveled;
            steps++;
            foot.NeedsReturn = true;
            foot.StatusPulse = 1f;
            bobImpulse = Mathf.Min(1f, bobImpulse + stepBobStrength + Vector2.Distance(oldFoot, foot.Candidate.Position) * 0.04f);
            PlayStep();

            if (WalkingRules.IsBodyColliding(bodyPosition, maze))
            {
                EndRun();
            }
        }

        private void EndRun()
        {
            state = WalkingGameState.Result;
            PlayBump(1f);
            if (distanceMeters > bestDistanceMeters)
            {
                bestDistanceMeters = distanceMeters;
                PlayerPrefs.SetFloat(BestDistanceKey, bestDistanceMeters);
                PlayerPrefs.Save();
            }

            activeTouches.Clear();
            ResetFootRuntime(leftFoot);
            ResetFootRuntime(rightFoot);
        }

        private void UpdateCamera(bool snap = false)
        {
            if (gameCamera == null)
            {
                return;
            }

            var moveT = snap ? 1f : 1f - Mathf.Exp(-cameraMoveLerp * Time.deltaTime);
            cameraBodyPosition = Vector2.Lerp(cameraBodyPosition, bodyPosition, moveT);
            var bob = Mathf.Sin((1f - bobImpulse) * Mathf.PI) * bobImpulse;
            var targetPosition = new Vector3(cameraBodyPosition.x, eyeHeight + bob, cameraBodyPosition.y);
            gameCamera.transform.position = targetPosition;

            var forward3 = new Vector3(facing.x, 0f, facing.y);
            if (forward3.sqrMagnitude < 0.001f)
            {
                forward3 = Vector3.forward;
            }

            var targetRotation = Quaternion.LookRotation(forward3.normalized, Vector3.up);
            gameCamera.transform.rotation = snap
                ? targetRotation
                : Quaternion.Slerp(gameCamera.transform.rotation, targetRotation, 1f - Mathf.Exp(-cameraTurnLerp * Time.deltaTime));
        }

        private void UpdateUi()
        {
            if (distanceText == null)
            {
                return;
            }

            distanceText.text = $"{distanceMeters:0.0} m";
            bestText.text = $"BEST {bestDistanceMeters:0.0}";
            stepText.text = state == WalkingGameState.Playing ? $"{steps} STEPS" : state.ToString().ToUpperInvariant();

            var ready = state == WalkingGameState.Ready;
            titleText.gameObject.SetActive(ready);
            hintText.gameObject.SetActive(ready);
            titleText.text = "Thumbwalk";
            hintText.text = "Step high. Pull low. Repeat.";

            resultText.gameObject.SetActive(state == WalkingGameState.Result);
            restartButton.gameObject.SetActive(state == WalkingGameState.Result);
            if (state == WalkingGameState.Result)
            {
                resultText.text = $"Result\n{distanceMeters:0.0} m\nBest {bestDistanceMeters:0.0} m";
            }

            ApplyFootStatus(leftFoot, leftStatusText, leftStatusBadge, leftTouchZone);
            ApplyFootStatus(rightFoot, rightStatusText, rightStatusBadge, rightTouchZone);
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            var scale = Mathf.Clamp(Screen.width / 720f, 0.8f, 1.55f);
            var margin = 22f * scale;
            var topHeight = 68f * scale;
            if (state != WalkingGameState.Result)
            {
                DrawInputGuide(scale, state == WalkingGameState.Ready || steps < 8 || leftFoot.NeedsReturn || rightFoot.NeedsReturn);
            }

            DrawGuiRect(new Rect(margin, margin, Screen.width - margin * 2f, topHeight), new Color(1f, 0.99f, 0.96f, 0.82f));
            GUI.Label(new Rect(margin + 18f * scale, margin + 12f * scale, 240f * scale, topHeight), $"{distanceMeters:0.0} m", hudStyle);
            GUI.Label(new Rect(Screen.width - margin - 260f * scale, margin + 10f * scale, 240f * scale, topHeight), $"BEST {bestDistanceMeters:0.0}\n{steps} STEPS", smallHudStyle);

            DrawFootBadge(leftFoot, new Rect(margin, Screen.height - 88f * scale, 170f * scale, 50f * scale), scale);
            DrawFootBadge(rightFoot, new Rect(Screen.width - margin - 170f * scale, Screen.height - 88f * scale, 170f * scale, 50f * scale), scale);

            if (state == WalkingGameState.Ready)
            {
                DrawReadyCoach(scale);
            }
            else if (state == WalkingGameState.Result)
            {
                DrawGuiRect(new Rect(Screen.width * 0.18f, Screen.height * 0.27f, Screen.width * 0.64f, 240f * scale), new Color(1f, 0.99f, 0.96f, 0.9f));
                GUI.Label(new Rect(Screen.width * 0.18f, Screen.height * 0.28f, Screen.width * 0.64f, 110f * scale), $"Result\n{distanceMeters:0.0} m", titleStyle);
                if (GUI.Button(new Rect(Screen.width * 0.35f, Screen.height * 0.43f, Screen.width * 0.3f, 56f * scale), "Restart", buttonStyle))
                {
                    ResetRun();
                }
            }
        }

        private void DrawInputGuide(float scale, bool showLabels)
        {
            var splitTop = Screen.height * 0.16f;
            var returnTop = Screen.height * (1f - WalkingRules.ReturnGestureMaxScreenY);
            var lowHeight = Screen.height - returnTop;
            var leftLowColor = leftFoot.NeedsReturn || leftFoot.Mode == InputMode.Return
                ? new Color(Blue.r, Blue.g, Blue.b, 0.13f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.055f);
            var rightLowColor = rightFoot.NeedsReturn || rightFoot.Mode == InputMode.Return
                ? new Color(Blue.r, Blue.g, Blue.b, 0.13f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.055f);

            DrawGuiRect(new Rect(0f, returnTop, Screen.width * 0.5f, lowHeight), leftLowColor);
            DrawGuiRect(new Rect(Screen.width * 0.5f, returnTop, Screen.width * 0.5f, lowHeight), rightLowColor);
            DrawGuiRect(new Rect(Screen.width * 0.5f - 1f * scale, splitTop, 2f * scale, Screen.height - splitTop), new Color(Ink.r, Ink.g, Ink.b, 0.13f));
            DrawGuiRect(new Rect(0f, returnTop, Screen.width, 2f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.12f));

            if (!showLabels)
            {
                return;
            }

            DrawGuideChip(new Rect(26f * scale, Screen.height * 0.18f, 150f * scale, 38f * scale), "STEP HIGH", Warm, scale);
            DrawGuideChip(new Rect(Screen.width - 176f * scale, Screen.height * 0.18f, 150f * scale, 38f * scale), "STEP HIGH", Warm, scale);
            DrawGuideChip(new Rect(26f * scale, Screen.height - 150f * scale, 160f * scale, 38f * scale), "PULL LOW", Blue, scale);
            DrawGuideChip(new Rect(Screen.width - 186f * scale, Screen.height - 150f * scale, 160f * scale, 38f * scale), "PULL LOW", Blue, scale);
        }

        private void DrawReadyCoach(float scale)
        {
            var panel = new Rect(Screen.width * 0.09f, Screen.height * 0.20f, Screen.width * 0.82f, Screen.height * 0.39f);
            DrawGuiRect(panel, new Color(1f, 0.99f, 0.96f, 0.9f));
            GUI.Label(new Rect(panel.x + 18f * scale, panel.y + 12f * scale, panel.width - 36f * scale, 44f * scale), "Tap high. Lift. Pull low.", hintStyle);

            var gap = 18f * scale;
            var laneTop = panel.y + 68f * scale;
            var laneHeight = panel.height - 86f * scale;
            var laneWidth = (panel.width - gap * 3f) * 0.5f;
            var leftLane = new Rect(panel.x + gap, laneTop, laneWidth, laneHeight);
            var rightLane = new Rect(panel.x + gap * 2f + laneWidth, laneTop, laneWidth, laneHeight);
            DrawThumbLoop(leftLane, "LEFT", 0f, scale);
            DrawThumbLoop(rightLane, "RIGHT", 0.5f, scale);
        }

        private void DrawThumbLoop(Rect lane, string label, float phaseOffset, float scale)
        {
            DrawGuiRect(lane, new Color(Ink.r, Ink.g, Ink.b, 0.045f));
            GUI.Label(new Rect(lane.x, lane.y + 6f * scale, lane.width, 28f * scale), label, guideStyle);

            var topTarget = new Rect(lane.x + lane.width * 0.18f, lane.y + 42f * scale, lane.width * 0.64f, 30f * scale);
            var bottomTarget = new Rect(lane.x + lane.width * 0.18f, lane.yMax - 42f * scale, lane.width * 0.64f, 30f * scale);
            DrawGuideChip(topTarget, "STEP", Warm, scale);
            DrawGuideChip(bottomTarget, "PULL", Blue, scale);

            var pathX = lane.center.x - 3f * scale;
            DrawGuiRect(new Rect(pathX, topTarget.yMax, 6f * scale, bottomTarget.y - topTarget.yMax), new Color(Ink.r, Ink.g, Ink.b, 0.12f));

            var phase = Mathf.Repeat(Time.unscaledTime * 0.72f + phaseOffset, 1f);
            var upPhase = phase < 0.46f;
            var t = upPhase ? phase / 0.46f : (phase - 0.46f) / 0.54f;
            t = Mathf.SmoothStep(0f, 1f, t);
            var thumbY = upPhase
                ? Mathf.Lerp(bottomTarget.center.y, topTarget.center.y, t)
                : Mathf.Lerp(topTarget.center.y, bottomTarget.center.y, t);
            var thumbColor = upPhase ? Warm : Blue;
            var thumb = new Rect(lane.center.x - 19f * scale, thumbY - 19f * scale, 38f * scale, 38f * scale);
            DrawGuiRect(thumb, new Color(thumbColor.r, thumbColor.g, thumbColor.b, 0.92f));
            GUI.Label(new Rect(thumb.x, thumb.y + 5f * scale, thumb.width, thumb.height), upPhase ? "TAP" : "LOW", guideStyle);
        }

        private void DrawGuideChip(Rect rect, string text, Color color, float scale)
        {
            DrawGuiRect(rect, new Color(color.r, color.g, color.b, 0.78f));
            GUI.Label(new Rect(rect.x, rect.y + 3f * scale, rect.width, rect.height), text, guideStyle);
        }

        private void DrawFootBadge(FootRuntime foot, Rect rect, float scale)
        {
            DrawGuiRect(rect, FootStatusColor(foot));
            GUI.Label(new Rect(rect.x, rect.y + 7f * scale, rect.width, rect.height), FootStatusText(foot), smallHudStyle);
        }

        private static string FootStatusText(FootRuntime foot)
        {
            if (foot.Mode == InputMode.Placement)
            {
                return foot.Candidate.IsValid ? "STEP OK" : "NO STEP";
            }

            if (foot.Mode == InputMode.Return)
            {
                return "RETURNING";
            }

            return foot.NeedsReturn ? "PULL LOW" : "STEP";
        }

        private static Color FootStatusColor(FootRuntime foot)
        {
            if (foot.Mode == InputMode.Placement)
            {
                return foot.Candidate.IsValid
                    ? new Color(Green.r, Green.g, Green.b, 0.82f)
                    : new Color(Red.r, Red.g, Red.b, 0.86f);
            }

            if (foot.Mode == InputMode.Return)
            {
                return new Color(Blue.r, Blue.g, Blue.b, 0.84f);
            }

            return foot.NeedsReturn
                ? new Color(Warm.r, Warm.g, Warm.b, 0.86f)
                : new Color(1f, 0.99f, 0.96f, 0.75f);
        }

        private void EnsureGuiStyles()
        {
            if (hudStyle != null)
            {
                return;
            }

            hudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(30f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            smallHudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(44f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f),
                wordWrap = true,
                normal = { textColor = Ink }
            };
            guideStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Ink }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(22f),
                fontStyle = FontStyle.Bold
            };
        }

        private static void DrawGuiRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void ApplyFootStatus(FootRuntime foot, Text label, Image badge, Image zone)
        {
            var color = new Color32(255, 253, 247, 210);
            var zoneColor = new Color(1f, 1f, 1f, 0.02f);
            var text = foot.Side == WalkingFootSide.Left ? "LEFT" : "RIGHT";

            if (foot.Mode == InputMode.Placement)
            {
                if (foot.Candidate.IsValid)
                {
                    text = "STEP OK";
                    color = WithAlpha(Green, 226);
                    zoneColor = new Color(Green.r, Green.g, Green.b, 0.09f);
                }
                else
                {
                    text = "NO STEP";
                    color = WithAlpha(Red, 226);
                    zoneColor = new Color(Red.r, Red.g, Red.b, 0.11f);
                }
            }
            else if (foot.Mode == InputMode.Return)
            {
                text = "RETURNING";
                color = WithAlpha(Blue, 226);
                zoneColor = new Color(Blue.r, Blue.g, Blue.b, 0.10f);
            }
            else if (foot.NeedsReturn)
            {
                text = "PULL LOW";
                color = WithAlpha(Warm, 226);
                zoneColor = new Color(Warm.r, Warm.g, Warm.b, 0.11f);
            }
            else
            {
                text = "STEP";
            }

            label.text = text;
            badge.color = Color.Lerp(color, Color.white, foot.StatusPulse * 0.12f);
            if (zone != null)
            {
                zone.color = zoneColor;
            }
        }

        private void UpdateDebugMarkers(bool snap = false)
        {
            if (debugRoot == null)
            {
                return;
            }

            debugRoot.gameObject.SetActive(debugFootMarkers);
            if (!debugFootMarkers)
            {
                return;
            }

            SetMarker(leftMarker, leftFootPosition, snap);
            SetMarker(rightMarker, rightFootPosition, snap);
        }

        private static void SetMarker(Transform marker, Vector2 position, bool snap)
        {
            if (marker == null)
            {
                return;
            }

            var target = new Vector3(position.x, 0.04f, position.y);
            marker.position = snap ? target : Vector3.Lerp(marker.position, target, 18f * Time.deltaTime);
        }

        private void PlayStep()
        {
            if (audioSource != null && stepClip != null)
            {
                audioSource.PlayOneShot(stepClip, 0.34f);
            }
        }

        private void PlayBump(float volume)
        {
            if (audioSource != null && bumpClip != null)
            {
                audioSource.PlayOneShot(bumpClip, Mathf.Clamp01(volume) * 0.45f);
            }
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - t / duration);
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * envelope;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var cube = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshFilter>().sharedMesh = CreateUnitCubeMesh();
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Mesh CreateUnitCubeMesh()
        {
            var mesh = new Mesh
            {
                name = "Walking Unit Cube"
            };

            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
            };

            mesh.vertices = vertices;
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                1, 2, 6, 1, 6, 5,
                5, 6, 7, 5, 7, 4,
                4, 7, 3, 4, 3, 0,
                3, 7, 6, 3, 6, 2,
                4, 0, 1, 4, 1, 5
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader =
                Shader.Find("Hidden/Internal-Colored") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("UI/Default") ??
                Shader.Find("Standard");
            var material = new Material(shader);
            material.name = materialName;
            material.color = color;
            material.mainTexture = Texture2D.whiteTexture;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private static Color WithAlpha(Color color, byte alpha)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(color.r * 255f),
                (byte)Mathf.RoundToInt(color.g * 255f),
                (byte)Mathf.RoundToInt(color.b * 255f),
                alpha);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchored, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.one);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
