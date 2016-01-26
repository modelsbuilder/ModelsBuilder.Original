namespace Umbraco.ModelsBuilder.Umbraco
{
    internal interface IPureLiveModelsEngine
    {
        void NotifyRebuilding();
        void NotifyRebuilt();
    }
}
