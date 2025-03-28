using System;
using System.Globalization;
using NUnit.Framework;

namespace DotLiquid.Tests.Filters
{
    [TestFixture]
    public class StandardFiltersV21Tests : StandardFiltersTestsBase
    {
        public override SyntaxCompatibility SyntaxCompatibilityLevel => SyntaxCompatibility.DotLiquid21;
        public override CapitalizeDelegate Capitalize => i => LegacyFilters.CapitalizeV21(i);
        public override MathDelegate Divide => (i, o) => StandardFilters.DividedBy(_context, i, o);
        public override MathDelegate Plus => (i, o) => StandardFilters.Plus(_context, i, o);
        public override MathDelegate Minus => (i, o) => StandardFilters.Minus(_context, i, o);
        public override MathDelegate Modulo => (i, o) => StandardFilters.Modulo(_context, i, o);
        public override RemoveFirstDelegate RemoveFirst => (a, b) => LegacyFilters.RemoveFirstV21(a, b);
        public override ReplaceDelegate Replace => (i, s, r) => StandardFilters.Replace(i, s, r);
        public override ReplaceFirstDelegate ReplaceFirst => (a, b, c) => LegacyFilters.ReplaceFirstV21(a, b, c);
        public override RoundDelegate Round => (i, p) => LegacyFilters.Round(i, p);
        public override SingleInputDelegate Abs => i => LegacyFilters.Abs(_context, i);
        public override SingleInputDelegate Ceil => i => LegacyFilters.Ceil(_context, i);
        public override SingleInputDelegate Floor => i => LegacyFilters.Floor(_context, i);
        public override SliceDelegate Slice => (a, b, c) => c.HasValue ? LegacyFilters.Slice(a, b, c.Value) : LegacyFilters.Slice(a, b);
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
        public void TestCapitalizeBehavesLikeUpcaseFirst()
        {
            Assert.That(Capitalize(input: " my boss is Mr. Doe."), Is.EqualTo(" My boss is Mr. Doe."));
            Assert.That(Capitalize(input: "my great title"), Is.EqualTo("My great title"));
        }

        [Test]
        public void TestDividedByStringIsParsed()
        {
            Assert.That(Divide(input: "12", operand: 3), Is.EqualTo(4));
            Assert.That(Divide(input: 12, operand: "3"), Is.EqualTo(4));
        }

        [Test]
        public void TestPlusStringAdds()
        {
            Assert.That(Plus(input: "1", operand: 1), Is.EqualTo(2));
            Assert.That(Plus(input: 1, operand: "1"), Is.EqualTo(2));
            Assert.That(Plus(input: "1", operand: "1"), Is.EqualTo(2));
            Assert.That(Plus(input: 2, operand: "3.5"), Is.EqualTo(5.5));
            Assert.That(Plus(input: "3.5", operand: 2), Is.EqualTo(5.5));
        }

        [Test]
        public void TestMinusStringIsParsed()
        {
            Assert.That(Minus(input: "2", operand: 1), Is.EqualTo(1));
            Assert.That(Minus(input: 2, operand: 1), Is.EqualTo(1));
            Assert.That(Minus(input: 2, operand: 3.5), Is.EqualTo(-1.5));
            Assert.That(Minus(input: "2.5", operand: 4), Is.EqualTo(-1.5));
            Assert.That(Minus(input: "2.5", operand: "3.5"), Is.EqualTo(-1));
        }

        [Test]
        public void TestModuloStringIsParsed()
        {
            Assert.That(Modulo(input: "3", operand: 2), Is.EqualTo(1));
            Assert.That(Modulo(input: 3, operand: "2"), Is.EqualTo(1));
        }

        [Test]
        public void TestTimesStringIsParsed()
        {
            Assert.That(Times(input: "3", operand: 4), Is.EqualTo(12));
            Assert.That(Times(input: 3, operand: "4"), Is.EqualTo(12));
            Assert.That(Times(input: "3", operand: "4"), Is.EqualTo(12));
        }

