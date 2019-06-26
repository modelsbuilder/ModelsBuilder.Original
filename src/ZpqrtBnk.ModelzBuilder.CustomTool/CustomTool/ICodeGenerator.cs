namespace ChristianHelle.DeveloperTools.CodeGenerators.Resw.VSPackage.CustomTool
{
    public interface ICodeGenerator
    {
        IResourceParser ResourceParser { get; set; }
        string Namespace { get; set; }
        string GenerateCode();
    }
}
