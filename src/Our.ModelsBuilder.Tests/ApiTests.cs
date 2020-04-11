using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Our.ModelsBuilder.Web.Api;

namespace Our.ModelsBuilder.Tests
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
using Our.ModelsBuilder;

namespace Umbraco.Demo3.Core.Models
{
    //[RenamePropertyType(""issued"", ""DateIssued"")]
    public partial class NewsItem
    {
    }
}
";
            const string text2 = @"
using Our.ModelsBuilder;

//[assembly:IgnoreContentType(""product"")]
";

            var api = new ApiClient("http://umbraco.local", "user", "password");
            var ourFiles = new Dictionary<string, string>
            {
                {"file1", text1},
                {"file2", text2},
            };
            var res = api.GetModels(ourFiles, "Our.ModelsBuilder.Tests.Models");

            foreach (var kvp in res)
            {
                Console.WriteLine("****");
                Console.WriteLine(kvp.Key);
                Console.WriteLine("----");
                Console.WriteLine(kvp.Value);
            }
        }

        [TestCase("a", "b")]
        [TestCase("a:b", "c:d")]
        [TestCase("%xx%a%b:c:d:e", "x:y%z%b")]
        public void TokenTests(string username, string password)
        {
            var separator = ":".ToCharArray();

            // ApiClient code
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(ApiClient.EncodeTokenElement(username) + ':' + ApiClient.EncodeTokenElement(password)));

            // ApiBasicAuthFilter code
            var credentials = Encoding.ASCII
                .GetString(Convert.FromBase64String(token))
                .Split(separator);
            if (credentials.Length != 2)
                throw new Exception();

            var username2 = ApiClient.DecodeTokenElement(credentials[0]);
            var password2 = ApiClient.DecodeTokenElement(credentials[1]);

            Assert.AreEqual(username, username2);
            Assert.AreEqual(password, password2);
        }
    }
}
