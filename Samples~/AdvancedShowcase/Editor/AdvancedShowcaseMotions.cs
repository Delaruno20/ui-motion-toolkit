using System.IO;
using UnityEditor;
using UnityEngine;
using UIMotionToolkit.Editor;

namespace UIMotionToolkit.Samples.Advanced.Editor
{
    /// <summary>
    /// Authors every motion of the Dark Fantasy Showcase from code with <see cref="MotionAsset"/>.
    /// Output = ordinary Timeline assets (Motions/): open them in the Timeline window to see the tracks.
    /// Reading order: the big choreographies first, then the small reusable state motions.
    /// Menu: Tools > UI Motion Toolkit > Samples > Rebuild Advanced Showcase Motions.
    /// </summary>
    public static class AdvancedShowcaseMotions
    {
        // Palette: must match the :root variables of AdvancedShowcase.uss, because a state motion
        // ends exactly on the USS rest state.
        private static readonly Color Ink = Rgb(230, 220, 203);
        private static readonly Color Muted = Rgb(141, 129, 120);
        private static readonly Color Gold = Rgb(201, 160, 74);
        private static readonly Color Line = Rgb(58, 44, 48);
        private static readonly Color CrimsonDark = Rgb(74, 22, 30);
        private static readonly Color CrimsonBright = Rgb(224, 85, 94);
        private static readonly Color Clear = new(0, 0, 0, 0);
        private static readonly Color ChipBg = Rgb(28, 22, 32);
        private static readonly Color BossFill = Rgb(150, 120, 60);

        private static Color Rgb(int r, int g, int b, float a = 1f) => new(r / 255f, g / 255f, b / 255f, a);

        private static string _folder;

        [MenuItem("Tools/UI Motion Toolkit/Samples/Rebuild Advanced Showcase Motions")]
        public static void Rebuild()
        {
            _folder = SampleFolder() + "/Motions";
            Intro();
            Ambient();
            Boss();
            Bars();
            Impact();
            MenuItemAndChips();
            TabsAndPages();
            Toasts();
            Popup();
            UIMotionLibraryBuilder.Rebuild(_folder + "/AdvancedShowcaseLibrary.asset", _folder);
            AssetDatabase.SaveAssets();
        }

        private static MotionAsset Begin(string id) => MotionAsset.Begin($"{_folder}/{id}.asset");

        // ================================================================= big choreographies

        /// <summary>
        /// Scope = #stage. ~3.5 s, 26 tracks. Each block starts later than the previous one, which is the whole
        /// trick of a good intro: a clear order of reading. Stagger turns one track into a cascade.
        /// </summary>
        private static void Intro()
        {
            MotionAsset m = Begin("Showcase.Intro");

            // 0.0 - 1.0  Curtain: a black overlay fades to the USS color (transparent). Color clip with explicit neutral.
            m.Track("#curtain").ColorFrom(UIMotionColorTarget.BackgroundColor, Color.black, 0, 1.0, MotionEase.InOutSine, neutral: Clear);

            // 0.3 -      Embers rise one after another (stagger = delay between matched elements).
            m.Track(".ember", stagger: 0.12f).FadeIn(0.3, 0.6).MoveFrom(new Vector2(0, 50), 0.3, 1.2, MotionEase.OutCubic);

            // 0.35 -     Logo slams in: overshoot-free expo scale, fade, a white-hot color that cools down,
            //            plus a Class clip that adds ".is-glowing" (USS text-shadow) only while it plays.
            m.Track("#logo")
                .ScaleFrom(1.22f, 0.35, 1.10, MotionEase.OutExpo)
                .FadeIn(0.35, 0.70)
                .ColorFrom(UIMotionColorTarget.TextColor, Color.white, 0.35, 1.2, MotionEase.OutCubic, neutral: Ink)
                .Class("is-glowing", 0.35, 1.3);

            // 0.95 -     Rule draws itself from the left (USS transform-origin: left).
            m.Track("#logo-rule").ScaleFrom(new Vector2(0f, 1f), 0.95, 0.70, MotionEase.OutExpo);
            m.Track("#tagline").FadeIn(1.2, 0.6).MoveFrom(new Vector2(0, 12), 1.2, 0.6, MotionEase.OutCubic);

            // 1.5 -      Menu cascade.
            m.Track(".menu-item", stagger: 0.09f).FadeIn(1.5, 0.4).MoveFrom(new Vector2(-80, 0), 1.5, 0.6, MotionEase.OutCubic);

            // 1.9 -      HUD pieces arrive from their own edges.
            m.Track("#player-card").MoveFrom(new Vector2(-140, 0), 1.9, 0.7, MotionEase.OutCubic).FadeIn(1.9, 0.5);
            m.Track("#player-card .bar__fill, #player-card .bar__trail", stagger: 0.12f).ScaleFrom(new Vector2(0f, 1f), 2.4, 0.7, MotionEase.OutCubic);
            m.Track("#location-card").MoveFrom(new Vector2(120, 0), 2.1, 0.7, MotionEase.OutCubic).FadeIn(2.1, 0.5);
            m.Track("#boss-card").MoveFrom(new Vector2(0, -70), 2.4, 0.8, MotionEase.OutBack).FadeIn(2.4, 0.5);
            m.Track("#boss-card .bar__fill, #boss-card .bar__trail").ScaleFrom(new Vector2(0f, 1f), 2.9, 0.8, MotionEase.OutCubic);
            m.Track("#journal").MoveFrom(new Vector2(140, 0), 2.5, 0.7, MotionEase.OutCubic).FadeIn(2.5, 0.5);
            m.Track("#page-0 .entry", stagger: 0.06f).FadeIn(2.9, 0.35).MoveFrom(new Vector2(30, 0), 2.9, 0.4, MotionEase.OutCubic);
            m.Track("#control-bar").MoveFrom(new Vector2(0, 70), 2.9, 0.6, MotionEase.OutCubic).FadeIn(2.9, 0.4);
            m.Save();
        }

