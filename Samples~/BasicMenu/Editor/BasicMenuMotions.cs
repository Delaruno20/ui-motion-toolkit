using System.IO;
using UnityEditor;
using UnityEngine;
using UIMotionToolkit.Editor;

namespace UIMotionToolkit.Samples.Basic.Editor
{
    /// <summary>
    /// Authors the Basic Menu motions from code with <see cref="MotionAsset"/>. The result is ordinary Timeline
    /// assets: open any of them in the Timeline window (Window > Sequencing > Timeline) to see and tweak the tracks.
    /// Re-running this overwrites them. Menu: Tools > UI Motion Toolkit > Samples > Rebuild Basic Menu Motions.
    /// </summary>
    public static class BasicMenuMotions
    {
        private static readonly Color Line = new(70 / 255f, 48 / 255f, 54 / 255f);
        private static readonly Color Button = new(30 / 255f, 25 / 255f, 32 / 255f);
        private static readonly Color ButtonFocus = new(74 / 255f, 24 / 255f, 32 / 255f);
        private static readonly Color Accent = new(214 / 255f, 78 / 255f, 84 / 255f);
        private static readonly Color Muted = new(138 / 255f, 126 / 255f, 120 / 255f);
        private static readonly Color Text = new(222 / 255f, 212 / 255f, 198 / 255f);

        [MenuItem("Tools/UI Motion Toolkit/Samples/Rebuild Basic Menu Motions")]
        public static void Rebuild()
        {
            string folder = SampleFolder() + "/Motions";

            // ---- Menu: staggered entrance (scope = #menu itself, ":scope" targets the scope) ----
            MotionAsset menuShow = MotionAsset.Begin(folder + "/Basic.Menu.Show.asset");
            menuShow.Track(":scope").FadeIn(0.00, 0.35).MoveFrom(new Vector2(0, 28), 0.00, 0.50, MotionEase.OutCubic);
            menuShow.Track(".menu__title").FadeIn(0.15, 0.40).MoveFrom(new Vector2(0, -14), 0.15, 0.50, MotionEase.OutCubic);
            menuShow.Track(".menu__rule").ScaleFrom(new Vector2(0f, 1f), 0.25, 0.55, MotionEase.OutExpo);
            // stagger: element i starts i x 0.07 s later.
            menuShow.Track(".menu-button", stagger: 0.07f).FadeIn(0.35, 0.30).MoveFrom(new Vector2(-48, 0), 0.35, 0.45, MotionEase.OutCubic);
            menuShow.Save();

            MotionAsset menuHide = MotionAsset.Begin(folder + "/Basic.Menu.Hide.asset");
            // staggerReverse: last button leaves first.
            menuHide.Track(".menu-button", stagger: 0.05f, staggerReverse: true).FadeOut(0.00, 0.22).MoveTo(new Vector2(-36, 0), 0.00, 0.25, MotionEase.InCubic);
            menuHide.Track(":scope").FadeOut(0.20, 0.25);
            menuHide.Save();

            // ---- Button: focus is a REVERSIBLE state (Toggle). End values == the USS :focus rest state. ----
            MotionAsset focus = MotionAsset.Begin(folder + "/Basic.Button.Focus.asset");
            focus.Track(":scope")
                .MoveTo(new Vector2(10, 0), 0, 0.16, MotionEase.OutCubic)
                .ColorTo(UIMotionColorTarget.BackgroundColor, ButtonFocus, 0, 0.16, MotionEase.OutCubic, neutral: Button)
                .ColorTo(UIMotionColorTarget.BorderColor, Accent, 0, 0.16, MotionEase.OutCubic, neutral: Line);
            focus.Save();

            // ---- Button press: one-shot pulse ----
            MotionAsset press = MotionAsset.Begin(folder + "/Basic.Button.Press.asset");
            press.Track(":scope").ScalePulse(0.95f, 0, 0.20, MotionEase.OutSine, inShare: 0.35);
            press.Save();

            // ---- Error: shake + red flash on the border ----
            MotionAsset shake = MotionAsset.Begin(folder + "/Basic.Error.Shake.asset");
            shake.Track(":scope")
                .Shake(0, 0.45, new Vector2(10, 0), rotation: 0f, frequency: 26f)
                .ColorPulse(UIMotionColorTarget.BorderColor, Accent, 0, 0.45, MotionEase.InOutSine, inShare: 0.2);
            shake.Save();

            // ---- Popup: backdrop fade + springy window + staggered rows ----
            MotionAsset popupShow = MotionAsset.Begin(folder + "/Basic.Popup.Show.asset");
            popupShow.Track(".popup__backdrop").FadeIn(0, 0.25);
            popupShow.Track(".popup__window").FadeIn(0.05, 0.20).Pop(0.05, 0.40, from: 0.82f).MoveFrom(new Vector2(0, 24), 0.05, 0.40, MotionEase.OutCubic);
            popupShow.Track(".popup__row", stagger: 0.06f).FadeIn(0.25, 0.25).MoveFrom(new Vector2(0, 10), 0.25, 0.30, MotionEase.OutCubic);
            popupShow.Save();

            MotionAsset popupHide = MotionAsset.Begin(folder + "/Basic.Popup.Hide.asset");
            popupHide.Track(".popup__window").FadeOut(0, 0.18).ScaleTo(0.92f, 0, 0.20, MotionEase.InCubic);
            popupHide.Track(".popup__backdrop").FadeOut(0.06, 0.18);
            popupHide.Save();

            // ---- Hint label flash (text color) ----
            MotionAsset hint = MotionAsset.Begin(folder + "/Basic.Hint.Flash.asset");
            hint.Track(":scope").ColorPulse(UIMotionColorTarget.TextColor, Text, 0, 0.6, MotionEase.InOutSine, inShare: 0.25, neutral: Muted);
            hint.Save();

            UIMotionLibraryBuilder.Rebuild(folder + "/BasicMenuLibrary.asset", folder);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Folder of this sample, wherever it was imported (Assets/Samples/...).</summary>
        private static string SampleFolder()
        {
            string[] guids = AssetDatabase.FindAssets("BasicMenuMotions t:MonoScript");
            string script = AssetDatabase.GUIDToAssetPath(guids[0]);
            return Path.GetDirectoryName(Path.GetDirectoryName(script))!.Replace('\\', '/');
        }
    }
}
