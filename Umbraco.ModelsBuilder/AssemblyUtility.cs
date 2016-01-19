using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using Umbraco.Core;

namespace Umbraco.ModelsBuilder
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

    internal static class AssemblyUtility
    {
        static AssemblyUtility()
        {
            // caching in a static var - not going to change
            AllReferencedAssemblyLocations = GetAllReferencedAssemblyLocations();
        }

        public static IEnumerable<string> AllReferencedAssemblyLocations { get; private set; }

        private static IEnumerable<string> GetAllReferencedAssemblyLocations()
        {
            if (HostingEnvironment.IsHosted)
            {
                var assemblies = new HashSet<Assembly>(
                    BuildManager.GetReferencedAssemblies()
                        .Cast<Assembly>()
                        .Where(a => a.IsDynamic == false && a.Location.IsNullOrWhiteSpace() == false));
                return assemblies.Select(x => x.Location).Distinct();
            }

            //force load in all reference types
            return ForceLoadingAllReferencedAssemblies();
        }

        private static IEnumerable<string> ForceLoadingAllReferencedAssemblies()
        {
            //TODO: This method has bugs since I've been stuck in an infinite loop with it, though this shouldn't
            // execute while in the web application anyways.

            var assemblies = new List<Assembly>();
            var tmp1 = new List<Assembly>();
            var failed = new List<AssemblyName>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.IsDynamic == false)
                .Where(x => !string.IsNullOrWhiteSpace(x.Location))) // though... IsDynamic should be enough?
            {
                assemblies.Add(assembly);
                tmp1.Add(assembly);
            }

            // fixme - AssemblyUtility questions
            // - should we also load everything that's in the same directory?
            // - do we want to load in the current app domain?
            // - if this runs within Umbraco then we have already loaded them all?

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
            return assemblies.Select(x => x.Location).Distinct();
        }
    }
}
