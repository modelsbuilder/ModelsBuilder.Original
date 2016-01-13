//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using NUnit.Framework;
//using Umbraco.ModelsBuilder.Umbraco;

//namespace Umbraco.ModelsBuilder.Tests
//{
//    [TestFixture]
//    public class UmbracoApplicationTests
//    {
//        [Test]
//        public void Test()
//        {
//            // start and terminate
//            using (var app = Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
//            { }

//            // start and terminate
//            using (var app = Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
//            { }

//            // start, use and terminate
//            using (var app = Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
//            {
//                var types = app.GetContentTypes();
//            }
//        }
//    }
//}
