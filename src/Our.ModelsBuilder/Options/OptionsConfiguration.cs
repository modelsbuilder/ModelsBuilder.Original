using System;
using System.Collections.Generic;

namespace Our.ModelsBuilder.Options
{
    public class OptionsConfiguration
    {
        private List<Action<ModelsBuilderOptions>> _configureOptions;
        private List<Action<CodeOptionsBuilder>> _configureCodeOptions;
        private ModelsBuilderOptions _modelsBuilderOptions;

        public void AddConfigure(Action<ModelsBuilderOptions> configure)
        {
            (_configureOptions ??= new List<Action<ModelsBuilderOptions>>()).Add(configure);
        }

        public void AddConfigure(Action<CodeOptionsBuilder> configure)
        {
            (_configureCodeOptions ??= new List<Action<CodeOptionsBuilder>>()).Add(configure);
        }

        public ModelsBuilderOptions ModelsBuilderOptions
            => _modelsBuilderOptions ??= Configure(new ModelsBuilderOptions());

        private ModelsBuilderOptions Configure(ModelsBuilderOptions modelsBuilderOptions)
        {
            if (_configureOptions == null) 
                return modelsBuilderOptions;

            foreach (var configure in _configureOptions)
                configure(modelsBuilderOptions);
            return modelsBuilderOptions;
        }

        public T Configure<T>(T codeOptionsBuilder)
            where T : CodeOptionsBuilder
        {
            if (_configureCodeOptions == null)
                return codeOptionsBuilder;

            foreach (var configure in _configureCodeOptions)
                configure(codeOptionsBuilder);
            return codeOptionsBuilder;
        }
    }
}
