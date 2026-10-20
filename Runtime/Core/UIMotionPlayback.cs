using System;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// One motion timeline playing on one scope element. Created by <see cref="UIMotion"/>; ticked on
    /// unscaled time (menus keep moving while the game is paused). Plays forward or backward from any
    /// time: a state motion (focus, shown) is turned off by reversing it from where it is.
    /// </summary>
    public sealed class UIMotionPlayback
    {
        private readonly CompiledMotion _motion;
        private MotionEvaluator _evaluator;
        private bool _wasAttached;
        private double _time;

        internal UIMotionPlayback(CompiledMotion motion, VisualElement scope, string channel)
        {
            _motion = motion;
            Scope = scope;
            Channel = channel;
            Bind();
        }

        public TimelineAsset Motion => _motion.Asset;
        public VisualElement Scope { get; }
        public string Channel { get; }

        /// <summary>Timeline duration plus the longest stagger delay of the matched elements.</summary>
        public double Duration { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool Reversed { get; private set; }
        public bool Loop { get; set; }

        /// <summary>Playback rate (1 = authored speed).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Cleared inline styles when the playback ends (default): the USS state is the rest state.</summary>
        public bool RestoreOnFinish { get; set; } = true;

        /// <summary>Raised when the playback reaches its end (forward) or its start (reversed). Not on <see cref="Stop"/>.</summary>
        public event Action<UIMotionPlayback> Finished;

        /// <summary>Single completion callback owned by the <see cref="UIMotion"/> helpers (replaced on each toggle).</summary>
        internal Action Callback { get; set; }

        public double Time
        {
            get => _time;
            set
            {
                _time = Math.Clamp(value, 0.0, Duration);
                Evaluate();
            }
        }

        /// <summary>Moves the playhead without writing anything (state bookkeeping).</summary>
        internal void SetTimeSilently(double time) => _time = Math.Clamp(time, 0.0, Duration);

        /// <summary>0 at the start of the motion, 1 at its end.</summary>
        public float Progress => Duration > 0.0 ? (float)(_time / Duration) : 1f;

        /// <summary>Re-resolves the selectors (elements added to the scope since the playback started).</summary>
        public void Bind()
        {
            _evaluator?.Restore();
            _evaluator = new MotionEvaluator(_motion, Scope);
            Duration = _motion.Duration + _evaluator.StaggerSpan;
        }

        public void PlayForward()
        {
            Reversed = false;
            Start();
        }

        public void PlayReverse()
        {
            Reversed = true;
            Start();
        }

        /// <summary>Jumps to the end of the current direction and finishes now.</summary>
        public void Complete()
        {
            if (!IsPlaying)
                return;
            _time = Reversed ? 0.0 : Duration;
            Evaluate();
            Finish();
        }

        /// <summary>Stops without raising <see cref="Finished"/>; <paramref name="restore"/> clears the inline styles.</summary>
        public void Stop(bool restore = true)
        {
            bool wasRegistered = IsPlaying;
            IsPlaying = false;
            if (restore)
                Restore();
            if (wasRegistered)
                UIMotion.Unregister(this);
        }

        public void Evaluate() => _evaluator.Evaluate(_time);

        public void Restore() => _evaluator.Restore();

        private void Start()
        {
            _wasAttached |= Scope.panel != null;
            if (!IsPlaying)
            {
                IsPlaying = true;
                UIMotion.Register(this);
            }
            Evaluate();
        }

        /// <summary>Advances by <paramref name="deltaTime"/> seconds; false once finished.</summary>
        internal bool Tick(float deltaTime)
        {
            if (!IsPlaying)
                return false;
            if (Scope.panel == null)
            {
                // Removed from its panel: drop it (a scope never attached yet may still be built).
                if (_wasAttached)
                {
                    Stop();
                    return false;
                }
            }
            else
            {
                _wasAttached = true;
            }

            double step = deltaTime * Speed;
            _time += Reversed ? -step : step;
            bool ended = Reversed ? _time <= 0.0 : _time >= Duration;
            if (ended && Loop && Duration > 0.0)
            {
                _time = Reversed ? Duration + _time % Duration : _time % Duration;
                ended = false;
            }
            if (ended)
            {
                _time = Reversed ? 0.0 : Duration;
                Evaluate();
                Finish();
                return false;
            }
            Evaluate();
            return true;
        }

        private void Finish()
        {
            IsPlaying = false;
            UIMotion.Unregister(this);
            // Before the callback: whatever it starts on the same elements must not be cleared after.
            // Both happen in the same frame, before the panel renders.
            if (RestoreOnFinish)
                Restore();
            Action callback = Callback;
            Callback = null;
            callback?.Invoke();
            Finished?.Invoke(this);
        }
    }
}
