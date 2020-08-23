using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PowerShellClient.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class PSClientInput_Tests
    {
        [TestMethod]
        [DoNotParallelize]
        public async Task ReadLine_Exception1_Tests()
        {
            using (var client = new PSClient())
            {
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                try
                {
                    var result = await client.InvokeScriptAsync<string>("Read-Host");
                    Assert.Fail("Read-Host did not fail!");
                }
                catch (Exception ex)
                {
                    if (!(ex is InvalidOperationException) && !(ex is CmdletInvocationException))
                    {
                        throw;
                    }
                }
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task ReadLine_Exception2_Tests()
        {
            using (var client = new PSClient())
            {
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                client.ConfigureNonInteractiveConsoleHost();
                await Assert.ThrowsExceptionAsync<CmdletInvocationException>(async () => await client.InvokeScriptAsync<string>("Read-Host"));
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task ReadLine_Exception3_Tests()
        {
            using (var client = new PSClient())
            {
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                client.ConfigureNonInteractiveSilentHost();
                await Assert.ThrowsExceptionAsync<CmdletInvocationException>(async () => await client.InvokeScriptAsync<string>("Read-Host"));
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task ReadLine_Test()
        {
            using (var client = new PSClient())
            {
                await client.OpenAsync(PSConnectionInfo.CreateLocalConnection());
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                client.ConfigureNonInteractiveConsoleHost();

                client.HostUI.ReadLineCallback = () => "Hello World";
                Assert.AreEqual("Hello World", (await client.InvokeScriptAsync<string>("Read-Host")).Single());
            }
        }
    }
}