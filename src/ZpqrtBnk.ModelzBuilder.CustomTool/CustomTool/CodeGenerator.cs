namespace ChristianHelle.DeveloperTools.CodeGenerators.Resw.VSPackage.CustomTool
{
    public abstract class CodeGenerator : ICodeGenerator
    {
        protected CodeGenerator(IResourceParser resourceParser, string defaultNamespace)
        {
            ResourceParser = resourceParser;
            Namespace = defaultNamespace;
        }

        public IResourceParser ResourceParser { get; set; }
        public string Namespace { get; set; }

        public abstract string GenerateCode();
    }
}