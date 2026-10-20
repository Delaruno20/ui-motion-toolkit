using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// A compiled motion bound to the elements its tracks matched in one scope. Each frame every track
    /// adds its offsets into the state of its elements, then each element is written once: several
    /// tracks (one per property, or a move and a shake) combine on the same element.
    /// </summary>
    internal sealed class MotionEvaluator
    {
        private const int ColorTargets = 4;

        private sealed class ElementState
        {
            public VisualElement Element;
            public Channels Channels;
            public Vector2 Position;
            public Vector2 Scale;
            public float Rotation;
            public float Opacity;
            public int Display;
            public int Visible;
            public readonly Color[] ColorSum = new Color[ColorTargets];
            public readonly float[] ColorWeight = new float[ColorTargets];
            public readonly Color[] Neutral = new Color[ColorTargets];
            public readonly bool[] HasNeutralOverride = new bool[ColorTargets];
            public bool NeutralRead;
            public readonly HashSet<string> ActiveClasses = new();
            public readonly HashSet<string> AddedClasses = new();

            public void Reset()
            {
                Position = Vector2.zero;
                Scale = Vector2.one;
                Rotation = 0f;
                Opacity = 1f;
                Display = -1;
                Visible = -1;
                for (int c = 0; c < ColorTargets; c++)
                {
                    ColorSum[c] = Color.clear;
                    ColorWeight[c] = 0f;
                }
                ActiveClasses.Clear();
            }
        }

        private sealed class Binding
        {
            public MotionTrack Track;
            public ElementState[] Elements;
        }

        private readonly List<Binding> _bindings = new();
        private readonly List<ElementState> _states = new();

        public MotionEvaluator(CompiledMotion motion, VisualElement scope)
        {
            var byElement = new Dictionary<VisualElement, ElementState>();
            foreach (MotionTrack track in motion.Tracks)
            {
                List<VisualElement> matches = UIMotionSelector.Resolve(track.Selector, scope);
                if (matches.Count == 0)
                    continue;
                var binding = new Binding { Track = track, Elements = new ElementState[matches.Count] };
                for (int i = 0; i < matches.Count; i++)
                {
                    VisualElement e = matches[i];
                    if (!byElement.TryGetValue(e, out ElementState state))
                    {
                        state = new ElementState { Element = e };
                        byElement[e] = state;
                        _states.Add(state);
                    }
                    state.Channels |= track.Channels;
                    foreach (ClipGroup group in track.Groups)
                    {
                        if (group.Kind != ClipKind.Color || !group.HasNeutral)
                            continue;
                        state.HasNeutralOverride[(int)group.Target] = true;
                        state.Neutral[(int)group.Target] = group.Neutral;
                    }
                    binding.Elements[i] = state;
                    if (track.UsageHints)
                        AddUsageHints(e, track.Channels);
                }
                _bindings.Add(binding);
                StaggerSpan = Math.Max(StaggerSpan, track.Stagger * (matches.Count - 1));
            }
        }

        /// <summary>Longest stagger delay of the bound tracks (added to the motion duration).</summary>
        public double StaggerSpan { get; }

        public int ElementCount => _states.Count;

        public void Evaluate(double time)
        {
            foreach (ElementState state in _states)
                state.Reset();
            foreach (Binding binding in _bindings)
            {
                MotionTrack track = binding.Track;
                int count = binding.Elements.Length;
                for (int i = 0; i < count; i++)
                {
                    int order = track.StaggerReverse ? count - 1 - i : i;
                    double local = Math.Max(0.0, time - order * track.Stagger);
                    Accumulate(track, binding.Elements[i], local);
                }
            }
            foreach (ElementState state in _states)
                Write(state);
        }

        /// <summary>Clears every inline value written: the USS styles apply again.</summary>
        public void Restore()
        {
            foreach (ElementState state in _states)
            {
                VisualElement e = state.Element;
                Channels channels = state.Channels;
                if ((channels & Channels.Translate) != 0) e.style.translate = StyleKeyword.Null;
                if ((channels & Channels.Scale) != 0) e.style.scale = StyleKeyword.Null;
                if ((channels & Channels.Rotate) != 0) e.style.rotate = StyleKeyword.Null;
                if ((channels & Channels.Opacity) != 0) e.style.opacity = StyleKeyword.Null;
                if ((channels & Channels.Display) != 0) e.style.display = StyleKeyword.Null;
                if ((channels & Channels.Visibility) != 0) e.style.visibility = StyleKeyword.Null;
                if ((channels & Channels.BackgroundColor) != 0) e.style.backgroundColor = StyleKeyword.Null;
                if ((channels & Channels.ImageTint) != 0) e.style.unityBackgroundImageTintColor = StyleKeyword.Null;
                if ((channels & Channels.TextColor) != 0) e.style.color = StyleKeyword.Null;
                if ((channels & Channels.BorderColor) != 0)
                {
                    e.style.borderTopColor = StyleKeyword.Null;
                    e.style.borderRightColor = StyleKeyword.Null;
                    e.style.borderBottomColor = StyleKeyword.Null;
                    e.style.borderLeftColor = StyleKeyword.Null;
                }
                foreach (string className in state.AddedClasses)
                    e.RemoveFromClassList(className);
                state.AddedClasses.Clear();
            }
        }

        private static void AddUsageHints(VisualElement e, Channels channels)
        {
            UsageHints hints = UsageHints.None;
            if ((channels & (Channels.Translate | Channels.Scale | Channels.Rotate)) != 0)
                hints |= UsageHints.DynamicTransform;
            if ((channels & (Channels.Opacity | Channels.BackgroundColor | Channels.ImageTint | Channels.TextColor | Channels.BorderColor)) != 0)
                hints |= UsageHints.DynamicColor;
            if ((e.usageHints & hints) != hints)
                e.usageHints |= hints;
        }

        private static void Accumulate(MotionTrack track, ElementState s, double t)
        {
            foreach (ClipGroup group in track.Groups)
            {
                bool inside = false;
                foreach (MotionClip clip in group.Clips)
                {
                    if (t < clip.Start || t > clip.End)
                        continue;
                    inside = true;
                    Add(clip, clip.WeightAt(t), t, s);
                }
                if (inside || !group.Holds)
                    continue;

                // Hold the nearest edge: the first clip's start before it, else the last ended clip's end.
                MotionClip held = group.Clips[0];
                double at = held.Start;
                if (t >= held.Start)
                {
                    foreach (MotionClip clip in group.Clips)
                        if (clip.End <= t)
                            held = clip;
                    at = held.End;
                }
                Add(held, held.WeightAt(at), at, s);
            }
        }

        private static void Add(MotionClip clip, float w, double t, ElementState s)
        {
            switch (clip.Kind)
            {
                case ClipKind.Position: s.Position += clip.Vector * w; break;
                case ClipKind.Scale: s.Scale += clip.Vector * w; break;
                case ClipKind.Rotation: s.Rotation += clip.Scalar * w; break;
                case ClipKind.Opacity: s.Opacity += clip.Scalar * w; break;
                case ClipKind.Color:
                    s.ColorSum[(int)clip.Target] += clip.Color * w;
                    s.ColorWeight[(int)clip.Target] += w;
                    break;
                case ClipKind.Shake:
                {
                    UIMotionShakeBehaviour shake = clip.Shake;
                    float x = (float)((t - clip.Start) * shake.Frequency);
                    float seed = shake.Seed * 17.31f;
                    s.Position.x += (Mathf.PerlinNoise(seed, x) * 2f - 1f) * shake.Amplitude.x * w;
                    s.Position.y += (Mathf.PerlinNoise(seed + 5.7f, x) * 2f - 1f) * shake.Amplitude.y * w;
                    s.Rotation += (Mathf.PerlinNoise(seed + 11.3f, x) * 2f - 1f) * shake.RotationAmplitude * w;
                    break;
                }
                case ClipKind.Class: s.ActiveClasses.Add(clip.ClassName); break;
                case ClipKind.Display: s.Display = clip.Flag ? 1 : 0; break;
                case ClipKind.Visibility: s.Visible = clip.Flag ? 1 : 0; break;
            }
        }

        private static void Write(ElementState s)
        {
            VisualElement e = s.Element;
            Channels channels = s.Channels;
            if ((channels & Channels.Translate) != 0)
                e.style.translate = new Translate(s.Position.x, s.Position.y);
            if ((channels & Channels.Scale) != 0)
                e.style.scale = new Scale(s.Scale);
            if ((channels & Channels.Rotate) != 0)
                e.style.rotate = new Rotate(new Angle(s.Rotation, AngleUnit.Degree));
            if ((channels & Channels.Opacity) != 0)
                e.style.opacity = Mathf.Clamp01(s.Opacity);
            if ((channels & Channels.Display) != 0)
                e.style.display = s.Display < 0 ? new StyleEnum<DisplayStyle>(StyleKeyword.Null)
                    : s.Display == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if ((channels & Channels.Visibility) != 0)
                e.style.visibility = s.Visible < 0 ? new StyleEnum<Visibility>(StyleKeyword.Null)
                    : s.Visible == 1 ? Visibility.Visible : Visibility.Hidden;
            if ((channels & (Channels.BackgroundColor | Channels.ImageTint | Channels.TextColor | Channels.BorderColor)) != 0)
                WriteColors(s);
            if ((channels & Channels.Class) != 0)
                WriteClasses(s);
        }

        private static void WriteColors(ElementState s)
        {
            VisualElement e = s.Element;
            if (!s.NeutralRead)
            {
                s.NeutralRead = true;
                IResolvedStyle r = e.resolvedStyle;
                ReadNeutral(s, UIMotionColorTarget.BackgroundColor, r.backgroundColor);
                ReadNeutral(s, UIMotionColorTarget.ImageTint, r.unityBackgroundImageTintColor);
                ReadNeutral(s, UIMotionColorTarget.TextColor, r.color);
                ReadNeutral(s, UIMotionColorTarget.BorderColor, r.borderTopColor);
            }
            for (int c = 0; c < ColorTargets; c++)
            {
                if ((s.Channels & ColorChannel(c)) == 0)
                    continue;
                float w = s.ColorWeight[c];
                Color value = w >= 1f ? s.ColorSum[c] / w : s.Neutral[c] * (1f - w) + s.ColorSum[c];
                switch ((UIMotionColorTarget)c)
                {
                    case UIMotionColorTarget.BackgroundColor: e.style.backgroundColor = value; break;
                    case UIMotionColorTarget.ImageTint: e.style.unityBackgroundImageTintColor = value; break;
                    case UIMotionColorTarget.TextColor: e.style.color = value; break;
                    case UIMotionColorTarget.BorderColor:
                        e.style.borderTopColor = value;
                        e.style.borderRightColor = value;
                        e.style.borderBottomColor = value;
                        e.style.borderLeftColor = value;
                        break;
                }
            }
        }

        private static void ReadNeutral(ElementState s, UIMotionColorTarget target, Color resolved)
        {
            if (!s.HasNeutralOverride[(int)target])
                s.Neutral[(int)target] = resolved;
        }

        private static Channels ColorChannel(int target) => (UIMotionColorTarget)target switch
        {
            UIMotionColorTarget.BackgroundColor => Channels.BackgroundColor,
            UIMotionColorTarget.TextColor => Channels.TextColor,
            UIMotionColorTarget.BorderColor => Channels.BorderColor,
            _ => Channels.ImageTint
        };

        private static void WriteClasses(ElementState s)
        {
            VisualElement e = s.Element;
            foreach (string className in s.ActiveClasses)
                if (!e.ClassListContains(className))
                {
                    e.AddToClassList(className);
                    s.AddedClasses.Add(className);
                }
            s.AddedClasses.RemoveWhere(className =>
            {
                if (s.ActiveClasses.Contains(className))
                    return false;
                e.RemoveFromClassList(className);
                return true;
            });
        }
    }
}
