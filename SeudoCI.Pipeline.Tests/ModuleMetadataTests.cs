namespace SeudoCI.Pipeline.Tests;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SeudoCI.Pipeline.Modules.EmailNotify;
using SeudoCI.Pipeline.Modules.FolderArchive;
using SeudoCI.Pipeline.Modules.FTPDistribute;
using SeudoCI.Pipeline.Modules.GitSource;
using SeudoCI.Pipeline.Modules.PerforceSource;
using SeudoCI.Pipeline.Modules.PerforceSource.Shared;
using SeudoCI.Pipeline.Modules.SFTPDistribute;
using SeudoCI.Pipeline.Modules.SMBDistribute;
using SeudoCI.Pipeline.Modules.SteamDistribute;
using SeudoCI.Pipeline.Modules.ShellBuild;
using SeudoCI.Pipeline.Modules.UnityBuild;
using SeudoCI.Pipeline.Modules.ZipArchive;
using SeudoCI.Pipeline.Shared;

/// <summary>
/// Validates module metadata and registry integration for every pipeline module.
/// </summary>
[TestFixture]
public class ModuleMetadataTests
{
    private static readonly ModuleExpectation[] Expectations =
    [
        ModuleExpectation.Create<GitSourceModule, ISourceModule, GitSourceStep, GitSourceConfig>("Git", "Git"),
        ModuleExpectation.Create<PerforceModule, ISourceModule, PerforceStep, PerforceConfig>("Perforce", "Perforce"),
        ModuleExpectation.Create<ShellBuildModule, IBuildModule, ShellBuildStep, ShellBuildStepConfig>("Shell", "Shell Build"),
        ModuleExpectation.Create<UnityStandardBuildModule, IBuildModule, UnityStandardBuildStep, UnityStandardBuildConfig>("Unity (Standard)", "Unity Standard Build"),
        ModuleExpectation.Create<UnityParameterizedBuildModule, IBuildModule, UnityParameterizedBuildStep, UnityParameterizedBuildConfig>("Unity (Parameterized)", "Unity Parameterized Build"),
        ModuleExpectation.Create<UnityExecuteMethodBuildModule, IBuildModule, UnityExecuteMethodBuildStep, UnityExecuteMethodBuildConfig>("Unity (Execute Method)", "Unity Execute Method"),
        ModuleExpectation.Create<FolderArchiveModule, IArchiveModule, FolderArchiveStep, FolderArchiveConfig>("Folder", "Folder"),
        ModuleExpectation.Create<ZipArchiveModule, IArchiveModule, ZipArchiveStep, ZipArchiveConfig>("Zip", "Zip File"),
        ModuleExpectation.Create<FTPDistributeModule, IDistributeModule, FTPDistributeStep, FTPDistributeConfig>("FTP", "FTP Upload"),
        ModuleExpectation.Create<SFTPDistributeModule, IDistributeModule, SFTPDistributeStep, SFTPDistributeConfig>("SFTP", "SFTP Upload"),
        ModuleExpectation.Create<SMBDistributeModule, IDistributeModule, SMBDistributeStep, SMBDistributeConfig>("SMB", "SMB Transfer"),
        ModuleExpectation.Create<SteamDistributeModule, IDistributeModule, SteamDistributeStep, SteamDistributeConfig>("Steam", "Steam Upload"),
        ModuleExpectation.Create<EmailNotifyModule, INotifyModule, EmailNotifyStep, EmailNotifyConfig>("Email", "Email Notification")
    ];

    public static IEnumerable<TestCaseData> ModuleExpectationCases => Expectations
        .Select(expectation => new TestCaseData(expectation)
            .SetName($"{nameof(Module_ShouldExposeValidMetadata)}({expectation.ModuleType.Name})"));

