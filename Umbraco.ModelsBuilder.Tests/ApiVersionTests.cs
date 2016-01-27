using System;
using NUnit.Framework;
using Umbraco.ModelsBuilder.Api;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class ApiVersionTests
    {
        [Test]
        public void IsCompatibleTest()
        {
            // executing version 3.0.0.0, accepting connections from 2.0.0.0
            var av = new ApiVersion(new Version(3, 0, 0, 0), new Version(2, 0, 0, 0));

            // server version 1.0.0.0 is not compatible (too old)
            Assert.IsFalse(av.IsCompatibleWith(new Version(1, 0, 0, 0)));

            // server version 2.0.0.0 or 3.0.0.0 is compatible
            Assert.IsTrue(av.IsCompatibleWith(new Version(2, 0, 0, 0)));
            Assert.IsTrue(av.IsCompatibleWith(new Version(3, 0, 0, 0)));

            // server version 4.0.0.0 is not compatible (too recent)
            Assert.IsFalse(av.IsCompatibleWith(new Version(4, 0, 0, 0)));

            // but can declare it is, indeed, compatible with version 2.0.0.0 or 3.0.0.0
            Assert.IsTrue(av.IsCompatibleWith(new Version(4, 0, 0, 0), new Version(2, 0, 0, 0)));
            Assert.IsTrue(av.IsCompatibleWith(new Version(4, 0, 0, 0), new Version(3, 0, 0, 0)));

            // but...
            Assert.IsFalse(av.IsCompatibleWith(new Version(4, 0, 0, 0), new Version(3, 0, 0, 1)));
        }

        [Test]
        public void CurrentIsCompatibleTest()
        {
            var av = ApiVersion.Current;

            // client version < MinClientVersionSupportedByServer are not supported
            Assert.IsFalse(av.IsCompatibleWith(GetPreviousVersion(av.MinClientVersionSupportedByServer)));

            // client version MinClientVersionSupportedByServer-Version are supported
            Assert.IsTrue(av.IsCompatibleWith(av.MinClientVersionSupportedByServer));
            Assert.IsTrue(av.IsCompatibleWith(GetPreviousVersion(av.Version)));

            // client version > Version are not supported
            Assert.IsFalse(av.IsCompatibleWith(GetNextVersion(av.Version)));

            // unless client says so
            Assert.IsTrue(av.IsCompatibleWith(GetNextVersion(av.Version), av.Version));
        }

        [Test]
        public void NextVersionTest()
        {
            Assert.AreEqual(new Version(0, 0, 0, 1), GetNextVersion(new Version(0, 0, 0, 0)));
            Assert.AreEqual(new Version(1, 0, 0, 1), GetNextVersion(new Version(1, 0, 0, 0)));
            Assert.AreEqual(new Version(1, 0, 0, 8), GetNextVersion(new Version(1, 0, 0, 7)));
        }

        [Test]
        public void PreviousVersionTest()
        {
            Assert.AreEqual(new Version(0, 0, 0, 0), GetPreviousVersion(new Version(0, 0, 0, 1)));
            Assert.AreEqual(new Version(0, 0, 0, 1), GetPreviousVersion(new Version(0, 0, 0, 2)));
            Assert.AreEqual(new Version(0, 0, 0, 999), GetPreviousVersion(new Version(0, 0, 1, 0)));
            Assert.AreEqual(new Version(0, 0, 999, 999), GetPreviousVersion(new Version(0, 1, 0, 0)));
            Assert.AreEqual(new Version(0, 999, 999, 999), GetPreviousVersion(new Version(1, 0, 0, 0)));
            Assert.AreEqual(new Version(1, 999, 999, 999), GetPreviousVersion(new Version(2, 0, 0, 0)));
        }

        private static Version GetNextVersion(Version version)
        {
            return new Version(version.Major, version.Minor, version.Build, version.Revision + 1);
        }

        private static Version GetPreviousVersion(Version version)
        {
            if (version.Revision > 0)
                return new Version(version.Major, version.Minor, version.Build, version.Revision - 1);
            if (version.Build > 0)
                return new Version(version.Major, version.Minor, version.Build - 1, 999);
            if (version.Minor > 0)
                return new Version(version.Major, version.Minor - 1, 999, 999);
            if (version.Major > 0)
                return new Version(version.Major - 1, 999, 999, 999);

            throw new ArgumentOutOfRangeException(nameof(version));
        }
    }
}
