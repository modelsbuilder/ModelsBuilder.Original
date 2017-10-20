using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class ConfigTests
    {
        [SetUp]
        public void Setup()
        {
            // this is needed to ensure that Config.Setup is OK in each test
            UmbracoConfigExtensions.ResetConfig();
        }

        [Test]
        public void Test1()
        {
            Config.Setup(new Config(modelsNamespace: "test1"));
            Assert.AreEqual("test1", UmbracoConfig.For.ModelsBuilder().ModelsNamespace);
        }

        [Test]
        public void Test2()
        {
            Config.Setup(new Config(modelsNamespace: "test2"));
            Assert.AreEqual("test2", UmbracoConfig.For.ModelsBuilder().ModelsNamespace);
        }

        [Test]
        public void DefaultModelsNamespace1()
        {
            Config.Setup(new Config(enable: true));
            Assert.AreEqual(Config.DefaultModelsNamespace, UmbracoConfig.For.ModelsBuilder().ModelsNamespace);
        }

        [Test]
        public void DefaultModelsNamespace2()
        {
            Config.Setup(new Config());
            Assert.AreEqual(Config.DefaultModelsNamespace, UmbracoConfig.For.ModelsBuilder().ModelsNamespace);
        }

        [Test]
        public void DefaultStaticMixinGetterPattern1()
        {
            Config.Setup(new Config(enable: true));
            Assert.AreEqual(Config.DefaultStaticMixinGetterPattern, UmbracoConfig.For.ModelsBuilder().StaticMixinGetterPattern);
        }

        [Test]
        public void DefaultStaticMixinGetterPattern2()
        {
            Config.Setup(new Config());
            Assert.AreEqual(Config.DefaultStaticMixinGetterPattern, UmbracoConfig.For.ModelsBuilder().StaticMixinGetterPattern);
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