        /// <summary>
        /// Looped by code (UIMotion.Play(..., loop: true)). Every clip is a Pulse (neutral -> offset -> neutral),
        /// so the loop point is seamless.
        /// </summary>
        private static void Ambient()
        {
            MotionAsset m = Begin("Showcase.Ambient");
            m.Track(".ember", stagger: 0.3f).FadePulse(0, 2.6, 0.15f).MovePulse(new Vector2(0, -36), 0, 2.6);
            m.Track("#logo").ColorPulse(UIMotionColorTarget.TextColor, Rgb(255, 226, 196), 0, 4.0, MotionEase.InOutSine, 0.5, neutral: Ink);
            m.Save();
        }

        // ================================================================= boss card

        private static void Boss()
        {
            MotionAsset show = Begin("Showcase.Boss.Show");
            show.Track(":scope").FadeIn(0, 0.5).MoveFrom(new Vector2(0, -60), 0, 0.7, MotionEase.OutBack);
            show.Track(".boss-card__rule").ScaleFrom(new Vector2(0f, 1f), 0.3, 0.6, MotionEase.OutExpo);
            show.Track(".bar__fill, .bar__trail").ScaleFrom(new Vector2(0f, 1f), 0.4, 0.9, MotionEase.OutCubic);
            show.Save();

            MotionAsset hide = Begin("Showcase.Boss.Hide");
            hide.Track(":scope").FadeOut(0, 0.6).MoveTo(new Vector2(0, -30), 0, 0.6, MotionEase.InCubic);
            hide.Save();

            // REVERSIBLE STATE: forward = enraged, reversed = calm. Its end values equal the USS ".is-enraged" look.
            MotionAsset enrage = Begin("Showcase.Boss.Enrage");
            enrage.Track(":scope").ColorTo(UIMotionColorTarget.BorderColor, CrimsonBright, 0, 0.45, MotionEase.InOutSine, neutral: Clear);
            enrage.Track(".boss-card__name").ColorTo(UIMotionColorTarget.TextColor, CrimsonBright, 0, 0.45, MotionEase.InOutSine, neutral: Ink);
            enrage.Track(".bar__fill").ColorTo(UIMotionColorTarget.BackgroundColor, CrimsonBright, 0, 0.45, MotionEase.InOutSine, neutral: BossFill);
            enrage.Save();
        }

        // ================================================================= bars (scope = one .bar, any bar)

