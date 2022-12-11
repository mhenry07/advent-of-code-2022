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
    public Task<Results> GetResultsCharBuffersAsync() =>
        Solution.GetResultsCharBuffersAsync();

    [Benchmark]
    public Task<Results> GetResultsStackRowsAsync() =>
        Solution.GetResultsStackRowsAsync();
}
