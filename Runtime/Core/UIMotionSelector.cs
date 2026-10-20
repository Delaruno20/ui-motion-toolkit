using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UIMotionToolkit
{
    /// <summary>
    /// Resolves the selector written as the name of a <see cref="UIMotionTrack"/> against the scope of a
    /// playback. A USS-like subset, same spirit as UI Toolkit Timeline:
    /// <list type="bullet">
    /// <item><c>#name</c>, <c>.class</c>, <c>#name.class.other</c>, <c>*</c> — compound parts;</item>
    /// <item><c>a b</c> — descendant, <c>a &gt; b</c> — direct child;</item>
    /// <item><c>a, b</c> — union (stagger order = order of the alternatives, then tree order);</item>
    /// <item><c>:scope</c> (or <c>&amp;</c>) — the scope element itself, also usable as the first part
    /// (<c>:scope &gt; .item</c>).</item>
    /// </list>
    /// The first part also matches the scope itself, so a component motion can name its own block class.
    /// Matches come in tree order. In a track name, text after <c>|</c> is a label ("#panel | fade") and a
    /// trailing " (1)" (added by Timeline to duplicate names) is ignored: see <see cref="FromTrackName"/>.
    /// </summary>
    public static class UIMotionSelector
    {
        public const string ScopeKeyword = ":scope";

        private static readonly char[] Whitespace = { ' ', '\t', '\n', '\r' };
        private static readonly List<VisualElement> s_current = new();
        private static readonly List<VisualElement> s_next = new();
        private static readonly HashSet<VisualElement> s_seen = new();

        /// <summary>Selector part of a track name: label after '|' and Timeline's " (n)" suffix removed.</summary>
        public static string FromTrackName(string trackName)
        {
            if (string.IsNullOrEmpty(trackName))
                return string.Empty;
            int bar = trackName.IndexOf('|');
            string selector = bar >= 0 ? trackName.Substring(0, bar) : trackName;
            return System.Text.RegularExpressions.Regex.Replace(selector, @"\s\(\d+\)\s*$", string.Empty).Trim();
        }

        public static List<VisualElement> Resolve(string selector, VisualElement scope)
        {
            var results = new List<VisualElement>();
            Resolve(selector, scope, results);
            return results;
        }

        public static void Resolve(string selector, VisualElement scope, List<VisualElement> results)
        {
            results.Clear();
            if (scope == null || string.IsNullOrWhiteSpace(selector))
                return;

            var unique = new HashSet<VisualElement>();
            foreach (string alternative in selector.Split(','))
            {
                string[] parts = Tokenize(alternative);
                if (parts.Length == 0)
                    continue;

                s_current.Clear();
                int index = 0;
                bool child = false;
                if (IsScope(parts[0]))
                {
                    s_current.Add(scope);
                    index = 1;
                }
                else
                {
                    Part first = Part.Parse(parts[0]);
                    if (first.Matches(scope))
                        s_current.Add(scope);
                    CollectDescendants(scope, first, s_current);
                    index = 1;
                }

                for (; index < parts.Length && s_current.Count > 0; index++)
                {
                    if (parts[index] == ">")
                    {
                        child = true;
                        continue;
                    }
                    Part part = Part.Parse(parts[index]);
                    s_next.Clear();
                    s_seen.Clear();
                    foreach (VisualElement parent in s_current)
                    {
                        if (child)
                        {
                            for (int i = 0; i < parent.hierarchy.childCount; i++)
                            {
                                VisualElement c = parent.hierarchy[i];
                                if (part.Matches(c) && s_seen.Add(c))
                                    s_next.Add(c);
                            }
                        }
                        else
                        {
                            int before = s_next.Count;
                            CollectDescendants(parent, part, s_next);
                            for (int i = s_next.Count - 1; i >= before; i--)
                                if (!s_seen.Add(s_next[i]))
                                    s_next.RemoveAt(i);
                        }
                    }
                    child = false;
                    s_current.Clear();
                    s_current.AddRange(s_next);
                }

                foreach (VisualElement element in s_current)
                    if (unique.Add(element))
                        results.Add(element);
            }
            s_current.Clear();
            s_next.Clear();
            s_seen.Clear();
        }

        private static bool IsScope(string part) => part == ScopeKeyword || part == "&";

        private static string[] Tokenize(string alternative)
        {
            // "a>b" and "a > b" both become ["a", ">", "b"].
            return alternative.Replace(">", " > ").Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void CollectDescendants(VisualElement parent, Part part, List<VisualElement> into)
        {
            int count = parent.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                VisualElement c = parent.hierarchy[i];
                if (part.Matches(c))
                    into.Add(c);
                CollectDescendants(c, part, into);
            }
        }

        private readonly struct Part
        {
            private readonly string _name;
            private readonly string[] _classes;

            private Part(string name, string[] classes)
            {
                _name = name;
                _classes = classes;
            }

            public static Part Parse(string text)
            {
                if (text == "*")
                    return new Part(null, Array.Empty<string>());
                string name = null;
                var classes = new List<string>();
                int i = 0;
                while (i < text.Length)
                {
                    char kind = text[i];
                    int end = text.IndexOfAny(new[] { '#', '.' }, i + 1);
                    if (end < 0) end = text.Length;
                    string token = text.Substring(i + 1, end - i - 1);
                    if (kind == '#') name = token;
                    else if (kind == '.') classes.Add(token);
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[UIMotionToolkit] Selector part '{text}': only #name, .class and * are supported.");
                        return new Part("\0", Array.Empty<string>());
                    }
                    i = end;
                }
                return new Part(name, classes.ToArray());
            }

            public bool Matches(VisualElement element)
            {
                if (_name != null && element.name != _name)
                    return false;
                foreach (string c in _classes)
                    if (!element.ClassListContains(c))
                        return false;
                return true;
            }
        }
    }
}
