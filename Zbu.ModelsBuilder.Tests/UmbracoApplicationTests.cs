using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Zbu.ModelsBuilder.Umbraco;

namespace Zbu.ModelsBuilder.Tests
{
    [TestFixture]
    public class UmbracoApplicationTests
    {
        [Test]
        public void Test()
        {
            // start and terminate
            using (var app = Application.GetApplication())
            { }

            // start and terminate
            using (var app = Application.GetApplication())
            { }

            // start, use and terminate
            using (var app = Application.GetApplication())
            {
                var types = app.GetContentTypes();
            }
        }
    }
}
