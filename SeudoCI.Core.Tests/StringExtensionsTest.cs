namespace SeudoCI.Core.Tests;

[TestFixture]
public class StringExtensionsTest
{
    [Test]
    public void SanitizeFilename_ReplacesSpaces()
    {
        var sanitized = "My Project Name".SanitizeFilename();

        Assert.That(sanitized, Is.EqualTo("My_Project_Name"));
    }

    [Test]
    public void SanitizeFilename_DoesNotContainInvalidCharacters()
    {
        var invalidName = "File<>:\"/\\|?*Name";
        var sanitized = invalidName.SanitizeFilename();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            Assert.That(sanitized.Contains(invalidChar), Is.False, $"Sanitized name should not contain '{invalidChar}'");
        }
    }
}
