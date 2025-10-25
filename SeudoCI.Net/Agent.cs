using System;

namespace SeudoCI.Net;

/// <summary>
/// Describes a running build agent process.
/// </summary>
public sealed class Agent : IEquatable<Agent>
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Comparer.GetHashCode(Name ?? string.Empty),
            Comparer.GetHashCode(Address ?? string.Empty));
    }

    public override bool Equals(object? obj)
    {
        return obj is Agent other && Equals(other);
    }

    public bool Equals(Agent? other)
    {
        if (other is null)
        {
            return false;
        }

        return Comparer.Equals(Name, other.Name) && Comparer.Equals(Address, other.Address);
    }
}
