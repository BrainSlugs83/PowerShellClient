# Better PowerShell Client
... is a simple easy to use .NET API for interacting with PowerShell, it handles subtleties for you, has overrides for getting back strong typed objects from PowerShell commands and scripts, and can execute code locally and remotely (which can be useful to manage remote machines, and unzip files down to them, etc.).

Note that for the following code samples the documentation will be using the synchronous methods, but all of them have async variants with the appropriate suffix.

### Installing the Package

```ps
Install-Package BetterPowerShellClient
```

### Creating a Connection

To create a connected (to the local machine) client object, that writes output to the Console, use the following code:

```csharp
using (var client = new PSClient(PSConnectionInfo.CreateLocalConnection()))
{
    client.ConfigureNonInteractiveConsoleHost();    

    // Client is connected and configured to write output to the console.

}
```

Note that `ConfigureNonInteractiveSilentHost` is also an option, as is manually configuring the UI Host.

Additionally we could instead call the default constructor with no parameters, and perform the connection manually using `client.Open(cxnNfo);` or `await client.OpenAsync(cxnNfo);` commands (i.e. if we wanted to defer the connection, or perform it asychronously).

It is important to create this object in a `using` block though, or to call `client.Dispose` in a finally block, as this will close the connection and free up any resources associated with it.

### Simple Command Invocation

With a connected and configured client object, you can execute PowerShell scripts easily:
```csharp
client.InvokeScript("Write-Host 'Hello, World' -ForegroundColor Yellow -NoNewLine");
```

You can also invoke commands, with strong typed parameters:
```csharp
client.InvokeCommand
(
    // Command:
    "Write-Host",
    new
    {
        // Parameters:
        Object = "Hello, World",
        ForegroundColor = ConsoleColor.Cyan
    },
    // Flags
    "NoNewLine"
);
```

### Getting Results Back

You can read back values from your commands, like so:
```csharp
int result = client.InvokeScript<int>("5 * 24").Single();
Console.WriteLine(result); // 120
```

When we call one of these generic methods, we're signalling to PowerShell to hand us all of the objects that were on the stack that match the given type.

If we don't specify a generic method, we're telling PowerShell to consume the items that were left on the stack (which is what it does by default).  When this happens, it will write those values out to the configured UI Host.

For example, the following code will consume the stack of `FileSystemInfo` objects handed back by the `dir` command and return them to your C# code as an `ICollection<FileSystemInfo>`:
```csharp
var files = client.InvokeCommand<FileSystemInfo>("dir");
```

Whereas, the following code (which calls the exact same PowerShell command), does not specify a generic type, so it leaves the items on the stack, and they will be written to the configured UI host.
```csharp
client.InvokeCommand("dir");
```

### PowerShell File System Operations

There's a whole set of file system operations tucked away into the `FileSystem` object, which can be super useful if you're operating against a remote machine (but they will work with any connection, even to the local machine), in this example, we'll push down a text file from an array of bytes:
```csharp
client.FileSystem.PutFile
(
    @"c:\path\to\file.txt", 
    Encoding.UTF8.GetBytes("Hello, World!")
);
```

#### Unziping Files
You can even unzip files to a connected PowerShell client.

```csharp
// Get a ZipArchive somehow, here's a simple one:
using (var ms = new MemoryStream(Convert.FromBase64String("UEsDBBQAAgAIAItsF1EpklViDgAAAAwAAAAGAAAASGkudHh083D18fFXCPcP8nFRBABQSwECFAAUAAIACACLbBdRKZJVYg4AAAAMAAAABgAAAAAAAAABACAAAAAAAAAASGkudHh0UEsFBgAAAAABAAEANAAAADIAAAAAAA==")))
using (var zipFile = new System.IO.Compression.ZipArchive(ms))
{
    // Unzip the file through the connected PowerShell Client:
    client.FileSystem.UnzipTo
    (
        zipFile, 
        @"C:\path\to\unzip\"
    );
}
```

### Remote Connection Example
Performing remote connections is allowed, and the API is abstracted so that all commands will work the exact same as they do if you are operating locally, be warned though, that connecting to a remote machine requires configuration on both machines (see the troubleshooting below, for a way to test if your connection is valid).

```csharp
using (var client = new PSClient())
{
    // Perform a remote connection:
    var cxnNfo = PSConnectionInfo.CreateRemoteConnection
    (
        "SomeComputer.westus.cloudapp.azure.com",
        "UserName",
        "Password".ToSecureString(),
        3389
    );
    cxnNfo.UseSecurePowerShell = true;      // use SSL . . .
    cxnNfo.RequireValidCertificate = false; // ⚠ but don't validate the cert! ⚠
    
    client.Open(cxnNfo); // Or await client.OpenAsync(cxnNfo);
    client.ConfigureNonInteractiveConsoleHost();

    // Now, just do stuff with the connection, like normal:
    var files = client.InvokeCommand<FileSystemInfo>
    (
        "Get-ChildItem",
        new
        {
            Path = @"C:\"
        }
    );

    foreach (var file in files)
    {
        Console.WriteLine(file.FullName);
    }
}
```

#### Troubleshooting Remote Connections

If you're unable to make the Remote PowerShell stuff work, you probably have something configured wrong.  I recommend testing that your configuration works via the PowerShell ISE, using the following script:
```ps
# ==================== Configure Me! ====================
$computerName = "SomeComputer.westus.cloudapp.azure.com";
$port = 3389;
$user = "UserName";
$rawPwd = "My Completely Insecure Password";
# =======================================================

$pwd = ($rawPwd | ConvertTo-SecureString -AsPlainText -Force);

$cred = New-Object -TypeName System.Management.Automation.PSCredential `
    -ArgumentList $user, $pwd;

Enter-PSSession -ComputerName $computerName -Port $port -UseSSL `
    -SessionOption(New-PSsessionOption -SkipCACheck -SkipCNCheck) -Credential $cred;

```

If you're able to make that work, then you should have no problem connecting via the API.