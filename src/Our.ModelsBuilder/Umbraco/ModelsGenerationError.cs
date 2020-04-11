using System;
using System.IO;
using System.Text;
using Our.ModelsBuilder.Options;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.ModelsBuilder.Umbraco
{
    public static class ModelsGenerationError
    {
        private static ModelsBuilderOptions Options => Current.Factory.GetInstance<ModelsBuilderOptions>();

        public static void Clear()
        {
            var errFile = GetErrFile();
            if (errFile == null) return;

            // "If the file to be deleted does not exist, no exception is thrown."
            File.Delete(errFile);
        }

        public static void Report(string message, Exception e)
        {
            var errFile = GetErrFile();
            if (errFile == null) return;

            var sb = new StringBuilder();
            sb.Append(message);
            sb.Append("\r\n");
            sb.Append(e.Message);
            sb.Append("\r\n\r\n");
            sb.Append(e.StackTrace);
            sb.Append("\r\n");

            File.WriteAllText(errFile, sb.ToString());
        }

        public static string GetLastError()
        {
            var errFile = GetErrFile();
            if (errFile == null) return null;

            try
            {
                return File.ReadAllText(errFile);
            }
            catch // accepted
            {
                return null;
            }
        }

        private static string GetErrFile()
        {
            var modelsDirectory = Options.ModelsDirectory;

            return Directory.Exists(modelsDirectory) 
                ? Path.Combine(modelsDirectory, "models.err") 
                : null;
        }
    }
}
