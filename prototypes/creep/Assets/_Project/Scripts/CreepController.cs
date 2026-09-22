using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.Creep
{
    public sealed class CreepController : MonoBehaviour
    {
        private const string BestDistanceKey = "mannlab.creep.best_distance";
        private const int GridWidth = 31;
        private const int VisibleColumns = 9;
        private const int RenderColumns = VisibleColumns + 4;
        private const int ChunkHeight = 14;
        private const int VisibleRows = 22;
        private const float PlayerRadius = 0.31f;
        private const float EnemyRadius = 0.33f;
        private const float StartY = 4f;
        private const float MaxPlayerSpeed = 4.65f;
        private const float SilentSpeed = 1.05f;
        private const float FogStartingY = -5.2f;
        private const float FogKillPadding = 0.2f;
        private const float TileGapPixels = 1.5f;

        private readonly Dictionary<Vector2Int, bool> openCells = new Dictionary<Vector2Int, bool>();
        private readonly HashSet<Vector2Int> noisyCells = new HashSet<Vector2Int>();
        private readonly List<TileView> tileViews = new List<TileView>();
        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<Image> fogHands = new List<Image>();
        private readonly System.Random random = new System.Random();

        private Canvas canvas;
        private RectTransform playfield;
        private RectTransform worldRoot;
        private RectTransform playerRoot;
        private RectTransform fogRoot;
        private RectTransform enemyRoot;
        private RectTransform tileRoot;
        private RectTransform uiRoot;
        private RectTransform noiseFill;
        private RectTransform playerNoiseRing;
        private RectTransform deathSnareRoot;
        private RectTransform resultPanel;
        private RectTransform joystickKnob;
        private RectTransform joystickBase;
        private Image playerBody;
        private Image playerCore;
        private Image playerLeftFoot;
        private Image playerRightFoot;
        private Image playerBag;
        private Image playerPhone;
        private Image deathOverlay;
        private Image fogShade;
        private FogGraphic fogGraphic;
        private Text distanceText;
        private Text bestText;
        private Text stateText;
        private Text announcementText;
        private Text resultTitleText;
        private Text resultScoreText;
        private Text restartText;
        private JoystickPad joystick;
        private Sprite squareSprite;
        private Font uiFont;
        private AudioSource audioSource;
        private AudioClip fogClip;
        private AudioClip caughtClip;
        private AudioClip stepClip;
        private AudioClip rattleClip;

        private Vector2 playerPosition;
        private Vector2 playerDirection = Vector2.up;
        private float playerNoise;
        private float locomotionCycle;
        private int lastStepIndex;
        private float cameraX;
        private float cameraY;
        private float distance;
        private float bestDistance;
        private float fogY;
        private float runSeconds;
        private float announcementPulse;
        private float generatedToY;
        private float nextEnemySpawnY;
        private float deathSeconds;
        private int pathCursorX;
        private bool runEnded;
        private bool smokeMode;
        private EndReason endReason;

        private enum EnemyState
        {
            Idle,
            Suspicious,
            Chase
        }

        private enum EnemyKind
        {
            Staff,
            Customer
        }

        private enum EndReason
        {
            Fog,
            Enemy
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            squareSprite = CreateSquareSprite();
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Apple SD Gothic Neo", "Arial", "Helvetica" },
                18);
            bestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);
            smokeMode = Application.absoluteURL.Contains("smoke=1", StringComparison.OrdinalIgnoreCase);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            fogClip = CreateToneClip("Fog Engulf", 72f, 0.42f, 0.3f);
            caughtClip = CreateToneClip("Caught", 144f, 0.18f, 0.38f);
            stepClip = CreateToneClip("Step", 420f, 0.035f, 0.09f);
            rattleClip = CreateToneClip("Dropped Goods Rattle", 760f, 0.055f, 0.12f);

            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (runEnded)
            {
                AnimateDeath(Time.deltaTime);

                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
                {
                    StartRun();
                }

                return;
            }

            if (smokeMode && Input.GetKeyDown(KeyCode.G))
            {
                EndRun(EndReason.Fog);
                return;
            }

            var deltaTime = Mathf.Min(Time.deltaTime, 0.033f);
            runSeconds += deltaTime;
            UpdatePlayer(deltaTime);
            UpdateFog(deltaTime);
            UpdateEnemies(deltaTime);
            EnsureGeneratedAhead();
            UpdateCamera(deltaTime);
            UpdateViews();
            CheckLoss();
        }

        private void StartRun()
        {
            openCells.Clear();
            noisyCells.Clear();
            enemies.Clear();
            foreach (Transform child in enemyRoot)
            {
                Destroy(child.gameObject);
            }

            runEnded = false;
            deathSeconds = 0f;
            runSeconds = 0f;
            announcementPulse = 0f;
            endReason = EndReason.Fog;
            playerPosition = new Vector2(GridWidth * 0.5f, StartY);
            playerDirection = Vector2.up;
            playerNoise = 0f;
            locomotionCycle = 0f;
            lastStepIndex = -1;
            distance = 0f;
            fogY = FogStartingY;
            cameraX = GridWidth * 0.5f;
            cameraY = 6f;
            generatedToY = -1f;
            pathCursorX = GridWidth / 2;
            nextEnemySpawnY = 13f;

            playerRoot.gameObject.SetActive(true);
            playerRoot.localScale = Vector3.one;
            playerBody.color = new Color32(242, 202, 89, 255);
            playerCore.color = new Color32(255, 249, 191, 255);
            playerLeftFoot.color = new Color32(222, 169, 72, 255);
            playerRightFoot.color = new Color32(222, 169, 72, 255);
            playerBag.color = new Color32(156, 95, 62, 255);
            playerPhone.color = new Color32(176, 229, 215, 255);
            deathOverlay.color = Color.clear;
            deathSnareRoot.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            stateText.text = "AFTER HOURS";

            GenerateInitialMap();
            EnsureGeneratedAhead();
            UpdateCamera(10f);
            UpdateViews();
        }

        private void GenerateInitialMap()
        {
            for (var y = -6; y < 0; y++)
            {
                for (var x = 0; x < GridWidth; x++)
                {
                    SetOpen(x, y, x >= pathCursorX - 3 && x <= pathCursorX + 3);
                }
            }

            var startCellY = 0;
            pathCursorX = GridWidth / 2;
            SetOpen(pathCursorX, startCellY, true);
            for (var y = 0; y <= 7; y++)
            {
                for (var x = pathCursorX - 3; x <= pathCursorX + 3; x++)
                {
                    SetOpen(x, y, true);
                }
            }

            generatedToY = -1f;
        }

        private void EnsureGeneratedAhead()
        {
            var neededY = Mathf.CeilToInt(cameraY + VisibleRows);
            while (generatedToY < neededY)
            {
                GenerateChunk(Mathf.FloorToInt(generatedToY) + 1);
            }

            while (nextEnemySpawnY < cameraY + VisibleRows * 0.9f)
            {
                TrySpawnEnemy(nextEnemySpawnY);
                nextEnemySpawnY += Mathf.Lerp(6.8f, 3.2f, Difficulty01());
            }
        }

        private void GenerateChunk(int yStart)
        {
            var chunkEnd = yStart + ChunkHeight;
            var centerLane = Mathf.Clamp(pathCursorX + random.Next(-2, 3), 4, GridWidth - 5);
            var lanes = new[]
            {
                2 + random.Next(0, 2),
                Mathf.Clamp(centerLane - 8 + random.Next(-1, 2), 1, GridWidth - 2),
                Mathf.Clamp(centerLane - 4 + random.Next(-1, 2), 1, GridWidth - 2),
                centerLane,
                Mathf.Clamp(centerLane + 4 + random.Next(-1, 2), 1, GridWidth - 2),
                Mathf.Clamp(centerLane + 8 + random.Next(-1, 2), 1, GridWidth - 2),
                GridWidth - 3 - random.Next(0, 2)
            };

            for (var y = yStart; y < chunkEnd; y++)
            {
                foreach (var lane in lanes)
                {
                    SetOpen(lane, y, true);
                }

                if (y % 5 == 2)
                {
                    SetOpen(centerLane - 1, y, true);
                    SetOpen(centerLane + 1, y, true);
                }

                if (random.NextDouble() < 0.3)
                {
                    var lane = lanes[random.Next(lanes.Length)];
                    SetOpen(Mathf.Clamp(lane - 1, 1, GridWidth - 2), y, true);
                    SetOpen(Mathf.Clamp(lane + 1, 1, GridWidth - 2), y, true);
                }
            }

            CarveHorizontal(yStart, pathCursorX, centerLane);
            CarveHorizontal(yStart + 1, lanes[0], lanes[lanes.Length - 1]);
            CarveHorizontal(yStart + 4 + random.Next(0, 2), lanes[1], lanes[3]);
            CarveHorizontal(yStart + 7 + random.Next(0, 2), lanes[3], lanes[5]);
            CarveHorizontal(yStart + 10 + random.Next(0, 2), lanes[2], lanes[6]);
            CarveHorizontal(chunkEnd - 2, lanes[0], lanes[lanes.Length - 1]);

            if (random.NextDouble() < 0.7)
            {
                CarveRoom(centerLane, yStart + 3 + random.Next(0, 5));
            }

            ScatterDroppedGoods(yStart, chunkEnd);
            pathCursorX = centerLane;
            generatedToY = chunkEnd - 1;
        }

        private void ScatterDroppedGoods(int yStart, int yEnd)
        {
            for (var y = yStart + 2; y < yEnd - 1; y++)
            {
                for (var x = 1; x < GridWidth - 1; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!IsOpenCell(cell) || y < StartY + 6f || random.NextDouble() > 0.09)
                    {
                        continue;
                    }

                    noisyCells.Add(cell);
                }
            }
        }

        private void CarveHorizontal(int y, int fromX, int toX)
        {
            var minX = Mathf.Clamp(Mathf.Min(fromX, toX), 1, GridWidth - 2);
            var maxX = Mathf.Clamp(Mathf.Max(fromX, toX), 1, GridWidth - 2);
            for (var x = minX; x <= maxX; x++)
            {
                SetOpen(x, y, true);
            }
        }

        private void CarveRoom(int centerX, int centerY)
        {
            for (var y = centerY - 1; y <= centerY + 1; y++)
            {
                for (var x = centerX - 1; x <= centerX + 1; x++)
                {
                    SetOpen(x, y, true);
                }
            }
        }

        private void SetOpen(int x, int y, bool open)
        {
            if (x < 0 || x >= GridWidth)
            {
                return;
            }

            openCells[new Vector2Int(x, y)] = open;
        }

        private void TrySpawnEnemy(float aroundY)
        {
            var targetCount = 2 + Mathf.FloorToInt(distance / 85f);
            targetCount = Mathf.Clamp(targetCount, 2, 11);

            if (enemies.Count >= targetCount)
            {
                return;
            }

            var candidates = new List<Vector2Int>();
            var minY = Mathf.FloorToInt(aroundY);
            for (var y = minY; y < minY + 8; y++)
            {
                for (var x = 1; x < GridWidth - 1; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsOpenCell(cell) && Vector2.Distance(CellCenter(cell), playerPosition) > 7f)
                    {
                        candidates.Add(cell);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var spawnCell = candidates[random.Next(candidates.Count)];
            var enemy = new Enemy
            {
                Position = CellCenter(spawnCell),
                Direction = random.NextDouble() < 0.5 ? Vector2.left : Vector2.right,
                PatrolDirection = random.NextDouble() < 0.5 ? Vector2.left : Vector2.right,
                Kind = random.NextDouble() < 0.68 ? EnemyKind.Staff : EnemyKind.Customer,
                Roamer = random.NextDouble() < Mathf.Lerp(0.16f, 0.34f, Difficulty01()),
                State = EnemyState.Idle,
                Suspicion = 0f,
                WanderCooldown = UnityEngine.Random.Range(1.2f, 3.8f)
            };
            enemy.Root = CreateEnemyView(enemyRoot);
            enemies.Add(enemy);
        }

        private void UpdatePlayer(float deltaTime)
        {
            var input = ReadInput();
            var inputMagnitude = Mathf.Clamp01(input.magnitude);
            var moving = inputMagnitude > 0.04f;

            if (moving)
            {
                var moveDirection = input.normalized;
                playerDirection = Vector2.Lerp(playerDirection, moveDirection, 12f * deltaTime).normalized;
                var speed = Mathf.Lerp(SilentSpeed, MaxPlayerSpeed, Mathf.Pow(inputMagnitude, 1.35f));
                TryMove(ref playerPosition, moveDirection * speed * deltaTime, PlayerRadius);
                var baseNoise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 1f, inputMagnitude));
                var onDroppedGoods = IsOnDroppedGoods(playerPosition);
                playerNoise = Mathf.Clamp01(baseNoise + (onDroppedGoods ? Mathf.Lerp(0.18f, 0.42f, baseNoise) : 0f));
                locomotionCycle += deltaTime * Mathf.Lerp(4.4f, 13.5f, inputMagnitude);

                var stepIndex = Mathf.FloorToInt(locomotionCycle * 2f);
                if (stepIndex != lastStepIndex && playerNoise > 0.16f)
                {
                    lastStepIndex = stepIndex;
                    audioSource.PlayOneShot(onDroppedGoods ? rattleClip : stepClip, Mathf.Lerp(0.15f, 0.65f, playerNoise));
                }
            }
            else
            {
                playerNoise = Mathf.MoveTowards(playerNoise, 0f, 7f * deltaTime);
                locomotionCycle = Mathf.MoveTowards(locomotionCycle, Mathf.Round(locomotionCycle), 5f * deltaTime);
            }

            distance = Mathf.Max(distance, (playerPosition.y - StartY) * 6f);
        }

        private bool IsOnDroppedGoods(Vector2 position)
        {
            return noisyCells.Contains(WorldToCell(position))
                || noisyCells.Contains(WorldToCell(position + playerDirection.normalized * PlayerRadius * 0.75f));
        }

        private Vector2 ReadInput()
        {
            var axis = Vector2.zero;

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                axis.x -= 1f;
            }

            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                axis.x += 1f;
            }

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                axis.y += 1f;
            }

            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                axis.y -= 1f;
            }

            if (axis.sqrMagnitude > 1f)
            {
                axis.Normalize();
            }

            if (joystick != null && joystick.Value.sqrMagnitude > axis.sqrMagnitude)
            {
                axis = joystick.Value;
            }

            return axis;
        }

        private void UpdateFog(float deltaTime)
        {
            var difficulty = Difficulty01();
            var targetGap = Mathf.Lerp(9.8f, 5.8f, difficulty);
            var targetY = playerPosition.y - targetGap;
            var driftSpeed = Mathf.Lerp(1.12f, 2.35f, difficulty);
            fogY = Mathf.MoveTowards(fogY, targetY, driftSpeed * deltaTime);
            fogY += Mathf.Lerp(0.18f, 0.48f, difficulty) * deltaTime;
        }

        private void UpdateEnemies(float deltaTime)
        {
            var difficulty = Difficulty01();
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];

                if (enemy.Position.y < fogY - 3.5f)
                {
                    Destroy(enemy.Root.Rect.gameObject);
                    enemies.RemoveAt(i);
                    continue;
                }

                var toPlayer = playerPosition - enemy.Position;
                var distanceToPlayer = toPlayer.magnitude;
                var seesPlayer = CanSeePlayer(enemy, toPlayer, distanceToPlayer, difficulty);
                var hearsPlayer = CanHearPlayer(enemy, toPlayer, distanceToPlayer, difficulty);

                if (seesPlayer || hearsPlayer)
                {
                    enemy.LastKnownPlayerPosition = playerPosition;
                    var gain = seesPlayer ? 1.9f : 1.1f;
                    if (enemy.State == EnemyState.Suspicious)
                    {
                        gain *= 1.25f;
                    }

                    enemy.Suspicion += gain * deltaTime;
                }
                else
                {
                    var decay = enemy.State == EnemyState.Chase ? 0.28f : 0.52f;
                    enemy.Suspicion -= decay * deltaTime;
                }

                enemy.Suspicion = Mathf.Clamp01(enemy.Suspicion);

                if (enemy.Suspicion >= 0.82f)
                {
                    enemy.State = EnemyState.Chase;
                }
                else if (enemy.Suspicion >= 0.18f)
                {
                    enemy.State = EnemyState.Suspicious;
                }
                else
                {
                    enemy.State = EnemyState.Idle;
                }

                MoveEnemy(enemy, deltaTime, difficulty);
                UpdateEnemyView(enemy, difficulty);
            }
        }

        private bool CanSeePlayer(Enemy enemy, Vector2 toPlayer, float distanceToPlayer, float difficulty)
        {
            var visionRange = Mathf.Lerp(3.4f, 6.1f, difficulty);
            if (distanceToPlayer > visionRange || distanceToPlayer < 0.01f)
            {
                return false;
            }

            var visionAngle = Mathf.Lerp(54f, 82f, difficulty);
            var angle = Vector2.Angle(enemy.Direction, toPlayer);
            return angle <= visionAngle * 0.5f && HasLineOfSight(enemy.Position, playerPosition);
        }

        private bool CanHearPlayer(Enemy enemy, Vector2 toPlayer, float distanceToPlayer, float difficulty)
        {
            if (playerNoise <= 0.02f)
            {
                return false;
            }

            var hearingRange = Mathf.Lerp(2.2f, 5.7f, difficulty);
            var facing = Vector2.Dot(enemy.Direction.normalized, toPlayer.normalized);
            var facingModifier = facing > 0.15f ? 1.16f : 0.58f;
            var noisyRange = hearingRange * playerNoise * facingModifier;
            return distanceToPlayer <= noisyRange && HasLineOfSight(enemy.Position, playerPosition);
        }

        private void MoveEnemy(Enemy enemy, float deltaTime, float difficulty)
        {
            Vector2 desiredDirection;
            var speed = 0f;

            if (enemy.State == EnemyState.Chase)
            {
                desiredDirection = (playerPosition - enemy.Position).normalized;
                speed = Mathf.Lerp(4.7f, 6.05f, difficulty);
            }
            else if (enemy.State == EnemyState.Suspicious)
            {
                desiredDirection = (enemy.LastKnownPlayerPosition - enemy.Position).normalized;
                speed = Mathf.Lerp(1.45f, 2.25f, difficulty);
            }
            else
            {
                enemy.WanderCooldown -= deltaTime;
                if (enemy.WanderCooldown <= 0f || !CanMove(enemy.Position, enemy.PatrolDirection * 0.32f, EnemyRadius))
                {
                    enemy.PatrolDirection = PickPatrolDirection(enemy);
                    enemy.WanderCooldown = enemy.Roamer
                        ? UnityEngine.Random.Range(1.1f, 2.6f)
                        : UnityEngine.Random.Range(1.8f, 4.4f);
                }

                desiredDirection = enemy.PatrolDirection;
                speed = enemy.Roamer ? Mathf.Lerp(0.22f, 0.58f, difficulty) : 0f;
            }

            if (desiredDirection.sqrMagnitude < 0.01f)
            {
                desiredDirection = enemy.Direction;
            }

            var turnSpeed = enemy.State == EnemyState.Idle ? 2.4f : enemy.State == EnemyState.Suspicious ? 6.2f : 12f;
            enemy.Direction = Vector2.Lerp(enemy.Direction, desiredDirection.normalized, turnSpeed * deltaTime).normalized;

            if (speed > 0.01f)
            {
                TryMove(ref enemy.Position, desiredDirection.normalized * speed * deltaTime, EnemyRadius);
            }
        }

        private Vector2 PickPatrolDirection(Enemy enemy)
        {
            var options = new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var candidate = options[random.Next(options.Length)];
                if (CanMove(enemy.Position, candidate * 0.42f, EnemyRadius))
                {
                    return candidate;
                }
            }

            return -enemy.PatrolDirection;
        }

        private void CheckLoss()
        {
            if (playerPosition.y - PlayerRadius <= fogY + FogKillPadding)
            {
                EndRun(EndReason.Fog);
                return;
            }

            foreach (var enemy in enemies)
            {
                if (Vector2.Distance(enemy.Position, playerPosition) <= PlayerRadius + EnemyRadius)
                {
                    EndRun(EndReason.Enemy);
                    return;
                }
            }
        }

        private void EndRun(EndReason reason)
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            endReason = reason;
            deathSeconds = 0f;

            if (distance > bestDistance)
            {
                bestDistance = distance;
                PlayerPrefs.SetFloat(BestDistanceKey, bestDistance);
                PlayerPrefs.Save();
            }

            audioSource.PlayOneShot(reason == EndReason.Fog ? fogClip : caughtClip);
            deathSnareRoot.gameObject.SetActive(reason == EndReason.Fog);

            resultTitleText.text = reason == EndReason.Fog ? "STORE CLOSED" : "FOUND";
            resultScoreText.text = $"{Mathf.FloorToInt(distance)} m\nBest {Mathf.FloorToInt(bestDistance)} m";
            resultPanel.gameObject.SetActive(false);
        }

        private void AnimateDeath(float deltaTime)
        {
            deathSeconds += deltaTime;
            var t = Mathf.Clamp01(deathSeconds / 0.95f);
            playerBody.color = Color.Lerp(
                new Color32(242, 202, 89, 255),
                endReason == EndReason.Fog ? new Color32(91, 116, 122, 255) : new Color32(190, 56, 72, 255),
                t);
            playerCore.color = Color.Lerp(new Color32(255, 249, 191, 255), Color.clear, t);
            playerLeftFoot.color = Color.Lerp(new Color32(222, 169, 72, 255), Color.clear, t);
            playerRightFoot.color = Color.Lerp(new Color32(222, 169, 72, 255), Color.clear, t);
            playerBag.color = Color.Lerp(new Color32(156, 95, 62, 255), Color.clear, t);
            playerPhone.color = Color.Lerp(new Color32(176, 229, 215, 255), Color.clear, t);
            playerRoot.localScale = Vector3.one * Mathf.Lerp(1f, endReason == EndReason.Fog ? 0.58f : 1.18f, t);

            if (endReason == EndReason.Fog)
            {
                var down = WorldToLocal(playerPosition + Vector2.down * (t * 0.8f));
                playerRoot.anchoredPosition = down;

                for (var i = 0; i < fogHands.Count; i++)
                {
                    var hand = fogHands[i];
                    var angle = i / (float)fogHands.Count * Mathf.PI * 2f + t * 2.2f;
                    var radius = Mathf.Lerp(66f, 15f, t);
                    hand.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    hand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f);
                    hand.color = new Color(0.46f, 0.84f, 0.82f, Mathf.Lerp(0.35f, 0.92f, t));
                }

                deathOverlay.color = new Color(0.02f, 0.09f, 0.11f, Mathf.Lerp(0f, 0.72f, t));
            }
            else
            {
                var pulse = Mathf.Sin(t * Mathf.PI);
                deathOverlay.color = new Color(0.42f, 0.02f, 0.04f, Mathf.Lerp(0f, 0.68f, t) + pulse * 0.12f);
            }

            if (deathSeconds >= 0.92f)
            {
                resultPanel.gameObject.SetActive(true);
            }
        }

        private void UpdateCamera(float deltaTime)
        {
            var halfVisibleColumns = VisibleColumns * 0.5f;
            var desiredX = Mathf.Clamp(playerPosition.x, halfVisibleColumns, GridWidth - halfVisibleColumns);
            var desiredY = Mathf.Max(6f, playerPosition.y + 3.7f, fogY + 9.2f);
            cameraX = Mathf.Lerp(cameraX, desiredX, Mathf.Clamp01(deltaTime * 6f));
            cameraY = Mathf.Lerp(cameraY, desiredY, Mathf.Clamp01(deltaTime * 5.2f));
        }

        private void UpdateViews()
        {
            UpdateTiles();
            var playerScreen = WorldToLocal(playerPosition);
            playerRoot.anchoredPosition = playerScreen;
            playerRoot.localRotation = Quaternion.Euler(0f, 0f, VectorToAngle(playerDirection) - 90f);
            UpdatePlayerLocomotion();

            playerNoiseRing.anchoredPosition = playerScreen;
            playerNoiseRing.gameObject.SetActive(false);

            fogRoot.anchoredPosition = new Vector2(0f, WorldToLocal(new Vector2(0f, fogY)).y);
            var fogHeight = Mathf.Max(220f, playfield.rect.height * 0.52f);
            fogRoot.sizeDelta = new Vector2(playfield.rect.width, fogHeight);
            fogGraphic.WavePhase = Time.time * 0.85f;
            fogGraphic.SetVerticesDirty();
            fogShade.color = new Color(0.05f, 0.13f, 0.16f, Mathf.Clamp01(Mathf.InverseLerp(6.5f, 1.8f, playerPosition.y - fogY)) * 0.38f);

            distanceText.text = $"{Mathf.FloorToInt(distance)} m";
            bestText.text = $"BEST {Mathf.FloorToInt(bestDistance)}";
            noiseFill.anchorMax = new Vector2(Mathf.Clamp01(playerNoise), 1f);
            UpdateAnnouncement();

            var nearestThreat = FindNearestThreatState();
            stateText.text = nearestThreat;
        }

        private void UpdateAnnouncement()
        {
            announcementPulse += Time.deltaTime;
            var pulse = Mathf.Sin(announcementPulse * 2.8f) * 0.5f + 0.5f;
            announcementText.text = playerNoise > 0.72f
                ? "PA: PLEASE WALK. NO RUNNING."
                : "PA: PLEASE MOVE TO THE BACK OF THE MART.";
            announcementText.color = Color.Lerp(
                new Color32(126, 184, 174, 210),
                new Color32(232, 228, 184, 255),
                playerNoise > 0.72f ? pulse : 0.25f + pulse * 0.25f);
        }

        private void UpdatePlayerLocomotion()
        {
            var stride = Mathf.Sin(locomotionCycle * Mathf.PI * 2f);
            var lift = Mathf.Abs(stride);
            var speedAmount = Mathf.Clamp01(playerNoise);
            var footSpread = Mathf.Lerp(6f, 11f, speedAmount);
            var strideDepth = Mathf.Lerp(3f, 13f, speedAmount);
            var bodyBob = Mathf.Lerp(0f, 4.5f, speedAmount) * lift;
            var bodySquash = Mathf.Lerp(1f, 0.92f, speedAmount * lift);

            playerBody.rectTransform.anchoredPosition = new Vector2(0f, bodyBob * 0.3f);
            playerBody.rectTransform.localScale = new Vector3(1f + speedAmount * lift * 0.08f, bodySquash, 1f);
            playerCore.rectTransform.anchoredPosition = new Vector2(0f, 4f + bodyBob * 0.2f);
            playerBag.rectTransform.anchoredPosition = new Vector2(-18f - speedAmount * lift * 1.4f, -2f + bodyBob * 0.1f);
            playerBag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f + stride * Mathf.Lerp(1.5f, 7f, speedAmount));
            playerPhone.rectTransform.anchoredPosition = new Vector2(17f, 9f + bodyBob * 0.1f);
            playerPhone.color = Color.Lerp(new Color32(138, 196, 186, 190), new Color32(216, 253, 232, 245), speedAmount * 0.45f);

            playerLeftFoot.rectTransform.anchoredPosition = new Vector2(-footSpread, -14f + stride * strideDepth);
            playerRightFoot.rectTransform.anchoredPosition = new Vector2(footSpread, -14f - stride * strideDepth);
            playerLeftFoot.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.18f, Mathf.Max(0f, stride) * speedAmount);
            playerRightFoot.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.18f, Mathf.Max(0f, -stride) * speedAmount);

            var footAlpha = Mathf.Lerp(0.5f, 1f, speedAmount);
            playerLeftFoot.color = new Color(0.87f, 0.66f, 0.28f, footAlpha);
            playerRightFoot.color = new Color(0.87f, 0.66f, 0.28f, footAlpha);
        }

        private string FindNearestThreatState()
        {
            var chasing = false;
            var suspicious = false;
            foreach (var enemy in enemies)
            {
                chasing |= enemy.State == EnemyState.Chase;
                suspicious |= enemy.State == EnemyState.Suspicious;
            }

            if (chasing)
            {
                return "FOUND";
            }

            if (suspicious || playerPosition.y - fogY < 3.8f)
            {
                return "AFTER HOURS";
            }

            return "AFTER HOURS";
        }

        private void UpdateTiles()
        {
            if (tileViews.Count == 0 || playfield.rect.width <= 0.1f)
            {
                return;
            }

            var cellSize = CellPixels();
            var bottomY = Mathf.FloorToInt(cameraY - VisibleRows * 0.5f) - 2;
            var leftX = Mathf.Clamp(Mathf.FloorToInt(cameraX - VisibleColumns * 0.5f) - 2, 0, Mathf.Max(0, GridWidth - RenderColumns));
            var index = 0;

            for (var y = bottomY; y < bottomY + VisibleRows + 4; y++)
            {
                for (var x = leftX; x < leftX + RenderColumns; x++)
                {
                    var tile = tileViews[index++];
                    var cell = new Vector2Int(x, y);
                    var open = IsOpenCell(cell);
                    var tilePosition = WorldToLocal(CellCenter(x, y));
                    tile.Image.rectTransform.sizeDelta = Vector2.one * Mathf.Max(1f, cellSize - TileGapPixels);
                    tile.Image.rectTransform.anchoredPosition = tilePosition;
                    var hash = Mathf.Abs((x * 83492791) ^ (y * 297121507));
                    var floorTone = (byte)(26 + hash % 10);
                    tile.Image.color = open
                        ? new Color32(floorTone, (byte)(floorTone + 2), (byte)(floorTone + 1), 255)
                        : new Color32(14, 18, 17, 255);
                    tile.Image.enabled = true;

                    if (!open)
                    {
                        var fogAmount = Mathf.Clamp01(Mathf.InverseLerp(fogY + 5.5f, fogY - 0.5f, y));
                        tile.Image.color = Color.Lerp(tile.Image.color, new Color32(49, 84, 88, 255), fogAmount * 0.55f);
                    }

                    tile.FloorLine.enabled = open;
                    tile.FloorScuff.enabled = open && hash % 5 == 0;
                    if (open)
                    {
                        tile.FloorLine.rectTransform.sizeDelta = new Vector2(cellSize * 0.88f, Mathf.Max(1f, cellSize * 0.025f));
                        tile.FloorLine.rectTransform.anchoredPosition = tilePosition + new Vector2(0f, -cellSize * 0.44f);
                        tile.FloorLine.rectTransform.localRotation = Quaternion.identity;
                        tile.FloorLine.color = new Color32(77, 85, 78, 46);

                        tile.FloorScuff.rectTransform.sizeDelta = new Vector2(cellSize * 0.38f, Mathf.Max(1f, cellSize * 0.03f));
                        tile.FloorScuff.rectTransform.anchoredPosition = tilePosition + new Vector2(((hash % 7) - 3) * cellSize * 0.055f, (((hash / 7) % 7) - 3) * cellSize * 0.045f);
                        tile.FloorScuff.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (hash % 11 - 5) * 6f);
                        tile.FloorScuff.color = new Color32(121, 114, 94, 42);
                    }

                    tile.ShelfBand.enabled = !open;
                    tile.ShelfTag.enabled = !open && hash % 3 != 0;
                    tile.ShelfStock.enabled = !open;
                    tile.ShelfText.enabled = !open && hash % 23 == 0;
                    if (!open)
                    {
                        tile.ShelfBand.rectTransform.sizeDelta = new Vector2(cellSize * 0.64f, cellSize * 0.11f);
                        tile.ShelfBand.rectTransform.anchoredPosition = tilePosition + new Vector2(0f, (hash % 3 - 1) * cellSize * 0.18f);
                        tile.ShelfBand.rectTransform.localRotation = Quaternion.identity;
                        tile.ShelfBand.color = hash % 4 == 0
                            ? new Color32(126, 184, 174, 170)
                            : hash % 4 == 1
                                ? new Color32(206, 170, 70, 160)
                                : new Color32(87, 103, 96, 150);

                        tile.ShelfTag.rectTransform.sizeDelta = new Vector2(cellSize * 0.23f, cellSize * 0.1f);
                        tile.ShelfTag.rectTransform.anchoredPosition = tilePosition + new Vector2((hash % 2 == 0 ? -1f : 1f) * cellSize * 0.25f, -cellSize * 0.28f);
                        tile.ShelfTag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (hash % 5 - 2) * 5f);
                        tile.ShelfTag.color = new Color32(235, 218, 136, 185);

                        tile.ShelfStock.rectTransform.sizeDelta = new Vector2(cellSize * 0.17f, cellSize * 0.52f);
                        tile.ShelfStock.rectTransform.anchoredPosition = tilePosition + new Vector2(((hash / 3) % 5 - 2) * cellSize * 0.11f, 0f);
                        tile.ShelfStock.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (hash % 3 - 1) * 4f);
                        tile.ShelfStock.color = hash % 5 == 0
                            ? new Color32(152, 65, 67, 145)
                            : hash % 5 == 1
                                ? new Color32(91, 143, 137, 145)
                                : hash % 5 == 2
                                    ? new Color32(192, 157, 72, 145)
                                    : new Color32(116, 128, 117, 130);

                        if (tile.ShelfText.enabled)
                        {
                            tile.ShelfText.rectTransform.sizeDelta = new Vector2(cellSize * 0.82f, cellSize * 0.22f);
                            tile.ShelfText.rectTransform.anchoredPosition = tilePosition + new Vector2(0f, cellSize * 0.21f);
                            tile.ShelfText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (hash % 5 - 2) * 2.5f);
                            tile.ShelfText.fontSize = Mathf.Clamp(Mathf.RoundToInt(cellSize * 0.15f), 7, 14);
                            tile.ShelfText.text = ShelfLabel(hash);
                            tile.ShelfText.color = new Color32(221, 215, 171, 145);
                        }
                    }

                    var productVisible = open && noisyCells.Contains(cell) && y > fogY - 1f;
                    tile.Product.enabled = productVisible;
                    tile.ProductLabel.enabled = productVisible;
                    if (productVisible)
                    {
                        var productHash = Mathf.Abs((x * 73856093) ^ (y * 19349663));
                        var wide = productHash % 2 == 0;
                        tile.Product.rectTransform.sizeDelta = wide
                            ? new Vector2(cellSize * 0.42f, cellSize * 0.16f)
                            : new Vector2(cellSize * 0.22f, cellSize * 0.32f);
                        tile.Product.rectTransform.anchoredPosition = tilePosition + new Vector2(((productHash % 5) - 2) * cellSize * 0.035f, (((productHash / 5) % 5) - 2) * cellSize * 0.035f);
                        tile.Product.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (productHash % 7 - 3) * 13f);
                        tile.Product.color = productHash % 3 == 0
                            ? new Color32(202, 173, 96, 220)
                            : productHash % 3 == 1
                                ? new Color32(137, 177, 171, 210)
                                : new Color32(199, 116, 89, 205);

                        tile.ProductLabel.rectTransform.sizeDelta = new Vector2(tile.Product.rectTransform.sizeDelta.x * 0.65f, Mathf.Max(1f, cellSize * 0.035f));
                        tile.ProductLabel.rectTransform.anchoredPosition = tile.Product.rectTransform.anchoredPosition + new Vector2(0f, -tile.Product.rectTransform.sizeDelta.y * 0.08f);
                        tile.ProductLabel.rectTransform.localRotation = tile.Product.rectTransform.localRotation;
                        tile.ProductLabel.color = new Color32(245, 236, 178, 170);
                    }
                }
            }
        }

        private void UpdateEnemyView(Enemy enemy, float difficulty)
        {
            var body = enemy.Root.Body;
            var vest = enemy.Root.Vest;
            var badge = enemy.Root.Badge;
            var head = enemy.Root.Head;
            var leftArm = enemy.Root.LeftArm;
            var rightArm = enemy.Root.RightArm;
            var eye = enemy.Root.Eye;
            var alert = enemy.Root.AlertText;

            var cellSize = CellPixels();
            var chasePulse = enemy.State == EnemyState.Chase ? Mathf.Sin(Time.time * 24f + enemy.Position.y) : 0f;
            var jitter = enemy.State == EnemyState.Chase
                ? new Vector2(Mathf.Sin(Time.time * 31f + enemy.Position.x), Mathf.Cos(Time.time * 29f + enemy.Position.y)) * cellSize * 0.025f
                : Vector2.zero;
            enemy.Root.Rect.anchoredPosition = WorldToLocal(enemy.Position) + jitter;
            enemy.Root.Rect.localRotation = Quaternion.Euler(0f, 0f, VectorToAngle(enemy.Direction) - 90f + chasePulse * 2.2f);
            enemy.Root.Rect.localScale = Vector3.one * (enemy.State == EnemyState.Chase ? 1.08f + Mathf.Abs(chasePulse) * 0.06f : 1f);

            enemy.Root.Rect.sizeDelta = Vector2.one * (cellSize * 0.78f);
            body.rectTransform.sizeDelta = new Vector2(cellSize * 0.36f, cellSize * 0.54f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -cellSize * 0.04f);
            body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, enemy.Kind == EnemyKind.Staff ? 0f : 4f);

            head.rectTransform.sizeDelta = new Vector2(cellSize * 0.24f, cellSize * 0.2f);
            head.rectTransform.anchoredPosition = new Vector2(0f, cellSize * 0.23f);
            head.rectTransform.localRotation = Quaternion.Euler(0f, 0f, enemy.State == EnemyState.Idle ? Mathf.Sin(Time.time * 1.3f + enemy.Position.x) * 2f : 0f);

            leftArm.rectTransform.sizeDelta = new Vector2(cellSize * 0.075f, cellSize * 0.46f);
            leftArm.rectTransform.anchoredPosition = new Vector2(-cellSize * 0.24f, -cellSize * 0.04f);
            leftArm.rectTransform.localRotation = Quaternion.Euler(0f, 0f, enemy.State == EnemyState.Chase ? -18f - chasePulse * 10f : -7f);
            rightArm.rectTransform.sizeDelta = new Vector2(cellSize * 0.075f, cellSize * 0.46f);
            rightArm.rectTransform.anchoredPosition = new Vector2(cellSize * 0.24f, -cellSize * 0.04f);
            rightArm.rectTransform.localRotation = Quaternion.Euler(0f, 0f, enemy.State == EnemyState.Chase ? 18f + chasePulse * 10f : 7f);

            vest.rectTransform.sizeDelta = enemy.Kind == EnemyKind.Staff
                ? new Vector2(cellSize * 0.27f, cellSize * 0.34f)
                : new Vector2(cellSize * 0.26f, cellSize * 0.2f);
            vest.rectTransform.anchoredPosition = enemy.Kind == EnemyKind.Staff
                ? new Vector2(0f, -cellSize * 0.05f)
                : new Vector2(cellSize * 0.24f, -cellSize * 0.16f);
            vest.rectTransform.localRotation = Quaternion.Euler(0f, 0f, enemy.Kind == EnemyKind.Staff ? 0f : -9f);

            badge.rectTransform.sizeDelta = enemy.Kind == EnemyKind.Staff
                ? new Vector2(cellSize * 0.105f, cellSize * 0.055f)
                : new Vector2(cellSize * 0.18f, cellSize * 0.055f);
            badge.rectTransform.anchoredPosition = enemy.Kind == EnemyKind.Staff
                ? new Vector2(cellSize * 0.07f, cellSize * 0.03f)
                : new Vector2(cellSize * 0.24f, -cellSize * 0.16f);
            badge.rectTransform.localRotation = vest.rectTransform.localRotation;

            eye.rectTransform.anchoredPosition = Vector2.up * (cellSize * 0.25f);
            eye.rectTransform.sizeDelta = new Vector2(cellSize * 0.13f, enemy.State == EnemyState.Chase ? cellSize * 0.34f : cellSize * 0.24f);

            var stateColor = enemy.State == EnemyState.Chase
                ? new Color32(226, 54, 73, 255)
                : enemy.State == EnemyState.Suspicious
                    ? new Color32(235, 182, 75, 255)
                    : enemy.Kind == EnemyKind.Staff
                        ? new Color32(76, 101, 104, 250)
                        : new Color32(102, 94, 95, 235);
            body.color = stateColor;
            head.color = enemy.State == EnemyState.Idle ? new Color32(36, 43, 42, 255) : new Color32(58, 46, 47, 255);
            leftArm.color = Color.Lerp(body.color, new Color32(24, 30, 31, 255), 0.24f);
            rightArm.color = leftArm.color;
            vest.color = enemy.Kind == EnemyKind.Staff
                ? (enemy.State == EnemyState.Chase ? new Color32(61, 182, 178, 245) : new Color32(58, 130, 135, 210))
                : (enemy.State == EnemyState.Chase ? new Color32(176, 59, 76, 225) : new Color32(133, 104, 79, 185));
            badge.color = enemy.State == EnemyState.Chase ? new Color32(255, 238, 167, 245) : new Color32(235, 218, 136, 195);
            eye.color = enemy.State == EnemyState.Idle ? new Color32(120, 220, 209, 185) : new Color32(255, 238, 167, 255);
            alert.text = string.Empty;
            alert.color = eye.color;
            alert.rectTransform.anchoredPosition = new Vector2(0f, cellSize * 0.48f);

            enemy.Root.Cone.gameObject.SetActive(false);
        }

        private Vector2 WorldToLocal(Vector2 worldPosition)
        {
            var cellSize = CellPixels();
            var left = -VisibleColumns * 0.5f * cellSize;
            var leftWorldX = cameraX - VisibleColumns * 0.5f;
            var bottomWorldY = cameraY - VisibleRows * 0.5f;
            var bottom = -VisibleRows * 0.5f * cellSize;
            return new Vector2(left + (worldPosition.x - leftWorldX) * cellSize, bottom + (worldPosition.y - bottomWorldY) * cellSize);
        }

        private float CellPixels()
        {
            if (playfield == null)
            {
                return 56f;
            }

            return Mathf.Min(playfield.rect.width / VisibleColumns, playfield.rect.height / VisibleRows);
        }

        private bool TryMove(ref Vector2 position, Vector2 movement, float radius)
        {
            var moved = false;
            var xMove = new Vector2(movement.x, 0f);
            if (xMove.sqrMagnitude > 0f && CanMove(position, xMove, radius))
            {
                position += xMove;
                moved = true;
            }

            var yMove = new Vector2(0f, movement.y);
            if (yMove.sqrMagnitude > 0f && CanMove(position, yMove, radius))
            {
                position += yMove;
                moved = true;
            }

            return moved;
        }

        private bool CanMove(Vector2 position, Vector2 movement, float radius)
        {
            var target = position + movement;
            var samples = new[]
            {
                target + new Vector2(-radius, -radius),
                target + new Vector2(radius, -radius),
                target + new Vector2(-radius, radius),
                target + new Vector2(radius, radius)
            };

            foreach (var sample in samples)
            {
                if (!IsOpenCell(WorldToCell(sample)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            var steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 3.2f);
            for (var i = 1; i < steps; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)steps);
                if (!IsOpenCell(WorldToCell(p)))
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2Int WorldToCell(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        }

        private Vector2 CellCenter(Vector2Int cell)
        {
            return CellCenter(cell.x, cell.y);
        }

        private Vector2 CellCenter(int x, int y)
        {
            return new Vector2(x + 0.5f, y + 0.5f);
        }

        private bool IsOpenCell(Vector2Int cell)
        {
            return openCells.TryGetValue(cell, out var open) && open;
        }

        private float Difficulty01()
        {
            return Mathf.Clamp01(distance / 520f);
        }

        private static float VectorToAngle(Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        }

        private static string ShelfLabel(int hash)
        {
            switch (hash % 7)
            {
                case 0:
                    return "SALE";
                case 1:
                    return "CEREAL";
                case 2:
                    return "FROZEN";
                case 3:
                    return "DAIRY";
                case 4:
                    return "CLEAN";
                case 5:
                    return "TOYS";
                default:
                    return "BACK";
            }
        }

        private void BuildInterface()
        {
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            playfield = CreateRect("Playfield", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var background = CreateImage("Background", playfield, new Color32(8, 11, 15, 255));
            Stretch(background.rectTransform);

            tileRoot = CreateRect("Tiles", playfield, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            worldRoot = CreateRect("World", playfield, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            enemyRoot = CreateRect("Enemies", worldRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateTileViews();
            CreateFogView();
            CreatePlayerView();
            CreateOverlay();
            CreateJoystick();
            CreateResultPanel();
        }

        private void CreateTileViews()
        {
            var count = RenderColumns * (VisibleRows + 4);
            for (var i = 0; i < count; i++)
            {
                var image = CreateImage("Tile", tileRoot, new Color32(15, 20, 24, 255));
                image.raycastTarget = false;
                var floorLine = CreateImage("Floor Tile Line", tileRoot, new Color32(77, 85, 78, 46));
                floorLine.raycastTarget = false;
                floorLine.enabled = false;
                var floorScuff = CreateImage("Floor Scuff", tileRoot, new Color32(121, 114, 94, 42));
                floorScuff.raycastTarget = false;
                floorScuff.enabled = false;
                var shelfStock = CreateImage("Shelf Stock", tileRoot, new Color32(116, 128, 117, 130));
                shelfStock.raycastTarget = false;
                shelfStock.enabled = false;
                var shelfBand = CreateImage("Shelf Label", tileRoot, new Color32(87, 103, 96, 190));
                shelfBand.raycastTarget = false;
                shelfBand.enabled = false;
                var shelfTag = CreateImage("Shelf Price Tag", tileRoot, new Color32(235, 218, 136, 185));
                shelfTag.raycastTarget = false;
                shelfTag.enabled = false;
                var shelfText = CreateText("Shelf Sign", tileRoot, string.Empty, 10, TextAnchor.MiddleCenter, new Color32(221, 215, 171, 145));
                shelfText.enabled = false;
                var product = CreateImage("Dropped Goods", tileRoot, new Color32(190, 160, 91, 210));
                product.raycastTarget = false;
                product.enabled = false;
                var productLabel = CreateImage("Dropped Goods Label", tileRoot, new Color32(245, 236, 178, 170));
                productLabel.raycastTarget = false;
                productLabel.enabled = false;
                tileViews.Add(new TileView
                {
                    Image = image,
                    FloorLine = floorLine,
                    FloorScuff = floorScuff,
                    ShelfStock = shelfStock,
                    ShelfBand = shelfBand,
                    ShelfTag = shelfTag,
                    ShelfText = shelfText,
                    Product = product,
                    ProductLabel = productLabel
                });
            }
        }

        private void CreateFogView()
        {
            fogShade = CreateImage("Fog Proximity Shade", playfield, Color.clear);
            Stretch(fogShade.rectTransform);
            fogShade.raycastTarget = false;

            fogRoot = CreateRect("Encroaching Fog", playfield, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero);
            fogRoot.pivot = new Vector2(0.5f, 1f);
            fogRoot.sizeDelta = new Vector2(0f, 450f);
            fogGraphic = fogRoot.gameObject.AddComponent<FogGraphic>();
            fogGraphic.raycastTarget = false;
            fogGraphic.color = new Color32(53, 122, 126, 205);

            deathSnareRoot = CreateRect("Fog Snare", worldRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            deathSnareRoot.sizeDelta = Vector2.one * 170f;
            deathSnareRoot.gameObject.SetActive(false);
            for (var i = 0; i < 8; i++)
            {
                var hand = CreateImage("Fog Hand", deathSnareRoot, new Color(0.46f, 0.84f, 0.82f, 0.35f));
                hand.rectTransform.sizeDelta = new Vector2(18f, 92f);
                hand.rectTransform.pivot = new Vector2(0.5f, 0f);
                fogHands.Add(hand);
            }
        }

        private void CreatePlayerView()
        {
            playerNoiseRing = CreateRect("Noise Ring", worldRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var ring = playerNoiseRing.gameObject.AddComponent<RingGraphic>();
            ring.color = new Color(0.95f, 0.8f, 0.34f, 0.22f);
            ring.Thickness = 8f;
            ring.raycastTarget = false;

            playerRoot = CreateRect("Player", worldRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            playerRoot.sizeDelta = new Vector2(52f, 52f);
            playerLeftFoot = CreateImage("Left Foot", playerRoot, new Color32(222, 169, 72, 255));
            playerLeftFoot.rectTransform.sizeDelta = new Vector2(11f, 20f);
            playerLeftFoot.rectTransform.anchoredPosition = new Vector2(-7f, -14f);
            playerRightFoot = CreateImage("Right Foot", playerRoot, new Color32(222, 169, 72, 255));
            playerRightFoot.rectTransform.sizeDelta = new Vector2(11f, 20f);
            playerRightFoot.rectTransform.anchoredPosition = new Vector2(7f, -14f);
            playerBody = CreateImage("Player Body", playerRoot, new Color32(242, 202, 89, 255));
            playerBody.rectTransform.sizeDelta = new Vector2(42f, 48f);
            playerBody.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            playerCore = CreateImage("Player Core", playerRoot, new Color32(255, 249, 191, 255));
            playerCore.rectTransform.sizeDelta = new Vector2(15f, 24f);
            playerCore.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            playerBag = CreateImage("Shopping Bag", playerRoot, new Color32(156, 95, 62, 255));
            playerBag.rectTransform.sizeDelta = new Vector2(12f, 20f);
            playerBag.rectTransform.anchoredPosition = new Vector2(-18f, -2f);
            playerBag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            playerPhone = CreateImage("Phone Glow", playerRoot, new Color32(176, 229, 215, 255));
            playerPhone.rectTransform.sizeDelta = new Vector2(8f, 13f);
            playerPhone.rectTransform.anchoredPosition = new Vector2(17f, 9f);
            playerPhone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
        }

        private void CreateOverlay()
        {
            uiRoot = CreateRect("HUD", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            distanceText = CreateText("Distance", uiRoot, "0 m", 54, TextAnchor.UpperLeft, new Color32(235, 231, 212, 255));
            distanceText.rectTransform.anchorMin = new Vector2(0f, 1f);
            distanceText.rectTransform.anchorMax = new Vector2(0f, 1f);
            distanceText.rectTransform.pivot = new Vector2(0f, 1f);
            distanceText.rectTransform.anchoredPosition = new Vector2(44f, -38f);
            distanceText.rectTransform.sizeDelta = new Vector2(420f, 76f);

            bestText = CreateText("Best", uiRoot, "BEST 0", 25, TextAnchor.UpperLeft, new Color32(129, 181, 177, 255));
            bestText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bestText.rectTransform.anchorMax = new Vector2(0f, 1f);
            bestText.rectTransform.pivot = new Vector2(0f, 1f);
            bestText.rectTransform.anchoredPosition = new Vector2(48f, -105f);
            bestText.rectTransform.sizeDelta = new Vector2(340f, 46f);

            stateText = CreateText("State", uiRoot, "AFTER HOURS", 34, TextAnchor.UpperRight, new Color32(255, 238, 167, 255));
            stateText.rectTransform.anchorMin = new Vector2(1f, 1f);
            stateText.rectTransform.anchorMax = new Vector2(1f, 1f);
            stateText.rectTransform.pivot = new Vector2(1f, 1f);
            stateText.rectTransform.anchoredPosition = new Vector2(-44f, -52f);
            stateText.rectTransform.sizeDelta = new Vector2(360f, 58f);

            var noiseBack = CreateImage("Noise Meter Back", uiRoot, new Color(0.1f, 0.13f, 0.15f, 0.82f));
            noiseBack.rectTransform.anchorMin = new Vector2(1f, 1f);
            noiseBack.rectTransform.anchorMax = new Vector2(1f, 1f);
            noiseBack.rectTransform.pivot = new Vector2(1f, 1f);
            noiseBack.rectTransform.anchoredPosition = new Vector2(-48f, -118f);
            noiseBack.rectTransform.sizeDelta = new Vector2(260f, 16f);

            var fill = CreateImage("Noise Meter Fill", noiseBack.rectTransform, new Color32(236, 188, 83, 255));
            noiseFill = fill.rectTransform;
            noiseFill.anchorMin = Vector2.zero;
            noiseFill.anchorMax = new Vector2(0f, 1f);
            noiseFill.pivot = new Vector2(0f, 0.5f);
            noiseFill.offsetMin = Vector2.zero;
            noiseFill.offsetMax = Vector2.zero;
            noiseBack.gameObject.SetActive(false);

            announcementText = CreateText("Announcement", uiRoot, "PA: PLEASE MOVE TO THE BACK OF THE MART.", 22, TextAnchor.UpperCenter, new Color32(126, 184, 174, 190));
            announcementText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            announcementText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            announcementText.rectTransform.pivot = new Vector2(0.5f, 1f);
            announcementText.rectTransform.anchoredPosition = new Vector2(0f, -154f);
            announcementText.rectTransform.sizeDelta = new Vector2(0f, 42f);

            deathOverlay = CreateImage("Death Overlay", uiRoot, Color.clear);
            Stretch(deathOverlay.rectTransform);
            deathOverlay.raycastTarget = false;
        }

        private void CreateJoystick()
        {
            joystickBase = CreateRect("Virtual Joystick", uiRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            Stretch(joystickBase);
            var touchCatcher = joystickBase.gameObject.AddComponent<Image>();
            touchCatcher.sprite = squareSprite;
            touchCatcher.color = Color.clear;
            touchCatcher.raycastTarget = true;

            var joystickVisual = CreateRect("Joystick Visual", joystickBase, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(206f, 206f));
            joystickVisual.anchoredPosition = new Vector2(188f, 188f);

            var visualRing = joystickVisual.gameObject.AddComponent<RingGraphic>();
            visualRing.color = new Color(0.78f, 0.85f, 0.8f, 0.18f);
            visualRing.Thickness = 14f;
            visualRing.raycastTarget = false;

            joystickKnob = CreateRect("Knob", joystickVisual, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72f, 72f));
            var knobImage = joystickKnob.gameObject.AddComponent<Image>();
            knobImage.sprite = squareSprite;
            knobImage.color = new Color(0.83f, 0.88f, 0.74f, 0.58f);
            knobImage.raycastTarget = false;

            joystick = joystickBase.gameObject.AddComponent<JoystickPad>();
            joystick.Visual = joystickVisual;
            joystick.Knob = joystickKnob;
            joystick.Radius = 82f;
            joystick.IdleAlpha = 0.18f;
            joystick.ActiveAlpha = 0.82f;
        }

        private void CreateResultPanel()
        {
            resultPanel = CreateRect("Result Panel", uiRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 430f));
            var panelImage = resultPanel.gameObject.AddComponent<Image>();
            panelImage.sprite = squareSprite;
            panelImage.color = new Color(0.05f, 0.07f, 0.08f, 0.92f);

            resultTitleText = CreateText("Result Title", resultPanel, "STORE CLOSED", 46, TextAnchor.MiddleCenter, new Color32(239, 231, 205, 255));
            resultTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            resultTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            resultTitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            resultTitleText.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            resultTitleText.rectTransform.sizeDelta = new Vector2(-70f, 82f);

            resultScoreText = CreateText("Result Score", resultPanel, "0 m\nBest 0 m", 34, TextAnchor.MiddleCenter, new Color32(150, 210, 202, 255));
            resultScoreText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            resultScoreText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            resultScoreText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            resultScoreText.rectTransform.anchoredPosition = new Vector2(0f, 28f);
            resultScoreText.rectTransform.sizeDelta = new Vector2(-70f, 122f);

            var restartButton = CreateImage("Restart Button", resultPanel, new Color32(218, 184, 77, 255));
            restartButton.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            restartButton.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            restartButton.rectTransform.pivot = new Vector2(0.5f, 0f);
            restartButton.rectTransform.anchoredPosition = new Vector2(0f, 48f);
            restartButton.rectTransform.sizeDelta = new Vector2(320f, 78f);
            var button = restartButton.gameObject.AddComponent<Button>();
            button.onClick.AddListener(StartRun);

            restartText = CreateText("Restart Text", restartButton.rectTransform, "RESTART", 32, TextAnchor.MiddleCenter, new Color32(20, 24, 26, 255));
            Stretch(restartText.rectTransform);
            resultPanel.gameObject.SetActive(false);
        }

        private EnemyView CreateEnemyView(RectTransform parent)
        {
            var root = CreateRect("After-Hours Customer", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 54f));
            var coneObject = CreateRect("Sight Cone", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero);
            var cone = coneObject.gameObject.AddComponent<VisionConeGraphic>();
            cone.raycastTarget = false;

            var leftArm = CreateImage("Left Arm", root, new Color32(63, 83, 86, 235));
            var rightArm = CreateImage("Right Arm", root, new Color32(63, 83, 86, 235));
            var body = CreateImage("Body", root, new Color32(84, 122, 132, 255));
            var vest = CreateImage("Vest Or Basket", root, new Color32(67, 135, 140, 215));
            var badge = CreateImage("Badge", root, new Color32(235, 218, 136, 215));
            var head = CreateImage("Head", root, new Color32(42, 49, 48, 255));

            var eye = CreateImage("Eye", root, new Color32(120, 220, 209, 220));
            var alert = CreateText("Alert", root, string.Empty, 38, TextAnchor.MiddleCenter, Color.white);
            alert.raycastTarget = false;

            return new EnemyView
            {
                Rect = root,
                Body = body,
                Vest = vest,
                Badge = badge,
                Head = head,
                LeftArm = leftArm,
                RightArm = rightArm,
                Eye = eye,
                AlertText = alert,
                Cone = cone
            };
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = squareSprite;
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        private static AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - t / duration);
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * volume;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private sealed class TileView
        {
            public Image Image;
            public Image FloorLine;
            public Image FloorScuff;
            public Image ShelfStock;
            public Image ShelfBand;
            public Image ShelfTag;
            public Text ShelfText;
            public Image Product;
            public Image ProductLabel;
        }

        private sealed class Enemy
        {
            public Vector2 Position;
            public Vector2 Direction;
            public Vector2 PatrolDirection;
            public Vector2 LastKnownPlayerPosition;
            public EnemyKind Kind;
            public EnemyState State;
            public float Suspicion;
            public float WanderCooldown;
            public bool Roamer;
            public EnemyView Root;
        }

        private sealed class EnemyView
        {
            public RectTransform Rect;
            public Image Body;
            public Image Vest;
            public Image Badge;
            public Image Head;
            public Image LeftArm;
            public Image RightArm;
            public Image Eye;
            public Text AlertText;
            public VisionConeGraphic Cone;
        }
    }

    public sealed class JoystickPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Visual;
        public RectTransform Knob;
        public float Radius = 82f;
        public float IdleAlpha = 0.18f;
        public float ActiveAlpha = 0.82f;
        public Vector2 Value { get; private set; }

        private CanvasGroup visualGroup;
        private Vector2 pointerStart;
        private bool active;

        private void Update()
        {
            EnsureCanvasGroup();
            var targetAlpha = active ? ActiveAlpha : IdleAlpha;
            visualGroup.alpha = Mathf.MoveTowards(visualGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 4.5f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out pointerStart))
            {
                return;
            }

            active = true;
            if (Visual != null)
            {
                Visual.anchoredPosition = pointerStart;
            }

            if (Knob != null)
            {
                Knob.anchoredPosition = Vector2.zero;
            }

            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            active = false;
            Value = Vector2.zero;
            if (Knob != null)
            {
                Knob.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateValue(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local))
            {
                return;
            }

            var offset = Vector2.ClampMagnitude(local - pointerStart, Radius);
            Value = offset / Radius;
            if (Knob != null)
            {
                Knob.anchoredPosition = offset;
            }
        }

        private void EnsureCanvasGroup()
        {
            if (visualGroup != null)
            {
                return;
            }

            var target = Visual != null ? Visual.gameObject : gameObject;
            visualGroup = target.GetComponent<CanvasGroup>();
            if (visualGroup == null)
            {
                visualGroup = target.AddComponent<CanvasGroup>();
            }

            visualGroup.alpha = IdleAlpha;
            visualGroup.blocksRaycasts = false;
            visualGroup.interactable = false;
        }
    }

    public sealed class RingGraphic : MaskableGraphic
    {
        public float Thickness = 6f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var inner = Mathf.Max(0f, radius - Thickness);
            var center = rect.center;
            const int segments = 64;

            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + direction * radius, color, Vector2.zero);
                vh.AddVert(center + direction * inner, color, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var a = i * 2;
                vh.AddTriangle(a, a + 1, a + 2);
                vh.AddTriangle(a + 1, a + 3, a + 2);
            }
        }
    }

    public sealed class VisionConeGraphic : MaskableGraphic
    {
        public float RangePixels = 180f;
        public float AngleDegrees = 64f;
        public Color Tint = new Color(0.35f, 0.78f, 0.72f, 0.14f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            color = Tint;
            const int segments = 18;
            var origin = Vector2.zero;
            vh.AddVert(origin, color, Vector2.zero);

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(-AngleDegrees * 0.5f, AngleDegrees * 0.5f, t) + 90f;
                var radians = angle * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * RangePixels;
                vh.AddVert(point, color, Vector2.zero);
            }

            for (var i = 1; i <= segments; i++)
            {
                vh.AddTriangle(0, i, i + 1);
            }
        }
    }

    public sealed class FogGraphic : MaskableGraphic
    {
        public float WavePhase;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            const int segments = 24;
            var baseY = rect.yMax;
            var bottomY = rect.yMin;
            var left = rect.xMin;
            var width = rect.width;

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var x = left + width * t;
                var wave = Mathf.Sin(t * Mathf.PI * 5.5f + WavePhase) * 18f
                    + Mathf.Sin(t * Mathf.PI * 11f - WavePhase * 0.7f) * 8f;
                var top = baseY + wave;
                vh.AddVert(new Vector2(x, bottomY), color, Vector2.zero);
                vh.AddVert(new Vector2(x, top), color, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var a = i * 2;
                vh.AddTriangle(a, a + 1, a + 2);
                vh.AddTriangle(a + 1, a + 3, a + 2);
            }
        }
    }
}
