using System;
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
        public override RoundDelegate Round => (i, p) => StandardFilters.Round(_context, i, p);
        public override SingleInputDelegate Abs => i => StandardFilters.Abs(_context, i);
        public override SingleInputDelegate Ceil => i => StandardFilters.Ceil(_context, i);
        public override SingleInputDelegate Floor => i => StandardFilters.Floor(_context, i);
        public override SliceDelegate Slice => (a, b, c) => c.HasValue ? StandardFilters.Slice(a, b, c.Value) : StandardFilters.Slice(a, b);
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
        public void TestRoundHandlesBadParams()
        {
            Assert.That(Round("1.2345678", "two"), Is.EqualTo(1m).And.TypeOf(typeof(decimal)));
            Assert.That(Round("1.2345678", "-2"), Is.EqualTo(1m).And.TypeOf(typeof(decimal)));
            Assert.That(Round(1.123456789012345678901234567890123m, 50),
                Is.EqualTo(1.1234567890123456789012345679m).And.TypeOf(typeof(decimal))); // max = 28 places
            Assert.That(Round("1.2345678", "2.7"), Is.EqualTo(1.23m).And.TypeOf(typeof(decimal)));

            Assert.That(Round("1.2345678", 2.7), Is.EqualTo(1.23m));

            Helper.AssertTemplateResult("1.23", "{{ 1.234678 | round: 2.7 }}", syntax: SyntaxCompatibilityLevel);
            Helper.AssertTemplateResult("1.235", "{{ 1.234678 | round: 3.1 }}", syntax: SyntaxCompatibilityLevel);

            Helper.AssertTemplateResult("1", "{{ 1.234678 | round: -3 }}", syntax: SyntaxCompatibilityLevel);
        }

        [Test]
        public void TestAbsFloatingPointTypes()
        {
            Assert.That(Abs("notNumber"), Is.EqualTo(0).And.TypeOf(typeof(int)));
            Assert.That(Abs(10), Is.EqualTo(10).And.TypeOf(typeof(int)));
            Assert.That(Abs(-5), Is.EqualTo(5).And.TypeOf(typeof(int)));
            Assert.That(Abs(19.86), Is.EqualTo(19.86).And.TypeOf(typeof(double)));
            Assert.That(Abs(-19.86m), Is.EqualTo(19.86m).And.TypeOf(typeof(decimal)));
            Assert.That(Abs(-19.86), Is.EqualTo(19.86).And.TypeOf(typeof(double)));
            Assert.That(Abs("10"), Is.EqualTo(10).And.TypeOf(typeof(int)));
            Assert.That(Abs("-5"), Is.EqualTo(5).And.TypeOf(typeof(int)));
            Assert.That(Abs("30.60"), Is.EqualTo(30.60).And.TypeOf(typeof(decimal)));
            Assert.That(Abs("30.60a"), Is.EqualTo(0).And.TypeOf(typeof(int)));
            Assert.That(Abs(null), Is.EqualTo(0).And.TypeOf(typeof(int)));
        }

        [Test]
        public void TestCeilFloatingPointTypes()
        {
            Assert.That(Ceil(1.9), Is.EqualTo(2).And.TypeOf(typeof(double)));
            Assert.That(Ceil(1.9m), Is.EqualTo(2).And.TypeOf(typeof(decimal)));
            Assert.That(Ceil("1.9"), Is.EqualTo(2).And.TypeOf(typeof(decimal)));
        }

        [Test]
        public void TestCeilBadInput()
        {
            Assert.That(Ceil(null), Is.EqualTo(0).And.TypeOf(typeof(int)));
            Assert.That(Ceil(""), Is.EqualTo(0).And.TypeOf(typeof(int)));
            Assert.That(Ceil("two"), Is.EqualTo(0).And.TypeOf(typeof(int)));

            Helper.AssertTemplateResult("0", "{{ nonesuch | ceil }}", syntax: SyntaxCompatibilityLevel);
        }

        [Test]
        public void TestFloorFloatingPointTypes()
        {
            Assert.That(Floor(1.9), Is.EqualTo(1).And.TypeOf(typeof(double)));
            Assert.That(Floor(1.9m), Is.EqualTo(1).And.TypeOf(typeof(decimal)));
            Assert.That(Floor("1.9"), Is.EqualTo(1).And.TypeOf(typeof(decimal)));
        }

        [Test]
        public void TestFloorBadInput()
        {
            Assert.That(Floor(null), Is.EqualTo(0).And.TypeOf(typeof(int)));
            Assert.That(Floor(""), Is.EqualTo(0).And.TypeOf(typeof(int)));
            Assert.That(Floor("two"), Is.EqualTo(0).And.TypeOf(typeof(int)));

            Helper.AssertTemplateResult("0", "{{ nonesuch | floor }}", syntax: SyntaxCompatibilityLevel);
        }
    }
}
