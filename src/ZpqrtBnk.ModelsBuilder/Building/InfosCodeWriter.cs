using System.Collections.Generic;
using Our.ModelsBuilder.Api;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Writes the infos class.
    /// </summary>
    public class InfosCodeWriter : ModelsCodeWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InfosCodeWriter"/> class.
        /// </summary>
        public InfosCodeWriter(ModelsCodeWriter origin)
            : base(origin)
        { }

        /// <summary>
        /// Writes the infos class.
        /// </summary>
        public virtual void WriteInfosClass(CodeModel models)
        {
            WriteIndentLine("/// <summary>Provides information about models.</summary>");
            WriteBlockStart($"public static partial class {CodeModel.ModelInfosClassName}");
            WriteInfosClassBody(models);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes the infos class body.
        /// </summary>
        protected virtual void WriteInfosClassBody(CodeModel models)
        {
            WriteIndentLine("/// <summary>Gets the name of the generator.</summary>");
            WriteIndentLine($"public const string Name = \"{CodeModel.GeneratorName}\";");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the version of the generator that generated the files.</summary>");
            WriteIndentLine($"public const string VersionString = \"{ApiVersion.Current.Version}\";");
            WriteLine();

            WriteContentTypesInfos(models.ContentTypes.ContentTypes);
        }

        /// <summary>
        /// Writes the content types infos.
        /// </summary>
        protected virtual void WriteContentTypesInfos(List<ContentTypeModel> models)
        {
            WriteContentTypesInfosClass(models);
            WriteLine();

            WriteIndentLine("/// <summary>Gets the content type model infos.</summary>");
            WriteLocalGeneratedCodeAttribute();
            WriteIndentLine("public static IReadOnlyCollection<ContentTypeModelInfo> ContentTypeInfos => _contentTypeInfos;");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the model infos for a content type.</summary>");
            WriteLocalGeneratedCodeAttribute();
            WriteIndentLine("public static ContentTypeModelInfo GetContentTypeInfos(string alias) => _contentTypeInfos.FirstOrDefault(x => x.Alias == alias);");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the model infos for a content type.</summary>");
            WriteLocalGeneratedCodeAttribute();
            WriteIndentLine("public static ContentTypeModelInfo GetContentTypeInfos<TModel>() => _contentTypeInfos.FirstOrDefault(x => x.ClrType == typeof(TModel));");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the model infos for a content type.</summary>");
            WriteLocalGeneratedCodeAttribute();
            WriteIndentLine("public static ContentTypeModelInfo GetContentTypeInfos(Type typeofModel) => _contentTypeInfos.FirstOrDefault(x => x.ClrType == typeofModel);");
            WriteLine();

            WriteContentTypesInfosCollection(models);
        }

        /// <summary>
        /// Writes the content type infos class.
        /// </summary>
        protected virtual void WriteContentTypesInfosClass(IEnumerable<ContentTypeModel> models)
        {
            WriteIndentLine("/// <summary>Provides information about content type models.</summary>");
            WriteBlockStart("public static class ContentTypes");
            WriteContentTypesInfosClassBody(models);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes the content type infos class body.
        /// </summary>
        protected virtual void WriteContentTypesInfosClassBody(IEnumerable<ContentTypeModel> models)
        {
            var first = true;
            foreach (var model in models)
            {
                WriteLineBetween(ref first);
                WriteContentTypeInfosClass(model);
            }
        }

        /// <summary>
        /// Writes the collection of content types infos.
        /// </summary>
        protected virtual void WriteContentTypesInfosCollection(IEnumerable<ContentTypeModel> models)
        {
            WriteLocalGeneratedCodeAttribute();
            WriteBlockStart("private static readonly ContentTypeModelInfo[] _contentTypeInfos = ");

            var firstType = true;
            foreach (var model in models)
            {
                WriteBetween(ref firstType, $",{NewLine}");

                WriteIndent($"new ContentTypeModelInfo(\"{model.Alias}\", \"{model.ClrName}\", typeof(");
                WriteClrType(CodeModel.ModelsNamespace + "." + model.ClrName);
                Write(")");
                if (model.Properties.Count > 0)
                {
                    WriteLine(",");
                    Indent();
                    var firstProperty = true;
                    foreach (var propertyModel in model.Properties)
                    {
                        WriteBetween(ref firstProperty, $",{NewLine}");
                        WriteIndent($"new PropertyTypeModelInfo(\"{propertyModel.Alias}\", \"{propertyModel.ClrName}\", typeof(");
                        WriteClrType(propertyModel.ValueTypeClrFullName);
                        Write("))");
                    }
                    Outdent();
                }

                Write(")");
            }

            WriteLine();

            Outdent();
            WriteIndentLine("};"); // beware of the ';'
        }

        /// <summary>
        /// Writes a content type infos class.
        /// </summary>
        protected virtual void WriteContentTypeInfosClass(ContentTypeModel model)
        {
            WriteIndentLine($"/// <summary>Provides information about the {model.ClrName} content type.</summary>");
            WriteBlockStart($"public static class {model.ClrName}");
            WriteContentTypeInfosClassBody(model);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes a content type infos class body.
        /// </summary>
        protected virtual void WriteContentTypeInfosClassBody(ContentTypeModel model)
        {
            WriteIndentLine("/// <summary>Gets the item type of the content type.</summary>");
            WriteIndentLine($"public const PublishedItemType ItemType = PublishedItemType.{model.Kind.ToPublishedItemType()};");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the alias of the content type.</summary>");
            WriteIndentLine($"public const string Alias = \"{model.Alias}\";");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the content type.</summary>");
            WriteIndentLine("public static IPublishedContentType GetContentType() => PublishedModelUtility.GetModelContentType(ItemType, Alias);");
            WriteLine();

            WritePropertyTypesInfosClass(model);
        }

        /// <summary>
        /// Writes a content type property infos class.
        /// </summary>
        protected virtual void WritePropertyTypesInfosClass(ContentTypeModel model)
        {
            WriteIndentLine($"/// <summary>Provides information about the properties of the {model.ClrName} content type.</summary>");
            WriteBlockStart("public static class Properties");
            WritePropertyTypesInfosClassBody(model);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes a content type property infos class body.
        /// </summary>
        protected virtual void WritePropertyTypesInfosClassBody(ContentTypeModel model)
        {
            var first = true;
            foreach (var propertyModel in model.Properties)
            {
                WriteLineBetween(ref first);
                WritePropertyTypeInfosClass(propertyModel);
            }
        }

        /// <summary>
        /// Writes a property type infos class.
        /// </summary>
        protected virtual void WritePropertyTypeInfosClass(PropertyTypeModel model)
        {
            WriteIndentLine($"/// <summary>Provides information about the {model.ClrName} property type.</summary>");
            WriteBlockStart($"public static class {model.ClrName}");
            WritePropertyTypeInfosClassBody(model);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes a property type infos class body.
        /// </summary>
        protected virtual void WritePropertyTypeInfosClassBody(PropertyTypeModel model)
        {
            WriteIndentLine("/// <summary>Gets the alias of the property type.</summary>");
            WriteIndentLine($"public const string Alias = \"{model.Alias}\";");
            WriteLine();

            WriteIndentLine("/// <summary>Gets the property type.</summary>");
            WriteIndentLine("public static IPublishedPropertyType GetPropertyType() => GetContentType().GetPropertyType(Alias);");
        }
    }
}