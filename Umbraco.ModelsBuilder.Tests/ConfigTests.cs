using System;
using System.Collections.Generic;
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

        [Test]
        public void DefaultModelsPath()
        {
            Config.Setup(new Config());
            Assert.AreEqual(Config.DefaultModelsPath, UmbracoConfig.For.ModelsBuilder().ModelsPath);
        }
    }
}
