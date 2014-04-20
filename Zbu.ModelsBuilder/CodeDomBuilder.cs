using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    // FIXME - do NOT use that one as it is OBSOLETE 

    public class CodeDomBuilder : Builder
    {
        public void Generate(CodeNamespace ns, TypeModel typeModel)
        {
            // what about USING?
            // what about references?

            if (typeModel.IsMixin)
            {
                var i = new CodeTypeDeclaration("I" + typeModel.Name)
                {
                    IsInterface = true,
                    IsPartial = true,
                    Attributes = MemberAttributes.Public
                };
                i.BaseTypes.Add(typeModel.BaseType == null ? "IPublishedContent" : "I" + typeModel.BaseType.Name);

                foreach (var mixinType in typeModel.DeclaringInterfaces)
                    i.BaseTypes.Add(mixinType.Name);

                i.Comments.Add(new CodeCommentStatement(
                    string.Format("Mixin content Type {0} with alias \"{1}\"", typeModel.Id, typeModel.Alias)));

                foreach (var propertyModel in typeModel.Properties)
                {
                    var p = new CodeMemberProperty();
                    p.Name = propertyModel.Name;
                    p.Type = new CodeTypeReference(propertyModel.ClrType);
                    p.Attributes = MemberAttributes.Public;
                    p.HasGet = true;
                    p.HasSet = false;
                    i.Members.Add(p);
                }
            }

            var c = new CodeTypeDeclaration(typeModel.Name)
            {
                IsClass = true,
                IsPartial = true,
                Attributes = MemberAttributes.Public
            };

            c.BaseTypes.Add(typeModel.BaseType == null ? "PublishedContentModel" : typeModel.BaseType.Name);

            // if it's a missing it implements its own interface
            if (typeModel.IsMixin)
                c.BaseTypes.Add("I" + typeModel.Name);

            // write the mixins, if any, as interfaces
            // only if not a mixin because otherwise the interface already has them
            if (typeModel.IsMixin == false)
                foreach (var mixinType in typeModel.DeclaringInterfaces)
                    c.BaseTypes.Add("I" + mixinType.Name);

            foreach (var mixin in typeModel.MixinTypes)
                c.BaseTypes.Add("I" + mixin.Name);

            c.Comments.Add(new CodeCommentStatement(
                string.Format("Content Type {0} with alias \"{1}\"", typeModel.Id, typeModel.Alias)));

            foreach (var propertyModel in typeModel.Properties)
            {
                var p = new CodeMemberProperty();
                p.Name = propertyModel.Name;
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
