using System;
using System.IO;
using System.Linq;
using Umbraco.ModelsBuilder.Api;
using Umbraco.ModelsBuilder.Building;
using SysConsole = System.Console;

namespace Umbraco.ModelsBuilder.Console
{
    class Program
    {
        static void Usage()
        {
            SysConsole.WriteLine("Usage: Umbraco.ModelsBuilder.Console [-d <directory>] [-ns <namespace>] <api>");
            SysConsole.WriteLine("\t<directory>: models directory");
            SysConsole.WriteLine("\t<namespace>: models namespace");
            SysConsole.WriteLine("\t<api>:       API uri, including user and password (%-encoded)");
            SysConsole.WriteLine("Example: Umbraco.ModelsBuilder.Console -d c:/models -ns My.Models http://john%40doe.com:1234@example.com");
        }

        static void Main(string[] args)
        {
            SysConsole.WriteLine("Umbraco.ModelsBuilder v{0}", ApiVersion.Current.Version);

            string apiUrl, apiUser, apiPassword;
            string modelsDirectory, modelsNamespace;

            apiUrl = apiUser = apiPassword = null;
            modelsDirectory = modelsNamespace = null;

            var i = 0;
            var visitedUri = false;
            while (args.Length > i)
            {
                if (args[i] == "-d")
                {
                    if (args.Length == i + 1)
                    {
                        Usage();
                        return;
                    }
                    var dir = args[i + 1];
                    modelsDirectory = Path.IsPathRooted(dir)
                        ? dir
                        : Path.Combine(Directory.GetCurrentDirectory(), dir);
                    i += 2;
                }
                else if (args[i] == "-ns")
                {
                    if (args.Length == i + 1)
                    {
                        Usage();
                        return;
                    }
                    modelsNamespace = args[i + 1];
                    i += 2;
                }
                else if (!visitedUri)
                {
                    var uriString = args[i];
                    Uri uri;
                    if (!Uri.TryCreate(uriString, UriKind.Absolute, out uri))
                    {
                        SysConsole.WriteLine("Invalid API uri.");
                        Usage();
                        return;
                    }
                    var pos = uri.UserInfo.IndexOf(':');
                    if (pos <= 0 || pos == uri.UserInfo.Length - 1)
                    {
                        SysConsole.WriteLine("Invalid API uri.");
                        Usage();
                        return;
                    }
                    apiUser = uri.UserInfo.Substring(0, pos).Replace('+', ' ');
                    apiUser = Uri.UnescapeDataString(apiUser);
                    apiPassword = uri.UserInfo.Substring(pos + 1).Replace('+', ' ');
                    apiPassword = Uri.UnescapeDataString(apiPassword);
                    apiUrl = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
                    visitedUri = true;
                    i += 1;
                }
                else
                {
                    Usage();
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiUser) || string.IsNullOrWhiteSpace(apiPassword))
            {
                Usage();
                return;
            }

            if (string.IsNullOrWhiteSpace(modelsDirectory))
                modelsDirectory = Directory.GetCurrentDirectory();

            if (!Directory.Exists(modelsDirectory))
            {
                SysConsole.WriteLine("Invalid directory.");
                Usage();
                return;
            }

            try
            {
                SysConsole.WriteLine("Generating...");
                GenerateModels(apiUrl, apiUser, apiPassword, modelsDirectory, modelsNamespace);
                SysConsole.WriteLine("Done.");
            }
            catch (Exception e)
            {
                var message = string.Format("Exception: {0}: {1}",
                    e.GetType().Name, e.Message);
                SysConsole.WriteLine(message);
                SysConsole.WriteLine(e.StackTrace);

                var inner = e.InnerException;
                while (inner != null)
                {
                    message = string.Format("Inner: {0}: {1}", inner.GetType().Name, inner.Message);
                    SysConsole.WriteLine(message);
                    inner = inner.InnerException;
                }

                var aggr = e as AggregateException;
                if (aggr != null)
                    foreach (var aggrInner in aggr.Flatten().InnerExceptions)
                    {
                        message = string.Format("AggregateInner: {0}: {1}", aggrInner.GetType().Name, aggrInner.Message);
                        SysConsole.WriteLine(message);
                    }
            }
        }

        private static void GenerateModels(string apiUrl, string apiUser, string apiPassword, string modelsDirectory, string modelsNamespace)
        {
            var api = new ApiClient(apiUrl, apiUser, apiPassword);
            api.ValidateClientVersion(); // so we get a meaningful error message first

            // exclude .generated.cs files but don't delete them now, should anything go wrong
            var ourFiles = Directory.GetFiles(modelsDirectory, "*.cs")
                .Where(x => !x.EndsWith(".generated.cs"))
                .ToDictionary(x => x, File.ReadAllText);
            var genFiles = api.GetModels(ourFiles, modelsNamespace);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            foreach (var file in genFiles)
            {
                var filename = Path.Combine(modelsDirectory, file.Key + ".generated.cs");
                File.WriteAllText(filename, file.Value);
            }
        }
    }
}
