using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using UIMotionToolkit.Editor;

namespace UIMotionToolkit.Tests
{
    /// <summary>Plays real motion timelines on detached elements, ticking by hand (no frames, no panel).</summary>
    public sealed class UIMotionPlaybackTests
    {
        private const string Folder = "Assets/__UIMotionToolkitTests__";
        private TimelineAsset _fade;
        private UIMotionLibrary _library;
        private VisualElement _element;
        private bool _manual;

        [OneTimeSetUp]
        public void Build()
        {
            // A 1 s motion: opacity 0 -> neutral on the scope, 0.5 px/s style offset irrelevant.
            MotionAsset m = MotionAsset.Begin(Folder + "/Test.Fade.asset");
            m.Track(":scope").FadeIn(0, 1.0, MotionEase.Linear);
            _fade = m.Save();
            _library = ScriptableObject.CreateInstance<UIMotionLibrary>();
            _library.SetMotions(new[] { _fade });
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(_library);
            AssetDatabase.DeleteAsset(Folder);
        }

        [SetUp]
        public void SetUp()
        {
            _manual = UIMotion.ManualTick;
            UIMotion.ManualTick = true;
            UIMotion.Library = _library;
            _element = new VisualElement();
        }

        [TearDown]
        public void TearDown()
        {
            UIMotion.StopAll();
            UIMotion.ManualTick = _manual;
            UIMotion.Library = null;
        }

        private static float Opacity(VisualElement e) => e.style.opacity.keyword == StyleKeyword.Undefined ? e.style.opacity.value : float.NaN;

        [Test]
        public void Play_WritesInlineStyleThenClearsItAtTheEnd()
        {
            bool finished = false;
            UIMotion.Play(_element, "Test.Fade", () => finished = true);
            Assert.IsTrue(UIMotion.IsPlaying(_element));
            Assert.AreEqual(0f, Opacity(_element), 0.001f);          // first frame = the "from" offset

            UIMotion.Tick(0.05f);
            UIMotion.Tick(0.05f);
            Assert.AreEqual(0.1f, Opacity(_element), 0.05f);

            for (int i = 0; i < 20; i++)
                UIMotion.Tick(0.05f);                                // ticks are clamped to 0.1 s
            Assert.IsTrue(finished);
            Assert.IsFalse(UIMotion.IsPlaying(_element));
            Assert.IsTrue(float.IsNaN(Opacity(_element)), "inline opacity must be cleared: the USS state is the rest state");
        }

        [Test]
        public void SameChannel_ReplacesTheRunningPlayback()
        {
            UIMotionPlayback first = UIMotion.Play(_element, "Test.Fade");
            UIMotionPlayback second = UIMotion.Play(_element, "Test.Fade");
            Assert.IsFalse(first.IsPlaying);
            Assert.IsTrue(second.IsPlaying);
            Assert.AreEqual(1, UIMotion.ActiveCount);
        }

        [Test]
        public void Toggle_ReversesFromWhereItIs()
        {
            UIMotionPlayback on = UIMotion.Toggle(_element, "Test.Fade", true);
            UIMotion.Tick(0.1f);
            UIMotion.Tick(0.1f);
            UIMotion.Tick(0.1f);
            double reached = on.Time;
            Assert.Greater(reached, 0.2);

            UIMotionPlayback off = UIMotion.Toggle(_element, "Test.Fade", false);
            Assert.AreSame(on, off, "a toggle keeps one playback per element and channel");
            Assert.IsTrue(off.Reversed);
            Assert.AreEqual(reached, off.Time, 1e-6, "no pop: it reverses from the current time");

            for (int i = 0; i < 20; i++)
                UIMotion.Tick(0.1f);
            Assert.AreEqual(0.0, off.Time, 1e-6);
        }

        [Test]
        public void Toggle_AlreadyAtRest_DoesNothing()
        {
            UIMotion.Toggle(_element, "Test.Fade", false);
            for (int i = 0; i < 20; i++) UIMotion.Tick(0.1f);
            int before = UIMotion.ActiveCount;
            UIMotion.Toggle(_element, "Test.Fade", false);
            Assert.AreEqual(before, UIMotion.ActiveCount);
        }

        [Test]
        public void ShowHide_DriveTheHiddenClass()
        {
            _element.AddToClassList(UIMotion.HiddenClass);
            UIMotion.Show(_element, "Test.Fade");
            Assert.IsFalse(_element.ClassListContains(UIMotion.HiddenClass), "Show reveals the element at once, then animates");
            Assert.IsTrue(UIMotion.IsShown(_element));

            bool hidden = false;
            UIMotion.Hide(_element, "Test.Fade", onHidden: () => hidden = true);   // reverse of the show motion
            Assert.IsTrue(UIMotion.IsHiding(_element));
            Assert.IsFalse(UIMotion.IsShown(_element));
            for (int i = 0; i < 30; i++) UIMotion.Tick(0.1f);
            Assert.IsTrue(hidden);
            Assert.IsTrue(_element.ClassListContains(UIMotion.HiddenClass));
        }

        [Test]
        public void MissingMotion_ChangesStateInstantly()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No motion 'Does.Not.Exist'"));
            bool shown = false;
            _element.AddToClassList(UIMotion.HiddenClass);
            UIMotion.Show(_element, "Does.Not.Exist", () => shown = true);
            Assert.IsTrue(shown);
            Assert.IsFalse(_element.ClassListContains(UIMotion.HiddenClass));
        }

        [Test]
        public void Complete_JumpsToTheEndAndRunsCallbacks()
        {
            bool finished = false;
            UIMotion.Play(_element, "Test.Fade", () => finished = true);
            UIMotion.Complete(_element);
            Assert.IsTrue(finished);
            Assert.IsFalse(UIMotion.IsPlaying(_element));
        }

        [Test]
        public void Loop_NeverFinishes()
        {
            bool finished = false;
            UIMotion.Play(_element, "Test.Fade", () => finished = true, loop: true);
            for (int i = 0; i < 40; i++) UIMotion.Tick(0.1f);
            Assert.IsFalse(finished);
            Assert.IsTrue(UIMotion.IsPlaying(_element));
        }

        [Test]
        public void Stagger_ExtendsTheDuration()
        {
            MotionAsset m = MotionAsset.Begin(Folder + "/Test.Stagger.asset");
            m.Track(".row", stagger: 0.2f).FadeIn(0, 0.5, MotionEase.Linear);
            TimelineAsset staggered = m.Save();

            var list = new VisualElement();
            for (int i = 0; i < 4; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("row");
                list.Add(row);
            }
            UIMotionPlayback playback = UIMotion.Play(list, staggered);
            Assert.AreEqual(0.5 + 3 * 0.2, playback.Duration, 1e-4);
        }
    }
}