        [Test]
        public void TestRemoveFirstRegexFails()
        {
            Assert.That(RemoveFirst(input: "Mr. Jones", @string: "."), Is.EqualTo(expected: "Mr Jones"));
            Assert.That(RemoveFirst(input: "a a a a", @string: "[Aa] "), Is.EqualTo("a a a a"));
        }

        [Test]
        public void TestReplaceRegexFails()
        {
            Assert.That(Replace(input: "a A A a", @string: "[Aa]", replacement: "b"), Is.EqualTo(expected: "a A A a"));
        }

        [Test]
        public void TestReplaceFirstRegexFails()
        {
            Assert.That(ReplaceFirst(input: "a A A a", @string: "[Aa]", replacement: "b"), Is.EqualTo(expected: "a A A a"));
        }

        [Test]
        public void TestRoundHandlesBadParams()
        {
            Assert.That(Round("1.2345678", "two"), Is.Null);
            Assert.That(Round("1.2345678", -2), Is.Null);
            Assert.That(Round(1.123456789012345678901234567890123m, 50), Is.Null);

            Assert.That(Round("1.2345678", 2.7), Is.EqualTo(1.235m));

            Helper.AssertTemplateResult("1.235", "{{ 1.234678 | round: 2.7 }}", syntax: SyntaxCompatibilityLevel);
            Helper.AssertTemplateResult("1.235", "{{ 1.234678 | round: 3.1 }}", syntax: SyntaxCompatibilityLevel);

            Helper.AssertTemplateResult("", "{{ 1.234678 | round: -3 }}", syntax: SyntaxCompatibilityLevel);
        }

        [Test]
        public void TestAbsFloatingPointTypes()
        {
            Assert.That(Abs(10), Is.EqualTo(10).And.TypeOf(typeof(double)));
            Assert.That(Abs("10"), Is.EqualTo(10).And.TypeOf(typeof(double)));
            Assert.That(Abs(-19.86m), Is.EqualTo(19.86).And.TypeOf(typeof(double)));
            Assert.That(Abs("30.60"), Is.EqualTo(30.60).And.TypeOf(typeof(double)));
            Assert.That(Abs("30.60a"), Is.EqualTo(0).And.TypeOf(typeof(double)));
            Assert.That(Abs(null), Is.EqualTo(0).And.TypeOf(typeof(double)));
        }

        [Test]
        public void TestCeilFloatingPointTypes()
        {
            Assert.That(Ceil(1.9), Is.EqualTo(2).And.TypeOf(typeof(decimal)));
            Assert.That(Ceil(1.9m), Is.EqualTo(2).And.TypeOf(typeof(decimal)));
            Assert.That(Ceil("1.9"), Is.EqualTo(2).And.TypeOf(typeof(decimal)));
        }

        [Test]
        public void TestCeilBadInput()
        {
            Assert.That(Ceil(null), Is.Null);
            Assert.That(Ceil(""), Is.Null);
            Assert.That(Ceil("two"), Is.Null);

            Helper.AssertTemplateResult("", "{{ nonesuch | ceil }}", syntax: SyntaxCompatibilityLevel);
        }

        [Test]
        public void TestFloorFloatingPointTypes()
        {
            Assert.That(Floor(1.9), Is.EqualTo(1).And.TypeOf(typeof(decimal)));
            Assert.That(Floor(1.9m), Is.EqualTo(1).And.TypeOf(typeof(decimal)));
            Assert.That(Floor("1.9"), Is.EqualTo(1).And.TypeOf(typeof(decimal)));
        }

        [Test]
        public void TestFloorBadInput()
        {
            Assert.That(Floor(null), Is.Null);
            Assert.That(Floor(""), Is.Null);
            Assert.That(Floor("two"), Is.Null);

            Helper.AssertTemplateResult("", "{{ nonesuch | floor }}", syntax: SyntaxCompatibilityLevel);
        }
    }
}
