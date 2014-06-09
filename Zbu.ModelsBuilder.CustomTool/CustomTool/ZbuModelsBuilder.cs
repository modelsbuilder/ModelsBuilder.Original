using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Zbu.ModelsBuilder.CustomTool.VisualStudio;

namespace Zbu.ModelsBuilder.CustomTool.CustomTool
{
    [ComVisible(true)]
    public abstract class ZbuModelsBuilder : IVsSingleFileGenerator
    {
        private readonly CodeDomProvider _codeDomProvider;
        private readonly TypeAttributes? _classAccessibility;

        protected ZbuModelsBuilder(CodeDomProvider codeDomProvider, TypeAttributes? classAccessibility = null)
        {
            this._codeDomProvider = codeDomProvider;
            this._classAccessibility = classAccessibility;
        }

        #region IVsSingleFileGenerator Members

        public abstract int DefaultExtension(out string pbstrDefaultExtension);

        public virtual int Generate(string wszInputFilePath,
                                    string bstrInputFileContents,
                                    string wszDefaultNamespace,
                                    IntPtr[] rgbOutputFileContents,
                                    out uint pcbOutput,
                                    IVsGeneratorProgress pGenerateProgress)
        {
            try
            {
                // though that only happens if you explicitely set it to whitespaces
                // otherwise VisualStudio will use the default one... so it will work
                // if the namespace is left empty in VS.
                if (string.IsNullOrWhiteSpace(wszDefaultNamespace))
                    throw new Exception("No namespace.");

                VisualStudioHelper.ReportMessage("Starting {0}.", DateTime.Now);

                var path = Path.GetDirectoryName(wszInputFilePath) ?? "";
                
                var options = VisualStudioHelper.GetOptions();
                options.Validate();

                // that whole block tries to run the standalone application
                // but it's a dead-end: what if we want the global.asax to register
                // some property converters and we want to have them detected?!

                /*
                // get umbraco root directory in ApplicationData
                // because the RemoteApplication runs in ApplicationData
                // this creates the directory if required
                var umbracoRoot = Umbraco.Application.GetLocalApplicationDataRootDirectory();

                // recreate the bin directory
                // fixme - once we've crashed we cannot delete the dll files
                // fixme - thought that using a domain would precisely prevent this?
                // fixme - or should we just run from where we are but shadow-copy dlls?
                var umbracoBin = Path.Combine(umbracoRoot, "bin");
                if (Directory.Exists(umbracoBin))
                    Directory.Delete(umbracoBin, true);
                Directory.CreateDirectory(umbracoBin);

                // copy the local dlls
                var projectBin = VisualStudioHelper.GetProjectBin(options.BinaryDirectory);
                VisualStudioHelper.ReportMessage("Copy dlls from {0} into {1}.", projectBin, umbracoBin);
                foreach (var file in Directory.GetFiles(projectBin, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var filename = Path.GetFileName(file);
                    VisualStudioHelper.ReportMessage("Copy {0}.", filename);
                    File.Copy(file, Path.Combine(umbracoBin, filename));
                }

                // copy our dlls
                // fixme - overriding, is this a good idea?
                // CodeBase looks like file:\C:\Users\JohnDoe\AppData\Local\Microsoft\VisualStudio\11.0\Extensions\4mtv4yyr.bgy\Zbu.ModelsBuilder.dll
                var localBin = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
                VisualStudioHelper.ReportMessage("Copy Zbu.ModelsBuilder dlls from {0} into {1}.", localBin, umbracoBin);
                foreach (var filename in new[] {"Zbu.ModelsBuilder.dll", "Zbu.ModelsBuilder.Umbraco.dll"})
                {
                    var file = Path.Combine(localBin, filename);
                    VisualStudioHelper.ReportMessage("Copy {0}.", filename);
                    File.Copy(file, Path.Combine(umbracoBin, filename), true);
                }

                // create a new app domain for Umbraco
                var domainSetup = new AppDomainSetup
                {
                    ApplicationName = "Zbu.ModelsBuilder.CustomTool",
                    ApplicationBase = umbracoRoot,
                    PrivateBinPath = umbracoBin,
                    // read http://msdn.microsoft.com/it-it/library/43wc4hhs.aspx
                    // read http://connect.microsoft.com/VisualStudio/feedback/details/536783/vsip-assembly-file-handles-not-being-released-after-appdomain-unload
                    // we want to be able to delete the dlls once the domain has been unloaded
                    LoaderOptimization = LoaderOptimization.MultiDomainHost
                };
                VisualStudioHelper.ReportMessage("Create AppDomain and connect to Umbraco.");
                AppDomain domain = null;
                IList<TypeModel> modelTypes;
                try
                {
                    domain = AppDomain.CreateDomain("Zbu.ModelsBuilder.CustomTool", null, domainSetup);

                    // instanciate the RemoteApplication and retrieve the model types
                    // because it runs in the domain and with all files copied, Umbraco's plugin
                    // manager should detect and load all our files, property converters, etc
                    var assemblyFile = Path.Combine(umbracoBin, "Zbu.ModelsBuilder.Umbraco.dll");
                    var remote = domain.CreateInstanceFromAndUnwrap(assemblyFile, "Zbu.ModelsBuilder.Umbraco.RemoteApplication") as Umbraco.RemoteApplication;
                    modelTypes = remote.GetContentAndMediaTypes(options.ConnectionString, options.DatabaseProvider);
                }
                finally
                {
                    if (domain != null)
                        try
                        {
                            // kill the domain
                            AppDomain.Unload(domain);
                        }
// ReSharper disable once EmptyGeneralCatchClause
                        catch {} // I know what you think
                }
                */

                // so let's start from scratch by hitting Umbraco via a WebApi
                // FIXME - cleanup all this if it works
                // but a TypeModel reference the CLR type of the property
                // so the API wants to deserialize that type => must know about it
                // so we still have the issue that those types are not referenced
                // and so... we still need the whole AppDomain stuff unless we get
                // everything generated on Umbraco's side (by passing the parsed
                // stuff out there?)
                //var modelTypes = AspNet.ModelsBuilderApi.GetTypeModels(options.UmbracoUrl, options.UmbracoUser, options.UmbracoPassword);

                // exclude .generated.cs files but don't delete them now, should anything go wrong
                var ourFiles = Directory.GetFiles(path, "*.cs")
                    .Where(x => !x.EndsWith(".generated.cs"))
                    .ToDictionary(x => x, File.ReadAllText);
                //var codeInfos = CodeInfos.ParseFiles(Directory.GetFiles(path, "*.cs").Where(x => !x.EndsWith(".generated.cs")));
                var api = new AspNet.ModelsBuilderApi(options.UmbracoUrl, options.UmbracoUser, options.UmbracoPassword);
                api.ValidateClientVersion(); // so we get a meaningful error message first
                var genFiles = api.GetModels(ourFiles, wszDefaultNamespace);

                /*
                VisualStudioHelper.ReportMessage("Found {0} content types in Umbraco.", modelTypes.Count);
                */

                var vsitem = VisualStudioHelper.GetSourceItem(wszInputFilePath);
                VisualStudioHelper.ClearExistingItems(vsitem);

                foreach (var file in Directory.GetFiles(path, "*.generated.cs"))
                    File.Delete(file);

                /*
                var builder = new TextBuilder();
                builder.Namespace = wszDefaultNamespace;
                builder.Prepare(modelTypes, CodeInfos.ParseFiles(Directory.GetFiles(path, "*.cs")));

                VisualStudioHelper.ReportMessage("Need to generate {0} files.", modelTypes.Count);

                var inputFilename = Path.GetFileNameWithoutExtension(wszInputFilePath);
                if (modelTypes.Any(x => x.Name.InvariantEquals(inputFilename)))
                    throw new Exception("Name collision, there is a model named " + inputFilename);

                foreach (var modelType in modelTypes)
                {
                    var sb = new StringBuilder();
                    builder.Generate(sb, modelType);
                    var filename = Path.Combine(path, modelType.Name + ".generated.cs");
                    File.WriteAllText(filename, sb.ToString());
                    VisualStudioHelper.AddGeneratedItem(vsitem, filename);
                }

                VisualStudioHelper.ReportMessage("Generated {0} files.", modelTypes.Count);
                */

                foreach (var file in genFiles)
                {
                    var filename = Path.Combine(path, file.Key + ".generated.cs");
                    File.WriteAllText(filename, file.Value);
                    VisualStudioHelper.AddGeneratedItem(vsitem, filename);
                }

                // we need to generate something
                var code = new StringBuilder();
                new TextBuilder(new TypeModel[] { }).WriteHeader(code);
                code.Append("// ZpqrtBnk Umbraco ModelsBuilder\n");
                code.AppendFormat("// {0:yyyy-MM-ddTHH:mm:ssZ}", DateTime.UtcNow);

                var data = Encoding.Default.GetBytes(code.ToString());
                var ptr = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);
                pcbOutput = (uint)data.Length;
                rgbOutputFileContents[0] = ptr;

                VisualStudioHelper.ReportMessage("Done.");
            }
            catch (Exception e)
            {
                var message = string.Format("ZbuModelsBuilder failed to generate code: {0}: {1}",
                    e.GetType().Name, e.Message);
                VisualStudioHelper.ReportError(pGenerateProgress, message);
                VisualStudioHelper.ReportMessage(message);
                VisualStudioHelper.ReportMessage(e.StackTrace);

                var inner = e.InnerException;
                while (inner != null)
                {
                    message = string.Format("Inner: {0}: {1}", inner.GetType().Name, inner.Message);
                    VisualStudioHelper.ReportMessage(message);
                    VisualStudioHelper.ReportMessage(inner.StackTrace);
                    inner = inner.InnerException;
                }

                var aggr = e as AggregateException;
                if (aggr != null)
                    foreach (var aggrInner in aggr.Flatten().InnerExceptions)
                    {
                        message = string.Format("AggregateInner: {0}: {1}", aggrInner.GetType().Name, aggrInner.Message);
                        VisualStudioHelper.ReportMessage(message);
                        VisualStudioHelper.ReportMessage(aggrInner.StackTrace);
                    }

                //MessageBox.Show(e.Message, "Unable to generate code");
                throw;
            }

            return 0;
        }

        #endregion
    }
}