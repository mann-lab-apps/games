using System;
using System.Collections.Generic;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.GatherAndShot
{
    public sealed class GatherAndShotController : MonoBehaviour
    {
        private const string BestScoreKey = "mannlab.gather_and_shot.best_score";
        private const float WorldHalfHeight = 6.6f;
        private const float WorldHalfWidth = 3.7f;
        private const float WarmthBarWidth = 620f;
        private const float DirectionInputDeadZone = 26f;
        private const float DirectionInputMaxDistance = 180f;
        private const float JoystickVisualRadius = 66f;

        private static readonly Color SnowTint = new Color32(239, 249, 255, 255);
        private static readonly Color PaperBlue = new Color32(218, 238, 242, 255);
        private static readonly Color WarmthColor = new Color32(239, 126, 87, 255);
        private static readonly Color AmmoColor = new Color32(88, 166, 206, 255);
        private static readonly Color RunnerTint = new Color32(236, 94, 123, 255);
        private static readonly Color WalkerTint = new Color32(70, 151, 174, 255);
        private static readonly Color HeavyTint = new Color32(107, 92, 130, 255);

        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<Pickup> pickups = new List<Pickup>();
        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly List<Burst> bursts = new List<Burst>();
        private readonly System.Random random = new System.Random(Environment.TickCount);

        private Camera worldCamera;
        private Sprite playerSprite;
        private Sprite walkerSprite;
        private Sprite runnerSprite;
        private Sprite heavySprite;
        private Sprite snowballSprite;
        private Sprite driftSprite;
        private Sprite puffSprite;
        private SpriteRenderer playerRenderer;
        private RectTransform joystickBase;
        private RectTransform joystickKnob;
        private RectTransform joystickRoot;
        private Text scoreText;
        private Text bestText;
        private Text ammoText;
        private Image warmthFill;
        private GameObject resultPanel;
        private Text resultScoreText;
        private Vector2 playerPosition;
        private Vector2 playerVelocity;
        private Vector2 joystickVector;
        private GatherAndShotGameState state;
        private float warmth;
        private int ammo;
        private int score;
        private int bestScore;
        private float elapsedSeconds;
        private float nextSpawnAt;
        private float nextPickupAt;
        private float nextFireAt;
        private float contactReadyAt;
        private bool joystickHeld;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                worldCamera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                worldCamera.tag = "MainCamera";
            }

            worldCamera.orthographic = true;
            worldCamera.orthographicSize = WorldHalfHeight;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = SketchPalette.Paper;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);

            LoadSprites();
            BuildWorld();
            BuildHud();
            StartRun();
        }

        private void Update()
        {
            if (state == GatherAndShotGameState.GameOver)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            UpdateJoystick();
            MovePlayer();
            UpdatePickups();
            UpdateEnemies();
            UpdateProjectiles();
            UpdateBursts();
            TryAutoFire();
            TrySpawnEnemy();
            TrySpawnPickup();
            UpdateHud();
        }

        private void StartRun()
        {
            ClearActors();
            state = GatherAndShotGameState.Playing;
            playerPosition = Vector2.zero;
            playerVelocity = Vector2.zero;
            joystickVector = Vector2.zero;
            joystickHeld = false;
            warmth = GatherAndShotBalance.MaxWarmth;
            ammo = 3;
            score = 0;
            elapsedSeconds = 0f;
            nextFireAt = 0f;
            contactReadyAt = 0f;
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            resultPanel.SetActive(false);
            playerRenderer.transform.position = playerPosition;

            for (var i = 0; i < 7; i++)
            {
                SpawnPickup(i % 3 == 0);
            }

            nextSpawnAt = Time.time + 0.45f;
            nextPickupAt = Time.time + 0.25f;
            UpdateHud();
            UpdateJoystickVisual(false);
        }

        private void LoadSprites()
        {
            playerSprite = LoadSprite("player", new Color32(73, 150, 202, 255), 128);
            walkerSprite = LoadSprite("walker", WalkerTint, 128);
            runnerSprite = LoadSprite("runner", RunnerTint, 128);
            heavySprite = LoadSprite("heavy", HeavyTint, 128);
            snowballSprite = LoadSprite("snowball", Color.white, 96);
            driftSprite = LoadSprite("snowdrift", new Color32(226, 244, 251, 255), 128);
            puffSprite = LoadSprite("puff", new Color32(238, 249, 255, 210), 96);
        }

        private void BuildWorld()
        {
            var bg = new GameObject("Snow Paper", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            bg.sprite = CreateSolidSprite("SnowPaper", 768, 1365, SnowTint, 64f);
            bg.transform.position = new Vector3(0f, 0f, 8f);
            bg.sortingOrder = -20;

            BuildBoundaryMarkers();

            for (var i = 0; i < 18; i++)
            {
                var stroke = new GameObject("Sketch Snow Line", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                stroke.sprite = CreateSolidSprite("SnowLine", 80 + random.Next(90), 4, PaperBlue, 32f);
                stroke.transform.position = new Vector3(
                    RandomRange(-PlayHalfWidth, PlayHalfWidth),
                    RandomRange(-WorldHalfHeight + 0.5f, WorldHalfHeight - 0.5f),
                    6f);
                stroke.transform.rotation = Quaternion.Euler(0f, 0f, RandomRange(-13f, 13f));
                stroke.sortingOrder = -15;
            }

            playerRenderer = new GameObject("Player", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            playerRenderer.sprite = playerSprite;
            playerRenderer.sortingOrder = 20;
            playerRenderer.transform.localScale = Vector3.one * 0.82f;
        }

        private void BuildHud()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);

            var safe = SketchUiFactory.CreateSafeAreaRoot(canvasObject.transform);
            var top = CreateRect(safe, "Top HUD", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -218f), new Vector2(0f, -24f));
            bestText = CreateText(top, "Best", "BEST 0", 32, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(36f, -18f), new Vector2(300f, 56f));
            scoreText = CreateText(top, "Score", "0", 60, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(260f, 78f));
            ammoText = CreateText(top, "Ammo", "SNOW x3", 34, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-36f, -22f), new Vector2(300f, 58f));

            var warmthBack = CreatePanel(top, "Warmth Back", new Vector2(0.5f, 1f), new Vector2(WarmthBarWidth, 38f), SketchPalette.WarmShadow);
            warmthBack.anchoredPosition = new Vector2(0f, -112f);
            warmthFill = CreateImage(warmthBack, "Warmth Fill", WarmthColor);
            warmthFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            warmthFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            warmthFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            warmthFill.rectTransform.offsetMin = Vector2.zero;
            warmthFill.rectTransform.offsetMax = Vector2.zero;

            joystickRoot = safe;
            joystickBase = CreatePanel(safe, "Move Direction Guide", new Vector2(0.5f, 0.5f), new Vector2(230f, 230f), new Color32(255, 253, 247, 118));
            joystickKnob = CreatePanel(joystickBase, "Joystick Knob", new Vector2(0.5f, 0.5f), new Vector2(86f, 86f), new Color32(88, 166, 206, 210));
            joystickBase.gameObject.SetActive(false);

            resultPanel = CreatePanel(safe, "Result Panel", new Vector2(0.5f, 0.5f), new Vector2(660f, 520f), SketchPalette.TilePaper).gameObject;
            CreateText(resultPanel.transform, "Result Title", "GAME OVER", 58, TextAnchor.MiddleCenter, new Vector2(0f, 142f), new Vector2(560f, 82f));
            resultScoreText = CreateText(resultPanel.transform, "Result Score", "0", 38, TextAnchor.MiddleCenter, new Vector2(0f, 54f), new Vector2(560f, 68f));
            var again = CreateButton(resultPanel.transform, "Again Button", "Again", new Vector2(0f, -120f), new Vector2(260f, 92f));
            again.onClick.AddListener(StartRun);
            resultPanel.SetActive(false);
        }

        private void UpdateJoystick()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    BeginJoystick(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    DragJoystick(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndJoystick();
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginJoystick(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                DragJoystick(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndJoystick();
            }
        }

        private void BeginJoystick(Vector2 screenPosition)
        {
            joystickHeld = true;
            UpdateJoystickVisual(true);
            DragJoystick(screenPosition);
        }

        private void DragJoystick(Vector2 screenPosition)
        {
            if (!joystickHeld)
            {
                return;
            }

            PositionJoystickGuideAtPlayer();
            var playerScreen = (Vector2)worldCamera.WorldToScreenPoint(playerPosition);
            var delta = screenPosition - playerScreen;
            joystickVector = delta.magnitude <= DirectionInputDeadZone
                ? Vector2.zero
                : Vector2.ClampMagnitude(delta, DirectionInputMaxDistance) / DirectionInputMaxDistance;
            joystickKnob.anchoredPosition = joystickVector * JoystickVisualRadius;
        }

        private void EndJoystick()
        {
            joystickHeld = false;
            joystickVector = Vector2.zero;
            UpdateJoystickVisual(false);
        }

        private void UpdateJoystickVisual(bool held)
        {
            joystickBase.gameObject.SetActive(held);
            joystickKnob.anchoredPosition = Vector2.zero;
            joystickBase.localScale = held ? Vector3.one * 1.05f : Vector3.one;
            if (held)
            {
                PositionJoystickGuideAtPlayer();
            }
        }

        private void MovePlayer()
        {
            var speed = GatherAndShotBalance.PlayerSpeed(elapsedSeconds);
            playerVelocity = Vector2.Lerp(playerVelocity, joystickVector * speed, Time.deltaTime * 12f);
            playerPosition += playerVelocity * Time.deltaTime;
            playerPosition.x = Mathf.Clamp(playerPosition.x, -PlayHalfWidth + 0.38f, PlayHalfWidth - 0.38f);
            playerPosition.y = Mathf.Clamp(playerPosition.y, -WorldHalfHeight + 0.55f, WorldHalfHeight - 0.55f);
            playerRenderer.transform.position = playerPosition;
            if (playerVelocity.sqrMagnitude > 0.05f)
            {
                playerRenderer.transform.localScale = new Vector3(playerVelocity.x < -0.05f ? -0.82f : 0.82f, 0.82f, 1f);
            }

            if (joystickHeld)
            {
                PositionJoystickGuideAtPlayer();
            }
        }

        private void TrySpawnEnemy()
        {
            if (Time.time < nextSpawnAt || enemies.Count >= GatherAndShotBalance.MaxLiveEnemies(elapsedSeconds))
            {
                return;
            }

            SpawnEnemy(GatherAndShotBalance.RollEnemyKind(random, elapsedSeconds));
            nextSpawnAt = Time.time + GatherAndShotBalance.SpawnGap(elapsedSeconds);
        }

        private void SpawnEnemy(EnemyKind kind)
        {
            var angle = RandomRange(0f, Mathf.PI * 2f);
            var spawn = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RandomRange(6.2f, 7.8f);
            spawn.x = Mathf.Clamp(spawn.x, -PlayHalfWidth - 0.9f, PlayHalfWidth + 0.9f);
            spawn.y = Mathf.Clamp(spawn.y, -WorldHalfHeight - 0.9f, WorldHalfHeight + 0.9f);

            var renderer = new GameObject(kind.ToString(), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = kind == EnemyKind.Runner ? runnerSprite : kind == EnemyKind.Heavy ? heavySprite : walkerSprite;
            renderer.color = kind == EnemyKind.Runner ? RunnerTint : kind == EnemyKind.Heavy ? HeavyTint : WalkerTint;
            renderer.sortingOrder = 12;
            renderer.transform.position = spawn;
            renderer.transform.localScale = Vector3.one * (kind == EnemyKind.Heavy ? 1.05f : kind == EnemyKind.Runner ? 0.68f : 0.82f);

            enemies.Add(new Enemy
            {
                Kind = kind,
                Renderer = renderer,
                Position = spawn,
                Health = GatherAndShotBalance.StartingHealth(kind),
                Seed = RandomRange(0f, 100f)
            });
        }

        private void TrySpawnPickup()
        {
            if (Time.time < nextPickupAt || pickups.Count > 18)
            {
                return;
            }

            SpawnPickup(random.NextDouble() < 0.28d);
            nextPickupAt = Time.time + RandomRange(0.55f, 1.15f);
        }

        private void SpawnPickup(bool drift)
        {
            var renderer = new GameObject(drift ? "Snowdrift" : "Snowball Pickup", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = drift ? driftSprite : snowballSprite;
            renderer.sortingOrder = 4;
            renderer.transform.position = new Vector3(RandomRange(-PlayHalfWidth + 0.4f, PlayHalfWidth - 0.4f), RandomRange(-WorldHalfHeight + 0.75f, WorldHalfHeight - 0.75f), 0f);
            renderer.transform.localScale = Vector3.one * (drift ? 0.74f : 0.48f);
            pickups.Add(new Pickup
            {
                Drift = drift,
                Renderer = renderer,
                Position = renderer.transform.position
            });
        }

        private void UpdatePickups()
        {
            for (var i = pickups.Count - 1; i >= 0; i--)
            {
                var pickup = pickups[i];
                pickup.Renderer.transform.Rotate(0f, 0f, (pickup.Drift ? 10f : -18f) * Time.deltaTime);
                if (Vector2.Distance(playerPosition, pickup.Position) <= GatherAndShotBalance.PickupRadius)
                {
                    ammo = Mathf.Min(GatherAndShotBalance.MaxAmmo, ammo + GatherAndShotBalance.PickupAmmo(pickup.Drift ? "Drift" : "Ball"));
                    SpawnBurst(pickup.Position, 0.62f, 5);
                    Destroy(pickup.Renderer.gameObject);
                    pickups.RemoveAt(i);
                }
            }
        }

        private void UpdateEnemies()
        {
            var speedMultiplier = GatherAndShotBalance.EnemySpeedMultiplier(elapsedSeconds);
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                var toPlayer = playerPosition - enemy.Position;
                var direction = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
                var wobble = new Vector2(Mathf.Sin(Time.time * 2.1f + enemy.Seed), Mathf.Cos(Time.time * 1.7f + enemy.Seed)) * 0.16f;
                enemy.Position += (direction + wobble).normalized * GatherAndShotBalance.EnemyBaseSpeed(enemy.Kind) * speedMultiplier * Time.deltaTime;
                enemy.Renderer.transform.position = enemy.Position;
                enemy.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 7f + enemy.Seed) * 3.5f);
                if (direction.x != 0f)
                {
                    var scale = enemy.Renderer.transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (direction.x < 0f ? -1f : 1f);
                    enemy.Renderer.transform.localScale = scale;
                }

                if (Time.time >= contactReadyAt
                    && Vector2.Distance(playerPosition, enemy.Position) <= GatherAndShotBalance.EnemyContactRadius(enemy.Kind))
                {
                    warmth = GatherAndShotBalance.ApplyContactDamage(warmth);
                    contactReadyAt = Time.time + GatherAndShotBalance.ContactCooldownSeconds;
                    playerVelocity = (playerPosition - enemy.Position).normalized * 5.2f;
                    SpawnBurst(playerPosition, 0.95f, 8);
                    if (GatherAndShotBalance.IsGameOver(warmth))
                    {
                        EndRun();
                        return;
                    }
                }
            }
        }

        private void TryAutoFire()
        {
            if (ammo <= 0 || Time.time < nextFireAt)
            {
                return;
            }

            var target = FindNearestEnemyInRange();
            if (target == null)
            {
                return;
            }

            ammo--;
            nextFireAt = Time.time + GatherAndShotBalance.FireCooldownSeconds;
            var renderer = new GameObject("Snowball Projectile", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = snowballSprite;
            renderer.sortingOrder = 18;
            renderer.transform.position = playerPosition;
            renderer.transform.localScale = Vector3.one * 0.38f;
            projectiles.Add(new Projectile
            {
                Renderer = renderer,
                Position = playerPosition,
                Target = target,
                Direction = (target.Position - playerPosition).normalized,
                Life = 1.2f
            });
        }

        private Enemy FindNearestEnemyInRange()
        {
            Enemy nearest = null;
            var bestDistance = GatherAndShotBalance.FireRange * GatherAndShotBalance.FireRange;
            foreach (var enemy in enemies)
            {
                var distance = (enemy.Position - playerPosition).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    nearest = enemy;
                    bestDistance = distance;
                }
            }

            return nearest;
        }

        private void UpdateProjectiles()
        {
            for (var i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                projectile.Life -= Time.deltaTime;
                if (projectile.Target != null)
                {
                    projectile.Direction = Vector2.Lerp(projectile.Direction, (projectile.Target.Position - projectile.Position).normalized, Time.deltaTime * 5f).normalized;
                }

                projectile.Position += projectile.Direction * GatherAndShotBalance.ProjectileSpeed * Time.deltaTime;
                projectile.Renderer.transform.position = projectile.Position;
                projectile.Renderer.transform.Rotate(0f, 0f, 540f * Time.deltaTime);

                var hit = projectile.Target != null
                    && Vector2.Distance(projectile.Position, projectile.Target.Position) <= GatherAndShotBalance.ProjectileHitRadius;
                if (hit)
                {
                    DamageEnemy(projectile.Target);
                }

                if (hit || projectile.Life <= 0f)
                {
                    SpawnBurst(projectile.Position, 0.66f, 6);
                    Destroy(projectile.Renderer.gameObject);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void DamageEnemy(Enemy enemy)
        {
            enemy.Health--;
            SpawnBurst(enemy.Position, enemy.Kind == EnemyKind.Heavy ? 1.15f : 0.84f, enemy.Kind == EnemyKind.Heavy ? 10 : 7);
            if (enemy.Health > 0)
            {
                enemy.Position += (enemy.Position - playerPosition).normalized * 0.34f;
                return;
            }

            score++;
            Destroy(enemy.Renderer.gameObject);
            enemies.Remove(enemy);
        }

        private void SpawnBurst(Vector2 position, float scale, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var renderer = new GameObject("Snow Puff", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.sprite = puffSprite;
                renderer.sortingOrder = 30;
                renderer.transform.position = position;
                renderer.transform.localScale = Vector3.one * RandomRange(0.18f, 0.36f) * scale;
                bursts.Add(new Burst
                {
                    Renderer = renderer,
                    Position = position,
                    Velocity = new Vector2(RandomRange(-1f, 1f), RandomRange(-1f, 1f)).normalized * RandomRange(0.7f, 2.4f) * scale,
                    Life = RandomRange(0.18f, 0.36f)
                });
            }
        }

        private void UpdateBursts()
        {
            for (var i = bursts.Count - 1; i >= 0; i--)
            {
                var burst = bursts[i];
                burst.Life -= Time.deltaTime;
                burst.Position += burst.Velocity * Time.deltaTime;
                burst.Renderer.transform.position = burst.Position;
                var color = burst.Renderer.color;
                color.a = Mathf.Clamp01(burst.Life / 0.32f);
                burst.Renderer.color = color;
                if (burst.Life <= 0f)
                {
                    Destroy(burst.Renderer.gameObject);
                    bursts.RemoveAt(i);
                }
            }
        }

        private void EndRun()
        {
            state = GatherAndShotGameState.GameOver;
            bestScore = Mathf.Max(bestScore, score);
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
            resultScoreText.text = $"Score {score}\nBest {bestScore}";
            resultPanel.SetActive(true);
            UpdateHud();
            EndJoystick();
        }

        private void UpdateHud()
        {
            scoreText.text = score.ToString();
            bestText.text = $"BEST {bestScore}";
            ammoText.text = $"SNOW x{ammo}";
            warmthFill.rectTransform.sizeDelta = new Vector2(WarmthBarWidth * Mathf.Clamp01(warmth / GatherAndShotBalance.MaxWarmth), 0f);
        }

        private void ClearActors()
        {
            foreach (var enemy in enemies)
            {
                if (enemy.Renderer != null)
                {
                    Destroy(enemy.Renderer.gameObject);
                }
            }

            foreach (var pickup in pickups)
            {
                if (pickup.Renderer != null)
                {
                    Destroy(pickup.Renderer.gameObject);
                }
            }

            foreach (var projectile in projectiles)
            {
                if (projectile.Renderer != null)
                {
                    Destroy(projectile.Renderer.gameObject);
                }
            }

            foreach (var burst in bursts)
            {
                if (burst.Renderer != null)
                {
                    Destroy(burst.Renderer.gameObject);
                }
            }

            enemies.Clear();
            pickups.Clear();
            projectiles.Clear();
            bursts.Clear();
        }

        private void BuildBoundaryMarkers()
        {
            var fill = new Color32(200, 229, 235, 96);
            var ink = new Color32(122, 170, 178, 84);
            var halfWidth = PlayHalfWidth;
            var horizontalPixels = Mathf.CeilToInt((halfWidth * 2f + 0.4f) * 96f);
            const float edge = 0.1f;

            CreateWorldBand("North Snowbank", new Vector2(0f, WorldHalfHeight - edge), horizontalPixels, 26, fill, 96f, 0f, -18);
            CreateWorldBand("South Snowbank", new Vector2(0f, -WorldHalfHeight + edge), horizontalPixels, 26, fill, 96f, 0f, -18);
            CreateWorldBand("West Snowbank", new Vector2(-halfWidth + edge, 0f), 26, 1365, fill, 96f, 0f, -18);
            CreateWorldBand("East Snowbank", new Vector2(halfWidth - edge, 0f), 26, 1365, fill, 96f, 0f, -18);

            CreateWorldBand("North Edge Ink", new Vector2(0f, WorldHalfHeight - 0.24f), horizontalPixels - 36, 4, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
            CreateWorldBand("South Edge Ink", new Vector2(0f, -WorldHalfHeight + 0.24f), horizontalPixels - 36, 4, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
            CreateWorldBand("West Edge Ink", new Vector2(-halfWidth + 0.24f, 0f), 4, 1300, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
            CreateWorldBand("East Edge Ink", new Vector2(halfWidth - 0.24f, 0f), 4, 1300, ink, 96f, RandomRange(-1.4f, 1.4f), -14);
        }

        private void CreateWorldBand(string name, Vector2 position, int width, int height, Color color, float pixelsPerUnit, float rotation, int sortingOrder)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(name, width, height, color, pixelsPerUnit);
            renderer.transform.position = new Vector3(position.x, position.y, 7f);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            renderer.sortingOrder = sortingOrder;
        }

        private Sprite LoadSprite(string name, Color fallbackColor, int size)
        {
            var texture = Resources.Load<Texture2D>($"GatherAndShot/{name}");
            if (texture == null)
            {
                return CreateSolidSprite(name, size, size, fallbackColor, 96f);
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        }

        private static Sprite CreateSolidSprite(string name, int width, int height, Color color, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.name = name;
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            panel.anchorMin = anchor;
            panel.anchorMax = anchor;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            AddSketchOutline(panel, 3.25f, 3.2f);
            return panel;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions)
        {
            return CreateText(parent, name, value, size, alignment, new Vector2(0.5f, 0.5f), position, dimensions);
        }

        private Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = anchor;
            text.rectTransform.anchorMax = anchor;
            text.rectTransform.pivot = anchor;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = dimensions;
            text.font = GetDefaultFont();
            text.text = value;
            text.fontSize = size;
            text.color = SketchPalette.Ink;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, size - 12);
            text.resizeTextMaxSize = size;
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 dimensions)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            button.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().anchoredPosition = position;
            button.GetComponent<RectTransform>().sizeDelta = dimensions;
            button.GetComponent<Image>().color = SketchPalette.WarmHighlight;
            button.colors = SketchUiFactory.ButtonColors();
            AddSketchOutline(button.GetComponent<RectTransform>(), 3.25f, 3.2f);
            CreateText(button.transform, $"{name} Label", label, 38, TextAnchor.MiddleCenter, Vector2.zero, dimensions);
            return button;
        }

        private void PositionJoystickGuideAtPlayer()
        {
            if (joystickRoot == null || joystickBase == null || worldCamera == null)
            {
                return;
            }

            var playerScreen = RectTransformUtility.WorldToScreenPoint(worldCamera, playerPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickRoot, playerScreen, null, out var localPoint))
            {
                joystickBase.anchoredPosition = localPoint;
            }
        }

        private float PlayHalfWidth => Mathf.Min(WorldHalfWidth, worldCamera.orthographicSize * worldCamera.aspect);

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void AddSketchOutline(RectTransform target, float thickness, float jitter)
        {
            var outline = new GameObject("Sketch Outline", typeof(RectTransform), typeof(SketchOutlineGraphic));
            outline.transform.SetParent(target, false);

            var rect = outline.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var graphic = outline.GetComponent<SketchOutlineGraphic>();
            graphic.color = SketchPalette.Ink;
            graphic.raycastTarget = false;
            graphic.Thickness = thickness;
            graphic.Jitter = jitter;
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private sealed class Enemy
        {
            public EnemyKind Kind;
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public int Health;
            public float Seed;
        }

        private sealed class Pickup
        {
            public bool Drift;
            public SpriteRenderer Renderer;
            public Vector2 Position;
        }

        private sealed class Projectile
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Direction;
            public Enemy Target;
            public float Life;
        }

        private sealed class Burst
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
        }
    }
}
