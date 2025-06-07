namespace SeudoCI.Tests;

using Agent;

[TestFixture]
public class AgentNameTest
{
    [Test]
    public void GetRandomName_ReturnsName()
    {
        var rand = new Random(1000);
        var name = AgentName.GetRandomName(rand);
            
        Assert.That(name, Is.EqualTo("cheerful-dragonfly"));
    }
}