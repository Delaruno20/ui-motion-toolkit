using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// Plays motion timelines (<see cref="UIMotionTrack"/>) on UI Toolkit elements, from code, without a
    /// PlayableDirector: any root (UIDocument, PanelRenderer, a list item built at runtime), any number
    /// of instances of the same asset, forward or reversed, on unscaled time.
    /// <para>Playbacks live in <b>channels</b> (scope element + name): starting a motion in a busy channel
    /// replaces the previous one. Three ways to use it:</para>
    /// <list type="bullet">
    /// <item><see cref="Play(VisualElement,string,Action,string,bool)"/> — one-shot (a hit, a pulse, an ambient loop);</item>
    /// <item><see cref="Toggle"/> — reversible state (focus, selected): on plays the motion forward,
    /// off plays it backward from where it is, so quick on/off never pops;</item>
    /// <item><see cref="Show"/> / <see cref="Hide"/> — presence of a screen, panel or popup with the
    /// <c>is-hidden</c> class: shown at once then animated in; animated out then hidden.</item>
    /// </list>
    /// Motion ids are timeline asset names listed in the <see cref="UIMotionLibrary"/>. A missing motion
    /// is not an error for the UI: the state changes instantly.
    /// </summary>
    public static class UIMotion
    {
        public const string VisibilityChannel = "visibility";
        public const string HiddenClass = "is-hidden";
        private const float MaxDeltaTime = 0.1f;

        private static readonly List<UIMotionPlayback> s_active = new();
        private static readonly List<UIMotionPlayback> s_ticking = new();
        private static readonly Dictionary<(VisualElement, string), UIMotionPlayback> s_channels = new();
        private static readonly HashSet<string> s_warned = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, Dictionary<string, UIMotionPlayback>> s_states = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, object> s_hiding = new();
        private static UIMotionLibrary s_library;
        private static bool s_libraryLoaded;
#if UNITY_EDITOR
        private static bool s_tickerReady;
#endif

        /// <summary>The library motion ids resolve against (Resources/UI/UIMotionLibrary by default).</summary>
        public static UIMotionLibrary Library
        {
            get
            {
                if (!s_libraryLoaded || s_library == null)
                {
                    s_libraryLoaded = true;
                    s_library = Resources.Load<UIMotionLibrary>(UIMotionLibrary.ResourcePath);
                }
                return s_library;
            }
            set
            {
                s_library = value;
                s_libraryLoaded = value != null;
            }
        }

        /// <summary>When true nothing ticks by itself: call <see cref="Tick"/> (offscreen previews, tests).</summary>
        public static bool ManualTick { get; set; }

        public static int ActiveCount => s_active.Count;

        public static TimelineAsset Find(string motionId)
        {
            TimelineAsset motion = Library != null ? Library.Find(motionId) : null;
            if (motion == null && !string.IsNullOrEmpty(motionId) && s_warned.Add(motionId))
                Debug.LogWarning($"[UIMotionToolkit] No motion '{motionId}' in the UIMotionLibrary: the change is instant.");
            return motion;
        }

        public static bool Has(string motionId) => Library != null && Library.Find(motionId) != null;

        // ------------------------------------------------------------------ one-shot

        public static UIMotionPlayback Play(VisualElement scope, string motionId, Action onFinished = null,
            string channel = null, bool loop = false) =>
            Play(scope, Find(motionId), onFinished, channel ?? motionId, loop);

        public static UIMotionPlayback Play(VisualElement scope, TimelineAsset motion, Action onFinished = null,
            string channel = null, bool loop = false)
        {
            if (scope == null || motion == null)
            {
                onFinished?.Invoke();
                return null;
            }
            channel ??= motion.name;
            Stop(scope, channel);
            var playback = new UIMotionPlayback(CompiledMotion.Get(motion), scope, channel) { Loop = loop, Callback = onFinished };
            playback.PlayForward();
            return playback;
        }

        // ------------------------------------------------------------------ reversible states

        /// <summary>
        /// Drives a reversible state motion: <paramref name="on"/> plays it forward, off backward, each
        /// from the current time of the playback already in the channel. At rest (finished either way)
        /// nothing is inline: the USS state (pseudo-class, modifier) must match the motion's end (on)
        /// and start (off). <paramref name="instant"/> drops any playback and lets the USS apply.
        /// </summary>
        public static UIMotionPlayback Toggle(VisualElement scope, string motionId, bool on, Action onFinished = null,
            string channel = null, bool instant = false)
        {
            if (scope == null)
            {
                onFinished?.Invoke();
                return null;
            }
            channel ??= motionId;
            // State playbacks stay bound to their element between toggles: no re-resolving on every focus
            // change, and color clips keep the neutral read the first time.
            Dictionary<string, UIMotionPlayback> states = s_states.GetOrCreateValue(scope);
            states.TryGetValue(channel ?? string.Empty, out UIMotionPlayback current);
            if (instant)
            {
                if (current != null)
                {
                    current.Stop();
                    current.SetTimeSilently(on ? current.Duration : 0.0);
                }
                onFinished?.Invoke();
                return current;
            }
            TimelineAsset motion = Find(motionId);
            if (motion == null)
            {
                current?.Stop();
                onFinished?.Invoke();
                return null;
            }
            if (current == null || current.Motion != motion)
            {
                current?.Stop();
                current = new UIMotionPlayback(CompiledMotion.Get(motion), scope, channel);
                current.SetTimeSilently(on ? 0.0 : current.Duration);
                states[channel ?? string.Empty] = current;
            }
            else if (!current.IsPlaying && current.Time >= current.Duration == on)
            {
                // Already at rest in the asked state.
                onFinished?.Invoke();
                return current;
            }
            current.Callback = onFinished;
            if (on) current.PlayForward();
            else current.PlayReverse();
            return current;
        }

        // ------------------------------------------------------------------ presence

        /// <summary>
        /// Removes <paramref name="hiddenClass"/> and plays <paramref name="showMotionId"/> in the
        /// visibility channel (or resumes it forward if it was being reversed by <see cref="Hide"/>).
        /// </summary>
        public static UIMotionPlayback Show(VisualElement element, string showMotionId, Action onShown = null,
            string hiddenClass = HiddenClass, bool instant = false)
        {
            if (element == null)
                return null;
            s_hiding.Remove(element);
            element.RemoveFromClassList(hiddenClass);
            UIMotionPlayback current = Get(element, VisibilityChannel);
            TimelineAsset motion = instant ? null : Find(showMotionId);
            if (motion == null)
            {
                current?.Stop();
                onShown?.Invoke();
                return null;
            }
            if (current != null && current.Motion == motion)
            {
                current.Callback = onShown;
                current.PlayForward();
                return current;
            }
            return Play(element, motion, onShown, VisibilityChannel);
        }

        /// <summary>
        /// Plays <paramref name="hideMotionId"/> (or <paramref name="showMotionId"/> reversed when it is
        /// null) in the visibility channel, then adds <paramref name="hiddenClass"/>.
        /// </summary>
        public static UIMotionPlayback Hide(VisualElement element, string showMotionId, string hideMotionId = null,
            Action onHidden = null, string hiddenClass = HiddenClass, bool instant = false)
        {
            if (element == null)
                return null;
            UIMotionPlayback current = Get(element, VisibilityChannel);
            void Done()
            {
                s_hiding.Remove(element);
                element.AddToClassList(hiddenClass);
                onHidden?.Invoke();
            }

            s_hiding.Remove(element);
            s_hiding.Add(element, null);

            if (instant || (current == null && element.ClassListContains(hiddenClass)))
            {
                current?.Stop();
                Done();
                return null;
            }
            if (!string.IsNullOrEmpty(hideMotionId))
            {
                TimelineAsset hide = Find(hideMotionId);
                if (hide != null)
                {
                    if (current != null && current.Motion == hide && current.IsPlaying)
                    {
                        current.Callback = Done;
                        return current;
                    }
                    return Play(element, hide, Done, VisibilityChannel);
                }
            }
            TimelineAsset show = Find(showMotionId);
            if (show == null)
            {
                current?.Stop();
                Done();
                return null;
            }
            if (current == null || current.Motion != show)
            {
                current?.Stop();
                current = new UIMotionPlayback(CompiledMotion.Get(show), element, VisibilityChannel);
                current.Time = current.Duration;
            }
            current.Callback = Done;
            current.PlayReverse();
            return current;
        }

        /// <summary>Shown and not on its way out (<see cref="Hide"/> running).</summary>
        public static bool IsShown(VisualElement element, string hiddenClass = HiddenClass) =>
            element != null && !element.ClassListContains(hiddenClass) && !IsHiding(element);

        /// <summary>A <see cref="Hide"/> is playing on <paramref name="element"/> (still visible, leaving).</summary>
        public static bool IsHiding(VisualElement element) => element != null && s_hiding.TryGetValue(element, out _);

        // ------------------------------------------------------------------ queries / control

        public static UIMotionPlayback Get(VisualElement scope, string channel)
        {
            if (scope == null || channel == null)
                return null;
            return s_channels.TryGetValue((scope, channel), out UIMotionPlayback playback) ? playback : null;
        }

        /// <summary>True while a playback runs on <paramref name="scope"/> (in <paramref name="channel"/>, or any).</summary>
        public static bool IsPlaying(VisualElement scope, string channel = null)
        {
            if (channel != null)
                return Get(scope, channel) != null;
            foreach (UIMotionPlayback playback in s_active)
                if (playback.Scope == scope)
                    return true;
            return false;
        }

        /// <summary>Stops the playbacks of <paramref name="scope"/> (one channel, or all) without callbacks.</summary>
        public static void Stop(VisualElement scope, string channel = null, bool restore = true)
        {
            if (scope == null)
                return;
            if (channel != null)
            {
                Get(scope, channel)?.Stop(restore);
                return;
            }
            for (int i = s_active.Count - 1; i >= 0; i--)
                if (i < s_active.Count && s_active[i].Scope == scope)
                    s_active[i].Stop(restore);
        }

        /// <summary>Jumps the playbacks of <paramref name="scope"/> to their end and runs their callbacks.</summary>
        public static void Complete(VisualElement scope, string channel = null)
        {
            if (scope == null)
                return;
            if (channel != null)
            {
                Get(scope, channel)?.Complete();
                return;
            }
            for (int i = s_active.Count - 1; i >= 0; i--)
                if (i < s_active.Count && s_active[i].Scope == scope)
                    s_active[i].Complete();
        }

        /// <summary>Stops every playback whose scope is <paramref name="root"/> or inside it.</summary>
        public static void StopUnder(VisualElement root, bool restore = true)
        {
            for (int i = s_active.Count - 1; i >= 0; i--)
            {
                if (i >= s_active.Count)
                    continue;
                VisualElement scope = s_active[i].Scope;
                if (scope == root || root.Contains(scope))
                    s_active[i].Stop(restore);
            }
        }

        public static void StopAll(bool restore = true)
        {
            for (int i = s_active.Count - 1; i >= 0; i--)
                if (i < s_active.Count)
                    s_active[i].Stop(restore);
        }

        /// <summary>Advances every playback (called by the runner each frame with unscaled time).</summary>
        public static void Tick(float deltaTime)
        {
            if (s_active.Count == 0)
                return;
            deltaTime = Mathf.Min(deltaTime, MaxDeltaTime);
            s_ticking.Clear();
            s_ticking.AddRange(s_active);
            foreach (UIMotionPlayback playback in s_ticking)
                playback.Tick(deltaTime);
            s_ticking.Clear();
        }

        internal static void Register(UIMotionPlayback playback)
        {
            if (Get(playback.Scope, playback.Channel) is { } other && other != playback)
                other.Stop();
            s_channels[(playback.Scope, playback.Channel)] = playback;
            if (!s_active.Contains(playback))
                s_active.Add(playback);
            EnsureTicker();
        }

        internal static void Unregister(UIMotionPlayback playback)
        {
            s_active.Remove(playback);
            if (s_channels.TryGetValue((playback.Scope, playback.Channel), out UIMotionPlayback registered) && registered == playback)
                s_channels.Remove((playback.Scope, playback.Channel));
        }

        private static void EnsureTicker()
        {
            if (ManualTick)
                return;
            if (Application.isPlaying)
            {
                UIMotionRunner.Ensure();
                return;
            }
#if UNITY_EDITOR
            if (s_tickerReady)
                return;
            s_tickerReady = true;
            double last = UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.update += () =>
            {
                double now = UnityEditor.EditorApplication.timeSinceStartup;
                float dt = (float)(now - last);
                last = now;
                if (!Application.isPlaying && !ManualTick)
                    Tick(dt);
            };
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_active.Clear();
            s_channels.Clear();
            s_warned.Clear();
            s_library = null;
            s_libraryLoaded = false;
        }
    }
}