    [TestCaseSource(nameof(ModuleExpectationCases))]
    public void Module_ShouldExposeValidMetadata(ModuleExpectation expectation)
    {
        var module = expectation.CreateModule();

        Assert.Multiple(() =>
        {
            Assert.That(module, Is.Not.Null, "Module factory returned null.");
            Assert.That(module, Is.InstanceOf(expectation.ModuleInterface));
            Assert.That(module.GetType(), Is.EqualTo(expectation.ModuleType));
            Assert.That(module.Name, Is.EqualTo(expectation.ExpectedName));
            Assert.That(module.StepConfigName, Is.EqualTo(expectation.ExpectedStepConfigName));
            Assert.That(module.StepType, Is.EqualTo(expectation.ExpectedStepType));
            Assert.That(module.StepConfigType, Is.EqualTo(expectation.ExpectedStepConfigType));
            Assert.That(module.StepType, Is.Not.Null);
            Assert.That(module.StepConfigType, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(module.StepType!.IsAbstract, Is.False, "Step type must be concrete.");
            Assert.That(module.StepType.GetConstructor(Type.EmptyTypes), Is.Not.Null,
                "Step type must provide a public parameterless constructor for reflection instantiation.");
            Assert.That(module.StepConfigType!.IsAbstract, Is.False, "Config type must be concrete.");
            Assert.That(module.StepConfigType.GetConstructor(Type.EmptyTypes), Is.Not.Null,
                "Config type must provide a public parameterless constructor for JSON deserialization.");
        });

        var category = expectation.Category;

        Assert.Multiple(() =>
        {
            Assert.That(category.StepInterfaceType.IsAssignableFrom(module.StepType!), Is.True,
                $"{module.StepType} must implement {category.StepInterfaceType}.");
            Assert.That(category.ConfigBaseType.IsAssignableFrom(module.StepConfigType!), Is.True,
                $"{module.StepConfigType} must inherit from {category.ConfigBaseType}.");
            Assert.That(typeof(IPipelineStep).IsAssignableFrom(module.StepType), Is.True,
                "Step type must implement IPipelineStep.");
            Assert.That(typeof(IInitializable<>).MakeGenericType(module.StepConfigType)
                .IsAssignableFrom(module.StepType), Is.True,
                "Step type must be initializable with the module's configuration type.");
        });
    }

    [Test]
    public void ModuleRegistry_ShouldReturnModulesAcrossCategories()
    {
        var registry = new ModuleRegistry();
        var modules = Expectations.Select(expectation => new
        {
            Expectation = expectation,
            Module = expectation.CreateModule()
        }).ToList();

        foreach (var entry in modules)
        {
            registry.RegisterModule(entry.Module);
        }

        Assert.That(registry.GetAllModules(), Is.EquivalentTo(modules.Select(m => m.Module)));

        foreach (var entry in modules)
        {
            var expectation = entry.Expectation;
            var module = entry.Module;

            var getModules = typeof(ModuleRegistry)
                .GetMethod(nameof(ModuleRegistry.GetModules))!
                .MakeGenericMethod(expectation.ModuleInterface);
            var modulesByInterface = ((IEnumerable)getModules.Invoke(registry, null)!)
                .Cast<object>()
                .ToList();

            Assert.That(modulesByInterface, Does.Contain(module));

            var getModulesForStep = typeof(ModuleRegistry)
                .GetMethod(nameof(ModuleRegistry.GetModulesForStepType))!
                .MakeGenericMethod(expectation.Category.StepInterfaceType);
            var modulesByStep = ((IEnumerable)getModulesForStep.Invoke(registry, null)!)
                .Cast<object>()
                .ToList();

            Assert.That(modulesByStep, Does.Contain(module));
        }
    }

    [Test]
    public void ModuleRegistry_ShouldRegisterConvertersForAllConfigs()
    {
        var registry = new ModuleRegistry();

        foreach (var expectation in Expectations)
        {
            registry.RegisterModule(expectation.CreateModule());
        }

        var converters = registry.GetJsonConverters();

        foreach (var expectation in Expectations)
        {
            var category = expectation.Category;
            var converter = converters.FirstOrDefault(c => c.CanConvert(category.ConfigBaseType));

            Assert.That(converter, Is.Not.Null,
                $"Expected a JSON converter for base config type {category.ConfigBaseType.Name}.");

            var serializer = new JsonSerializer();
            serializer.Converters.Add(converter!);

            using var reader = new JsonTextReader(new StringReader($"{{ \"Type\": \"{expectation.ExpectedStepConfigName}\" }}"));
            var deserialized = serializer.Deserialize(reader, category.ConfigBaseType);

            Assert.That(deserialized, Is.InstanceOf(expectation.ExpectedStepConfigType));
        }
    }

    public sealed record ModuleExpectation(
        Func<IModule> Factory,
        Type ModuleType,
        Type ModuleInterface,
        Type ExpectedStepType,
        Type ExpectedStepConfigType,
        string ExpectedName,
        string ExpectedStepConfigName)
    {
        public static ModuleExpectation Create<TModule, TInterface, TStep, TConfig>(string expectedName, string expectedStepConfigName)
            where TModule : class, TInterface, IModule, new()
            where TInterface : IModule
            where TStep : class
            where TConfig : StepConfig
        {
            return new ModuleExpectation(
                Factory: static () => new TModule(),
                ModuleType: typeof(TModule),
                ModuleInterface: typeof(TInterface),
                ExpectedStepType: typeof(TStep),
                ExpectedStepConfigType: typeof(TConfig),
                ExpectedName: expectedName,
                ExpectedStepConfigName: expectedStepConfigName);
        }

        public IModule CreateModule() => Factory();

        public ModuleCategoryAttribute Category => ModuleInterface.GetCustomAttribute<ModuleCategoryAttribute>()
            ?? throw new InvalidOperationException($"Module interface {ModuleInterface.Name} is missing ModuleCategoryAttribute.");
    }
}
