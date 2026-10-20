using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UIMotionToolkit.Editor
{
    /// <summary>Editor helpers: preview a motion timeline in the Timeline window.</summary>
    public static class UIMotionMenu
    {
        /// <summary>
        /// Adds a "Motion Preview" director under the selected UIDocument / PanelRenderer object, playing
        /// the timeline selected in the Project window (if any): open it in the Timeline window and scrub.
        /// Set the scope selector on <see cref="UIMotionPreviewScope"/> for a component motion.
        /// </summary>
        [MenuItem("Tools/UI Motion Toolkit/Create Preview Director")]
        [MenuItem("GameObject/UI Toolkit/UI Motion Preview Director", false, 30)]
        public static void CreatePreviewDirector()
        {
            GameObject host = Selection.activeGameObject;
            if (host == null || !HasPanel(host))
            {
                EditorUtility.DisplayDialog("UI Motion Toolkit", "Select a GameObject with a UIDocument first.", "OK");
                return;
            }
            var go = new GameObject("Motion Preview");
            Undo.RegisterCreatedObjectUndo(go, "Create UI Motion Preview");
            go.transform.SetParent(host.transform, false);
            var director = go.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            go.AddComponent<UIMotionPreviewScope>();
            foreach (Object o in Selection.objects)
                if (o is TimelineAsset timeline)
                    director.playableAsset = timeline;
            Selection.activeGameObject = go;
        }

        private static bool HasPanel(GameObject host)
        {
            if (host.GetComponentInParent<UIDocument>() != null)
                return true;
#if UIMOTION_PANEL_RENDERER
            return host.GetComponentInParent<PanelRenderer>() != null;
#else
            return false;
#endif
        }
    }
}
