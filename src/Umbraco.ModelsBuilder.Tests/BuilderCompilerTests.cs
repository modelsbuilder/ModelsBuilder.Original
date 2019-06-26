using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Composing;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class BuilderCompilerTests
    {
        private string _tempDir;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            Current.Reset();
            Current.UnlockConfigs();
            Current.Configs.Add(() => new Config());
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void CanCompile()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code = new Dictionary<string, string>();

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;
            var btype0 = btypes[0];

            var sb = new StringBuilder();
            builder.Generate(sb, btype0);

            var text = sb.ToString();

            var compiler = new Compiler();
            compiler.Compile("Whatever", new Dictionary<string, string> { { "code", text } }, _tempDir);
            Assert.IsTrue(File.Exists(Path.Combine(_tempDir, "Whatever.dll")));
        }
    }
}
