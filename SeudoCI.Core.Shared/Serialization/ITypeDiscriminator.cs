namespace SeudoCI.Core.Serialization;

using System;
using System.Collections.Generic;

/// <summary>
/// Describes how to map a serialized discriminator value to a concrete <see cref="Type"/>.
/// </summary>
public interface ITypeDiscriminator
{
    /// <summary>
    /// The abstract or base type that the discriminator applies to.
    /// </summary>
    Type BaseType { get; }

    /// <summary>
    /// All discriminator values mapped to their respective concrete types.
    /// </summary>
    IEnumerable<KeyValuePair<string, Type>> Mappings { get; }

    /// <summary>
    /// Attempt to resolve the concrete type associated with the provided discriminator.
    /// </summary>
    /// <param name="value">The discriminator value from the serialized document.</param>
    /// <param name="type">The concrete type corresponding to the discriminator.</param>
    /// <returns><c>true</c> if a matching type was found; otherwise <c>false</c>.</returns>
    bool TryResolve(string value, out Type type);
}
