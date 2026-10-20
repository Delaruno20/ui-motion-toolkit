using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace UIMotionToolkit
{
    /// <summary>
    /// Every motion timeline the project plays by id (= asset name, e.g. "PauseMenu.Show"). Components
    /// (VisualElements, no serialized fields) and views look motions up here instead of each holding
    /// references. Loaded from Resources/<see cref="ResourcePath"/> on first use.
    /// </summary>
    [CreateAssetMenu(menuName = "UI Motion Toolkit/Motion Library", fileName = "UIMotionLibrary")]
    public sealed class UIMotionLibrary : ScriptableObject
    {
        public const string ResourcePath = "UI/UIMotionLibrary";

        [SerializeField] private List<TimelineAsset> motions = new();

        private Dictionary<string, TimelineAsset> _byId;

        public IReadOnlyList<TimelineAsset> Motions => motions;

        public TimelineAsset Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, TimelineAsset>();
                foreach (TimelineAsset motion in motions)
                    if (motion != null)
                        _byId[motion.name] = motion;
            }
            return _byId.TryGetValue(id, out TimelineAsset found) ? found : null;
        }

        public void SetMotions(IEnumerable<TimelineAsset> list)
        {
            motions = new List<TimelineAsset>(list);
            _byId = null;
        }

        private void OnValidate() => _byId = null;
    }
}
