using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UITTimeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace UIMotionToolkit.Editor
{
    /// <summary>Easing of a motion clip (weight curve of its ease in / ease out).</summary>
    public enum MotionEase
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InCubic,
        OutCubic,
        InOutCubic,
        OutQuint,
        OutExpo,
        InExpo,
        /// <summary>Overshoots the target then settles (springy entrance).</summary>
        OutBack,
        /// <summary>Pulls back first then goes (wind-up exit).</summary>
        InBack
    }

    /// <summary>
    /// Writes motion timelines from code (screens are generated, re-run to retune; the result stays
    /// editable in the Timeline window). One <see cref="UIMotionTrack"/> per selector and property group;
    /// helper names follow the motion: <c>FadeIn</c> / <c>MoveFrom</c> / <c>ScaleFrom</c> end at the neutral
    /// (USS) state, <c>FadeOut</c> / <c>MoveTo</c> / <c>ScaleTo</c> leave it, <c>*Pulse</c> go and come back.
    /// Times in seconds; an "entering" clip holds its offset before <c>at</c>. Each property of a selector
    /// has its own track; keep the clips of one track from overlapping (Timeline would turn the overlap
    /// into a blend): use a labelled <see cref="MotionAsset.Track"/> for a second, overlapping clip.
    /// </summary>
    public sealed class MotionAsset
    {
        private readonly Dictionary<string, UIMotionTrack> _tracks = new();

        private MotionAsset(TimelineAsset timeline) => Timeline = timeline;

        public TimelineAsset Timeline { get; }

        /// <summary>Empties the timeline at <paramref name="path"/> (GUID kept) or creates it.</summary>
        public static MotionAsset Begin(string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, path);
            }
            else
            {
                foreach (TrackAsset track in timeline.GetRootTracks().ToList())
                    timeline.DeleteTrack(track);
            }
            return new MotionAsset(timeline);
        }

        /// <summary>
        /// Elements matching <paramref name="selector"/>: each property gets its own track ("#panel | fade",
        /// "#panel | move"…), all tracks of an element add up. <paramref name="label"/> makes separate
        /// tracks (e.g. a second scale clip overlapping the first one).
        /// </summary>
        public MotionTrackBuilder Track(string selector, float stagger = 0f, bool staggerReverse = false, string label = null) =>
            new(this, selector, stagger, staggerReverse, label);

        internal UIMotionTrack TrackFor(string selector, string property, float stagger, bool staggerReverse, string label)
        {
            string key = selector + " | " + property + (string.IsNullOrEmpty(label) ? string.Empty : " " + label);
            if (_tracks.TryGetValue(key, out UIMotionTrack existing))
                return existing;
            var track = Timeline.CreateTrack<UIMotionTrack>(null, key);
            track.Stagger = stagger;
            track.StaggerReverse = staggerReverse;
            _tracks[key] = track;
            return track;
        }

        public TimelineAsset Save()
        {
            EditorUtility.SetDirty(Timeline);
            AssetDatabase.SaveAssets();
            return Timeline;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }

    public sealed class MotionTrackBuilder
    {
        private readonly MotionAsset _asset;
        private readonly string _selector;
        private readonly float _stagger;
        private readonly bool _staggerReverse;
        private readonly string _label;

        internal MotionTrackBuilder(MotionAsset asset, string selector, float stagger, bool staggerReverse, string label)
        {
            _asset = asset;
            _selector = selector;
            _stagger = stagger;
            _staggerReverse = staggerReverse;
            _label = label;
        }

        private UIMotionTrack TrackFor(string property) => _asset.TrackFor(_selector, property, _stagger, _staggerReverse, _label);

        private static string PropertyOf<T>() =>
            typeof(T) == typeof(UITOpacityClip) ? "fade"
            : typeof(T) == typeof(UITPositionClip) ? "move"
            : typeof(T) == typeof(UITScaleClip) ? "scale"
            : typeof(T) == typeof(UITRotationClip) ? "rotate"
            : typeof(T) == typeof(UIMotionColorClip) ? "color"
            : typeof(T) == typeof(UIMotionShakeClip) ? "shake"
            : typeof(T) == typeof(UITClassClip) ? "class"
            : typeof(T) == typeof(UITDisplayClip) ? "display"
            : typeof(T) == typeof(UITVisibilityClip) ? "visibility"
            : typeof(T).Name;

        // ------------------------------------------------------------------ opacity

        public MotionTrackBuilder FadeIn(double at, double duration, MotionEase ease = MotionEase.OutCubic, float from = 0f) =>
            Enter<UITOpacityClip>(at, duration, ease, c => c._template.Opacity = from - 1f, "fade in");

        public MotionTrackBuilder FadeOut(double at, double duration, MotionEase ease = MotionEase.InOutSine, float to = 0f) =>
            Exit<UITOpacityClip>(at, duration, ease, c => c._template.Opacity = to - 1f, "fade out");

        /// <summary>Dips the opacity to <paramref name="to"/> and back (breathing, blink).</summary>
        public MotionTrackBuilder FadePulse(double at, double duration, float to, MotionEase ease = MotionEase.InOutSine, double inShare = 0.5) =>
            Pulse<UITOpacityClip>(at, duration, ease, inShare, c => c._template.Opacity = to - 1f, "fade pulse");

        // ------------------------------------------------------------------ position (px)

        public MotionTrackBuilder MoveFrom(Vector2 offset, double at, double duration, MotionEase ease = MotionEase.OutCubic) =>
            Enter<UITPositionClip>(at, duration, ease, c => c._template.Position = offset, "move from");

        public MotionTrackBuilder MoveTo(Vector2 offset, double at, double duration, MotionEase ease = MotionEase.InCubic) =>
            Exit<UITPositionClip>(at, duration, ease, c => c._template.Position = offset, "move to");

        public MotionTrackBuilder MovePulse(Vector2 offset, double at, double duration, MotionEase ease = MotionEase.InOutSine, double inShare = 0.5) =>
            Pulse<UITPositionClip>(at, duration, ease, inShare, c => c._template.Position = offset, "move pulse");

        // ------------------------------------------------------------------ scale (absolute factors)

        public MotionTrackBuilder ScaleFrom(Vector2 scale, double at, double duration, MotionEase ease = MotionEase.OutCubic) =>
            Enter<UITScaleClip>(at, duration, ease, c => c._template.Scale = scale - Vector2.one, "scale from");

        public MotionTrackBuilder ScaleFrom(float scale, double at, double duration, MotionEase ease = MotionEase.OutCubic) =>
            ScaleFrom(new Vector2(scale, scale), at, duration, ease);

        public MotionTrackBuilder ScaleTo(Vector2 scale, double at, double duration, MotionEase ease = MotionEase.InCubic) =>
            Exit<UITScaleClip>(at, duration, ease, c => c._template.Scale = scale - Vector2.one, "scale to");

        public MotionTrackBuilder ScaleTo(float scale, double at, double duration, MotionEase ease = MotionEase.InCubic) =>
            ScaleTo(new Vector2(scale, scale), at, duration, ease);

        public MotionTrackBuilder ScalePulse(float scale, double at, double duration, MotionEase ease = MotionEase.InOutSine, double inShare = 0.5) =>
            Pulse<UITScaleClip>(at, duration, ease, inShare, c => c._template.Scale = new Vector2(scale - 1f, scale - 1f), "scale pulse");

        public MotionTrackBuilder ScalePulse(Vector2 scale, double at, double duration, MotionEase ease = MotionEase.InOutSine, double inShare = 0.5) =>
            Pulse<UITScaleClip>(at, duration, ease, inShare, c => c._template.Scale = scale - Vector2.one, "scale pulse");

        /// <summary>Springy appearance: grows from <paramref name="from"/>, overshoots a little and settles.</summary>
        public MotionTrackBuilder Pop(double at, double duration, float from = 0.6f) =>
            ScaleFrom(from, at, duration, MotionEase.OutBack);

        // ------------------------------------------------------------------ rotation (degrees)

        public MotionTrackBuilder RotateFrom(float degrees, double at, double duration, MotionEase ease = MotionEase.OutCubic) =>
            Enter<UITRotationClip>(at, duration, ease, c => c._template.Rotation = degrees, "rotate from");

        public MotionTrackBuilder RotateTo(float degrees, double at, double duration, MotionEase ease = MotionEase.InCubic) =>
            Exit<UITRotationClip>(at, duration, ease, c => c._template.Rotation = degrees, "rotate to");

        public MotionTrackBuilder RotatePulse(float degrees, double at, double duration, MotionEase ease = MotionEase.InOutSine, double inShare = 0.5) =>
            Pulse<UITRotationClip>(at, duration, ease, inShare, c => c._template.Rotation = degrees, "rotate pulse");

        // ------------------------------------------------------------------ color

        private UIMotionColorTarget _colorTarget;

        /// <summary>Color <paramref name="color"/> → the neutral one (<paramref name="neutral"/>, else the USS one read at bind).</summary>
        public MotionTrackBuilder ColorFrom(UIMotionColorTarget target, Color color, double at, double duration, MotionEase ease = MotionEase.OutCubic, Color? neutral = null)
        {
            _colorTarget = target;
            return Enter<UIMotionColorClip>(at, duration, ease, c => SetColor(c, target, color, neutral), "color from");
        }

        public MotionTrackBuilder ColorTo(UIMotionColorTarget target, Color color, double at, double duration, MotionEase ease = MotionEase.InOutSine, Color? neutral = null)
        {
            _colorTarget = target;
            return Exit<UIMotionColorClip>(at, duration, ease, c => SetColor(c, target, color, neutral), "color to");
        }

        public MotionTrackBuilder ColorPulse(UIMotionColorTarget target, Color color, double at, double duration, MotionEase ease = MotionEase.InOutSine, double inShare = 0.3, Color? neutral = null)
        {
            _colorTarget = target;
            return Pulse<UIMotionColorClip>(at, duration, ease, inShare, c => SetColor(c, target, color, neutral), "color pulse");
        }

        private static void SetColor(UIMotionColorClip clip, UIMotionColorTarget target, Color color, Color? neutral)
        {
            clip._template.Target = target;
            clip._template.Color = color;
            clip._template.OverrideNeutral = neutral.HasValue;
            clip._template.Neutral = neutral ?? Color.white;
        }

        // ------------------------------------------------------------------ shake / discrete

        /// <summary>Noise shake fading out over <paramref name="duration"/>.</summary>
        public MotionTrackBuilder Shake(double at, double duration, Vector2 amplitude, float rotation = 0f, float frequency = 22f, int seed = 1)
        {
            TimelineClip clip = Clip<UIMotionShakeClip>(at, duration, "shake", c =>
            {
                c._template.Amplitude = amplitude;
                c._template.RotationAmplitude = rotation;
                c._template.Frequency = frequency;
                c._template.Seed = seed;
            });
            clip.easeOutDuration = duration * 0.85;
            clip.mixOutCurve = Curve(MotionEase.OutCubic, true);
            return this;
        }

        public MotionTrackBuilder Class(string className, double at, double duration)
        {
            Clip<UITClassClip>(at, duration, className, c => c._template.ClassName = className);
            return this;
        }

        public MotionTrackBuilder Display(bool display, double at, double duration)
        {
            Clip<UITDisplayClip>(at, duration, display ? "display" : "no display", c => c._template.Display = display);
            return this;
        }

        public MotionTrackBuilder Visible(bool visible, double at, double duration)
        {
            Clip<UITVisibilityClip>(at, duration, visible ? "visible" : "hidden", c => c._template.Visible = visible);
            return this;
        }

        // ------------------------------------------------------------------ primitives

        /// <summary>Offset → neutral: weight 1 until <paramref name="at"/> then eased to 0.</summary>
        public MotionTrackBuilder Enter<T>(double at, double duration, MotionEase ease, Action<T> setup, string label) where T : UnityEngine.Playables.PlayableAsset
        {
            TimelineClip clip = Clip(at, duration, label, setup);
            clip.easeOutDuration = clip.duration;
            clip.mixOutCurve = Curve(ease, true);
            return this;
        }

        /// <summary>Neutral → offset: weight eased from 0 to 1, then held.</summary>
        public MotionTrackBuilder Exit<T>(double at, double duration, MotionEase ease, Action<T> setup, string label) where T : UnityEngine.Playables.PlayableAsset
        {
            TimelineClip clip = Clip(at, duration, label, setup);
            clip.easeInDuration = clip.duration;
            clip.mixInCurve = Curve(ease, false);
            return this;
        }

        /// <summary>Neutral → offset → neutral; <paramref name="inShare"/> of the time going there.</summary>
        public MotionTrackBuilder Pulse<T>(double at, double duration, MotionEase ease, double inShare, Action<T> setup, string label) where T : UnityEngine.Playables.PlayableAsset
        {
            TimelineClip clip = Clip(at, duration, label, setup);
            inShare = Math.Clamp(inShare, 0.01, 0.99);
            clip.easeInDuration = clip.duration * inShare;
            clip.easeOutDuration = clip.duration * (1.0 - inShare);
            clip.mixInCurve = Curve(ease, false);
            clip.mixOutCurve = Curve(ease, true);
            return this;
        }

        /// <summary>Constant value over [at, at + duration] (held outside, like every numeric clip).</summary>
        public MotionTrackBuilder Hold<T>(double at, double duration, Action<T> setup, string label = "hold") where T : UnityEngine.Playables.PlayableAsset
        {
            Clip(at, duration, label, setup);
            return this;
        }

        private TimelineClip Clip<T>(double at, double duration, string label, Action<T> setup) where T : UnityEngine.Playables.PlayableAsset
        {
            UIMotionTrack track = TrackFor(typeof(T) == typeof(UIMotionColorClip) ? "color " + _colorTarget : PropertyOf<T>());
            TimelineClip clip = track.CreateClip<T>();
            clip.start = Math.Max(0.0, at);
            clip.duration = Math.Max(1.0 / 120.0, duration);
            clip.displayName = label;
            setup((T)clip.asset);
            EditorUtility.SetDirty(clip.asset);
            return clip;
        }

        /// <summary>Weight curve of an ease: mix in goes 0 → 1, mix out 1 → 0, both shaped by <paramref name="ease"/> on progress.</summary>
        public static AnimationCurve Curve(MotionEase ease, bool mixOut)
        {
            const int samples = 16;
            var keys = new Keyframe[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                float x = i / (float)samples;
                float y = Evaluate(ease, x);
                keys[i] = new Keyframe(x, mixOut ? 1f - y : y);
            }
            var curve = new AnimationCurve(keys);
            for (int i = 0; i <= samples; i++)
            {
                float before = keys[Math.Max(0, i - 1)].value;
                float after = keys[Math.Min(samples, i + 1)].value;
                float span = (Math.Min(samples, i + 1) - Math.Max(0, i - 1)) / (float)samples;
                Keyframe k = keys[i];
                k.inTangent = k.outTangent = (after - before) / span;
                curve.MoveKey(i, k);
            }
            return curve;
        }

        public static float Evaluate(MotionEase ease, float x) => ease switch
        {
            MotionEase.Linear => x,
            MotionEase.InSine => 1f - Mathf.Cos(x * Mathf.PI * 0.5f),
            MotionEase.OutSine => Mathf.Sin(x * Mathf.PI * 0.5f),
            MotionEase.InOutSine => -(Mathf.Cos(Mathf.PI * x) - 1f) * 0.5f,
            MotionEase.InCubic => x * x * x,
            MotionEase.OutCubic => 1f - Mathf.Pow(1f - x, 3f),
            MotionEase.InOutCubic => x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f,
            MotionEase.OutQuint => 1f - Mathf.Pow(1f - x, 5f),
            MotionEase.OutExpo => x >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * x),
            MotionEase.InExpo => x <= 0f ? 0f : Mathf.Pow(2f, 10f * x - 10f),
            MotionEase.OutBack => 1f + 2.70158f * Mathf.Pow(x - 1f, 3f) + 1.70158f * Mathf.Pow(x - 1f, 2f),
            MotionEase.InBack => 2.70158f * x * x * x - 1.70158f * x * x,
            _ => x
        };
    }
}
