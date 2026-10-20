using NUnit.Framework;
using UnityEngine.UIElements;

namespace UIMotionToolkit.Tests
{
    public sealed class UIMotionSelectorTests
    {
        private VisualElement _root;

        [SetUp]
        public void SetUp()
        {
            // root > panel(#panel) > [a.item, b.item > c.item], d.other
            _root = new VisualElement { name = "root" };
            var panel = new VisualElement { name = "panel" };
            var a = new VisualElement { name = "a" };
            var b = new VisualElement { name = "b" };
            var c = new VisualElement { name = "c" };
            var d = new VisualElement { name = "d" };
            a.AddToClassList("item");
            b.AddToClassList("item");
            c.AddToClassList("item");
            d.AddToClassList("other");
            b.Add(c);
            panel.Add(a);
            panel.Add(b);
            _root.Add(panel);
            _root.Add(d);
        }

        [TestCase("#panel | fade", "#panel")]
        [TestCase("  .item | move (2) ", ".item")]
        [TestCase(".item (1)", ".item")]
        [TestCase("", "")]
        public void FromTrackName_StripsLabelAndSuffix(string trackName, string expected) =>
            Assert.AreEqual(expected, UIMotionSelector.FromTrackName(trackName));

        [Test]
        public void ClassSelector_MatchesInTreeOrder()
        {
            var matches = UIMotionSelector.Resolve(".item", _root);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Names(matches));
        }

        [Test]
        public void Descendant_And_DirectChild()
        {
            Assert.AreEqual(1, UIMotionSelector.Resolve("#b .item", _root).Count);
            Assert.AreEqual(0, UIMotionSelector.Resolve("#panel > #c", _root).Count);
            Assert.AreEqual(1, UIMotionSelector.Resolve("#b > .item", _root).Count);
        }

        [Test]
        public void Union_KeepsAlternativeOrder() =>
            CollectionAssert.AreEqual(new[] { "d", "a" }, Names(UIMotionSelector.Resolve("#d, #a", _root)));

        [Test]
        public void Scope_MatchesTheScopeItself()
        {
            var scope = _root.Q("panel");
            CollectionAssert.AreEqual(new[] { "panel" }, Names(UIMotionSelector.Resolve(":scope", scope)));
            CollectionAssert.AreEqual(new[] { "a", "b" }, Names(UIMotionSelector.Resolve(":scope > .item", scope)));
        }

        [Test]
        public void Unknown_ReturnsEmpty() => Assert.AreEqual(0, UIMotionSelector.Resolve("#nope", _root).Count);

        private static string[] Names(System.Collections.Generic.List<VisualElement> list) =>
            list.ConvertAll(e => e.name).ToArray();
    }
}
