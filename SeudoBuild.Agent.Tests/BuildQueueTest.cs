using System.IO;
using NSubstitute;
using NUnit.Framework;
using SeudoBuild.Core;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent.Tests
{
    [TestFixture]
    public class BuildQueueTest
    {
        private IModuleLoader _mockLoader;
        private ILogger _mockLogger;
        private IBuilder _mockBuilder;
        private const string DocumentsDir = "Documents";
        
        [SetUp]
        public void SetUp()
        {
            _mockLoader = Substitute.For<IModuleLoader>();
            _mockLogger = Substitute.For<ILogger>();
            _mockBuilder = Substitute.For<IBuilder>();
        }
        
        [Test]
        public void StartQueue_DocumentsDirMissing_ThrowsException()
        {
            IFileSystem fs = Substitute.For<IFileSystem>();
            fs.DocumentsPath.Returns("Documents");
            fs.DirectoryExists("Documents").Returns(false);
            var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                buildQueue.StartQueue(fs);
            });
        }
        
        [Test]
        public void StartQueue_DocumentsDirExists_CreatesBuildDir()
        {
            IFileSystem fs = Substitute.For<IFileSystem>();
            fs.DocumentsPath.Returns(DocumentsDir);
            fs.DirectoryExists(DocumentsDir).Returns(true);
            var buildQueue = new BuildQueue(_mockBuilder, _mockLoader, _mockLogger);
            
            buildQueue.StartQueue(fs);
            
            fs.Received().CreateDirectory(Arg.Any<string>());
        }

        [Test]
        public void EnqueueBuild_AddsBuild()
        {
            Assert.Fail();
        }
        
        [Test]
        public void GetAllBuildResults_ReturnsBuilds()
        {
            Assert.Fail();
        }
        
        [Test]
        public void GetBuildResult_ReturnsBuilds()
        {
            Assert.Fail();
        }
        
        [Test]
        public void CancelBuild_BuildIsCancelled()
        {
            Assert.Fail();
        }

        [Test]
        public void ActiveBuild_Finishes_NextBuildBegins()
        {
            Assert.Fail();
        }
    }
}