using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UIMotionToolkit
{
    /// <summary>
    /// Noise shake (hit, error, impact): adds a smooth random offset to the position (px) and rotation
    /// (degrees) of the track's elements, scaled by the clip weight; fade it out with an ease out.
    /// Deterministic: the same time always gives the same offset (scrubbing, reverse play).
    /// </summary>
    [Serializable]
    public class UIMotionShakeClip : PlayableAsset, ITimelineClipAsset
    {
        public UIMotionShakeBehaviour _template = new();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override double duration => 0.35;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) =>
            ScriptPlayable<UIMotionShakeBehaviour>.Create(graph, _template);
    }

    [Serializable]
    public class UIMotionShakeBehaviour : PlayableBehaviour
    {
        [Tooltip("Maximum offset in px on each axis.")]
        public Vector2 Amplitude = new(8f, 4f);
        [Tooltip("Maximum rotation in degrees.")]
        public float RotationAmplitude;
        [Tooltip("Oscillations per second.")]
        public float Frequency = 22f;
        public int Seed = 1;
    }
}
