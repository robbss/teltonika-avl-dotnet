using BenchmarkDotNet.Attributes;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Benchmarks;

[MemoryDiagnoser]
public class ParserBenchmarks
{
    [Benchmark]
    public AvlPacket Parse_Codec8() => AvlParser.Parse(BenchmarkData.Codec8Packet);

    [Benchmark]
    public AvlPacket Parse_Codec8Extended() => AvlParser.Parse(BenchmarkData.Codec8ExtPacket);

    [Benchmark]
    public AvlPacket Parse_Codec16() => AvlParser.Parse(BenchmarkData.Codec16Packet);

    [Benchmark]
    public GprsCommandPacket ParseCommand_Codec12() => AvlParser.ParseCommand(BenchmarkData.Codec12Packet);

    [Benchmark]
    public string ParseImei() => AvlParser.ParseImei(BenchmarkData.ImeiFrame);
}
