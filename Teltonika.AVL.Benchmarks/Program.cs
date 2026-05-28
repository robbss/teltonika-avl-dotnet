using BenchmarkDotNet.Running;

namespace Teltonika.AVL.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ParserBenchmarks>();
    }
}