using NSubstitute;
using NUnit.Framework;
using SeudoBuild.Core;

namespace SeudoBuild.Pipeline.Tests
{
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
            Assert.Fail();
        }
        
        [Test]
        public void CreatePipelineSteps_WithSourceStepType_ReturnsSourceSteps()
        {
            var config = new ProjectConfig();
            var pipeline = new ProjectPipeline(config, "target");
            
            var mockSourceStep = Substitute.For<ISourceStep>();
            
            _loader.CreatePipelineStep<ISourceStep>(Arg.Any<StepConfig>(), _workspace, _logger).Returns(mockSourceStep);
            
            var steps = pipeline.CreatePipelineSteps<ISourceStep>(_loader, _workspace, _logger);
            
            CollectionAssert.Contains(steps, mockSourceStep);
        }

        [Test]
        public void CreatePipelineSteps_WithBuildStepType_ReturnsBuildSteps()
        {
            Assert.Fail();
        }
        
        [Test]
        public void CreatePipelineSteps_WithArchiveStepType_ReturnsBuildSteps()
        {
            Assert.Fail();
        }
        
        [Test]
        public void CreatePipelineSteps_WithDistributeStepType_ReturnsBuildSteps()
        {
            Assert.Fail();
        }
        
        [Test]
        public void CreatePipelineSteps_WithNotifyStepType_ReturnsBuildSteps()
        {
            Assert.Fail();
        }
    }
}