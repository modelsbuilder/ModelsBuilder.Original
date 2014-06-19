using System.CodeDom;
using System.Collections.Generic;

namespace Zbu.ModelsBuilder.Build
{
    // NOTE
    // See nodes in Builder.cs class - that one does not work, is not complete,
    // and was just some sort of experiment...

    /// <summary>
    /// Implements a builder that works by using CodeDom
    /// </summary>
    public class CodeDomBuilder : Builder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeDomBuilder"/> class with a list of models to generate.
        /// </summary>
        /// <param name="typeModels">The list of models to generate.</param>
        public CodeDomBuilder(IList<TypeModel> typeModels)
            : base(typeModels)
        { }

        /// <summary>
        /// Outputs a generated model to a code namespace.
        /// </summary>
        /// <param name="ns">The code namespace.</param>
        /// <param name="typeModel">The model to generate.</param>
        public void Generate(CodeNamespace ns, TypeModel typeModel)
        {
            // what about USING?
            // what about references?

            if (typeModel.IsMixin)
            {
                var i = new CodeTypeDeclaration("I" + typeModel.ClrName)
                {
                    IsInterface = true,
                    IsPartial = true,
                    Attributes = MemberAttributes.Public
                };
                i.BaseTypes.Add(typeModel.BaseType == null ? "IPublishedContent" : "I" + typeModel.BaseType.ClrName);

                foreach (var mixinType in typeModel.DeclaringInterfaces)
                    i.BaseTypes.Add(mixinType.ClrName);

                i.Comments.Add(new CodeCommentStatement(
                    string.Format("Mixin content Type {0} with alias \"{1}\"", typeModel.Id, typeModel.Alias)));

                foreach (var propertyModel in typeModel.Properties)
                {
                    var p = new CodeMemberProperty();
                    p.Name = propertyModel.ClrName;
                    p.Type = new CodeTypeReference(propertyModel.ClrType);
                    p.Attributes = MemberAttributes.Public;
                    p.HasGet = true;
                    p.HasSet = false;
                    i.Members.Add(p);
                }
            }

            var c = new CodeTypeDeclaration(typeModel.ClrName)
            {
                IsClass = true,
                IsPartial = true,
                Attributes = MemberAttributes.Public
            };

            c.BaseTypes.Add(typeModel.BaseType == null ? "PublishedContentModel" : typeModel.BaseType.ClrName);

            // if it's a missing it implements its own interface
            if (typeModel.IsMixin)
                c.BaseTypes.Add("I" + typeModel.ClrName);

            // write the mixins, if any, as interfaces
            // only if not a mixin because otherwise the interface already has them
            if (typeModel.IsMixin == false)
                foreach (var mixinType in typeModel.DeclaringInterfaces)
                    c.BaseTypes.Add("I" + mixinType.ClrName);

            foreach (var mixin in typeModel.MixinTypes)
                c.BaseTypes.Add("I" + mixin.ClrName);

            c.Comments.Add(new CodeCommentStatement(
                string.Format("Content Type {0} with alias \"{1}\"", typeModel.Id, typeModel.Alias)));

            foreach (var propertyModel in typeModel.Properties)
            {
                var p = new CodeMemberProperty();
                p.Name = propertyModel.ClrName;
                p.Type = new CodeTypeReference(propertyModel.ClrType);
                p.Attributes = MemberAttributes.Public;
                p.HasGet = true;
                p.HasSet = false;
                p.GetStatements.Add(new CodeMethodReturnStatement( // return
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeThisReferenceExpression(), // this
                            "GetPropertyValue", // .GetPropertyValue
                            new[] // <T>
                            {
                                new CodeTypeReference(propertyModel.ClrType)
                            }),
                            new CodeExpression[] // ("alias")
                            {
                                new CodePrimitiveExpression(propertyModel.Alias)
                            })));
                c.Members.Add(p);
            }
        }
    }
}
