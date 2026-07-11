using System;
using System.Collections.Generic;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Tests.Testing;

namespace Our.ModelsBuilder.Tests.Support
{
    // tests for support cases
    [TestFixture]
    public class SupportTests : TestsBase
    {
        [Test]
        public void Issue128()
        {
            // Umbraco returns nice, pascal-cased names

            var codeModelData = new CodeModelData();

            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "seoComposition",
                ClrName = "SeoComposition",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,

                IsMixin = true
            };
            codeModelData.ContentTypes.Add(type1);

            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "metaDescription",
                ContentType = type1,
                ClrName = "MetaDescription",
                ValueTypeClrFullName = "string",
                ValueType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "page",
                ClrName = "Page",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,

                MixinContentTypes = { type1 }
            };
            codeModelData.ContentTypes.Add(type2);

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;

namespace Umbraco.Web.PublishedModels
{
    public partial class Page
    {
        [ImplementPropertyType(""metaDescription"")]
        public string MetaDescription => ""..."";
    }

    public partial class SeoComposition
    {
        //[ImplementPropertyType(""metaDescription"")]
		//public string MetaDescription => ""..."";

        //public static string GetMetaDescription(ISeoComposition that) => ""..."";
    }
}
"}
            };

            var codeOptionsBuilder = new CodeOptionsBuilder();
            new CodeParser { WriteDiagnostics = true }.Parse(code, codeOptionsBuilder);
            var codeModelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var codeModel = codeModelBuilder.Build(codeModelData);
            var writer = new CodeWriter(codeModel);

            foreach (var modelToGenerate in codeModel.ContentTypes.ContentTypes)
            {
                writer.Reset();
                writer.ContentTypesCodeWriter.WriteModel(modelToGenerate);
                Console.WriteLine(writer.Code);
            }
        }

        [Test]
        public void Issue132()
        {
            // Umbraco returns nice, pascal-cased names

            var codeModelData = new CodeModelData();

            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "seoComposition",
                ClrName = "SeoComposition",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,

                IsMixin = true
            };
            codeModelData.ContentTypes.Add(type1);

            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "metaDescription",
                ContentType = type1,
                ClrName = "MetaDescription",
                ValueTypeClrFullName = "string",
                ValueType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "page",
                ClrName = "Page",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,

                MixinContentTypes = { type1 }
            };
            codeModelData.ContentTypes.Add(type2);

            var type3 = new ContentTypeModel
            {
                Id = 3,
                Alias = "other",
                ClrName = "Other",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,

                MixinContentTypes = { type1 }
            };
            codeModelData.ContentTypes.Add(type3);

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;

namespace Umbraco.Web.PublishedModels
{
    [IgnorePropertyType(""metaDescription"")]
    public partial class Page
    { }
}
"}
            };

            var codeOptionsBuilder = new CodeOptionsBuilder();
            new CodeParser { WriteDiagnostics = true }.Parse(code, codeOptionsBuilder);
            var codeModelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var codeModel = codeModelBuilder.Build(codeModelData);
            var writer = new CodeWriter(codeModel);

            foreach (var modelToGenerate in codeModel.ContentTypes.ContentTypes)
            {
                writer.Reset();
                writer.ContentTypesCodeWriter.WriteModel(modelToGenerate);
                Console.WriteLine(writer.Code);
            }
        }
    }
}
