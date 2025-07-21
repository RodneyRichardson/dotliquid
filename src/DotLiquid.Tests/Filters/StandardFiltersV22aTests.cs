using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace DotLiquid.Tests.Filters
{
    [TestFixture]
    public class StandardFiltersV22aTests : StandardFiltersTestsBase
    {
        public override SyntaxCompatibility SyntaxCompatibilityLevel => SyntaxCompatibility.DotLiquid22a;
        public override CapitalizeDelegate Capitalize => i => StandardFilters.Capitalize(i);
        public override MathDelegate Divide => (i, o) => StandardFilters.DividedBy(_context, i, o);
        public override MathDelegate Plus => (i, o) => StandardFilters.Plus(_context, i, o);
        public override MathDelegate Minus => (i, o) => StandardFilters.Minus(_context, i, o);
        public override MathDelegate Modulo => (i, o) => StandardFilters.Modulo(_context, i, o);
        public override RemoveFirstDelegate RemoveFirst => (a, b) => LegacyFilters.RemoveFirstV21(a, b);
        public override ReplaceDelegate Replace => (i, s, r) => StandardFilters.Replace(i, s, r);
        public override ReplaceFirstDelegate ReplaceFirst => (i, s, r) => LegacyFilters.ReplaceFirstV21(i, s, r);
        public override SliceDelegate Slice => (a, b, c) => c.HasValue ? StandardFilters.Slice(a, b, c.Value) : StandardFilters.Slice(a, b);
        public override SortDelegate Sort => (input, property) => StandardFilters.Sort(input, property);
        public override SplitDelegate Split => (i, p) => LegacyFilters.Split(i, p);
        public override MathDelegate Times => (i, o) => StandardFilters.Times(_context, i, o);
        public override TruncateWordsDelegate TruncateWords => (i, w, s) =>
        {
            if (w.HasValue)
                return s == null ? LegacyFilters.TruncateWords(i, w.Value) : LegacyFilters.TruncateWords(i, w.Value, s);
            return LegacyFilters.TruncateWords(i);
        };

        private Context _context;

        [OneTimeSetUp]
        public void SetUp()
        {
            _context = new Context(CultureInfo.InvariantCulture)
            {
                SyntaxCompatibilityLevel = SyntaxCompatibilityLevel
            };
        }

        [Test]
        public void TestReplaceFirstInvalidSearchReturnsInput()
        {
            Assert.That(ReplaceFirst(input: "a a a a", @string: null, replacement: "b"), Is.EqualTo("a a a a"));
            Assert.That(ReplaceFirst(input: "a a a a", @string: "", replacement: "b"), Is.EqualTo("a a a a"));
        }

        [Test]
        public void TestSlice()
        {
            // Verify Liquid compliance from V22a syntax:
            Assert.That(Slice(null, 1), Is.EqualTo("")); // DotLiquid test case
            Assert.That(Slice("", 10), Is.EqualTo("")); // DotLiquid test case
            Assert.That(Slice(123, 1), Is.EqualTo(123)); // Ignore invalid input

            Assert.That(Slice(null, 0), Is.EqualTo("")); // Liquid test case
            Assert.That(Slice("foobar", 100, 10), Is.EqualTo("")); // Liquid test case
        }

        [Test]
        public void TestSplitNullReturnsArrayWithNull()
        {
            Assert.That(Split(null, null), Is.EqualTo(new string[] { null }).AsCollection);

        }

        [Test]
        public void TestTruncateWordsLessOneWordAllowed()
        {
            Assert.That(TruncateWords("Ground control to Major Tom.", 0), Is.EqualTo("..."));
            Assert.That(TruncateWords("Ground control to Major Tom.", -1), Is.EqualTo("..."));
        }

        [Test]
        public void TestSort()
        {
            var ints = new[] { 10, 3, 2, 1 };
            Assert.That(Sort(null), Is.EqualTo(null));
            Assert.That(Sort(new string[] { }), Is.EqualTo(new string[] { }).AsCollection);
            Assert.That(Sort(ints), Is.EqualTo(new[] { 1, 2, 3, 10 }).AsCollection);
            Assert.That(Sort(new[] { new { a = 10 }, new { a = 3 }, new { a = 1 }, new { a = 2 } }, "a"), Is.EqualTo(new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 10 } }).AsCollection);

            var strings = new[] { "zebra", "octopus", "giraffe", "Sally Snake" };
            Assert.That(Sort(strings), Is.EqualTo(new[] { "Sally Snake", "giraffe", "octopus", "zebra" }).AsCollection);

            var hashes = new List<Hash>();
            for (var i = 0; i < strings.Length; i++)
                hashes.Add(CreateHash(ints[i], strings[i]));
            Assert.That(Sort(hashes, "content"), Is.EqualTo(new[] { hashes[3], hashes[2], hashes[1], hashes[0] }).AsCollection);
            Assert.That(Sort(hashes, "sortby"), Is.EqualTo(new[] { hashes[3], hashes[2], hashes[1], hashes[0] }).AsCollection);
        }
    }
}
