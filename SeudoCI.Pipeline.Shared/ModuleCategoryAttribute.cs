namespace SeudoCI.Pipeline.Shared;

using System;

/// <summary>
/// Attribute to define the module category, associating module interfaces with their step interfaces and config base types.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class ModuleCategoryAttribute : Attribute
{
    /// <summary>
    /// The step interface type (e.g., ISourceStep, IBuildStep)
    /// </summary>
    public Type StepInterfaceType { get; }
    
    /// <summary>
    /// The configuration base type (e.g., SourceStepConfig, BuildStepConfig)
    /// </summary>
    public Type ConfigBaseType { get; }
    
    /// <summary>
    /// The category name for display purposes
    /// </summary>
    public string CategoryName { get; }

    public ModuleCategoryAttribute(Type stepInterfaceType, Type configBaseType, string categoryName)
    {
        StepInterfaceType = stepInterfaceType ?? throw new ArgumentNullException(nameof(stepInterfaceType));
        ConfigBaseType = configBaseType ?? throw new ArgumentNullException(nameof(configBaseType));
        CategoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
    }
}