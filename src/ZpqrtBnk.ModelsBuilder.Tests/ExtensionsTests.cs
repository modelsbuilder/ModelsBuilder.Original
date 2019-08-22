using System.Collections.Generic;
using NUnit.Framework;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void RemoveAll()
        {
            var list = new List<string>
            {
                "a1", "z1", "a2", "z2", "a3", "z3"
            };

            list.RemoveAll(x => x.StartsWith("a"));
            
            Assert.AreEqual(3, list.Count);
            Assert.IsTrue(list.TrueForAll(x => x.StartsWith("z")));
        }
    }
}
