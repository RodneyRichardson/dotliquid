using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using DotLiquid.NamingConventions;

namespace DotLiquid.Tests.Filters
{
    [TestFixture]
    public class StandardFiltersV20Tests : StandardFiltersTestsBase
    {
        public override SyntaxCompatibility SyntaxCompatibilityLevel => SyntaxCompatibility.DotLiquid20;
        public override CapitalizeDelegate Capitalize => i => LegacyFilters.Capitalize(_context, i);
        public override MathDelegate Divide => (i, o) => StandardFilters.DividedBy(_context, i, o);
        public override MathDelegate Plus => (i, o) => LegacyFilters.Plus(_context, i, o);
        public override MathDelegate Minus => (i, o) => StandardFilters.Minus(_context, i, o);
        public override MathDelegate Modulo => (i, o) => StandardFilters.Modulo(_context, i, o);
        public override RemoveFirstDelegate RemoveFirst => (a, b) => LegacyFilters.RemoveFirst(a, b);
        public override ReplaceDelegate Replace => (i, s, r) => LegacyFilters.Replace(i, s, r);
        public override ReplaceFirstDelegate ReplaceFirst => (a, b, c) => LegacyFilters.ReplaceFirst(a, b, c);
        public override SliceDelegate Slice => (a, b, c) => c.HasValue ? LegacyFilters.Slice(a, b, c.Value) : LegacyFilters.Slice(a, b);
        public override SortDelegate Sort => (input, property) => LegacyFilters.Sort(input, property);
        public override SplitDelegate Split => (i, p) => LegacyFilters.Split(i, p);
        public override MathDelegate Times => (i, o) => LegacyFilters.Times(_context, i, o);
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
        public void TestCapitalizeBehavesLikeTitleize()
        {
            Assert.That(Capitalize(input: "That is one sentence."), Is.EqualTo("That Is One Sentence."));
            Assert.That(Capitalize(input: "title"), Is.EqualTo("Title"));
        }

