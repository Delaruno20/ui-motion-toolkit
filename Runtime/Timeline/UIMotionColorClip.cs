using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UIMotionToolkit
{
    /// <summary>Which color style a <see cref="UIMotionColorClip"/> drives.</summary>
    public enum UIMotionColorTarget
    {
        BackgroundColor,
        ImageTint,
        TextColor,
        BorderColor
    }

    /// <summary>
    /// Blends a color style of the track's elements toward <see cref="UIMotionColorBehaviour.Color"/> by
    /// the clip weight (ease in / out curves). Neutral (weight 0) = <see cref="UIMotionColorBehaviour.Neutral"/>
    /// when <see cref="UIMotionColorBehaviour.OverrideNeutral"/> is set (recommended for state motions:
    /// the USS color of the "off" state), else the color the USS gave the element when it was bound.
    /// </summary>
    [Serializable]
    public class UIMotionColorClip : PlayableAsset, ITimelineClipAsset
    {
        public UIMotionColorBehaviour _template = new();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override double duration => 0.3;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) =>
            ScriptPlayable<UIMotionColorBehaviour>.Create(graph, _template);
    }

    [Serializable]
    public class UIMotionColorBehaviour : PlayableBehaviour
    {
        public UIMotionColorTarget Target = UIMotionColorTarget.ImageTint;
        public Color Color = Color.white;
        [Tooltip("Use Neutral as the weight-0 color instead of reading the element's USS color.")]
        public bool OverrideNeutral;
        public Color Neutral = Color.white;
    }
}
