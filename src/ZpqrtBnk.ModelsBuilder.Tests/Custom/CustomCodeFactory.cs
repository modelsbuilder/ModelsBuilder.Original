using System.Text;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;

namespace Our.ModelsBuilder.Tests.Custom
{
    public class CustomCodeFactory : ICodeFactory
    {
        public ICodeModelDataSource CreateCodeModelDataSource()
            => new CustomCodeModelDataSource();

        public CodeOptionsBuilder CreateCodeOptionsBuilder()
            => new CustomCodeOptionsBuilder();

        public ICodeParser CreateCodeParser()
            => new CustomCodeParser();

        public ICodeModelBuilder CreateCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions)
            => new CustomCodeModelBuilderX(options, codeOptions);

        public ICodeWriter CreateCodeWriter(CodeModel model, StringBuilder text = null)
            => new CustomCodeWriter(model, text);
    }

    public class CustomCodeModelDataSource : ICodeModelDataSource
    {
        public CodeModelData GetCodeModelData()
             => new CodeModelData();
    }

    // custom: support more options
    //
    public class CustomCodeOptionsBuilder : CodeOptionsBuilder
    {
        public CustomCodeOptionsBuilder()
            : this (new CustomCodeOptions())
        { }

        private CustomCodeOptionsBuilder(CustomCodeOptions codeOptions)
            : this(codeOptions, new CustomContentTypesCodeOptionsBuilder(codeOptions.CustomContentTypes))
        { }

        private CustomCodeOptionsBuilder(CustomCodeOptions codeOptions, CustomContentTypesCodeOptionsBuilder contentTypesCodeOptionsBuilder)
            : base(codeOptions, contentTypesCodeOptionsBuilder)
        {
            CustomOptions = codeOptions;
            CustomContentTypes = contentTypesCodeOptionsBuilder;
        }

        public CustomCodeOptions CustomOptions { get; }

        public CustomContentTypesCodeOptionsBuilder CustomContentTypes { get; }

        // nothing to override
    }

    // custom: support more options
    //
    public class CustomContentTypesCodeOptionsBuilder : ContentTypesCodeOptionsBuilder
    {
        public CustomContentTypesCodeOptionsBuilder(CustomContentTypesCodeOptions options)
            : base(options)
        {
            CustomOptions = options;
        }

        public CustomContentTypesCodeOptions CustomOptions { get; }

        // override!
    }

    // custom: support more options
    //
    public class CustomCodeOptions : CodeOptions
    {
        public CustomCodeOptions() 
            : this(new CustomContentTypesCodeOptions())
        { }

        private CustomCodeOptions(CustomContentTypesCodeOptions contentTypesCodeOptions)
            : base(contentTypesCodeOptions)
        {
            CustomContentTypes = contentTypesCodeOptions;
        }

        public CustomContentTypesCodeOptions CustomContentTypes { get; }

        // nothing to override
    }

    // custom: support more options
    //
    public class CustomContentTypesCodeOptions : ContentTypesCodeOptions
    {
        // nothing to override
    }

    public class CustomCodeModelBuilderX : CodeModelBuilder // FIXME name?
    {
        public CustomCodeModelBuilderX(ModelsBuilderOptions options, CodeOptions codeOptions) 
            : base(options, codeOptions, new CustomContentTypesCodeModelBuilder(options, codeOptions))
        { }

        // building the code model - the options we are getting may be custom options
        // override Build
    }

    public class CustomContentTypesCodeModelBuilder : ContentTypesCodeModelBuilder
    {
        public CustomContentTypesCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions) 
            : base(options, codeOptions)
        { }

        // tons of things we can override when building the code model

        // determines the Clr name of a content type
        protected override string GetClrName(ContentTypeModel contentTypeModel)
        {
            return "PREFIX_" + base.GetClrName(contentTypeModel) + "_SUFFIX";
        }

        // determines the Clr name of a property type
        protected override string GetClrName(PropertyTypeModel propertyModel)
        {
            return "PREFIX_" + base.GetClrName(propertyModel) + "_SUFFIX";
        }

        // the rest is more internal stuff - do we want to override it?
    }

    public class CustomCodeParser : CodeParser
    { }

    public class CustomCodeWriter : CodeWriter
    {
        // FIXME the custom types & infos writers should be in ctors too!

        public CustomCodeWriter(CodeModel model, StringBuilder text = null) 
            : base(model, text)
        { }

        // override...
    }

    public class CustomContentTypesCodeWriter : ContentTypesCodeWriter
    {
        public CustomContentTypesCodeWriter(ModelsCodeWriter origin) 
            : base(origin)
        { }
    }
}
