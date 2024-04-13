namespace Teltonika.AVL.Elements;
 
public class AvlElement<T>(short id, T value) : AvlElement(id, value)
    where T : unmanaged
{
    public new T Value { get { return (T)base.Value; } set { base.Value = value; } }
}

public abstract class AvlElement(short id, object value)
{
    public short Id { get; set; } = id;

    public object Value { get; set; } = value;
}