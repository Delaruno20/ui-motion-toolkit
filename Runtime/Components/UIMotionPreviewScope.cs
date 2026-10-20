using UnityEngine;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// Put next to a PlayableDirector that plays a motion timeline (child of a UIDocument or of a
    /// PanelRenderer object) to preview it in the Timeline window: the tracks resolve inside the first
    /// element matching <see cref="scopeSelector"/> (empty = the whole document), like
    /// <see cref="UIMotion"/>.Play does at runtime with a component instance.
    /// Menu: GameObject > UI Toolkit > UI Motion Preview Director.
    /// </summary>
    [AddComponentMenu("UI Motion Toolkit/UI Motion Preview Scope")]
    public sealed class UIMotionPreviewScope : MonoBehaviour
    {
        [Tooltip("Selector of the scope element (e.g. #resume-button, .save-slot-card). Empty: document root.")]
        [SerializeField] private string scopeSelector;

#if UIMOTION_PANEL_RENDERER
        private VisualElement _panelRendererRoot;
        private PanelRenderer _panelRenderer;
#endif

        public string ScopeSelector
        {
            get => scopeSelector;
            set => scopeSelector = value;
        }

        /// <summary>Scope for tracks evaluated by a director on <paramref name="owner"/>.</summary>
        public static VisualElement ResolveScope(GameObject owner)
        {
            if (owner == null)
                return null;
            var preview = owner.GetComponent<UIMotionPreviewScope>();
            VisualElement root = preview != null ? preview.Root() : DocumentRoot(owner);
            if (root == null || preview == null || string.IsNullOrWhiteSpace(preview.scopeSelector))
                return root;
            var matches = UIMotionSelector.Resolve(preview.scopeSelector, root);
            return matches.Count > 0 ? matches[0] : null;
        }

        private VisualElement Root()
        {
            VisualElement root = DocumentRoot(gameObject);
            if (root != null)
                return root;
#if UIMOTION_PANEL_RENDERER
            if (_panelRenderer == null)
            {
                _panelRenderer = GetComponentInParent<PanelRenderer>();
                _panelRenderer?.RegisterUIReloadCallback((_, documentRoot, _) => _panelRendererRoot = documentRoot);
            }
            return _panelRendererRoot;
#else
            return null;
#endif
        }

        private static VisualElement DocumentRoot(GameObject owner)
        {
            var document = owner.GetComponentInParent<UIDocument>();
            return document != null ? document.rootVisualElement : null;
        }
    }
}
