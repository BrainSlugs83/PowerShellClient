using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PowerShellClient.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class Exception_Tests
    {
        [TestMethod]
        public void ExceptionExploder_Tests()
        {
            Assert.AreEqual(0, PSUtils.GetAllExceptions(null).Count());
            Assert.AreEqual(0, PSUtils.GetAllExceptions(new object[] { }).Count());

            Assert.AreEqual(1, PSUtils.GetAllExceptions("Hello World").Count());
            Assert.AreEqual(2, PSUtils.GetAllExceptions(new object[] { "Hello World", new Exception("Goodbye, World") }).Count());
            Assert.AreEqual
            (
                3,
                PSUtils.GetAllExceptions
                (
                    new object[]
                    {
                            "Hello World",
                            new Exception("Goodbye, World"),
                            new AggregateException("HI!")
                    }
                )
                .Count()
            );

            var rn = Environment.NewLine;
            Assert.AreEqual($"Hello{rn}World", PSUtils.GetSingleException(new[] { "Hello", "World" }).Message);
            Assert.AreEqual("Unspecified Error", PSUtils.GetSingleException(null).Message);
            Assert.AreEqual("Unspecified Error", PSUtils.GetSingleException(string.Empty).Message);
            Assert.AreEqual("42", PSUtils.GetSingleException(42).Message);
        }

        [TestMethod]
        public void SingleAndAggregateExceptionTests()
        {
            Exception ex = new FileNotFoundException("FOOP");
            Assert.AreEqual(ex, PSUtils.GetSingleException(ex));
            Assert.AreEqual(1, PSUtils.GetAllExceptions(ex).Count());
            Assert.AreEqual(ex, PSUtils.GetAllExceptions(ex).Single());

            ex = new AggregateException(new[] { new Exception("1"), new Exception("2"), new Exception("3") });
            Assert.AreEqual(ex, PSUtils.GetSingleException(ex));
            Assert.AreEqual(3, PSUtils.GetAllExceptions(ex).Count());

            var inner = new Exception("1");
            ex = new AggregateException(new[] { inner });
            Assert.AreEqual(inner, PSUtils.GetSingleException(ex));
            Assert.AreEqual(1, PSUtils.GetAllExceptions(ex).Count());
            Assert.AreEqual(inner, PSUtils.GetAllExceptions(ex).Single());

            ex = new AggregateException(inner);
            Assert.AreEqual(inner, PSUtils.GetSingleException(ex));
            Assert.AreEqual(1, PSUtils.GetAllExceptions(ex).Count());
            Assert.AreEqual(inner, PSUtils.GetAllExceptions(ex).Single());

            ex = PSUtils.GetSingleException(new[] { new Exception("1"), new Exception("2") });
            Assert.IsTrue(ex is AggregateException);
            Assert.AreEqual(2, (ex as AggregateException).InnerExceptions.Count);
            Assert.AreEqual(2, PSUtils.GetAllExceptions(ex).Count());

        }
    }
}
