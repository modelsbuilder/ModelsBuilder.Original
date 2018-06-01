using System;
using NUnit.Framework;
using Semver;
using Umbraco.ModelsBuilder.Api;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class ApiVersionTests
    {
        [Test]
        public void IsCompatibleTest()
        {
            // executing version 3.0.0, accepting connections from 2.0.0
            var av = new ApiVersion(new SemVersion(3, 0, 0), new SemVersion(2, 0, 0));

            // server version 1.0.0 is not compatible (too old)
            Assert.IsFalse(av.IsCompatibleWith(new SemVersion(1, 0, 0)));

            // server version 2.0.0 or 3.0.0 is compatible
            Assert.IsTrue(av.IsCompatibleWith(new SemVersion(2, 0, 0)));
            Assert.IsTrue(av.IsCompatibleWith(new SemVersion(3, 0, 0)));

            // server version 4.0.0 is not compatible (too recent)
            Assert.IsFalse(av.IsCompatibleWith(new SemVersion(4, 0, 0)));

            // but can declare it is, indeed, compatible with version 2.0.0 or 3.0.0
            Assert.IsTrue(av.IsCompatibleWith(new SemVersion(4, 0, 0), new SemVersion(2, 0, 0)));
            Assert.IsTrue(av.IsCompatibleWith(new SemVersion(4, 0, 0), new SemVersion(3, 0, 0)));

            // but...
            Assert.IsFalse(av.IsCompatibleWith(new SemVersion(4, 0, 0), new SemVersion(3, 0, 1)));
        }

        [Test]
        public void CurrentIsCompatibleTest()
        {
            var av = ApiVersion.Current;

            // client version < MinClientVersionSupportedByServer are not supported
            Assert.IsFalse(av.IsCompatibleWith(GetPreviousVersion(av.MinClientVersionSupportedByServer)));

            // client version MinClientVersionSupportedByServer-Version are supported
            Assert.IsTrue(av.IsCompatibleWith(av.MinClientVersionSupportedByServer));

            // client version > Version are not supported
            Assert.IsFalse(av.IsCompatibleWith(GetNextVersion(av.Version)));

            // unless client says so
            Assert.IsTrue(av.IsCompatibleWith(GetNextVersion(av.Version), av.Version));
        }

        [Test]
        public void NextVersionTest()
        {
            Assert.AreEqual(new SemVersion(0, 0, 1), GetNextVersion(new SemVersion(0, 0, 0)));
            Assert.AreEqual(new SemVersion(1, 0, 1), GetNextVersion(new SemVersion(1, 0, 0)));
            Assert.AreEqual(new SemVersion(1, 0, 8), GetNextVersion(new SemVersion(1, 0, 7)));
        }

        [Test]
        public void PreviousVersionTest()
        {
            Assert.AreEqual(new SemVersion(0, 0, 0), GetPreviousVersion(new SemVersion(0, 0, 1)));
            Assert.AreEqual(new SemVersion(0, 0, 1), GetPreviousVersion(new SemVersion(0, 0, 2)));
            Assert.AreEqual(new SemVersion(0, 0, 999), GetPreviousVersion(new SemVersion(0, 1, 0)));
            Assert.AreEqual(new SemVersion(0, 0, 999), GetPreviousVersion(new SemVersion(0, 1, 0)));
            Assert.AreEqual(new SemVersion(0, 999, 999), GetPreviousVersion(new SemVersion(1, 0, 0)));
            Assert.AreEqual(new SemVersion(1, 999, 999), GetPreviousVersion(new SemVersion(2, 0, 0)));
        }

        private static SemVersion GetNextVersion(SemVersion version)
        {
            return new SemVersion(version.Major, version.Minor, version.Patch + 1);
        }

        private static SemVersion GetPreviousVersion(SemVersion version)
        {
            if (version.Prerelease != "")
            {
                var p = version.Prerelease.Split('.');
                return new SemVersion(version.Major, version.Minor, version.Patch, p[0] + "." + (int.Parse(p[1]) - 1));
            }
            if (version.Patch > 0)
                return new SemVersion(version.Major, version.Minor, version.Patch - 1);
            if (version.Minor > 0)
                return new SemVersion(version.Major, version.Minor - 1, 999);
            if (version.Major > 0)
                return new SemVersion(version.Major - 1, 999, 999);

            throw new ArgumentOutOfRangeException(nameof(version));
        }
    }
}
