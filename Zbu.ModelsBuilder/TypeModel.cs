using System;
using System.Collections.Generic;

namespace Zbu.ModelsBuilder
{
    public class TypeModel
    {
        public int Id;
        public string Alias;
        public string Name;
        public int BaseTypeId;
        public TypeModel BaseType;
        public string ModelBaseClassName;
        public readonly List<PropertyModel> Properties = new List<PropertyModel>();
        public readonly List<TypeModel> MixinTypes = new List<TypeModel>();
        public readonly List<TypeModel> DeclaringInterfaces = new List<TypeModel>();
        public readonly List<TypeModel> ImplementingInterfaces = new List<TypeModel>();
        public bool IsMixin;

        public enum ItemTypes
        {
            Content,
            Media
        }

        private ItemTypes _itemType;

        public ItemTypes ItemType
        {
            get { return _itemType; }
            set
            {
                switch (value)
                {
                    case ItemTypes.Content:
                    case ItemTypes.Media:
                        _itemType = value;
                        break;
                    default:
                        throw new ArgumentException("value");
                }
            }
        }

        public List<TypeModel> GetTypeTree()
        {
            var tree = new List<TypeModel>();
            GetTypeTree(tree, this);
            return tree;
        }

        public static void GetTypeTree(ICollection<TypeModel> types, TypeModel type)
        {
            if (types.Contains(type) == false)
                types.Add(type);
            if (type.BaseType != null)
                GetTypeTree(types, type.BaseType);
            foreach (var mixin in type.MixinTypes)
                GetTypeTree(types, mixin);
        }
    }
}
