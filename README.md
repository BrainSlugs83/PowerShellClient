# Better PowerShell Client
... is a simple easy to use .NET API for interacting with PowerShell, it handles subtleties for you, has overrides for getting back strong typed objects from PowerShell commands and scripts, and can execute code locally and remotely (which can be useful to manage remote machines, and unzip files down to them, etc.).

Note that for the following code samples the documentation will be using the synchronous methods, but all of them have async variants with the appropriate suffix.

### Creating a Connection

To create a connected (to the local machine) client object, that writes output to the Console, use the following code:

```csharp
using var client = new PSClient(PSConnectionInfo.CreateLocalConnection());
client.ConfigureNonInteractiveConsoleHost();
```

Note that `ConfigureNonInteractiveSilentHost` is also an option, as is manually configuring the UI Host.

Additionally we could instead call the default constructor with no parameters, and perform the connection manually using `client.Open(cxnNfo);` or `await client.OpenAsync(cxnNfo);` commands (i.e. if we wanted to defer the connection, or perform it asychronously).

It is important to create this object in a `using` block though, or to call `client.Dispose` in a finally block, as this will close the connection and free up any resources associated with it.

### Simple Command Invocation

With the client object you can execute PowerShell scripts easily:
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

You can read back values from your commands:
```csharp
int result = client.InvokeScript<int>("5 * 24").Single();
Console.WriteLine(result); // 120
```

### Remote File Operations

There's a whole set of file system operations, which are super useful if you're operating against a remote machine:
```csharp
client.FileSystem.PutFile(@"c:\path\to\file.txt", Encoding.UTF8.GetBytes("Hello, World!"));
```

### Unziping Files
You can even unzip files to a connected PowerShell client.
```csharp
using (var ms = new MemoryStream(/* ... get zip data somehow ... */))
using (var zip = new System.IO.Compression.ZipArchive(ms))
{
    client.FileSystem.UnzipTo(zip, @"C:\path\to\unzip\");
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
```

#### Troubleshooting Remote Connections

If you're unable to make the Remote PowerShell stuff work, you probably have something configured wrong.  I recommend testing that your configuration works via the PowerShell ISE, using the following script:
```ps

$computerName = "SomeComputer.westus.cloudapp.azure.com";
$port = 3389;

$user = "UserName";
$pwd = ("My Completely Insecure Password" | ConvertTo-SecureString -AsPlainText -Force);

$cred = New-Object -TypeName System.Management.Automation.PSCredential `
    -ArgumentList $user, $pwd;

Enter-PSSession -ComputerName $computerName -Port $port -UseSSL `
    -SessionOption(New-PSsessionOption -SkipCACheck -SkipCNCheck) -Credential $cred;

```

If you're able to make that work, then you should have no problem connecting via the API.