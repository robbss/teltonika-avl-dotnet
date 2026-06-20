using BenchmarkDotNet.Attributes;

namespace Teltonika.Avl.Benchmarks;

[MemoryDiagnoser]
public class EncoderBenchmarks
{
    [Benchmark]
    public byte[] Encode_Codec8() => AvlEncoder.Encode(BenchmarkData.ParsedCodec8Packet);

    [Benchmark]
    public byte[] Encode_Codec8Extended() => AvlEncoder.Encode(BenchmarkData.ParsedCodec8ExtPacket);

    [Benchmark]
    public byte[] Encode_Codec16() => AvlEncoder.Encode(BenchmarkData.ParsedCodec16Packet);

    [Benchmark]
    public byte[] EncodeCommand_Codec12() => AvlEncoder.EncodeCommand(BenchmarkData.ParsedCommandPacket);

    [Benchmark]
    public byte[] EncodeImei() => AvlEncoder.EncodeImei("356307042441013");
}
