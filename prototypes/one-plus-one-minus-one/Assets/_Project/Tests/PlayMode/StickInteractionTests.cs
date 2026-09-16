using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MannLab.Games.OnePlusOneMinusOne.Tests
{
    public sealed class StickInteractionTests
    {
        private OnePlusOneMinusOneController controller;
        private Scene scene;
        private static readonly string[] ProgressKeys = {
            "OnePlusOneMinusOne.GoalMode.HighestUnlockedRound",
            "OnePlusOneMinusOne.GoalMode.Completed",
            "OnePlusOneMinusOne.Audio.Enabled"
        };
        private bool[] savedKeys;
        private int[] savedProgress;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savedKeys = ProgressKeys.Select(PlayerPrefs.HasKey).ToArray();
            savedProgress = ProgressKeys.Select(key => PlayerPrefs.GetInt(key, 0)).ToArray();
            foreach (var key in ProgressKeys) PlayerPrefs.DeleteKey(key);
            scene = SceneManager.CreateScene("Stick interaction test");
            SceneManager.SetActiveScene(scene);
            controller = new GameObject("Test game").AddComponent<OnePlusOneMinusOneController>();
            Call("HideRoundSelect");
            Call("LoadRound", 8);
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var i = 0; i < ProgressKeys.Length; i++)
            {
                if (savedKeys[i]) PlayerPrefs.SetInt(ProgressKeys[i], savedProgress[i]);
                else PlayerPrefs.DeleteKey(ProgressKeys[i]);
            }
            PlayerPrefs.Save();
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator SoundPreferenceSurvivesResetAndControllerRestart()
        {
            var toggle = Field<Toggle>("soundToggle");
            Assert.That(toggle.isOn, Is.True);
            toggle.isOn = false;
            Assert.That(Field<AudioSource>("sfxSource").mute, Is.True);
            Call("ResetRound");
            Assert.That(toggle.isOn, Is.False);
            UnityEngine.Object.Destroy(controller.gameObject);
            yield return null;
            controller = new GameObject("Restored game").AddComponent<OnePlusOneMinusOneController>();
            yield return null;
            Assert.That(Field<Toggle>("soundToggle").isOn, Is.False);
            Assert.That(Field<AudioSource>("sfxSource").mute, Is.True);
            Field<Toggle>("soundToggle").isOn = true;
            Assert.That(Field<AudioSource>("sfxSource").mute, Is.False);
            Assert.That(PlayerPrefs.GetInt("OnePlusOneMinusOne.Audio.Enabled"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RoundPickerKeepsReadableLabelsAndTouchTargetsOnSmallPhone()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(720, 1278);
            typeof(OnePlusOneMinusOneController).GetField("highestUnlockedRoundIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, 99);
            Call("ShowRoundSelect");
            yield return null;
            Canvas.ForceUpdateCanvases();
            const float cssScale = 320f / 720f;
            var panel = Field<RectTransform>("roundSelectPanel");
            var pager = (RectTransform)panel.Find("Round Pager");
            var actions = (RectTransform)panel.Find("Round Actions");
            var originalPagerBounds = BoundsIn(pager, safe);
            var originalActionBounds = BoundsIn(actions, safe);
            var soundBounds = BoundsIn(Field<Toggle>("soundToggle").GetComponent<RectTransform>(), safe);
            Assert.That(soundBounds.height * cssScale, Is.GreaterThanOrEqualTo(44f));
            Assert.That(soundBounds.Overlaps(BoundsIn((RectTransform)panel.Find("Round Select Header/Round Select Title"), safe)), Is.False);
            for (var page = 0; page < 9; page++)
            {
                if (page > 0) Call("ChangeRoundSelectPage", 1);
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector2.Distance(BoundsIn(pager, safe).center, originalPagerBounds.center),
                    Is.LessThan(0.01f), $"Page {page + 1}: navigation moved after changing pages");
                Assert.That(Vector2.Distance(BoundsIn(actions, safe).center, originalActionBounds.center),
                    Is.LessThan(0.01f), $"Page {page + 1}: Close moved after changing pages");
                var buttons = Field<List<Button>>("roundSelectButtons").Where(b => b.gameObject.activeSelf)
                    .Concat(new[] { Field<Button>("roundPrevButton"), Field<Button>("roundNextButton"),
                        panel.Find("Round Actions/Close Round Select").GetComponent<Button>() });
                foreach (var button in buttons)
                {
                    var bounds = BoundsIn((RectTransform)button.transform, safe);
                    Assert.That(bounds.height * cssScale, Is.GreaterThanOrEqualTo(44f), button.name);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(safe.rect.xMin));
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(safe.rect.xMax));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(safe.rect.yMin));
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(safe.rect.yMax));
                }
                foreach (var label in Field<List<Text>>("roundSelectLabels").Where(t => t.gameObject.activeSelf))
                {
                    var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                    settings.scaleFactor = 1f;
                    using var generator = new TextGenerator();
                    generator.Populate(label.text, settings);
                    var fontSize = label.resizeTextForBestFit ? generator.fontSizeUsedForBestFit : label.fontSize;
                    Assert.That(fontSize * panel.localScale.x * cssScale, Is.GreaterThanOrEqualTo(12f), label.text);
                    settings.resizeTextForBestFit = false;
                    settings.fontSize = fontSize;
                    Assert.That(generator.GetPreferredHeight(label.text, settings),
                        Is.LessThanOrEqualTo(label.rectTransform.rect.height + 0.5f), "Clipped label: " + label.text);
                }
            }
        }

        [UnityTest]
        public IEnumerator PlayerEqualityHidesFixedTargetAndRestoresItWhenRemoved()
        {
            const int roundIndex = 38;
            var expectedTarget = "= " + OnePlusOneMinusOneRules.FormatNumber(
                OnePlusOneMinusOneRules.GoalModeRounds[roundIndex].TargetValue);
            Call("LoadRound", roundIndex);
            Call("FillCurrentRoundWithSample");
            Call("RefreshUi");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Field<Text>("targetText").text, Is.EqualTo(expectedTarget));
            var originalSlotPosition = SlotPosition(0);
            var symbols = Field<string[]>("slotSymbols");
            Call("CycleStickPose", 1, 0);
            Call("CycleStickPose", 1, 0);
            yield return null;
            Assert.That(symbols[1], Is.EqualTo("="), "Rotating a plus into two horizontal sticks must space them apart");
            Assert.That(Field<Text>("targetText").gameObject.activeSelf, Is.False,
                "A player equality must not be followed by a contradictory fixed target");
            Assert.That(Vector2.Distance(SlotPosition(0), originalSlotPosition), Is.LessThan(0.01f),
                "Recognizing an equality must not move the player's slots");
            Call("CycleStickPose", 1, 0);
            yield return null;
            Assert.That(symbols[1], Is.EqualTo("+"));
            Assert.That(Field<Text>("targetText").gameObject.activeSelf, Is.True);
            Assert.That(Field<Text>("targetText").text, Is.EqualTo(expectedTarget));
        }

        [UnityTest]
        public IEnumerator FixedTargetsStayOnOneLineWithinTheirBounds()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            foreach (var width in new[] { 1080f, 720f, 390f, 320f })
            {
                safe.sizeDelta = new Vector2(width, width * 2);
                for (var index = 0; index < 100; index++)
                {
                    Call("LoadRound", index);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var target = Field<Text>("targetText");
                    if (!target.gameObject.activeInHierarchy) continue;
                    var context = $"Round {index + 1}, safe width {width}, target {target.text}";
                    Assert.That(target.cachedTextGenerator.lineCount, Is.EqualTo(1), context);
                    var settings = target.GetGenerationSettings(Vector2.zero);
                    settings.resizeTextForBestFit = false;
                    if (target.resizeTextForBestFit)
                        settings.fontSize = target.cachedTextGenerator.fontSizeUsedForBestFit;
                    var textWidth = target.cachedTextGeneratorForLayout.GetPreferredWidth(target.text, settings) / target.pixelsPerUnit;
                    Assert.That(textWidth, Is.LessThanOrEqualTo(target.rectTransform.rect.width + 1f), context);
                }
            }
        }

        [UnityTest]
        public IEnumerator FixedTargetKeepsRenderedGlyphsDuringPlacement()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(720, 1278);
            Call("LoadRound", 86);
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertTargetGlyphs("Before placement");

            foreach (var step in new[] { (slot: 0, turns: 0), (slot: 1, turns: 0), (slot: 1, turns: 2) })
            {
                for (var turn = 0; turn < step.turns; turn++)
                {
                    Call("TapBankStick", 0);
                    yield return null;
                }
                var pointer = Pointer(1, SlotPosition(step.slot));
                Call("BeginStickDrag", pointer, SourceGroup(), -1, 0);
                Call("EndStickDrag", pointer);
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertTargetGlyphs($"After slot {step.slot} placement");
            }
        }

        private void AssertTargetGlyphs(string context)
        {
            var target = Field<Text>("targetText");
            Assert.That(target.gameObject.activeInHierarchy, Is.True, context);
            Assert.That(target.text, Is.EqualTo("= 122"), context);
            var mesh = target.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null, context);
            Assert.That(mesh.vertexCount, Is.GreaterThanOrEqualTo(16), context);
            Assert.That(target.canvasRenderer.cull, Is.False, context);
        }

        [UnityTest]
        public IEnumerator FontAtlasRefreshRepairsStaleTargetMeshOnNextFrame()
        {
            Call("LoadRound", 86);
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertTargetGlyphs("Initial target");
            var target = Field<Text>("targetText");
            var materialRefreshed = false;
            target.RegisterDirtyMaterialCallback(() => materialRefreshed = true);
            target.canvasRenderer.Clear();
            Call("OnFontTextureRebuilt", target.font);
            Assert.That(Field<bool>("pendingFontMeshRefresh"), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertTargetGlyphs("Deferred atlas refresh");
            Assert.That(materialRefreshed, Is.True, "Atlas replacement must refresh the bound texture as well as glyph UVs");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Field<bool>("pendingFontMeshRefresh"), Is.False,
                "Stable text must not require rebuilding every frame");
        }

        [UnityTest]
        public IEnumerator FontAtlasSettlesAfterRoundTransitions()
        {
            foreach (var index in new[] { 0, 7, 95, 96, 97, 98, 99 })
            {
                Call("LoadRound", index);
                yield return null;
                Canvas.ForceUpdateCanvases();
            }
            for (var frame = 0; frame < 10; frame++) yield return null;
            var rebuilds = 0;
            var gameFont = Field<Font>("font");
            Action<Font> observe = rebuilt => { if (rebuilt == gameFont) rebuilds++; };
            Font.textureRebuilt += observe;
            try
            {
                for (var frame = 0; frame < 60; frame++) yield return null;
                Assert.That(rebuilds, Is.Zero, "Stable idle text must not churn the font atlas");
                Assert.That(Field<bool>("pendingFontMeshRefresh"), Is.False);
            }
            finally
            {
                Font.textureRebuilt -= observe;
            }
        }

        [UnityTest]
        public IEnumerator PrivacyEntryOnlyAppearsWhenRequired()
        {
            Call("ShowRoundSelect");
            Call("UpdatePrivacyOptionsEntry", false);
            var privacy = Field<Button>("privacyOptionsButton");
            Assert.That(privacy.gameObject.activeSelf, Is.False);
            Call("UpdatePrivacyOptionsEntry", true);
            Canvas.ForceUpdateCanvases();
            Assert.That(privacy.gameObject.activeSelf && privacy.interactable, Is.True);
            var actions = (RectTransform)privacy.transform.parent;
            var close = (RectTransform)actions.Find("Close Round Select");
            Assert.That(BoundsIn(close, actions).Overlaps(BoundsIn((RectTransform)privacy.transform, actions)), Is.False);
            Assert.That(((RectTransform)privacy.transform).rect.height, Is.EqualTo(80f));
            Call("UpdatePrivacyOptionsEntry", false);
            Canvas.ForceUpdateCanvases();
            Assert.That(close.rect.width, Is.EqualTo(actions.rect.width).Within(0.5f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LongRoundKeepsFooterInsidePortraitStage()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(560, 840);
            Call("UpdateStageLayout");
            Call("LoadRound", 29);
            yield return null;
            safe.sizeDelta = new Vector2(720, 1280);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var stage = Field<RectTransform>("stageRoot");
            Assert.That(Field<float>("equationLayoutWidth"), Is.EqualTo(612f).Within(0.5f));
            var footer = (RectTransform)stage.Find("Footer");
            var corners = new Vector3[4];
            footer.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var p = safe.InverseTransformPoint(corner);
                Assert.That(p.y, Is.InRange(safe.rect.yMin, safe.rect.yMax), "Footer leaves safe area");
            }

            for (var round = 0; round < 100; round++)
            {
                Call("LoadRound", round);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var target = Field<Text>("targetText").rectTransform;
                var targetBounds = BoundsIn(target, safe);
                foreach (var view in (IList)Field<object>("slotViews"))
                {
                    var slot = (RectTransform)view.GetType().GetProperty("Root").GetValue(view);
                    var bounds = BoundsIn(slot, safe);
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(safe.rect.yMin), $"Round {round + 1} slot bottom");
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(safe.rect.xMin), $"Round {round + 1} slot left");
                    Assert.That(bounds.xMax, Is.LessThanOrEqualTo(safe.rect.xMax), $"Round {round + 1} slot right");
                    if (target.gameObject.activeSelf)
                        Assert.That(bounds.Overlaps(targetBounds), Is.False, $"Round {round + 1} target overlaps slot");
                }
                Assert.That(BoundsIn(footer, safe).yMin, Is.GreaterThanOrEqualTo(safe.rect.yMin), $"Round {round + 1} footer");
                var bank = Field<RectTransform>("stickBank");
                foreach (var stick in Field<List<RectTransform>>("bankStickViews"))
                {
                    var body = (RectTransform)stick.Find("Raw Stick Body");
                    var bounds = BoundsIn(body, bank);
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(bank.rect.yMin - 4f), $"Round {round + 1} bank bottom");
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(bank.rect.yMax + 4f), $"Round {round + 1} bank top");
                }
            }
        }

        private static Rect BoundsIn(RectTransform rect, RectTransform parent)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var points = corners.Select(parent.InverseTransformPoint).ToArray();
            return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
        }

        [UnityTest]
        public IEnumerator GeneratedEffectsHaveAnAudioListenerAndPlayableSignal()
        {
            Assert.That(UnityEngine.Object.FindObjectsOfType<AudioListener>().Length, Is.EqualTo(1));
            var clips = (IDictionary)Field<object>("sfxClips");
            Assert.That(clips.Count, Is.GreaterThan(0));
            foreach (AudioClip clip in clips.Values)
            {
                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True);
                Assert.That(samples.Any(sample => Mathf.Abs(sample) > 0.001f), Is.True, clip.name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstClearAdEventKeepsNonReplayContext()
        {
            Call("LoadRound", 0);
            Call("FillCurrentRoundWithSample");
            var events = new List<string>();
            Application.LogCallback capture = (message, trace, type) => events.Add(message);
            Application.logMessageReceived += capture;
            try
            {
                Call("CheckCurrent");
                Assert.That(events.Any(e => e.Contains("ad_interstitial_opportunity") && e.Contains("is_replay=false")), Is.True);
                Assert.That(PlayerPrefs.GetInt(ProgressKeys[0]), Is.EqualTo(1));
                Call("LoadRound", 0);
                Assert.That(Field<bool>("currentRoundIsReplay"), Is.True);
            }
            finally { Application.logMessageReceived -= capture; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnavailableMilestoneAdIsSkippedWithoutQueuingLateDisplay()
        {
            Call("LoadRound", 9);
            Call("FillCurrentRoundWithSample");
            var events = new List<string>();
            Application.LogCallback capture = (message, trace, type) => events.Add(message);
            Application.logMessageReceived += capture;
            try
            {
                Call("CheckCurrent");
                Assert.That(events.Any(e => e.Contains("ad_interstitial_opportunity") &&
                    e.Contains("eligible=true") && e.Contains("will_show=false")), Is.True);
                Assert.That(events.Any(e => e.Contains("Game-over interstitial check")), Is.False);
                yield return new WaitForSeconds(1.3f);
                Assert.That(Field<int>("roundIndex"), Is.EqualTo(10));
            }
            finally { Application.logMessageReceived -= capture; }
        }

        [UnityTest]
        public IEnumerator FinalClearSurvivesRestartAndReplayDoesNotOfferAd()
        {
            Call("LoadRound", 99);
            Call("FillCurrentRoundWithSample");
            Call("CheckCurrent");
            Assert.That(PlayerPrefs.GetInt(ProgressKeys[1], 0), Is.EqualTo(1));
            Call("ResetRound");
            Assert.That(PlayerPrefs.GetInt(ProgressKeys[1], 0), Is.EqualTo(1));
            yield return SceneManager.UnloadSceneAsync(scene);
            scene = SceneManager.CreateScene("Restart after final clear");
            SceneManager.SetActiveScene(scene);
            controller = new GameObject("Restarted game").AddComponent<OnePlusOneMinusOneController>();
            yield return null;
            Assert.That(Field<int>("roundIndex"), Is.EqualTo(99));
            Assert.That(Field<RectTransform>("roundSelectOverlay").gameObject.activeSelf, Is.True);
            Assert.That(Field<List<Text>>("roundSelectLabels").Any(t => t.text.StartsWith("100 Done")), Is.True);
            Call("HideRoundSelect");
            Call("ResetRound");
            Assert.That(Field<bool>("currentRoundIsReplay"), Is.True);
            var args = new object[] { null };
            Assert.That(Call("ShouldOfferRoundClearInterstitial", args), Is.False);
            Assert.That(args[0], Is.EqualTo("replay_round"));
        }

        [UnityTest]
        public IEnumerator PlacementAndRotationDoNotPresentTokenAsEquationResult()
        {
            Assert.That(Call("TryAddStickToSlot", 0, false, StickPose.CenterVertical), Is.True);
            Assert.That(Field<string[]>("slotSymbols")[0], Is.EqualTo("1"));
            Assert.That(Field<Text>("feedbackText").text, Is.Empty);
            Call("CycleStickPose", 0, 0);
            Assert.That(Field<string[]>("slotSymbols")[0], Is.EqualTo("/"));
            Assert.That(Field<Text>("feedbackText").text, Is.Empty);
            Call("CheckCurrent");
            Assert.That(Field<Text>("feedbackText").text, Is.EqualTo("Fill every box."));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecognitionLabelsStayReadableOnDenseSmallPhoneBoards()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(720, 1278);
            Call("UpdateStageLayout");
            foreach (var round in new[] { 52, 53, 54, 56, 58, 99 })
            {
                Call("LoadRound", round);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var labels = Field<RectTransform>("equationRow").GetComponentsInChildren<Text>(true)
                    .Where(text => text.name == "Recognition Label").ToArray();
                Assert.That(labels.Length, Is.EqualTo(OnePlusOneMinusOneRules.GoalModeRounds[round].SlotTypes.Length));
                foreach (var label in labels)
                {
                    label.text = "111";
                    var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                    settings.scaleFactor = 1f;
                    using var generator = new TextGenerator();
                    generator.Populate(label.text, settings);
                    var scale = label.transform.lossyScale.x / safe.lossyScale.x * (320f / 720f);
                    Assert.That(generator.fontSizeUsedForBestFit * scale, Is.GreaterThanOrEqualTo(10f),
                        $"Round {round + 1}: recognition label");
                    settings.resizeTextForBestFit = false;
                    settings.fontSize = generator.fontSizeUsedForBestFit;
                    Assert.That(generator.GetPreferredHeight(label.text, settings),
                        Is.LessThanOrEqualTo(label.rectTransform.rect.height + 0.5f));
                    var stickLayer = (RectTransform)label.transform.parent.Find("Slot Sticks");
                    Assert.That(BoundsIn(label.rectTransform, safe).Overlaps(BoundsIn(stickLayer, safe)), Is.False);
                }
            }
        }

        [UnityTest]
        public IEnumerator EssentialStatusTextRemainsReadableOnSmallPhone()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(720, 1278);
            Call("UpdateStageLayout");
            foreach (var round in Enumerable.Range(0, 15).Concat(new[] { 49, 99 }))
            {
                Call("LoadRound", round);
                Field<Text>("feedbackText").text = "Makes 111.";
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (var field in new[] { "roundText", "stickText", "feedbackText", "tutorialText" })
                {
                    var label = Field<Text>(field);
                    if (!label.gameObject.activeSelf) continue;
                    var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                    settings.scaleFactor = 1f;
                    using var generator = new TextGenerator();
                    generator.Populate(label.text, settings);
                    Assert.That(generator.fontSizeUsedForBestFit * (320f / 720f),
                        Is.GreaterThanOrEqualTo(field == "feedbackText" ? 14f : 12f), $"Round {round + 1}: {field}");
                }
            }
        }

        [UnityTest]
        public IEnumerator FooterCommandsKeepTouchSizeAndReadableTextOnSmallPhone()
        {
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = new Vector2(720, 1278);
            Call("UpdateStageLayout");
            Call("LoadRound", 49);
            yield return null;
            Canvas.ForceUpdateCanvases();
            const float cssScale = 320f / 720f;
            var footer = Field<RectTransform>("stageRoot").Find("Footer");
            foreach (var button in footer.GetComponentsInChildren<Button>())
            {
                var bounds = BoundsIn((RectTransform)button.transform, safe);
                Assert.That(bounds.height * cssScale, Is.GreaterThanOrEqualTo(44f), button.name);
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(safe.rect.yMin));
                var label = button.GetComponentInChildren<Text>();
                using var generator = new TextGenerator();
                var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                settings.scaleFactor = 1f;
                generator.Populate(label.text, settings);
                Assert.That(generator.fontSizeUsedForBestFit * cssScale, Is.GreaterThanOrEqualTo(14f), label.text);
            }
        }

        [UnityTest]
        public IEnumerator BankPickupUsesItsCellInsteadOfOnlyTheThinBody()
        {
            Call("RefreshUi");
            yield return null;
            Canvas.ForceUpdateCanvases();
            var view = Field<List<RectTransform>>("bankStickViews")[0];
            var margin = view.TransformPoint(new Vector3(view.rect.width * 0.35f, 0f, 0f));
            var pointer = Pointer(1, RectTransformUtility.WorldToScreenPoint(null, margin));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(handler, Is.SameAs(view.gameObject), "A near miss within the pickup cell should still select its stick.");
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Bank[0], Is.EqualTo(StickPose.CenterSlash));
        }

        [UnityTest]
        public IEnumerator SingleStickRotationMatchesInsideAndOutsideTheBoard()
        {
            Place(0);
            for (var turn = 0; turn < 4; turn++)
            {
                Call("TapBankStick", 0);
                Call("CycleStickPose", 0, 0);
                Assert.That(Slots[0][0], Is.EqualTo(Bank[0]), "A tap must turn the same stick the same way in both locations.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ValidWrongResultShowsItsCalculatedValueWithoutAdvancing()
        {
            Call("LoadRound", 29);
            Call("FillCurrentRoundWithSample");
            Slots[7][0] = StickPose.CenterHorizontal;
            Call("UpdateRecognizedSymbol", 7);
            Call("RefreshUi");
            Call("CheckCurrent");
            Assert.That(Field<Text>("feedbackText").text, Is.EqualTo("Makes 0."));
            Assert.That(Field<int>("roundIndex"), Is.EqualTo(29));
            Assert.That(Field<bool>("isAdvancing"), Is.False);
            Assert.That(Field<int>("currentRoundFailureCount"), Is.EqualTo(1));
            yield return null;
        }

        [Test]
        public void ValidWrongResultFeedbackReusesExactReasonText()
        {
            Assert.That(StaticCall("CalculatedValueFailureText",
                    new EquationResult(true, 0d, string.Empty), "Result is 0."),
                Is.EqualTo("Makes 0."));
            Assert.That(StaticCall("CalculatedValueFailureText",
                    new EquationResult(true, 0d, string.Empty), "Result is 1/111111111."),
                Is.EqualTo("Makes 1/111111111."));
            Assert.That(StaticCall("CalculatedValueFailureText",
                    new EquationResult(true, 0.25d, string.Empty), string.Empty),
                Is.EqualTo("Makes 0.25."));
        }

        [UnityTest]
        public IEnumerator UnequalSidesShowBothValuesWithoutAdvancing()
        {
            Call("LoadRound", 58);
            var poses = new[] {
                new[] { StickPose.LeftVertical, StickPose.RightVertical },
                new[] { StickPose.CenterSlash },
                new[] { StickPose.CenterVertical },
                new[] { StickPose.TopHorizontal, StickPose.BottomHorizontal },
                new[] { StickPose.LeftVertical, StickPose.RightVertical },
                new[] { StickPose.CenterSlash },
                new[] { StickPose.LeftVertical, StickPose.RightVertical }
            };
            Bank.Clear();
            for (var i = 0; i < poses.Length; i++)
            {
                Slots[i].AddRange(poses[i]);
                Call("UpdateRecognizedSymbol", i);
            }
            Call("RefreshUi");
            Call("CheckCurrent");
            Assert.That(Field<Text>("feedbackText").text, Is.EqualTo("11 is not 1."));
            Assert.That(Field<int>("roundIndex"), Is.EqualTo(58));
            Assert.That(Field<bool>("isAdvancing"), Is.False);
            Assert.That(Field<int>("currentRoundFailureCount"), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(ProgressKeys[0], 0), Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BankPickupTargetsStayPutAfterRemovalAndReturn()
        {
            foreach (var roundIndex in new[] { 8, 12 })
            {
                Call("LoadRound", roundIndex);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var views = Field<List<RectTransform>>("bankStickViews");
                var positions = views.Select(v => v.anchoredPosition).ToArray();
                var scale = views[0].localScale;
                var pointer = Pointer(1, SlotPosition(0));
                Call("BeginStickDrag", pointer, SourceGroup(), -1, 1);
                Call("EndStickDrag", pointer);
                for (var i = 0; i < Bank.Count; i++)
                {
                    Assert.That(views[i].anchoredPosition, Is.EqualTo(positions[i < 1 ? i : i + 1]),
                        "Remaining pickup targets must not move after a neighbour is used.");
                    Assert.That(views[i].localScale, Is.EqualTo(scale));
                }
                var returning = Pointer(1, new Vector2(-100, -100));
                Call("BeginStickDrag", returning, SourceGroup(), 0, 0);
                Call("EndStickDrag", returning);
                CollectionAssert.AreEquivalent(positions, views.Select(v => v.anchoredPosition).ToArray());
                Assert.That(views[Bank.Count - 1].anchoredPosition, Is.EqualTo(positions[1]));
                Call("ResetRound");
                CollectionAssert.AreEqual(positions, views.Select(v => v.anchoredPosition).ToArray());
            }
        }

        [UnityTest]
        public IEnumerator FullSlotRejectsPlacedStickWithoutChangingEitherSlot()
        {
            Place(0); Place(0); Place(0); Place(1);
            var original = Slots.Select(s => s.ToArray()).ToArray();
            var bankCount = Bank.Count;
            var pointer = Pointer(1, SlotPosition(0));
            Call("BeginStickDrag", pointer, SourceGroup(), 1, 0);
            Call("EndStickDrag", pointer);
            Assert.That(Bank.Count, Is.EqualTo(bankCount));
            for (var i = 0; i < original.Length; i++) CollectionAssert.AreEqual(original[i], Slots[i]);
            Assert.That(Field<Text>("feedbackText").text, Is.EqualTo("Box fits 3."));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullSlotRejectsBankStickAndKeepsCapacityFeedback()
        {
            Place(0); Place(0); Place(0);
            var bankCount = Bank.Count;
            var pointer = Pointer(1, SlotPosition(0));
            Call("BeginStickDrag", pointer, SourceGroup(), -1, 0);
            Call("EndStickDrag", pointer);
            Assert.That(Bank.Count, Is.EqualTo(bankCount));
            Assert.That(Slots[0].Count, Is.EqualTo(3));
            Assert.That(Field<Text>("feedbackText").text, Is.EqualTo("Box fits 3."));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DroppingOutsideReturnsExactlyOneStick()
        {
            Place(0);
            var count = Bank.Count;
            var pointer = Pointer(1, new Vector2(-100, -100));
            Call("BeginStickDrag", pointer, SourceGroup(), 0, 0);
            Call("EndStickDrag", pointer);
            Assert.That(Slots[0], Is.Empty);
            Assert.That(Bank.Count, Is.EqualTo(count + 1));
            Assert.That(Field<RectTransform>("dragGhost"), Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SecondPointerCannotReplaceOrFinishActiveDrag()
        {
            var first = Pointer(1, SlotPosition(0));
            var second = Pointer(2, SlotPosition(1));
            Call("BeginStickDrag", first, SourceGroup(), -1, 0);
            var ghost = Field<RectTransform>("dragGhost");
            Call("BeginStickDrag", second, SourceGroup(), -1, 1);
            Call("EndStickDrag", second);
            Assert.That(Field<RectTransform>("dragGhost"), Is.SameAs(ghost));
            Assert.That(Slots[1], Is.Empty);
            Call("EndStickDrag", first);
            Assert.That(Slots[0].Count, Is.EqualTo(1));
            Assert.That(Bank.Count, Is.EqualTo(4));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancelledDragRestoresFeedbackAndPreservesTheBoard()
        {
            Place(0);
            foreach (var callback in new[] { "OnApplicationFocus", "OnApplicationPause", "OnDisable" })
            {
                var feedback = Field<Text>("feedbackText");
                feedback.text = "Makes 1111.";
                feedback.color = Color.red;
                var pointer = Pointer(1, SlotPosition(1));
                var group = SourceGroup();
                Call("BeginStickDrag", pointer, group, 0, 0);
                if (callback == "OnDisable") Call(callback);
                else Call(callback, callback == "OnApplicationPause");
                Call("EndStickDrag", pointer);
                Assert.That(feedback.text, Is.EqualTo("Makes 1111."), callback);
                Assert.That(feedback.color, Is.EqualTo(Color.red), callback);
                Assert.That(Slots[0].Count, Is.EqualTo(1));
                Assert.That(Slots[1], Is.Empty);
                Assert.That(Bank.Count, Is.EqualTo(4));
                Assert.That(group.alpha, Is.EqualTo(1f));
                Assert.That(Field<RectTransform>("dragGhost"), Is.Null);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SecondaryPressDuringDragCannotBecomeALateRotation()
        {
            var view = Field<List<RectTransform>>("bankStickViews")[0].gameObject;
            var first = Pointer(1, SlotPosition(0));
            var second = Pointer(2, SlotPosition(0));
            second.eligibleForClick = true;
            ExecuteEvents.Execute(view, first, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(view, first, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(view, first, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(view, second, ExecuteEvents.pointerDownHandler);
            Call("OnApplicationFocus", false);
            // The second finger is released after the owning drag was cancelled.
            if (second.eligibleForClick)
                ExecuteEvents.Execute(view, second, ExecuteEvents.pointerClickHandler);
            Assert.That(Bank[0], Is.EqualTo(StickPose.CenterVertical));
            Assert.That(second.eligibleForClick, Is.False);
            Assert.That(Slots.All(s => s.Count == 0), Is.True);
            Assert.That(Bank.Count, Is.EqualTo(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingTapDoesNotRotateAfterPause()
        {
            var view = Field<List<RectTransform>>("bankStickViews")[0].gameObject;
            var pointer = Pointer(1, Vector2.zero);
            pointer.eligibleForClick = true;
            ExecuteEvents.Execute(view, pointer, ExecuteEvents.pointerDownHandler);
            Call("OnApplicationPause", true);
            ExecuteEvents.Execute(view, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Bank[0], Is.EqualTo(StickPose.CenterVertical));
            Assert.That(pointer.eligibleForClick, Is.False);
            Call("OnApplicationPause", false);
            pointer.eligibleForClick = true;
            ExecuteEvents.Execute(view, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(view, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Bank[0], Is.EqualTo(StickPose.CenterSlash), "A new press after resuming must still rotate.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewPressAfterResumeDoesNotReviveAnOlderPointersClick()
        {
            var view = Field<List<RectTransform>>("bankStickViews")[0].gameObject;
            var interrupted = Pointer(1, Vector2.zero);
            interrupted.eligibleForClick = true;
            ExecuteEvents.Execute(view, interrupted, ExecuteEvents.pointerDownHandler);
            Call("OnApplicationPause", true);
            Call("OnApplicationPause", false);
            var fresh = Pointer(2, Vector2.zero);
            fresh.eligibleForClick = true;
            ExecuteEvents.Execute(view, fresh, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(view, interrupted, ExecuteEvents.pointerClickHandler);
            Assert.That(Bank[0], Is.EqualTo(StickPose.CenterVertical), "A new finger must not revive the interrupted finger's click.");
            Assert.That(interrupted.eligibleForClick, Is.False);
            ExecuteEvents.Execute(view, interrupted, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(view, interrupted, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(view, interrupted, ExecuteEvents.endDragHandler);
            Assert.That(Field<RectTransform>("dragGhost"), Is.Null);
            ExecuteEvents.Execute(view, fresh, ExecuteEvents.pointerClickHandler);
            Assert.That(Bank[0], Is.EqualTo(StickPose.CenterSlash), "The fresh finger must still rotate once.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingAnyStickFromTripleOneKeepsElevenRecognized()
        {
            for (var removed = 0; removed < 3; removed++)
            {
                Call("LoadRound", 7);
                Call("FillCurrentRoundWithSample");
                Call("RefreshUi");
                Call("RemoveStickFromSlot", 0, removed);
                Call("RefreshUi");
                Assert.That(Field<string[]>("slotSymbols")[0], Is.EqualTo("11"),
                    $"Removing stick {removed} from 111 should leave 11.");
                Assert.That(Slots[0], Is.EqualTo(new[] { StickPose.LeftVertical, StickPose.RightVertical }));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DraggingEachTripleStickPreservesCountsAndRemainingEleven()
        {
            foreach (var destination in new[] { -1, 1 })
            {
                for (var removed = 0; removed < 3; removed++)
                {
                    Call("LoadRound", 8);
                    Place(0);
                    Place(0);
                    Place(0);
                    Call("RefreshUi");
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var total = Bank.Count + Slots.Sum(slot => slot.Count);
                    var bankBefore = Bank.Count;
                    Assert.That(Field<string[]>("slotSymbols")[0], Is.EqualTo("111"));
                    var pointer = Pointer(1, destination < 0 ? new Vector2(-100, -100) : SlotPosition(destination));
                    var group = SourceGroup();
                    Call("BeginStickDrag", pointer, group, 0, removed);
                    Assert.That(Field<RectTransform>("dragGhost"), Is.Not.Null);
                    Call("MoveStickDrag", pointer);
                    Call("EndStickDrag", pointer);
                    var context = $"Stick {removed}, destination {destination}";
                    Assert.That(Field<string[]>("slotSymbols")[0], Is.EqualTo("11"), context);
                    Assert.That(Slots[0], Is.EqualTo(new[] { StickPose.LeftVertical, StickPose.RightVertical }), context);
                    Assert.That(Bank.Count + Slots.Sum(slot => slot.Count), Is.EqualTo(total), context);
                    Assert.That(Bank.Count, Is.EqualTo(bankBefore + (destination < 0 ? 1 : 0)), context);
                    if (destination >= 0)
                    {
                        Assert.That(Slots[destination].Count, Is.EqualTo(1), context);
                        Assert.That(Field<string[]>("slotSymbols")[destination], Is.EqualTo("1"), context);
                    }
                    Assert.That(Field<RectTransform>("dragGhost"), Is.Null, context);
                    Assert.That(group.alpha, Is.EqualTo(1f), context);
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator RepeatedVisualRefreshDoesNotRetainDestroyedFaces()
        {
            var faces = (IList)Field<object>("friendFaces");
            var baselineCount = faces.Count;
            for (var i = 0; i < 40; i++)
            {
                Call("TapBankStick", 0);
                if (i % 5 == 0) Call("ResetRound");
                yield return null;
            }
            var liveCount = faces.Cast<object>().Count(face =>
                (RectTransform)face.GetType().GetProperty("Root").GetValue(face) != null);
            Debug.Log($"Face registry after repeated refresh: {faces.Count} entries, {liveCount} live roots, baseline {baselineCount}.");
            Assert.That(faces.Count, Is.EqualTo(liveCount), "Destroyed faces must not remain in the animation/blink registry.");
            Assert.That(faces.Count, Is.EqualTo(5), "Only the current five bank faces should remain.");

            Place(0);
            Call("RefreshUi");
            for (var i = 0; i < 20; i++)
            {
                Call("TapDroppedStick", 0, 0);
                yield return null;
                Assert.That(faces.Cast<object>().All(face =>
                    (RectTransform)face.GetType().GetProperty("Root").GetValue(face) != null), Is.True);
                Assert.That(faces.Count, Is.LessThanOrEqualTo(10));
            }

            foreach (var round in new[] { 0, 1, 2, 3, 4, 5, 7, 8, 17, 59 })
            {
                Call("LoadRound", round);
                Call("FillCurrentRoundWithSample");
                Call("RefreshUi");
                yield return null;
                var expected = faces.Count;
                var equation = Field<RectTransform>("equationRow");
                var mouthCount = equation.GetComponentsInChildren<Transform>().Count(t => t.name == "Mouth");
                Assert.That(mouthCount, Is.GreaterThan(0));
                for (var refresh = 0; refresh < 5; refresh++)
                {
                    Call("RefreshUi");
                    yield return null;
                    var roots = faces.Cast<object>().Select(face =>
                        (RectTransform)face.GetType().GetProperty("Root").GetValue(face)).ToArray();
                    Assert.That(roots.Length, Is.EqualTo(expected), $"Round {round + 1}: face count changed");
                    Assert.That(roots.All(root => root != null && root.gameObject.activeInHierarchy), Is.True);
                    Assert.That(roots.Distinct().Count(), Is.EqualTo(expected));
                    Assert.That(equation.GetComponentsInChildren<Transform>().Count(t => t.name == "Mouth"),
                        Is.EqualTo(mouthCount), "Rebuilding must preserve faces, including non-animated placed faces.");
                }
            }
        }

        [UnityTest]
        public IEnumerator RepeatedInputAndResizeKeepLabelsAndSettleTheFontAtlas()
        {
            var gameFont = Field<Font>("font");
            var safe = Field<RectTransform>("safeRoot");
            foreach (var behaviour in safe.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            var rounds = new[] { 7, 29, 49, 74, 89, 99 };
            var rebuilds = 0;
            Action<Font> observe = rebuilt => { if (rebuilt == gameFont) rebuilds++; };
            Font.textureRebuilt += observe;
            try
            {
                for (var cycle = 0; cycle < 60; cycle++)
                {
                    var round = rounds[cycle % rounds.Length];
                    Call("LoadRound", round);
                    Call("ResetRound");
                    Place(0);
                    Call("TapDroppedStick", 0, 0);
                    Call("RefreshUi");
                    var pose = Slots[0][0];
                    var pointer = Pointer(1, SlotPosition(0));
                    Call("BeginStickDrag", pointer, SourceGroup(), 0, 0);
                    safe.sizeDelta = new Vector2(cycle % 2 == 0 ? 390f : 720f, 1280f);
                    Call("UpdateStageLayout");
                    Call("EndStickDrag", pointer);
                    Assert.That(Slots[0], Is.EqualTo(new[] { pose }), $"Cycle {cycle}: cancelled resize changed placement");
                    Assert.That(Field<RectTransform>("dragGhost"), Is.Null);
                    Call("CheckCurrent");
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var target = Field<Text>("targetText");
                    var expectsTarget = !OnePlusOneMinusOneRules.GoalModeRounds[round].SampleSolution.Contains("=");
                    Assert.That(target.gameObject.activeInHierarchy, Is.EqualTo(expectsTarget));
                    if (expectsTarget)
                    {
                        Assert.That(target.cachedTextGenerator.lineCount, Is.EqualTo(1));
                        Assert.That(target.cachedTextGenerator.vertexCount, Is.GreaterThan(0));
                    }
                    var views = (IList)Field<object>("slotViews");
                    var recognition = (Text)views[0].GetType().GetProperty("RecognitionText").GetValue(views[0]);
                    Assert.That(recognition.text, Is.EqualTo("/"));
                    Assert.That(recognition.cachedTextGenerator.vertexCount, Is.GreaterThan(0));
                }
                for (var i = 0; i < 20; i++) yield return null;
                var settledRebuilds = rebuilds;
                for (var i = 0; i < 120; i++) yield return null;
                Assert.That(rebuilds, Is.EqualTo(settledRebuilds), "Idle text must stop rebuilding after the mixed-input run.");
                Assert.That(Field<bool>("pendingFontMeshRefresh"), Is.False);
                Debug.Log($"Mixed-input atlas observation: 60 cycles, {rebuilds} rebuild events, none in final 120 idle frames.");
            }
            finally
            {
                Font.textureRebuilt -= observe;
            }
        }

        [UnityTest]
        public IEnumerator FilledBoardCanBeCheckedAfterSettlingDraggedStick()
        {
            Call("FillCurrentRoundWithSample");
            Call("RefreshUi");
            var pointer = Pointer(1, SlotPosition(0));
            Call("BeginStickDrag", pointer, SourceGroup(), 0, 0);
            Assert.That(Field<Button>("checkButton").interactable, Is.False);
            Call("EndStickDrag", pointer);
            Assert.That(Field<Button>("checkButton").interactable, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResetDuringDragCancelsPendingDrop()
        {
            var pointer = Pointer(1, SlotPosition(0));
            var group = SourceGroup();
            Call("BeginStickDrag", pointer, group, -1, 0);
            Call("ResetRound");
            Call("EndStickDrag", pointer);
            Assert.That(Slots.All(s => s.Count == 0), Is.True);
            Assert.That(Bank.Count, Is.EqualTo(5));
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(Field<RectTransform>("dragGhost"), Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LosingFocusCancelsDragWithoutMovingStick()
        {
            Place(0);
            var pointer = Pointer(1, new Vector2(-100, -100));
            var group = SourceGroup();
            Call("BeginStickDrag", pointer, group, 0, 0);
            controller.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            Call("EndStickDrag", pointer);
            Assert.That(Slots[0].Count, Is.EqualTo(1));
            Assert.That(Bank.Count, Is.EqualTo(4));
            Assert.That(group.alpha, Is.EqualTo(1f));
            yield return null;
        }

        private List<StickPose>[] Slots => Field<List<StickPose>[]>("slotStickPoses");
        private List<StickPose> Bank => Field<List<StickPose>>("bankStickPoses");
        private void Place(int slot)
        {
            Assert.That(Call("TryAddStickToSlot", slot, true, StickPose.CenterVertical), Is.True);
            Call("RemoveBankStick", 0);
        }
        private CanvasGroup SourceGroup() => new GameObject("Drag source").AddComponent<CanvasGroup>();
        private Vector2 SlotPosition(int slot)
        {
            var views = (IList)Field<object>("slotViews");
            var root = (RectTransform)views[slot].GetType().GetProperty("Root").GetValue(views[slot]);
            return RectTransformUtility.WorldToScreenPoint(null, root.position);
        }
        private PointerEventData Pointer(int id, Vector2 position) => new PointerEventData(EventSystem.current)
        {
            pointerId = id, position = position, button = PointerEventData.InputButton.Left
        };
        private T Field<T>(string name) => (T)typeof(OnePlusOneMinusOneController)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
        private object Call(string name, params object[] args)
        {
            var method = typeof(OnePlusOneMinusOneController).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == name && m.GetParameters().Length == args.Length);
            return method.Invoke(controller, args);
        }
        private static object StaticCall(string name, params object[] args)
        {
            var method = typeof(OnePlusOneMinusOneController).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(m => m.Name == name && m.GetParameters().Length == args.Length);
            return method.Invoke(null, args);
        }
    }
}
