using System;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellClient
{
    /// <summary>
    /// Used for executing PowerShell against a connected machine.
    /// </summary>
    public interface IPSClient : IDisposable
    {
        /// <summary>
        /// Gets a copy of the current connection information.
        /// </summary>
        PSConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Gets the associated <see cref="PSClientHost" />.
        /// </summary>
        PSClientHost Host { get; }

        /// <summary>
        /// Gets the associated <see cref="PSClientHostUI" />.
        /// </summary>
        PSClientHostUI HostUI { get; }

        /// <summary>
        /// Gets the connected file system.
        /// </summary>
        PSFileSystem FileSystem { get; }

        /// <summary>
        /// Opens the connection.
        /// </summary>
        /// <param name="connectionInfo">
        /// The connection information.
        /// </param>
        void Open(PSConnectionInfo connectionInfo);

        /// <summary>
        /// Opens the connection, asynchronously.
        /// </summary>
        /// <param name="connectionInfo">
        /// The connection information.
        /// </param>
        Task OpenAsync(PSConnectionInfo connectionInfo);

        /// <summary>
        /// Tests that a given connection is valid and can be opened.
        /// </summary>
        /// <param name="connectionInfo">
        /// The connection information.
        /// </param>
        bool TestConnection(PSConnectionInfo connectionInfo);

        /// <summary>
        /// Tests that a given connection is valid and can be opened, asynchronously.
        /// </summary>
        /// <param name="connectionInfo">
        /// The connection information.
        /// </param>
        Task<bool> TestConnectionAsync(PSConnectionInfo connectionInfo);

        /// <summary>
        /// Closes the connection (if open).
        /// </summary>
        /// <remarks>
        /// Must not fail or throw.
        /// </remarks>
        void Close();

        /// <summary>
        /// Closes the connection, asynchronously (if open).
        /// </summary>
        /// <remarks>
        /// Must not fail or throw.
        /// </remarks>
        Task CloseAsync();

        /// <summary>
        /// Invokes a PowerShell command.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="cmdlet">The name of the cmdlet to invoke.</param>
        /// <param name="parameters">
        /// An anonymous object containing the parameters for the cmdlet.
        /// </param>
        /// <param name="switches">A collection of switches for the cmdlet.</param>
        ICollection<T> InvokeCommand<T>(string cmdlet, object parameters = null, params string[] switches);

        /// <summary>
        /// Invokes a PowerShell command.
        /// </summary>
        /// <param name="cmdlet">The name of the cmdlet to invoke.</param>
        /// <param name="parameters">
        /// An anonymous object containing the parameters for the cmdlet.
        /// </param>
        /// <param name="switches">A collection of switches for the cmdlet.</param>
        void InvokeCommand(string cmdlet, object parameters = null, params string[] switches);

        /// <summary>
        /// Invokes a PowerShell command asynchronously.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="cmdlet">The name of the cmdlet to invoke.</param>
        /// <param name="parameters">
        /// An anonymous object containing the parameters for the cmdlet.
        /// </param>
        /// <param name="switches">A collection of switches for the cmdlet.</param>
        Task<ICollection<T>> InvokeCommandAsync<T>(string cmdlet, object parameters = null, params string[] switches);

        /// <summary>
        /// Invokes a PowerShell command asynchronously.
        /// </summary>
        /// <param name="cmdlet">The name of the cmdlet to invoke.</param>
        /// <param name="parameters">
        /// An anonymous object containing the parameters for the cmdlet.
        /// </param>
        /// <param name="switches">A collection of switches for the cmdlet.</param>
        Task InvokeCommandAsync(string cmdlet, object parameters = null, params string[] switches);

        /// <summary>
        /// Invokes a PowerShell command.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="command">The PowerShell command to invoke.</param>
        ICollection<T> InvokeCommand<T>(Command command);

        /// <summary>
        /// Invokes a PowerShell command.
        /// </summary>
        /// <param name="command">The PowerShell command to invoke.</param>
        void InvokeCommand(Command command);

        /// <summary>
        /// Invokes a PowerShell command asynchronously.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="command">The PowerShell command to invoke.</param>
        Task<ICollection<T>> InvokeCommandAsync<T>(Command command);

        /// <summary>
        /// Invokes a PowerShell command asynchronously.
        /// </summary>
        /// <param name="command">The PowerShell command to invoke.</param>
        Task InvokeCommandAsync(Command command);

        /// <summary>
        /// Invokes PowerShell command(s).
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="commands">The PowerShell commands to invoke.</param>
        ICollection<T> InvokePipedCommands<T>(params Command[] commands);

        /// <summary>
        /// Invokes PowerShell command(s).
        /// </summary>
        /// <param name="commands">The PowerShell commands to invoke.</param>
        void InvokePipedCommands(params Command[] commands);

        /// <summary>
        /// Invokes PowerShell command(s) asynchronously.
        /// </summary>
        /// <param name="commands">The PowerShell commands to invoke.</param>
        Task InvokeCommandsAsync(params Command[] commands);

        /// <summary>
        /// Invokes PowerShell command(s) asynchronously.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="commands">The PowerShell commands to invoke.</param>
        Task<ICollection<T>> InvokeCommandsAsync<T>(params Command[] commands);

        /// <summary>
        /// Invokes an arbitrary block of PowerShell script.
        /// </summary>
        /// <param name="script">The PowerShell script block.</param>
        void InvokeScript(string script);

        /// <summary>
        /// Invokes an arbitrary block of PowerShell script.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="script">The PowerShell script block.</param>
        ICollection<T> InvokeScript<T>(string script);

        /// <summary>
        /// Invokes an arbitrary PowerShell script block, asynchronously.
        /// </summary>
        /// <param name="script">The PowerShell script block.</param>
        Task InvokeScriptAsync(string script);

        /// <summary>
        /// Invokes an arbitrary PowerShell script block, asynchronously.
        /// </summary>
        /// <typeparam name="T">
        /// The type of results to return from the output stream.
        /// </typeparam>
        /// <param name="script">The PowerShell script block.</param>
        Task<ICollection<T>> InvokeScriptAsync<T>(string script);
    }
}
