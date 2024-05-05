using System;
using NUnit.Framework;
using SeudoCI.Agent;

namespace SeudoCI.Tests
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