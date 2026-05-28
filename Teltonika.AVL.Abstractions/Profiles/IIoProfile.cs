namespace Teltonika.AVL.Profiles;

public interface IIoProfile
{
    string Name { get; }

    bool TryGetDefinition(int rawId, out IoPropertyDefinition? definition);
}