using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace UIMotionToolkit.Editor
{
    /// <summary>Fills a <see cref="UIMotionLibrary"/> with every timeline holding a UI motion track under some folders.</summary>
    public static class UIMotionLibraryBuilder
    {
        /// <summary>Where <see cref="UIMotion.Library"/> looks by default (Resources/UI/UIMotionLibrary).</summary>
        public const string DefaultLibraryPath = "Assets/Resources/" + UIMotionLibrary.ResourcePath + ".asset";

        /// <summary>
        /// Creates the default library if needed and fills it with every motion timeline of the project
        /// (outside packages). For finer control call <see cref="Rebuild"/> with your own folders.
        /// </summary>
        [MenuItem("Tools/UI Motion Toolkit/Rebuild Default Motion Library")]
        public static void RebuildDefault() => Rebuild(DefaultLibraryPath, "Assets");

        public static UIMotionLibrary Rebuild(string libraryPath, params string[] folders)
        {
            var library = AssetDatabase.LoadAssetAtPath<UIMotionLibrary>(libraryPath);
            if (library == null)
            {
                MotionAsset.EnsureFolder(Path.GetDirectoryName(libraryPath)!.Replace('\\', '/'));
                library = ScriptableObject.CreateInstance<UIMotionLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }

            var motions = new List<TimelineAsset>();
            var names = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:TimelineAsset", folders))
            {
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (timeline == null || !timeline.GetOutputTracks().Any(t => t is UIMotionTrack || t is UITTimeline.UITVisualElementTrack))
                    continue;
                if (!names.Add(timeline.name))
                    Debug.LogWarning($"[UIMotionToolkit] Two motions are named '{timeline.name}': ids must be unique ({AssetDatabase.GetAssetPath(timeline)}).");
                motions.Add(timeline);
            }
            motions.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            library.SetMotions(motions);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIMotionToolkit] {libraryPath}: {motions.Count} motions.");
            return library;
        }
    }
}
