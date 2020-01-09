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
    public class PSFileSystem_Tests
    {
        [TestMethod]
        public void Null_Test()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new PSFileSystem(null));
        }

        [TestMethod]
        public async Task PutFile_Tests()
        {
            await PutFile(@"C:\temp\test.txt");
            await PutFile(@"C:\temp\[test].txt");
        }

        public async Task PutFile(string path)
        {
            using (var client = new PSClient(PSConnectionInfo.CreateLocalConnection()))
            {
                await client.FileSystem.PutFileAsync(path, Encoding.UTF8.GetBytes("HELLO WORLD!"), true);
                Assert.IsTrue(await client.FileSystem.PathExistsAsync(path, true, false));
                Assert.IsTrue(File.Exists(path));
                var txt = await client.FileSystem.GetFileHashAsync(path, "MD5+Length");
                Assert.AreEqual("B59BC37D6441D96785BDA7AB2AE98F75::12", txt);

                await client.FileSystem.DeleteFileAsync(path);
                Assert.IsFalse(await client.FileSystem.PathExistsAsync(path, true, false));
                Assert.IsFalse(File.Exists(path));
            }


        }

    }
}
