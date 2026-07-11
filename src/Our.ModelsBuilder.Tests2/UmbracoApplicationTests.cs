using System;
using System.Collections.Generic;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Umbraco;

namespace Our.ModelsBuilder.Tests
{
    [TestFixture]
    public class UmbracoApplicationTests
    {
        [Test]
        public void ThrowsOnDuplicateAliases()
        {
            var typeModels = new List<ContentTypeModel>
            {
                new ContentTypeModel { Kind = ContentTypeKind.Content, Alias = "content1" },
                new ContentTypeModel { Kind = ContentTypeKind.Content, Alias = "content2" },
                new ContentTypeModel { Kind = ContentTypeKind.Media, Alias = "media1" },
                new ContentTypeModel { Kind = ContentTypeKind.Media, Alias = "media2" },
                new ContentTypeModel { Kind = ContentTypeKind.Member, Alias = "member1" },
                new ContentTypeModel { Kind = ContentTypeKind.Member, Alias = "member2" },
            };

            Assert.AreEqual(6, UmbracoServices.EnsureDistinctAliases(typeModels).Count);

            typeModels.Add(new ContentTypeModel { Kind = ContentTypeKind.Media, Alias = "content1" });

            try
            {
                UmbracoServices.EnsureDistinctAliases(typeModels);
                Assert.Fail("Expected NotSupportedException.");
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
