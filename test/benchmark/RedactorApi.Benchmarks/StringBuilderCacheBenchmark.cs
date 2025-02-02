using System.Text;
using BenchmarkDotNet.Attributes;
using RedactorApi.Util;
namespace RedactorApi.Benchmarks;

[MemoryDiagnoser]
// A simple benchmark to compare the performance of different ways to trim a StringBuilder and use caching
public class StringBuilderTrimBenchmark
{

    private readonly string _testString = new('A', 2048);

    private readonly Dictionary<int, string> _args = new()
    {
            { 0, "" },
            {10, new string(' ', 10)},
            {20, new string(' ', 20)},
            {50, new string(' ', 50)},
            {100, new string(' ', 100)},
            {200, new string(' ', 200)},
        };

    [GlobalSetup]
    public void Setup()
    {
    }

    public IEnumerable<(int length,string value)> ArgumentList()
    {
        return _args.Select(arg => (arg.Key, arg.Value));
    }

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderToStringTrim((int length, string spaces) value)
    {
        var sb = new StringBuilder();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.ToString().Trim();
        return result.Length;
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderTrimToString((int length, string spaces) value)
    {
        var sb = new StringBuilder();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.Trim();
        return result.Length;
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderTrimStartToString((int length, string spaces) value)
    {
        var sb = new StringBuilder();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.TrimStart().ToString();
        return result.Length;
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderTrimEndToString((int length, string spaces) value)
    {
        var sb = new StringBuilder();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.TrimEnd().ToString();
        return result.Length;
    }

    /* Cached */

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderCacheTrimToString((int length, string spaces) value)
    {
        var sb = StringBuilderCache.Acquire();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.TrimAndRelease();
        return result.Length;
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderCacheTrimStartToString((int length, string spaces) value)
    {
        var sb = StringBuilderCache.Acquire();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.StartTrimAndRelease();
        return result.Length;
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentList))]
    public int StringBuilderCacheTrimEndToString((int length, string spaces) value)
    {
        var sb = StringBuilderCache.Acquire();
        sb.Append(value.spaces);
        sb.Append(_testString);
        sb.Append(value.spaces);
        var result = sb.EndTrimAndRelease();
        return result.Length;
    }
}
