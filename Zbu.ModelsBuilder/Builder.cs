using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Zbu.ModelsBuilder
{
    public abstract class Builder
    {
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private readonly IList<TypeModel> _typeModels;

        public string Namespace { get; set; }
        public IList<string> Using { get { return TypesUsing; } }

        protected readonly IList<string> TypesUsing = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "System.Web",
            "Umbraco.Core.Models",
            "Umbraco.Core.Models.PublishedContent",
            "Umbraco.Web",
            "Zbu.ModelsBuilder",
            "Zbu.ModelsBuilder.Umbraco",
        };

        protected Builder(IList<TypeModel> typeModels)
        {
            _typeModels = typeModels;
        }

        public void Prepare(DiscoveryResult disco)
        {
            // mark IsContentIgnored models that we discovered should be ignored
            // then propagate / ignore children of ignored contents
            // ignore content = don't generate a class for it, don't generate children
            foreach (var typeModel in _typeModels.Where(x => disco.IsContentIgnored(x.Alias)))
                typeModel.IsContentIgnored = true;
            foreach (var typeModel in _typeModels.Where(x => x.EnumerateBaseTypes().Any(xx => x.IsContentIgnored)))
                typeModel.IsContentIgnored = true;

            // handle model renames
            foreach (var typeModel in _typeModels.Where(x => disco.IsContentRenamed(x.Alias)))
                typeModel.Name = disco.ContentName(typeModel.Alias);

            // mark OmitBase models that we discovered already have a base class
            foreach (var typeModel in _typeModels.Where(x => disco.HasContentBase(disco.ContentName(x.Alias) ?? x.Name)))
                typeModel.OmitBase = true;

            foreach (var typeModel in _typeModels)
            {
                // mark IsRemoved properties that we discovered should be ignored
                // ie is marked as ignored on type, or on any parent type
                var tm = typeModel;
                foreach (var property in typeModel.Properties
                    .Where(property => tm.EnumerateBaseTypes(true).Any(x => disco.IsPropertyIgnored(disco.ContentName(x.Alias) ?? x.Name, property.Alias))))
                {
                    property.IsRemoved = true;
                }

                // handle property renames
                foreach (var property in typeModel.Properties)
                    property.Name = disco.PropertyName(disco.ContentName(typeModel.Alias) ?? typeModel.Name, property.Alias) ?? property.Name;
            }

            // ensure we have no duplicates type names
            foreach (var xx in _typeModels.Where(x => !x.IsContentIgnored).GroupBy(x => x.Name).Where(x => x.Count() > 1))
                throw new InvalidOperationException(string.Format("Type name \"{0}\" is used for types with alias {1}. Should be used for one type only.", 
                    xx.Key, 
                    string.Join(", ", xx.Select(x => "\"" + x.Alias + "\""))));

            // ensure we have no duplicates property names
            foreach (var typeModel in _typeModels.Where(x => !x.IsContentIgnored))
                foreach (var xx in typeModel.Properties.Where(x => !x.IsRemoved).GroupBy(x => x.Name).Where(x => x.Count() > 1))
                    throw new InvalidOperationException(string.Format("Property name \"{0}\" in type with alias \"{1}\" is used for properties with alias {2}. Should be used for one property only.",
                        xx.Key, typeModel.Alias,
                        string.Join(", ", xx.Select(x => "\"" + x.Alias + "\""))));

            // ensure we have no collision between base types
            foreach (var xx in _typeModels.Where(x => !x.IsContentIgnored).Where(x => x.BaseType != null && x.OmitBase))
                throw new InvalidOperationException(string.Format("Type alias \"{0}\" has a more than one parent class.",
                    xx.Alias));

            // discover interfaces that need to be declared / implemented
            foreach (var typeModel in _typeModels)
            {
                // collect all the (non-removed) types implemented at parent level
                // ie the parent content types and the mixins content types, recursively
                var parentImplems = new List<TypeModel>();
                if (typeModel.BaseType != null && !typeModel.BaseType.IsContentIgnored)
                    TypeModel.CollectImplems(parentImplems, typeModel.BaseType);

                // interfaces we must declare we implement (initially empty)
                // ie this type's mixins, except those that have been removed,
                // and except those that are already declared at the parent level
                // in other words, DeclaringInterfaces is "local mixins"
                var declaring = typeModel.MixinTypes
                    .Where(x => !x.IsContentIgnored)
                    .Except(parentImplems);
                typeModel.DeclaringInterfaces.AddRange(declaring);

                // interfaces we must actually implement (initially empty)
                // if we declare we implement a mixin interface, we must actually implement
                // its properties, all recursively (ie if the mixin interface implements...)
                // so, starting with local mixins, we collect all the (non-removed) types above them
                var mixinImplems = new List<TypeModel>();
                foreach (var i in typeModel.DeclaringInterfaces)
                    TypeModel.CollectImplems(mixinImplems, i);
                // and then we remove from that list anything that is already declared at the parent level
                typeModel.ImplementingInterfaces.AddRange(mixinImplems.Except(parentImplems));
            }
        }

        public IEnumerable<TypeModel> GetModelsToGenerate()
        {
            return _typeModels.Where(x => !x.IsContentIgnored);
        }
    }
}
