using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration.UmbracoSettings;
using ZpqrtBnk.ModelsBuilder.Building;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    // tests for support cases
    [TestFixture]
    public class SupportTests
    {
        [SetUp]
        public void Setup()
        {
            Current.Reset();
            Current.UnlockConfigs();
            Current.Configs.Add(() => new Config());
            Current.Configs.Add<IUmbracoSettingsSection>(() => new UmbracoSettingsSection());
        }

        [Test]
        public void Issue128()
        {
            // Umbraco returns nice, pascal-cased names

            var types = new List<ContentTypeModel>();

            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "seoComposition",
                ClrName = "SeoComposition",
                ParentId = 0,
                BaseType = null,
                ItemType = ContentTypeModel.ItemTypes.Content,

                IsMixin = true
            };
            types.Add(type1);

            type1.Properties.Add(new PropertyModel
            {
                Alias = "metaDescription",
                ContentType = type1,
                ClrName = "MetaDescription",
                ClrTypeName = "string",
                ModelClrType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "page",
                ClrName = "Page",
                ParentId = 0,
                BaseType = null,
                ItemType = ContentTypeModel.ItemTypes.Content,

                MixinTypes = { type1 }
            };
            types.Add(type2);

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;

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

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser{ WriteDiagnostics = true }.Parse(code, refs);
            var model = new CodeModel { ContentTypeModels = types };
            model.Apply(new Config(), parseResult, null);
            var writer = new CodeWriter(model);

            foreach (var modelToGenerate in types)
            {
                writer.Reset();
                writer.WriteContentTypeModel(modelToGenerate);
                Console.WriteLine(writer.Code);
            }
        }

        [Test]
        public void Issue132()
        {
            // Umbraco returns nice, pascal-cased names

            var types = new List<ContentTypeModel>();

            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "seoComposition",
                ClrName = "SeoComposition",
                ParentId = 0,
                BaseType = null,
                ItemType = ContentTypeModel.ItemTypes.Content,

                IsMixin = true
            };
            types.Add(type1);

            type1.Properties.Add(new PropertyModel
            {
                Alias = "metaDescription",
                ContentType = type1,
                ClrName = "MetaDescription",
                ClrTypeName = "string",
                ModelClrType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "page",
                ClrName = "Page",
                ParentId = 0,
                BaseType = null,
                ItemType = ContentTypeModel.ItemTypes.Content,

                MixinTypes = { type1 }
            };
            types.Add(type2);

            var type3 = new ContentTypeModel
            {
                Id = 3,
                Alias = "other",
                ClrName = "Other",
                ParentId = 0,
                BaseType = null,
                ItemType = ContentTypeModel.ItemTypes.Content,

                MixinTypes = { type1 }
            };
            types.Add(type3);

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;

namespace Umbraco.Web.PublishedModels
{
    [IgnorePropertyType(""metaDescription"")]
    public partial class Page
    { }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser { WriteDiagnostics = true }.Parse(code, refs);
            var model = new CodeModel { ContentTypeModels = types };
            model.Apply(new Config(), parseResult, null);
            var writer = new CodeWriter(model);

            foreach (var modelToGenerate in types)
            {
                writer.Reset();
                writer.WriteContentTypeModel(modelToGenerate);
                Console.WriteLine(writer.Code);
            }
        }
    }
}
