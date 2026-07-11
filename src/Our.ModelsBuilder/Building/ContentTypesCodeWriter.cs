using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Building;

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
    protected delegate void WriteParameters(ref bool first, CodeModel codeMode, PropertyTypeModel model);

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
    public virtual void WriteModels(CodeModel codeModel, IEnumerable<ContentTypeModel> models)
    {
        var first = true;
        foreach (var typeModel in models)
        {
            WriteLineBetween(ref first);
            WriteModel(codeModel, typeModel);
        }
    }

    /// <summary>
    /// Writes a complete content type model.
    /// </summary>
    public virtual void WriteModel(CodeModel codeModel, ContentTypeModel model)
    {
        var first = true;

        foreach (var propertyTypeModel in model.ExpandedProperties.Where(x => x.Errors != null))
        {
            WriteLineBetween(ref first);
            WritePropertyWarning(model, propertyTypeModel);
        }

        if (WriteWithLineBetween(ref first, model.IsMixin)) // FIXME: model.IsMixin model.GenerateInterface
            WriteInterface(codeModel, model);

        if (WriteWithLineBetween(ref first, model.Properties.Count > 0)) // FIXME: model.HasProperties
            WriteExtensionsClass(codeModel, model);

        if (WriteWithLineBetween(ref first, true /*model.IsClass*/)) // FIXME: model.IsClass model.GenerateClass
            WriteModelClass(codeModel, model);
    }

    /// <summary>
    /// Writes the interface declaration.
    /// </summary>
    /// <remarks>Appends the properties with <see cref="WriteInterfaceMembers"/>.</remarks>
    protected virtual void WriteInterface(CodeModel codeModel, ContentTypeModel model)
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
            WriteClrType(codeModel, ToInterface(codeModel, model.BaseClassClrFullName));
            sep = ", ";
        }

        foreach (var mixinType in model.LocalMixinContentTypes)
        {
            Write(sep);
            WriteClrType(codeModel, codeModel.ModelsNamespace + ".I" + mixinType.ClrName);
        }

        WriteLine();
        WriteBlockStart();
        WriteInterfaceBody(codeModel, model);
        WriteBlockEnd();
    }

    /// <summary>
    /// Writes the interface body.
    /// </summary>
    protected virtual void WriteInterfaceBody(CodeModel codeModel, ContentTypeModel model)
    {
        if (codeModel.PropertyStyle == PropertyStyle.Property ||
            codeModel.PropertyStyle == PropertyStyle.PropertyAndExtensionMethods)
            WriteInterfaceMembers(codeModel, model);
    }

    /// <summary>
    /// Writes the interface members.
    /// </summary>
    protected virtual void WriteInterfaceMembers(CodeModel codeModel, ContentTypeModel model)
    {
        // write the properties - only the local (non-ignored) ones, we're an interface
        var firstProperty = true;
        foreach (var propertyModel in model.Properties.Where(x => x.Errors == null))
        {
            WriteLineBetween(ref firstProperty);
            WriteInterfaceProperty(codeModel, propertyModel);
        }
    }

    /// <summary>
    /// Writes an interface property.
    /// </summary>
    protected virtual void WriteInterfaceProperty(CodeModel codeModel, PropertyTypeModel model)
    {
        WriteMemberComment(model, false);
        WriteGeneratedCodeAttribute(codeModel);
        WriteIndent();
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        WriteLine($" {model.ClrName} {{ get; }}");
    }

    /// <summary>
    /// Writes the model class.
    /// </summary>
    protected virtual void WriteModelClass(CodeModel codeModel, ContentTypeModel model)
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
        WriteContentTypeAliasConstant(codeModel, model);
        WriteLine(")]");

        WriteIndent($"public partial class {model.ClrName}");

        var sep = " : ";
        if (!model.OmitBaseClass)
        {
            Write(sep);
            WriteClrType(codeModel, model.BaseClassClrFullName);
            sep = ", ";
        }

        if (model.IsMixin)
        {
            // if it's a mixin it implements its own interface - which implements all the others
            Write(sep);
            WriteClrType(codeModel, codeModel.ModelsNamespace + ".I" + model.ClrName);
        }
        else
        {
            // write the mixins, if any, as interfaces
            // only if not a mixin because otherwise the interface already has them already
            foreach (var mixinType in model.LocalMixinContentTypes)
            {
                Write(sep);
                WriteClrType(codeModel, codeModel.ModelsNamespace + ".I" + mixinType.ClrName);
            }
        }

        WriteLine();

        WriteBlockStart();
        WriteModelClassBody(codeModel, model);
        WriteBlockEnd();
    }

    /// <summary>
    /// Writes the model class body.
    /// </summary>
    protected virtual void WriteModelClassBody(CodeModel codeModel, ContentTypeModel model)
    {
        var first = true;

        // write the ctor
        if (WriteWithLineBetween(ref first, !model.OmitConstructor))
            WriteModelClassConstructor(codeModel, model);

        // write the properties
        WriteModelClassMembers(ref first, codeModel, model);
    }

    /// <summary>
    /// Writes the model class constructor.
    /// </summary>
    protected virtual void WriteModelClassConstructor(CodeModel codeModel, ContentTypeModel model)
    {
        WriteIndentLine($"/// <summary>Initializes a new instance of the {model.ClrName} class.</summary>");
        WriteGeneratedCodeAttribute(codeModel);
        WriteIndentLine($"public {model.ClrName}(IPublished{(model.IsElement ? "Element" : "Content")} content, IPublishedValueFallback fallback)");
        Indent();
        WriteIndentLine(": base(content, fallback)");
        Outdent();
        WriteIndentLine("{ }");
    }

    /// <summary>
    /// Writes the model class members.
    /// </summary>
    protected virtual void WriteModelClassMembers(ref bool first, CodeModel codeModel, ContentTypeModel model)
    {
        foreach (var propertyTypeModel in model.ExpandedProperties.Where(x => x.Errors == null))
        {
            switch (codeModel.PropertyStyle)
            {
                case PropertyStyle.Property:
                case PropertyStyle.PropertyAndExtensionMethods:

                    WriteLineBetween(ref first);
                    WriteModelClassProperty(codeModel, propertyTypeModel);
                    break;

                case PropertyStyle.Methods:

                    switch (codeModel.FallbackStyle)
                    {
                        case FallbackStyle.Nothing:
                            WriteLineBetween(ref first);
                            WriteModelClassMethod(codeModel, propertyTypeModel);
                            break;

                        case FallbackStyle.Classic:
                            WriteLineBetween(ref first);
                            WriteModelClassMethod(codeModel, propertyTypeModel, WriteClassicFallbackParametersDeclaration, WriteClassicFallbackParametersUsage);
                            break;

                        case FallbackStyle.Modern:
                            WriteLineBetween(ref first);
                            WriteModelClassMethod(codeModel, propertyTypeModel, WriteModernFallbackParametersDeclaration, WriteModernFallbackParametersUsage);
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
    protected virtual void WriteModelClassProperty(CodeModel codeModel, PropertyTypeModel model)
    {
        WriteMemberComment(model, true);
        WriteGeneratedCodeAttribute(codeModel);

        // cannot get rid of that one, because the embedded MB provides a method that uses it
        // FIXME: but, that method uses the ORIGINAL attribute = wtf?!
        WriteIndent("[ImplementPropertyType(");
        WritePropertyTypeAliasConstant(codeModel, model);
        WriteLine(")]");

        WriteIndent("public virtual ");
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        WriteLine($" {model.ClrName} => {model.ContentType.ClrName}Extensions.{model.ClrName}(this);");
    }

    /// <summary>
    /// Writes a model class method.
    /// </summary>
    protected virtual void WriteModelClassMethod(CodeModel codeModel, PropertyTypeModel model, WriteParameters writeDeclaration = null, WriteParameters writeUsage = null)
    {
        WriteMemberComment(model, true);
        WriteGeneratedCodeAttribute(codeModel);

        WriteIndent("public virtual ");
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        Write(" ");
        Write(model.ClrName);
        Write("(");
        var first = true;
        WriteVariantParametersDeclaration(ref first, model);
        writeDeclaration?.Invoke(ref first, codeModel, model);
        WriteLine(")");

        Indent();

        WriteIndent("=> ");
        Write(model.ContentType.ClrName);
        Write("Extensions.");
        Write(model.ClrName);
        Write("(this");
        first = false;
        WriteVariantParametersUsage(ref first, model);
        writeUsage?.Invoke(ref first, codeModel, model);
        WriteLine(");");

        Outdent();
    }

    /// <summary>
    /// Writes the extensions class.
    /// </summary>
    protected virtual void WriteExtensionsClass(CodeModel codeModel, ContentTypeModel model)
    {
        if (model.Properties.Count(x => x.Errors == null) == 0)
            return;

        WriteIndentLine($"/// <summary>Provides extensions for the {(model.IsMixin ? "I" : "")}{model.ClrName} {(model.IsMixin ? "interface" : "class")}.</summary>");
        WriteBlockStart($"public static partial class {model.ClrName}Extensions");
        WriteExtensionsClassBody(codeModel, model);
        WriteBlockEnd();
    }

    /// <summary>
    /// Writes the extensions class body.
    /// </summary>
    protected virtual void WriteExtensionsClassBody(CodeModel codeModel, ContentTypeModel model)
    {
        WriteExtensionsClassMembers(codeModel, model);
    }

    /// <summary>
    /// Writes the extensions class members.
    /// </summary>
    protected virtual void WriteExtensionsClassMembers(CodeModel codeModel, ContentTypeModel model)
    {
        var first = true;
        foreach (var propertyTypeModel in model.Properties.Where(x => x.Errors == null))
        {
            if (codeModel.PropertyStyle == PropertyStyle.Property)
            {
                WriteLineBetween(ref first);
                WriteExtensionsClassMethod(codeModel, propertyTypeModel);
                continue;
            }

            switch (codeModel.FallbackStyle)
            {
                case FallbackStyle.Nothing:
                    WriteLineBetween(ref first);
                    WriteExtensionsClassMethod(codeModel, propertyTypeModel);
                    break;

                case FallbackStyle.Classic:
                    WriteLineBetween(ref first);
                    WriteExtensionsClassMethod(codeModel, propertyTypeModel, WriteClassicFallbackParametersDeclaration, WriteClassicFallbackParametersUsage);
                    break;

                case FallbackStyle.Modern:
                    WriteLineBetween(ref first);
                    WriteExtensionsClassMethod(codeModel, propertyTypeModel, WriteModernFallbackParametersDeclaration, WriteModernFallbackParametersUsage, WriteModernValueGeneric);
                    break;
            }
        }
    }

    /// <summary>
    /// Writes an extensions class method.
    /// </summary>
    protected virtual void WriteExtensionsClassMethod(CodeModel codeModel, PropertyTypeModel model,
        WriteParameters writeDeclaration = null,
        WriteParameters writeUsage = null,
        Action<CodeModel, PropertyTypeModel> writeValueGeneric = null)
    {
        WriteMemberComment(model, true);
        WriteGeneratedCodeAttribute(codeModel);

        var first = false;

        WriteIndent("public static ");
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        Write(" ");
        Write(model.ClrName);
        Write("(");
        if (codeModel.PropertyStyle == PropertyStyle.ExtensionMethods ||
            codeModel.PropertyStyle == PropertyStyle.PropertyAndExtensionMethods)
            Write("this ");
        Write($"{(model.ContentType.IsMixin ? "I" : "")}{model.ContentType.ClrName} model");
        WriteVariantParametersDeclaration(ref first, model);
        writeDeclaration?.Invoke(ref first, codeModel, model);
        WriteLine(")");

        Indent();

        WriteIndent("=> model.Value");
        (writeValueGeneric ?? WriteDefaultValueGeneric).Invoke(codeModel, model);
        Write("(");
        WritePropertyTypeAliasConstant(codeModel, model);
        WriteVariantParametersUsage(ref first, model);
        writeUsage?.Invoke(ref first, codeModel, model);
        WriteLine(");");

        Outdent();
    }

    /// <summary>
    /// Writes the default generic parameters of the Value method.
    /// </summary>
    protected virtual void WriteDefaultValueGeneric(CodeModel codeModel, PropertyTypeModel model)
    {
        // always write the generic Value<> version

        Write("<");
        WriteClrType(codeModel, model.ValueTypeClrFullName); // could be 'object'
        Write(">");
    }

    /// <summary>
    /// Writes the generic parameters of the Value method for the modern fallback style.
    /// </summary>
    protected virtual void WriteModernValueGeneric(CodeModel codeModel, PropertyTypeModel model)
    {
        // write the generic Value<,> version

        var contentTypeClrName = model.ContentType.IsMixin
            ? "I" + model.ContentType.ClrName
            : model.ContentType.ClrName;

        var contentTypeClrFullName = codeModel.ModelsNamespace + "." + contentTypeClrName;

        Write("<");
        WriteClrType(codeModel, contentTypeClrFullName);
        Write(", ");
        WriteClrType(codeModel, model.ValueTypeClrFullName); // could be 'object'
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
    protected virtual void WriteClassicFallbackParametersDeclaration(ref bool first, CodeModel codeModel, PropertyTypeModel model)
    {
        WriteBetween(ref first, ", ");
        Write("Fallback fallback = default, ");
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        Write(" defaultValue = default");
    }

    /// <summary>
    /// Writes the fallback parameters usage for classic style.
    /// </summary>
    protected virtual void WriteClassicFallbackParametersUsage(ref bool first, CodeModel codeMode, PropertyTypeModel model)
    {
        WriteBetween(ref first, ", ");
        Write("fallback: fallback, defaultValue: defaultValue");
    }

    /// <summary>
    /// Writes the fallback parameters declaration for modern style.
    /// </summary>
    protected virtual void WriteModernFallbackParametersDeclaration(ref bool first, CodeModel codeModel, PropertyTypeModel model)
    {
        var contentTypeClrName = model.ContentType.IsMixin
            ? "I" + model.ContentType.ClrName
            : model.ContentType.ClrName;

        WriteBetween(ref first, ", ");
        Write("Func<FallbackInfos<");
        Write(contentTypeClrName);
        Write(", ");
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        Write(">, ");
        WriteClrType(codeModel, model.ValueTypeClrFullName);
        Write("> fallback = default");
    }

    /// <summary>
    /// Writes the fallback parameters usage for modern style.
    /// </summary>
    protected virtual void WriteModernFallbackParametersUsage(ref bool first, CodeModel codeMode, PropertyTypeModel model)
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
    protected virtual void WritePropertyTypeAliasConstant(CodeModel codeModel, PropertyTypeModel model)
    {
        Write(codeModel.ModelInfosClassName);
        Write(".ContentTypes.");
        Write(model.ContentType.ClrName);
        Write(".Properties.");
        Write(model.ClrName);
        Write(".Alias");
    }

    /// <summary>
    /// Writes the constant name for a content type alias.
    /// </summary>
    protected virtual void WriteContentTypeAliasConstant(CodeModel codeModel, ContentTypeModel model)
    {
        Write(codeModel.ModelInfosClassName);
        Write(".ContentTypes.");
        Write(model.ClrName);
        Write(".Alias");
    }

    // TODO: explain
    protected string ToInterface(CodeModel codeModel, string type)
    {
        if (type == codeModel.ContentTypes.ContentBaseClassClrFullName)
            return codeModel.ContentTypes.ContentBaseInterfaceClrFullName;

        if (type == codeModel.ContentTypes.ElementBaseClassClrFullName)
            return codeModel.ContentTypes.ElementBaseInterfaceClrFullName;

        var pos = type.LastIndexOf('.');
        return pos < 0
            ? "I" + type
            : type.Substring(0, pos + 1) + "I" + type.Substring(pos + 1);
    }
}