        private static void Bars()
        {
            MotionAsset hit = Begin("Showcase.Bar.Hit");
            hit.Track(":scope").Shake(0, 0.40, new Vector2(7, 3), rotation: 0.6f, frequency: 30f);
            hit.Track(".bar__fill").ColorPulse(UIMotionColorTarget.BackgroundColor, Color.white, 0, 0.30, MotionEase.InOutSine, inShare: 0.15);
            hit.Save();

            MotionAsset heal = Begin("Showcase.Bar.Heal");
            heal.Track(".bar__fill").ColorPulse(UIMotionColorTarget.BackgroundColor, Rgb(190, 230, 170), 0, 0.6, MotionEase.InOutSine, inShare: 0.25);
            heal.Track(":scope").ScalePulse(new Vector2(1f, 1.25f), 0, 0.5, MotionEase.OutSine, inShare: 0.3);
            heal.Save();

            // Looped while health is low.
            MotionAsset critical = Begin("Showcase.Bar.Critical");
            critical.Track(":scope").ColorPulse(UIMotionColorTarget.BorderColor, CrimsonBright, 0, 0.9, MotionEase.InOutSine, 0.5, neutral: Line);
            critical.Track(".bar__fill").FadePulse(0, 0.9, 0.55f);
            critical.Save();
        }

        // ================================================================= screen impact (scope = #screen)

        private static void Impact()
        {
            MotionAsset impact = Begin("Showcase.Screen.Impact");
            impact.Track("#stage").Shake(0, 0.35, new Vector2(5, 5), rotation: 0.3f, frequency: 28f);
            impact.Track("#flash").ColorPulse(UIMotionColorTarget.BackgroundColor, Rgb(158, 42, 53, 0.28f), 0, 0.35, MotionEase.InOutSine, 0.2, neutral: Rgb(158, 42, 53, 0f));
            impact.Save();

            MotionAsset hurt = Begin("Showcase.Screen.Hurt");
            hurt.Track("#stage").Shake(0, 0.55, new Vector2(12, 8), rotation: 0.8f, frequency: 24f);
            hurt.Track("#flash").ColorPulse(UIMotionColorTarget.BackgroundColor, Rgb(158, 42, 53, 0.34f), 0, 0.6, MotionEase.InOutSine, 0.15, neutral: Rgb(158, 42, 53, 0f));
            hurt.Track("#player-card").Shake(0.05, 0.45, new Vector2(10, 0), frequency: 30f);
            hurt.Save();
        }

        // ================================================================= focus / press

        private static void MenuItemAndChips()
        {
            // Scope = one .menu-item. Rest state when focused (USS): translate 18px, gold text, marker scale 1.
            MotionAsset focus = Begin("Showcase.MenuItem.Focus");
            focus.Track(":scope")
                .MoveTo(new Vector2(18, 0), 0, 0.20, MotionEase.OutCubic)
                .ColorTo(UIMotionColorTarget.TextColor, Gold, 0, 0.20, MotionEase.OutCubic, neutral: Muted);
            focus.Track(".menu-item__marker").ScaleFrom(0f, 0.04, 0.26, MotionEase.OutBack);
            focus.Save();

            MotionAsset press = Begin("Showcase.MenuItem.Press");
            press.Track(":scope").ScalePulse(0.96f, 0, 0.22, MotionEase.OutSine, 0.35);
            press.Track(".menu-item__marker").ScalePulse(1.9f, 0, 0.30, MotionEase.OutSine, 0.3);
            press.Save();

            MotionAsset chipFocus = Begin("Showcase.Chip.Focus");
            chipFocus.Track(":scope")
                .ColorTo(UIMotionColorTarget.BackgroundColor, CrimsonDark, 0, 0.14, MotionEase.OutCubic, neutral: ChipBg)
                .ColorTo(UIMotionColorTarget.BorderColor, CrimsonBright, 0, 0.14, MotionEase.OutCubic, neutral: Line);
            chipFocus.Save();

            MotionAsset chipPress = Begin("Showcase.Chip.Press");
            chipPress.Track(":scope").ScalePulse(0.94f, 0, 0.18, MotionEase.OutSine, 0.35);
            chipPress.Save();
        }

        // ================================================================= tabs

