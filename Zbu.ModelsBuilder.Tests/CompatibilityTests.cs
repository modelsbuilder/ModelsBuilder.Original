using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Zbu.ModelsBuilder.Tests
{
    [TestFixture]
    public class CompatibilityTests
    {
        [Test]
        public void TestCompatibility()
        {
            // client version < 2.1.0 not supported
            Assert.IsFalse(IsOk(new Version(2, 0, 0, 0)));
            Assert.IsFalse(IsOk(new Version(2, 0, 1, 0)));
            Assert.IsFalse(IsOk(new Version(2, 0, 2, 0)));
            Assert.IsFalse(IsOk(new Version(2, 0, 3, 0)));
            Assert.IsFalse(IsOk(new Version(2, 0, 4, 0)));

            // client version 2.1.0-2.1.2 supported
            Assert.IsTrue(IsOk(new Version(2, 1, 0, 0)));
            Assert.IsTrue(IsOk(new Version(2, 1, 1, 0)));
            Assert.IsTrue(IsOk(new Version(2, 1, 2, 0)));

            // client version > 2.1.2 not supported
            Assert.IsFalse(IsOk(new Version(2, 1, 3, 0)));

            // unless client says so
            Assert.IsTrue(IsOk(new Version(2, 1, 3, 0), new Version(2, 1, 0, 0)));
        }

        private static bool IsOk(Version clientVersion, Version minServerVersionSupportingClient = null)
        {
            // that what's in ModelsBuilderApiController

            var isOk = minServerVersionSupportingClient == null
                ? Compatibility.IsCompatible(clientVersion)
                : Compatibility.IsCompatible(clientVersion, minServerVersionSupportingClient);
            return isOk;
        }
    }
}
