using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZpqrtBnk.ModelsBuilder.Building;
using ZpqrtBnk.ModelsBuilder.Umbraco;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class UmbracoApplicationTests
    {
        //[Test]
        //public void Test()
        //{
        //    // start and terminate
        //    using (var app = Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
        //    { }

        //    // start and terminate
        //    using (var app = Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
        //    { }

        //    // start, use and terminate
        //    using (var app = Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
        //    {
        //        var types = app.GetContentTypes();
        //    }
        //}

        [Test]
        public void ThrowsOnDuplicateAliases()
        {
            var typeModels = new List<ContentTypeModel>
            {
                new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Content, Alias = "content1" },
                new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Content, Alias = "content2" },
                new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Media, Alias = "media1" },
                new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Media, Alias = "media2" },
                new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Member, Alias = "member1" },
                new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Member, Alias = "member2" },
            };

            Assert.AreEqual(6, UmbracoServices.EnsureDistinctAliases(typeModels).Count);

            typeModels.Add(new ContentTypeModel { ItemType = ContentTypeModel.ItemTypes.Media, Alias = "content1" });

            try
            {
                UmbracoServices.EnsureDistinctAliases(typeModels);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            Assert.Fail("Expected NotSupportedException.");
        }
    }
}
