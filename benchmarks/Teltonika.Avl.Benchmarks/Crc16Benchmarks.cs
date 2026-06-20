using BenchmarkDotNet.Attributes;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Benchmarks;

[MemoryDiagnoser]
public class Crc16Benchmarks
{
    [Benchmark]
    public ushort Compute_SmallPayload() => Crc16Ibm.Compute(BenchmarkData.CrcSmallPayload);

    [Benchmark]
    public ushort Compute_LargePayload() => Crc16Ibm.Compute(BenchmarkData.CrcLargePayload);
}
