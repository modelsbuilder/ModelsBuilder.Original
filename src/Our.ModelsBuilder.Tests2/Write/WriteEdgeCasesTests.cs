using System;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Tests.Write
{
    [TestFixture]
    public class WriteEdgeCasesTests : TestsBase
    {
        [Test]
        public void WriteAmbiguousTypes()
        {
            var modelSource = new CodeModelData();

            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "type1",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
                IsMixin = true,
            };
            modelSource.ContentTypes.Add(type1);
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop1",
                ContentType = type1,
                ValueType = typeof(IPublishedContent),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type1,
                ValueType = typeof(global::System.Text.StringBuilder),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop3",
                ContentType = type1,
                ValueType = typeof(global::Umbraco.Core.IO.FileSecurityException),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();

            // forces conflict with Our.ModelsBuilder.Umbraco
            codeOptionsBuilder.SetModelsNamespace("Our.ModelsBuilder.Models");

            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            var writer = new CodeWriter(model).ContentTypesCodeWriter;

            foreach (var typeModel in model.ContentTypes.ContentTypes)
                writer.WriteModel(typeModel);
            var generated = writer.Code;

            Console.WriteLine(generated);

            Assert.IsTrue(generated.Contains(" IPublishedContent Prop1"));
            Assert.IsTrue(generated.Contains(" System.Text.StringBuilder Prop2"));
            Assert.IsTrue(generated.Contains(" global::Umbraco.Core.IO.FileSecurityException Prop3"));
        }
    }
}
