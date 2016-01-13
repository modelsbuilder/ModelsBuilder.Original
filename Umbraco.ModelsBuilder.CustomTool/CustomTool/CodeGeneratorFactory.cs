using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;

namespace ChristianHelle.DeveloperTools.CodeGenerators.Resw.VSPackage.CustomTool
{
    public class CodeGeneratorFactory
    {
        public ICodeGenerator Create(string className, string defaultNamespace, string inputFileContents, CodeDomProvider codeDomProvider = null, TypeAttributes? classAccessibility = null)
        {
            return new CodeDomCodeGenerator(new ResourceParser(inputFileContents), className, defaultNamespace, codeDomProvider, classAccessibility);
        }
    }
}
