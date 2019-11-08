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

            var types = new List<TypeModel>();

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "seoComposition",
                ClrName = "SeoComposition",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,

                IsMixin = true
            };
            types.Add(type1);

            type1.Properties.Add(new PropertyModel
            {
                Alias = "metaDescription",
                ClrName = "MetaDescription",
                ClrTypeName = "string",
                ModelClrType = typeof(string)
            });

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "page",
                ClrName = "Page",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,

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
            var builder = new Builder();
            var context = builder.Build(new Config(), parseResult, null, new CodeModels { TypeModels = types });
            var writer = new CodeWriter(context);

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

            var types = new List<TypeModel>();

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "seoComposition",
                ClrName = "SeoComposition",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,

                IsMixin = true
            };
            types.Add(type1);

            type1.Properties.Add(new PropertyModel
            {
                Alias = "metaDescription",
                ClrName = "MetaDescription",
                ClrTypeName = "string",
                ModelClrType = typeof(string)
            });

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "page",
                ClrName = "Page",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,

                MixinTypes = { type1 }
            };
            types.Add(type2);

            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "other",
                ClrName = "Other",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,

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
            var builder = new Builder();
            var context = builder.Build(new Config(), parseResult, null, new CodeModels { TypeModels = types });
            var writer = new CodeWriter(context);

            foreach (var modelToGenerate in types)
            {
                writer.Reset();
                writer.WriteContentTypeModel(modelToGenerate);
                Console.WriteLine(writer.Code);
            }
        }
    }
}
