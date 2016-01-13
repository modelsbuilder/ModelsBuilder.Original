using System;
using System.Reflection;
using System.Web.Mvc;
using Umbraco.Core.Profiling;

namespace Umbraco.ModelsBuilder.AspNet.ViewEngine
{
    static class ViewEngineCollectionExtensions
    {
        public static void Substitute<TEngine>(this ViewEngineCollection engines, IViewEngine engine)
            where TEngine : IViewEngine
        {
            var index = 0;

            while (index < engines.Count)
            {
                //var profiling = engines[index] as ProfilingViewEngine;
                //if (profiling == null)
                //    throw new Exception("Panic: not a ProfilingViewEngine.");

                //var innerProperty = typeof(ProfilingViewEngine)
                //    .GetField("Inner", BindingFlags.Instance | BindingFlags.NonPublic);
                //if (innerProperty == null)
                //    throw new Exception("Panic: no Inner field.");

                //var profiled = innerProperty.GetValue(profiling);
                //if (profiled == null)
                //    throw new Exception("Panic: Inner is null.");

                //if (profiled is TEngine) break;

                if (engines[index] is TEngine) break;
                index++;
            }

            if (index == engines.Count)
                throw new Exception("Panic: could not find engine to subsitute.");

            //var profilingEngine = new ProfilingViewEngine(engine);
            engines.RemoveAt(index);
            engines.Insert(index, engine);
        }
    }
}
