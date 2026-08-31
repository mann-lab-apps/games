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
        private static Mesh sharedCubeMesh;
        private static Mesh sharedSphereMesh;

        [Header("Debug")]
        [SerializeField] private bool debugFootMarkers = DefaultDebugFootMarkers;

        [Header("Camera")]
        [SerializeField] private float eyeHeight = 1.48f;
        [SerializeField] private float cameraMoveLerp = 5.8f;
        [SerializeField] private float cameraTurnLerp = 8f;
        [SerializeField] private float stepBobStrength = 0.055f;
        [SerializeField] private float thirdPersonDistance = 2.35f;
        [SerializeField] private float thirdPersonHeight = 1.92f;
        [SerializeField] private float thirdPersonLookAhead = 1.02f;
        [SerializeField] private float avatarMoveLerp = 5.4f;
        [SerializeField] private float avatarTurnLerp = 7.2f;

        [Header("World")]
        [SerializeField] private float runDurationSeconds = 30f;
        [SerializeField] private float openFieldHalfWidth = 18f;
        [SerializeField] private float openFieldLength = 180f;
        [SerializeField] private int openFieldObstacleCount = 12;

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
        private Transform playerRoot;
        private Transform playerBody;
        private Transform playerBackMark;
        private Transform playerHead;
        private Transform playerFacePatch;
        private Transform playerBeak;
        private Transform playerLeftArm;
        private Transform playerRightArm;
        private Transform playerLeftFoot;
        private Transform playerRightFoot;
        private Transform playerShadow;
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
        private Texture2D circleTexture;
        private Texture2D ringTexture;
        private readonly List<StepStamp> stepStamps = new List<StepStamp>();
        private readonly List<FieldObstacle> fieldObstacles = new List<FieldObstacle>();

        private Vector2 leftFootPosition;
        private Vector2 rightFootPosition;
        private Vector2 bodyPosition;
        private Vector2 previousBodyPosition;
        private Vector2 facing = Vector2.up;
        private Vector2 visualBodyPosition;
        private Vector2 visualFacing = Vector2.up;
        private Vector2 cameraBodyPosition;
        private float distanceMeters;
        private float bestDistanceMeters;
        private float runTimeRemaining;
        private int steps;
        private float bobImpulse;
        private float invalidPulse;
        private WalkingFootSide lastLandedSide = WalkingFootSide.Left;
        private float bodyLeanPulse;
        private float suppressMouseInputUntil;

        private const string BestDistanceKey = "MannLab.Walking.BestDistance";

        private enum InputMode
        {
            Idle,
            Placement,
            Return,
            Ignored,
            LandedHold,
            InvalidHold
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

        private sealed class StepStamp
        {
            public Transform Root { get; set; }
            public Material Material { get; set; }
            public Color Color { get; set; }
            public float Age { get; set; }
        }

        private readonly struct FieldObstacle
        {
            public FieldObstacle(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }

            public Vector2 Center { get; }
            public float Radius { get; }
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
            UpdateRunTimer();
            UpdateCandidates();
            UpdatePlayerAvatar();
            UpdateStepStamps();
            UpdateCamera();
            UpdateUi();
            UpdateDebugMarkers();
            bobImpulse = Mathf.MoveTowards(bobImpulse, 0f, Time.deltaTime * 4.5f);
            invalidPulse = Mathf.MoveTowards(invalidPulse, 0f, Time.deltaTime * 4f);
            bodyLeanPulse = Mathf.MoveTowards(bodyLeanPulse, 0f, Time.deltaTime * 4.2f);
            leftFoot.StatusPulse = Mathf.MoveTowards(leftFoot.StatusPulse, 0f, Time.deltaTime * 4f);
            rightFoot.StatusPulse = Mathf.MoveTowards(rightFoot.StatusPulse, 0f, Time.deltaTime * 4f);
        }

        private void ResetRun()
        {
            maze = null;
            BuildWorld();

            bodyPosition = new Vector2(0f, 0.4f);
            leftFootPosition = bodyPosition + new Vector2(-WalkingRules.NaturalHalfStance, 0f);
            rightFootPosition = bodyPosition + new Vector2(WalkingRules.NaturalHalfStance, 0f);
            previousBodyPosition = bodyPosition;
            facing = Vector2.up;
            visualBodyPosition = bodyPosition;
            visualFacing = facing;
            cameraBodyPosition = bodyPosition;
            distanceMeters = 0f;
            runTimeRemaining = runDurationSeconds;
            steps = 0;
            bobImpulse = 0f;
            invalidPulse = 0f;
            bodyLeanPulse = 0f;
            lastLandedSide = WalkingFootSide.Left;
            ClearStepStamps();
            ResetFootRuntime(leftFoot);
            ResetFootRuntime(rightFoot);
            activeTouches.Clear();
            state = WalkingGameState.Ready;
            BuildPlayerAvatar();
            UpdatePlayerAvatar(true);
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
            gameCamera.fieldOfView = 58f;
            gameCamera.nearClipPlane = 0.04f;
            gameCamera.farClipPlane = 180f;
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
            var floorMaterial = CreateMaterial("Paper Floor", new Color32(255, 253, 247, 255));
            var lineMaterial = CreateMaterial("Floor Ink Lines", new Color32(186, 181, 170, 255));
            var goalMaterial = CreateMaterial("Forward Ice Marks", new Color32(145, 197, 215, 255));
            var obstacleShadowMaterial = CreateMaterial("Iceberg Snow Shadow", new Color32(218, 232, 234, 255));
            var obstacleMaterial = CreateMaterial("Iceberg Blue Face", new Color32(188, 226, 237, 255));
            var obstacleTopMaterial = CreateMaterial("Iceberg Snow Face", new Color32(252, 254, 250, 255));
            var obstacleStrokeMaterial = CreateMaterial("Iceberg Ink Cracks", new Color32(66, 126, 153, 255));
            var fieldWidth = openFieldHalfWidth * 2f;
            var fieldCenterZ = openFieldLength * 0.5f;
            fieldObstacles.Clear();

            CreateCube(
                "Open Paper Field",
                new Vector3(0f, -0.035f, fieldCenterZ),
                new Vector3(fieldWidth, 0.06f, openFieldLength + 18f),
                floorMaterial,
                worldRoot);

            CreateCube(
                "Start Sketch Line",
                new Vector3(0f, 0.006f, 0.22f),
                new Vector3(fieldWidth * 0.20f, 0.012f, 0.065f),
                goalMaterial,
                worldRoot);

            for (var i = 0; i <= Mathf.CeilToInt(openFieldLength / 2f); i++)
            {
                var z = i * 2f;
                var major = i % 5 == 0;
                CreateCube(
                    major ? "Forward Distance Mark" : "Forward Tick",
                    new Vector3(0f, 0.018f, z),
                    new Vector3(major ? fieldWidth * 0.18f : fieldWidth * 0.08f, 0.02f, major ? 0.08f : 0.05f),
                    major ? goalMaterial : lineMaterial,
                    worldRoot);
            }

            for (var i = 0; i <= Mathf.CeilToInt(openFieldLength / 6f); i++)
            {
                var z = i * 6f + 1.2f;
                var sideOffset = 4.5f + Mathf.Sin(i * 1.77f) * 1.7f;
                CreateCube(
                    "Left Snow Sketch Tick",
                    new Vector3(-sideOffset, 0.014f, z),
                    new Vector3(1.1f, 0.018f, 0.052f),
                    lineMaterial,
                    worldRoot);
                CreateCube(
                    "Right Snow Sketch Tick",
                    new Vector3(sideOffset * 1.08f, 0.014f, z + 2.6f),
                    new Vector3(1.1f, 0.018f, 0.052f),
                    lineMaterial,
                    worldRoot);
            }

            BuildOpenFieldObstacles(obstacleShadowMaterial, obstacleMaterial, obstacleTopMaterial, obstacleStrokeMaterial);
            BuildDebugMarkers();
        }

        private void BuildOpenFieldObstacles(
            Material obstacleShadowMaterial,
            Material obstacleMaterial,
            Material obstacleTopMaterial,
            Material obstacleStrokeMaterial)
        {
            var random = new System.Random(unchecked(System.DateTime.UtcNow.Millisecond * 92821 + steps * 97));
            var count = Mathf.Max(0, openFieldObstacleCount);
            var usableLength = Mathf.Max(20f, openFieldLength - 18f);

            for (var i = 0; i < count; i++)
            {
                var lane = i / (float)Mathf.Max(1, count - 1);
                var z = 13f + lane * usableLength + RandomRange(random, -2.4f, 2.4f);
                var x = RandomRange(random, -openFieldHalfWidth + 2.4f, openFieldHalfWidth - 2.4f);
                if (z < 40f && Mathf.Abs(x) < 2.2f)
                {
                    x += x < 0f ? -3.2f : 3.2f;
                }

                var radius = RandomRange(random, 0.42f, 0.82f);
                var center = new Vector2(x, z);
                fieldObstacles.Add(new FieldObstacle(center, radius));

                var width = radius * RandomRange(random, 1.95f, 2.85f);
                var depth = radius * RandomRange(random, 1.65f, 2.35f);
                CreateIcebergObstacle(
                    center,
                    width,
                    depth,
                    RandomRange(random, 1.12f, 1.58f),
                    RandomRange(random, -18f, 18f),
                    obstacleShadowMaterial,
                    obstacleMaterial,
                    obstacleTopMaterial,
                    obstacleStrokeMaterial,
                    random);
            }
        }

        private void CreateIcebergObstacle(
            Vector2 center,
            float width,
            float depth,
            float height,
            float yaw,
            Material shadowMaterial,
            Material bodyMaterial,
            Material capMaterial,
            Material strokeMaterial,
            System.Random random)
        {
            var root = new GameObject("Sketch Iceberg").transform;
            root.SetParent(worldRoot, false);
            root.position = new Vector3(center.x, 0f, center.y);
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            var groundWash = CreateEllipsoid(
                "Iceberg Ground Wash",
                Vector3.zero,
                new Vector3(width * 1.22f, 0.024f, depth * 1.08f),
                shadowMaterial,
                root);
            groundWash.transform.localPosition = new Vector3(0.05f, 0.012f, -0.05f);

            CreateIcebergShard(
                "Iceberg Main Peak",
                new Vector3(0f, 0.024f, 0f),
                new Vector3(width * 0.92f, height, depth * 0.82f),
                bodyMaterial,
                root,
                RandomRange(random, -8f, 8f),
                RandomRange(random, -0.10f, 0.12f));
            CreateIcebergShard(
                "Iceberg Side Peak",
                new Vector3(-width * 0.32f, 0.018f, depth * 0.10f),
                new Vector3(width * 0.55f, height * 0.72f, depth * 0.58f),
                bodyMaterial,
                root,
                RandomRange(random, -18f, -8f),
                RandomRange(random, -0.14f, 0.04f));
            CreateIcebergShard(
                "Iceberg Rear Peak",
                new Vector3(width * 0.30f, 0.016f, -depth * 0.12f),
                new Vector3(width * 0.48f, height * 0.58f, depth * 0.50f),
                bodyMaterial,
                root,
                RandomRange(random, 8f, 18f),
                RandomRange(random, -0.02f, 0.16f));

            CreateIcebergShard(
                "Iceberg Snowy Tip",
                new Vector3(-width * 0.02f, height * 0.64f, -depth * 0.02f),
                new Vector3(width * 0.46f, height * 0.25f, depth * 0.40f),
                capMaterial,
                root,
                RandomRange(random, -6f, 6f),
                RandomRange(random, -0.04f, 0.08f));

            CreateIcebergStroke(root, strokeMaterial, new Vector3(-width * 0.26f, height * 0.40f, -depth * 0.36f), new Vector3(width * 0.46f, 0.038f, depth * 0.038f), RandomRange(random, -30f, -14f));
            CreateIcebergStroke(root, strokeMaterial, new Vector3(width * 0.20f, height * 0.52f, depth * 0.30f), new Vector3(width * 0.38f, 0.038f, depth * 0.038f), RandomRange(random, 13f, 30f));
            CreateIcebergStroke(root, strokeMaterial, new Vector3(width * -0.03f, height * 0.72f, depth * -0.30f), new Vector3(width * 0.26f, 0.034f, depth * 0.034f), RandomRange(random, -10f, 10f));
            CreateIcebergStroke(root, strokeMaterial, new Vector3(width * 0.36f, height * 0.28f, depth * -0.05f), new Vector3(width * 0.34f, 0.034f, depth * 0.034f), RandomRange(random, 42f, 58f));
        }

        private static void CreateIcebergStroke(Transform obstacle, Material material, Vector3 localPosition, Vector3 localScale, float yaw)
        {
            var stroke = CreateCube("Iceberg Ink Stroke", Vector3.zero, localScale, material, obstacle);
            stroke.transform.localPosition = localPosition;
            stroke.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static GameObject CreateIcebergShard(
            string objectName,
            Vector3 localPosition,
            Vector3 scale,
            Material material,
            Transform parent,
            float yaw,
            float peakLean)
        {
            var shard = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            shard.transform.SetParent(parent, false);
            shard.transform.localPosition = localPosition;
            shard.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            shard.transform.localScale = scale;
            shard.GetComponent<MeshFilter>().sharedMesh = CreateIcebergShardMesh(peakLean);
            var renderer = shard.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return shard;
        }

        private static Mesh CreateIcebergShardMesh(float peakLean)
        {
            var vertices = new[]
            {
                new Vector3(-0.50f, 0f, -0.30f),
                new Vector3(-0.16f, 0f, -0.48f),
                new Vector3(0.34f, 0f, -0.42f),
                new Vector3(0.54f, 0f, -0.05f),
                new Vector3(0.38f, 0f, 0.40f),
                new Vector3(-0.10f, 0f, 0.50f),
                new Vector3(-0.48f, 0f, 0.24f),
                new Vector3(-0.56f, 0f, -0.10f),
                new Vector3(-0.30f, 0.46f, -0.18f),
                new Vector3(-0.08f, 0.54f, -0.30f),
                new Vector3(0.24f, 0.50f, -0.24f),
                new Vector3(0.33f, 0.46f, -0.02f),
                new Vector3(0.24f, 0.52f, 0.25f),
                new Vector3(-0.06f, 0.48f, 0.31f),
                new Vector3(-0.30f, 0.44f, 0.14f),
                new Vector3(-0.34f, 0.46f, -0.06f),
                new Vector3(peakLean, 1f, 0.02f),
            };
            var triangles = new List<int>();
            for (var i = 0; i < 8; i++)
            {
                var next = (i + 1) % 8;
                AddDoubleSidedTriangle(triangles, i, i + 8, next + 8);
                AddDoubleSidedTriangle(triangles, i, next + 8, next);
                AddDoubleSidedTriangle(triangles, i + 8, 16, next + 8);
            }

            var mesh = new Mesh
            {
                name = "Thumbwaddle Iceberg Shard",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDoubleSidedTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(a);
        }

        private static Transform CreateHatchedShadow(string objectName, Material material, Transform parent)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(parent, false);

            for (var i = 0; i < 6; i++)
            {
                var x = Mathf.Lerp(-0.30f, 0.30f, i / 5f);
                var bar = CreateCube(
                    "Penguin Shadow Hatch",
                    Vector3.zero,
                    new Vector3(0.42f, 0.016f, 0.026f),
                    material,
                    root);
                bar.transform.localPosition = new Vector3(x, 0f, Mathf.Sin(i * 1.7f) * 0.055f);
                bar.transform.localRotation = Quaternion.Euler(0f, -28f, 0f);
            }

            return root;
        }

        private void BuildPlayerAvatar()
        {
            if (playerRoot != null)
            {
                Destroy(playerRoot.gameObject);
            }

            playerRoot = new GameObject("Thumbwaddle Player").transform;
            playerRoot.SetParent(worldRoot, false);

            var shadowMaterial = CreateMaterial("Penguin Hatch Shadow", new Color32(211, 222, 220, 255));
            var bodyMaterial = CreateMaterial("Penguin Ink Body", new Color32(35, 38, 39, 255));
            var patchMaterial = CreateMaterial("Penguin Paper Patch", new Color32(255, 253, 247, 255));
            var beakMaterial = CreateMaterial("Penguin Beak", new Color32(247, 181, 71, 255));
            var footMaterial = CreateMaterial("Penguin Feet", new Color32(236, 143, 58, 255));

            playerShadow = CreateHatchedShadow("Player Hatched Shadow", shadowMaterial, playerRoot);
            playerBody = CreateEllipsoid(
                "Penguin Body",
                Vector3.zero,
                new Vector3(0.62f, 0.88f, 0.46f),
                bodyMaterial,
                playerRoot).transform;
            playerBackMark = CreateEllipsoid(
                "Penguin Back Patch",
                Vector3.zero,
                new Vector3(0.40f, 0.50f, 0.045f),
                patchMaterial,
                playerRoot).transform;
            playerHead = CreateEllipsoid(
                "Penguin Head",
                Vector3.zero,
                new Vector3(0.46f, 0.42f, 0.44f),
                bodyMaterial,
                playerRoot).transform;
            playerFacePatch = CreateEllipsoid(
                "Penguin Face Patch",
                Vector3.zero,
                new Vector3(0.30f, 0.22f, 0.035f),
                patchMaterial,
                playerRoot).transform;
            playerBeak = CreateEllipsoid(
                "Penguin Beak",
                Vector3.zero,
                new Vector3(0.15f, 0.08f, 0.18f),
                beakMaterial,
                playerRoot).transform;
            playerLeftArm = CreateEllipsoid(
                "Penguin Left Flipper",
                Vector3.zero,
                new Vector3(0.16f, 0.48f, 0.16f),
                bodyMaterial,
                playerRoot).transform;
            playerRightArm = CreateEllipsoid(
                "Penguin Right Flipper",
                Vector3.zero,
                new Vector3(0.16f, 0.48f, 0.16f),
                bodyMaterial,
                playerRoot).transform;
            playerLeftFoot = CreateEllipsoid(
                "Penguin Left Foot",
                Vector3.zero,
                new Vector3(0.27f, 0.07f, 0.45f),
                footMaterial,
                playerRoot).transform;
            playerRightFoot = CreateEllipsoid(
                "Penguin Right Foot",
                Vector3.zero,
                new Vector3(0.27f, 0.07f, 0.45f),
                footMaterial,
                playerRoot).transform;
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
                suppressMouseInputUntil = Time.unscaledTime + 0.18f;
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

            if (Time.unscaledTime < suppressMouseInputUntil)
            {
                return;
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
            if (activeTouches.TryGetValue(pointerId, out var previousFoot))
            {
                ReleasePointer(previousFoot, pointerId, false);
            }

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

            var side = WalkingRules.FootSideForScreenPosition(screenPosition, new Vector2(Screen.width, Screen.height));
            var foot = side == WalkingFootSide.Left ? leftFoot : rightFoot;
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
            var landed = TryLandFoot(foot);
            if (state == WalkingGameState.Playing)
            {
                foot.Mode = landed ? InputMode.LandedHold : InputMode.InvalidHold;
            }
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
            else if (foot.Mode == InputMode.LandedHold)
            {
                foot.ScreenPosition = screenPosition;
                if (foot.NeedsReturn && WalkingRules.IsReturnGesturePosition(screenPosition, new Vector2(Screen.width, Screen.height)))
                {
                    foot.NeedsReturn = false;
                    foot.Mode = InputMode.Return;
                    foot.StatusPulse = 1f;
                }
            }
        }

        private void EndPointer(int pointerId, Vector2 screenPosition)
        {
            if (!activeTouches.TryGetValue(pointerId, out var foot))
            {
                return;
            }

            ReleasePointer(foot, pointerId, true);
        }

        private void ReleasePointer(FootRuntime foot, int pointerId, bool playFeedback)
        {
            if (foot.Mode == InputMode.Return)
            {
                foot.NeedsReturn = false;
                foot.StatusPulse = 1f;
            }
            else if (playFeedback && foot.Mode == InputMode.Ignored)
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
            var placement = WalkingRules.ValidateFootPlacement(foot.Side, support, candidate, facing, maze);
            foot.Candidate = placement.IsValid && IsCircleTouchingFieldObstacle(candidate, WalkingRules.FootRadius)
                ? new WalkingFootPlacement(candidate, false, "obstacle")
                : placement;
        }

        private bool TryLandFoot(FootRuntime foot)
        {
            UpdateCandidate(foot);
            if (!foot.Candidate.IsValid)
            {
                invalidPulse = 1f;
                foot.StatusPulse = 1f;
                PlayBump(0.35f);
                return false;
            }

            previousBodyPosition = bodyPosition;
            var oldBody = bodyPosition;
            var oldFoot = foot.Side == WalkingFootSide.Left ? leftFootPosition : rightFootPosition;
            var proposedLeftFoot = foot.Side == WalkingFootSide.Left ? foot.Candidate.Position : leftFootPosition;
            var proposedRightFoot = foot.Side == WalkingFootSide.Right ? foot.Candidate.Position : rightFootPosition;
            var proposedBody = WalkingRules.BodyCenter(proposedLeftFoot, proposedRightFoot);

            if (IsCircleTouchingFieldObstacle(proposedBody, WalkingRules.BodyRadius))
            {
                invalidPulse = 1f;
                foot.StatusPulse = 1f;
                PlayBump(0.45f);
                return false;
            }

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
                var runForward = Vector2.Lerp(stepDirection.normalized, Vector2.up, 0.42f).normalized;
                facing = Vector2.Lerp(footForward, runForward, 0.74f).normalized;
            }
            else
            {
                facing = Vector2.Lerp(footForward, Vector2.up, 0.42f).normalized;
            }

            var traveled = Vector2.Distance(oldBody, bodyPosition);
            distanceMeters += traveled;
            steps++;
            foot.NeedsReturn = true;
            foot.StatusPulse = 1f;
            lastLandedSide = foot.Side;
            bodyLeanPulse = 1f;
            bobImpulse = Mathf.Min(1f, bobImpulse + stepBobStrength + Vector2.Distance(oldFoot, foot.Candidate.Position) * 0.04f);
            CreateStepStamp(foot.Candidate.Position, foot.Side);
            PlayStep();

            return true;
        }

        private void UpdateRunTimer()
        {
            if (state != WalkingGameState.Playing)
            {
                return;
            }

            runTimeRemaining = Mathf.Max(0f, runTimeRemaining - Time.deltaTime);
            if (runTimeRemaining <= 0f)
            {
                EndRun();
            }
        }

        private bool IsCircleTouchingFieldObstacle(Vector2 center, float radius)
        {
            for (var i = 0; i < fieldObstacles.Count; i++)
            {
                var obstacle = fieldObstacles[i];
                if (Vector2.Distance(center, obstacle.Center) <= radius + obstacle.Radius)
                {
                    return true;
                }
            }

            return false;
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
            cameraBodyPosition = Vector2.Lerp(cameraBodyPosition, visualBodyPosition, moveT);
            var bob = Mathf.Sin((1f - bobImpulse) * Mathf.PI) * bobImpulse;
            var forward3 = new Vector3(visualFacing.x, 0f, visualFacing.y);
            if (forward3.sqrMagnitude < 0.001f)
            {
                forward3 = Vector3.forward;
            }

            forward3.Normalize();
            var basePosition = new Vector3(cameraBodyPosition.x, 0f, cameraBodyPosition.y);
            var targetPosition = basePosition - forward3 * thirdPersonDistance + Vector3.up * (thirdPersonHeight + bob * 0.42f);
            var lookTarget = basePosition + forward3 * thirdPersonLookAhead + Vector3.up * Mathf.Max(0.82f, eyeHeight * 0.54f);
            gameCamera.transform.position = targetPosition;

            var targetRotation = Quaternion.LookRotation((lookTarget - targetPosition).normalized, Vector3.up);
            gameCamera.transform.rotation = snap
                ? targetRotation
                : Quaternion.Slerp(gameCamera.transform.rotation, targetRotation, 1f - Mathf.Exp(-cameraTurnLerp * Time.deltaTime));
        }

        private void UpdatePlayerAvatar(bool snap = false)
        {
            if (playerRoot == null)
            {
                return;
            }

            var moveT = snap ? 1f : 1f - Mathf.Exp(-avatarMoveLerp * Time.deltaTime);
            var turnT = snap ? 1f : 1f - Mathf.Exp(-avatarTurnLerp * Time.deltaTime);
            visualBodyPosition = Vector2.Lerp(visualBodyPosition, bodyPosition, moveT);
            visualFacing = Vector2.Lerp(visualFacing, facing, turnT);
            if (visualFacing.sqrMagnitude < 0.001f)
            {
                visualFacing = Vector2.up;
            }

            visualFacing.Normalize();
            var forward3 = new Vector3(visualFacing.x, 0f, visualFacing.y);
            var targetRotation = Quaternion.LookRotation(forward3, Vector3.up);
            var rootTarget = new Vector3(visualBodyPosition.x, 0f, visualBodyPosition.y);
            var bodyBob = Mathf.Sin((1f - bobImpulse) * Mathf.PI) * bobImpulse * 0.14f;
            var landedSide = lastLandedSide == WalkingFootSide.Left ? -1f : 1f;
            var lean = landedSide * bodyLeanPulse;

            playerRoot.position = rootTarget;
            playerRoot.rotation = targetRotation;

            if (playerShadow != null)
            {
                playerShadow.localPosition = new Vector3(0f, 0.026f, -0.02f);
                playerShadow.localScale = new Vector3(1f, 1f, 1f);
            }

            if (playerBody != null)
            {
                playerBody.localPosition = new Vector3(lean * 0.045f, 0.45f + bodyBob - bodyLeanPulse * 0.025f, 0f);
                playerBody.localRotation = Quaternion.Euler(0f, 0f, -lean * 7f);
                playerBody.localScale = new Vector3(0.62f + bodyLeanPulse * 0.035f, 0.88f - bodyLeanPulse * 0.045f, 0.46f);
            }

            if (playerBackMark != null)
            {
                playerBackMark.localPosition = new Vector3(lean * 0.045f, 0.40f + bodyBob - bodyLeanPulse * 0.025f, -0.238f);
                playerBackMark.localRotation = Quaternion.Euler(0f, 0f, -lean * 7f);
                playerBackMark.localScale = new Vector3(0.24f, 0.16f, 0.035f);
            }

            if (playerHead != null)
            {
                playerHead.localPosition = new Vector3(lean * 0.065f, 1.03f + bodyBob - bodyLeanPulse * 0.018f, 0.02f);
                playerHead.localRotation = Quaternion.Euler(0f, 0f, -lean * 5f);
                playerHead.localScale = new Vector3(0.46f, 0.42f, 0.44f);
            }

            if (playerFacePatch != null)
            {
                playerFacePatch.localPosition = new Vector3(lean * 0.066f, 1.03f + bodyBob - bodyLeanPulse * 0.018f, 0.238f);
                playerFacePatch.localRotation = Quaternion.Euler(0f, 0f, -lean * 5f);
                playerFacePatch.localScale = new Vector3(0.30f, 0.22f, 0.035f);
            }

            if (playerBeak != null)
            {
                playerBeak.localPosition = new Vector3(lean * 0.066f, 0.99f + bodyBob - bodyLeanPulse * 0.018f, 0.305f);
                playerBeak.localRotation = Quaternion.Euler(4f, 0f, -lean * 5f);
                playerBeak.localScale = new Vector3(0.15f, 0.08f, 0.18f);
            }

            if (playerLeftArm != null)
            {
                var swing = Mathf.Clamp01(rightFoot.StatusPulse + bobImpulse * 2.6f);
                playerLeftArm.localPosition = new Vector3(-0.255f + lean * 0.024f, 0.70f + bodyBob * 0.55f, -0.045f + swing * 0.035f);
                playerLeftArm.localRotation = Quaternion.Euler(7f + swing * 10f, 0f, -38f - lean * 3f);
                playerLeftArm.localScale = new Vector3(0.17f, 0.42f, 0.15f);
            }

            if (playerRightArm != null)
            {
                var swing = Mathf.Clamp01(leftFoot.StatusPulse + bobImpulse * 2.6f);
                playerRightArm.localPosition = new Vector3(0.255f + lean * 0.024f, 0.70f + bodyBob * 0.55f, -0.045f + swing * 0.035f);
                playerRightArm.localRotation = Quaternion.Euler(7f + swing * 10f, 0f, 38f - lean * 3f);
                playerRightArm.localScale = new Vector3(0.17f, 0.42f, 0.15f);
            }

            SetAvatarFoot(playerLeftFoot, leftFootPosition, leftFoot.StatusPulse, targetRotation, snap);
            SetAvatarFoot(playerRightFoot, rightFootPosition, rightFoot.StatusPulse, targetRotation, snap);
        }

        private static void SetAvatarFoot(Transform foot, Vector2 position, float pulse, Quaternion rotation, bool snap)
        {
            if (foot == null)
            {
                return;
            }

            var target = new Vector3(position.x, 0.055f + pulse * 0.035f, position.y);
            var t = snap || pulse > 0.72f ? 1f : 1f - Mathf.Exp(-20f * Time.deltaTime);
            foot.position = snap ? target : Vector3.Lerp(foot.position, target, t);
            foot.rotation = snap ? rotation : Quaternion.Slerp(foot.rotation, rotation, t);
            var spread = 1f + Mathf.Clamp01(pulse) * 0.16f;
            foot.localScale = new Vector3(0.27f * spread, 0.07f, 0.45f * spread);
        }

        private void CreateStepStamp(Vector2 position, WalkingFootSide side)
        {
            if (worldRoot == null)
            {
                return;
            }

            var color = side == WalkingFootSide.Left ? Blue : Warm;
            var material = CreateMaterial("Step Stamp Material", color);
            var root = new GameObject("Step Ink Stamp").transform;
            root.SetParent(worldRoot, false);
            root.position = new Vector3(position.x, 0.075f, position.y);
            var forward3 = new Vector3(facing.x, 0f, facing.y);
            if (forward3.sqrMagnitude < 0.001f)
            {
                forward3 = Vector3.forward;
            }

            root.rotation = Quaternion.LookRotation(forward3.normalized, Vector3.up);
            CreateStampBar(root, material, new Vector3(0f, 0f, 0.22f), new Vector3(0.30f, 0.018f, 0.045f));
            CreateStampBar(root, material, new Vector3(0f, 0f, -0.22f), new Vector3(0.30f, 0.018f, 0.045f));
            CreateStampBar(root, material, new Vector3(-0.17f, 0f, 0f), new Vector3(0.045f, 0.018f, 0.36f));
            CreateStampBar(root, material, new Vector3(0.17f, 0f, 0f), new Vector3(0.045f, 0.018f, 0.36f));
            stepStamps.Add(new StepStamp
            {
                Root = root,
                Material = material,
                Color = color,
                Age = 0f
            });
        }

        private void UpdateStepStamps()
        {
            const float lifetime = 0.38f;
            for (var i = stepStamps.Count - 1; i >= 0; i--)
            {
                var stamp = stepStamps[i];
                if (stamp.Root == null)
                {
                    stepStamps.RemoveAt(i);
                    continue;
                }

                stamp.Age += Time.deltaTime;
                var t = Mathf.Clamp01(stamp.Age / lifetime);
                stamp.Root.localScale = Vector3.one * Mathf.Lerp(0.76f, 1.32f, Mathf.SmoothStep(0f, 1f, t));
                SetMaterialColor(stamp.Material, Color.Lerp(stamp.Color, Paper, t * 0.82f));
                if (stamp.Age >= lifetime)
                {
                    Destroy(stamp.Root.gameObject);
                    stepStamps.RemoveAt(i);
                }
            }
        }

        private void ClearStepStamps()
        {
            for (var i = 0; i < stepStamps.Count; i++)
            {
                if (stepStamps[i].Root != null)
                {
                    Destroy(stepStamps[i].Root.gameObject);
                }
            }

            stepStamps.Clear();
        }

        private static void CreateStampBar(Transform root, Material material, Vector3 localPosition, Vector3 localScale)
        {
            var bar = CreateCube("Step Stamp Stroke", Vector3.zero, localScale, material, root);
            bar.transform.localPosition = localPosition;
        }

        private void UpdateUi()
        {
            if (distanceText == null)
            {
                return;
            }

            distanceText.text = $"{distanceMeters:0.0} m";
            bestText.text = $"{Mathf.CeilToInt(runTimeRemaining)} s";
            stepText.text = state == WalkingGameState.Playing ? steps.ToString() : string.Empty;

            var ready = state == WalkingGameState.Ready;
            titleText.gameObject.SetActive(ready);
            hintText.gameObject.SetActive(ready);
            titleText.text = "Thumbwaddle";
            hintText.text = string.Empty;

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
            GUI.Label(new Rect(Screen.width * 0.5f - 64f * scale, margin + 12f * scale, 128f * scale, topHeight), $"{Mathf.CeilToInt(runTimeRemaining)}s", smallHudStyle);
            GUI.Label(new Rect(Screen.width - margin - 180f * scale, margin + 12f * scale, 160f * scale, topHeight), $"{bestDistanceMeters:0.0} m", smallHudStyle);

            DrawFootSignal(leftFoot, new Rect(margin, Screen.height - 90f * scale, 108f * scale, 54f * scale), scale);
            DrawFootSignal(rightFoot, new Rect(Screen.width - margin - 108f * scale, Screen.height - 90f * scale, 108f * scale, 54f * scale), scale);

            if (state == WalkingGameState.Ready)
            {
                DrawReadyCoach(scale);
            }
            else if (state == WalkingGameState.Result)
            {
                DrawGuiRect(new Rect(Screen.width * 0.18f, Screen.height * 0.27f, Screen.width * 0.64f, 240f * scale), new Color(1f, 0.99f, 0.96f, 0.9f));
                GUI.Label(new Rect(Screen.width * 0.18f, Screen.height * 0.28f, Screen.width * 0.64f, 128f * scale), $"Result\n{distanceMeters:0.0} m\nBest {bestDistanceMeters:0.0} m", titleStyle);
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
            var leftLowActive = leftFoot.NeedsReturn || leftFoot.Mode == InputMode.Return;
            var rightLowActive = rightFoot.NeedsReturn || rightFoot.Mode == InputMode.Return;
            var leftLowColor = leftLowActive
                ? new Color(Blue.r, Blue.g, Blue.b, 0.12f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.045f);
            var rightLowColor = rightLowActive
                ? new Color(Blue.r, Blue.g, Blue.b, 0.12f)
                : new Color(Blue.r, Blue.g, Blue.b, 0.045f);

            DrawGuiRect(new Rect(0f, returnTop, Screen.width * 0.5f, lowHeight), leftLowColor);
            DrawGuiRect(new Rect(Screen.width * 0.5f, returnTop, Screen.width * 0.5f, lowHeight), rightLowColor);
            DrawGuiRect(new Rect(Screen.width * 0.5f - 1f * scale, splitTop, 2f * scale, Screen.height - splitTop), new Color(Ink.r, Ink.g, Ink.b, 0.08f));
            DrawGuiRect(new Rect(0f, returnTop, Screen.width, 2f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.08f));

            var suggestedSide = steps % 2 == 0 ? WalkingFootSide.Left : WalkingFootSide.Right;
            DrawTouchGlyph(leftFoot, new Rect(0f, splitTop, Screen.width * 0.5f, returnTop - splitTop), true, showLabels && suggestedSide == WalkingFootSide.Left && !leftFoot.NeedsReturn, scale);
            DrawTouchGlyph(rightFoot, new Rect(Screen.width * 0.5f, splitTop, Screen.width * 0.5f, returnTop - splitTop), true, showLabels && suggestedSide == WalkingFootSide.Right && !rightFoot.NeedsReturn, scale);
            DrawTouchGlyph(leftFoot, new Rect(0f, returnTop, Screen.width * 0.5f, lowHeight), false, showLabels && leftLowActive, scale);
            DrawTouchGlyph(rightFoot, new Rect(Screen.width * 0.5f, returnTop, Screen.width * 0.5f, lowHeight), false, showLabels && rightLowActive, scale);
        }

        private void DrawTouchGlyph(FootRuntime foot, Rect zone, bool stepZone, bool suggested, float scale)
        {
            var phase = Pulse01(Time.unscaledTime * 1.55f + (foot.Side == WalkingFootSide.Left ? 0f : 0.5f));
            var invalid = foot.Mode == InputMode.InvalidHold || (foot.Mode == InputMode.Placement && !foot.Candidate.IsValid);
            var active = stepZone
                ? foot.Mode == InputMode.Placement || (!foot.NeedsReturn && suggested)
                : foot.Mode == InputMode.Return || foot.NeedsReturn || suggested;
            var color = invalid
                ? Red
                : stepZone
                    ? Warm
                    : Blue;
            var alpha = active ? Mathf.Lerp(0.34f, 0.62f, phase) : 0.16f;
            var size = (active ? Mathf.Lerp(32f, 45f, phase) : 28f) * scale;
            var xJitter = invalid ? Mathf.Sin(Time.unscaledTime * 42f) * 5f * scale * Mathf.Clamp01(foot.StatusPulse + 0.25f) : 0f;
            var center = stepZone
                ? new Vector2(zone.center.x + xJitter, Mathf.Lerp(zone.yMin + 76f * scale, zone.yMax - 66f * scale, 0.48f))
                : new Vector2(zone.center.x + xJitter, zone.center.y);
            var rect = CenteredRect(center, size, size);

            if (stepZone)
            {
                DrawRing(Inflate(rect, 7f * scale), new Color(color.r, color.g, color.b, alpha));
                DrawFootprint(center, foot.Side, scale * 0.82f, new Color(color.r, color.g, color.b, alpha * 0.85f));
                if (invalid)
                {
                    DrawGuiRect(new Rect(rect.xMin - 14f * scale, rect.center.y - 3f * scale, rect.width + 28f * scale, 6f * scale), new Color(Red.r, Red.g, Red.b, 0.42f));
                }

                return;
            }

            DrawReturnPocket(center, 116f * scale, 32f * scale, 6f * scale, new Color(color.r, color.g, color.b, alpha));
            DrawThumb(CenteredRect(new Vector2(center.x, center.y - 34f * scale), size * 0.58f, size * 0.68f), new Color(color.r, color.g, color.b, alpha * 0.45f));
        }

        private void DrawReadyCoach(float scale)
        {
            var panel = new Rect(Screen.width * 0.10f, Screen.height * 0.18f, Screen.width * 0.80f, Screen.height * 0.42f);
            DrawGuiRect(panel, new Color(1f, 0.99f, 0.96f, 0.9f));
            GUI.Label(new Rect(panel.x + 18f * scale, panel.y + 8f * scale, panel.width - 36f * scale, 34f * scale), "Thumbwaddle", guideStyle);

            var gap = 18f * scale;
            var laneTop = panel.y + 48f * scale;
            var laneHeight = panel.height - 66f * scale;
            var laneWidth = (panel.width - gap * 3f) * 0.5f;
            var leftLane = new Rect(panel.x + gap, laneTop, laneWidth, laneHeight);
            var rightLane = new Rect(panel.x + gap * 2f + laneWidth, laneTop, laneWidth, laneHeight);
            DrawThumbLoop(leftLane, leftFoot.Side, 0f, scale);
            DrawThumbLoop(rightLane, rightFoot.Side, 0.5f, scale);
        }

        private void DrawThumbLoop(Rect lane, WalkingFootSide side, float phaseOffset, float scale)
        {
            DrawGuiRect(lane, new Color(Ink.r, Ink.g, Ink.b, 0.045f));

            var topCenter = new Vector2(lane.center.x, lane.y + 55f * scale);
            var bottomCenter = new Vector2(lane.center.x, lane.yMax - 48f * scale);
            var loop = Mathf.Repeat(Time.unscaledTime * 0.82f + phaseOffset, 1f);
            var stampPulse = loop < 0.28f ? 1f - loop / 0.28f : Mathf.Clamp01(1f - (loop - 0.28f) / 0.52f);
            var pocketPulse = loop > 0.34f && loop < 0.78f ? Pulse01((loop - 0.34f) * 2.2f) : 0.15f;

            DrawRing(CenteredRect(topCenter, (52f + stampPulse * 18f) * scale, (52f + stampPulse * 18f) * scale), new Color(Warm.r, Warm.g, Warm.b, 0.34f + stampPulse * 0.36f));
            DrawFootprint(topCenter, side, scale * (0.86f + stampPulse * 0.12f), new Color(Warm.r, Warm.g, Warm.b, 0.34f + stampPulse * 0.48f));
            DrawReturnPocket(bottomCenter, 94f * scale, 28f * scale, 5f * scale, new Color(Blue.r, Blue.g, Blue.b, 0.30f + pocketPulse * 0.28f));

            if (loop >= 0.10f && loop < 0.74f)
            {
                var t = Mathf.InverseLerp(0.10f, 0.74f, loop);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var center = Vector2.Lerp(topCenter, bottomCenter, eased);
                var fade = 1f - Mathf.SmoothStep(0.58f, 0.74f, loop);
                var pullColor = Color.Lerp(Warm, Blue, Mathf.Clamp01(t));
                DrawThumb(CenteredRect(center, 42f * scale, 50f * scale), new Color(pullColor.r, pullColor.g, pullColor.b, 0.88f * fade));

                var ghostCount = 3;
                for (var i = 1; i <= ghostCount; i++)
                {
                    var ghostT = Mathf.Clamp01(eased - i * 0.15f);
                    if (ghostT <= 0f)
                    {
                        continue;
                    }

                    var ghostCenter = Vector2.Lerp(topCenter, bottomCenter, ghostT);
                    DrawCircle(CenteredRect(ghostCenter, 8f * scale, 8f * scale), new Color(Ink.r, Ink.g, Ink.b, 0.06f * fade));
                }
            }
            else if (loop < 0.10f)
            {
                DrawThumb(CenteredRect(topCenter, 46f * scale, 54f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.95f));
            }
        }

        private void DrawFootSignal(FootRuntime foot, Rect rect, float scale)
        {
            DrawGuiRect(rect, FootStatusColor(foot));
            var center = rect.center;
            var dot = CenteredRect(center, 24f * scale, 24f * scale);
            var lineColor = new Color(Ink.r, Ink.g, Ink.b, 0.42f);
            DrawGuiRect(new Rect(rect.x + 14f * scale, center.y - 2f * scale, rect.width - 28f * scale, 4f * scale), lineColor);

            if (foot.Mode == InputMode.InvalidHold || (foot.Mode == InputMode.Placement && !foot.Candidate.IsValid))
            {
                DrawRing(Inflate(dot, 8f * scale), new Color(Red.r, Red.g, Red.b, 0.9f));
                DrawGuiRect(new Rect(center.x - 18f * scale, center.y - 3f * scale, 36f * scale, 6f * scale), new Color(Red.r, Red.g, Red.b, 0.75f));
                return;
            }

            if (foot.Mode == InputMode.Return || foot.NeedsReturn)
            {
                DrawGuiRect(new Rect(center.x - 24f * scale, center.y + 9f * scale, 48f * scale, 5f * scale), new Color(Blue.r, Blue.g, Blue.b, 0.88f));
                DrawCircle(dot, new Color(Blue.r, Blue.g, Blue.b, 0.42f));
                return;
            }

            DrawRing(Inflate(dot, 8f * scale), new Color(Warm.r, Warm.g, Warm.b, 0.72f));
            DrawCircle(dot, new Color(Warm.r, Warm.g, Warm.b, 0.5f));
        }

        private static Color FootStatusColor(FootRuntime foot)
        {
            if (foot.Mode == InputMode.InvalidHold || foot.Mode == InputMode.Placement)
            {
                return foot.Mode != InputMode.InvalidHold && foot.Candidate.IsValid
                    ? new Color(Green.r, Green.g, Green.b, 0.20f)
                    : new Color(Red.r, Red.g, Red.b, 0.24f);
            }

            if (foot.Mode == InputMode.Return)
            {
                return new Color(Blue.r, Blue.g, Blue.b, 0.24f);
            }

            return foot.NeedsReturn
                ? new Color(Warm.r, Warm.g, Warm.b, 0.22f)
                : new Color(1f, 0.99f, 0.96f, 0.46f);
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

            EnsureShapeTextures();
        }

        private static void DrawGuiRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawCircle(Rect rect, Color color)
        {
            EnsureShapeTextures();
            DrawGuiTexture(rect, circleTexture, color);
        }

        private void DrawRing(Rect rect, Color color)
        {
            EnsureShapeTextures();
            DrawGuiTexture(rect, ringTexture, color);
        }

        private void DrawThumb(Rect rect, Color color)
        {
            DrawCircle(rect, color);
            var highlight = new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.12f, rect.width * 0.44f, rect.height * 0.26f);
            DrawCircle(highlight, new Color(1f, 1f, 1f, color.a * 0.22f));
        }

        private void DrawFootprint(Vector2 center, WalkingFootSide side, float scale, Color color)
        {
            var mirror = side == WalkingFootSide.Left ? -1f : 1f;
            DrawCircle(CenteredRect(center + new Vector2(-mirror * 2f * scale, 7f * scale), 18f * scale, 34f * scale), color);
            DrawCircle(CenteredRect(center + new Vector2(mirror * 3f * scale, -12f * scale), 10f * scale, 10f * scale), color);
            DrawCircle(CenteredRect(center + new Vector2(-mirror * 6f * scale, -9f * scale), 8f * scale, 8f * scale), color);
            DrawCircle(CenteredRect(center + new Vector2(mirror * 11f * scale, -5f * scale), 7f * scale, 7f * scale), color);
        }

        private static void DrawReturnPocket(Vector2 center, float width, float height, float stroke, Color color)
        {
            var pocket = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            DrawGuiRect(new Rect(pocket.x, pocket.yMax - stroke, pocket.width, stroke), color);
            DrawGuiRect(new Rect(pocket.x, pocket.y, stroke, pocket.height), color);
            DrawGuiRect(new Rect(pocket.xMax - stroke, pocket.y, stroke, pocket.height), color);
        }

        private static void DrawGuiTexture(Rect rect, Texture2D texture, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        private void EnsureShapeTextures()
        {
            if (circleTexture != null && ringTexture != null)
            {
                return;
            }

            circleTexture = CreateCircleTexture("Thumbwaddle Circle", 64, 0f);
            ringTexture = CreateCircleTexture("Thumbwaddle Ring", 64, 0.64f);
        }

        private static Texture2D CreateCircleTexture(string textureName, int size, float innerRadius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var outerAlpha = Mathf.Clamp01((1f - distance) * 10f);
                    var innerAlpha = innerRadius <= 0f ? 1f : Mathf.Clamp01((distance - innerRadius) * 14f);
                    var alpha = (byte)Mathf.RoundToInt(255f * outerAlpha * innerAlpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Rect CenteredRect(Vector2 center, float width, float height)
        {
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private static Rect Inflate(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static float Pulse01(float time)
        {
            return 0.5f + Mathf.Sin(time * Mathf.PI * 2f) * 0.5f;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (max - min) * (float)random.NextDouble();
        }

        private static void ApplyFootStatus(FootRuntime foot, Text label, Image badge, Image zone)
        {
            var color = new Color32(255, 253, 247, 210);
            var zoneColor = new Color(1f, 1f, 1f, 0.02f);

            if (foot.Mode == InputMode.Placement)
            {
                if (foot.Candidate.IsValid)
                {
                    color = WithAlpha(Green, 226);
                    zoneColor = new Color(Green.r, Green.g, Green.b, 0.09f);
                }
                else
                {
                    color = WithAlpha(Red, 226);
                    zoneColor = new Color(Red.r, Red.g, Red.b, 0.11f);
                }
            }
            else if (foot.Mode == InputMode.Return)
            {
                color = WithAlpha(Blue, 226);
                zoneColor = new Color(Blue.r, Blue.g, Blue.b, 0.10f);
            }
            else if (foot.NeedsReturn)
            {
                color = WithAlpha(Warm, 226);
                zoneColor = new Color(Warm.r, Warm.g, Warm.b, 0.11f);
            }

            label.text = string.Empty;
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
            cube.GetComponent<MeshFilter>().sharedMesh = sharedCubeMesh ?? (sharedCubeMesh = CreateUnitCubeMesh());
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return cube;
        }

        private static GameObject CreateEllipsoid(string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var ellipsoid = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            ellipsoid.name = objectName;
            ellipsoid.transform.SetParent(parent, false);
            ellipsoid.transform.position = position;
            ellipsoid.transform.localScale = scale;
            ellipsoid.GetComponent<MeshFilter>().sharedMesh = sharedSphereMesh ?? (sharedSphereMesh = CreateUnitSphereMesh());
            var renderer = ellipsoid.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ellipsoid;
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

        private static Mesh CreateUnitSphereMesh()
        {
            const int longitudeSegments = 18;
            const int latitudeSegments = 10;
            var vertices = new Vector3[(latitudeSegments + 1) * (longitudeSegments + 1)];
            var triangles = new int[latitudeSegments * longitudeSegments * 6];

            for (var lat = 0; lat <= latitudeSegments; lat++)
            {
                var v = lat / (float)latitudeSegments;
                var theta = v * Mathf.PI;
                var sinTheta = Mathf.Sin(theta);
                var cosTheta = Mathf.Cos(theta);

                for (var lon = 0; lon <= longitudeSegments; lon++)
                {
                    var u = lon / (float)longitudeSegments;
                    var phi = u * Mathf.PI * 2f;
                    var index = lat * (longitudeSegments + 1) + lon;
                    vertices[index] = new Vector3(
                        Mathf.Cos(phi) * sinTheta * 0.5f,
                        cosTheta * 0.5f,
                        Mathf.Sin(phi) * sinTheta * 0.5f);
                }
            }

            var tri = 0;
            for (var lat = 0; lat < latitudeSegments; lat++)
            {
                for (var lon = 0; lon < longitudeSegments; lon++)
                {
                    var current = lat * (longitudeSegments + 1) + lon;
                    var next = current + longitudeSegments + 1;
                    triangles[tri++] = current;
                    triangles[tri++] = next;
                    triangles[tri++] = current + 1;
                    triangles[tri++] = current + 1;
                    triangles[tri++] = next;
                    triangles[tri++] = next + 1;
                }
            }

            var mesh = new Mesh
            {
                name = "Thumbwaddle Rounded Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader =
                Shader.Find("Unlit/Color") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("UI/Default") ??
                Shader.Find("Standard") ??
                Shader.Find("Hidden/Internal-Colored");
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

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
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
