using NSubstitute;
using SeudoCI.Core;
using SeudoCI.Pipeline;

namespace SeudoCI.Pipeline.Tests;

[TestFixture]
public class ProjectPipelineTest
{
    private IModuleLoader _loader;
    private ITargetWorkspace _workspace;
    private ILogger _logger;

    [SetUp]
    public void SetUp()
    {
        _loader = Substitute.For<IModuleLoader>();
        _workspace = Substitute.For<ITargetWorkspace>();
        _logger = Substitute.For<ILogger>();
    }

    [Test]
    public void LoadBuildStepModules_LoadsAllModules()
    {
        var sourceConfig = new TestSourceStepConfig();
        var buildConfig = new TestBuildStepConfig();
        var archiveConfig = new TestArchiveStepConfig();
        var distributeConfig = new TestDistributeStepConfig();
        var notifyConfig = new TestNotifyStepConfig();

        var targetConfig = new BuildTargetConfig
        {
            TargetName = "target",
            SourceSteps = [sourceConfig],
            BuildSteps = [buildConfig],
            ArchiveSteps = [archiveConfig],
            DistributeSteps = [distributeConfig],
            NotifySteps = [notifyConfig],
        };

        var pipeline = CreatePipeline(targetConfig);

        var sourceStep = Substitute.For<ISourceStep>();
        var buildStep = Substitute.For<IBuildStep>();
        var archiveStep = Substitute.For<IArchiveStep>();
        var distributeStep = Substitute.For<IDistributeStep>();
        var notifyStep = Substitute.For<INotifyStep>();

        _loader.CreatePipelineStep<ISourceStep>(sourceConfig, _workspace, _logger).Returns(sourceStep);
        _loader.CreatePipelineStep<IBuildStep>(buildConfig, _workspace, _logger).Returns(buildStep);
        _loader.CreatePipelineStep<IArchiveStep>(archiveConfig, _workspace, _logger).Returns(archiveStep);
        _loader.CreatePipelineStep<IDistributeStep>(distributeConfig, _workspace, _logger).Returns(distributeStep);
        _loader.CreatePipelineStep<INotifyStep>(notifyConfig, _workspace, _logger).Returns(notifyStep);

        pipeline.LoadBuildStepModules(_loader, _workspace, _logger);

        CollectionAssert.AreEquivalent(new[] { sourceStep }, pipeline.GetPipelineSteps<ISourceStep>());
        CollectionAssert.AreEquivalent(new[] { buildStep }, pipeline.GetPipelineSteps<IBuildStep>());
        CollectionAssert.AreEquivalent(new[] { archiveStep }, pipeline.GetPipelineSteps<IArchiveStep>());
        CollectionAssert.AreEquivalent(new[] { distributeStep }, pipeline.GetPipelineSteps<IDistributeStep>());
        CollectionAssert.AreEquivalent(new[] { notifyStep }, pipeline.GetPipelineSteps<INotifyStep>());

        _loader.Received(1).CreatePipelineStep<ISourceStep>(sourceConfig, _workspace, _logger);
        _loader.Received(1).CreatePipelineStep<IBuildStep>(buildConfig, _workspace, _logger);
        _loader.Received(1).CreatePipelineStep<IArchiveStep>(archiveConfig, _workspace, _logger);
        _loader.Received(1).CreatePipelineStep<IDistributeStep>(distributeConfig, _workspace, _logger);
        _loader.Received(1).CreatePipelineStep<INotifyStep>(notifyConfig, _workspace, _logger);
    }

    [Test]
    public void CreatePipelineSteps_WithSourceStepType_ReturnsSourceSteps()
    {
        var sourceConfig = new TestSourceStepConfig();
        var pipeline = CreatePipeline(new BuildTargetConfig
        {
            TargetName = "target",
            SourceSteps = [sourceConfig],
        });

        var mockSourceStep = Substitute.For<ISourceStep>();

        _loader.CreatePipelineStep<ISourceStep>(sourceConfig, _workspace, _logger).Returns(mockSourceStep);

        var steps = pipeline.CreatePipelineSteps<ISourceStep>(_loader, _workspace, _logger);

        CollectionAssert.Contains(steps, mockSourceStep);
    }

