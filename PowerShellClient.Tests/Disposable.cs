using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PowerShellClient.Tests
{
    public class Disposable : IDisposable
    {
        Action disposeCallback;

        public Disposable(Action disposeCallback)
        {
            this.disposeCallback = disposeCallback;
        }

        public void Dispose()
        {
            disposeCallback?.Invoke();
            disposeCallback = null;
        }
    }

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class Disposable_Tests
    {
        [TestMethod]
        public void Null_Tests()
        {
            var x = new Disposable(null);
            x.Dispose();
        }

        [TestMethod]
        public void MultipleDispose_Tests()
        {
            int z = 0;
            var x = new Disposable(()=> { z++; });

            Assert.AreEqual(0, z);
            x.Dispose();
            Assert.AreEqual(1, z);
            x.Dispose();
            Assert.AreEqual(1, z); // do not continue to increment.
        }

    }
}
