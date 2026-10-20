using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UIMotionToolkit.Editor
{
    /// <summary>What <see cref="UIMotionPreviewRenderer"/> renders: a UXML, a motion, the times to capture.</summary>
    public sealed class MotionPreviewRequest
    {
        public VisualTreeAsset Uxml;
        public PanelSettings Panel;
        public Vector2Int Size = new(1920, 1080);
        /// <summary>Called once the tree is laid out (fill fake data, remove is-hidden…).</summary>
        public Action<VisualElement> Prepare;
        /// <summary>Scope of the motion inside the document root (default: the root).</summary>
        public Func<VisualElement, VisualElement> Scope;
        public TimelineAsset Motion;
        /// <summary>Times in seconds; negative = from the end of the playback (-0 = last frame).</summary>
        public double[] Times = { 0.0 };
        public bool Reverse;
        /// <summary>Output path relative to the project; "{t}" is replaced by the time.</summary>
        public string Output = ".agent/motion/{t}.png";
    }

    /// <summary>
    /// Offscreen captures of a motion at given times (batchmode without -nographics, or menu): the
    /// motion is evaluated directly (no ticking), USS transitions are cut so the frames are exact.
    /// </summary>
    public static class UIMotionPreviewRenderer
    {
        private const int SettleFrames = 4;

        public static void Render(MotionPreviewRequest request, Action<bool> done)
        {
            var rt = new RenderTexture(request.Size.x, request.Size.y, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            PanelSettings panel = UnityEngine.Object.Instantiate(request.Panel);
            panel.targetTexture = rt;
            panel.clearColor = true;

            var go = new GameObject("UIMotionPreview") { hideFlags = HideFlags.DontSave };
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = request.Uxml;

            Type util = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo update = util.GetMethod("UpdatePanels", flags);
            MethodInfo render = util.GetMethod("RenderOffscreenPanels", flags);

            bool manual = UIMotion.ManualTick;
            UIMotion.ManualTick = true;
            int frame = 0;
            int shot = 0;
            UIMotionPlayback playback = null;
            bool success = true;

            void Finish()
            {
                EditorApplication.update -= Tick;
                playback?.Restore();
                UIMotion.ManualTick = manual;
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(panel);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                done?.Invoke(success);
            }

            void Tick()
            {
                try
                {
                    frame++;
                    update.Invoke(null, null);
                    VisualElement root = doc.rootVisualElement;
                    if (frame == 2)
                    {
                        var noTransition = new StyleList<TimeValue>(new List<TimeValue> { new(0f) });
                        root.Query().ForEach(e => e.style.transitionDuration = noTransition);
                        request.Prepare?.Invoke(root);
                        return;
                    }
                    if (frame < SettleFrames)
                        return;
                    if (playback == null)
                    {
                        VisualElement scope = request.Scope != null ? request.Scope(root) : root;
                        playback = new UIMotionPlayback(CompiledMotion.Get(request.Motion), scope, "preview");
                    }
                    if (shot >= request.Times.Length)
                    {
                        Finish();
                        return;
                    }
                    // Even frames set the time and let the panel lay out, odd frames capture.
                    if ((frame - SettleFrames) % 2 == 0)
                    {
                        double t = request.Times[shot];
                        if (t < 0 || (t == 0 && double.IsNegative(t))) t = playback.Duration + t;
                        playback.Time = request.Reverse ? playback.Duration - t : t;
                        return;
                    }
                    render.Invoke(null, null);
                    string time = request.Times[shot].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    Capture(rt, request.Output.Replace("{t}", time));
                    shot++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[UIMotionToolkit] preview FAILED " + e);
                    success = false;
                    Finish();
                }
            }

            EditorApplication.update += Tick;
        }

        /// <summary>Renders several requests one after the other.</summary>
        public static void RenderAll(IReadOnlyList<MotionPreviewRequest> requests, Action<bool> done, int index = 0, bool allOk = true)
        {
            if (index >= requests.Count)
            {
                done?.Invoke(allOk);
                return;
            }
            Render(requests[index], ok => RenderAll(requests, done, index + 1, allOk && ok));
        }

        private static void Capture(RenderTexture rt, string output)
        {
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, output);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[UIMotionToolkit] preview -> " + path);
        }
    }
}
