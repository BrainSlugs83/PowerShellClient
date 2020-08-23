# Better PowerShell Client
... is a simple easy to use .NET API for interacting with PowerShell, it handles subtleties for you, has overrides for getting back strong typed objects from PowerShell commands and scripts, and can execute code locally and remotely (which can be useful to manage remote machines, and unzip files down to them, etc.).

Note that for the following code samples the documentation will be using the synchronous methods, but all of them have async variants with the appropriate suffix.

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

In PowerShell, all commands return a stack of objects -- so in the above sample, we're using the LINQ extension method `.Single()` to get back the single result.

Note that, even if your command doesn't use a `return` statement explicitly, that PowerShell will put any non-consumed objects on to the stack.

When invoking a command, if you ask for a result back, we will try to convert the stack into the type you specify and return all of the matching items to you.

But just like in regular PowerShell, for any item that is not consumed, it will be eventually be written to the console instead.

For example, this code will consume the stack of files handed back by the "dir" command and return them to your C# code.
```csharp
var files = client.InvokeCommand<FileSystemInfo>("dir");
```

Whereas this command (the exact same PowerShell command), leaves the items on the stack, so they will be output to the screen instead (assuming you've configured your ConsoleHost):
```csharp
client.InvokeCommand("dir");
```

(In case it's not clear, storing the results in the `files` variable is not 100% necessary here, just specifying the result type by calling the generic version of the method is all we have to do to signal that we want to consume the output, of course.)

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
using (var ms = new MemoryStream(/* ... get zip data somehow ... */))
using (var zipFile = new System.IO.Compression.ZipArchive(ms))
{
    client.FileSystem.UnzipTo
    (
        zipFile, 
        @"C:\path\to\unzip\"
    );
}
```

Please Note: the above code won't work as-is; you'll have to actually supply a zip file for that! 😉

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