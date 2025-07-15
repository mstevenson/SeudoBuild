namespace SeudoCI.Agent.Tests;

using System.Text.RegularExpressions;

[TestFixture]
public class AgentNameTest
{
    [Test]
    public void GetRandomName_WithSeed_ReturnsConsistentName()
    {
        // Arrange
        var rand = new Random(1000);
        
        // Act
        var name = AgentName.GetRandomName(rand);
        
        // Assert
        Assert.That(name, Is.EqualTo("cheerful-dragonfly"));
    }

    [Test]
    public void GetRandomName_WithSameSeed_ReturnsIdenticalNames()
    {
        // Arrange
        var rand1 = new Random(42);
        var rand2 = new Random(42);
        
        // Act
        var name1 = AgentName.GetRandomName(rand1);
        var name2 = AgentName.GetRandomName(rand2);
        
        // Assert
        Assert.That(name1, Is.EqualTo(name2));
    }

    [Test]
    public void GetRandomName_WithDifferentSeeds_ReturnsDifferentNames()
    {
        // Arrange
        var rand1 = new Random(100);
        var rand2 = new Random(200);
        
        // Act
        var name1 = AgentName.GetRandomName(rand1);
        var name2 = AgentName.GetRandomName(rand2);
        
        // Assert
        Assert.That(name1, Is.Not.EqualTo(name2));
    }

    [Test]
    public void GetRandomName_WithoutSeed_ReturnsValidName()
    {
        // Act
        var name = AgentName.GetRandomName();
        
        // Assert
        Assert.That(name, Is.Not.Null);
        Assert.That(name, Is.Not.Empty);
        Assert.That(name, Does.Match(@"^[a-z]+-[a-z]+$")); // lowercase adjective-animal format
    }

    [Test]
    public void GetRandomName_WithNullRandom_ReturnsValidName()
    {
        // Act
        var name = AgentName.GetRandomName(null);
        
        // Assert
        Assert.That(name, Is.Not.Null);
        Assert.That(name, Is.Not.Empty);
        Assert.That(name, Does.Match(@"^[a-z]+-[a-z]+$"));
    }

    [Test]
    public void GetRandomName_Format_IsCorrect()
    {
        // Arrange
        var rand = new Random(12345);
        
        // Act
        var name = AgentName.GetRandomName(rand);
        
        // Assert
        Assert.That(name, Does.Contain("-"));
        var parts = name.Split('-');
        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.That(parts[0], Is.Not.Empty); // adjective
        Assert.That(parts[1], Is.Not.Empty); // animal
    }

    [Test]
    public void GetRandomName_IsLowercase()
    {
        // Arrange
        var rand = new Random(99999);
        
        // Act
        var name = AgentName.GetRandomName(rand);
        
        // Assert
        Assert.That(name, Is.EqualTo(name.ToLower()));
    }

    [Test]
    public void GetRandomName_MultipleCallsWithSameSeed_ProducesSequence()
    {
        // Arrange
        var rand1 = new Random(500);
        var rand2 = new Random(500);
        
        // Act
        var name1a = AgentName.GetRandomName(rand1);
        var name1b = AgentName.GetRandomName(rand1);
        var name2a = AgentName.GetRandomName(rand2);
        var name2b = AgentName.GetRandomName(rand2);
        
        // Assert
        Assert.That(name1a, Is.EqualTo(name2a)); // First calls are identical
        Assert.That(name1b, Is.EqualTo(name2b)); // Second calls are identical
        Assert.That(name1a, Is.Not.EqualTo(name1b)); // But first != second due to random state
    }

    [Test]
    public void GetUniqueAgentName_ReturnsConsistentName()
    {
        // Act
        var name1 = AgentName.GetUniqueAgentName();
        var name2 = AgentName.GetUniqueAgentName();
        
        // Assert
        Assert.That(name1, Is.EqualTo(name2)); // Should be deterministic based on MAC address
    }

