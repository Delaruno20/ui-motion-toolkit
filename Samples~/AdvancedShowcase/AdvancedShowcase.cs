using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UIMotionToolkit;

namespace UIMotionToolkit.Samples.Advanced
{
    /// <summary>
    /// "Dark Fantasy Showcase": a game-style screen driven entirely by UI Motion Toolkit.
    /// What to look for, feature by feature (motion ids are the timeline names in Motions/):
    /// <list type="bullet">
    /// <item>One-shot choreography, skippable, then a seamless loop: <c>Intro</c>, <c>Ambient</c> (UIMotion.Play, Complete, loop).</item>
    /// <item>Reversible state: menu items, tabs, chips, boss enrage (UIMotion.Toggle).</item>
    /// <item>Presence of panels: boss card, journal pages with direction-aware transitions, popup (UIMotion.Show / Hide).</item>
    /// <item>The same motion on many instances: <c>Bar.Hit</c> on the player and boss bars, one <c>Toast.*</c> chain per runtime-built toast.</item>
    /// <item>Layered effects on one scope: screen impact = stage shake + red flash in one timeline.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class AdvancedShowcaseSample : MonoBehaviour
    {
        [SerializeField] private UIMotionLibrary library;

        private sealed class BarState
        {
            public VisualElement Bar, Fill, Trail;
            public float Value = 1f, TrailValue = 1f, Hold;
        }

        private readonly Dictionary<VisualElement, BarState> _bars = new();
        private readonly List<Button> _tabs = new();
        private readonly List<VisualElement> _pages = new();

        private VisualElement _root, _stage, _boss, _popup, _toasts;
        private BarState _health, _stamina, _bossBar;
        private int _tab;
        private bool _enraged;

        private void Start()
        {
            UIMotion.Library = library;
            _root = GetComponent<UIDocument>().rootVisualElement;
            _stage = _root.Q("stage");
            _boss = _root.Q("boss-card");
            _popup = _root.Q("popup");
            _toasts = _root.Q("toasts");

            _health = Bar("bar-health");
            _stamina = Bar("bar-stamina");
            _bossBar = Bar("bar-boss");
            _stamina.Value = _stamina.TrailValue = 0.8f;
            Apply(_stamina);

            // Hover = focus on keyboard-style widgets, then ONE call binds every widget of a kind.
            foreach (VisualElement e in _root.Query(className: "menu-item").ToList()) e.RegisterCallback<PointerEnterEvent>(_ => e.Focus());
            foreach (VisualElement e in _root.Query(className: "chip").ToList()) e.RegisterCallback<PointerEnterEvent>(_ => e.Focus());
            UIMotionTriggers.BindAll(_root, ".menu-item", "Showcase.MenuItem.Focus", "Showcase.MenuItem.Press");
            UIMotionTriggers.BindAll(_root, ".chip", "Showcase.Chip.Focus", "Showcase.Chip.Press");
            UIMotionTriggers.BindAll(_root, ".tab", null, "Showcase.Chip.Press");

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Button tab = _root.Q<Button>($"tab-{i}");
                tab.clicked += () => SelectTab(index);
                _tabs.Add(tab);
                _pages.Add(_root.Q($"page-{i}"));
            }

            _root.Q<Button>("chip-intro").clicked += PlayIntro;
            _root.Q<Button>("chip-hit").clicked += () => DamageBoss(Random.Range(0.1f, 0.22f));
            _root.Q<Button>("chip-hurt").clicked += () => HurtPlayer(Random.Range(0.12f, 0.25f));
            _root.Q<Button>("chip-heal").clicked += Heal;
            _root.Q<Button>("chip-enrage").clicked += ToggleEnrage;
            _root.Q<Button>("chip-toast").clicked += () => Toast("NEW RELIC", "Cinder Locket added to your relics.");
            _root.Q<Button>("chip-popup").clicked += () => UIMotion.Show(_popup, "Showcase.Popup.Show");
            _root.Q<Button>("popup-no").clicked += ClosePopup;
            _root.Q<Button>("popup-yes").clicked += () =>
            {
                ClosePopup();
                Toast("QUEST ABANDONED", "The beacons fade into the dark.");
            };

            // A click during the intro jumps to its end (Complete runs the callbacks, so the loop still starts).
            _stage.RegisterCallback<PointerDownEvent>(_ => UIMotion.Complete(_stage, "Showcase.Intro"));

            _root.schedule.Execute(TickBars).Every(16);
            PlayIntro();
        }

        // ---------------------------------------------------------------- choreography

        private void PlayIntro()
        {
            UIMotion.Stop(_stage, "ambient");
            // onFinished fires at the end of the intro (or on Complete): hand over to the ambient loop.
            UIMotion.Play(_stage, "Showcase.Intro", onFinished: () =>
            {
                UIMotion.Play(_stage, "Showcase.Ambient", channel: "ambient", loop: true);
                _root.Q(className: "menu-item")?.Focus();
            });
        }

        // ---------------------------------------------------------------- bars

        private BarState Bar(string name)
        {
            VisualElement bar = _root.Q(name);
            var state = new BarState { Bar = bar, Fill = bar.Q(className: "bar__fill"), Trail = bar.Q(className: "bar__trail") };
            _bars[bar] = state;
            return state;
        }

