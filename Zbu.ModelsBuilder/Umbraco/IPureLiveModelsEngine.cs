namespace Zbu.ModelsBuilder.Umbraco
{
    public interface IPureLiveModelsEngine
    {
        void NotifyRebuilding();
        void NotifyRebuilt();
    }
}
