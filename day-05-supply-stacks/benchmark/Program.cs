using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<Day05>();
    }
}

[InProcessAttribute]
[MemoryDiagnoser]
public class Day05
{
    [Benchmark]
    public async Task<string?> PipelineSpanCharBuffersAsync()
    {
        var results = await Solution.GetResultsCharBuffersAsync();
        return results.Results9001.TopCrates.ToString();
    }

    [Benchmark]
    public async Task<string?> PipelineSpanStackRowsAsync()
    {
        var results = await Solution.GetResultsStackRowsAsync();
        return results.Results9001.TopCrates.ToString();
    }

    [Benchmark]
    public async Task<string?> RegexReadAllLinesAsync()
    {
        var results = await Solution.GetResultsRegexAllLinesAsync();
        return results.Results9001.TopCrates.ToString();
    }

    [Benchmark]
    public async Task<string?> RegexPipelineAsync()
    {
        var results = await Solution.GetResultsRegexPipelineAsync();
        return results.Results9001.TopCrates.ToString();
    }
}