        private static void Apply(BarState s)
        {
            s.Fill.style.width = Length.Percent(s.Value * 100f);
            s.Trail.style.width = Length.Percent(s.TrailValue * 100f);
        }

        /// <summary>Damage trail: the light segment waits, then drains toward the real value.</summary>
        private void TickBars()
        {
            foreach (BarState s in _bars.Values)
            {
                if (s.TrailValue <= s.Value) continue;
                s.Hold -= 0.016f;
                if (s.Hold > 0f) continue;
                s.TrailValue = Mathf.MoveTowards(s.TrailValue, s.Value, 0.0075f);
                Apply(s);
            }
        }

        private void SetBar(BarState s, float value)
        {
            float old = s.Value;
            s.Value = Mathf.Clamp01(value);
            if (s.Value >= old) s.TrailValue = s.Value;     // heal: no trail
            else s.Hold = 0.45f;
            Apply(s);
        }

        // ---------------------------------------------------------------- gameplay-ish actions

        private void DamageBoss(float amount)
        {
            if (!UIMotion.IsShown(_boss)) return;
            SetBar(_bossBar, _bossBar.Value - amount);
            UIMotion.Play(_bossBar.Bar, "Showcase.Bar.Hit");        // same motion, any bar
            UIMotion.Play(_root, "Showcase.Screen.Impact");
            if (_bossBar.Value > 0f) return;

            Toast("VICTORY", "The Pale Regent falls.");
            UIMotion.Hide(_boss, "Showcase.Boss.Show", "Showcase.Boss.Hide", onHidden: () =>
                _boss.schedule.Execute(() =>
                {
                    SetBar(_bossBar, 1f);
                    UIMotion.Show(_boss, "Showcase.Boss.Show");
                }).ExecuteLater(1500));
        }

        private void HurtPlayer(float amount)
        {
            SetBar(_health, _health.Value - amount);
            UIMotion.Play(_health.Bar, "Showcase.Bar.Hit");
            UIMotion.Play(_root, "Showcase.Screen.Hurt");
            // Below 30%: looping warning pulse on the bar, started and stopped by state.
            if (_health.Value < 0.3f && !UIMotion.IsPlaying(_health.Bar, "critical"))
                UIMotion.Play(_health.Bar, "Showcase.Bar.Critical", channel: "critical", loop: true);
            if (_health.Value <= 0f) SetBar(_health, 1f);          // endless demo: "respawn"
            if (_health.Value >= 0.3f) UIMotion.Stop(_health.Bar, "critical");
        }

        private void Heal()
        {
            SetBar(_health, 1f);
            SetBar(_stamina, 1f);
            UIMotion.Stop(_health.Bar, "critical");
            UIMotion.Play(_health.Bar, "Showcase.Bar.Heal");
            UIMotion.Play(_stamina.Bar, "Showcase.Bar.Heal");
        }

        private void ToggleEnrage()
        {
            _enraged = !_enraged;
            // Toggle contract: set the USS state (class) first, the motion animates between the two looks.
            _boss.EnableInClassList("is-enraged", _enraged);
            UIMotion.Toggle(_boss, "Showcase.Boss.Enrage", _enraged);
            _root.Q<Button>("chip-enrage").text = _enraged ? "Calm boss" : "Enrage boss";
        }

        // ---------------------------------------------------------------- tabs: direction-aware page transitions

        private void SelectTab(int index)
        {
            if (index == _tab) return;
            int old = _tab;
            _tab = index;
            bool forward = index > old;

            _tabs[old].RemoveFromClassList("tab--selected");
            UIMotion.Toggle(_tabs[old], "Showcase.Tab.Select", false);
            _tabs[index].AddToClassList("tab--selected");
            UIMotion.Toggle(_tabs[index], "Showcase.Tab.Select", true);

            UIMotion.Hide(_pages[old], null, forward ? "Showcase.Page.OutLeft" : "Showcase.Page.OutRight");
            UIMotion.Show(_pages[index], forward ? "Showcase.Page.InRight" : "Showcase.Page.InLeft");
        }

        // ---------------------------------------------------------------- toasts: chained one-shots on runtime-built elements

        private void Toast(string title, string text)
        {
            var toast = new VisualElement();
            toast.AddToClassList("toast");
            var heading = new Label(title) { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("toast__title");
            toast.Add(heading);
            var body = new Label(text) { pickingMode = PickingMode.Ignore };
            body.AddToClassList("toast__text");
            toast.Add(body);
            var life = new VisualElement { pickingMode = PickingMode.Ignore };
            life.AddToClassList("toast__life");
            toast.Add(life);
            _toasts.Add(toast);

            // Show -> Life (the bar drains) -> Hide -> remove. Each step starts from the previous one's callback.
            UIMotion.Play(toast, "Showcase.Toast.Show", onFinished: () =>
                UIMotion.Play(toast, "Showcase.Toast.Life", onFinished: () =>
                    UIMotion.Play(toast, "Showcase.Toast.Hide", onFinished: () => toast.RemoveFromHierarchy())));
        }

        private void ClosePopup() => UIMotion.Hide(_popup, "Showcase.Popup.Show", "Showcase.Popup.Hide");
    }
}
