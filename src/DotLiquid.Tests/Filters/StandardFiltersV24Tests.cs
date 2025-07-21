using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using NUnit.Framework;

namespace DotLiquid.Tests.Filters
{
    [TestFixture]
    public class StandardFiltersV24Tests : StandardFiltersTestsBase
    {
        public override SyntaxCompatibility SyntaxCompatibilityLevel => SyntaxCompatibility.DotLiquid24;
        public override CapitalizeDelegate Capitalize => i => StandardFilters.Capitalize(i);
        public override MathDelegate Divide => (i, o) => StandardFilters.DividedBy(_context, i, o);
        public override MathDelegate Plus => (i, o) => StandardFilters.Plus(_context, i, o);
        public override MathDelegate Minus => (i, o) => StandardFilters.Minus(_context, i, o);
        public override MathDelegate Modulo => (i, o) => StandardFilters.Modulo(_context, i, o);
        public override RemoveFirstDelegate RemoveFirst => (a, b) => StandardFilters.RemoveFirst(a, b);
        public override ReplaceDelegate Replace => (i, s, r) => StandardFilters.Replace(i, s, r);
        public override ReplaceFirstDelegate ReplaceFirst => (a, b, c) => StandardFilters.ReplaceFirst(a, b, c);
        public override SliceDelegate Slice => (a, b, c) => c.HasValue ? StandardFilters.Slice(a, b, c.Value) : StandardFilters.Slice(a, b);
        public override SortDelegate Sort => (input, property) => StandardFilters.Sort(input, property);
        public override SplitDelegate Split => (i, p) => StandardFilters.Split(i, p);
        public override MathDelegate Times => (i, o) => StandardFilters.Times(_context, i, o);
        public override TruncateWordsDelegate TruncateWords => (i, w, s) =>
        {
            if (w.HasValue)
                return s == null ? StandardFilters.TruncateWords(i, w.Value) : StandardFilters.TruncateWords(i, w.Value, s);
            return StandardFilters.TruncateWords(i);
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
        public void TestReplaceFirstInvalidSearchPrepends()
        {
            Assert.That(ReplaceFirst(input: "a a a a", @string: null, replacement: "b"), Is.EqualTo("ba a a a"));
            Assert.That(ReplaceFirst(input: "a a a a", @string: "", replacement: "b"), Is.EqualTo("ba a a a"));
        }

        [Test]
        public void TestSplitNullReturnsEmptyArray()
        {
            Assert.That(Split(null, null), Has.Exactly(0).Items);
        }

        [Test]
        public void TestTruncateWordsLessOneWordIgnored()
        {
            Assert.That(TruncateWords("Ground control to Major Tom.", 0), Is.EqualTo("Ground..."));
            Assert.That(TruncateWords("Ground control to Major Tom.", -1), Is.EqualTo("Ground..."));
        }

        [Test]
        public void TestTruncateWordsWhitespaceCollapsed()
        {
            Assert.That(TruncateWords("    one    two three    four  ", 2), Is.EqualTo("one two..."));
            Assert.That(TruncateWords("one  two\tthree\nfour", 3), Is.EqualTo("one two three..."));
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

        [Test]
        public void TestSort_NestedArray()
        {
            // Tests for issue: https://github.com/dotliquid/dotliquid/issues/533
            var inputData = new[]
            {
                new { Names = new[] { "Joe", "Smith" } },
                new { Names = new[] { "Aaron", "Aaronsen" } }
            };
            //var input = Hash.FromAnonymousObject(inputData);
            var expected = new[] { "AaronAaronsen", "JoeSmith" };

            var output = Sort(inputData, "Names");

            Assert.That(output, Is.Not.Null);
            Assert.That(output, Is.EqualTo(expected).AsCollection);
        }
    }
}
