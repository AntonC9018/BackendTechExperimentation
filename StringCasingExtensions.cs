using System.Globalization;
using System.Text;

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
    
    private static void ToUpperSnakeCase(StringBuilder builderValue, ReadOnlySpan<char> input)
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
}