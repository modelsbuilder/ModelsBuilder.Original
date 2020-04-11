using System;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Composing;

namespace Our.ModelsBuilder.Umbraco
{
    public static class PublishedModelUtility
    {
        // looks safer but probably useless... ppl should not call these methods directly
        // and if they do... they have to take care about not doing stupid things

        //public static PublishedPropertyType GetModelPropertyType2<T>(Expression<Func<T, object>> selector)
        //    where T : PublishedContentModel
        //{
        //    var type = typeof (T);
        //    var s1 = type.GetField("ModelTypeAlias", BindingFlags.Public | BindingFlags.Static);
        //    var alias = (s1.IsLiteral && s1.IsInitOnly && s1.FieldType == typeof(string)) ? (string)s1.GetValue(null) : null;
        //    var s2 = type.GetField("ModelItemType", BindingFlags.Public | BindingFlags.Static);
        //    var itemType = (s2.IsLiteral && s2.IsInitOnly && s2.FieldType == typeof(PublishedItemType)) ? (PublishedItemType)s2.GetValue(null) : 0;

        //    var contentType = PublishedContentType.Get(itemType, alias);
        //    // etc...
        //}

        public static IPublishedContentType GetModelContentType(PublishedItemType itemType, string alias)
        {
            var facade = Current.UmbracoContext.PublishedSnapshot; // fixme inject!
            switch (itemType)
            {
                case PublishedItemType.Content:
                    return facade.Content.GetContentType(alias);
                case PublishedItemType.Media:
                    return facade.Media.GetContentType(alias);
                case PublishedItemType.Member:
                    return facade.Members.GetContentType(alias);
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType));
            }
        }
    }
}
