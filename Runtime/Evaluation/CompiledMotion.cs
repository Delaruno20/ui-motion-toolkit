using System;
using System.Collections.Generic;
using UITTimeline;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    internal enum ClipKind
    {
        Position,
        Scale,
        Rotation,
        Opacity,
        Color,
        Shake,
        Class,
        Display,
        Visibility
    }

    [Flags]
    internal enum Channels
    {
        None = 0,
        Translate = 1 << 0,
        Scale = 1 << 1,
        Rotate = 1 << 2,
        Opacity = 1 << 3,
        BackgroundColor = 1 << 4,
        ImageTint = 1 << 5,
        TextColor = 1 << 6,
        BorderColor = 1 << 7,
        Display = 1 << 8,
        Visibility = 1 << 9,
        Class = 1 << 10
    }

    /// <summary>One clip of a track, its value read once from the clip asset.</summary>
    internal sealed class MotionClip
    {
        public TimelineClip Clip;
        public ClipKind Kind;
        public Vector2 Vector;
        public float Scalar;
        public Color Color;
        public UIMotionColorTarget Target;
        public string ClassName;
        public bool Flag;
        public UIMotionShakeBehaviour Shake;

        public double Start => Clip.start;
        public double End => Clip.end;

        /// <summary>
        /// Ease in x ease out weight, like Timeline's EvaluateMixIn / EvaluateMixOut but without clamping
        /// the curve value: an overshooting curve (back ease) springs past the target.
        /// </summary>
        public float WeightAt(double time)
        {
            float weight = 1f;
            double mixIn = Clip.mixInDuration;
            if (mixIn > 1e-6)
                weight *= Clip.mixInCurve.Evaluate(Mathf.Clamp01((float)((time - Clip.start) / mixIn)));
            double mixOut = Clip.mixOutDuration;
            if (mixOut > 1e-6)
                weight *= Clip.mixOutCurve.Evaluate(Mathf.Clamp01((float)((time - Clip.mixOutTime) / mixOut)));
            return weight;
        }
    }

    /// <summary>Clips of one track driving the same property, sorted by start.</summary>
    internal sealed class ClipGroup
    {
        public ClipKind Kind;
        public UIMotionColorTarget Target;
        public bool HasNeutral;
        public Color Neutral;
        public readonly List<MotionClip> Clips = new();
        /// <summary>Constant-value clips hold their edge value outside the clips.</summary>
        public bool Holds => Kind <= ClipKind.Color;
    }

    internal sealed class MotionTrack
    {
        public string Selector;
        public float Stagger;
        public bool StaggerReverse;
        public bool UsageHints;
        public Channels Channels;
        public readonly List<ClipGroup> Groups = new();
        public readonly List<string> Classes = new();
    }

    /// <summary>
    /// A motion timeline read into plain data (tracks → clip groups) and evaluated without a
    /// PlayableGraph: the runtime player and the Timeline window share <see cref="Evaluate"/>.
    /// </summary>
    internal sealed class CompiledMotion
    {
        public TimelineAsset Asset;
        public double Duration;
        public readonly List<MotionTrack> Tracks = new();

#if !UNITY_EDITOR
        private static readonly Dictionary<TimelineAsset, CompiledMotion> s_cache = new();
#endif

        public static CompiledMotion Get(TimelineAsset asset)
        {
            if (asset == null)
                return null;
#if UNITY_EDITOR
            // Clips may have been edited in the Timeline window since the last play.
            return Compile(asset);
#else
            if (!s_cache.TryGetValue(asset, out CompiledMotion motion))
                s_cache[asset] = motion = Compile(asset);
            return motion;
#endif
        }

        public static CompiledMotion Compile(TimelineAsset asset)
        {
            var motion = new CompiledMotion { Asset = asset, Duration = asset.duration };
            foreach (TrackAsset track in asset.GetOutputTracks())
            {
                if (track.muted)
                    continue;
                MotionTrack compiled = CompileTrack(track);
                if (compiled != null)
                    motion.Tracks.Add(compiled);
            }
            return motion;
        }

        public static MotionTrack CompileTrack(TrackAsset track)
        {
            var compiled = new MotionTrack { Selector = UIMotionSelector.FromTrackName(track.name), UsageHints = true };
            switch (track)
            {
                case UIMotionTrack motionTrack:
                    compiled.Stagger = motionTrack.Stagger;
                    compiled.StaggerReverse = motionTrack.StaggerReverse;
                    compiled.UsageHints = motionTrack.AutomaticUsageHints;
                    break;
                case UITVisualElementTrack:
                    break;
                default:
                    return null;
            }

            var clips = new List<TimelineClip>(track.GetClips());
            clips.Sort((a, b) => a.start.CompareTo(b.start));
            foreach (TimelineClip clip in clips)
            {
                MotionClip motionClip = Read(clip);
                if (motionClip == null)
                    continue;
                ClipGroup group = compiled.Groups.Find(g => g.Kind == motionClip.Kind
                    && (motionClip.Kind != ClipKind.Color || g.Target == motionClip.Target));
                if (group == null)
                {
                    group = new ClipGroup { Kind = motionClip.Kind, Target = motionClip.Target };
                    compiled.Groups.Add(group);
                }
                group.Clips.Add(motionClip);
                if (clip.asset is UIMotionColorClip { _template: { OverrideNeutral: true } } colorClip && !group.HasNeutral)
                {
                    group.HasNeutral = true;
                    group.Neutral = colorClip._template.Neutral;
                }
                compiled.Channels |= ChannelOf(motionClip);
                if (motionClip.Kind == ClipKind.Class && !compiled.Classes.Contains(motionClip.ClassName))
                    compiled.Classes.Add(motionClip.ClassName);
            }
            return compiled.Groups.Count > 0 ? compiled : null;
        }

        private static MotionClip Read(TimelineClip clip)
        {
            var c = new MotionClip { Clip = clip };
            switch (clip.asset)
            {
                case UITPositionClip p: c.Kind = ClipKind.Position; c.Vector = p._template.Position; break;
                case UITScaleClip s: c.Kind = ClipKind.Scale; c.Vector = s._template.Scale; break;
                case UITRotationClip r: c.Kind = ClipKind.Rotation; c.Scalar = r._template.Rotation; break;
                case UITOpacityClip o: c.Kind = ClipKind.Opacity; c.Scalar = o._template.Opacity; break;
                case UIMotionColorClip col:
                    c.Kind = ClipKind.Color;
                    c.Color = col._template.Color;
                    c.Target = col._template.Target;
                    break;
                case UIMotionShakeClip sh: c.Kind = ClipKind.Shake; c.Shake = sh._template; break;
                case UITClassClip cl:
                    if (string.IsNullOrEmpty(cl._template.ClassName))
                        return null;
                    c.Kind = ClipKind.Class;
                    c.ClassName = cl._template.ClassName;
                    break;
                case UITDisplayClip d: c.Kind = ClipKind.Display; c.Flag = d._template.Display; break;
                case UITVisibilityClip v: c.Kind = ClipKind.Visibility; c.Flag = v._template.Visible; break;
                default: return null;
            }
            return c;
        }

        private static Channels ChannelOf(MotionClip clip) => clip.Kind switch
        {
            ClipKind.Position => Channels.Translate,
            ClipKind.Scale => Channels.Scale,
            ClipKind.Rotation => Channels.Rotate,
            ClipKind.Opacity => Channels.Opacity,
            ClipKind.Shake => Channels.Translate | (clip.Shake.RotationAmplitude != 0f ? Channels.Rotate : Channels.None),
            ClipKind.Class => Channels.Class,
            ClipKind.Display => Channels.Display,
            ClipKind.Visibility => Channels.Visibility,
            ClipKind.Color => clip.Target switch
            {
                UIMotionColorTarget.BackgroundColor => Channels.BackgroundColor,
                UIMotionColorTarget.TextColor => Channels.TextColor,
                UIMotionColorTarget.BorderColor => Channels.BorderColor,
                _ => Channels.ImageTint
            },
            _ => Channels.None
        };
    }

}
