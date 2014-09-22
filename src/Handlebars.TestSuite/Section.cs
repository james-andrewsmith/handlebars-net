using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Handlebars.TestSuite
{
    [TestClass]
    public class SectionTestClass
    {
        #region // Engine Management //
        [ClassInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            _engine = HandlebarsFactory.CreateEngine();
        }

        private static IHandlebarsEngine _engine;
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
