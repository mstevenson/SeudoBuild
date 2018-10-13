using System;
using NUnit.Framework;
using NSubstitute;
using NUnit.Framework.Constraints;
using SeudoBuild.Pipeline;
using SeudoBuild.Core;

namespace SeudoBuild.Agent.Tests
{
    [TestFixture]
    public class BuilderTest
    {
        private IModuleLoader _mockLoader;
        private ILogger _mockLogger;
        private IPipelineRunner _mockPipeline;
        
        [SetUp]
        public void SetUp()
        {
            _mockLoader = Substitute.For<IModuleLoader>();
            _mockLogger = Substitute.For<ILogger>();
            _mockPipeline = Substitute.For<IPipelineRunner>();
        }
        
        [Test]
        public void Build_ProjectConfigIsNull_ThrowsException()
        {
            var builder = new Builder(_mockLoader, _mockLogger);
            
            Assert.Throws<ArgumentException> (() =>
            {
                builder.Build(_mockPipeline, null, "target");
            });
        }

        [Test]
        public void Build_EmptyConfig_TargetIsNull_ThrowsException()
        {
            var builder = new Builder(_mockLoader, _mockLogger);
            var config = new ProjectConfig();

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                builder.Build(_mockPipeline, config, null);
            });
        }

        [Test]
        public void Build_WithConfig_ExecutesPipeline()
        {
            var builder = new Builder(_mockLoader, _mockLogger);
            var config = new ProjectConfig();
            var target = "target";

            builder.Build(_mockPipeline, config, target);
            
            _mockPipeline.Received().ExecutePipeline(config, target, _mockLoader);
        }
    }
}