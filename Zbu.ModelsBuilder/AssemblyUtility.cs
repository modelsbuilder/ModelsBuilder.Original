using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    public static class AssemblyUtility
    {
        // fixme - this is slow and should probably be cached in a static var!
        // fixme - if this runs within Umbraco then we have already loaded them all?!

        public static IEnumerable<string> GetAllReferencedAssemblyLocations()
        {
            var assemblies = new List<Assembly>();
            var tmp1 = new List<Assembly>();
            var failed = new List<AssemblyName>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
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
