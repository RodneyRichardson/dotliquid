using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using DotLiquid.Util;

namespace DotLiquid
{
    /// <summary>
    /// Standard Liquid filters
    /// </summary>
    /// <see href="https://shopify.github.io/liquid/filters/"/>
    public static class StandardFilters
    {
        private static readonly Lazy<Regex> StripHtmlBlocks = new Lazy<Regex>(() => R.C(@"<script.*?</script>|<!--.*?-->|<style.*?</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase), LazyThreadSafetyMode.ExecutionAndPublication);
        private static readonly Lazy<Regex> StripHtmlTags = new Lazy<Regex>(() => R.C(@"<.*?>", RegexOptions.Singleline), LazyThreadSafetyMode.ExecutionAndPublication);
        private static string Space = " ";
#if NETSTANDARD1_3
        private class StringAwareObjectComparer : IComparer
        {
            private readonly StringComparer _stringComparer;

            public StringAwareObjectComparer(StringComparer stringComparer)
            {
                _stringComparer = stringComparer;
            }

            public int Compare(Object x, Object y)
            {
                if (x == y)
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;

                if (x is string textX && y is string textY)
                    return _stringComparer.Compare(textX, textY);

                return Comparer<object>.Default.Compare(x, y);
            }
        }
#endif

        /// <summary>
        /// Return the size of an array or of an string
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static int Size(object input)
        {
            if (input is string stringInput)
            {
                return stringInput.Length;
            }
            if (input is IEnumerable enumerableInput)
            {
                return enumerableInput.Cast<object>().Count();
            }
            return 0;
        }

        /// <summary>
        /// Returns a substring of one character or series of array items beginning at the index specified by the first argument.
        /// </summary>
        /// <param name="input">The input to be sliced</param>
        /// <param name="offset">zero-based start position of string or array, negative values count back from the end of the string/array.</param>
        /// <param name="length">An optional argument specifies the length of the substring or number of array items to be returned</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid22a)]
        public static object Slice(object input, int offset, int length = 1)
        {
            if (input is IEnumerable enumerableInput)
            {
                var inputSize = Size(input);
                var skip = offset;
                var take = length;

                // Check if the offset is specified from the end of the string/array
                if (offset < 0)
                {
                    if (Math.Abs(offset) < inputSize)
                    {
                        skip = inputSize + offset;
                    }
                    else
                    {
                        // the required slice starts before element zero of the string/array
                        skip = 0;
                        take = inputSize + offset + length;
                    }
                }

                return enumerableInput.Cast<object>().Skip(skip).Take<object>(take);
            }

            return input == null ? string.Empty : input;
        }

        /// <summary>
        /// convert a input string to DOWNCASE
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string Downcase(string input)
        {
            return input == null ? input : input.ToLower();
        }

        /// <summary>
        /// convert a input string to UPCASE
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string Upcase(string input)
        {
            return input == null
                ? input
                : input.ToUpper();
        }

        /// <summary>
        /// convert a input string to URLENCODE
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string UrlEncode(string input)
        {
            return input == null
                ? input
                : System.Net.WebUtility.UrlEncode(input);
        }

        /// <summary>
        /// convert a input string to URLDECODE
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string UrlDecode(string input)
        {
            return input == null
                ? input
                : System.Net.WebUtility.UrlDecode(input);
        }

        /// <summary>
        /// capitalize words in the input sentence
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid22)]
        public static string Capitalize(string input)
        {
            if (input.IsNullOrWhiteSpace())
                return input;

            var trimmed = input.TrimStart();
            return input.Substring(0, input.Length - trimmed.Length) + char.ToUpper(trimmed[0]) + trimmed.Substring(1).ToLower();
        }

        /// <summary>
        /// Escape html chars
        /// </summary>
        /// <param name="input">String to escape</param>
        /// <returns>Escaped string</returns>
        /// <remarks>Alias of H</remarks>
        [LiquidFilter(Name = nameof(Escape), Alias = "H")]
        public static string Escape(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                return WebUtility.HtmlEncode(input);
            }
            catch
            {
                return input;
            }
        }

        /// <summary>
        /// Escapes a string without changing existing escaped entities.
        /// It doesn’t change strings that don’t have anything to escape.
        /// </summary>
        /// <param name="input">String to escape</param>
        /// <returns>Escaped string</returns>
        /// <see href="https://shopify.github.io/liquid/filters/escape_once/"/>
        public static string EscapeOnce(string input)
        {
            return string.IsNullOrEmpty(input) ? input : WebUtility.HtmlEncode(WebUtility.HtmlDecode(input));
        }

