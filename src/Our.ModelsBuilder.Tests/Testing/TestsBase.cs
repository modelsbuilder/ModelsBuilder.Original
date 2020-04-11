using Moq;
using NUnit.Framework;
using Umbraco.Core.Composing;
using Umbraco.Core.Strings;

namespace Our.ModelsBuilder.Tests.Testing
{
    public abstract class TestsBase
    {
        [SetUp]
        public void SetUp()
        {
            Current.Reset();

            // need a static IShortStringHelper ;(
            var shortStringHelper = new DefaultShortStringHelper(new DefaultShortStringHelperConfig());
            var factory = Mock.Of<IFactory>();
            Mock.Get(factory).Setup(x => x.TryGetInstance(typeof(IShortStringHelper))).Returns(shortStringHelper);
            Current.Factory = factory;
        }
    }
}