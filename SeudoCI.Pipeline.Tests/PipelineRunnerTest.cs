
using System;
using System.IO;
using NSubstitute;
using NUnit.Framework;
using SeudoCI.Core;
using SeudoCI.Pipeline;

namespace SeudoCI.Pipeline.Tests;

[TestFixture]
public class PipelineRunnerTest
{
    private ILogger _logger = null!;
    private IModuleLoader _moduleLoader = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger>();
        _moduleLoader = Substitute.For<IModuleLoader>();
    }

    [Test]
    public void ExecutePipeline_RunsNotifyAfterFailure_WhenConfigured()
    {
        using var tempDirectory = new TempDirectory();

        var pipelineConfig = new PipelineConfig { BaseDirectory = tempDirectory.Path };
        var runner = new PipelineRunner(pipelineConfig, _logger);

        var projectConfig = CreateProjectConfig(runNotifyOnFailure: true);

        var sourceStep = Substitute.For<ISourceStep>();
        sourceStep.ExecuteStep(Arg.Any<ITargetWorkspace>()).Returns(new SourceStepResults { IsSuccess = true });
        var buildStep = Substitute.For<IBuildStep>();
        buildStep.ExecuteStep(Arg.Any<SourceSequenceResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new BuildStepResults { IsSuccess = false, Exception = new Exception("Build failed") });
        var archiveStep = Substitute.For<IArchiveStep>();
        archiveStep.ExecuteStep(Arg.Any<BuildSequenceResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new ArchiveStepResults { IsSuccess = true });
        var distributeStep = Substitute.For<IDistributeStep>();
        distributeStep.ExecuteStep(Arg.Any<ArchiveSequenceResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new DistributeStepResults { IsSuccess = true });
        var notifyStep = Substitute.For<INotifyStep>();
        notifyStep.ExecuteStep(Arg.Any<PipelineRunResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new NotifyStepResults { IsSuccess = true });

        ConfigureModuleLoader(projectConfig, sourceStep, buildStep, archiveStep, distributeStep, notifyStep);

        runner.ExecutePipeline(projectConfig, projectConfig.BuildTargets[0].TargetName, _moduleLoader);

        notifyStep.Received(1).ExecuteStep(Arg.Is<PipelineRunResults>(results => !results.BuildResults.IsSuccess),
            Arg.Any<ITargetWorkspace>());
    }

    [Test]
    public void ExecutePipeline_SkipsNotifyAfterFailure_WhenNotConfigured()
    {
        using var tempDirectory = new TempDirectory();

        var pipelineConfig = new PipelineConfig { BaseDirectory = tempDirectory.Path };
        var runner = new PipelineRunner(pipelineConfig, _logger);

        var projectConfig = CreateProjectConfig(runNotifyOnFailure: false);

        var sourceStep = Substitute.For<ISourceStep>();
        sourceStep.ExecuteStep(Arg.Any<ITargetWorkspace>()).Returns(new SourceStepResults { IsSuccess = true });
        var buildStep = Substitute.For<IBuildStep>();
        buildStep.ExecuteStep(Arg.Any<SourceSequenceResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new BuildStepResults { IsSuccess = false, Exception = new Exception("Build failed") });
        var archiveStep = Substitute.For<IArchiveStep>();
        archiveStep.ExecuteStep(Arg.Any<BuildSequenceResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new ArchiveStepResults { IsSuccess = true });
        var distributeStep = Substitute.For<IDistributeStep>();
        distributeStep.ExecuteStep(Arg.Any<ArchiveSequenceResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new DistributeStepResults { IsSuccess = true });
        var notifyStep = Substitute.For<INotifyStep>();
        notifyStep.ExecuteStep(Arg.Any<PipelineRunResults>(), Arg.Any<ITargetWorkspace>())
            .Returns(new NotifyStepResults { IsSuccess = true });

        ConfigureModuleLoader(projectConfig, sourceStep, buildStep, archiveStep, distributeStep, notifyStep);

        runner.ExecutePipeline(projectConfig, projectConfig.BuildTargets[0].TargetName, _moduleLoader);

        notifyStep.DidNotReceive().ExecuteStep(Arg.Any<PipelineRunResults>(), Arg.Any<ITargetWorkspace>());
    }

    private void ConfigureModuleLoader(ProjectConfig projectConfig,
        ISourceStep sourceStep,
        IBuildStep buildStep,
        IArchiveStep archiveStep,
        IDistributeStep distributeStep,
        INotifyStep notifyStep)
    {
        var target = projectConfig.BuildTargets[0];
        _moduleLoader.CreatePipelineStep<ISourceStep>(target.SourceSteps[0], Arg.Any<ITargetWorkspace>(), Arg.Any<ILogger>())
            .Returns(sourceStep);
        _moduleLoader.CreatePipelineStep<IBuildStep>(target.BuildSteps[0], Arg.Any<ITargetWorkspace>(), Arg.Any<ILogger>())
            .Returns(buildStep);
        _moduleLoader.CreatePipelineStep<IArchiveStep>(target.ArchiveSteps[0], Arg.Any<ITargetWorkspace>(), Arg.Any<ILogger>())
            .Returns(archiveStep);
        _moduleLoader.CreatePipelineStep<IDistributeStep>(target.DistributeSteps[0], Arg.Any<ITargetWorkspace>(), Arg.Any<ILogger>())
            .Returns(distributeStep);
        _moduleLoader.CreatePipelineStep<INotifyStep>(target.NotifySteps[0], Arg.Any<ITargetWorkspace>(), Arg.Any<ILogger>())
            .Returns(notifyStep);
    }

    private static ProjectConfig CreateProjectConfig(bool runNotifyOnFailure)
    {
        var target = new BuildTargetConfig
        {
            TargetName = "default",
            SourceSteps = [new TestSourceStepConfig()],
            BuildSteps = [new TestBuildStepConfig()],
            ArchiveSteps = [new TestArchiveStepConfig()],
            DistributeSteps = [new TestDistributeStepConfig()],
            NotifySteps = [new TestNotifyStepConfig { RunOnFailure = runNotifyOnFailure }]
        };

        return new ProjectConfig
        {
            ProjectName = "TestProject",
            BuildTargets = { target }
        };
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }
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
