namespace SeudoCI.Pipeline.Shared;

using System;

/// <summary>
/// Attribute to map pipeline step types to their corresponding configuration collections in BuildTargetConfig.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class StepConfigMappingAttribute : Attribute
{
    /// <summary>
    /// The property name in BuildTargetConfig that contains the step configurations (e.g., "SourceSteps", "BuildSteps")
    /// </summary>
    public string ConfigPropertyName { get; }

    public StepConfigMappingAttribute(string configPropertyName)
    {
        ConfigPropertyName = configPropertyName ?? throw new ArgumentNullException(nameof(configPropertyName));
    }
}