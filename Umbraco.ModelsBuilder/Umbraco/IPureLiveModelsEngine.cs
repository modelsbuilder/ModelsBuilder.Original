namespace Umbraco.ModelsBuilder.Umbraco
{
    public interface IPureLiveModelsEngine
    {
        void NotifyRebuilding();
        void NotifyRebuilt();
    }
}
