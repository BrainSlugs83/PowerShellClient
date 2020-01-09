using System;
using System.Collections.Generic;
using System.Text;

namespace PowerShellClient
{
    /// <summary>
    /// Specifies the PowerShell Stream to be used, by the various Write-* cmdlets.
    /// </summary>
    public enum PSOutputStream
    {
        /// <summary>
        /// Write-Host
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-host
        /// </remarks>
        Info = 1,

        /// <summary>
        /// Write-Error
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-error
        /// </remarks>
        Error = 2,

        /// <summary>
        /// Write-Warning
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-warning
        /// </remarks>
        Warning = 3,

        /// <summary>
        /// Write-Verbose
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-verbose
        /// </remarks>
        Verbose = 4,

        /// <summary>
        /// Write-Debug
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-debug
        /// </remarks>
        Debug = 5,

        /// <summary>
        /// Write-Host
        /// </summary>
        /// <remarks>
        /// See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-host
        /// </remarks>
        Default = Info
    }
}
