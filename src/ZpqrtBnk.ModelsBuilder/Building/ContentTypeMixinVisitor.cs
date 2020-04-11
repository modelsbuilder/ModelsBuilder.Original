using System;

namespace Our.ModelsBuilder.Building
{
    public class ContentTypeMixinVisitor
    {
        public enum MixinKind
        {
            Parent,
            Inherited,
            Direct,
            Transitive
        };

        public void Visit(ContentTypeModel contentTypeModel, Action<ContentTypeModel, MixinKind> action)
        {
            if (contentTypeModel.BaseContentType != null)
                Visit(contentTypeModel.BaseContentType, MixinKind.Parent, MixinKind.Inherited, action);

            foreach (var mixinContentTypeModel in contentTypeModel.MixinContentTypes)
                Visit(mixinContentTypeModel, MixinKind.Direct, MixinKind.Transitive, action);
        }

        private static void Visit(ContentTypeModel contentTypeModel, MixinKind kind, MixinKind nextKind, Action<ContentTypeModel, MixinKind> action)
        {
            action(contentTypeModel, kind);

            if (contentTypeModel.BaseContentType != null)
            {
                action(contentTypeModel.BaseContentType, nextKind);
                Visit(contentTypeModel.BaseContentType, nextKind, nextKind, action);
            }

            foreach (var mixinContentTypeModel in contentTypeModel.MixinContentTypes)
                Visit(mixinContentTypeModel, nextKind, nextKind, action);
        }
    }
}