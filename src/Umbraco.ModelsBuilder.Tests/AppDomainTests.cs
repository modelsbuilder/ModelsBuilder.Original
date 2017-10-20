using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class AppDomainTests
    {
        [Test]
        [Ignore("no idea what we're testing here?")]
        public void Test()
        {
            // read http://msdn.microsoft.com/en-us/library/ms173139%28v=vs.90%29.aspx

            // test executes in project/bin/Debug or /Release
            Console.WriteLine("FriendlyName " + AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("BaseDirectory " + AppDomain.CurrentDomain.BaseDirectory); // project's bin
            Console.WriteLine("SearchPath " + AppDomain.CurrentDomain.RelativeSearchPath);
            Console.WriteLine("CodeBase " + Assembly.GetExecutingAssembly().CodeBase);
            Console.WriteLine("CurrentDirectory " + Directory.GetCurrentDirectory());

            var domainSetup = new AppDomainSetup();

            var bzzt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bzzt");
            Console.WriteLine("Bzzt " + bzzt);
            if (Directory.Exists(bzzt))
                Directory.Delete(bzzt, true);
            Directory.CreateDirectory(bzzt);
            var load = Path.Combine(bzzt, "Umbraco.ModelsBuilder.Tests.dll"); // we want to load the copy!
            File.Copy("Umbraco.ModelsBuilder.Tests.dll", load);

            // fixme - why do we want copies? why cant we load stuff from where we are?
            // because - we want Umbraco plugin whatever to discover those things properly!
            File.Copy("Umbraco.ModelsBuilder.dll", Path.Combine(bzzt, "Umbraco.ModelsBuilder.dll")); // REQUIRED if we load copies

            // fixme - notes
            // because we set a root dir to the app which is in appdata
            // then IOHelper.GetRootDirectorySafe returns that directory ie AppData/Zbu
            // and IOHelper.GetRootDirectoryBinFolder looks into ~/bin/debug, ~/bin/release, ~/bin then ~/
            // and this is where TypeFinder.GetAllAssemblies will be looking into

            // app domain defaults to resharper's bin - FIXME how can it load our dll?
            // this makes sure it uses the right app base
            // with: directly look for the assembly in there
            // without: look for the assembly in resharper's bin first... then in there, why? 'cos it's the calling assembly!
            domainSetup.ApplicationBase = bzzt;
            // /bin rejected, outside appbase ... but anything else seems to be ignored?
            //domainSetup.PrivateBinPath = "bzzt"; // absolutely no idea what it does
            domainSetup.ApplicationName = "Test Application";

            // fixme - then we MUST copy a bunch of binaries out there!
            var domain = AppDomain.CreateDomain("Test Domain", null, domainSetup);
            // the dll here is relative to the local domain path, not the remote one?!
            var remote = domain.CreateInstanceFromAndUnwrap(load, "Umbraco.ModelsBuilder.Tests.RemoteObject") as RemoteObject;
            var sho = remote.GetSharedObjects();
            Console.WriteLine(remote.GetAppDomainDetails());
            //Console.WriteLine(domain.);
            AppDomain.Unload(domain);

            Assert.Throws<AppDomainUnloadedException>(() => remote.GetAppDomainDetails());

            var asho = sho.ToArray();
            Assert.AreEqual(1, asho.Length);
            Assert.AreEqual("hello", asho[0].Value);
        }
    }

    // read
    // http://blogs.microsoft.co.il/sasha/2010/05/06/assembly-private-bin-path-pitfall/
    // run fuslogvw.exe from an elevated Visual Studio command prompt

    [Serializable]
    public class SharedObject
    {
        public string Value { get; set; }
    }

    public class RemoteObject : MarshalByRefObject
    {
        public IEnumerable<SharedObject> GetSharedObjects()
        {
            // in order for this to work... where should we look into?
            Assembly.Load("Umbraco.ModelsBuilder");

            return new[] { new SharedObject { Value = "hello" } };
        }

        public string GetAppDomainDetails()
        {
            return AppDomain.CurrentDomain.FriendlyName
                + Environment.NewLine + AppDomain.CurrentDomain.BaseDirectory
                + Environment.NewLine + AppDomain.CurrentDomain.RelativeSearchPath
                + Environment.NewLine + Assembly.GetExecutingAssembly().CodeBase;
        }
    }
}
