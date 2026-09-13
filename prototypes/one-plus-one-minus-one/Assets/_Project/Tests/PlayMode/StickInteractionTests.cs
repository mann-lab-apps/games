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
            var soundBounds = BoundsIn(Field<Toggle>("soundToggle").GetComponent<RectTransform>(), safe);
            Assert.That(soundBounds.height * cssScale, Is.GreaterThanOrEqualTo(44f));
            Assert.That(soundBounds.Overlaps(BoundsIn((RectTransform)panel.Find("Round Select Header/Round Select Title"), safe)), Is.False);
            for (var page = 0; page < 9; page++)
            {
                if (page > 0) Call("ChangeRoundSelectPage", 1);
                yield return null;
                Canvas.ForceUpdateCanvases();
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
            Call("LoadRound", 38);
            Call("FillCurrentRoundWithSample");
            Call("RefreshUi");
            yield return null;
            Canvas.ForceUpdateCanvases();
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
            Assert.That(Field<Text>("targetText").text, Is.EqualTo("= 22"));
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
            target.canvasRenderer.Clear();
            Call("OnFontTextureRebuilt", target.font);
            Assert.That(Field<bool>("pendingFontMeshRefresh"), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertTargetGlyphs("Deferred atlas refresh");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Field<bool>("pendingFontMeshRefresh"), Is.False,
                "Stable text must not require rebuilding every frame");
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
    }
}
