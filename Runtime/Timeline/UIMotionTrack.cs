using System;
using UITTimeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UIMotionToolkit
{
    /// <summary>
    /// Timeline track animating UI Toolkit elements. The <b>track name is the selector</b> of the elements
    /// it drives (<see cref="UIMotionSelector"/>: <c>#name</c>, <c>.class</c>, descendants, <c>:scope</c>;
    /// text after <c>|</c> is a free label: "#panel | slide"), resolved inside the scope given to
    /// <see cref="UIMotion"/>.Play — a whole screen or a single component instance, so one asset serves
    /// every card / button / toast.
    /// <para>Clips: the UI Toolkit Timeline ones (position, rotation, scale, opacity, class, display,
    /// visibility) plus <see cref="UIMotionColorClip"/> and <see cref="UIMotionShakeClip"/>. Numeric clips
    /// are offsets from the neutral state (no offset, scale 1, opacity 1) weighted by their ease curves.
    /// Unlike the package's track, only the properties the track has clips for are written, and they are
    /// cleared when the playback ends: the USS state is the rest state.</para>
    /// <para>Outside its clips a track holds the nearest edge value (first clip's start before it, the
    /// previous clip's end after it) for the numeric clips; class / display / visibility / shake only act
    /// inside their clip.</para>
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(UITPositionClip)), TrackClipType(typeof(UITScaleClip)),
     TrackClipType(typeof(UITRotationClip)), TrackClipType(typeof(UITOpacityClip)),
     TrackClipType(typeof(UITClassClip)), TrackClipType(typeof(UITDisplayClip)),
     TrackClipType(typeof(UITVisibilityClip)), TrackClipType(typeof(UIMotionColorClip)),
     TrackClipType(typeof(UIMotionShakeClip))]
    [TrackColor(0.93f, 0.62f, 0.23f)]
    public class UIMotionTrack : TrackAsset
    {
        [Tooltip("Delay in seconds between two matched elements (lists, menus): element i plays the track i x stagger later.")]
        [SerializeField, Min(0f)] private float stagger;
        [Tooltip("Stagger from the last matched element to the first.")]
        [SerializeField] private bool staggerReverse;
        [Tooltip("Adds the DynamicTransform / DynamicColor usage hints the clips need to the elements.")]
        [SerializeField] private bool automaticUsageHints = true;

        public string Selector => UIMotionSelector.FromTrackName(name);

        public float Stagger
        {
            get => stagger;
            set => stagger = Mathf.Max(0f, value);
        }

        public bool StaggerReverse
        {
            get => staggerReverse;
            set => staggerReverse = value;
        }

        public bool AutomaticUsageHints
        {
            get => automaticUsageHints;
            set => automaticUsageHints = value;
        }

        /// <summary>Timeline window / PlayableDirector path: same evaluation as the runtime player.</summary>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<UIMotionDirectorMixer>.Create(graph, inputCount);
            mixer.GetBehaviour().Initialize(this, go);
            return mixer;
        }
    }
}
