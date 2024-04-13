namespace Teltonika.AVL.Elements;

public interface IAvlElementParser
{
    public AvlElement[] ParseElements(AvlIOElement[] elements);
}