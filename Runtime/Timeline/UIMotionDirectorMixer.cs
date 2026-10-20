using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// Mixer of a <see cref="UIMotionTrack"/> played by a PlayableDirector (Timeline window preview,
    /// cutscene UI). The mixer of the timeline's first motion track evaluates the whole motion, with the
    /// same code as <see cref="UIMotion"/>, at the director's time, in the scope given by
    /// <see cref="UIMotionPreviewScope"/> or the parent UIDocument; the other tracks' mixers do nothing.
    /// </summary>
    public sealed class UIMotionDirectorMixer : PlayableBehaviour
    {
        private UIMotionTrack _track;
        private GameObject _owner;
        private bool _leader;
        private UIMotionPlayback _playback;
        private VisualElement _scope;

        internal void Initialize(UIMotionTrack track, GameObject owner)
        {
            _track = track;
            _owner = owner;
            TimelineAsset timeline = track.timelineAsset;
            if (timeline == null)
                return;
            foreach (TrackAsset t in timeline.GetOutputTracks())
            {
                if (t is not UIMotionTrack || t.muted)
                    continue;
                _leader = t == track;
                break;
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!_leader)
                return;
            VisualElement scope = UIMotionPreviewScope.ResolveScope(_owner);
            if (scope == null)
                return;
            if (_playback == null || scope != _scope)
            {
                _playback?.Restore();
                _scope = scope;
                _playback = new UIMotionPlayback(CompiledMotion.Compile(_track.timelineAsset), scope, "director");
            }
            var director = _owner != null ? _owner.GetComponent<PlayableDirector>() : null;
            double time = director != null ? director.time : playable.GetGraph().GetRootPlayable(0).GetTime();
            _playback.SetTimeSilently(time);
            _playback.Evaluate();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            _playback?.Restore();
            _playback = null;
        }
    }
}
