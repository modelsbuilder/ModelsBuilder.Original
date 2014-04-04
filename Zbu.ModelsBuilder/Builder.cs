using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Roslyn.Compilers.CSharp;

namespace Zbu.ModelsBuilder
{
    public abstract class Builder
    {
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public string Namespace { get; set; }
        public IList<string> Using { get { return _typesUsing; } }

        protected readonly IList<string> _typesUsing = new List<string>
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

        #region Prepare

        public void Prepare(IList<TypeModel> typeModels)
        {
            // ensure we have no duplicates
            var names = typeModels.Select(x => x.Name).Distinct();
            if (names.Count() != typeModels.Count)
                throw new InvalidOperationException("Duplicate type names have been found.");

            // ensure
            if (typeModels.Any(x => x.BaseType != null && x.ModelBaseClassName != null))
                throw new InvalidOperationException("Types with a base type and a base class name have been found.");

            // discover interfaces that need to be declared / implemented
            foreach (var typeModel in typeModels)
            {
                var parentTree = typeModel.BaseType == null
                    ? new List<TypeModel>()
                    : typeModel.BaseType.GetTypeTree();

                typeModel.DeclaringInterfaces.AddRange(typeModel.MixinTypes.Except(parentTree));

                var recursiveInterfaces = new List<TypeModel>();
                foreach (var i in typeModel.DeclaringInterfaces)
                    TypeModel.GetTypeTree(recursiveInterfaces, i);
                typeModel.ImplementingInterfaces.AddRange(recursiveInterfaces.Except(parentTree));
            }
        }

        #endregion

        #region Parse

        public void Parse(string code, IList<TypeModel> genTypes)
        {
            var tree = SyntaxTree.ParseText(code);
            var writer = new CodeWalker();
            writer.Visit(tree.GetRoot(),
                onIgnoreContentType: alias => genTypes.RemoveAll(x => x.Alias.InvariantEquals(alias)),
                onIgnorePropertyType: (contentName, propertyAlias) =>
                {
                    if (string.IsNullOrWhiteSpace(propertyAlias)) return;
                    var type = genTypes.SingleOrDefault(x => x.Name == contentName);
                    if (type == null) return;

                    var star = propertyAlias.EndsWith("*");
                    if (star) propertyAlias = propertyAlias.Substring(0, propertyAlias.Length - 1);
                    type.Properties.RemoveAll(x => 
                        star ? x.Alias.StartsWith(propertyAlias) : x.Alias == propertyAlias);
                },
                onRenamePropertyType: (contentName, propertyAlias, propertyName) =>
                {
                    if (string.IsNullOrWhiteSpace(contentName)) return;
                    if (string.IsNullOrWhiteSpace(propertyAlias)) return;
                    if (string.IsNullOrWhiteSpace(propertyName)) return;

                    var type = genTypes.SingleOrDefault(x => x.Name == contentName);
                    if (type == null) return;

                    var property = type.Properties.SingleOrDefault(x => x.Alias == propertyAlias);
                    if (property == null) return;

                    property.Name = propertyName;
                },
                onRenameContentType: (contentName, contentAlias) =>
                {
                    if (string.IsNullOrWhiteSpace(contentName)) return;
                    var type = genTypes.SingleOrDefault(x => x.Alias.InvariantEquals(contentAlias));
                    if (type != null)
                        type.Name = contentName;
                },
                onDefineModelBaseClass: (contentName, modelBaseClassName) =>
                {
                    if (string.IsNullOrWhiteSpace(modelBaseClassName)) return;
                    var type = genTypes.SingleOrDefault(x => x.Name == contentName);
                    if (type == null) return;
                    type.ModelBaseClassName = modelBaseClassName;
                });

            // at that point some types might have been removed / ignored, that we
            // actually need - but we can't tell, because they may be implemented by the user
        }

        #endregion
    }
}
