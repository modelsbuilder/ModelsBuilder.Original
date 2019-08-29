using System.Configuration;
using NUnit.Framework;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void Test1()
        {
            var config = new Config(modelsNamespace: "test1");
            Assert.AreEqual("test1", config.ModelsNamespace);
        }

        [Test]
        public void Test2()
        {
            var config = new Config(modelsNamespace: "test2");
            Assert.AreEqual("test2", config.ModelsNamespace);
        }

        [Test]
        public void DefaultModelsNamespace1()
        {
            var config = new Config(enable: true);
            Assert.AreEqual(Config.DefaultModelsNamespace, config.ModelsNamespace);
        }

        [Test]
        public void DefaultModelsNamespace2()
        {
            var config = new Config();
            Assert.AreEqual(Config.DefaultModelsNamespace, config.ModelsNamespace);
        }

        [TestCase("c:/path/to/root", "~/dir/models", false, "c:\\path\\to\\root\\dir\\models")]
        [TestCase("c:/path/to/root", "~/../../dir/models", true, "c:\\path\\dir\\models")]
        [TestCase("c:/path/to/root", "c:/another/path/to/elsewhere", true, "c:\\another\\path\\to\\elsewhere")]
        public void GetModelsDirectoryTests(string root, string config, bool acceptUnsafe, string expected)
        {
            Assert.AreEqual(expected, Config.GetModelsDirectory(root, config, acceptUnsafe));
        }

        [TestCase("c:/path/to/root", "~/../../dir/models", false)]
        [TestCase("c:/path/to/root", "c:/another/path/to/elsewhere", false)]
        public void GetModelsDirectoryThrowsTests(string root, string config, bool acceptUnsafe)
        {
            Assert.Throws<ConfigurationErrorsException>(() =>
            {
                var modelsDirectory = Config.GetModelsDirectory(root, config, acceptUnsafe);
            });
        }
    }
}
