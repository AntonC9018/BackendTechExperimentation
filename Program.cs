using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using efcore_transactions;

BenchmarkRunner.Run<Benchmarks>();

public class Benchmarks
{
    public static IEnumerable<string> Params
    {
        get
        {
            yield return "simpleCamelCaseThing";
            yield return "longerCamelCaseThing" + string.Join("", Enumerable.Repeat("LongerCamelCaseThing", 10));
            yield return "longerCamelCaseThing" + string.Join("", Enumerable.Repeat("LongerCamelCaseThing", 100));
            yield return "longerCamelCaseThing" + string.Join("", Enumerable.Repeat("LongerCamelCaseThing", 1000));
            yield return "рашенКамелКейсThing";
            yield return "N&&#oT___Camel   Case thing *(#(*&78932789";
        }
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(Params))]
    public string Span_NewStringBuilderEachTime(string input)
    {
        var stringBuilder = new StringBuilder();
        StringCasingExtensions.ToUpperSnakeCase(stringBuilder, input);
        return stringBuilder.ToString();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Params))]
    public string Span_ThreadLocalStringBuilder(string input)
    {
        return input.ToUpperSnakeCase();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Params))]
    public string Regex_NoCaching(string input)
    {
        return input.CamelCaseToUpperSnakeCase();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Params))]
    public string Regex_Caching(string input)
    {
        return input.CamelCaseToUpperSnakeCase_CachedRegex();
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(Params))]
    public string Regex_Compiled(string input)
    {
        return input.CamelCaseToUpperSnakeCase_CompiledRegex();
    }
}
