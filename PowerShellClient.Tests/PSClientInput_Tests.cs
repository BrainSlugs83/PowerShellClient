using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PowerShellClient.Tests
{
    [TestClass]
    public class PSClientInput_Tests
    {
        [TestMethod]
        public async Task ReadLine_Tests()
        {
            using (var client = new PSClient(PSConnectionInfo.CreateLocalConnection()))
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await client.InvokeScriptAsync<string>("Read-Host"));
                
                client.ConfigureNonInteractiveConsoleHost();
                await Assert.ThrowsExceptionAsync<CmdletInvocationException>(async () => await client.InvokeScriptAsync<string>("Read-Host"));

                client.ConfigureNonInteractiveSilentHost();
                await Assert.ThrowsExceptionAsync<CmdletInvocationException>(async () => await client.InvokeScriptAsync<string>("Read-Host"));

                client.HostUI.ReadLineCallback = () => "Hello World";
                Assert.AreEqual("Hello World", (await client.InvokeScriptAsync<string>("Read-Host")).Single());
            }
        }
    }
}
