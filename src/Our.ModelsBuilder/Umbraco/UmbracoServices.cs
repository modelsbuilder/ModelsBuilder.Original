using System;
using System.Collections.Generic;
using System.Linq;
using Our.ModelsBuilder.Building;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Our.ModelsBuilder.Umbraco
{
    public class UmbracoServices
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IMediaTypeService _mediaTypeService;
        private readonly IMemberTypeService _memberTypeService;
        private readonly IPublishedContentTypeFactory _publishedContentTypeFactory;

        public UmbracoServices(IContentTypeService contentTypeService, IMediaTypeService mediaTypeService, IMemberTypeService memberTypeService, IPublishedContentTypeFactory publishedContentTypeFactory)
        {
            _contentTypeService = contentTypeService;
            _mediaTypeService = mediaTypeService;
            _memberTypeService = memberTypeService;
            _publishedContentTypeFactory = publishedContentTypeFactory;
        }

        #region Services

        public CodeModelData GetModelSource()
        {
            return new CodeModelData { ContentTypes = GetAllTypes() };
        }

        public List<ContentTypeModel> GetAllTypes()
        {
            var types = new List<ContentTypeModel>();

            types.AddRange(GetTypes(PublishedItemType.Content, _contentTypeService.GetAll().Cast<IContentTypeComposition>().ToArray()));
            types.AddRange(GetTypes(PublishedItemType.Media, _mediaTypeService.GetAll().Cast<IContentTypeComposition>().ToArray()));
            types.AddRange(GetTypes(PublishedItemType.Member, _memberTypeService.GetAll().Cast<IContentTypeComposition>().ToArray()));

            return EnsureDistinctAliases(types);
        }

        public List<ContentTypeModel> GetContentTypes()
        {
            var contentTypes = _contentTypeService.GetAll().Cast<IContentTypeComposition>().ToArray();
            return GetTypes(PublishedItemType.Content, contentTypes); // aliases have to be unique here
        }

        public List<ContentTypeModel> GetMediaTypes()
        {
            var contentTypes = _mediaTypeService.GetAll().Cast<IContentTypeComposition>().ToArray();
            return GetTypes(PublishedItemType.Media, contentTypes); // aliases have to be unique here
        }

        public List<ContentTypeModel> GetMemberTypes()
        {
            var memberTypes = _memberTypeService.GetAll().Cast<IContentTypeComposition>().ToArray();
            return GetTypes(PublishedItemType.Member, memberTypes); // aliases have to be unique here
        }

        private List<ContentTypeModel> GetTypes(PublishedItemType itemType, IContentTypeComposition[] contentTypes)
        {
            var contentTypeModels = new List<ContentTypeModel>();

            // get the types and the properties
            foreach (var contentType in contentTypes)
            {
                var contentTypeModel = new ContentTypeModel
                {
                    Id = contentType.Id,
                    Alias = contentType.Alias,
                    ParentId = contentType.ParentId,

                    Name = contentType.Name,
                    Description = contentType.Description,
                    Variations = contentType.Variations
                };

                var publishedContentType = _publishedContentTypeFactory.CreateContentType(contentType);
                contentTypeModel.Kind = publishedContentType.ItemType == PublishedItemType.Element ? ContentTypeKind.Element : itemType switch
                {
                    PublishedItemType.Content => ContentTypeKind.Content,
                    PublishedItemType.Media => ContentTypeKind.Media,
                    PublishedItemType.Member => ContentTypeKind.Member,
                    _ => throw new InvalidOperationException($"Unsupported PublishedItemType \"{itemType}\".")
                };

                contentTypeModels.Add(contentTypeModel);

                foreach (var propertyType in contentType.PropertyTypes)
                {
                    var propertyTypeModel = new PropertyTypeModel
                    {
                        Alias = propertyType.Alias,
                        EditorAlias = propertyType.PropertyEditorAlias,
                        ContentType = contentTypeModel,

                        Name = propertyType.Name,
                        Description = propertyType.Description,
                        Variations = propertyType.Variations
                    };

                    var publishedPropertyType = publishedContentType.GetPropertyType(propertyType.Alias);
                    if (publishedPropertyType == null)
                        throw new Exception($"Panic: could not get published property type {contentType.Alias}.{propertyType.Alias}.");

                    propertyTypeModel.ValueType = publishedPropertyType.ModelClrType;

                    contentTypeModel.Properties.Add(propertyTypeModel);
                }
            }

            // wire the base types
            foreach (var contentTypeModel in contentTypeModels.Where(x => x.ParentId > 0))
            {
                contentTypeModel.BaseContentType = contentTypeModels.SingleOrDefault(x => x.Id == contentTypeModel.ParentId);
            }

            // discover mixins
            foreach (var contentType in contentTypes)
            {
                var contentTypeModel = contentTypeModels.SingleOrDefault(x => x.Id == contentType.Id);
                if (contentTypeModel == null) throw new Exception("Panic: no type model matching content type.");

                var compositionTypes = contentType switch
                {
                    IMediaType contentTypeAsMedia => contentTypeAsMedia.ContentTypeComposition,
                    IContentType contentTypeAsContent => contentTypeAsContent.ContentTypeComposition,
                    IMemberType contentTypeAsMember => contentTypeAsMember.ContentTypeComposition,
                    _ => throw new Exception($"Panic: unsupported type \"{contentType.GetType().FullName}\".")
                };

                foreach (var compositionType in compositionTypes)
                {
                    var compositionContentTypeModel = contentTypeModels.SingleOrDefault(x => x.Id == compositionType.Id);
                    if (compositionContentTypeModel == null) throw new Exception("Panic: composition type does not exist.");

                    // exclude the parent
                    if (compositionType.Id == contentType.ParentId) continue;

                    // add others to mixins
                    contentTypeModel.MixinContentTypes.Add(compositionContentTypeModel);
                }
            }

            return contentTypeModels;
        }

        internal static List<ContentTypeModel> EnsureDistinctAliases(List<ContentTypeModel> contentTypeModels)
        {
            foreach (var errorContentTypeModelGroup in contentTypeModels.GroupBy(x => x.Alias.ToLowerInvariant()).Where(x => x.Count() > 1))
            {
                throw new NotSupportedException($"Alias \"{errorContentTypeModelGroup.Key}\" is used by types"
                    + $" {string.Join(", ", errorContentTypeModelGroup.Select(x => x.Kind + ":\"" + x.Alias + "\""))}. Aliases have to be unique."
                    + " One of the aliases must be modified in order to use the ModelsBuilder.");
            }
            return contentTypeModels;
        }

        #endregion
    }
}
