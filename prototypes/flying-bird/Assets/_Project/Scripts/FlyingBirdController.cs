using System;
using System.Collections.Generic;
using MannLab.HyperCasual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MannLab.Games.FlyingBird
{
    public sealed class FlyingBirdController : MonoBehaviour
    {
        private const string BestDistanceKey = "mannlab.flying_bird.best_distance";
        private const float MaxDisplayAltitude = 105f;
        private const float StartingAltitude = 56f;
        private const float StartingSpeed = 16f;
        private const float StartingEnergy = 150f;

        private readonly List<WindZone> windZones = new List<WindZone>();

        private System.Random random;
        private AudioSource audioSource;
        private AudioClip flapClip;
        private AudioClip updraftClip;
        private AudioClip failClip;
        private Text distanceText;
        private Text bestText;
        private Image energyFill;
        private RectTransform energyFillRect;
        private RectTransform birdRoot;
        private RectTransform leftWing;
        private RectTransform rightWing;
        private Image birdBody;
        private RawImage birdBodySprite;
        private RawImage wingPairSprite;
        private RawImage birdFrameSprite;
        private Image[] windIconPieces;
        private Image[] windStreaks;
        private GameObject resultPanel;
        private Text resultTitleText;
        private Text resultScoreText;
        private Texture2D gullBodyTexture;
        private Texture2D wingsGlideTexture;
        private Texture2D wingsUpTexture;
        private Texture2D wingsDownTexture;
        private Texture2D frameGlideTexture;
        private Texture2D frameUpTexture;
        private Texture2D frameDownTexture;

        private float distance;
        private float bestDistance;
        private float altitude;
        private float speed;
        private float verticalSpeed;
        private float energy;
        private float pitch;
        private float flapPulse;
        private float stallSeconds;
        private float lastUpdraftPingDistance;
        private bool runEnded;

        private void Awake()
        {
            MobileRuntime.ApplyDefaults();
            random = new System.Random(Environment.TickCount);
            bestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            flapClip = CreateToneClip("Flap", 520f, 0.045f, 0.28f);
            updraftClip = CreateToneClip("Updraft", 880f, 0.08f, 0.18f);
            failClip = CreateToneClip("Landing", 130f, 0.18f, 0.34f);

            BuildInterface();
            StartRun();
        }

        private void Update()
        {
            if (runEnded)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    StartRun();
                }

                return;
            }

            SimulateFlight(Time.deltaTime);
            UpdateInterface();
        }

        private void StartRun()
        {
            distance = 0f;
            altitude = StartingAltitude;
            speed = StartingSpeed;
            verticalSpeed = 0f;
            energy = StartingEnergy;
            pitch = 4f;
            flapPulse = 0f;
            stallSeconds = 0f;
            lastUpdraftPingDistance = -1000f;
            runEnded = false;
            resultPanel.SetActive(false);

            GenerateWindMap();
            UpdateInterface();
        }

        private void SimulateFlight(float deltaTime)
        {
            var flapping = IsFlapHeld() && energy > 0.5f;
            var wind = WindAt(distance);
            var windInfluence = flapping ? 0.48f : 1f;
            var horizontalWind = wind.Horizontal * wind.Strength * windInfluence;
            var verticalWind = wind.Vertical * wind.Strength * windInfluence;

            if (flapping)
            {
                energy = Mathf.Max(0f, energy - 42f * deltaTime);
                verticalSpeed += 8.8f * deltaTime;
                speed += (3.8f + Mathf.Max(0f, horizontalWind) * 0.35f) * deltaTime;
                pitch = Mathf.MoveTowards(pitch, 18f, 58f * deltaTime);
                flapPulse = Mathf.MoveTowards(flapPulse, 1f, 8f * deltaTime);

                if (!audioSource.isPlaying && UnityEngine.Random.value < deltaTime * 8f)
                {
                    PlayClip(flapClip);
                }
            }
            else
            {
                var glideTargetPitch = wind.Kind == WindKind.Updraft ? -2f : -13f;
                pitch = Mathf.MoveTowards(pitch, glideTargetPitch, 24f * deltaTime);
                verticalSpeed += (-5.2f + verticalWind * 5.8f) * deltaTime;
                speed += (horizontalWind + Mathf.Clamp(-pitch, 0f, 18f) * 0.1f - 0.58f) * deltaTime;
                flapPulse = Mathf.MoveTowards(flapPulse, 0f, 5f * deltaTime);

                if (wind.Kind == WindKind.Updraft && distance - lastUpdraftPingDistance > 34f)
                {
                    lastUpdraftPingDistance = distance;
                    PlayClip(updraftClip);
                }
            }

            speed -= Mathf.Max(0f, pitch - 14f) * 0.07f * deltaTime;
            speed = Mathf.Clamp(speed, 4.2f, 34f);

            if ((pitch > 24f && speed < 12.5f) || speed < 5f)
            {
                stallSeconds = Mathf.Max(stallSeconds, 0.55f);
            }

            if (stallSeconds > 0f)
            {
                stallSeconds -= deltaTime;
                verticalSpeed -= 9f * deltaTime;
                pitch = Mathf.MoveTowards(pitch, -26f, 80f * deltaTime);
                speed = Mathf.Max(4f, speed - 4.4f * deltaTime);
            }

            verticalSpeed = Mathf.Clamp(verticalSpeed, -24f, 16f);
            altitude += verticalSpeed * deltaTime;
            distance += speed * deltaTime;

            if (altitude <= 0f)
            {
                altitude = 0f;
                EndRun();
            }
        }

        private bool IsFlapHeld()
        {
            return Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0) || Input.touchCount > 0;
        }

        private void EndRun()
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            PlayClip(failClip);

            if (distance > bestDistance)
            {
                bestDistance = distance;
                PlayerPrefs.SetFloat(BestDistanceKey, bestDistance);
                PlayerPrefs.Save();
            }

            UpdateInterface();
            resultTitleText.text = "Flight End";
            resultScoreText.text = $"{Mathf.FloorToInt(distance)} m\nBest {Mathf.FloorToInt(bestDistance)} m";
            resultPanel.SetActive(true);
        }

        private void GenerateWindMap()
        {
            windZones.Clear();

            var cursor = 95f;
            AddWindZone(cursor, 90f, WindKind.Tailwind, 0.85f);
            cursor += 168f;
            AddWindZone(cursor, 95f, WindKind.Updraft, 0.82f);
            cursor += 156f;

            while (cursor < 3600f)
            {
                cursor += random.Next(44, 96);
                var roll = random.NextDouble();
                var kind = roll < 0.34 ? WindKind.Updraft : roll < 0.67 ? WindKind.Tailwind : WindKind.Headwind;
                var length = random.Next(62, 142);
                var strength = 0.65f + (float)random.NextDouble() * 0.62f + Mathf.Clamp01(cursor / 1800f) * 0.18f;
                AddWindZone(cursor, length, kind, strength);
                cursor += length;
            }
        }

        private void AddWindZone(float start, float length, WindKind kind, float strength)
        {
            windZones.Add(new WindZone(start, start + length, kind, strength));
        }

        private WindZone WindAt(float atDistance)
        {
            for (var i = 0; i < windZones.Count; i++)
            {
                if (windZones[i].Contains(atDistance))
                {
                    return windZones[i];
                }
            }

            return WindZone.Calm;
        }

        private WindZone NextWind(float atDistance)
        {
            for (var i = 0; i < windZones.Count; i++)
            {
                if (windZones[i].End >= atDistance)
                {
                    return windZones[i];
                }
            }

            return WindZone.Calm;
        }

        private void UpdateInterface()
        {
            distanceText.text = $"{Mathf.FloorToInt(distance)} m";
            bestText.text = $"Best {Mathf.FloorToInt(bestDistance)} m";
            UpdateEnergyGauge();

            var wind = WindAt(distance);
            var nextWind = NextWind(distance);
            UpdateWindIcon(wind, nextWind);

            var altitudeT = Mathf.Clamp01(altitude / MaxDisplayAltitude);
            birdRoot.anchoredPosition = new Vector2(-320f, Mathf.Lerp(-340f, 350f, altitudeT));
            birdRoot.localRotation = Quaternion.Euler(0f, 0f, pitch);

            var wingAngle = Mathf.Lerp(-8f, 28f, flapPulse);
            if (birdFrameSprite != null)
            {
                birdFrameSprite.texture = CurrentBirdFrameTexture();
                birdFrameSprite.color = stallSeconds > 0f ? new Color(1f, 0.88f, 0.56f, 0.96f) : Color.white;
            }
            else if (wingPairSprite != null)
            {
                wingPairSprite.texture = CurrentWingTexture();
                wingPairSprite.color = stallSeconds > 0f ? new Color(1f, 0.84f, 0.42f, 0.96f) : Color.white;
                if (birdBodySprite != null)
                {
                    birdBodySprite.color = stallSeconds > 0f ? new Color(1f, 0.88f, 0.56f, 0.96f) : Color.white;
                }
            }
            else
            {
                leftWing.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
                rightWing.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
                birdBody.color = stallSeconds > 0f ? SketchPalette.WarningAmber : SketchPalette.TilePaper;
            }

            UpdateWindStreaks(wind);
        }

        private void UpdateWindStreaks(WindZone wind)
        {
            if (windStreaks == null)
            {
                return;
            }

            var activeColor = WindStreakColor(wind.Kind, 0.28f);
            var calmColor = new Color(SketchPalette.HatchBlue.r, SketchPalette.HatchBlue.g, SketchPalette.HatchBlue.b, 0.15f);

            for (var i = 0; i < windStreaks.Length; i++)
            {
                var streak = windStreaks[i];
                if (streak != null)
                {
                    streak.color = wind.Kind == WindKind.Calm ? calmColor : activeColor;
                }
            }
        }

        private static Color WindStreakColor(WindKind kind, float alpha)
        {
            switch (kind)
            {
                case WindKind.Headwind:
                    return WithAlpha(SketchPalette.WarningAmber, alpha);
                case WindKind.Updraft:
                    return WithAlpha(SketchPalette.CorrectMarker, alpha);
                case WindKind.Tailwind:
                    return WithAlpha(SketchPalette.FocusBlue, alpha);
                default:
                    return WithAlpha(SketchPalette.HatchBlue, alpha);
            }
        }

        private void UpdateEnergyGauge()
        {
            if (energyFill == null || energyFillRect == null)
            {
                return;
            }

            var amount = Mathf.Clamp01(energy / StartingEnergy);
            energyFill.enabled = amount > 0.01f;
            energyFill.fillAmount = amount;
            energyFillRect.anchorMax = new Vector2(amount, 1f);
        }

        private void UpdateWindIcon(WindZone wind, WindZone nextWind)
        {
            if (windIconPieces == null)
            {
                return;
            }

            var displayedKind = wind.Kind == WindKind.Calm ? nextWind.Kind : wind.Kind;
            var alpha = wind.Kind == WindKind.Calm ? 0.45f : 0.9f;
            var color = WithAlpha(SketchPalette.MutedInk, 0.34f);
            switch (displayedKind)
            {
                case WindKind.Tailwind:
                    color = WithAlpha(SketchPalette.FocusBlue, alpha);
                    break;
                case WindKind.Headwind:
                    color = WithAlpha(SketchPalette.WarningAmber, alpha);
                    break;
                case WindKind.Updraft:
                    color = WithAlpha(SketchPalette.CorrectMarker, alpha);
                    break;
            }

            for (var i = 0; i < windIconPieces.Length; i++)
            {
                if (windIconPieces[i] != null)
                {
                    windIconPieces[i].enabled = displayedKind != WindKind.Calm;
                    windIconPieces[i].color = color;
                }
            }

            SetWindIconShape(displayedKind);
        }

        private Texture CurrentBirdFrameTexture()
        {
            if (!IsFlapHeld() || energy <= 0.5f)
            {
                return frameGlideTexture;
            }

            var flapFrame = Mathf.PingPong(Time.time * 10f, 1f);
            return flapFrame < 0.5f ? frameUpTexture : frameDownTexture;
        }

        private Texture CurrentWingTexture()
        {
            if (!IsFlapHeld() || energy <= 0.5f)
            {
                return wingsGlideTexture;
            }

            var flapFrame = Mathf.PingPong(Time.time * 10f, 1f);
            return flapFrame < 0.5f ? wingsUpTexture : wingsDownTexture;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            LoadBirdTextures();

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);
            CreateHeader(canvas.transform);
            CreateFlightStage(canvas.transform);
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

        private void CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            SetAnchor(header.GetComponent<RectTransform>(), 0f, 0.89f, 1f, 1f, 38f, -14f, -38f, -24f);

            distanceText = CreateText(header.transform, "0 m", 54, TextAnchor.MiddleLeft);
            SetAnchor(distanceText.GetComponent<RectTransform>(), 0f, 0f, 0.32f, 1f);

            bestText = CreateText(header.transform, "Best 0 m", 32, TextAnchor.MiddleCenter);
            bestText.color = SketchPalette.MutedInk;
            SetAnchor(bestText.GetComponent<RectTransform>(), 0.31f, 0f, 0.69f, 1f);

            energyFill = CreateAnchoredBar(header.transform, "Stamina Bar", 0.70f, 0.24f, 1f, 0.52f, SketchPalette.CorrectMarker);
        }

        private void CreateFlightStage(Transform parent)
        {
            var stage = new GameObject("Flight Stage", typeof(RectTransform));
            stage.transform.SetParent(parent, false);
            SetAnchor(stage.GetComponent<RectTransform>(), 0f, 0.05f, 1f, 0.89f, 42f, 0f, -42f, 0f);

            CreateClouds(stage.transform);
            CreateSeaBand(stage.transform);
            CreateShoreline(stage.transform);
            CreateWindStreaks(stage.transform);
            CreateWindIcon(stage.transform);

            birdRoot = new GameObject("Bird", typeof(RectTransform)).GetComponent<RectTransform>();
            birdRoot.transform.SetParent(stage.transform, false);
            birdRoot.anchorMin = new Vector2(0.5f, 0.5f);
            birdRoot.anchorMax = new Vector2(0.5f, 0.5f);
            birdRoot.sizeDelta = new Vector2(390f, 260f);

            if (frameGlideTexture != null && frameUpTexture != null && frameDownTexture != null)
            {
                CreateFullFrameBird(birdRoot);
                return;
            }

            if (gullBodyTexture != null && wingsGlideTexture != null && wingsUpTexture != null && wingsDownTexture != null)
            {
                CreateSpriteBird(birdRoot);
                return;
            }

            birdBody = new GameObject("Body", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            birdBody.transform.SetParent(birdRoot, false);
            birdBody.color = SketchPalette.TilePaper;
            var bodyRect = birdBody.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.27f, 0.30f);
            bodyRect.anchorMax = new Vector2(0.86f, 0.70f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            AddSketchOutline(birdBody.transform);

            leftWing = CreateWing(birdRoot, "Left Wing", new Vector2(0.26f, 0.49f), -18f);
            rightWing = CreateWing(birdRoot, "Right Wing", new Vector2(0.58f, 0.50f), 10f);

            var beak = new GameObject("Beak", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            beak.transform.SetParent(birdRoot, false);
            beak.text = ">";
            beak.font = GetDefaultFont();
            beak.fontSize = 56;
            beak.color = SketchPalette.WarningAmber;
            beak.alignment = TextAnchor.MiddleCenter;
            SetAnchor(beak.GetComponent<RectTransform>(), 0.78f, 0.22f, 1f, 0.78f);
        }

        private void LoadBirdTextures()
        {
            gullBodyTexture = Resources.Load<Texture2D>("wind-gull-body");
            wingsGlideTexture = Resources.Load<Texture2D>("wind-gull-wings-glide");
            wingsUpTexture = Resources.Load<Texture2D>("wind-gull-wings-up");
            wingsDownTexture = Resources.Load<Texture2D>("wind-gull-wings-down");
            frameGlideTexture = Resources.Load<Texture2D>("wind-gull-frame-glide");
            frameUpTexture = Resources.Load<Texture2D>("wind-gull-frame-up");
            frameDownTexture = Resources.Load<Texture2D>("wind-gull-frame-down");
        }

        private void CreateFullFrameBird(Transform parent)
        {
            birdFrameSprite = CreateRawSprite(parent, "Bird Frame Sprite", frameGlideTexture, new Vector2(360f, 252f), Vector2.zero);
        }

        private void CreateSpriteBird(Transform parent)
        {
            wingPairSprite = CreateRawSprite(parent, "Wing Pair", wingsGlideTexture, new Vector2(360f, 230f), new Vector2(-22f, 0f));
            wingPairSprite.transform.SetAsFirstSibling();

            birdBodySprite = CreateRawSprite(parent, "Body Sprite", gullBodyTexture, new Vector2(300f, 122f), new Vector2(-4f, 4f));
        }

        private static RawImage CreateRawSprite(Transform parent, string name, Texture texture, Vector2 size, Vector2 anchoredPosition)
        {
            var rawImage = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            rawImage.transform.SetParent(parent, false);
            rawImage.texture = texture;
            rawImage.color = Color.white;

            var rect = rawImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rawImage;
        }

        private RectTransform CreateWing(Transform parent, string name, Vector2 anchor, float rotation)
        {
            var wing = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            wing.transform.SetParent(parent, false);
            wing.anchorMin = anchor;
            wing.anchorMax = anchor;
            wing.pivot = new Vector2(0f, 0.5f);
            wing.sizeDelta = new Vector2(96f, 24f);
            wing.anchoredPosition = Vector2.zero;
            wing.localRotation = Quaternion.Euler(0f, 0f, rotation);
            wing.GetComponent<Image>().color = SketchPalette.HatchBlue;
            AddSketchOutline(wing.transform);
            return wing;
        }

        private Image CreateAnchoredBar(Transform parent, string name, float minX, float minY, float maxX, float maxY, Color color)
        {
            var shell = new GameObject(name, typeof(RectTransform), typeof(Image));
            shell.transform.SetParent(parent, false);
            SetAnchor(shell.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
            shell.GetComponent<Image>().color = SketchPalette.TilePaper;
            AddSketchOutline(shell.transform);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(shell.transform, false);
            Stretch(fill.GetComponent<RectTransform>(), new Vector2(5f, 5f), new Vector2(-5f, -5f));
            fill.color = color;
            fill.type = Image.Type.Simple;
            energyFillRect = fill.GetComponent<RectTransform>();
            return fill;
        }

        private static void CreateClouds(Transform parent)
        {
            CreateCloud(parent, "Cloud A", 0.06f, 0.74f, 0.27f, 0.12f, 61);
            CreateCloud(parent, "Cloud B", 0.56f, 0.61f, 0.24f, 0.10f, 97);
            CreateCloud(parent, "Cloud C", 0.30f, 0.86f, 0.18f, 0.08f, 131);
        }

        private static void CreateCloud(Transform parent, string name, float x, float y, float width, float height, int seed)
        {
            var background = new Color32(255, 253, 247, 190);
            var hatch = new Color32(255, 255, 255, 105);

            CreateEllipseHatch(parent, $"{name} Body", x, y, x + width, y + height, background, hatch, 18f, 2.1f, seed);
            CreateEllipseHatch(parent, $"{name} Lift", x + width * 0.16f, y + height * 0.28f, x + width * 0.58f, y + height * 1.16f, background, hatch, 16f, 2f, seed + 9);
            CreateEllipseHatch(parent, $"{name} Tail", x + width * 0.54f, y + height * 0.10f, x + width * 1.08f, y + height * 0.82f, background, hatch, 17f, 1.9f, seed + 17);
        }

        private static void CreateSeaBand(Transform parent)
        {
            CreateHatchPatch(parent, "Sea", 0f, 0f, 1f, 0.16f, new Color32(250, 247, 239, 0), new Color32(61, 135, 176, 125), 4f, 20f, 2.6f, 203);
            CreateSolidLine(parent, "Horizon Line", 0.02f, 0.178f, 0.98f, 0.181f, new Color32(55, 51, 47, 150));
            CreateSolidLine(parent, "Water Edge", 0.05f, 0.157f, 0.86f, 0.160f, new Color32(61, 135, 176, 130));
        }

        private static void CreateShoreline(Transform parent)
        {
            for (var i = 0; i < 4; i++)
            {
                var wave = new GameObject($"Wave {i + 1}", typeof(RectTransform), typeof(Image));
                wave.transform.SetParent(parent, false);
                var y = 0.055f + i * 0.026f;
                SetAnchor(wave.GetComponent<RectTransform>(), 0.05f + i * 0.08f, y, 0.52f + i * 0.11f, y + 0.008f);
                wave.GetComponent<Image>().color = new Color32(255, 253, 247, 150);
            }
        }

        private static void CreateSolidLine(Transform parent, string name, float minX, float minY, float maxX, float maxY, Color color)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            SetAnchor(line.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
            line.GetComponent<Image>().color = color;
        }

        private void CreateWindStreaks(Transform parent)
        {
            windStreaks = new Image[8];
            for (var i = 0; i < windStreaks.Length; i++)
            {
                var streak = new GameObject($"Wind Streak {i + 1}", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                streak.transform.SetParent(parent, false);
                var y = 0.29f + i * 0.073f;
                var width = 0.13f + i % 3 * 0.07f;
                var x = 0.04f + i * 0.115f;
                SetAnchor(streak.GetComponent<RectTransform>(), x, y, Mathf.Min(0.98f, x + width), y + 0.004f);
                streak.color = new Color(SketchPalette.HatchBlue.r, SketchPalette.HatchBlue.g, SketchPalette.HatchBlue.b, 0.13f);
            }
        }

        private void CreateWindIcon(Transform parent)
        {
            var root = new GameObject("Wind Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            root.transform.SetParent(parent, false);
            SetAnchor(root, 0.80f, 0.75f, 0.98f, 0.95f);

            windIconPieces = new Image[5];
            for (var i = 0; i < windIconPieces.Length; i++)
            {
                windIconPieces[i] = CreateIconStroke(root, $"Wind Icon Stroke {i + 1}");
            }
        }

        private static Image CreateIconStroke(Transform parent, string name)
        {
            var stroke = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            stroke.transform.SetParent(parent, false);
            stroke.color = SketchPalette.FocusBlue;
            stroke.raycastTarget = false;
            stroke.enabled = false;
            return stroke;
        }

        private void SetWindIconShape(WindKind kind)
        {
            if (windIconPieces == null)
            {
                return;
            }

            for (var i = 0; i < windIconPieces.Length; i++)
            {
                if (windIconPieces[i] != null)
                {
                    windIconPieces[i].enabled = kind != WindKind.Calm;
                }
            }

            switch (kind)
            {
                case WindKind.Tailwind:
                    SetIconStroke(0, new Vector2(-8f, 0f), new Vector2(78f, 9f), 0f);
                    SetIconStroke(1, new Vector2(34f, 15f), new Vector2(44f, 9f), -42f);
                    SetIconStroke(2, new Vector2(34f, -15f), new Vector2(44f, 9f), 42f);
                    SetIconStroke(3, new Vector2(-42f, 22f), new Vector2(58f, 6f), 0f);
                    SetIconStroke(4, new Vector2(-50f, -22f), new Vector2(44f, 6f), 0f);
                    break;
                case WindKind.Headwind:
                    SetIconStroke(0, new Vector2(8f, 0f), new Vector2(78f, 9f), 0f);
                    SetIconStroke(1, new Vector2(-34f, 15f), new Vector2(44f, 9f), 42f);
                    SetIconStroke(2, new Vector2(-34f, -15f), new Vector2(44f, 9f), -42f);
                    SetIconStroke(3, new Vector2(42f, 22f), new Vector2(58f, 6f), 0f);
                    SetIconStroke(4, new Vector2(50f, -22f), new Vector2(44f, 6f), 0f);
                    break;
                case WindKind.Updraft:
                    SetIconStroke(0, new Vector2(0f, -10f), new Vector2(9f, 82f), 0f);
                    SetIconStroke(1, new Vector2(-14f, 28f), new Vector2(42f, 9f), 42f);
                    SetIconStroke(2, new Vector2(14f, 28f), new Vector2(42f, 9f), -42f);
                    SetIconStroke(3, new Vector2(-38f, -18f), new Vector2(7f, 54f), 0f);
                    SetIconStroke(4, new Vector2(38f, -26f), new Vector2(7f, 44f), 0f);
                    break;
            }
        }

        private void SetIconStroke(int index, Vector2 position, Vector2 size, float rotation)
        {
            if (windIconPieces == null || index < 0 || index >= windIconPieces.Length || windIconPieces[index] == null)
            {
                return;
            }

            var rect = windIconPieces[index].GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static SketchHatchFillGraphic CreateHatchPatch(
            Transform parent,
            string name,
            float minX,
            float minY,
            float maxX,
            float maxY,
            Color backgroundColor,
            Color hatchColor,
            float inset,
            float spacing,
            float thickness,
            int seed)
        {
            var hatch = new GameObject(name, typeof(RectTransform), typeof(SketchHatchFillGraphic)).GetComponent<SketchHatchFillGraphic>();
            hatch.transform.SetParent(parent, false);
            SetAnchor(hatch.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
            hatch.BackgroundColor = backgroundColor;
            hatch.HatchColor = hatchColor;
            hatch.Inset = inset;
            hatch.Spacing = spacing;
            hatch.Thickness = thickness;
            hatch.Jitter = 3.4f;
            hatch.Strokes = 2;
            hatch.Seed = seed;
            return hatch;
        }

        private static EllipseHatchGraphic CreateEllipseHatch(
            Transform parent,
            string name,
            float minX,
            float minY,
            float maxX,
            float maxY,
            Color backgroundColor,
            Color hatchColor,
            float spacing,
            float thickness,
            int seed)
        {
            var hatch = new GameObject(name, typeof(RectTransform), typeof(EllipseHatchGraphic)).GetComponent<EllipseHatchGraphic>();
            hatch.transform.SetParent(parent, false);
            SetAnchor(hatch.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
            hatch.BackgroundColor = backgroundColor;
            hatch.HatchColor = hatchColor;
            hatch.Spacing = spacing;
            hatch.Thickness = thickness;
            hatch.Jitter = 3f;
            hatch.Strokes = 2;
            hatch.Seed = seed;
            return hatch;
        }

        private void CreateResultPanel(Transform parent)
        {
            resultPanel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(parent, false);
            var rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640f, 420f);
            rect.anchoredPosition = Vector2.zero;
            resultPanel.GetComponent<Image>().color = new Color(SketchPalette.TilePaper.r, SketchPalette.TilePaper.g, SketchPalette.TilePaper.b, 0.97f);
            AddSketchOutline(resultPanel.transform);

            resultTitleText = CreateText(resultPanel.transform, "Flight End", 54, TextAnchor.MiddleCenter);
            SetAnchor(resultTitleText.GetComponent<RectTransform>(), 0f, 0.62f, 1f, 0.92f, 30f, 0f, -30f, 0f);

            resultScoreText = CreateText(resultPanel.transform, "0 m\nBest 0 m", 40, TextAnchor.MiddleCenter);
            SetAnchor(resultScoreText.GetComponent<RectTransform>(), 0f, 0.32f, 1f, 0.62f, 30f, 0f, -30f, 0f);

            var restart = CreateSketchButton(resultPanel.transform, "Again", 36);
            var restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.08f);
            restartRect.anchorMax = new Vector2(0.5f, 0.08f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.sizeDelta = new Vector2(270f, 92f);
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
            text.resizeTextMinSize = Mathf.Max(14, size / 2);
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

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static AudioClip CreateToneClip(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 30f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private sealed class EllipseHatchGraphic : Graphic
        {
            private Color backgroundColor = new Color32(255, 253, 247, 190);
            private Color hatchColor = new Color32(133, 203, 255, 70);
            private float spacing = 18f;
            private float thickness = 2f;
            private float jitter = 3f;
            private int strokes = 2;
            private int seed = 31;

            public Color BackgroundColor
            {
                get => backgroundColor;
                set
                {
                    backgroundColor = value;
                    SetVerticesDirty();
                }
            }

            public Color HatchColor
            {
                get => hatchColor;
                set
                {
                    hatchColor = value;
                    SetVerticesDirty();
                }
            }

            public float Spacing
            {
                get => spacing;
                set
                {
                    spacing = Mathf.Max(4f, value);
                    SetVerticesDirty();
                }
            }

            public float Thickness
            {
                get => thickness;
                set
                {
                    thickness = Mathf.Max(0.5f, value);
                    SetVerticesDirty();
                }
            }

            public float Jitter
            {
                get => jitter;
                set
                {
                    jitter = Mathf.Max(0f, value);
                    SetVerticesDirty();
                }
            }

            public int Strokes
            {
                get => strokes;
                set
                {
                    strokes = Mathf.Max(1, value);
                    SetVerticesDirty();
                }
            }

            public int Seed
            {
                get => seed;
                set
                {
                    seed = value;
                    SetVerticesDirty();
                }
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                var rect = GetPixelAdjustedRect();
                var center = rect.center;
                var radius = new Vector2(Mathf.Max(1f, rect.width * 0.5f), Mathf.Max(1f, rect.height * 0.5f));
                AddEllipse(vh, center, radius, backgroundColor);

                var direction = new Vector2(1f, 1f).normalized;
                var normal = new Vector2(-direction.y, direction.x);
                var diagonal = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height);
                var lineIndex = 0;

                for (var offset = -diagonal; offset <= diagonal; offset += spacing)
                {
                    for (var stroke = 0; stroke < strokes; stroke++)
                    {
                        var lineSeed = seed + lineIndex * 47 + stroke * 17;
                        var shiftedOffset = offset + (Fract(Mathf.Sin(lineSeed * 19.181f) * 21173.17f) - 0.5f) * jitter;
                        if (TryEllipseLine(center, radius, direction, normal, shiftedOffset, lineSeed, out var start, out var end))
                        {
                            AddLine(vh, start, end, hatchColor, thickness);
                        }
                    }

                    lineIndex++;
                }
            }

            private bool TryEllipseLine(Vector2 center, Vector2 radius, Vector2 direction, Vector2 normal, float offset, int lineSeed, out Vector2 start, out Vector2 end)
            {
                var origin = normal * offset;
                var a = direction.x * direction.x / (radius.x * radius.x) + direction.y * direction.y / (radius.y * radius.y);
                var b = 2f * (origin.x * direction.x / (radius.x * radius.x) + origin.y * direction.y / (radius.y * radius.y));
                var c = origin.x * origin.x / (radius.x * radius.x) + origin.y * origin.y / (radius.y * radius.y) - 1f;
                var discriminant = b * b - 4f * a * c;

                if (discriminant <= 0f || Mathf.Approximately(a, 0f))
                {
                    start = Vector2.zero;
                    end = Vector2.zero;
                    return false;
                }

                var root = Mathf.Sqrt(discriminant);
                var t0 = (-b - root) / (2f * a);
                var t1 = (-b + root) / (2f * a);
                start = center + origin + direction * t0 + JitterOffset(lineSeed + 1);
                end = center + origin + direction * t1 + JitterOffset(lineSeed + 2);
                return true;
            }

            private Vector2 JitterOffset(int value)
            {
                var x = Mathf.Sin(value * 12.9898f) * 43758.5453f;
                var y = Mathf.Sin((value + 19) * 78.233f) * 24634.6345f;
                return new Vector2((Fract(x) - 0.5f) * jitter, (Fract(y) - 0.5f) * jitter);
            }

            private static float Fract(float value)
            {
                return value - Mathf.Floor(value);
            }

            private static void AddEllipse(VertexHelper vh, Vector2 center, Vector2 radius, Color vertexColor)
            {
                const int segments = 36;
                var centerIndex = vh.currentVertCount;
                vh.AddVert(center, vertexColor, Vector2.zero);

                for (var i = 0; i <= segments; i++)
                {
                    var angle = Mathf.PI * 2f * i / segments;
                    var point = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                    vh.AddVert(point, vertexColor, Vector2.zero);
                }

                for (var i = 1; i <= segments; i++)
                {
                    vh.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
                }
            }

            private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end, Color vertexColor, float lineThickness)
            {
                var delta = end - start;
                if (delta.sqrMagnitude <= Mathf.Epsilon)
                {
                    return;
                }

                var direction = delta.normalized;
                var normal = new Vector2(-direction.y, direction.x) * (lineThickness * 0.5f);
                var index = vh.currentVertCount;

                vh.AddVert(start - normal, vertexColor, Vector2.zero);
                vh.AddVert(start + normal, vertexColor, Vector2.zero);
                vh.AddVert(end + normal, vertexColor, Vector2.zero);
                vh.AddVert(end - normal, vertexColor, Vector2.zero);

                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }

        private enum WindKind
        {
            Calm,
            Tailwind,
            Headwind,
            Updraft
        }

        private readonly struct WindZone
        {
            public static readonly WindZone Calm = new WindZone(float.MinValue, float.MinValue, WindKind.Calm, 0f);

            public readonly float Start;
            public readonly float End;
            public readonly WindKind Kind;
            public readonly float Strength;

            public WindZone(float start, float end, WindKind kind, float strength)
            {
                Start = start;
                End = end;
                Kind = kind;
                Strength = strength;
            }

            public float Horizontal
            {
                get
                {
                    if (Kind == WindKind.Tailwind)
                    {
                        return 4.6f;
                    }

                    if (Kind == WindKind.Headwind)
                    {
                        return -4.2f;
                    }

                    return 0f;
                }
            }

            public float Vertical => Kind == WindKind.Updraft ? 1f : 0f;

            public bool Contains(float atDistance)
            {
                return atDistance >= Start && atDistance <= End;
            }
        }
    }
}
