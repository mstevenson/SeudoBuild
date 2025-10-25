namespace SeudoCI.Pipeline;

using System;
using System.Collections.Generic;
using SeudoCI.Core.Serialization;

/// <summary>
/// Tracks the available configuration types for a specific <see cref="StepConfig"/> hierarchy.
/// </summary>
public class StepConfigConverter : ITypeDiscriminator
{
    private readonly Type _configBaseType;
    private readonly Dictionary<string, Type> _configTypeMap = new(StringComparer.OrdinalIgnoreCase);

    public StepConfigConverter(Type configBaseType)
    {
        if (configBaseType == typeof(StepConfig) || !typeof(StepConfig).IsAssignableFrom(configBaseType))
        {
            throw new ArgumentException("StepConfigConverter must be given a type that inherits from StepConfig");
        }

        _configBaseType = configBaseType;
    }

    public Type BaseType => _configBaseType;

    public IEnumerable<KeyValuePair<string, Type>> Mappings => _configTypeMap;

    public void RegisterConfigType(string name, Type type)
    {
        if (!_configBaseType.IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type} is not assignable from {_configBaseType}");
        }

        _configTypeMap.Add(name, type);
    }

    public bool TryResolve(string value, out Type type)
    {
        if (_configTypeMap.TryGetValue(value, out var resolved))
        {
            type = resolved;
            return true;
        }

        type = null!;
        return false;
    }
}

/// <summary>
/// Convenience helper to create a <see cref="StepConfigConverter"/> for a specific config type hierarchy.
/// </summary>
public class StepConfigConverter<T> : StepConfigConverter
    where T : StepConfig
{
    public StepConfigConverter() : base(typeof(T))
    {
    }
}
