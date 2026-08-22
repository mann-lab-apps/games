using System;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Rainwalker
{
    public sealed class RainwalkerController : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.rainwalker.best_score";
        private const int MaxRaindrops = 360;
        private const int MaxSplashes = 72;
        private const int MaxWetMarks = 24;

        private static readonly Vector2 UmbrellaPivot = new Vector2(92f, 12f);
        private static readonly Vector2 BodyTop = new Vector2(0f, -20f);
        private static readonly Vector2 BodyBottom = new Vector2(0f, -285f);
        private static readonly Vector2 HeadCenter = new Vector2(0f, 92f);
        private static readonly Vector2 ThreatTargetCenter = new Vector2(4f, 56f);

        private readonly System.Random random = new System.Random(Environment.TickCount);
        private readonly RainwalkerRaindrop[] raindrops = new RainwalkerRaindrop[MaxRaindrops];
        private readonly RainwalkerSplash[] splashes = new RainwalkerSplash[MaxSplashes];
        private readonly RainwalkerWetMark[] wetMarks = new RainwalkerWetMark[MaxWetMarks];

        private RectTransform playRoot;
        private RectTransform umbrellaRoot;
        private DoodleRainGraphic rainGraphic;
        private DoodleCharacterGraphic characterGraphic;
        private DoodleSplashGraphic splashGraphic;
        private DoodleWetMarkGraphic wetMarkGraphic;
        private DoodleWindGraphic windGraphic;
        private Text timerText;
        private Text hitsText;
        private Text scoreText;
        private GameObject readyPanel;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultScoreText;
        private AudioSource audioSource;
        private AudioClip blockClip;
        private AudioClip hitClip;
        private RainwalkerGameState state = RainwalkerGameState.Ready;
        private Vector2 rainDirection = new Vector2(0.2f, -1f).normalized;
        private float rainSpeed = 920f;
        private float elapsedSeconds;
        private float spawnTimer;
        private float nextDirectionChangeAt;
        private float rainAngle;
        private float targetRainAngle;
        private float rainAngleVelocity;
        private float targetUmbrellaAngle;
        private float umbrellaAngle;
        private float umbrellaAngleVelocity;
        private float walkPhase;
        private int wetHitCount;
        private int blockedCount;
        private int nextRaindrop;
        private int nextSplash;
        private int nextWetMark;
        private int bestScore;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            blockClip = CreateToneClip("Umbrella Tick", 680f, 0.035f, 0.18f);
            hitClip = CreateToneClip("Rain Hit", 210f, 0.075f, 0.26f);

            EnsureEventSystem();
            BuildInterface();
            EnterReady();
        }

        private void Update()
        {
            walkPhase += Time.deltaTime * (state == RainwalkerGameState.Playing ? 7.5f : 2.2f);
            characterGraphic.WalkPhase = walkPhase;
            characterGraphic.SetVerticesDirty();

            if (state == RainwalkerGameState.Playing)
            {
                UpdatePlaying();
            }
            else
            {
                umbrellaAngle = Mathf.SmoothDampAngle(umbrellaAngle, targetUmbrellaAngle, ref umbrellaAngleVelocity, 0.14f);
                ApplyUmbrellaVisual();
            }

            UpdateEffects(Time.deltaTime);
        }

        private void EnterReady()
        {
            state = RainwalkerGameState.Ready;
            elapsedSeconds = 0f;
            wetHitCount = 0;
            blockedCount = 0;
            spawnTimer = 0f;
            rainAngle = 0f;
            targetRainAngle = 0f;
            rainAngleVelocity = 0f;
            rainDirection = DirectionFromRainAngle(rainAngle);
            targetUmbrellaAngle = 0f;
            umbrellaAngle = 0f;
            umbrellaAngleVelocity = 0f;
            ClearRain();
            readyPanel.SetActive(true);
            resultPanel.SetActive(false);
            UpdateHud();
        }

        private void StartRun()
        {
            state = RainwalkerGameState.Playing;
            elapsedSeconds = 0f;
            wetHitCount = 0;
            blockedCount = 0;
            spawnTimer = 0f;
            nextDirectionChangeAt = 0f;
            rainAngle = 0f;
            targetRainAngle = 0f;
            rainAngleVelocity = 0f;
            rainDirection = DirectionFromRainAngle(rainAngle);
            targetUmbrellaAngle = 0f;
            umbrellaAngle = 0f;
            umbrellaAngleVelocity = 0f;
            ClearRain();
            readyPanel.SetActive(false);
            resultPanel.SetActive(false);
            ChangeRainDirection(true);
            nextDirectionChangeAt = RainwalkerRules.DirectionChangeSecondsForProgress(0f) * Mathf.Lerp(0.72f, 1.18f, (float)random.NextDouble());
            UpdateHud();
        }

        private void UpdatePlaying()
        {
            var deltaTime = Time.deltaTime;
            elapsedSeconds += deltaTime;
            var progress = Mathf.Clamp01(elapsedSeconds / RainwalkerRules.RoundSeconds);

            HandlePointerInput();
            umbrellaAngle = Mathf.SmoothDampAngle(umbrellaAngle, targetUmbrellaAngle, ref umbrellaAngleVelocity, 0.105f);
            ApplyUmbrellaVisual();

            if (elapsedSeconds >= nextDirectionChangeAt)
            {
                ChangeRainDirection(false);
                nextDirectionChangeAt = elapsedSeconds + RainwalkerRules.DirectionChangeSecondsForProgress(progress) * Mathf.Lerp(0.72f, 1.18f, (float)random.NextDouble());
            }

            UpdateRainDirection(progress);
            rainSpeed = RainwalkerRules.RainSpeedForProgress(progress);
            spawnTimer += deltaTime;
            var spawnInterval = RainwalkerRules.SpawnIntervalForProgress(progress);
            while (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                SpawnRaindrop(progress);
            }

            UpdateRaindrops(deltaTime);
            UpdateHud();

            if (elapsedSeconds >= RainwalkerRules.RoundSeconds)
            {
                ShowResult();
            }
        }

        private void HandlePointerInput()
        {
            var hasInput = false;
            var screenPosition = Vector2.zero;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                hasInput = touch.phase != TouchPhase.Canceled && touch.phase != TouchPhase.Ended;
                screenPosition = touch.position;
            }
            else if (Input.GetMouseButton(0))
            {
                hasInput = true;
                screenPosition = Input.mousePosition;
            }

            if (!hasInput)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(playRoot, screenPosition, null, out localPoint))
            {
                return;
            }

            var direction = localPoint - UmbrellaPivot;
            if (direction.sqrMagnitude < 1f)
            {
                return;
            }

            targetUmbrellaAngle = Mathf.Clamp(Vector2.SignedAngle(Vector2.up, direction), RainwalkerRules.UmbrellaMinAngle, RainwalkerRules.UmbrellaMaxAngle);
        }

        private void ChangeRainDirection(bool immediate)
        {
            var side = random.NextDouble() < 0.5 ? -1f : 1f;
            var progress = Mathf.Clamp01(elapsedSeconds / RainwalkerRules.RoundSeconds);
            var minAngle = Mathf.Lerp(24f, 31f, progress);
            var maxAngle = Mathf.Lerp(50f, 64f, progress);
            var gustRoll = (float)random.NextDouble();
            var t = gustRoll < 0.54f ? Mathf.Lerp(0.72f, 1f, (float)random.NextDouble()) : (float)random.NextDouble();
            targetRainAngle = Mathf.Lerp(minAngle, maxAngle, t) * side;
            if (immediate)
            {
                rainAngle = targetRainAngle;
                rainAngleVelocity = 0f;
                rainDirection = DirectionFromRainAngle(rainAngle);
                UpdateWindGraphic();
            }
        }

        private void UpdateRainDirection(float progress)
        {
            var smoothTime = Mathf.Lerp(0.82f, 0.42f, progress);
            rainAngle = Mathf.SmoothDampAngle(rainAngle, targetRainAngle, ref rainAngleVelocity, smoothTime);
            rainDirection = DirectionFromRainAngle(rainAngle);
            UpdateWindGraphic();
        }

        private static Vector2 DirectionFromRainAngle(float angleDegrees)
        {
            var angle = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)).normalized;
        }

        private void UpdateWindGraphic()
        {
            if (windGraphic != null)
            {
                windGraphic.Direction = rainDirection;
                windGraphic.SetVerticesDirty();
            }
        }

        private void SpawnRaindrop(float progress)
        {
            var rect = playRoot.rect;
            var margin = 300f;
            var threatDrop = random.NextDouble() < Mathf.Lerp(0.46f, 0.72f, progress);
            var target = ThreatTargetCenter + new Vector2(
                Mathf.Lerp(-92f, 92f, (float)random.NextDouble()),
                Mathf.Lerp(-86f, 172f, (float)random.NextDouble()));
            var drop = new RainwalkerRaindrop
            {
                Active = true,
                Position = threatDrop
                    ? SpawnPointForTarget(target, margin)
                    : new Vector2(
                        Mathf.Lerp(rect.xMin - margin, rect.xMax + margin, (float)random.NextDouble()),
                        rect.yMax + Mathf.Lerp(35f, 330f, (float)random.NextDouble())),
                Length = Mathf.Lerp(70f, 150f, (float)random.NextDouble()),
                Width = Mathf.Lerp(3.8f, 8.2f, (float)random.NextDouble()),
                Seed = random.Next(1, 100000)
            };

            drop.PreviousPosition = drop.Position;
            drop.Velocity = rainDirection * (rainSpeed + Mathf.Lerp(-170f, 340f, (float)random.NextDouble()) + progress * 180f);
            raindrops[nextRaindrop] = drop;
            nextRaindrop = (nextRaindrop + 1) % raindrops.Length;
            rainGraphic.SetVerticesDirty();
        }

        private Vector2 SpawnPointForTarget(Vector2 target, float margin)
        {
            var rect = playRoot.rect;
            var min = new Vector2(rect.xMin - margin, rect.yMin - margin);
            var max = new Vector2(rect.xMax + margin, rect.yMax + margin);
            var back = -rainDirection;
            var bestDistance = float.PositiveInfinity;
            var bestPoint = target - rainDirection * 900f;

            ConsiderSpawnBoundary(min.x, true, target, back, min, max, ref bestDistance, ref bestPoint);
            ConsiderSpawnBoundary(max.x, true, target, back, min, max, ref bestDistance, ref bestPoint);
            ConsiderSpawnBoundary(max.y, false, target, back, min, max, ref bestDistance, ref bestPoint);

            var cross = new Vector2(-rainDirection.y, rainDirection.x);
            return bestPoint + cross * Mathf.Lerp(-62f, 62f, (float)random.NextDouble());
        }

        private static void ConsiderSpawnBoundary(
            float boundary,
            bool verticalBoundary,
            Vector2 target,
            Vector2 back,
            Vector2 min,
            Vector2 max,
            ref float bestDistance,
            ref Vector2 bestPoint)
        {
            var axis = verticalBoundary ? back.x : back.y;
            if (Mathf.Abs(axis) <= 0.0001f)
            {
                return;
            }

            var distance = (boundary - (verticalBoundary ? target.x : target.y)) / axis;
            if (distance <= 70f || distance >= bestDistance)
            {
                return;
            }

            var point = target + back * distance;
            if (point.x < min.x - 1f || point.x > max.x + 1f || point.y < min.y - 1f || point.y > max.y + 1f)
            {
                return;
            }

            bestDistance = distance;
            bestPoint = point;
        }

        private void UpdateRaindrops(float deltaTime)
        {
            var rect = playRoot.rect;
            for (var i = 0; i < raindrops.Length; i++)
            {
                if (!raindrops[i].Active)
                {
                    continue;
                }

                var previous = raindrops[i].Position;
                var current = previous + raindrops[i].Velocity * deltaTime;
                raindrops[i].PreviousPosition = previous;
                raindrops[i].Position = current;

                Vector2 splashPoint;
                if (IsBlockedByUmbrella(previous, current, out splashPoint))
                {
                    raindrops[i].Active = false;
                    blockedCount++;
                    AddSplash(splashPoint, raindrops[i].Seed);
                    PlayClip(blockClip);
                    continue;
                }

                if (HitsCharacter(previous, current, out splashPoint))
                {
                    raindrops[i].Active = false;
                    wetHitCount++;
                    AddSplash(splashPoint, raindrops[i].Seed + 17);
                    AddWetMark(splashPoint, raindrops[i].Seed + 43);
                    PlayClip(hitClip);
                    continue;
                }

                if (current.y < rect.yMin - 180f || current.x < rect.xMin - 320f || current.x > rect.xMax + 320f)
                {
                    raindrops[i].Active = false;
                }
            }

            rainGraphic.SetVerticesDirty();
        }

        private bool IsBlockedByUmbrella(Vector2 previous, Vector2 current, out Vector2 hitPoint)
        {
            var endpoints = UmbrellaEndpoints();
            hitPoint = ClosestPointOnSegment(current, endpoints.Start, endpoints.End);
            var distance = DistanceBetweenSegments(previous, current, endpoints.Start, endpoints.End);
            return distance <= 30f;
        }

        private bool HitsCharacter(Vector2 previous, Vector2 current, out Vector2 hitPoint)
        {
            var headDistance = DistanceSegmentToPoint(previous, current, HeadCenter, out hitPoint);
            if (headDistance <= 58f)
            {
                return true;
            }

            var bodyDistance = DistanceBetweenSegments(previous, current, BodyTop, BodyBottom);
            hitPoint = ClosestPointOnSegment(current, BodyTop, BodyBottom);
            if (bodyDistance <= 56f)
            {
                return true;
            }

            var leftLeg = DistanceBetweenSegments(previous, current, BodyBottom, new Vector2(-64f, -405f));
            if (leftLeg <= 34f)
            {
                hitPoint = ClosestPointOnSegment(current, BodyBottom, new Vector2(-64f, -405f));
                return true;
            }

            var rightLeg = DistanceBetweenSegments(previous, current, BodyBottom, new Vector2(70f, -402f));
            hitPoint = ClosestPointOnSegment(current, BodyBottom, new Vector2(70f, -402f));
            return rightLeg <= 34f;
        }

        private UmbrellaSegment UmbrellaEndpoints()
        {
            var rotation = Quaternion.Euler(0f, 0f, umbrellaAngle);
            var start = UmbrellaPivot + (Vector2)(rotation * new Vector3(-238f, 236f, 0f));
            var end = UmbrellaPivot + (Vector2)(rotation * new Vector3(238f, 232f, 0f));
            return new UmbrellaSegment(start, end);
        }

        private void AddSplash(Vector2 position, int seed)
        {
            splashes[nextSplash] = new RainwalkerSplash
            {
                Active = true,
                Position = position,
                Age = 0f,
                Duration = 0.34f,
                Seed = seed
            };
            nextSplash = (nextSplash + 1) % splashes.Length;
            splashGraphic.SetVerticesDirty();
        }

        private void AddWetMark(Vector2 position, int seed)
        {
            var bodyPoint = new Vector2(Mathf.Clamp(position.x, -60f, 55f), Mathf.Clamp(position.y, -275f, 72f));
            wetMarks[nextWetMark] = new RainwalkerWetMark
            {
                Active = true,
                Position = bodyPoint,
                Radius = Mathf.Lerp(18f, 42f, (float)random.NextDouble()),
                Seed = seed,
                Alpha = 0.16f
            };
            nextWetMark = (nextWetMark + 1) % wetMarks.Length;
            wetMarkGraphic.SetVerticesDirty();
        }

        private void UpdateEffects(float deltaTime)
        {
            for (var i = 0; i < splashes.Length; i++)
            {
                if (!splashes[i].Active)
                {
                    continue;
                }

                splashes[i].Age += deltaTime;
                if (splashes[i].Age >= splashes[i].Duration)
                {
                    splashes[i].Active = false;
                }
            }

            splashGraphic.TimeSeed = Time.time;
            splashGraphic.SetVerticesDirty();
        }

        private void ShowResult()
        {
            state = RainwalkerGameState.Result;
            ClearRain();
            var score = RainwalkerRules.ScoreForHits(wetHitCount);
            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }

            resultTitleText.text = RainwalkerRules.GradeForScore(score);
            resultScoreText.text = $"Score {score}\nRain hits {wetHitCount}\nBlocked {blockedCount}\nBest {bestScore}";
            resultPanel.SetActive(true);
            UpdateHud();
        }

        private void ClearRain()
        {
            for (var i = 0; i < raindrops.Length; i++)
            {
                raindrops[i].Active = false;
            }

            for (var i = 0; i < splashes.Length; i++)
            {
                splashes[i].Active = false;
            }

            for (var i = 0; i < wetMarks.Length; i++)
            {
                wetMarks[i].Active = false;
            }

            rainGraphic.SetVerticesDirty();
            splashGraphic.SetVerticesDirty();
            wetMarkGraphic.SetVerticesDirty();
        }

        private void UpdateHud()
        {
            var remaining = Mathf.Max(0f, RainwalkerRules.RoundSeconds - elapsedSeconds);
            var score = RainwalkerRules.ScoreForHits(wetHitCount);
            timerText.text = $"{Mathf.CeilToInt(remaining):00}s";
            hitsText.text = $"Hits {wetHitCount}";
            scoreText.text = $"Score {score}";
        }

        private void ApplyUmbrellaVisual()
        {
            umbrellaRoot.localRotation = Quaternion.Euler(0f, 0f, umbrellaAngle);
        }

        private void BuildInterface()
        {
            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            playRoot = new GameObject("Play Root", typeof(RectTransform)).GetComponent<RectTransform>();
            playRoot.transform.SetParent(canvas.transform, false);
            Stretch(playRoot, Vector2.zero, Vector2.zero);

            windGraphic = AddGraphic<DoodleWindGraphic>(playRoot, "Rain Direction");
            Stretch(windGraphic.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            windGraphic.raycastTarget = false;

            rainGraphic = AddGraphic<DoodleRainGraphic>(playRoot, "Doodle Rain");
            Stretch(rainGraphic.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            rainGraphic.Drops = raindrops;
            rainGraphic.raycastTarget = false;

            characterGraphic = AddGraphic<DoodleCharacterGraphic>(playRoot, "Rainwalker");
            var characterRect = characterGraphic.GetComponent<RectTransform>();
            characterRect.anchorMin = new Vector2(0.5f, 0.5f);
            characterRect.anchorMax = new Vector2(0.5f, 0.5f);
            characterRect.pivot = new Vector2(0.5f, 0.5f);
            characterRect.sizeDelta = new Vector2(360f, 620f);
            characterRect.anchoredPosition = new Vector2(0f, -155f);
            characterGraphic.raycastTarget = false;

            wetMarkGraphic = AddGraphic<DoodleWetMarkGraphic>(playRoot, "Wet Marks");
            Stretch(wetMarkGraphic.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            wetMarkGraphic.Marks = wetMarks;
            wetMarkGraphic.raycastTarget = false;

            umbrellaRoot = new GameObject("Umbrella Root", typeof(RectTransform)).GetComponent<RectTransform>();
            umbrellaRoot.transform.SetParent(playRoot, false);
            umbrellaRoot.anchorMin = new Vector2(0.5f, 0.5f);
            umbrellaRoot.anchorMax = new Vector2(0.5f, 0.5f);
            umbrellaRoot.pivot = new Vector2(0.5f, 0f);
            umbrellaRoot.sizeDelta = new Vector2(560f, 430f);
            umbrellaRoot.anchoredPosition = UmbrellaPivot;

            var umbrella = AddGraphic<DoodleUmbrellaGraphic>(umbrellaRoot, "Doodle Umbrella");
            Stretch(umbrella.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            umbrella.raycastTarget = false;

            splashGraphic = AddGraphic<DoodleSplashGraphic>(playRoot, "Splash Marks");
            Stretch(splashGraphic.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            splashGraphic.Splashes = splashes;
            splashGraphic.raycastTarget = false;

            var safeRoot = SketchUiFactory.CreateSafeAreaRoot(canvas.transform);
            CreateHeader(safeRoot);
            CreateReadyPanel(safeRoot);
            CreateResultPanel(safeRoot);
        }

        private void CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            var rect = header.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(36f, -128f);
            rect.offsetMax = new Vector2(-36f, -26f);

            timerText = CreateText(header.transform, "30s", 48, TextAnchor.MiddleLeft);
            SetAnchor(timerText.GetComponent<RectTransform>(), 0f, 0f, 0.25f, 1f);

            hitsText = CreateText(header.transform, "Hits 0", 34, TextAnchor.MiddleCenter);
            SetAnchor(hitsText.GetComponent<RectTransform>(), 0.28f, 0f, 0.66f, 1f);

            scoreText = CreateText(header.transform, "Score 1000", 34, TextAnchor.MiddleRight);
            SetAnchor(scoreText.GetComponent<RectTransform>(), 0.66f, 0f, 1f, 1f);
        }

        private void CreateReadyPanel(Transform parent)
        {
            readyPanel = new GameObject("Ready Panel", typeof(RectTransform));
            readyPanel.transform.SetParent(parent, false);
            var rect = readyPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.12f);
            rect.anchorMax = new Vector2(0.5f, 0.12f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(620f, 190f);
            rect.anchoredPosition = Vector2.zero;

            var title = CreateText(readyPanel.transform, "RAINWALKER", 56, TextAnchor.MiddleCenter);
            SetAnchor(title.GetComponent<RectTransform>(), 0f, 0.48f, 1f, 1f);

            var startButton = CreateSketchButton(readyPanel.transform, "START", 42);
            var buttonRect = startButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(300f, 96f);
            buttonRect.anchoredPosition = Vector2.zero;
            startButton.onClick.AddListener(StartRun);
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

            resultTitleText = CreateText(resultPanel.transform, "S", 72, TextAnchor.MiddleCenter);
            SetAnchor(resultTitleText.GetComponent<RectTransform>(), 0f, 0.66f, 1f, 0.94f, 30f, 0f, -30f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "Score 1000", 38, TextAnchor.MiddleCenter);
            SetAnchor(resultScoreText.GetComponent<RectTransform>(), 0f, 0.28f, 1f, 0.67f, 36f, 0f, -36f, 0f);

            var restart = CreateSketchButton(resultPanel.transform, "AGAIN", 38);
            var restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.08f);
            restartRect.anchorMax = new Vector2(0.5f, 0.08f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.sizeDelta = new Vector2(280f, 92f);
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

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static T AddGraphic<T>(Transform parent, string name) where T : Graphic
        {
            var graphic = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
            graphic.transform.SetParent(parent, false);
            return graphic;
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

        private static float DistanceBetweenSegments(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (SegmentsIntersect(a, b, c, d))
            {
                return 0f;
            }

            Vector2 unused;
            return Mathf.Min(
                Mathf.Min(DistanceSegmentToPoint(a, b, c, out unused), DistanceSegmentToPoint(a, b, d, out unused)),
                Mathf.Min(DistanceSegmentToPoint(c, d, a, out unused), DistanceSegmentToPoint(c, d, b, out unused)));
        }

        private static float DistanceSegmentToPoint(Vector2 a, Vector2 b, Vector2 point, out Vector2 closest)
        {
            closest = ClosestPointOnSegment(point, a, b);
            return Vector2.Distance(point, closest);
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return a;
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
            return a + segment * t;
        }

        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var denominator = (b.x - a.x) * (d.y - c.y) - (b.y - a.y) * (d.x - c.x);
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return false;
            }

            var u = ((c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x)) / denominator;
            var t = ((c.x - a.x) * (d.y - c.y) - (c.y - a.y) * (d.x - c.x)) / denominator;
            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip);
        }

        private static AudioClip CreateToneClip(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 40f);
                samples[i] = Mathf.Sin(Mathf.PI * 2f * frequency * t) * envelope * volume;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private readonly struct UmbrellaSegment
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;

            public UmbrellaSegment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }
        }
    }

    public struct RainwalkerRaindrop
    {
        public bool Active;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Velocity;
        public float Length;
        public float Width;
        public int Seed;
    }

    public struct RainwalkerSplash
    {
        public bool Active;
        public Vector2 Position;
        public float Age;
        public float Duration;
        public int Seed;
    }

    public struct RainwalkerWetMark
    {
        public bool Active;
        public Vector2 Position;
        public float Radius;
        public int Seed;
        public float Alpha;
    }

    public sealed class DoodleWindGraphic : Graphic
    {
        public Vector2 Direction = new Vector2(0.2f, -1f).normalized;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var ink = new Color32(61, 91, 112, 75);
            var dir = Direction.sqrMagnitude > 0.01f ? Direction.normalized : Vector2.down;
            var normal = new Vector2(-dir.y, dir.x);
            for (var i = 0; i < 11; i++)
            {
                var y = Mathf.Lerp(rect.yMax - 260f, rect.yMin + 330f, i / 10f);
                var center = new Vector2(Mathf.Lerp(rect.xMin + 70f, rect.xMax - 70f, Fract(i * 0.372f)), y);
                var start = center - dir * 54f + normal * Mathf.Sin(i * 1.7f) * 18f;
                var end = center + dir * 54f;
                DoodleMesh.AddLine(vh, start, end, 4f, ink);
            }
        }

        private static float Fract(float value)
        {
            return value - Mathf.Floor(value);
        }
    }

    public sealed class DoodleRainGraphic : Graphic
    {
        public RainwalkerRaindrop[] Drops;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Drops == null)
            {
                return;
            }

            for (var i = 0; i < Drops.Length; i++)
            {
                if (!Drops[i].Active)
                {
                    continue;
                }

                var dir = Drops[i].Velocity.sqrMagnitude > 0.01f ? Drops[i].Velocity.normalized : Vector2.down;
                var wobble = new Vector2(Mathf.Sin((Drops[i].Seed + Time.time) * 2.1f), Mathf.Cos(Drops[i].Seed * 0.73f)) * 3.2f;
                var end = Drops[i].Position + wobble;
                var start = end - dir * Drops[i].Length + wobble * 0.35f;
                var blue = new Color32(73, 130, 164, 190);
                DoodleMesh.AddLine(vh, start, end, Drops[i].Width, blue);
                if ((Drops[i].Seed & 3) == 0)
                {
                    DoodleMesh.AddLine(vh, start + new Vector2(4f, -2f), end + new Vector2(5f, 1f), Drops[i].Width * 0.45f, new Color32(44, 90, 125, 110));
                }
            }
        }
    }

    public sealed class DoodleCharacterGraphic : Graphic
    {
        public float WalkPhase;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var ink = SketchPalette.Ink;
            var shirt = new Color32(242, 185, 67, 210);
            var skin = new Color32(245, 181, 128, 230);
            var footSwing = Mathf.Sin(WalkPhase) * 28f;
            var armSwing = Mathf.Sin(WalkPhase + Mathf.PI) * 22f;

            DoodleMesh.AddFilledCircle(vh, new Vector2(0f, 220f), 54f, 18, skin);
            DoodleMesh.AddCircle(vh, new Vector2(0f, 220f), 56f, 20, 5f, ink, 5, 10f);
            DoodleMesh.AddLine(vh, new Vector2(-18f, 232f), new Vector2(-6f, 235f), 4f, ink);
            DoodleMesh.AddLine(vh, new Vector2(16f, 232f), new Vector2(25f, 229f), 4f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-9f, 205f), new Vector2(18f, 205f), 4f, ink);

            var bodyA = new Vector2(-48f, 145f);
            var bodyB = new Vector2(52f, 136f);
            var bodyC = new Vector2(42f, -110f);
            var bodyD = new Vector2(-45f, -118f);
            DoodleMesh.AddPolygon(vh, new[] { bodyA, bodyB, bodyC, bodyD }, shirt);
            DoodleMesh.AddPolyline(vh, new[] { bodyA, bodyB, bodyC, bodyD, bodyA }, 6f, ink, 3, 8f);

            DoodleMesh.AddLine(vh, new Vector2(48f, 104f), new Vector2(94f, 68f + armSwing * 0.18f), 9f, ink);
            DoodleMesh.AddLine(vh, new Vector2(94f, 68f + armSwing * 0.18f), new Vector2(134f, 100f), 8f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-46f, 88f), new Vector2(-96f, 46f - armSwing * 0.12f), 8f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-96f, 46f - armSwing * 0.12f), new Vector2(-78f, -20f), 7f, ink);

            DoodleMesh.AddLine(vh, new Vector2(-22f, -110f), new Vector2(-54f - footSwing * 0.2f, -262f), 11f, ink);
            DoodleMesh.AddLine(vh, new Vector2(24f, -112f), new Vector2(64f + footSwing * 0.2f, -260f), 11f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-54f - footSwing * 0.2f, -262f), new Vector2(-104f - footSwing, -282f), 9f, ink);
            DoodleMesh.AddLine(vh, new Vector2(64f + footSwing * 0.2f, -260f), new Vector2(116f + footSwing, -272f), 9f, ink);
        }
    }

    public sealed class DoodleUmbrellaGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var canopy = new Color32(108, 185, 206, 222);
            var shade = new Color32(61, 139, 162, 132);
            var ink = SketchPalette.Ink;

            var arc = new Vector2[15];
            for (var i = 0; i < arc.Length; i++)
            {
                var t = i / (float)(arc.Length - 1);
                var x = Mathf.Lerp(-244f, 244f, t);
                var y = 224f + Mathf.Sin(t * Mathf.PI) * 128f + Mathf.Sin(i * 2.1f) * 5f;
                arc[i] = new Vector2(x, y);
            }

            var lowerCenter = new Vector2(0f, 216f);
            for (var i = 0; i < arc.Length - 1; i++)
            {
                DoodleMesh.AddTriangle(vh, lowerCenter, arc[i], arc[i + 1], canopy);
            }

            DoodleMesh.AddPolyline(vh, arc, 8f, ink, 3, 9f);
            DoodleMesh.AddLine(vh, new Vector2(-244f, 224f), new Vector2(244f, 218f), 7f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-220f, 226f), new Vector2(-138f, 188f), 6f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-138f, 188f), new Vector2(-42f, 226f), 6f, ink);
            DoodleMesh.AddLine(vh, new Vector2(-42f, 226f), new Vector2(50f, 188f), 6f, ink);
            DoodleMesh.AddLine(vh, new Vector2(50f, 188f), new Vector2(142f, 224f), 6f, ink);
            DoodleMesh.AddLine(vh, new Vector2(142f, 224f), new Vector2(232f, 216f), 6f, ink);

            for (var i = 2; i < arc.Length - 2; i += 3)
            {
                DoodleMesh.AddLine(vh, new Vector2(0f, 204f), arc[i], 4f, shade);
            }

            DoodleMesh.AddLine(vh, new Vector2(0f, 208f), new Vector2(0f, 34f), 8f, ink);
            DoodleMesh.AddLine(vh, new Vector2(0f, 34f), new Vector2(42f, 22f), 8f, ink);
            DoodleMesh.AddLine(vh, new Vector2(42f, 22f), new Vector2(52f, 56f), 7f, ink);
        }
    }

    public sealed class DoodleSplashGraphic : Graphic
    {
        public RainwalkerSplash[] Splashes;
        public float TimeSeed;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Splashes == null)
            {
                return;
            }

            for (var i = 0; i < Splashes.Length; i++)
            {
                if (!Splashes[i].Active)
                {
                    continue;
                }

                var t = Mathf.Clamp01(Splashes[i].Age / Splashes[i].Duration);
                var alpha = (byte)Mathf.RoundToInt(Mathf.Lerp(190f, 20f, t));
                var color = new Color32(36, 105, 145, alpha);
                for (var n = 0; n < 5; n++)
                {
                    var angle = (Splashes[i].Seed * 0.91f + n * 1.27f + TimeSeed * 0.2f) % (Mathf.PI * 2f);
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var start = Splashes[i].Position + dir * (8f + t * 18f);
                    var end = Splashes[i].Position + dir * (28f + t * 42f);
                    DoodleMesh.AddLine(vh, start, end, 4f, color);
                }
            }
        }
    }

    public sealed class DoodleWetMarkGraphic : Graphic
    {
        public RainwalkerWetMark[] Marks;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Marks == null)
            {
                return;
            }

            for (var i = 0; i < Marks.Length; i++)
            {
                if (!Marks[i].Active)
                {
                    continue;
                }

                var alpha = (byte)Mathf.RoundToInt(Marks[i].Alpha * 255f);
                var color = new Color32(38, 88, 120, alpha);
                DoodleMesh.AddFilledCircle(vh, Marks[i].Position, Marks[i].Radius, 12, color);
                DoodleMesh.AddFilledCircle(vh, Marks[i].Position + new Vector2(Marks[i].Radius * 0.42f, -Marks[i].Radius * 0.18f), Marks[i].Radius * 0.56f, 9, color);
            }
        }
    }

    public static class DoodleMesh
    {
        public static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color color)
        {
            var direction = end - start;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            direction.Normalize();
            var normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);
            var index = vh.currentVertCount;
            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        public static void AddPolyline(VertexHelper vh, Vector2[] points, float thickness, Color color, int strokes, float jitter)
        {
            for (var stroke = 0; stroke < strokes; stroke++)
            {
                for (var i = 0; i < points.Length - 1; i++)
                {
                    var start = points[i] + JitterOffset(i + stroke * 31, jitter);
                    var end = points[i + 1] + JitterOffset(i + 1 + stroke * 31, jitter);
                    AddLine(vh, start, end, thickness, color);
                }
            }
        }

        public static void AddCircle(VertexHelper vh, Vector2 center, float radius, int segments, float thickness, Color color, int strokes, float jitter)
        {
            var points = new Vector2[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            AddPolyline(vh, points, thickness, color, strokes, jitter);
        }

        public static void AddPolygon(VertexHelper vh, Vector2[] points, Color color)
        {
            if (points.Length < 3)
            {
                return;
            }

            for (var i = 1; i < points.Length - 1; i++)
            {
                AddTriangle(vh, points[0], points[i], points[i + 1], color);
            }
        }

        public static void AddFilledCircle(VertexHelper vh, Vector2 center, float radius, int segments, Color color)
        {
            for (var i = 0; i < segments; i++)
            {
                var a = Mathf.PI * 2f * i / segments;
                var b = Mathf.PI * 2f * (i + 1) / segments;
                AddTriangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
            }
        }

        public static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var index = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
        }

        private static Vector2 JitterOffset(int seed, float amount)
        {
            var x = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
            var y = Mathf.Sin((seed + 17) * 78.233f) * 24634.6345f;
            return new Vector2((Fract(x) - 0.5f) * amount, (Fract(y) - 0.5f) * amount);
        }

        private static float Fract(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}