        [Test]
        public void TestDividedByStringThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => Divide(input: "12", operand: 3));
            Assert.Throws<InvalidOperationException>(() => Divide(input: 12, operand: "3"));
        }

        [Test]
        public void TestPlusStringConcatenates()
        {
            Assert.That(Plus(input: "1", operand: 1), Is.EqualTo("11"));
            Assert.Throws<InvalidOperationException>(() => Plus(input: 1, operand: "1"));
        }

        [Test]
        public void TestMinusStringThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => Minus(input: "2", operand: 1));
            Assert.Throws<InvalidOperationException>(() => Minus(input: 2, operand: "1"));
        }

        [Test]
        public void TestModuloStringThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => Modulo(input: "3", operand: 2));
            Assert.Throws<InvalidOperationException>(() => Modulo(input: 2, operand: "2"));
        }

        [Test]
        public void TestTimesStringReplicates()
        {
            Assert.That(StandardFilters.Join((IEnumerable)Times(input: "foo", operand: 4), ""), Is.EqualTo("foofoofoofoo"));
            Assert.That(StandardFilters.Join((IEnumerable)Times(input: "3", operand: 4), ""), Is.EqualTo("3333"));
            Assert.Throws<InvalidOperationException>(() => Times(input: 3, operand: "4"));
            Assert.Throws<InvalidOperationException>(() => Times(input: "3", operand: "4"));
        }

        [Test]
        public void TestRemoveFirstRegexWorks()
        {
            Assert.That(RemoveFirst(input: "Mr. Jones", @string: "."), Is.EqualTo(expected: "r. Jones"));
            Assert.That(RemoveFirst(input: "a a a a", @string: "[Aa] "), Is.EqualTo("a a a"));
        }

        [Test]
        public void TestReplaceRegexWorks()
        {
            Assert.That(actual: Replace(input: "a A A a", @string: "[Aa]", replacement: "b"), Is.EqualTo(expected: "b b b b"));
        }

        [Test]
        public void TestReplaceFirstRegexWorks()
        {
            Assert.That(ReplaceFirst(input: "a A A a", @string: "[Aa]", replacement: "b"), Is.EqualTo(expected: "b A A a"));
        }

        [Test]
        public void TestSort()
        {
            var ints = new[] { 10, 3, 2, 1 };
            Assert.That(Sort(null), Is.EqualTo(null));
            Assert.That(Sort(new string[] { }), Is.EqualTo(new string[] { }).AsCollection);
            Assert.That(Sort(ints), Is.EqualTo(new[] { 1, 2, 3, 10 }).AsCollection);
            Assert.That(Sort(new[] { new { a = 10 }, new { a = 3 }, new { a = 1 }, new { a = 2 } }, "a"), Is.EqualTo(new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 10 } }).AsCollection);

            // Issue #393 - Incorrect (Case-Insensitve) Alphabetic Sort
            var strings = new[] { "zebra", "octopus", "giraffe", "Sally Snake" };
            Assert.That(Sort(strings), Is.EqualTo(new[] { "giraffe", "octopus", "Sally Snake", "zebra" }).AsCollection);

            var hashes = new List<Hash>();
            for (var i = 0; i < strings.Length; i++)
                hashes.Add(CreateHash(ints[i], strings[i]));
            Assert.That(Sort(hashes, "content"), Is.EqualTo(new[] { hashes[2], hashes[1], hashes[3], hashes[0] }).AsCollection);
            Assert.That(Sort(hashes, "sortby"), Is.EqualTo(new[] { hashes[3], hashes[2], hashes[1], hashes[0] }).AsCollection);
        }

        [Test]
        public void TestSort_OnHashList_WithProperty_DoesNotFlattenList()
        {
            var list = new List<Hash>();
            var hash1 = CreateHash(1, "Text1");
            var hash2 = CreateHash(2, "Text2");
            var hash3 = CreateHash(3, "Text3");
            list.Add(hash3);
            list.Add(hash1);
            list.Add(hash2);

            var result = Sort(list, "sortby").Cast<Hash>().ToArray();
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result[0]["content"], Is.EqualTo(hash1["content"]));
            Assert.That(result[1]["content"], Is.EqualTo(hash2["content"]));
            Assert.That(result[2]["content"], Is.EqualTo(hash3["content"]));
        }

        [Test]
        public void TestSort_OnDictionaryWithPropertyOnlyInSomeElement_ReturnsSortedDictionary()
        {
            var list = new List<Hash>();
            var hash1 = CreateHash(1, "Text1");
            var hash2 = CreateHash(2, "Text2");
            var hashWithNoSortByProperty = new Hash();
            hashWithNoSortByProperty.Add("content", "Text 3");
            list.Add(hash2);
            list.Add(hashWithNoSortByProperty);
            list.Add(hash1);

            var result = Sort(list, "sortby").Cast<Hash>().ToArray();
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result[0]["content"], Is.EqualTo(hashWithNoSortByProperty["content"]));
            Assert.That(result[1]["content"], Is.EqualTo(hash1["content"]));
            Assert.That(result[2]["content"], Is.EqualTo(hash2["content"]));
        }

        [Test]
        public void TestSort_Indexable()
        {
            var packages = new[] {
                new Package(numberOfPiecesPerPackage: 2, test: "p1"),
                new Package(numberOfPiecesPerPackage: 1, test: "p2"),
                new Package(numberOfPiecesPerPackage: 3, test: "p3"),
            };
            var expectedPackages = packages.OrderBy(p => p["numberOfPiecesPerPackage"]).ToArray();

            Helper.LockTemplateStaticVars(new RubyNamingConvention(), () =>
            {
                Assert.That(
                    actual: Sort(packages, "numberOfPiecesPerPackage"), Is.EqualTo(expected: expectedPackages).AsCollection);
            });
        }

        [Test]
        public void TestSort_ExpandoObject()
        {
            dynamic package1 = new ExpandoObject();
            package1.numberOfPiecesPerPackage = 2;
            package1.test = "p1";
            dynamic package2 = new ExpandoObject();
            package2.numberOfPiecesPerPackage = 1;
            package2.test = "p2";
            dynamic package3 = new ExpandoObject();
            package3.numberOfPiecesPerPackage = 3;
            package3.test = "p3";
            var packages = new List<ExpandoObject> { package1, package2, package3 };
            var expectedPackages = new List<ExpandoObject> { package2, package1, package3 };

            Assert.That(
                actual: Sort(packages, property: "numberOfPiecesPerPackage"), Is.EqualTo(expected: expectedPackages));
        }
    }
}
