using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text;

namespace PowerShellClient
{
    /// <summary>
    /// Custom Implementation of PSHost for the Remote PowerShell object.
    /// </summary>
    /// <seealso cref="PSHost" />
    /// <seealso cref="PSClientHostUI" />
    public class PSClientHost : PSHost
    {
        /// <summary>
        /// Gets or sets the most recent exit code that was passed in to the
        /// <see cref="SetShouldExit" /> method.
        /// </summary>
        public int ShouldExitCode { get; set; } = 0;

        /// <summary>
        /// Gets the current culture.
        /// </summary>
        public override CultureInfo CurrentCulture => CultureInfo.InvariantCulture;

        /// <summary>
        /// Gets the current UI culture.
        /// </summary>
        public override CultureInfo CurrentUICulture => CultureInfo.InvariantCulture;

        /// <summary>
        /// Gets the instance identifier.
        /// </summary>
        public override Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => GetType().AssemblyQualifiedName;

        /// <summary>
        /// Gets the PowerShell Host User Interface Object.
        /// </summary>
        public override PSHostUserInterface UI { get; } = new PSClientHostUI();

        /// <summary>
        /// Gets the version.
        /// </summary>
        public override Version Version => GetType().Assembly.GetName().Version;

        /// <summary>
        /// Enters the nested prompt.
        /// </summary>
        public override void EnterNestedPrompt()
        {
        }

        /// <summary>
        /// Exits the nested prompt.
        /// </summary>
        public override void ExitNestedPrompt()
        {
        }

        /// <summary>
        /// When overridden in a derived class, notifies the host that the Windows
        /// PowerShell runtime is about to execute a legacy command-line application. A
        /// legacy application is defined as a console-mode executable that can perform any
        /// of the following operations: read from stdin, write to stdout, write to stderr,
        /// or use any of the Windows console functions.
        /// </summary>
        /// <exception cref="HostException">
        /// The host can throw this exception when it cannot complete an operation.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// A non-interactive host should throw a "not implemented" exception when it
        /// receives this call.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// The host can throw this exception when an error occurs while a command is
        /// running.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The Windows PowerShell engine can call this method several times in the course
        /// of a single pipeline. For example, the pipeline abc.exe | bar-cmdlet | baz.exe
        /// causes a sequence of calls similar to the following:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// NotifyBeginApplication: Called once when abc.exe is started.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// NotifyBeginApplication: Called once when baz.exe is started.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// NotifyEndApplication: Called once when baz.exe terminates.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// NotifyEndApplication: Called once when abc.exe terminates.
        /// </description>
        /// </item>
        /// </list>
        /// <para>
        /// Note that the order in which the NotifyEndApplication method call follows its
        /// corresponding NotifyBeginApplication method call is not defined and should not
        /// be depended upon. In other words, the NotifyBeginApplication method can be
        /// called several times before a corresponding NotifyEndApplication method is
        /// called.The only thing that is guaranteed is that there will be an equal number
        /// of calls to the NotifyBeginApplication and NotifyEndApplication methods.
        /// </para>
        /// </remarks>
        public override void NotifyBeginApplication()
        {
        }

        /// <summary>
        /// When overridden in a derived class, notifies the host that the Windows
        /// PowerShell engine has completed the execution of a legacy command. A legacy
        /// application is defined as a console-mode executable that can perform any of the
        /// following operations: read from stdin, write to stdout, write to stderr, or use
        /// any of the Windows console functions.
        /// </summary>
        /// <exception cref="HostException">
        /// The host can throw this exception when it cannot complete an operation.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// A non-interactive host should throw a "not implemented" exception when it
        /// receives this call.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// The host can throw this exception when an error occurs while a command is
        /// running.
        /// </exception>
        /// <remarks>
        /// Note that the order in which the NotifyEndApplication method call follows its
        /// corresponding NotifyBeginApplication method is not defined and should not be
        /// depended upon. In other words, the NotifyBeginApplication method can be called
        /// several times before a corresponding NotifyEndApplication method is called. The
        /// only thing that is guaranteed is that there will be an equal number of calls to
        /// the NotifyBeginApplication and NotifyEndApplication methods.
        /// </remarks>
        public override void NotifyEndApplication()
        {
        }

        /// <summary>
        /// When overridden in a derived class, requests to end the current runspace. The
        /// Windows PowerShell engine calls this method to request that the host
        /// application shut down and terminate the host root runspace.
        /// </summary>
        /// <param name="exitCode">
        /// The exit code that is used to set the host's process exit code.
        /// </param>
        /// <exception cref="HostException">
        /// The host can throw this exception when it cannot complete an operation.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// The host can throw this exception when an error occurs while a command is
        /// running.
        /// </exception>
        /// <remarks>
        /// To honor this request, the host application should stop accepting and
        /// submitting commands to the engine and close the runspace.
        /// </remarks>
        public override void SetShouldExit(int exitCode)
        {
            ShouldExitCode = exitCode;
        }
    }
}
