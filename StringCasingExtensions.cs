using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace efcore_transactions;

public static class StringCasingExtensions
{
    private static readonly ThreadLocal<StringBuilder> _Builder = new(() => new StringBuilder());

    public static string ToUpperSnakeCase(this string input)
    {
        var span = input.AsSpan();
        return ToUpperSnakeCase(span);
    }
    
    public static string ToUpperSnakeCase(this ReadOnlySpan<char> input)
    {
        var globalBuilder = _Builder.Value!;
        ToUpperSnakeCase(globalBuilder, input);
        var result = globalBuilder.ToString();
        globalBuilder.Clear();
        return result;
    }

    enum SnakeLetterKind
    {
        Initial,
        Uppercase,
        Lowercase,
        Separator,
    }
    
    public static void ToUpperSnakeCase(StringBuilder builderValue, ReadOnlySpan<char> input)
    {
        var currentKind = SnakeLetterKind.Initial;
        const char separator = '_';
        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            var category = char.GetUnicodeCategory(ch);
            switch (category)
            {
                case UnicodeCategory.UppercaseLetter:
                {
                    switch (currentKind)
                    {
                        case SnakeLetterKind.Lowercase:
                        {
                            builderValue.Append(separator);
                            currentKind = SnakeLetterKind.Uppercase;
                            break;
                        }
                        default:
                        {
                            currentKind = SnakeLetterKind.Uppercase;
                            break;
                        }
                    }
                    builderValue.Append(ch);
                    break;
                }
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DashPunctuation:
                {
                    builderValue.Append(separator);
                    currentKind = SnakeLetterKind.Separator;
                    break;
                }
                case UnicodeCategory.LowercaseLetter:
                {
                    builderValue.Append(char.ToUpperInvariant(ch));
                    currentKind = SnakeLetterKind.Lowercase;
                    break;
                }
                case UnicodeCategory.DecimalDigitNumber:
                {
                    builderValue.Append(ch);
                    currentKind = SnakeLetterKind.Lowercase;
                    break;
                }
            }
        }
    }
    
    public static string CamelCaseToUpperSnakeCase(this string input)
    {
        var output = Regex.Replace(
            input: input,
            pattern: _RegexString,
            replacement: "$1_$2"
        );
        output = output.ToUpper();
        return output;
    }

    private const string _RegexString = @"(\G(?!^)|\b(?:[A-Z]{2}|[a-zA-Z][a-z]*))(?=[a-zA-Z]{2,}|\d)([A-Z](?:[A-Z]|[a-z]*)|\d+)";

    private static readonly Regex _ReplaceRegex = new Regex(_RegexString);
    
    private static readonly Regex _ReplaceRegexCompiled = new Regex(_RegexString, RegexOptions.Compiled);
    
    public static string CamelCaseToUpperSnakeCase_CachedRegex(this string input)
    {
        var output = _ReplaceRegex.Replace(input, "$1_$2");
        return output.ToUpper();
    }

    public static string CamelCaseToUpperSnakeCase_CompiledRegex(this string input)
    {
        var output = _ReplaceRegexCompiled.Replace(input, "$1_$2");
        return output.ToUpper();
    }
}