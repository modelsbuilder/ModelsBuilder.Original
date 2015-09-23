using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zbu.ModelsBuilder
{
    // issue [#67]
    // GetAllReferencedAssemblyLocations throws on dynamic assemblies that would
    // be loaded in the current AppDomain, because these assemblies do not have
    // a location.
    // We can either fix it by either ignoring dynamic assemblies, or finding a
    // way to create a MetadataReference to an assembly that exists only in
    // memory.
    // Cannot find a way to create such a MetadataReference as it can only create
    // from the assembly's bytes, which we don't have. However, if the assembly
    // exists only in memory, it cannot really be referenced in a compilation,
    // so it should be fine to exclude it.
    // Fixing by adding .Where(x => x.IsDynamic == false)

    public static class AssemblyUtility
    {
        // fixme - this is slow and should probably be cached in a static var!
        // fixme - if this runs within Umbraco then we have already loaded them all?!

        public static IEnumerable<string> GetAllReferencedAssemblyLocations()
        {
            var assemblies = new List<Assembly>();
            var tmp1 = new List<Assembly>();
            var failed = new List<AssemblyName>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.IsDynamic == false))
            {
                assemblies.Add(assembly);
                tmp1.Add(assembly);
            }
            // fixme - should we also load everything that's in the same directory?
            // fixme - do we want to load in the current app domain?
            while (tmp1.Count > 0)
            {
                var tmp2 = tmp1
                    .SelectMany(x => x.GetReferencedAssemblies())
                    .Distinct()
                    .Where(x => assemblies.All(xx => x.FullName != xx.FullName)) // we don't have it already
                    .Where(x => failed.All(xx => x.FullName != xx.FullName)) // it hasn't failed already
                    .ToArray();
                tmp1.Clear();
                foreach (var assemblyName in tmp2)
                {
                    try
                    {
                        var assembly = AppDomain.CurrentDomain.Load(assemblyName);
                        assemblies.Add(assembly);
                        tmp1.Add(assembly);
                    }
                    catch
                    {
                        failed.Add(assemblyName);
                    }
                }
            }
            return assemblies.Select(x => x.Location);
        }
    }
}
