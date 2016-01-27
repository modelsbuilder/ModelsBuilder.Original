using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbraco.ModelsBuilder.Api;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class ApiTests
    {
        //[Test]
        //[Ignore("That API has been disabled.")]
        //public void GetTypeModels()
        //{
        //    // note - works only if the website does not reference types that are not
        //    // referenced by the current test project!
        //    var api = new ModelsBuilderApi("http://umbraco.local", "user", "password");
        //    var res = api.GetTypeModels();
        //}

        [Test]
        [Ignore("Requires a proper endpoint.")]
        public void GetModels()
        {
            const string text1 = @"
using Umbraco.ModelsBuilder;

namespace Umbraco.Demo3.Core.Models
{
    [RenamePropertyType(""issued"", ""DateIssued"")]
    public partial class NewsItem
    {
    }
}
";
            const string text2 = @"
using Umbraco.ModelsBuilder;

[assembly:IgnoreContentType(""product"")]
";

            var api = new ApiClient("http://umbraco.local", "user", "password");
            var ourFiles = new Dictionary<string, string>
            {
                {"file1", text1},
                {"file2", text2},
            };
            var res = api.GetModels(ourFiles, "Umbraco.ModelsBuilder.Tests.Models");

            foreach (var kvp in res)
            {
                Console.WriteLine("****");
                Console.WriteLine(kvp.Key);
                Console.WriteLine("----");
                Console.WriteLine(kvp.Value);
            }
        }
    }
}