    [Test]
    public void GetUniqueAgentName_Format_IsCorrect()
    {
        // Act
        var name = AgentName.GetUniqueAgentName();
        
        // Assert
        Assert.That(name, Is.Not.Null);
        Assert.That(name, Is.Not.Empty);
        Assert.That(name, Does.Match(@"^[a-z]+-[a-z]+$"));
        
        var parts = name.Split('-');
        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.That(parts[0], Is.Not.Empty); // adjective
        Assert.That(parts[1], Is.Not.Empty); // animal
    }

    [Test]
    public void GetUniqueAgentName_IsLowercase()
    {
        // Act
        var name = AgentName.GetUniqueAgentName();
        
        // Assert
        Assert.That(name, Is.EqualTo(name.ToLower()));
    }

    [Test]
    public void GetRandomName_ProducesVarietyOfNames()
    {
        // Arrange
        var names = new HashSet<string>();
        var rand = new Random(12345);
        
        // Act - Generate many names to test variety
        for (int i = 0; i < 100; i++)
        {
            var name = AgentName.GetRandomName(new Random(i));
            names.Add(name);
        }
        
        // Assert
        Assert.That(names.Count, Is.GreaterThan(50)); // Should have good variety
    }

    [Test]
    public void GetRandomName_NoWhitespace()
    {
        // Arrange
        var rand = new Random(777);
        
        // Act
        var name = AgentName.GetRandomName(rand);
        
        // Assert
        Assert.That(name, Does.Not.Contain(" "));
        Assert.That(name, Does.Not.Contain("\t"));
        Assert.That(name, Does.Not.Contain("\n"));
        Assert.That(name, Does.Not.Contain("\r"));
    }

    [Test]
    public void GetRandomName_NoSpecialCharacters()
    {
        // Arrange
        var rand = new Random(888);
        
        // Act
        var name = AgentName.GetRandomName(rand);
        
        // Assert
        var allowedPattern = @"^[a-z\-]+$"; // Only lowercase letters and hyphens
        Assert.That(name, Does.Match(allowedPattern));
    }

    [Test]
    public void GetRandomName_ReasonableLength()
    {
        // Arrange
        var rand = new Random(999);
        
        // Act
        var name = AgentName.GetRandomName(rand);
        
        // Assert
        Assert.That(name.Length, Is.GreaterThan(5)); // At least "a-b" but realistically longer
        Assert.That(name.Length, Is.LessThan(50)); // Reasonable upper bound
    }

    [Test]
    public void GetRandomName_ExtremeSeeds_WorkCorrectly()
    {
        // Test with extreme seed values
        var extremeSeeds = new[] { int.MinValue, int.MaxValue, 0, -1, 1 };
        
        foreach (var seed in extremeSeeds)
        {
            // Act
            var name = AgentName.GetRandomName(new Random(seed));
            
            // Assert
            Assert.That(name, Is.Not.Null, $"Name should not be null for seed {seed}");
            Assert.That(name, Is.Not.Empty, $"Name should not be empty for seed {seed}");
            Assert.That(name, Does.Match(@"^[a-z]+-[a-z]+$"), $"Name format should be correct for seed {seed}");
        }
    }

    [Test]
    public void GetUniqueAgentName_MultipleCalls_AreDeterministic()
    {
        // Act - Call multiple times in sequence
        var names = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            names.Add(AgentName.GetUniqueAgentName());
        }
        
        // Assert - All should be identical since based on same MAC addresses
        var uniqueNames = names.Distinct().ToList();
        Assert.That(uniqueNames.Count, Is.EqualTo(1), "All calls should return the same name");
    }

    [Test]
    public void AgentName_DifferentMethods_ProduceDifferentFormats()
    {
        // This test verifies that random and unique methods can produce different results
        // (though on some systems they might coincidentally be the same)
        
        // Act
        var randomName = AgentName.GetRandomName();
        var uniqueName = AgentName.GetUniqueAgentName();
        
        // Assert - Both should be valid regardless of whether they're different
        Assert.That(randomName, Does.Match(@"^[a-z]+-[a-z]+$"));
        Assert.That(uniqueName, Does.Match(@"^[a-z]+-[a-z]+$"));
        
        // Note: We don't assert they're different since random might coincidentally 
        // match the MAC-based unique name, but both should be valid
    }
}