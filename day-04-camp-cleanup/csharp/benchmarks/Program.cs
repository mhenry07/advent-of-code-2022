using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<Day04>();
    }
}

[InProcessAttribute]
[MemoryDiagnoser]
public class Day04
{
    [Benchmark]
    public Task<Results> GetResultsPipeWriterReaderAsync() =>
        Solution.GetResultsPipeWriterReaderAsync();

    [Benchmark]
    public Task<Results> GetResultsPipeReaderAsyncEnumerableAsync() =>
        Solution.GetResultsPipeReaderAsyncEnumerableAsync();

    [Benchmark]
    public Task<Results> GetResultsReadAllBytesSpanAsync() =>
        Solution.GetResultsReadAllBytesSpanAsync();

    [Benchmark]
    public static Task<Results> GetResultsReadAllLinesAsync() =>
        Solution.GetResultsReadAllLinesAsync();

    [Benchmark]
    public static Task<Results> GetResultsReadLinesAsync() =>
        Solution.GetResultsReadLinesAsync();

    [Benchmark]
    public static Task<Results> GetResultsStreamReaderAsync() =>
        Solution.GetResultsStreamReaderAsync();
}
