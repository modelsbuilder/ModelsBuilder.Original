using System.Configuration;
using NUnit.Framework;
using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Tests
{
    [TestFixture]
    public class ConfigTests
    {
        [TestCase("c:/path/to/root", "~/dir/models", false, "c:\\path\\to\\root\\dir\\models")]
        [TestCase("c:/path/to/root", "~/../../dir/models", true, "c:\\path\\dir\\models")]
        [TestCase("c:/path/to/root", "c:/another/path/to/elsewhere", true, "c:\\another\\path\\to\\elsewhere")]
        public void GetModelsDirectoryTests(string root, string config, bool acceptUnsafe, string expected)
        {
            Assert.AreEqual(expected, OptionsWebConfigReader.GetModelsDirectory(root, config, acceptUnsafe));
        }

        [TestCase("c:/path/to/root", "~/../../dir/models", false)]
        [TestCase("c:/path/to/root", "c:/another/path/to/elsewhere", false)]
        public void GetModelsDirectoryThrowsTests(string root, string config, bool acceptUnsafe)
        {
            Assert.Throws<ConfigurationErrorsException>(() =>
            {
                var modelsDirectory = OptionsWebConfigReader.GetModelsDirectory(root, config, acceptUnsafe);
            });
        }
    }
}
