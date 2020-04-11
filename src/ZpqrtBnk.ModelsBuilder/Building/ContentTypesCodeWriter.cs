using System;
using System.Collections.Generic;
using System.Linq;
using Our.ModelsBuilder.Options.ContentTypes;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Writes content types.
    /// </summary>
    public class ContentTypesCodeWriter : ModelsCodeWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypesCodeWriter"/> class.
        /// </summary>
        public ContentTypesCodeWriter(ModelsCodeWriter origin)
            : base(origin)
        { }

        /// <summary>
        /// Writes parameters.
        /// </summary>
        protected delegate void WriteParameters(ref bool first, PropertyTypeModel model);

        /// <summary>
        /// Writes a warning for a property that could not be generated.
        /// </summary>
        protected virtual void WritePropertyWarning(ContentTypeModel contentTypeModel, PropertyTypeModel propertyTypeModel)
        {
            WriteIndentLine($"// Property \"{propertyTypeModel.Alias}\" has not been generated:");
            WriteIndentLine("//");

            static IEnumerable<string> SplitError(string error)
            {
                var p = 0;
                while (p < error.Length)
                {
                    var n = p + 50;
                    while (n < error.Length && error[n] != ' ') n++;
                    if (n >= error.Length) break;
                    yield return error.Substring(p, n - p);
                    p = n + 1;
                }
                if (p < error.Length)
                    yield return error.Substring(p);
            }

            foreach (var s in propertyTypeModel.Errors.SelectMany(SplitError))
                WriteIndentLine("//" + s);

            WriteLine();
            WriteLine($"#warning Property \"{propertyTypeModel.Alias}\" has not been generated (see code comments).");
        }

        /// <summary>
        /// Writes complete content models.
        /// </summary>
        public virtual void WriteModels(IEnumerable<ContentTypeModel> models)
        {
            var first = true;
            foreach (var typeModel in models)
            {
                WriteLineBetween(ref first);
                WriteModel(typeModel);
            }
        }

        /// <summary>
        /// Writes a complete content type model.
        /// </summary>
        public virtual void WriteModel(ContentTypeModel model)
        {
            var first = true;

            foreach (var propertyTypeModel in model.ExpandedProperties.Where(x => x.Errors != null))
            {
                WriteLineBetween(ref first);
                WritePropertyWarning(model, propertyTypeModel);
            }

            if (WriteWithLineBetween(ref first, model.IsMixin)) // FIXME: model.IsMixin model.GenerateInterface
                WriteInterface(model);

            if (WriteWithLineBetween(ref first, model.Properties.Count > 0)) // FIXME: model.HasProperties
                WriteExtensionsClass(model);

            if (WriteWithLineBetween(ref first, true /*model.IsClass*/)) // FIXME: model.IsClass model.GenerateClass
                WriteModelClass(model);
        }

        /// <summary>
        /// Writes the interface declaration.
        /// </summary>
        /// <remarks>Appends the properties with <see cref="WriteInterfaceMembers"/>.</remarks>
        protected virtual void WriteInterface(ContentTypeModel model)
        {
            if (model.HasCustomClrName)
                WriteIndentLine($"// Content Type with alias \"{model.Alias}\"");

            WriteIndentLine(!string.IsNullOrWhiteSpace(model.Name)
                ? $"/// <summary>{XmlCommentString(model.Name)}</summary>"
                : $"/// <summary>Represents a \"{model.Alias}\" content item.</summary>");

            WriteIndent($"public partial interface I{model.ClrName}");

            var sep = " : ";
            if (!model.OmitBaseClass)
            {
                Write(sep);
                WriteClrType(ToInterface(model.BaseClassClrFullName));
                sep = ", ";
            }

            foreach (var mixinType in model.LocalMixinContentTypes)
            {
                Write(sep);
                WriteClrType(CodeModel.ModelsNamespace + ".I" + mixinType.ClrName);
            }

            WriteLine();
            WriteBlockStart();
            WriteInterfaceBody(model);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes the interface body.
        /// </summary>
        protected virtual void WriteInterfaceBody(ContentTypeModel model)
        {
            if (CodeModel.ContentTypes.PropertyStyle == PropertyStyle.Property ||
                CodeModel.ContentTypes.PropertyStyle == PropertyStyle.PropertyAndExtensionMethods)
                WriteInterfaceMembers(model);
        }

        /// <summary>
        /// Writes the interface members.
        /// </summary>
        protected virtual void WriteInterfaceMembers(ContentTypeModel model)
        {
            // write the properties - only the local (non-ignored) ones, we're an interface
            var firstProperty = true;
            foreach (var propertyModel in model.Properties.Where(x => x.Errors == null))
            {
                WriteLineBetween(ref firstProperty);
                WriteInterfaceProperty(propertyModel);
            }
        }

        /// <summary>
        /// Writes an interface property.
        /// </summary>
        protected virtual void WriteInterfaceProperty(PropertyTypeModel model)
        {
            WriteMemberComment(model, false);
            WriteGeneratedCodeAttribute();
            WriteIndent();
            WriteClrType(model.ValueTypeClrFullName);
            WriteLine($" {model.ClrName} {{ get; }}");
        }

        /// <summary>
        /// Writes the model class.
        /// </summary>
        protected virtual void WriteModelClass(ContentTypeModel model)
        {
            if (model.HasCustomClrName)
                WriteIndentLine($"// Content Type with alias \"{model.Alias}\"");

            WriteIndentLine(!string.IsNullOrWhiteSpace(model.Name)
                ? $"/// <summary>{XmlCommentString(model.Name)}</summary>"
                : $"/// <summary>Represents a \"{model.Alias}\" content item.</summary>");

            // cannot do it now. see note in ImplementContentTypeAttribute
            //if (!type.HasImplement)
            //    sb.AppendFormat("\t[ImplementContentType(\"{0}\")]\n", type.Alias);

            WriteIndent("[PublishedModel(");
            WriteContentTypeAliasConstant(model);
            WriteLine(")]");

            WriteIndent($"public partial class {model.ClrName}");

            var sep = " : ";
            if (!model.OmitBaseClass)
            {
                Write(sep);
                WriteClrType(model.BaseClassClrFullName);
                sep = ", ";
            }

            if (model.IsMixin)
            {
                // if it's a mixin it implements its own interface - which implements all the others
                Write(sep);
                WriteClrType(CodeModel.ModelsNamespace + ".I" + model.ClrName);
            }
            else
            {
                // write the mixins, if any, as interfaces
                // only if not a mixin because otherwise the interface already has them already
                foreach (var mixinType in model.LocalMixinContentTypes)
                {
                    Write(sep);
                    WriteClrType(CodeModel.ModelsNamespace + ".I" + mixinType.ClrName);
                }
            }

            WriteLine();

            WriteBlockStart();
            WriteModelClassBody(model);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes the model class body.
        /// </summary>
        protected virtual void WriteModelClassBody(ContentTypeModel model)
        {
            var first = true;

            // write the ctor
            if (WriteWithLineBetween(ref first, !model.OmitConstructor))
                WriteModelClassConstructor(model);

            // write the properties
            WriteModelClassMembers(ref first, model);
        }

        /// <summary>
        /// Writes the model class constructor.
        /// </summary>
        protected virtual void WriteModelClassConstructor(ContentTypeModel model)
        {
            WriteIndentLine($"/// <summary>Initializes a new instance of the {model.ClrName} class.</summary>");
            WriteGeneratedCodeAttribute();
            WriteIndentLine($"public {model.ClrName}(IPublished{(model.IsElement ? "Element" : "Content")} content)");
            Indent();
            WriteIndentLine(": base(content)");
            Outdent();
            WriteIndentLine("{ }");
        }

        /// <summary>
        /// Writes the model class members.
        /// </summary>
        protected virtual void WriteModelClassMembers(ref bool first, ContentTypeModel model)
        {
            foreach (var propertyTypeModel in model.ExpandedProperties.Where(x => x.Errors == null))
            {
                switch (CodeModel.ContentTypes.PropertyStyle)
                {
                    case PropertyStyle.Property:
                    case PropertyStyle.PropertyAndExtensionMethods:

                        WriteLineBetween(ref first);
                        WriteModelClassProperty(propertyTypeModel);
                        break;

                    case PropertyStyle.Methods:

                        switch (CodeModel.ContentTypes.FallbackStyle)
                        {
                            case FallbackStyle.Nothing:
                                WriteLineBetween(ref first);
                                WriteModelClassMethod(propertyTypeModel);
                                break;

                            case FallbackStyle.Classic:
                                WriteLineBetween(ref first);
                                WriteModelClassMethod(propertyTypeModel, WriteClassicFallbackParametersDeclaration, WriteClassicFallbackParametersUsage);
                                break;

                            case FallbackStyle.Modern:
                                WriteLineBetween(ref first);
                                WriteModelClassMethod(propertyTypeModel, WriteModernFallbackParametersDeclaration, WriteModernFallbackParametersUsage);
                                break;
                        }
                        break;

                    case PropertyStyle.ExtensionMethods:
                        // nothing here
                        break;

                    default:
                        throw new Exception("panic");
                }
            }
        }

        /// <summary>
        /// Writes a model class property.
        /// </summary>
        protected virtual void WriteModelClassProperty(PropertyTypeModel model)
        {
            WriteMemberComment(model, true);
            WriteGeneratedCodeAttribute();

            // cannot get rid of that one, because the embedded MB provides a method that uses it
            // FIXME: but, that method uses the ORIGINAL attribute = wtf?!
            WriteIndent("[ImplementPropertyType(");
            WritePropertyTypeAliasConstant(model);
            WriteLine(")]");

            WriteIndent("public virtual ");
            WriteClrType(model.ValueTypeClrFullName);
            WriteLine($" {model.ClrName} => {model.ContentType.ClrName}Extensions.{model.ClrName}(this);");
        }

        /// <summary>
        /// Writes a model class method.
        /// </summary>
        protected virtual void WriteModelClassMethod(PropertyTypeModel model, WriteParameters writeDeclaration = null, WriteParameters writeUsage = null)
        {
            WriteMemberComment(model, true);
            WriteGeneratedCodeAttribute();

            WriteIndent("public virtual ");
            WriteClrType(model.ValueTypeClrFullName);
            Write(" ");
            Write(model.ClrName);
            Write("(");
            var first = true;
            WriteVariantParametersDeclaration(ref first, model);
            writeDeclaration?.Invoke(ref first, model);
            WriteLine(")");

            Indent();

            WriteIndent("=> ");
            Write(model.ContentType.ClrName);
            Write("Extensions.");
            Write(model.ClrName);
            Write("(this");
            first = false;
            WriteVariantParametersUsage(ref first, model);
            writeUsage?.Invoke(ref first, model);
            WriteLine(");");

            Outdent();
        }

        /// <summary>
        /// Writes the extensions class.
        /// </summary>
        protected virtual void WriteExtensionsClass(ContentTypeModel model)
        {
            if (model.Properties.Count(x => x.Errors == null) == 0)
                return;

            WriteIndentLine($"/// <summary>Provides extensions for the {(model.IsMixin ? "I" : "")}{model.ClrName} {(model.IsMixin ? "interface" : "class")}.</summary>");
            WriteBlockStart($"public static partial class {model.ClrName}Extensions");
            WriteExtensionsClassBody(model);
            WriteBlockEnd();
        }

        /// <summary>
        /// Writes the extensions class body.
        /// </summary>
        protected virtual void WriteExtensionsClassBody(ContentTypeModel model)
        {
            WriteExtensionsClassMembers(model);
        }

        /// <summary>
        /// Writes the extensions class members.
        /// </summary>
        protected virtual void WriteExtensionsClassMembers(ContentTypeModel model)
        {
            var first = true;
            foreach (var propertyTypeModel in model.Properties.Where(x => x.Errors == null))
            {
                if (CodeModel.ContentTypes.PropertyStyle == PropertyStyle.Property)
                {
                    WriteLineBetween(ref first);
                    WriteExtensionsClassMethod(propertyTypeModel);
                    continue;
                }

                switch (CodeModel.ContentTypes.FallbackStyle)
                {
                    case FallbackStyle.Nothing:
                        WriteLineBetween(ref first);
                        WriteExtensionsClassMethod(propertyTypeModel);
                        break;

                    case FallbackStyle.Classic:
                        WriteLineBetween(ref first);
                        WriteExtensionsClassMethod(propertyTypeModel, WriteClassicFallbackParametersDeclaration, WriteClassicFallbackParametersUsage);
                        break;

                    case FallbackStyle.Modern:
                        WriteLineBetween(ref first);
                        WriteExtensionsClassMethod(propertyTypeModel, WriteModernFallbackParametersDeclaration, WriteModernFallbackParametersUsage, WriteModernValueGeneric);
                        break;
                }
            }
        }

        /// <summary>
        /// Writes an extensions class method.
        /// </summary>
        protected virtual void WriteExtensionsClassMethod(PropertyTypeModel model,
            WriteParameters writeDeclaration = null,
            WriteParameters writeUsage = null,
            Action<PropertyTypeModel> writeValueGeneric = null)
        {
            WriteMemberComment(model, true);
            WriteGeneratedCodeAttribute();

            var first = false;

            WriteIndent("public static ");
            WriteClrType(model.ValueTypeClrFullName);
            Write(" ");
            Write(model.ClrName);
            Write("(");
            if (CodeModel.ContentTypes.PropertyStyle == PropertyStyle.ExtensionMethods ||
                CodeModel.ContentTypes.PropertyStyle == PropertyStyle.PropertyAndExtensionMethods)
                Write("this ");
            Write($"{(model.ContentType.IsMixin ? "I" : "")}{model.ContentType.ClrName} model");
            WriteVariantParametersDeclaration(ref first, model);
            writeDeclaration?.Invoke(ref first, model);
            WriteLine(")");

            Indent();

            WriteIndent("=> model.Value");
            (writeValueGeneric ?? WriteDefaultValueGeneric).Invoke(model);
            Write("(");
            WritePropertyTypeAliasConstant(model);
            WriteVariantParametersUsage(ref first, model);
            writeUsage?.Invoke(ref first, model);
            WriteLine(");");

            Outdent();
        }

        /// <summary>
        /// Writes the default generic parameters of the Value method.
        /// </summary>
        protected virtual void WriteDefaultValueGeneric(PropertyTypeModel model)
        {
            // always write the generic Value<> version

            Write("<");
            WriteClrType(model.ValueTypeClrFullName); // could be 'object'
            Write(">");
        }

        /// <summary>
        /// Writes the generic parameters of the Value method for the modern fallback style.
        /// </summary>
        protected virtual void WriteModernValueGeneric(PropertyTypeModel model)
        {
            // write the generic Value<,> version

            var contentTypeClrName = model.ContentType.IsMixin
                ? "I" + model.ContentType.ClrName
                : model.ContentType.ClrName;

            Write("<");
            WriteClrType(contentTypeClrName);
            Write(", ");
            WriteClrType(model.ValueTypeClrFullName); // could be 'object'
            Write(">");
        }

        /// <summary>
        /// Writes the variant parameters declaration.
        /// </summary>
        protected virtual void WriteVariantParametersDeclaration(ref bool first, PropertyTypeModel model)
        {
            if (model.VariesByCulture())
            {
                WriteBetween(ref first, ", ");
                Write("string culture = null");
            }

            if (model.VariesBySegment())
            {
                WriteBetween(ref first, ", ");
                Write("string segment = null");
            }
        }

        /// <summary>
        /// Writes the variant parameters usage.
        /// </summary>
        protected virtual void WriteVariantParametersUsage(ref bool first, PropertyTypeModel model)
        {
            if (model.VariesByCulture())
            {
                WriteBetween(ref first, ", ");
                Write("culture: culture");
            }

            if (model.VariesBySegment())
            {
                WriteBetween(ref first, ", ");
                Write("segment: segment");
            }
        }

        /// <summary>
        /// Writes the fallback declaration for classic style.
        /// </summary>
        protected virtual void WriteClassicFallbackParametersDeclaration(ref bool first, PropertyTypeModel model)
        {
            WriteBetween(ref first, ", ");
            Write("Fallback fallback = default, ");
            WriteClrType(model.ValueTypeClrFullName);
            Write(" defaultValue = default");
        }

        /// <summary>
        /// Writes the fallback parameters usage for classic style.
        /// </summary>
        protected virtual void WriteClassicFallbackParametersUsage(ref bool first, PropertyTypeModel model)
        {
            WriteBetween(ref first, ", ");
            Write("fallback: fallback, defaultValue: defaultValue");
        }

        /// <summary>
        /// Writes the fallback parameters declaration for modern style.
        /// </summary>
        protected virtual void WriteModernFallbackParametersDeclaration(ref bool first, PropertyTypeModel model)
        {
            var contentTypeClrName = model.ContentType.IsMixin
                ? "I" + model.ContentType.ClrName
                : model.ContentType.ClrName;

            WriteBetween(ref first, ", ");
            Write("Func<FallbackInfos<");
            Write(contentTypeClrName);
            Write(", ");
            WriteClrType(model.ValueTypeClrFullName);
            Write(">, ");
            WriteClrType(model.ValueTypeClrFullName);
            Write("> fallback = default");
        }

        /// <summary>
        /// Writes the fallback parameters usage for modern style.
        /// </summary>
        protected virtual void WriteModernFallbackParametersUsage(ref bool first, PropertyTypeModel model)
        {
            WriteBetween(ref first, ", ");
            Write("fallback: fallback");
        }

        /// <summary>
        /// Writes the comment on a member (property or method).
        /// </summary>
        protected virtual void WriteMemberComment(PropertyTypeModel model, bool implement)
        {
            // adds xml summary to each property
            if (model.ContentType.IsMixin && implement)
            {
                // if the property is declared in a mixin, inherit from the interface
                WriteIndentLine("/// <inheritdoc />");
            }
            else if (!string.IsNullOrWhiteSpace(model.Name) || !string.IsNullOrWhiteSpace(model.Description))
            {
                // if we have a name of a description, build a comment
                var summary = XmlCommentString(model.Name);
                if (!string.IsNullOrWhiteSpace(model.Description))
                    summary += " (" + XmlCommentString(model.Description) + ")";
                WriteIndentLine($"/// <summary>Gets the {summary}.</summary>");
            }
            else
            {
                // write some default comment
                WriteIndentLine($"/// <summary>Gets the value of the \"{model.Alias}\" property.</summary>");
            }
        }

        /// <summary>
        /// Writes the constant name for a property type alias.
        /// </summary>
        protected virtual void WritePropertyTypeAliasConstant(PropertyTypeModel model)
        {
            Write(CodeModel.ModelInfosClassName);
            Write(".ContentTypes.");
            Write(model.ContentType.ClrName);
            Write(".Properties.");
            Write(model.ClrName);
            Write(".Alias");
        }

        /// <summary>
        /// Writes the constant name for a content type alias.
        /// </summary>
        protected virtual void WriteContentTypeAliasConstant(ContentTypeModel model)
        {
            Write(CodeModel.ModelInfosClassName);
            Write(".ContentTypes.");
            Write(model.ClrName);
            Write(".Alias");
        }

        // TODO: explain
        protected string ToInterface(string type)
        {
            if (type == CodeModel.ContentTypes.ContentBaseClassClrFullName)
                return CodeModel.ContentTypes.ContentBaseInterfaceClrFullName;

            if (type == CodeModel.ContentTypes.ElementBaseClassClrFullName)
                return CodeModel.ContentTypes.ElementBaseInterfaceClrFullName;

            var pos = type.LastIndexOf('.');
            return pos < 0
                ? "I" + type
                : type.Substring(0, pos + 1) + "I" + type.Substring(pos + 1);
        }
    }
}