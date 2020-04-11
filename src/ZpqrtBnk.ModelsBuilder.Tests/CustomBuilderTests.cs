using System.Collections.Generic;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Umbraco;
using Umbraco.Core.Composing;

namespace Our.ModelsBuilder.Tests
{
    [TestFixture]
    public class CustomBuilderTests
    {
        // this should be the way to configure options
        // but ... wouldn't it be nicer to register .Configure(options => options.ContentTypes.IgnoreContentType("")) ??
        //
        // could: inject ICodeFactory in a component,
        //   then do factory.ConfigureCodeOptions(options => ...);
        // 'cos if we supply a IRegister.ConfigureCodeOptions(...) extension method, WHERE would it store it?
        // cannot store in the register, and so would need to be in composition

        // ReSharper disable once UnusedMember.Global, reason: composer
        public class ConfigureOptionsComposer : IOptionsComposer
        {
            public void Compose(Composition composition)
            {
                composition.ConfigureOptions(options => { options.DebugLevel = 666; });

                composition.ConfigureCodeOptions(optionsBuilder =>
                {
                    // replaces [IgnoreContentTypeAttribute]
                    // ignores a content type
                    // TODO: also, .FlattenContentType etc - what else?
                    // for anything more complex, override CodeBuilder.ContentTypes.GetContentTypeVisibility
                    optionsBuilder.ContentTypes.IgnoreContentType("contentAlias");

                    // replaces [RenameContentTypeAttribute]
                    // defines the Clr name of a content type model
                    // for anything more complex, override CodeBuilder.ContentTypes.GetContentTypeClrName
                    optionsBuilder.ContentTypes.SetContentTypeClrName("contentAlias", "ContentClrName");

                    // defines the namespace of a content type model
                    // for anything more complex, override CodeBuilder.ContentTypes.GetContentTypeNamespace
                    //options.SetContentTypeNamespace("contentAlias", "Some.Namespace");

                    // replaces [IgnorePropertyTypeAttribute]
                    // ignores a property type
                    // TODO: also, .FlattenProperty etc - what else?
                    // for anything more complex, override CodeBuilder.ContentTypes.GetPropertyTypeVisibility
                    optionsBuilder.ContentTypes.IgnorePropertyType(ContentTypeIdentity.Alias("contentTypeAlias"), "propertyAlias");

                    // replaces [RenamePropertyTypeAttribute]
                    // FIXME and then if the attribute is gone, we can work with ContentType alias EXCLUSIVELY?
                    // for anything more complex, override CodeBuilder.ContentTypes.GetPropertyTypeClrName
                    optionsBuilder.ContentTypes.SetPropertyTypeClrName(ContentTypeIdentity.Alias("contentTypeAlias"), "propertyAlias", "PropertyClrName");

                    // TODO: same for property type value Clr type
                    // TODO: options.SetModelInfosClassName("")
                    // TODO: options.ContentTypes.SetPropertyStyle(...)
                    // TODO: options.ContentTypes.FallbackStyle = ...
                });
            }
        }

        private class CustomCodeOptionsBuilder : CodeOptionsBuilder
        {
            public override CodeOptions CodeOptions
            {
                get
                {
                    // here we could tweak more things...

                    var options = base.CodeOptions;

                    // here we could tweak more things...

                    return options;
                }
            }
        }

        // demo
        // all config can be achieved in the model
        //
        private class CustomCodeFactory : CodeFactory
        {
            public CustomCodeFactory(UmbracoServices umbracoServices, OptionsConfiguration optionsConfiguration) 
                : base(umbracoServices, optionsConfiguration)
            { }

            public override CodeOptionsBuilder CreateCodeOptionsBuilder()
                => new CustomCodeOptionsBuilder();

            public override ICodeModelBuilder CreateCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions)
            {
                return new VeryCustomCodeModelBuilder(options, codeOptions);
            }
        }

        public class VeryCustomCodeModelBuilder : CodeModelBuilder
        {
            public VeryCustomCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions) 
                : base(options, codeOptions, new CustomContentTypesCodeModelBuilder(options, codeOptions))
            { }

            // replaces [ModelsUsingAttribute]
            protected override ISet<string> GetUsing()
            {
                var usings = base.GetUsing();
                usings.Add("My.Project");
                return usings;
            }
        }

        public class CustomContentTypesCodeModelBuilder : ContentTypesCodeModelBuilder
        {
            public CustomContentTypesCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions) 
                : base(options, codeOptions)
            { }

            // replaces [ContentModelsBaseClassAttribute]
            // replaces [ElementModelsBaseClassAttribute]
            protected override string GetContentTypeBaseClassClrFullName(ContentTypeModel contentTypeModel, string modelsNamespace)
            {
                // any kind of logic can go
                if (!contentTypeModel.IsElement && contentTypeModel.Alias == "alias") return "Base";

                return base.GetContentTypeBaseClassClrFullName(contentTypeModel, modelsNamespace);
            }

            protected override string GetClrName(ContentTypeModel contentTypeModel)
            {
                const string typeModelPrefix = "";
                const string typeModelSuffix = "";

                // replaces [ModelsBuilderConfigureAttribute]

                return typeModelPrefix + base.GetClrName(contentTypeModel) + typeModelSuffix;
            }

            protected override string GetClrName(PropertyTypeModel propertyModel)
            {
                const string propertyModelPrefix = "";
                const string propertyModelSuffix = "";

                // was not possible with [ModelsBuilderConfigureAttribute]

                return propertyModelPrefix + base.GetClrName(propertyModel) + propertyModelSuffix;
            }
        }
    }
}
