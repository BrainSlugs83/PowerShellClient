using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Security;
using System.Text;

namespace PowerShellClient
{
    /// <summary>
    /// Connection Info object for creating a PSClient.
    /// </summary>
    public class PSConnectionInfo : ICloneable
    {
        /// <summary>
        /// The default Remote PowerShell port.
        /// </summary>
        public const ushort DefaultRemotePowerShellPort = 5986;

        /// <summary>
        /// Gets or sets a value indicating whether or not a secure connection is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if a secure connection is required; otherwise <c>false</c>.
        /// </value>
        /// <remarks>
        /// Note that if <see cref="RequireValidCertificate">RequireValidCertificate</see> is set to <c>false</c>, that the 
        /// connection isn't really secure.  It's just using a secure port and protocol (in an insecure way).
        /// </remarks>
        public bool UseSecurePowerShell { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the SSL certificate must be validated
        /// or not.
        /// </summary>
        /// <remarks>
        /// This is set to false by default, as it requires a fair bit of setup to get it to work on a given machine.
        /// </remarks>
        public bool RequireValidCertificate { get; set; } = false;

        /// <summary>
        /// Gets or sets the Remote Computer Address.
        /// </summary>
        /// <value>
        /// IP Address, Machine Name (local network), FQDN, etc. of the remote computer.
        /// </value>
        public string ComputerAddress { get; set; }

        /// <summary>
        /// Gets or sets the port used to connect to the Remote Computer.
        /// </summary>
        public ushort Port { get; set; } = DefaultRemotePowerShellPort;

        /// <summary>
        /// Gets or sets the Credentials to be used by the remote connection.
        /// </summary>
        public PSCredential Credentials { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Returns <c>true</c> if the connection is a local one; otherwise <c>false</c>.
        /// </summary>
        public bool IsLocalConnection()
        {
            bool result = false;
            if (Port <= 0)
            {
                if (IPAddress.TryParse(ComputerAddress, out IPAddress ip))
                {
                    result = IPAddress.IsLoopback(ip); // this will work for IPV4 and IPV6 (which has several different ways of indicating localhost).
                }

                if (!result)
                {
                    result =
                    (
                        string.Equals(ComputerAddress, "localhost", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(ComputerAddress, "(local)", StringComparison.InvariantCultureIgnoreCase) // SQL-Style
                    );
                }
            }

            return result;
        }

        /// <summary>
        /// Creates Remote PowerShell Connection Information for a given computer, using a
        /// standard UserName and Password combination.
        /// </summary>
        /// <param name="computerAddress">
        /// The computer address (IP Address, Machine Name (local network), FQDN, etc.)
        /// </param>
        /// <param name="userName">The UserName to connect with.</param>
        /// <param name="password">The Password to connect with.</param>
        public static PSConnectionInfo CreateRemoteConnection
        (
            string computerAddress,
            string userName,
            SecureString password,
            ushort? customPort = null
        )
        {
            var result = new PSConnectionInfo
            {
                ComputerAddress = computerAddress,
                Credentials = new PSCredential
                (
                    userName,
                    password
                )
            };

            if (customPort.HasValue)
            {
                result.Port = customPort.Value;
            }
            return result;
        }

        /// <summary>
        /// Creates a <see cref="PSConnectionInfo" /> object that represents a
        /// connection to the local machine (i.e. allows you to use to library for
        /// non-remote connections).
        /// </summary>
        public static PSConnectionInfo CreateLocalConnection()
        {
            return new PSConnectionInfo
            {
                ComputerAddress = "127.0.0.1",
                Credentials = null,
                Port = 0,
                RequireValidCertificate = false,
                UseSecurePowerShell = false
            };
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => this.Clone();

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public PSConnectionInfo Clone() =>
            (PSConnectionInfo)this.MemberwiseClone();

        /// <summary>
        /// Converts this instance into runspace connection information.
        /// </summary>
        internal RunspaceConnectionInfo ToRunspaceConnectionInfo()
        {
            return new WSManConnectionInfo
            (
                UseSecurePowerShell,
                ComputerAddress,
                Port,
                "/wsman",
                "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
                Credentials
            )
            {
                UseCompression = true,
                OperationTimeout = (int)this.OperationTimeout.TotalMilliseconds,
                OpenTimeout = (int)this.ConnectionTimeout.TotalMilliseconds,
                SkipCACheck = !RequireValidCertificate,
                SkipCNCheck = !RequireValidCertificate
            };
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string result = ComputerAddress;

            if (IsLocalConnection())
            {
                result = "localhost";
            }

            if (Port > 0) { result += ":" + Port; }

            return result;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode() ^ GetType().GetHashCode();
        }
    }
}
