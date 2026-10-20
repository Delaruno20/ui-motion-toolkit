using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// Wires interaction events of an element to motions (the element is the scope):
    /// focus / hover drive a reversible <see cref="UIMotion.Toggle"/>, a press plays a one-shot.
    /// Binding the same motion twice on an element is ignored.
    /// </summary>
    public static class UIMotionTriggers
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, System.Collections.Generic.HashSet<string>> s_bound = new();

        private static bool FirstBinding(VisualElement element, string key) => s_bound.GetOrCreateValue(element).Add(key);

        public const string FocusChannel = "focus";
        public const string HoverChannel = "hover";
        public const string PressChannel = "press";

        /// <summary>FocusIn plays <paramref name="motionId"/> forward, FocusOut backward.</summary>
        public static void ToggleOnFocus(VisualElement element, string motionId, string channel = FocusChannel)
        {
            if (!FirstBinding(element, "focus:" + motionId))
                return;
            element.RegisterCallback<FocusInEvent>(_ => UIMotion.Toggle(element, motionId, true, channel: channel));
            element.RegisterCallback<FocusOutEvent>(_ => UIMotion.Toggle(element, motionId, false, channel: channel));
        }

        /// <summary>Pointer enter plays <paramref name="motionId"/> forward, pointer leave backward.</summary>
        public static void ToggleOnHover(VisualElement element, string motionId, string channel = HoverChannel)
        {
            if (!FirstBinding(element, "hover:" + motionId))
                return;
            element.RegisterCallback<PointerEnterEvent>(_ => UIMotion.Toggle(element, motionId, true, channel: channel));
            element.RegisterCallback<PointerLeaveEvent>(_ => UIMotion.Toggle(element, motionId, false, channel: channel));
        }

        /// <summary>Pointer down and navigation submit (Enter / pad A) play <paramref name="motionId"/> once.</summary>
        public static void PlayOnPress(VisualElement element, string motionId, string channel = PressChannel)
        {
            if (!FirstBinding(element, "press:" + motionId))
                return;
            element.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0 && element.enabledInHierarchy)
                    UIMotion.Play(element, motionId, channel: channel);
            }, TrickleDown.TrickleDown);
            element.RegisterCallback<NavigationSubmitEvent>(_ =>
            {
                if (element.enabledInHierarchy)
                    UIMotion.Play(element, motionId, channel: channel);
            }, TrickleDown.TrickleDown);
        }

        /// <summary><see cref="ToggleOnFocus"/> + <see cref="PlayOnPress"/> on every element matching <paramref name="selector"/> under <paramref name="root"/>.</summary>
        public static void BindAll(VisualElement root, string selector, string focusMotionId, string pressMotionId = null)
        {
            foreach (VisualElement element in UIMotionSelector.Resolve(selector, root))
            {
                if (!string.IsNullOrEmpty(focusMotionId))
                    ToggleOnFocus(element, focusMotionId);
                if (!string.IsNullOrEmpty(pressMotionId))
                    PlayOnPress(element, pressMotionId);
            }
        }
    }
}
