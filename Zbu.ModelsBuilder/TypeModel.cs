using System;
using System.Collections.Generic;
using System.Linq;

namespace Zbu.ModelsBuilder
{
    public class TypeModel
    {
        public int Id;
        public string Alias;
        public string Name;
        public int BaseTypeId;
        public TypeModel BaseType; // the parent type in Umbraco (type inherits its properties)
        public readonly List<PropertyModel> Properties = new List<PropertyModel>(); // the local properties (not inherited)

        public readonly List<TypeModel> MixinTypes = new List<TypeModel>(); // the mixin types in Umbraco (type inherits their properties)
        public readonly List<TypeModel> DeclaringInterfaces = new List<TypeModel>(); // must declare it implements those mixins
        public readonly List<TypeModel> ImplementingInterfaces = new List<TypeModel>(); // must implement properties for those mixins

        public bool HasBase;
        public bool IsRenamed;
        public bool IsMixin; // whether the type is a mixin for another type
        public bool IsParent; // whether the type is a parent for another type
        public bool IsContentIgnored; // whether the type should be removed from generation

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

        internal static void CollectImplems(ICollection<TypeModel> types, TypeModel type)
        {
            if (!type.IsContentIgnored && types.Contains(type) == false)
                types.Add(type);
            if (type.BaseType != null && !type.BaseType.IsContentIgnored)
                CollectImplems(types, type.BaseType);
            foreach (var mixin in type.MixinTypes.Where(x => !x.IsContentIgnored))
                CollectImplems(types, mixin);
        }

        public IEnumerable<TypeModel> EnumerateBaseTypes(bool andSelf = false)
        {
            var typeModel = andSelf ? this : BaseType;
            while (typeModel != null)
            {
                yield return typeModel;
                typeModel = typeModel.BaseType;
            }
        }
    }
}
