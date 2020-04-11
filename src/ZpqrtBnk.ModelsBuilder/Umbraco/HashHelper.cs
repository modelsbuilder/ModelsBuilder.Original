using System.Collections.Generic;
using System.Linq;
using Our.ModelsBuilder.Building;

namespace Our.ModelsBuilder.Umbraco
{
    class HashHelper
    {
        public static string Hash(IDictionary<string, string> ourFiles, IEnumerable<ContentTypeModel> typeModels)
        {
            var hash = new HashCombiner();

            foreach (var kvp in ourFiles)
                hash.Add(kvp.Key + "::" + kvp.Value);

            // see Our.ModelsBuilder.Umbraco.Application for what's important to hash
            // ie what comes from Umbraco (not computed by ModelsBuilder) and makes a difference

            foreach (var typeModel in typeModels.OrderBy(x => x.Alias))
            {
                hash.Add("--- CONTENT TYPE MODEL ---");
                hash.Add(typeModel.Id);
                hash.Add(typeModel.Alias);
                hash.Add(typeModel.ClrName);
                hash.Add(typeModel.ParentId);
                hash.Add(typeModel.Name);
                hash.Add(typeModel.Description);
                hash.Add(typeModel.Kind.ToString());
                hash.Add("MIXINS:" + string.Join(",", typeModel.MixinContentTypes.OrderBy(x => x.Id).Select(x => x.Id)));

                foreach (var prop in typeModel.Properties.OrderBy(x => x.Alias))
                {
                    hash.Add("--- PROPERTY ---");
                    hash.Add(prop.Alias);
                    hash.Add(prop.ClrName);
                    hash.Add(prop.Name);
                    hash.Add(prop.Description);
                    hash.Add(prop.ValueType.ToString()); // see ModelType tests, want ToString() not FullName
                }
            }

            return hash.GetCombinedHashCode();
        }
    }
}
