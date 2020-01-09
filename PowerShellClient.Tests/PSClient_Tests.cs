using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PowerShellClient.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class PSClient_Tests
    {
        public static string GetThisFilePath([CallerFilePath]string filePath = null) => filePath;
        public static string GetZipFilePath() => Path.Combine(Path.GetDirectoryName(GetThisFilePath()), "Test.zip");

        [TestMethod]
        public async Task TestConnection_Tests()
        {
            using (var client = new PSClient())
            {
                Assert.ThrowsException<ArgumentNullException>(() => client.TestConnection(null));
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await client.TestConnectionAsync(null));

                Assert.IsTrue(await client.TestConnectionAsync(PSConnectionInfo.CreateLocalConnection()));

                var rnd = new Random(Guid.NewGuid().GetHashCode());
                var badRemoteCxnNfo = PSConnectionInfo.CreateRemoteConnection
                (
                    "localhost",
                    "notarealusername",
                    "notarealpassword".ToSecureString(),
                    (ushort)rnd.Next(3000, 4000)
                );

                badRemoteCxnNfo.ConnectionTimeout = TimeSpan.FromSeconds(1);
                badRemoteCxnNfo.OperationTimeout = TimeSpan.FromSeconds(1);

                Assert.IsFalse(await client.TestConnectionAsync(badRemoteCxnNfo));
            }
        }

        [TestMethod]
        public async Task StringResult_Tests()
        {
            using (var client = new PSClient())
            {
                var value = "Hello World -- " + Guid.NewGuid().ToString();
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                var result = await client.InvokeScriptAsync<string>(PSUtils.EscapeString(value));

                Assert.AreEqual(value, result.Single());
            }
        }

        [TestMethod]
        public async Task TestWriteHost()
        {
            using (var client = new PSClient())
            {
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                string v = string.Empty;
                client.HostUI.WriteCallback = (s, fg, bg, c) =>
                {
                    if (s == PSOutputStream.Default)
                    {
                        v += c;
                    }
                };
                await client.InvokeScriptAsync("Write-Host \"Hello World\";");

                Assert.AreEqual("Hello World", v.Trim());
                Assert.IsTrue(v.EndsWith("\n"));

                // Also test multi-close
                await client.CloseAsync();
                await client.CloseAsync();
                await client.CloseAsync();
            }
        }

        [TestMethod]
        public async Task TestWriteHost2()
        {
            using (var client = new PSClient())
            {
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                client.ConfigureNonInteractiveConsoleHost();
                string v = string.Empty;

                var oldCallback = client.HostUI.WriteCallback;
                client.HostUI.WriteCallback = (s, fg, bg, c) =>
                {
                    if (s == PSOutputStream.Default)
                    {
                        v += c;
                    }

                    oldCallback?.Invoke(s, fg, bg, c);
                };
                await client.InvokeScriptAsync("Write-Host \"Hello World\";");

                Assert.AreEqual("Hello World", v.Trim());
                Assert.IsTrue(v.EndsWith("\n"));
            }
        }

        [TestMethod]
        public async Task ReadFile_Tests()
        {
            await ReadFile(GetThisFilePath());
            await GetHash(GetThisFilePath());

            await ReadFile(GetZipFilePath());
            await GetHash(GetZipFilePath());
        }

        [TestMethod]
        public async Task UnzipFile_Tests()
        {
            await UnzipFile(GetZipFilePath());
        }

        public async Task UnzipFile(string fileName)
        {
            var outputPath = Path.Combine(@"C:\Temp\", Path.GetFileNameWithoutExtension(fileName));

            using (var client = new PSClient(PSConnectionInfo.CreateLocalConnection()))
            using (var ms = new MemoryStream(File.ReadAllBytes(fileName)))
            using (var zar = new ZipArchive(ms))
            {
                await client.FileSystem.DeleteFolderRecursivelyAsync(outputPath);
                Assert.IsFalse(Directory.Exists(outputPath), $"Directory {outputPath} was not removed!");
                client.ConfigureNonInteractiveSilentHost();

                client.FileSystem.UnzipTo(zar, outputPath);

                foreach (var f in zar.Entries)
                {
                    var fn = Path.Combine(outputPath, f.FullName)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                        .TrimEnd(Path.DirectorySeparatorChar);

                    if (f.Length == 0)
                    {
                        Assert.IsTrue(Directory.Exists(fn), $"Directory {fn} does not exist!");
                    }
                    else
                    {
                        Assert.IsTrue(File.Exists(fn), $"File {fn} does not exist!");
                        Assert.AreEqual(f.Length, new FileInfo(fn).Length, $"File {fn} is the wrong size!");

                        string expectedHash, actualHash;
                        using (var s = f.Open())
                        using (var md5 = MD5.Create())
                        {
                            expectedHash = BitConverter.ToString(md5.ComputeHash(s))
                                .Replace("-", string.Empty).ToUpperInvariant();
                        }

                        using (var s = File.OpenRead(fn))
                        using (var md5 = MD5.Create())
                        {
                            actualHash = BitConverter.ToString(md5.ComputeHash(s))
                                .Replace("-", string.Empty).ToUpperInvariant();
                        }

                        Assert.AreEqual(expectedHash, actualHash, $"File {fn} does not match the expected MD5 hash.");

                        // TODO: Verify no extra files left on disk!
                    }
                }

                await client.FileSystem.DeleteFolderRecursivelyAsync(outputPath);
                Assert.IsFalse(Directory.Exists(outputPath), $"Directory {outputPath} was not removed!");
            }
        }

        public async Task ReadFile(string path)
        {
            var contents = File.ReadAllText(path);
            await ReadFile(path, contents);
        }

        public async Task GetHash(string path)
        {
            string hash;
            using (var s = File.OpenRead(path))
            using (var md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(s))
                    .Replace("-", string.Empty).ToUpperInvariant();
            }
            hash += "::" + new FileInfo(path).Length;

            await GetHash(path, hash);
        }

        public async Task ReadFile(string path, string contents)
        {
            Assert.IsFalse(path.IsNullOrWhiteSpace());
            Assert.IsFalse(contents.IsNullOrWhiteSpace());

            using (var client = new PSClient(PSConnectionInfo.CreateLocalConnection()))
            {
                var txt = await client.FileSystem.GetFileTextAsync(path);
                Assert.AreEqual(contents, txt);
            }
        }

        public async Task GetHash(string path, string md5)
        {
            Assert.IsFalse(path.IsNullOrWhiteSpace());
            Assert.IsFalse(md5.IsNullOrWhiteSpace());

            using (var client = new PSClient(PSConnectionInfo.CreateLocalConnection()))
            {
                Assert.IsTrue(md5.Contains("::"));
                Assert.IsFalse(md5.Before("::").IsNullOrWhiteSpace());
                Assert.IsFalse(md5.After("::").IsNullOrWhiteSpace());

                var txt = await client.FileSystem.GetFileHashAsync(path, "MD5+Length");
                Assert.AreEqual(md5, txt);

                txt = await client.FileSystem.GetFileHashAsync(path, "MD5");
                Assert.AreEqual(md5.Before("::"), txt);
            }
        }
    }
}
