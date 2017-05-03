using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Strings;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.Umbraco
{
    internal class Application
    {
        #region Applicationmanagement

        //private bool _installedConfigSystem;
        private static readonly object LockO = new object();
        private static Application _application;
        //private global::Umbraco.Web.Standalone.StandaloneApplication _umbracoApplication;

        private Application()
        {
            //_standalone = false;
        }

        // get app in ASP.NET context ie it already exists, not standalone, don't start anything
        public static Application GetApplication()
        {
            lock (LockO)
            {
                if (_application == null)
                {
                    _application = new Application();
                    // do NOT start it!
                }
                return _application;
            }
        }

        #endregion

        #region Services

        public IList<TypeModel> GetAllTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var types = new List<TypeModel>();

            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            types.AddRange(GetTypes(PublishedItemType.Content, contentTypeService.GetAllContentTypes().Cast<IContentTypeBase>().ToArray()));
            types.AddRange(GetTypes(PublishedItemType.Media, contentTypeService.GetAllMediaTypes().Cast<IContentTypeBase>().ToArray()));

            var memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            types.AddRange(GetTypes(PublishedItemType.Member, memberTypeService.GetAll().Cast<IContentTypeBase>().ToArray()));

            return EnsureDistinctAliases(types);
        }

        public IList<TypeModel> GetContentTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var contentTypes = contentTypeService.GetAllContentTypes().Cast<IContentTypeBase>().ToArray();
            return GetTypes(PublishedItemType.Content, contentTypes); // aliases have to be unique here
        }

        public IList<TypeModel> GetMediaTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var contentTypes = contentTypeService.GetAllMediaTypes().Cast<IContentTypeBase>().ToArray();
            return GetTypes(PublishedItemType.Media, contentTypes); // aliases have to be unique here
        }

        public IList<TypeModel> GetMemberTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            var memberTypes = memberTypeService.GetAll().Cast<IContentTypeBase>().ToArray();
            return GetTypes(PublishedItemType.Member, memberTypes); // aliases have to be unique here
        }

        public static string GetClrName(string name, string alias)
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

            switch (UmbracoConfig.For.ModelsBuilder().ClrNameSource)
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

        private static IList<TypeModel> GetTypes(PublishedItemType itemType, IContentTypeBase[] contentTypes)
        {
            var typeModels = new List<TypeModel>();
            var uniqueTypes = new HashSet<string>();

            // get the types and the properties
            foreach (var contentType in contentTypes)
            {
                var typeModel = new TypeModel
                {
                    Id = contentType.Id,
                    Alias = contentType.Alias,
                    ClrName = GetClrName(contentType.Name, contentType.Alias),
                    ParentId = contentType.ParentId,

                    Name = contentType.Name,
                    Description = contentType.Description
                };

                // of course this should never happen, but when it happens, better detect it
                // else we end up with weird nullrefs everywhere
                if (uniqueTypes.Contains(typeModel.ClrName))
                    throw new Exception($"Panic: duplicate type ClrName \"{typeModel.ClrName}\".");
                uniqueTypes.Add(typeModel.ClrName);

                switch (itemType)
                {
                    case PublishedItemType.Content:
                        typeModel.ItemType = TypeModel.ItemTypes.Content;
                        break;
                    case PublishedItemType.Media:
                        typeModel.ItemType = TypeModel.ItemTypes.Media;
                        break;
                    case PublishedItemType.Member:
                        typeModel.ItemType = TypeModel.ItemTypes.Member;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unsupported PublishedItemType \"{0}\".", itemType));
                }

                typeModels.Add(typeModel);

                var publishedContentType = PublishedContentType.Get(itemType, contentType.Alias);

                foreach (var propertyType in contentType.PropertyTypes)
                {
                    var propertyModel = new PropertyModel
                    {
                        Alias = propertyType.Alias,
                        ClrName = GetClrName(propertyType.Name, propertyType.Alias),

                        Name = propertyType.Name,
                        Description = propertyType.Description
                    };

                    var publishedPropertyType = publishedContentType.GetPropertyType(propertyType.Alias);
                    if (publishedPropertyType == null)
                        throw new Exception($"Panic: could not get published property type {contentType.Alias}.{propertyType.Alias}.");

                    propertyModel.ClrType = publishedPropertyType.ClrType;

                    typeModel.Properties.Add(propertyModel);
                }
            }

            // wire the base types
            foreach (var typeModel in typeModels.Where(x => x.ParentId > 0))
            {
                typeModel.BaseType = typeModels.SingleOrDefault(x => x.Id == typeModel.ParentId);
                // Umbraco 7.4 introduces content types containers, so even though ParentId > 0, the parent might
                // not be a content type - here we assume that BaseType being null while ParentId > 0 means that
                // the parent is a container (and we don't check).
                typeModel.IsParent = typeModel.BaseType != null;
            }

            // discover mixins
            foreach (var contentType in contentTypes)
            {
                var typeModel = typeModels.SingleOrDefault(x => x.Id == contentType.Id);
                if (typeModel == null) throw new Exception("Panic: no type model matching content type.");

                IEnumerable<IContentTypeComposition> compositionTypes;
                var contentTypeAsMedia = contentType as IMediaType;
                var contentTypeAsContent = contentType as IContentType;
                var contentTypeAsMember = contentType as IMemberType;
                if (contentTypeAsMedia != null) compositionTypes = contentTypeAsMedia.ContentTypeComposition;
                else if (contentTypeAsContent != null) compositionTypes = contentTypeAsContent.ContentTypeComposition;
                else if (contentTypeAsMember != null) compositionTypes = contentTypeAsMember.ContentTypeComposition;
                else throw new Exception(string.Format("Panic: unsupported type \"{0}\".", contentType.GetType().FullName));

                foreach (var compositionType in compositionTypes)
                {
                    var compositionModel = typeModels.SingleOrDefault(x => x.Id == compositionType.Id);
                    if (compositionModel == null) throw new Exception("Panic: composition type does not exist.");

                    if (compositionType.Id == contentType.ParentId) continue;

                    // add to mixins
                    typeModel.MixinTypes.Add(compositionModel);

                    // mark as mixin - as well as parents
                    compositionModel.IsMixin = true;
                    while ((compositionModel = compositionModel.BaseType) != null)
                        compositionModel.IsMixin = true;
                }
            }

            return typeModels;
        }

        internal static IList<TypeModel> EnsureDistinctAliases(IList<TypeModel> typeModels)
        {
            var groups = typeModels.GroupBy(x => x.Alias.ToLowerInvariant());
            foreach (var group in groups.Where(x => x.Count() > 1))
            {
                throw new NotSupportedException($"Alias \"{group.Key}\" is used by types"
                    + $" {string.Join(", ", group.Select(x => x.ItemType + ":\"" + x.Alias + "\""))}. Aliases have to be unique."
                    + " One of the aliases must be modified in order to use the ModelsBuilder.");
            }
            return typeModels;
        }

        #endregion
    }
}
