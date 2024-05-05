using System;
using NUnit.Framework;
using SeudoBuild.Agent;

namespace SeudoBuild.Tests
{
    [TestFixture]
    public class AgentNameTest
    {
        [Test]
        public void GetRandomName_ReturnsName()
        {
            var rand = new Random(1000);
            var name = AgentName.GetRandomName(rand);
            
            Assert.That("cheerful-dragonfly", Is.EqualTo(name));
        }
    }
}