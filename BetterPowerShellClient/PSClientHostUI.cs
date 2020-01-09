using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace PowerShellClient
{
    /// <summary>
    /// Custom Implementation of PSHostUserInterface for the PSClientHost object.
    /// </summary>
    /// <seealso cref="PSHost" />
    /// <seealso cref="PSClientHost" />
    /// <seealso cref="PSHostUserInterface" />
    public class PSClientHostUI : PSHostUserInterface
    {
        /// <summary>
        /// Gets or sets the prompt callback; argument names: caption, message,
        /// descriptions.
        /// </summary>
        public Func<string, string, Collection<FieldDescription>, Dictionary<string, PSObject>> PromptCallback { get; set; }

        /// <summary>
        /// Gets or sets the write callback; argument names: level, foregroundColor,
        /// backgroundColor, message.
        /// </summary>
        public Action<PSOutputStream, ConsoleColor?, ConsoleColor?, string> WriteCallback { get; set; }

        /// <summary>
        /// Gets or sets the prompt for choice callback; argument names: caption, message,
        /// choices, defaultChoice.
        /// </summary>
        public Func<string, string, Collection<ChoiceDescription>, int, int> PromptForChoiceCallback { get; set; }

        /// <summary>
        /// Gets or sets the prompt for credentials callback; argument names: caption,
        /// message, userName, targetName, allowedCredentialTypes, options.
        /// </summary>
        public Func<string, string, string, string, PSCredentialTypes, PSCredentialUIOptions, PSCredential> PromptForCredentialsCallback { get; set; }

        /// <summary>
        /// Gets or sets the read line callback.
        /// </summary>
        public Func<string> ReadLineCallback { get; set; }

        /// <summary>
        /// Gets or sets the write progress callback; argument names: sourceId, record.
        /// </summary>
        public Action<long, ProgressRecord> WriteProgressCallback { get; set; }

        private PSHostRawUserInterface rawUi;

        /// <summary>
        /// Gets the PowerShell Host Raw User Interface Object.
        /// </summary>
        public override PSHostRawUserInterface RawUI => rawUi;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSClientHostUI"
        /// /> class.
        /// </summary>
        public PSClientHostUI()
        {
            rawUi = new Mock<PSHostRawUserInterface>(MockBehavior.Loose)
            {
                CallBase = true
            }
            .SetupAllProperties()
            .Object;

            rawUi.BufferSize = new Size(1000, 9999);
        }

        /// <summary>
        /// Appends the specified message to the running log without any newlines, etc.
        /// (Might be a partial line.)
        /// </summary>
        /// <param name="level">The write level.</param>
        /// <param name="foregroundColor">The foreground color of the display.</param>
        /// <param name="backgroundColor">The background color of the display.</param>
        /// <param name="message">The message.</param>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic",
            Justification = "It might require instance data in the future, depending on how we log this information.")]
        private void WriteToLog
        (
            PSOutputStream level,
            ConsoleColor? foregroundColor,
            ConsoleColor? backgroundColor,
            string message
        )
        {
            Debug.WriteLine($"[{level}] {message?.TrimEnd()}");

            // Looks like the default Write-Host requests just come in as black on black -- seems
            // kind of a bad design, but whatever...
            if (foregroundColor.HasValue && backgroundColor.HasValue
                && foregroundColor.Value == backgroundColor.Value
                && foregroundColor.Value == ConsoleColor.Black)
            {
                foregroundColor = backgroundColor = null;
            }

            WriteCallback?.Invoke(level, foregroundColor, backgroundColor, message);
        }

        /// <summary>
        /// When overridden in a derived class, prompts the user for input.
        /// </summary>
        /// <param name="caption">The text that precedes the prompt.</param>
        /// <param name="message">The text of the prompt.</param>
        /// <param name="descriptions">
        /// A collection of <see cref="FieldDescription" /> objects that contains the user input.
        /// </param>
        /// <returns>
        /// A dictionary of types <see cref="string" /> and <see cref="PSObject" /> that contains
        /// the results of the user prompts. The keys of the dictionary are the field names from 
        /// the FieldDescription objects. The dictionary values are objects that represent the 
        /// values of the corresponding fields as collected from the user.
        /// </returns>
        public override Dictionary<string, PSObject> Prompt
        (
            string caption, 
            string message, 
            Collection<FieldDescription> descriptions
        )
        {
            if (PromptCallback == null) { throw new NotImplementedException(); }
            return PromptCallback(caption, message, descriptions);
        }

        /// <summary>
        /// When overridden in a derived class, provides a set of choices that enable the user to 
        /// choose a single option from a set of options.
        /// </summary>
        /// <param name="caption">The text that precedes (a title) the choices.</param>
        /// <param name="message">A message that describes the choice.</param>
        /// <param name="choices">
        /// A collection of <see cref="ChoiceDescription" /> objects that describe each choice.
        /// </param>
        /// <param name="defaultChoice">The default choice.</param>
        /// <returns>
        /// The index of the Choices parameter collection element that corresponds to the option 
        /// that is selected by the user.
        /// </returns>
        public override int PromptForChoice
        (
            string caption, 
            string message, 
            Collection<ChoiceDescription> choices, 
            int defaultChoice
        )
        {
            if (PromptForChoiceCallback == null) { throw new NotImplementedException(); }
            return PromptForChoiceCallback(caption, message, choices, defaultChoice);
        }

        /// <summary>
        /// When overridden in a derived class, prompts the user for credentials with a
        /// specified prompt window caption, prompt message, user name, and target name.
        /// </summary>
        /// <param name="caption">The caption for the message window.</param>
        /// <param name="message">The text of the message.</param>
        /// <param name="userName">
        /// The user name whose credential is to be prompted for. If this parameter set to
        /// <c>null</c> or an empty string, the function prompts for the user name first.
        /// </param>
        /// <param name="targetName">
        /// The name of the target for which the credential is collected.
        /// </param>
        /// <returns>
        /// A <see cref="PSCredential" /> object that contains the credentials for the
        /// target.
        /// </returns>
        public override PSCredential PromptForCredential
        (
            string caption,
            string message,
            string userName,
            string targetName
        )
        {
            return PromptForCredential
            (
                caption,
                message,
                userName,
                targetName,
                PSCredentialTypes.Default,
                PSCredentialUIOptions.Default
            );
        }

        /// <summary>
        /// When overridden in a derived class, prompts the user for credentials by using a 
        /// specified prompt window caption, prompt message, user name and target name, credential
        /// types allowed to be returned, and UI behavior options.
        /// </summary>
        /// <param name="caption">The caption for the message window.</param>
        /// <param name="message">The text of the message.</param>
        /// <param name="userName">
        /// The user name whose credential is to be prompted for. If this parameter set to 
        /// <c>null</c> or an empty string, the function prompts for the user name first.
        /// </param>
        /// <param name="targetName">
        /// The name of the target for which the credential is collected.
        /// </param>
        /// <param name="allowedCredentialTypes">
        /// A bitwise combination of the <see cref="PSCredentialTypes" /> enumeration values that 
        /// identify the types of credentials that can be returned.
        /// </param>
        /// <param name="options">
        /// A bitwise combination of the <see cref="PSCredentialUIOptions" /> enumeration values 
        /// that identify the UI behavior when it gathers the credentials.
        /// </param>
        /// <returns>
        /// A <see cref="PSCredential" /> object that contains the credentials for the target.
        /// </returns>
        public override PSCredential PromptForCredential
        (
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options
        )
        {
            if (PromptForCredentialsCallback == null) { throw new NotImplementedException(); }
            return PromptForCredentialsCallback
            (
                caption,
                message,
                userName,
                targetName,
                allowedCredentialTypes,
                options
            );
        }

        /// <summary>
        /// When overridden in a derived class, reads characters that are entered by the
        /// user until a newline (carriage return) character is encountered.
        /// </summary>
        /// <returns>The characters that are entered by the user.</returns>
        public override string ReadLine()
        {
            if (ReadLineCallback == null) { throw new NotImplementedException(); }
            return ReadLineCallback();
        }

        /// <summary>
        /// When overridden in a derived class, reads characters entered by the user until
        /// a newline (carriage return) character is encountered and returns the characters
        /// as a secure string.
        /// </summary>
        /// <returns>
        /// A SecureString object that contains the characters that are entered by the
        /// user.
        /// </returns>
        public override SecureString ReadLineAsSecureString()
        {
            return ReadLineCallback().ToSecureString();
        }

        /// <summary>
        /// When overridden in a derived class, writes characters to the output display of
        /// the host.
        /// </summary>
        /// <param name="value">The value.</param>
        public override void Write(string value)
        {
            WriteToLog(PSOutputStream.Default, null, null, value);
        }

        /// <summary>
        /// When overridden in a derived class, writes characters to the output display of
        /// the host with possible foreground and background colors.
        /// </summary>
        /// <param name="foregroundColor">The foreground color of the display.</param>
        /// <param name="backgroundColor">The background color of the display.</param>
        /// <param name="value">The characters to be written.</param>
        public override void Write
        (
            ConsoleColor foregroundColor, 
            ConsoleColor backgroundColor, 
            string value
        )
        {
            WriteToLog(PSOutputStream.Default, foregroundColor, backgroundColor, value);
        }

        /// <summary>
        /// When overridden in a derived class, displays a debug message to the user.
        /// </summary>
        /// <param name="message">Debug message to be displayed.</param>
        public override void WriteDebugLine(string message)
        {
            WriteToLog(PSOutputStream.Debug, null, null, (message ?? string.Empty) + Environment.NewLine);
        }

        /// <summary>
        /// When overridden in a derived class, writes a line to the error display of the
        /// host.
        /// </summary>
        /// <param name="message">Error message to be displayed.</param>
        public override void WriteErrorLine(string message)
        {
            WriteToLog(PSOutputStream.Error, null, null, (message ?? string.Empty) + Environment.NewLine);
        }

        /// <summary>
        /// When overridden in a derived class, writes a line of characters to the output
        /// display of the host and appends a newline (carriage return) character.
        /// </summary>
        /// <param name="value">The line of characters to be written.</param>
        public override void WriteLine(string value)
        {
            WriteToLog(PSOutputStream.Default, null, null, (value ?? string.Empty) + Environment.NewLine);
        }

        /// <summary>
        /// When overridden in a derived class, writes a progress report to be displayed to
        /// the user.
        /// </summary>
        /// <param name="sourceId">A unique identifier of the source of the record.</param>
        /// <param name="record">
        /// A ProgressRecord object that contains the progress record to be displayed.
        /// </param>
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            Debug.WriteLine($"[PROGRESS(Id: {sourceId})] " + PSUtils.Stringify(record));
            WriteProgressCallback?.Invoke(sourceId, record);
        }

        /// <summary>
        /// When overridden in a derived class, writes a verbose line to be displayed to
        /// the user.
        /// </summary>
        /// <param name="message">The verbose message to be displayed.</param>
        public override void WriteVerboseLine(string message)
        {
            WriteToLog(PSOutputStream.Verbose, null, null, (message ?? string.Empty) + Environment.NewLine);
        }

        /// <summary>
        /// When overridden in a derived class, writes a warning line to be displayed to
        /// the user.
        /// </summary>
        /// <param name="message">The warning message to be displayed.</param>
        public override void WriteWarningLine(string message)
        {
            WriteToLog(PSOutputStream.Warning, null, null, (message ?? string.Empty) + Environment.NewLine);
        }
    }
}
