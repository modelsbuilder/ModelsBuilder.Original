using System;
using System.Collections.Generic;
using System.Linq;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Strings;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Builds the content types code model.
    /// </summary>
    public class ContentTypesCodeModelBuilder
    {
        public ContentTypesCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions)
        {
            Options = options;
            CodeOptions = codeOptions;
            ContentTypesCodeOptions = codeOptions.ContentTypes;
        }

        protected ModelsBuilderOptions Options { get; }

        protected CodeOptions CodeOptions { get; } // FIXME: not pretty

        protected ContentTypesCodeOptions ContentTypesCodeOptions { get; }

        /// <summary>
        /// Builds the content types code model.
        /// </summary>
        public virtual void Build(CodeModel model)
        {
            var contentTypesModel = model.ContentTypes;

            contentTypesModel.ElementBaseClassClrFullName = ContentTypesCodeOptions.ElementBaseClassClrFullName;
            contentTypesModel.ElementBaseInterfaceClrFullName = ContentTypesCodeOptions.ElementBaseInterfaceClrFullName;
            contentTypesModel.ContentBaseClassClrFullName = ContentTypesCodeOptions.ContentBaseClassClrFullName;
            contentTypesModel.ContentBaseInterfaceClrFullName = ContentTypesCodeOptions.ContentBaseInterfaceClrFullName;

            // assign content types Clr names = we know the class name of each content type
            // these are local names (within the models namespace)
            AssignContentTypesClrNames(contentTypesModel);

            // assign property types Clr names = we know the property name of each property type
            AssignPropertyTypesClrNames(contentTypesModel);

            // assign Clr names for content types base classes
            // these are fully qualified names (with namespace)
            AssignContentTypeBaseClassClrNames(contentTypesModel, model.ModelsNamespace);

            // identify and remove ignored content and property types
            FilterIgnoredContentTypes(contentTypesModel);
            FilterIgnoredPropertyTypes(contentTypesModel);

            // tag content types which are mixins and/or parents
            IdentifyMixinsAndParents(contentTypesModel);

            // assign Clr names for property types value types
            AssignPropertyTypesValueTypeClrName(contentTypesModel);

            // validates that names are ok (no duplicates, no collisions)
            ValidateContentAndPropertyTypesClrNames(contentTypesModel);

            // validates that inheritance is OK (no content inheriting from element, etc)
            ValidateContentTypesInheritance(contentTypesModel);

            // omit some elements
            foreach (var contentTypeModel in contentTypesModel.ContentTypes)
            {
                if (ContentTypesCodeOptions.OmitContentTypeConstructor(contentTypeModel.ClrName))
                    contentTypeModel.OmitConstructor = true;
            }

            // at that point,
            // MixinContentTypes contains the local mixins
            // - some may be redundant because implicit (e.g. having A and B but A is composed of B)
            //   in which case we want to remove the implicit one (here, B) to build the list of interface
            //   that the content model needs to declare it implements
            // - some may already be implemented by the parent whereas others need to actually be
            //   implemented by the content model
            // FIXME: revisit implementing & mixins

            var visitor = new ContentTypeMixinVisitor();
            foreach (var contentTypeModel in contentTypesModel.ContentTypes)
            {
                var declare = new HashSet<ContentTypeModel>();
                var implement = new HashSet<ContentTypeModel>();
                var noDeclare = new List<ContentTypeModel>();
                var noImplement = new List<ContentTypeModel>();
                visitor.Visit(contentTypeModel, (mixin, kind) =>
                {
                    switch (kind)
                    {
                        case ContentTypeMixinVisitor.MixinKind.Transitive:
                            noDeclare.Add(mixin); // don't declare
                            implement.Add(mixin); // implement (unless...)
                            break;
                        case ContentTypeMixinVisitor.MixinKind.Inherited:
                        case ContentTypeMixinVisitor.MixinKind.Parent:
                            noDeclare.Add(mixin); // don't declare
                            noImplement.Add(mixin); // don't implement
                            break;
                        case ContentTypeMixinVisitor.MixinKind.Direct:
                            declare.Add(mixin); // declare (unless...)
                            implement.Add(mixin); // implement (unless...)
                            break;
                    }
                });

                contentTypeModel.LocalMixinContentTypes.AddRange(declare.Except(noDeclare));
                contentTypeModel.ExpandedMixinContentTypes.AddRange(implement.Except(noImplement));
            }

            // expand properties
            foreach (var contentTypeModel in contentTypesModel.ContentTypes)
            {
                // FIXME: ignores are not defined yet
                // for some reason... I define a base class for 2 content types, but it's not a parent nor a mixin
                // and I want to ignore a property... I define the ignore on the base class... it should be picked
                // BUT model.IgnoreMixinProperty() how should it work then?

                contentTypeModel.ExpandedProperties.AddRange(contentTypeModel.Properties);
                foreach (var mixinContentTypeModel in contentTypeModel.ExpandedMixinContentTypes)
                    contentTypeModel.ExpandedProperties.AddRange(mixinContentTypeModel.Properties.Where(x => !contentTypeModel.IgnoredMixinProperties.Contains(x)));
            }

            // detect mixin properties that have local implementations
            foreach (var contentTypeModel in contentTypesModel.ContentTypes)
            {
                foreach (var mixinPropertyType in contentTypeModel.ExpandedMixinContentTypes.SelectMany(x => x.Properties))
                {
                    // the content type model needs to implement the property,
                    // unless it has been ignored (locally, not at mixin level=

                    if (ContentTypesCodeOptions.IsPropertyIgnored(contentTypeModel.ClrName, mixinPropertyType.Alias))
                        contentTypeModel.IgnoredMixinProperties.Add(mixinPropertyType);
                }
            }

            SortContentTypeModels(contentTypesModel);
        }

        /// <summary>
        /// Assigns content types Clr names.
        /// </summary>
        protected virtual void AssignContentTypesClrNames(ContentTypesCodeModel model)
        {
            // assign Clr names
            // these are local Clr names e.g. "Product" or "Prices"
            foreach (var contentTypeModel in model.ContentTypes)
            {
                // implicit
                contentTypeModel.ClrName = GetClrName(contentTypeModel);

                // custom, specified by the transform
                var contentTypeClrName = ContentTypesCodeOptions.GetContentTypeClrName(contentTypeModel.Alias);
                if (contentTypeClrName != null)
                {
                    contentTypeModel.ClrName = contentTypeClrName;
                    contentTypeModel.HasCustomClrName = true; // indicates that the name might not match the alias
                }
            }

            // map, as soon as we have content types Clr names, as many things
            // depend on it, including GetPropertyTypeClrName right below
            ContentTypesCodeOptions.MapContentTypeAliasesToClrNames(model.ContentTypes.ToDictionary(x => x.Alias, x => x.ClrName));
        }

        /// <summary>
        /// Assigns the property types Clr names.
        /// </summary>
        protected virtual void AssignPropertyTypesClrNames(ContentTypesCodeModel model)
        {
            foreach (var contentTypeModel in model.ContentTypes)
            {
                foreach (var propertyTypeModel in contentTypeModel.Properties)
                {
                    // implicit
                    propertyTypeModel.ClrName = GetClrName(propertyTypeModel);

                    // custom, specified by the transform
                    var propertyTypeClrName = ContentTypesCodeOptions.GetPropertyTypeClrName(contentTypeModel.ClrName, propertyTypeModel.Alias);
                    if (propertyTypeClrName != null)
                    {
                        propertyTypeModel.ClrName = propertyTypeClrName;
                        //propertyTypeModel.HasCustomClrName = true; // indicates that the name might not match the alias
                    }
                }
            }
        }

        /// <summary>
        /// Ignores content types.
        /// </summary>
        protected virtual void FilterIgnoredContentTypes(ContentTypesCodeModel model)
        {
            // tag directly ignored content types
            foreach (var contentTypeModel in model.ContentTypes.Where(x => ContentTypesCodeOptions.IsContentTypeIgnored(x.Alias)))
                contentTypeModel.IsIgnored = true;

            // propagate ignored content types to children
            // if a content type is ignored, its children are ignored too
            foreach (var contentTypeModel in model.ContentTypes.Where(x => !x.IsIgnored && x.EnumerateBaseTypes().Any(xx => xx.IsIgnored)))
                contentTypeModel.IsIgnored = true;

            // unplug ignored content types
            foreach (var contentTypeModel in model.ContentTypes)
            {
                // from base
                if (contentTypeModel.BaseContentType != null && contentTypeModel.BaseContentType.IsIgnored)
                    contentTypeModel.BaseContentType = null;

                // from mixins
                contentTypeModel.MixinContentTypes.RemoveAll(x => x.IsIgnored);
            }

            // remove ignored content types
            model.ContentTypes.RemoveAll(x => x.IsIgnored);
        }

        /// <summary>
        /// Ignores property types.
        /// </summary>
        protected virtual void FilterIgnoredPropertyTypes(ContentTypesCodeModel model)
        {
            // FIXME ignoring properties?
            /*
            bool IsPropertyIgnored(ContentTypeModel model, string propertyTypeAlias)
            {
                // need to enumerate the hierarchy
                // - base types
                // - mixins
                // - actual classes and interfaces

                // note: is 'enumerate base types' just a way to visit parents/inherited?
            }
            */

            // tag ignored property types
            foreach (var contentTypeModel in model.ContentTypes)
            {
                foreach (var propertyTypeModel in contentTypeModel.Properties)
                {
                    // contentTypeModel.Properties contains the properties local to the content type and nothing
                    // else, however it is possible to ignore a property type that does *not* belong to a content type,
                    // and then it will be ignored for all child content types
                    // TODO: what about mixins?
                    var ignore = contentTypeModel.EnumerateBaseTypes(true).Any(x => ContentTypesCodeOptions.IsPropertyIgnored(x.ClrName, propertyTypeModel.Alias));
                    if (ignore)
                        propertyTypeModel.IsIgnored = true;
                }
            }

            // remove ignored property types
            foreach (var contentTypeModel in model.ContentTypes)
                contentTypeModel.Properties.RemoveAll(x => x.IsIgnored);
        }

        /// <summary>
        /// Identifies mixins and parents.
        /// </summary>
        protected virtual void IdentifyMixinsAndParents(ContentTypesCodeModel model)
        {
            // tag mixins
            foreach (var contentTypeModel in model.ContentTypes.SelectMany(x => x.MixinContentTypes))
                contentTypeModel.IsMixin = true;

            // propagate mixins to parents of mixins
            foreach (var contentTypeModel in model.ContentTypes.Where(x => x.IsMixin).SelectMany(x => x.EnumerateBaseTypes()))
                contentTypeModel.IsMixin = true;

            // tag parents
            foreach (var contentTypeModel in model.ContentTypes.SelectMany(x => x.EnumerateBaseTypes()))
                contentTypeModel.IsParent = true;
        }

        /// <summary>
        /// Assigns property type value types Clr names.
        /// </summary>
        protected virtual void AssignPropertyTypesValueTypeClrName(ContentTypesCodeModel model)
        {
            // now that we have Clr names for content types, we can map property value
            // types i.e. get the actual property value type Clr name, maybe mapping
            // ModelType types - all to Clr full names
            // TODO: ModelType.MapToName is probably inefficiently allocating dictionaries
            var map = model.ContentTypes.ToDictionary(x => x.Alias, x => CodeOptions.ModelsNamespace + "." + x.ClrName);
            foreach (var propertyTypeModel in model.ContentTypes.SelectMany(x => x.Properties))
                propertyTypeModel.ValueTypeClrFullName = ModelType.MapToName(propertyTypeModel.ValueType, map);
        }

        /// <summary>
        /// Validates content and property types Clr names.
        /// </summary>
        protected virtual void ValidateContentAndPropertyTypesClrNames(ContentTypesCodeModel model)
        {
            var isPureLive = Options.ModelsMode == ModelsMode.PureLive;

            // ensure we have no duplicates type names
            // and throw: just cannot build
            foreach (var errorContentTypeGroup in model.ContentTypes.GroupBy(x => x.ClrName).Where(x => x.Count() > 1))
                throw new InvalidOperationException($"Type name \"{errorContentTypeGroup.Key}\" is used"
                                                    + $" for types with alias {string.Join(", ", errorContentTypeGroup.Select(x => x.Kind + ":\"" + x.Alias + "\""))}. Names have to be unique."
                                                    + " Consider using an attribute to assign different names to conflicting types.");

            // ensure we have no duplicates property names
            // and throw: just cannot build
            foreach (var contentTypeModel in model.ContentTypes)
            foreach (var errorPropertyTypeGroup in contentTypeModel.Properties.GroupBy(x => x.ClrName).Where(x => x.Count() > 1))
                throw new InvalidOperationException($"Property name \"{errorPropertyTypeGroup.Key}\" in type {contentTypeModel.Kind}:\"{contentTypeModel.Alias}\""
                                                    + $" is used for properties with alias {string.Join(", ", errorPropertyTypeGroup.Select(x => "\"" + x.Alias + "\""))}. Names have to be unique."
                                                    + " Consider using an attribute to assign different names to conflicting properties.");

            // ensure content & property type don't have identical names (CSharp cannot handle it)
            // and don't necessarily throw, raise error on property, won't be generated
            foreach (var contentTypeModel in model.ContentTypes)
            {
                foreach (var propertyTypeModel in contentTypeModel.Properties.Where(x => x.ClrName == contentTypeModel.ClrName))
                {
                    if (!isPureLive)
                        throw new InvalidOperationException($"The model class for content type with alias \"{contentTypeModel.Alias}\" is named \"{propertyTypeModel.ClrName}\"."
                                                            + $" CSharp does not support using the same name for the property with alias \"{propertyTypeModel.Alias}\"."
                                                            + " Consider using an attribute to assign a different name to the property.");

                    // for purelive, will we generate a commented out properties with an error message,
                    // instead of throwing, because then it kills the sites and ppl don't understand why
                    propertyTypeModel.AddError($"The class {contentTypeModel.ClrName} cannot implement this property, because"
                                               + $" CSharp does not support naming the property with alias \"{propertyTypeModel.Alias}\" with the same name as content type with alias \"{contentTypeModel.Alias}\"."
                                               + " Consider using an attribute to assign a different name to the property.");
                }
            }
        }

        /// <summary>
        /// Assigns content types base class Clr names.
        /// </summary>
        protected virtual void AssignContentTypeBaseClassClrNames(ContentTypesCodeModel model, string modelsNamespace)
        {
            // assign base class Clr names
            foreach (var contentTypeModel in model.ContentTypes)
            {
                // if the content type has a custom base class, register we need to omit generating
                // the base class (and leave its name null), otherwise get and assign a name, which
                // can be the parent content type, or the default content/element base class, or
                // anything really

                if (ContentTypesCodeOptions.OmitContentTypeBaseClass(contentTypeModel.ClrName))
                {
                    contentTypeModel.BaseClassClrFullName = ContentTypesCodeOptions.ContentTypeBaseClass(contentTypeModel.ClrName);
                    contentTypeModel.OmitBaseClass = true;
                }
                else
                {
                    contentTypeModel.BaseClassClrFullName = GetContentTypeBaseClassClrFullName(contentTypeModel, modelsNamespace);
                }
            }
        }

        /// <summary>
        /// Validates content types inheritance.
        /// </summary>
        protected virtual void ValidateContentTypesInheritance(ContentTypesCodeModel model)
        {
            // ensure elements don't inherit from, or are composed of, non-elements
            foreach (var contentTypeModel in model.ContentTypes.Where(x => !x.IsIgnored && x.IsElement))
            {
                if (contentTypeModel.BaseContentType != null && contentTypeModel.BaseContentType.IsNotElement)
                    throw new Exception($"Cannot generate model for type '{contentTypeModel.Alias}' because it is an element type, but its parent type '{contentTypeModel.BaseContentType.Alias}' is not.");

                var errs = contentTypeModel.MixinContentTypes.Where(x => x.IsNotElement).ToList();
                if (errs.Count > 0)
                    throw new Exception($"Cannot generate model for type '{contentTypeModel.Alias}' because it is an element type, but it is composed of {string.Join(", ", errs.Select(x => "'" + x.Alias + "'"))} which {(errs.Count == 1 ? "is" : "are")} not.");
            }
        }

        /// <summary>
        /// Sorts the content type models, their properties, etc.
        /// </summary>
        protected virtual void SortContentTypeModels(ContentTypesCodeModel model)
        {
            model.ContentTypes.Sort((x, y) => string.Compare(x.ClrName, y.ClrName, StringComparison.InvariantCulture));

            foreach (var contentTypeModel in model.ContentTypes)
            {
                contentTypeModel.Properties.Sort((x, y) => string.Compare(x.ClrName, y.ClrName, StringComparison.InvariantCulture));
                contentTypeModel.ExpandedProperties.Sort((x, y) => string.Compare(x.ClrName, y.ClrName, StringComparison.InvariantCulture));

                contentTypeModel.MixinContentTypes.Sort((x, y) => string.Compare(x.ClrName, y.ClrName, StringComparison.InvariantCulture));
                contentTypeModel.LocalMixinContentTypes.Sort((x, y) => string.Compare(x.ClrName, y.ClrName, StringComparison.InvariantCulture));
                contentTypeModel.ExpandedMixinContentTypes.Sort((x, y) => string.Compare(x.ClrName, y.ClrName, StringComparison.InvariantCulture));
            }
        }

        /// <summary>
        /// Gets the Clr name for a type model.
        /// </summary>
        protected virtual string GetClrName(ContentTypeModel contentTypeModel)
            => GetClrName(contentTypeModel.Name, contentTypeModel.Alias);

        /// <summary>
        /// Gets the Clr name for a property model.
        /// </summary>
        protected virtual string GetClrName(PropertyTypeModel propertyModel)
            => GetClrName(propertyModel.Name, propertyModel.Alias);

        /// <summary>
        /// Gets the Clr name for a name, alias pair.
        /// </summary>
        protected virtual string GetClrName(string name, string alias)
        {
            // ideally we should just be able to re-use Umbraco's alias,
            // just upper-casing the first letter, however in v7 for backward
            // compatibility reasons aliases derive from names via ToSafeAlias which is
            //   PreFilter = ApplyUrlReplaceCharacters,
            //   IsTerm = (c, leading) => leading
            //     ? char.IsLetter(c) // only letters
            //     : (char.IsLetterOrDigit(c) || c == '_'), // letter, digit or underscore
            //   StringType = CleanStringType.Ascii | CleanStringType.UmbracoCase,
            //   BreakTermsOnUpper = false
            //
            // but that is not ideal with acronyms and casing
            // however we CANNOT change Umbraco
            // so, adding a way to "do it right" deriving from name, here

            switch (ContentTypesCodeOptions.ClrNameSource)
            {
                case ClrNameSource.RawAlias:
                    // use Umbraco's alias
                    return alias;

                case ClrNameSource.Alias:
                    // ModelsBuilder's legacy - but not ideal
                    return alias.ToCleanString(CleanStringType.ConvertCase | CleanStringType.PascalCase);

                case ClrNameSource.Name:
                    // derive from name
                    var source = name.TrimStart('_'); // because CleanStringType.ConvertCase accepts them
                    return source.ToCleanString(CleanStringType.ConvertCase | CleanStringType.PascalCase | CleanStringType.Ascii);

                default:
                    throw new Exception("Invalid ClrNameSource.");
            }
        }

        /// <summary>
        /// Gets the Clr name for the base class of a type model.
        /// </summary>
        protected virtual string GetContentTypeBaseClassClrFullName(ContentTypeModel contentTypeModel, string modelsNamespace)
        {
            if (contentTypeModel.OmitBaseClass)
                return null;

            if (contentTypeModel.BaseContentType != null)
                return modelsNamespace + "." + contentTypeModel.BaseContentType.ClrName;

            return contentTypeModel.IsElement ? ContentTypesCodeOptions.ElementBaseClassClrFullName : ContentTypesCodeOptions.ContentBaseClassClrFullName;
        }
    }
}