        /// <summary>
        /// Truncates a string down to x characters
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="length">optional maximum length of returned string, defaults to 50</param>
        /// <param name="truncateString">Optional suffix to append when string is truncated, defaults to ellipsis(...)</param>
        public static string Truncate(string input, int length = 50, string truncateString = "...")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (length < 0)
            {
                return truncateString;
            }

            var lengthExcludingTruncateString = truncateString == null ? length : length - truncateString.Length;
            return input.Length > length
                ? input.Substring(startIndex: 0, length: lengthExcludingTruncateString < 0 ? 0 : lengthExcludingTruncateString) + truncateString
                : input;
        }

        /// <summary>
        /// Truncate a string down to x words
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="words">optional maximum number of words in returned string, defaults to 15</param>
        /// <param name="truncateString">Optional suffix to append when string is truncated, defaults to ellipsis(...)</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24, Alias = "Truncatewords")]
        public static string TruncateWords(string input, int words = 15, string truncateString = "...")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (words <= 0)
                words = 1;

            // Split to an array using any ascii whitespace as noted in the StandardFilters.Split method.
            var wordArray = input.Split(Liquid.AsciiWhitespaceChars, words + 1, StringSplitOptions.RemoveEmptyEntries);
            return wordArray.Length > words
                ? string.Join(separator: Space, values: wordArray.Take(words)) + truncateString
                : input;
        }

        /// <summary>
        /// Split input string into an array of substrings separated by given pattern, eliminating empty entries at the end.
        /// </summary>
        /// <remarks>
        /// <para>If <paramref name="input"/> is null or empty, an empty array is returned.</para>
        /// <para>If <paramref name="pattern"/> is null or empty, the input string is converted to an array of single-character strings.</para>
        /// </remarks>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="pattern">separator string</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static string[] Split(string input, string pattern)
        {
            if (string.IsNullOrEmpty(input))
                return new string[] { };

            // If the pattern is empty convert to an array as specified in the Liquid Reverse filter example.
            // See: https://shopify.github.io/liquid/filters/reverse/
            return string.IsNullOrEmpty(pattern)
                ? input.ToCharArray().Select(character => character.ToString()).ToArray()
                : input.Split(new[] { pattern }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Strip all html nodes from input
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string StripHtml(string input)
        {
            return input.IsNullOrWhiteSpace()
                ? input
                : StripHtmlTags.Value.Replace(StripHtmlBlocks.Value.Replace(input, string.Empty), string.Empty);
        }

        /// <summary>
        /// Strip all whitespace from input
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string Strip(string input)
        {
            return input?.Trim();
        }

        /// <summary>
        /// Strip all leading whitespace from input
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string Lstrip(string input)
        {
            return input?.TrimStart();
        }

        /// <summary>
        /// Strip all trailing whitespace from input
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string Rstrip(string input)
        {
            return input?.TrimEnd();
        }

        /// <summary>
        /// Converts the input object into a formatted currency as specified by the context cuture, or languageTag parameter (if provided).
        /// </summary>
        /// <remarks>
        /// If the input is a string it is ALWAYS parsed using the context culture, the optional languageTag parameter is only applied for rendering.
        /// </remarks>
        /// <param name="context">default source of culture information</param>
        /// <param name="input">value to be parsed and formatted as a Currency</param>
        /// <param name="languageTag">optional override language for rendering, for example 'fr-FR'</param>
        /// <seealso href="https://shopify.dev/api/liquid/filters/money-filters#money">Shopify Money filter</seealso>
        public static string Currency(Context context, object input, string languageTag = null)
        {
            // Check for null input, return null
            if (input == null) return null;

            // Check for null only, allow an empty string as it represent the InvariantCulture
            var culture = languageTag == null ? context.CurrentCulture : new CultureInfo(languageTag.Trim());

            // Attempt to convert to a currency using the context current culture.
            if (IsReal(input))
                return Convert.ToDecimal(input).ToString("C", culture);
            if (decimal.TryParse(input.ToString(), NumberStyles.Currency, context.CurrentCulture, out decimal amount))
                return amount.ToString("C", culture);

            return input.ToString();
        }

        /// <summary>
        /// Remove all newlines from the string
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string StripNewlines(string input)
        {
            return input.IsNullOrWhiteSpace()
                ? input
                : Regex.Replace(input, @"(\r?\n)", string.Empty, RegexOptions.None, Template.RegexTimeOut);
        }

        /// <summary>
        /// Join elements of the array with a certain character between them
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="glue">separator to be inserted between array elements</param>
        public static string Join(IEnumerable input, string glue = " ")
        {
            if (input == null)
                return null;

            IEnumerable<object> castInput = input.Cast<object>();

            return string.Join(glue, castInput);
        }

        /// <summary>
        /// Sort elements of the array
        /// </summary>
        /// <param name="input">The object to sort</param>
        /// <param name="property">Optional property with which to sort an array of hashes or drops</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid22)]
        public static IEnumerable Sort(object input, string property = null) => SortInternal(StringComparer.Ordinal, input, property);

        /// <summary>
        /// Sort elements of the array in case-insensitive order
        /// </summary>
        /// <param name="input">The object to sort</param>
        /// <param name="property">Optional property with which to sort an array of hashes or drops</param>
        public static IEnumerable SortNatural(object input, string property = null)
        {
            return SortInternal(StringComparer.OrdinalIgnoreCase, input, property);
        }

        internal static IEnumerable SortInternal(StringComparer stringComparer, object input, string property = null)
        {
            if (input == null)
                return null;

            List<object> ary;
            if (input is IEnumerable<Hash> enumerableHash && !string.IsNullOrEmpty(property))
                ary = enumerableHash.Cast<object>().ToList();
            else if (input is IEnumerable enumerableInput)
                ary = enumerableInput.Flatten().Cast<object>().ToList();
            else
            {
                ary = new List<object>(new[] { input });
            }

            if (!ary.Any())
                return ary;

#if NETSTANDARD1_3
            var comparer = new StringAwareObjectComparer(stringComparer);
#else
            var comparer = stringComparer;
#endif 

            if (string.IsNullOrEmpty(property))
            {
                ary.Sort((a, b) => comparer.Compare(a, b));
            }
            else
            {
                ary.Sort((a, b) =>
                {
                    var aPropertyValue = ResolveObjectPropertyValue(a, property);
                    var bPropertyValue = ResolveObjectPropertyValue(b, property);
                    return comparer.Compare(aPropertyValue, bPropertyValue);
                });
            }

            return ary;
        }

        /// <summary>
        /// Map/collect on a given property
        /// </summary>
        /// <param name="enumerableInput">The enumerable.</param>
        /// <param name="property">The property to map.</param>
        public static IEnumerable Map(IEnumerable enumerableInput, string property)
        {
            if (enumerableInput == null)
                return null;

            // Enumerate to a list so we can repeatedly parse through the collection.
            List<object> listedInput = enumerableInput.Cast<object>().ToList();

            // If the list happens to be empty we are done already.
            if (!listedInput.Any())
                return listedInput;

            // Note that liquid assumes that contained complex elements are all following the same schema.
            // Hence here we only check if the first element has the property requested for the map.
            if (listedInput.All(element => element is IDictionary)
                && ((IDictionary)listedInput.First()).Contains(key: property))
                return listedInput.Select(element => ((IDictionary)element)[property]);

            return listedInput.Select(element => ResolveObjectPropertyValue(element, property));
        }

        /// <summary>
        /// Replaces every occurrence of the first argument in a string with the second argument
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">Substring to be replaced</param>
        /// <param name="replacement">Replacement string to be inserted</param>
        [LiquidFilter(Name = nameof(Replace), MinVersion = SyntaxCompatibility.DotLiquid21)]
        public static string Replace(string input, string @string, string replacement = "")
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(@string))
                return input;

            return input.Replace(@string, replacement);
        }

        /// <summary>
        /// Replace the first occurrence of a string with another
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">Substring to be replaced</param>
        /// <param name="replacement">Replacement string to be inserted</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static string ReplaceFirst(string input, string @string, string replacement = "")
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (string.IsNullOrEmpty(@string))
                return input.Insert(0, replacement ?? string.Empty);

            int position = input.IndexOf(@string);
            return position < 0 ? input : input.Remove(position, @string.Length).Insert(position, replacement ?? string.Empty);
        }

        /// <summary>
        /// Replace the last occurrence of a string with another
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">Substring to be replaced</param>
        /// <param name="replacement">Replacement string to be inserted</param>
        public static string ReplaceLast(string input, string @string, string replacement)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (string.IsNullOrEmpty(@string))
                return input.Insert(input.Length, replacement ?? string.Empty);

            int position = input.LastIndexOf(@string);
            return position < 0 ? input : input.Remove(position, @string.Length).Insert(position, replacement ?? string.Empty);
        }

        /// <summary>
        /// Removes every occurrence of the specified substring from a string.
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">String to be removed from input</param>
        public static string Remove(string input, string @string)
        {
            return input.IsNullOrWhiteSpace()
                ? input
                : input.Replace(@string, string.Empty);
        }

        /// <summary>
        /// Remove the first occurrence of a substring
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">String to be removed from input</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static string RemoveFirst(string input, string @string) => ReplaceFirst(input: input, @string: @string, replacement: string.Empty);

        /// <summary>
        /// Remove the last occurrence of a substring
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">String to be removed from input</param>
        public static string RemoveLast(string input, string @string) => ReplaceLast(input: input, @string: @string, replacement: string.Empty);

        /// <summary>
        /// Add one string to another
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">String to be added to the end of input</param>
        public static string Append(string input, string @string) => $"{input}{@string}";

        /// <summary>
        /// Prepend a string to another
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="string">String to be added to the beginning of input</param>
        public static string Prepend(string input, string @string) => $"{@string}{input}";

        /// <summary>
        /// Add <br /> tags in front of all newlines in input string
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static string NewlineToBr(string input)
        {
            return input.IsNullOrWhiteSpace()
                    ? input
                    : Regex.Replace(input, @"(\r?\n)", "<br />$1", RegexOptions.None, Template.RegexTimeOut);
        }

        /// <summary>
        /// Formats a date using a .NET date format string
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="format">Date format to be applied</param>
        /// <see cref="Liquid.UseRubyDateFormat">See UseRubyFormat for guidance on .NET vs. Ruby format support</see>
        public static string Date(Context context, object input, string format)
        {
            if (input == null)
                return null;

            if (input is DateTime date)
            {
                if (format.IsNullOrWhiteSpace())
                    return date.ToString(context.CurrentCulture);

                return context.UseRubyDateFormat
                    ? context.SyntaxCompatibilityLevel >= SyntaxCompatibility.DotLiquid21 ? new DateTimeOffset(date).ToStrFTime(format, context.CurrentCulture) : date.ToStrFTime(format, context.CurrentCulture)
                    : date.ToString(format, context.CurrentCulture);
            }

#if NET6_0_OR_GREATER
            if (input is DateOnly dateOnly)
            {
                if (format.IsNullOrWhiteSpace())
                    return dateOnly.ToString(context.CurrentCulture);
                return context.UseRubyDateFormat ? dateOnly.ToStrFTime(format, context.CurrentCulture) : dateOnly.ToString(format, context.CurrentCulture);
            }

            if (input is TimeOnly timeOnly)
            {
                if (format.IsNullOrWhiteSpace())
                    return timeOnly.ToString(context.CurrentCulture);
                return context.UseRubyDateFormat ? timeOnly.ToStrFTime(format, context.CurrentCulture) : timeOnly.ToString(format, context.CurrentCulture);
            }
#endif

            if (context.SyntaxCompatibilityLevel == SyntaxCompatibility.DotLiquid20)
                return DateLegacyParsing(context, input.ToString(), format);

            if (format.IsNullOrWhiteSpace())
                return input.ToString();

            DateTimeOffset dateTimeOffset;
            if (input is DateTimeOffset inputOffset)
            {
                dateTimeOffset = inputOffset;
            }
            else if ((input is decimal) || (input is double) || (input is float) || (input is int) || (input is uint) || (input is long) || (input is ulong) || (input is short) || (input is ushort))
            {
#if CORE
                dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(input)).ToLocalTime();
#else
                dateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(Convert.ToDouble(input)).ToLocalTime();
#endif
            }
            else
            {
                string value = input.ToString();

                if (string.Equals(value, "now", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "today", StringComparison.OrdinalIgnoreCase))
                {
                    dateTimeOffset = DateTimeOffset.Now;
                }
                else if (!DateTimeOffset.TryParse(value, context.CurrentCulture, DateTimeStyles.None, out dateTimeOffset))
                {
                    return value;
                }
            }

            return context.UseRubyDateFormat
                ? dateTimeOffset.ToStrFTime(format, context.CurrentCulture)
                : dateTimeOffset.ToString(format, context.CurrentCulture);
        }

        private static string DateLegacyParsing(Context context, string value, string format)
        {
            DateTime date;

            if (string.Equals(value, "now", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "today", StringComparison.OrdinalIgnoreCase))
            {
                date = DateTime.Now;

                if (format.IsNullOrWhiteSpace())
                    return date.ToString(context.CurrentCulture);
            }
            else if (!DateTime.TryParse(value, context.CurrentCulture, DateTimeStyles.None, out date))
            {
                return value;
            }

            if (format.IsNullOrWhiteSpace())
                return value;

            return context.UseRubyDateFormat ? date.ToStrFTime(format, context.CurrentCulture) : date.ToString(format, context.CurrentCulture);
        }

        /// <summary>
        /// Get the first element of the passed in array
        ///
        /// Example:
        ///   {{ product.images | first | to_img }}
        /// </summary>
        /// <param name="array"></param>
        public static object First(IEnumerable array)
        {
            if (array == null)
                return null;

            return array.Cast<object>().FirstOrDefault();
        }

        /// <summary>
        /// Get the last element of the passed in array
        ///
        /// Example:
        ///   {{ product.images | last | to_img }}
        /// </summary>
        /// <param name="array"></param>
        public static object Last(IEnumerable array)
        {
            if (array == null)
                return null;

            return array.Cast<object>().LastOrDefault();
        }

        /// <summary>
        /// Addition
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="operand">Number to be added to input</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid21)]
        public static object Plus(Context context, object input, object operand)
        {
            return DoMathsOperation(context, input, operand, Expression.AddChecked);
        }

        /// <summary>
        /// Subtraction
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="operand">Number to be subtracted from input</param>
        public static object Minus(Context context, object input, object operand)
        {
            return DoMathsOperation(context, input, operand, Expression.SubtractChecked);
        }

        /// <summary>
        /// Multiplication
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="operand">Number to multiple input by</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid21)]
        public static object Times(Context context, object input, object operand) => DoMathsOperation(context, input, operand, Expression.MultiplyChecked);

        /// <summary>
        /// Rounds a decimal value to the specified places
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="places">Number of decimal places for rounding</param>
        /// <returns>The rounded value; zero if input is invalid, or rounded to 0 decimals if places is invalid</returns>
        /// <remarks>Behaviour differs from Ruby implementation for negative places values.
        /// This will treat it as any other invalid places value, and round to closest integer.</remarks>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static object Round(Context context, object input, object places = null)
        {
            int decimals = 0;
            if (decimal.TryParse(places?.ToString(), NumberStyles.Any, context.CurrentCulture, out decimal placesValue))
            {
                const decimal MinDecimalPlaces = 0m;
                const decimal MaxDecimalPlaces = 28m;
                placesValue = Math.Max(MinDecimalPlaces, Math.Min(MaxDecimalPlaces, placesValue));
                decimals = (int)Math.Floor(placesValue);
            }

            if (decimal.TryParse(input?.ToString(), NumberStyles.Any, context.CurrentCulture, out decimal inputValue))
            {
                return Math.Round(inputValue, decimals);
            }

            return 0m;
        }

        /// <summary>
        /// Rounds a decimal value up to the next integer, unless already the integer value, removing all decimal places 
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <returns>The rounded value; zero if an exception has occurred</returns>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static object Ceil(Context context, object input)
        {
            if (input == null) return 0;

            if (input is string inputString)
            {
                input = inputString.CoerceToNumericType(context.FormatProvider, 0);
            }

            if (input is decimal inputDecimal) { return Math.Ceiling(inputDecimal); }
            else if (input is double inputDouble) { return Math.Ceiling(inputDouble); }
            else if (input is int inputInt32) { return inputInt32; }
            else if (input is long inputInt64) { return inputInt64; }
            else return 0;
        }

        /// <summary>
        /// Rounds a decimal value down to an integer, removing all decimal places 
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <returns>The rounded value; zero if an exception has occurred</returns>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static object Floor(Context context, object input)
        {
            if (input == null) return 0;

            if (input is string inputString)
            {
                input = inputString.CoerceToNumericType(context.FormatProvider, 0);
            }

            if (input is decimal inputDecimal) { return Math.Floor(inputDecimal); }
            else if (input is double inputDouble) { return Math.Floor(inputDouble); }
            else if (input is int inputInt32) { return inputInt32; }
            else if (input is long inputInt64) { return inputInt64; }
            else return 0;
        }

        /// <summary>
        /// Division
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="operand">Number to divide input by</param>
        public static object DividedBy(Context context, object input, object operand)
        {
            return DoMathsOperation(context, input, operand, Expression.Divide);
        }

        /// <summary>
        /// Performs an arithmetic remainder operation on the input
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="operand">Number to divide input by</param>
        public static object Modulo(Context context, object input, object operand)
        {
            return DoMathsOperation(context, input, operand, Expression.Modulo);
        }

        /// <summary>
        /// If a value isn't set for a variable in the template, allow the user to specify a default value for that variable
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="defaultValue">value to apply if input is nil, false or empty.</param>
        public static string Default(string input, string @defaultValue)
        {
            return !string.IsNullOrWhiteSpace(input) ? input : defaultValue;
        }

        private static bool IsReal(object o) => o is double || o is float || o is decimal;
        private static bool IsInteger(object o) => o is int || o is uint || o is long || o is ulong || o is short || o is ushort || o is byte || o is sbyte;
        private static bool IsNumeric(object o) => IsReal(o) || IsInteger(o);

        internal static object DoMathsOperation(Context context, object input, object operand, Func<Expression, Expression, BinaryExpression> operation)
        {
            if (input == null || operand == null)
                return null;

            // NOTE(David Burg): Try for maximal precision if the input and operand fit the decimal's range.
            // This avoids rounding errors in financial arithmetic.
            // E.g.: 0.1 | Plus 10 | Minus 10 to remain 0.1, not 0.0999999999999996
            // Otherwise revert to maximum range (possible precision loss).
            var shouldConvertStrings = context.SyntaxCompatibilityLevel >= SyntaxCompatibility.DotLiquid21 && ((input is string) || (operand is string));
            if (IsReal(input) || IsReal(operand) || shouldConvertStrings)
            {
                try
                {
                    input = Convert.ToDecimal(input);
                    operand = Convert.ToDecimal(operand);

                    return ExpressionUtility
                        .CreateExpression(
                            body: operation,
                            leftType: input.GetType(),
                            rightType: operand.GetType())
                        .DynamicInvoke(input, operand);
                }
                catch (Exception ex) when (ex is OverflowException || ex is DivideByZeroException || (ex is TargetInvocationException && (ex?.InnerException is OverflowException || ex?.InnerException is DivideByZeroException)))
                {
                    input = Convert.ToDouble(input);
                    operand = Convert.ToDouble(operand);
                }
            }

            try
            {
                return ExpressionUtility
                    .CreateExpression(
                        body: operation,
                        leftType: input.GetType(),
                        rightType: operand.GetType())
                    .DynamicInvoke(input, operand);
            }
            catch (TargetInvocationException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        /// <summary>
        /// Removes any duplicate elements in an array.
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static IEnumerable Uniq(object input)
        {
            if (input == null)
                return null;

            List<object> ary;
            if (input is IEnumerable)
                ary = ((IEnumerable)input).Flatten().Cast<object>().ToList();
            else
            {
                ary = new List<object>(new[] { input });
            }

            if (!ary.Any())
                return ary;

            return ary.Distinct().ToList();
        }

        /// <summary>
        /// Returns the absolute value of a number.
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static object Abs(Context context, object input)
        {
            if (input == null) return 0;

            if (input is string inputString)
            {
                input = inputString.CoerceToNumericType(context.FormatProvider, 0);
            }

            if (input is decimal inputDecimal) { return Math.Abs(inputDecimal); }
            else if (input is double inputDouble) { return Math.Abs(inputDouble); }
            else if (input is int inputInt32) { return Math.Abs(inputInt32); }
            else if (input is long inputInt64) { return Math.Abs(inputInt64); }
            else return 0;
        }

        /// <summary>
        /// Limits a number to a minimum value.
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="atLeast">Value to apply if more than input</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static object AtLeast(Context context, object input, object atLeast)
        {
            if (!decimal.TryParse(input?.ToString(), NumberStyles.Number, context.CurrentCulture, out decimal val1))
            {
                val1 = 0m;
            }
            if (!decimal.TryParse(atLeast?.ToString(), NumberStyles.Number, context.CurrentCulture, out decimal val2))
            {
                val2 = 0m;
            }
            return Math.Max(val1, val2);
        }

        /// <summary>
        /// Limits a number to a maximum value.
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <param name="atMost">Value to apply if less than input</param>
        [LiquidFilter(MinVersion = SyntaxCompatibility.DotLiquid24)]
        public static object AtMost(Context context, object input, object atMost)
        {
            if (!decimal.TryParse(input?.ToString(), NumberStyles.Number, context.CurrentCulture, out decimal val1))
            {
                val1 = 0m;
            }
            if (!decimal.TryParse(atMost?.ToString(), NumberStyles.Number, context.CurrentCulture, out decimal val2))
            {
                val2 = 0m;
            }
            return Math.Min(val1, val2);
        }

        /// <summary>
        /// Removes any nil values from an array.
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        public static IEnumerable Compact(object input)
        {
            if (input == null)
                return null;

            List<object> ary;
            if (input is IEnumerable)
                ary = ((IEnumerable)input).Flatten().Cast<object>().ToList();
            else
            {
                ary = new List<object>(new[] { input });
            }

            if (!ary.Any())
                return ary;

            ary.RemoveAll(item => item == null);
            return ary;
        }

        /// <summary>
        /// Creates an array including only the objects with a given property value, or any truthy value by default.
        /// </summary>
        /// <param name="input">an array to be filtered</param>
        /// <param name="propertyName">The name of the property to filter by</param>
        /// <param name="targetValue">Value to retain, if null object containing this property are retained</param>
        public static IEnumerable Where(IEnumerable input, string propertyName, object targetValue = null)
        {
            if (input == null)
                return null;

            if (propertyName.IsNullOrWhiteSpace())
                throw new ArgumentNullException(paramName: nameof(propertyName), message: $"'{nameof(propertyName)}' cannot be null or empty.");

            return input.Cast<object>().Where(source => source.HasMatchingProperty(propertyName, targetValue));
        }

        /// <summary>
        /// Sums all items in an array. If <paramref name="propertyName"/> is supplied, it sums the property values./> 
        /// </summary>
        /// <param name="context">The DotLiquid context</param>
        /// <param name="input">An array of numerics values, or objects with a numeric property, to be summed.</param>
        /// <param name="propertyName">The name of a numeric property to sum. </param>
        /// <returns>The sum of the input values.</returns>
        public static object Sum(Context context, IEnumerable input, string propertyName=null)
        {
            if (input == null)
                return 0;

            // If propertyName is specified, expect a list of objects with a numeric property of the same name
            if (propertyName != null)
            {
                IEnumerable<object> values = input.Cast<object>()
                    .Select(source => source.ResolveObjectPropertyValue(propertyName));
                return Sum(context, values);
            }

            object sum = 0;
            foreach (object value in input)
            {
                if (value != null)
                {
                    object valueToAdd = 0;
                    if (IsNumeric(value))
                    {
                        valueToAdd = value;
                    }
                    else if (value is string stringValue)
                    {
                        if (int.TryParse(stringValue, NumberStyles.Integer, context.FormatProvider, out int intValue))
                        {
                            valueToAdd = intValue;
                        }
                        else if (long.TryParse(stringValue, NumberStyles.Integer, context.FormatProvider, out long longValue))
                        {
                            valueToAdd = longValue;
                        }
                        else if (decimal.TryParse(stringValue, NumberStyles.Float, context.FormatProvider, out decimal decimalValue))
                        {
                            valueToAdd = decimalValue;
                        }
                        else if (double.TryParse(stringValue, NumberStyles.Float, context.FormatProvider, out double doubleValue))
                        {
                            valueToAdd = doubleValue;
                        }
                    }

                    sum = DoMathsOperation(context, sum, valueToAdd, Expression.AddChecked);
                }
            }

            return sum;
        }

        /// <summary>
        /// Checks if the given object has a matching property name.
        /// * If targetValue is provided, then the propertyValue is compared to targetValue
        /// * If targetValue is null, then the property is checked for "Truthyness".
        /// </summary>
        /// <param name="any">an object to be assessed</param>
        /// <param name="propertyName">The name of the property to test for</param>
        /// <param name="targetValue">target property value</param>
        private static bool HasMatchingProperty(this object any, string propertyName, object targetValue)
        {
            var propertyValue = ResolveObjectPropertyValue(any, propertyName);
            return targetValue == null || propertyValue == null
                ? propertyValue.IsTruthy()
                : propertyValue.SafeTypeInsensitiveEqual(targetValue);
        }

        private static object ResolveObjectPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
                return null;
            if (obj is IDictionary dictionary && dictionary.Contains(key: propertyName))
                return dictionary[propertyName];
            if (obj is IDictionary<string, object> dictionaryObject && dictionaryObject.ContainsKey(propertyName))
                return dictionaryObject[propertyName];
            var indexable = obj as IIndexable;
            if (indexable == null)
            {
                var type = obj.GetType();
                var safeTypeTransformer = Template.GetSafeTypeTransformer(type);
                if (safeTypeTransformer != null)
                    indexable = safeTypeTransformer(obj) as DropBase;
                else
                {
                    if (DropProxy.TryFromLiquidType(obj, type, out var drop))
                    {
                        indexable = drop;
                    }
                    else if (TypeUtility.IsAnonymousType(type) && obj.GetType().GetRuntimeProperty(propertyName) != null)
                    {
                        return type.GetRuntimeProperty(propertyName).GetValue(obj, null);
                    }
                }
            }

            return (indexable?.ContainsKey(propertyName) ?? false) ? indexable[propertyName] : null;
        }

        /// <summary>
        /// Concatenates (joins together) multiple arrays.
        /// The resulting array contains all the items from the input arrays.
        /// </summary>
        /// <remarks>
        /// Will not remove duplicate entries from the concatenated array
        /// unless you also use the uniq filter.
        /// </remarks>
        /// <param name="left">left hand (start) of the new concatenated array</param>
        /// <param name="right">array to be appended to left</param>
        /// <see href="https://shopify.github.io/liquid/filters/concat/"/>
        public static IEnumerable Concat(IEnumerable left, IEnumerable right)
        {
            // If either side is null, return the other side.
            if (left == null)
                return right;
            else if (right == null)
                return left;

            return left.Cast<object>().ToList().Concat(right.Cast<object>());
        }

        /// <summary>
        /// Reverses the order of the items in an array. `reverse` cannot reverse a string.
        /// </summary>
        /// <param name="input">Input to be transformed by this filter</param>
        /// <see href="https://shopify.github.io/liquid/filters/reverse/"/>
        public static IEnumerable Reverse(IEnumerable input)
        {
            if (input == null || input is string)
                return input;

            var inputList = input.Cast<object>().ToList();
            inputList.Reverse();
            return inputList;
        }

        /// <summary>
        /// Encodes a string to Base64 format.
        /// </summary>
        /// <see href="https://shopify.dev/api/liquid/filters#base64_encode"/>
        public static string Base64Encode(string input)
        {
            return (input == null) ? string.Empty : Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Decodes a string in Base64 format
        /// </summary>
        /// <see href="https://shopify.dev/api/liquid/filters#base64_decode"/>
        public static string Base64Decode(string input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(input));
            }
            catch (FormatException)
            {
                throw new ArgumentException(string.Format(Liquid.ResourceManager.GetString("Base64FilterInvalidInput"), Template.NamingConvention.GetMemberName(nameof(Base64Decode))));
            }
        }

        /// <summary>
        /// Encodes a string to URL-safe Base64 format
        /// </summary>
        /// <see href="https://shopify.dev/api/liquid/filters#base64_url_safe_encode"/>
        public static string Base64UrlSafeEncode(string input)
        {
            return (input == null) ? string.Empty
                : Convert.ToBase64String(Encoding.UTF8.GetBytes(input)).Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Decodes a string in URL-safe Base64 format.
        /// </summary>
        /// <see href="https://shopify.dev/api/liquid/filters#base64_url_safe_decode"/>
        public static string Base64UrlSafeDecode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var incoming = input.Replace('_', '/').Replace('-', '+');
            if (input[input.Length - 1] != '=')
            {
                switch (input.Length % 4)
                {
                    case 2: incoming += "=="; break;
                    case 3: incoming += "="; break;
                }
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(incoming));
            }
            catch (FormatException)
            {
                throw new ArgumentException(string.Format(Liquid.ResourceManager.GetString("Base64FilterInvalidInput"), Template.NamingConvention.GetMemberName(nameof(Base64UrlSafeDecode))));
            }
        }
    }

    internal static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrEmpty(s) || s.Trim().Length == 0;
        }
    }
}
