using NUnit.Framework;

namespace SeudoBuild.Tests
{
    [TestFixture]
    public class MacrosTest
    {
        Macros macros;

        [SetUp]
        public void Setup()
        {
            macros = new Macros();
        }

        [Test]
        public void ReplaceVariables_VariableDoesNotExist_RemovesVariable()
        {
            string str = "a %b% c";
            string result = macros.ReplaceVariablesInText(str);

            Assert.That(result, Is.EqualTo("a  c"));
        }

        [Test]
        public void ReplaceVariables_VariableExists_ReplacesVariable()
        {
            string str = "a %b% c";
            macros.Add("b", "1");
            string result = macros.ReplaceVariablesInText(str);

            Assert.That(result, Is.EqualTo("a 1 c"));
        }
    }
}
