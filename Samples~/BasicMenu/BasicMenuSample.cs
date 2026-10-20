using UnityEngine;
using UnityEngine.UIElements;
using UIMotionToolkit;

namespace UIMotionToolkit.Samples.Basic
{
    /// <summary>
    /// Everything the Basic Menu sample does with UI Motion Toolkit, in one short file.
    /// The motions themselves (timelines in Motions/) contain no code; this script only says WHEN to play them.
    /// Works with a <see cref="UIDocument"/> or, on Unity 6000.6+, a <see cref="PanelRenderer"/> on the same
    /// GameObject: UIMotion only needs a VisualElement, wherever it comes from.
    /// </summary>
    public sealed class BasicMenuSample : MonoBehaviour
    {
        // Motion ids = timeline asset names. They are resolved through the library below.
        private const string MenuShow = "Basic.Menu.Show";
        private const string MenuHide = "Basic.Menu.Hide";
        private const string ButtonFocus = "Basic.Button.Focus";
        private const string ButtonPress = "Basic.Button.Press";
        private const string Shake = "Basic.Error.Shake";
        private const string PopupShow = "Basic.Popup.Show";
        private const string PopupHide = "Basic.Popup.Hide";
        private const string HintFlash = "Basic.Hint.Flash";

        [Tooltip("The library listing the sample's motions (Motions/BasicMenuLibrary).")]
        [SerializeField] private UIMotionLibrary library;

        private VisualElement _menu;
        private VisualElement _popup;
        private Label _hint;

#if UNITY_6000_6_OR_NEWER
        private PanelRenderer _panelRenderer;

        // PanelRenderer builds (and may rebuild) its tree itself: bind every time it hands us a root.
        private void OnEnable()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            _panelRenderer?.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable() => _panelRenderer?.UnregisterUIReloadCallback(OnUIReload);

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version) => Bind(root);
#endif

        private void Start()
        {
            // UIDocument path (a PanelRenderer binds from its reload callback instead).
            var document = GetComponent<UIDocument>();
            if (document != null)
                Bind(document.rootVisualElement);
        }

        private void Bind(VisualElement root)
        {
            // 1. Tell the toolkit where to find motions by id. (Alternative: put the library in
            //    Resources/UI/UIMotionLibrary and skip this line.)
            UIMotion.Library = library;

            _menu = root.Q("menu");
            _popup = root.Q("popup");
            _hint = root.Q<Label>("hint");

            // 2. Hover/focus + press feedback on every button, in one call each.
            //    Focus is reversible: leaving a button plays the motion backwards from where it is.
            UIMotionTriggers.BindAll(root, ".menu-button", ButtonFocus, ButtonPress);
            foreach (VisualElement button in root.Query(className: "menu-button").ToList())
                button.RegisterCallback<PointerEnterEvent>(_ => button.Focus());

            // 3. Buttons.
            root.Q<Button>("new-game").clicked += FakeLoading;
            root.Q<Button>("options").clicked += () => UIMotion.Show(_popup, PopupShow);
            root.Q<Button>("popup-close").clicked += () => UIMotion.Hide(_popup, PopupShow, PopupHide);
            root.Q<Button>("continue").clicked += () => UIMotion.Play(root.Q("continue"), Shake);
            root.Q<Button>("quit").clicked += () =>
            {
                _hint.text = "Application.Quit() would run here.";
                UIMotion.Play(_hint, HintFlash);
            };

            // 4. Entrance.
            UIMotion.Show(_menu, MenuShow);
        }

        /// <summary>Hide with an exit motion, wait, show again: Show/Hide drive the "is-hidden" class for you.</summary>
        private void FakeLoading()
        {
            _hint.text = "Loading...";
            UIMotion.Play(_hint, HintFlash);
            UIMotion.Hide(_menu, MenuShow, MenuHide, onHidden: () =>
                _menu.schedule.Execute(() =>
                {
                    _hint.text = "Done. The menu is back.";
                    UIMotion.Show(_menu, MenuShow);
                }).ExecuteLater(1200));
        }
    }
}
