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
        public readonly List<PropertyModel> Properties = new List<PropertyModel>();
        public readonly List<TypeModel> MixinTypes = new List<TypeModel>();
        public readonly List<TypeModel> DeclaringInterfaces = new List<TypeModel>();
        public readonly List<TypeModel> ImplementingInterfaces = new List<TypeModel>();
        public bool IsMixin;

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