        private static void TabsAndPages()
        {
            // Scope = one .tab. Forward = selected. The underline grows from nothing, the text warms up.
            MotionAsset select = Begin("Showcase.Tab.Select");
            select.Track(".tab__bar").ScaleFrom(new Vector2(0f, 1f), 0, 0.28, MotionEase.OutCubic);
            select.Track(":scope").ColorFrom(UIMotionColorTarget.TextColor, Muted, 0, 0.28, MotionEase.OutCubic, neutral: Gold);
            select.Save();

            // Scope = one .page. Four directional variants chosen by code from the tab index delta.
            Page("Showcase.Page.InRight", +1);
            Page("Showcase.Page.InLeft", -1);
            MotionAsset outLeft = Begin("Showcase.Page.OutLeft");
            outLeft.Track(":scope").FadeOut(0, 0.22).MoveTo(new Vector2(-50, 0), 0, 0.25, MotionEase.InCubic);
            outLeft.Save();
            MotionAsset outRight = Begin("Showcase.Page.OutRight");
            outRight.Track(":scope").FadeOut(0, 0.22).MoveTo(new Vector2(50, 0), 0, 0.25, MotionEase.InCubic);
            outRight.Save();
        }

        private static void Page(string id, float side)
        {
            MotionAsset m = Begin(id);
            m.Track(":scope").FadeIn(0, 0.30).MoveFrom(new Vector2(60 * side, 0), 0, 0.40, MotionEase.OutCubic);
            m.Track(".entry", stagger: 0.05f).FadeIn(0.10, 0.30).MoveFrom(new Vector2(30 * side, 0), 0.10, 0.35, MotionEase.OutCubic);
            m.Save();
        }

        // ================================================================= toasts (scope = one runtime-built .toast)

        private static void Toasts()
        {
            MotionAsset show = Begin("Showcase.Toast.Show");
            show.Track(":scope").FadeIn(0, 0.35).MoveFrom(new Vector2(380, 0), 0, 0.55, MotionEase.OutExpo);
            show.Save();

            MotionAsset life = Begin("Showcase.Toast.Life");
            life.Track(".toast__life").ScaleTo(new Vector2(0f, 1f), 0, 3.2, MotionEase.Linear);
            life.Save();

            // The 0.01 s ScaleTo keeps the life bar empty: inline styles are cleared at the end of Life.
            MotionAsset hide = Begin("Showcase.Toast.Hide");
            hide.Track(":scope").FadeOut(0, 0.30).MoveTo(new Vector2(120, 0), 0, 0.35, MotionEase.InCubic);
            hide.Track(".toast__life").ScaleTo(new Vector2(0f, 1f), 0, 0.01, MotionEase.Linear);
            hide.Save();
        }

        // ================================================================= popup (scope = #popup)

        private static void Popup()
        {
            MotionAsset show = Begin("Showcase.Popup.Show");
            show.Track(".popup__backdrop").FadeIn(0, 0.30);
            show.Track(".popup__window").FadeIn(0.05, 0.20).Pop(0.05, 0.45, from: 0.80f).MoveFrom(new Vector2(0, 30), 0.05, 0.45, MotionEase.OutCubic);
            show.Track(".popup__title").MoveFrom(new Vector2(0, -14), 0.20, 0.45, MotionEase.OutCubic);
            show.Track(".popup__line", stagger: 0.08f).FadeIn(0.30, 0.30).MoveFrom(new Vector2(0, 10), 0.30, 0.35, MotionEase.OutCubic);
            show.Track(".chip", stagger: 0.07f).Pop(0.50, 0.35, from: 0.7f).FadeIn(0.50, 0.25);
            show.Save();

            MotionAsset hide = Begin("Showcase.Popup.Hide");
            hide.Track(".popup__window").FadeOut(0, 0.18).ScaleTo(0.92f, 0, 0.20, MotionEase.InCubic);
            hide.Track(".popup__backdrop").FadeOut(0.06, 0.18);
            hide.Save();
        }

        private static string SampleFolder()
        {
            string script = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("AdvancedShowcaseMotions t:MonoScript")[0]);
            return Path.GetDirectoryName(Path.GetDirectoryName(script))!.Replace('\\', '/');
        }
    }
}