    [Test]
    public void CreatePipelineSteps_WithBuildStepType_ReturnsBuildSteps()
    {
        var buildConfig = new TestBuildStepConfig();
        var pipeline = CreatePipeline(new BuildTargetConfig
        {
            TargetName = "target",
            BuildSteps = [buildConfig],
        });

        var buildStep = Substitute.For<IBuildStep>();

        _loader.CreatePipelineStep<IBuildStep>(buildConfig, _workspace, _logger).Returns(buildStep);

        var steps = pipeline.CreatePipelineSteps<IBuildStep>(_loader, _workspace, _logger);

        CollectionAssert.AreEquivalent(new[] { buildStep }, steps);
    }

    [Test]
    public void CreatePipelineSteps_WithArchiveStepType_ReturnsBuildSteps()
    {
        var archiveConfig = new TestArchiveStepConfig();
        var pipeline = CreatePipeline(new BuildTargetConfig
        {
            TargetName = "target",
            ArchiveSteps = [archiveConfig],
        });

        var archiveStep = Substitute.For<IArchiveStep>();

        _loader.CreatePipelineStep<IArchiveStep>(archiveConfig, _workspace, _logger).Returns(archiveStep);

        var steps = pipeline.CreatePipelineSteps<IArchiveStep>(_loader, _workspace, _logger);

        CollectionAssert.AreEquivalent(new[] { archiveStep }, steps);
    }

    [Test]
    public void CreatePipelineSteps_WithDistributeStepType_ReturnsBuildSteps()
    {
        var distributeConfig = new TestDistributeStepConfig();
        var pipeline = CreatePipeline(new BuildTargetConfig
        {
            TargetName = "target",
            DistributeSteps = [distributeConfig],
        });

        var distributeStep = Substitute.For<IDistributeStep>();

        _loader.CreatePipelineStep<IDistributeStep>(distributeConfig, _workspace, _logger).Returns(distributeStep);

        var steps = pipeline.CreatePipelineSteps<IDistributeStep>(_loader, _workspace, _logger);

        CollectionAssert.AreEquivalent(new[] { distributeStep }, steps);
    }

    [Test]
    public void CreatePipelineSteps_WithNotifyStepType_ReturnsBuildSteps()
    {
        var notifyConfig = new TestNotifyStepConfig();
        var pipeline = CreatePipeline(new BuildTargetConfig
        {
            TargetName = "target",
            NotifySteps = [notifyConfig],
        });

        var notifyStep = Substitute.For<INotifyStep>();

        _loader.CreatePipelineStep<INotifyStep>(notifyConfig, _workspace, _logger).Returns(notifyStep);

        var steps = pipeline.CreatePipelineSteps<INotifyStep>(_loader, _workspace, _logger);

        CollectionAssert.AreEquivalent(new[] { notifyStep }, steps);
    }

    private static ProjectPipeline CreatePipeline(BuildTargetConfig targetConfig)
    {
        var projectConfig = new ProjectConfig();
        projectConfig.BuildTargets.Add(targetConfig);
        return new ProjectPipeline(projectConfig, targetConfig.TargetName);
    }

    private sealed class TestSourceStepConfig : SourceStepConfig
    {
        public override string Name => nameof(TestSourceStepConfig);
    }

    private sealed class TestBuildStepConfig : BuildStepConfig
    {
        public override string Name => nameof(TestBuildStepConfig);
    }

    private sealed class TestArchiveStepConfig : ArchiveStepConfig
    {
        public override string Name => nameof(TestArchiveStepConfig);
    }

    private sealed class TestDistributeStepConfig : DistributeStepConfig
    {
        public override string Name => nameof(TestDistributeStepConfig);
    }

    private sealed class TestNotifyStepConfig : NotifyStepConfig
    {
        public override string Name => nameof(TestNotifyStepConfig);
    }
}