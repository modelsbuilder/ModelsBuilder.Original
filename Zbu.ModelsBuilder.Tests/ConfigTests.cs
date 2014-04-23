using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbraco.Web.Mvc;

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
            global::Umbraco.Web.Standalone.WriteableConfigSystem.Install();

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
            global::Umbraco.Web.Standalone.WriteableConfigSystem.Reset();
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);

            // can uninstall
            ConfigurationManager.AppSettings.Add("testKey", "testValue");
            global::Umbraco.Web.Standalone.WriteableConfigSystem.Uninstall();
            Assert.IsNull(ConfigurationManager.AppSettings["testKey"]);
            Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.AppSettings.Add("testKey", "testValue"));
        }

        [Test]
        public void TestConnectionString()
        {
            Assert.IsNull(ConfigurationManager.ConnectionStrings["foo"]);
            global::Umbraco.Web.Standalone.WriteableConfigSystem.Install();
            ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings("foo", "xxx", "xxx"));
            Assert.IsNotNull(ConfigurationManager.ConnectionStrings["foo"]);
            global::Umbraco.Web.Standalone.WriteableConfigSystem.Uninstall();
            Assert.IsNull(ConfigurationManager.ConnectionStrings["foo"]);
        }

        [Test]
        [Ignore("Cannot work if MySql provider is installed machine-wide.")]
        public void TestDbProvider()
        {
            var section = ConfigurationManager.GetSection("system.data");
            Assert.IsNotNull(section);
            var dataset = section as System.Data.DataSet;
            Assert.IsNotNull(dataset);
            System.Data.DataRowCollection dbProviderFactories = null;
            foreach (System.Data.DataTable t in dataset.Tables)
                if (t.TableName == "DbProviderFactories")
                {
                    dbProviderFactories = t.Rows;
                    break;
                }
            Assert.IsNotNull(dbProviderFactories);
            var exists = false;
            foreach (System.Data.DataRow r in dbProviderFactories)
            {
                if (r["InvariantName"].ToString() == "MySql.Data.MySqlClient")
                    exists = true;
                Console.WriteLine(dataset.Tables[0].Columns[0].ColumnName);
                Console.WriteLine("  = {0}", r[0]);
                Console.WriteLine(dataset.Tables[0].Columns[1].ColumnName);
                Console.WriteLine("  = {0}", r[1]);
                Console.WriteLine(dataset.Tables[0].Columns[2].ColumnName);
                Console.WriteLine("  = {0}", r[2]);
                Console.WriteLine(dataset.Tables[0].Columns[3].ColumnName);
                Console.WriteLine("  = {0}", r[3]);
                Console.WriteLine("--");
            }
            Assert.IsFalse(exists, "Test expects MySql provider to NOT exist yet.");

            // <add name="MySQL Data Provider" 
            //      invariant="MySql.Data.MySqlClient" 
            //      description=".Net Framework Data Provider for MySQL" 
            //      type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=6.6.5.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d" />
            dbProviderFactories.Add("MySQL Data Provider",
                ".Net Framework Data Provider for MySQL",
                "MySql.Data.MySqlClient",
                "MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=6.6.5.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d");
                //"MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data");

            dataset.AcceptChanges();

            foreach (System.Data.DataRow r in (ConfigurationManager.GetSection("system.data") as System.Data.DataSet).Tables[0].Rows)
            {
                Console.WriteLine(r["InvariantName"]);
            }

            var p = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
            Assert.IsNotNull(p);
        }
    }
}
