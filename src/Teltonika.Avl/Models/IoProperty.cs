namespace Teltonika.Avl.Models;

public readonly record struct IoProperty(ushort Id, ReadOnlyMemory<byte> Value);
