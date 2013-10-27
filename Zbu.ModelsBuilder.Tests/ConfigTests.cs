using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Zbu.ModelsBuilder.Tests
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void Test()
        {
            // not here
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);

            // throws because it is read-only
            Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.AppSettings.Add("testKey", "testValue"));

            // install editable configuration manger
            ConfigSystem.Install();

            // not here
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);

            // can add, read
            ConfigurationManager.AppSettings.Add("testKey", "testValue");
            Assert.AreEqual("testValue", ConfigurationManager.AppSettings["testKey"]);

            // can remove
            ConfigurationManager.AppSettings.Remove("testKey");
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);

            // can reset
            ConfigurationManager.AppSettings.Add("testKey", "testValue");
            Assert.AreEqual("testValue", ConfigurationManager.AppSettings["testKey"]);
            ConfigSystem.Reset();
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);

            // can uninstall
            ConfigurationManager.AppSettings.Add("testKey", "testValue");
            ConfigSystem.Uninstall();
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);
            Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.AppSettings.Add("testKey", "testValue"));
        }
    }
}
