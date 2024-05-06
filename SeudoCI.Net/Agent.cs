namespace SeudoCI.Net;

/// <summary>
/// Describes a running build agent process.
/// </summary>
public class Agent
{
    public string Name { get; set; }
    public string Address { get; set; }

    public override int GetHashCode()
    {
        return Name.GetHashCode() * 17 + Address.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var item = obj as Agent;

        if (item == null)
        {
            return false;
        }

        return Name.Equals(item.Name) && Address.Equals(item.Address);
    }
}