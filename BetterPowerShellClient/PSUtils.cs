using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

[assembly: InternalsVisibleTo("PowerShellClient.Tests")]

namespace PowerShellClient
{
    /// <summary>
    /// Static Utility Methods for working with PowerShell.
    /// </summary>
    public static class PSUtils
    {
        /// <summary>
        /// Converts a random object to a string that we can write out to the console / log file.
        /// </summary>
        /// <param name="input">The input object.</param>
        public static string Stringify(object input)
        {
            if (input == null) { return "[null]"; }

            try
            {
                var jss = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                };
                jss.Converters.Add(new StringEnumConverter());
                jss.Error += (s, e) => { /* do nothing */ };

                return JsonConvert.SerializeObject(input, jss);
            }
            catch
            {
                try
                {
                    return input.ToString();
                }
                catch
                {
                    return $"[{input.GetType()}]";
                }
            }
        }

        /// <summary>
        /// Escapes a string into the raw name of a variable.
        /// </summary>
        /// <param name="variableName">
        /// The name of the variable (without the dollar sign or escape characters).
        /// </param>
        /// <returns>
        /// <para>An escaped proper variable name (including the dollar sign and curly braces).</para>
        /// <para>See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_variables</para>
        /// </returns>
        public static string EscapeVariableName(string variableName)
        {
            variableName = EscapePowerShellPartial(variableName, false, 0);
            return variableName == null
                ? "$null"
                : "${" + variableName + "}";
        }

        /// <summary>
        /// Escapes a raw string value into a formatted and escaped string literal that can be used
        /// in a raw PowerShell script.
        /// </summary>
        /// <param name="rawValue">The raw string value to be escaped.</param>
        /// <returns>
        /// A formatted and escaped string literal that can be inserted into a powershell script
        /// verbatim, includes proper quotes and escape characters.
        /// </returns>
        /// <remarks>See: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_special_characters</remarks>
        public static string EscapeString(string rawValue) => EscapeString(rawValue, 0);

        internal static string EscapeString(string rawValue, int bracketEscapeTickCount)
        {
            rawValue = EscapePowerShellPartial(rawValue, true, bracketEscapeTickCount);

            return rawValue == null
                ? "$null"
                : "\"" + rawValue + "\"";
        }

        private static string EscapePowerShellPartial(string input, bool escapeDoubleQuotes, int bracketEscapeTickCount)
        {
            StringBuilder sb = null;

            if (input != null)
            {
                sb = new StringBuilder();
                foreach (char c in input)
                {
                    if (c == ((char)0)) { sb.Append("`0"); }
                    else if (c == ((char)7)) { sb.Append("`a"); }
                    else if (c == ((char)8)) { sb.Append("`b"); }
                    else if (c == ((char)9)) { sb.Append("`t"); }
                    else if (c == ((char)10)) { sb.Append("`n"); }
                    else if (c == ((char)11)) { sb.Append("`v"); }
                    else if (c == ((char)12)) { sb.Append("`f"); }
                    else if (c == ((char)13)) { sb.Append("`r"); }
                    else if (escapeDoubleQuotes && c == '"') { sb.Append("\"\""); }
                    else if (c == '[') { sb.Append("[".PadLeft(bracketEscapeTickCount + 1, '`')); }
                    else if (c == ']') { sb.Append("]".PadLeft(bracketEscapeTickCount + 1, '`')); }
                    else
                    {
                        if (c == '`' || c == '{' || c == '}' || c == '$') { sb.Append('`'); }
                        sb.Append(c);
                    }
                }
            }

            return sb?.ToString();
        }

        /// <summary>
        /// Creates a PowerShell <see cref="Command" /> that can be Invoked by an object that
        /// implements <see cref="IPSClient" />.
        /// </summary>
        /// <param name="cmdlet">The name of the cmdlet to invoke.</param>
        /// <param name="parameters">An anonymous object containing the parameters for the cmdlet.</param>
        /// <param name="switches">A collection of switches for the cmdlet.</param>
        public static Command CreateCommand
        (
            string cmdlet,
            object parameters = null,
            params string[] switches
        )
        {
            var cmd = new Command(cmdlet, false);

            foreach (var param in ConvertToDictionary(parameters))
            {
                cmd.Parameters.Add(param.Key, param.Value);
            }

            if (switches?.Any() == true)
            {
                foreach (var @switch in switches)
                {
                    if (!string.IsNullOrWhiteSpace(@switch))
                    {
                        cmd.Parameters.Add(@switch);
                    }
                }
            }

            return cmd;
        }

        /// <summary>
        /// Uses reflection to convert anonymous objects, KeyValue pairs, or even regular objects
        /// into dictionaries for easy inspection.
        /// </summary>
        /// <param name="input">The object to be converted into a dictionary.</param>
        /// <returns>
        /// A case-insensitive dictionary which contains the properties and values of the object
        /// that was passed in.
        /// </returns>
        public static Dictionary<string, object> ConvertToDictionary(object input)
        {
            var result = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            if (input != null)
            {
                bool done = false;

                try
                {
                    if (input is IEnumerable col)
                    {
                        foreach (dynamic c in col)
                        {
                            if (c is null) { continue; }

                            string name = null;
                            try
                            {
                                name = c.Key;
                            }
                            catch
                            {
                                name = c.Name;
                            }

                            result[name] = c.Value;
                            done = true;
                        }
                    }
                }
                catch
                {
                    done = false;
                }

                if (!done)
                {
                    var props = input.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        var getter = prop.GetAccessors(false).Where
                        (
                            mi => mi.ReturnType != typeof(void)
                            && mi.GetParameters().Length == 0
                        )
                        .FirstOrDefault();

                        result[prop.Name] = getter.Invoke(input, Array.Empty<object>());
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Configures a connected PowerShell session to write its output to the Console.
        /// </summary>
        /// <param name="ips">The connected <see cref="IPSClient" /> instance to configure.</param>
        /// <returns><c>true</c> if configuration is successful; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// The PowerShell session must be open and connected, or this method will not succeed;
        /// additionally if you close and re-open it, it will need to be configured again.
        /// </remarks>
        public static bool ConfigureNonInteractiveConsoleHost(this IPSClient ips)
        {
            bool success = false;
            if (ips?.HostUI != null)
            {
                ips.HostUI.PromptCallback = (caption, message, descriptions) =>
                {
                    throw new NotSupportedException
                    (
                        "PowerShell is prompting for user input! " + Environment.NewLine
                        + JsonConvert.SerializeObject
                        (
                            new
                            {
                                Caption = caption,
                                Message = message,
                                Descriptions = descriptions
                            },
                            Formatting.Indented
                        )
                    );
                };

                ips.HostUI.PromptForChoiceCallback = (caption, message, choices, defaultChoice) =>
                {
                    WithConsoleLock(() =>
                    {
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine
                        (
                            "[PowerShell-PromptForChoice] "
                        );
                        Console.WriteLine("Accepting DefaultChoice: " + defaultChoice);
                        Console.ResetColor();
                    });

                    return defaultChoice;
                };

                ips.HostUI.PromptForCredentialsCallback = (caption, message, userName, targetName, allowedCredentialTypes, options) =>
                {
                    throw new NotSupportedException
                    (
                        "PowerShell Prompting for Credentials!" + Environment.NewLine
                        + JsonConvert.SerializeObject
                        (
                            new
                            {
                                Caption = caption,
                                Message = message,
                                UserName = userName,
                                TargetName = targetName,
                                AllowedCredentialTypes = allowedCredentialTypes,
                                Options = options
                            },
                            Formatting.Indented
                        )
                    );
                };

                ips.HostUI.ReadLineCallback = () =>
                {
                    throw new NotSupportedException("PowerShell trying to ReadLine!");
                };

                ips.HostUI.WriteCallback = (level, foregroundColor, backgroundColor, message) =>
                {
                    WithConsoleLock(() =>
                    {
                        Console.ResetColor();

                        if (!foregroundColor.HasValue && !backgroundColor.HasValue)
                        {
                            // default colors:
                            if (level == PSOutputStream.Debug) { foregroundColor = ConsoleColor.Cyan; }
                            else if (level == PSOutputStream.Verbose) { foregroundColor = ConsoleColor.Green; }
                            else if (level == PSOutputStream.Info) { foregroundColor = ConsoleColor.White; }
                            else if (level == PSOutputStream.Warning) { foregroundColor = ConsoleColor.Yellow; }
                            else if (level == PSOutputStream.Error)
                            {
                                foregroundColor = ConsoleColor.White;
                                backgroundColor = ConsoleColor.DarkRed;
                            }
                        }

                        if (foregroundColor.HasValue) { Console.ForegroundColor = foregroundColor.Value; }
                        if (backgroundColor.HasValue) { Console.BackgroundColor = backgroundColor.Value; }

                        Console.WriteLine($"[PowerShell-{level}] {(message ?? string.Empty).Trim()}");
                        Console.ResetColor();
                    });
                };

                ips.HostUI.WriteProgressCallback = (sourceId, record) =>
                {
                    if (record?.Activity?.EqualsIgnoreCase("Preparing modules for first use.") != true)
                    {
                        WithConsoleLock(() =>
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.BackgroundColor = ConsoleColor.Black;

                            Console.Write("[PowerShell-Progress");
                            if (sourceId != 0)
                            {
                                Console.Write($"({sourceId})");
                            }
                            Console.WriteLine("]");
                            Console.WriteLine(JsonConvert.SerializeObject(record, Formatting.Indented));
                            Console.WriteLine("[/PowerShell-Progress]");
                            Console.ResetColor();
                        });
                    }
                };

                success = true;
            }

            return success;
        }

        /// <summary>
        /// Configures a connected PowerShell session to swallow any output.
        /// </summary>
        /// <param name="ips">The connected <see cref="IPSClient" /> instance to configure.</param>
        /// <returns><c>true</c> if configuration is successful; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// The PowerShell session must be open and connected, or this method will not succeed;
        /// additionally if you close and re-open it, it will need to be configured again.
        /// </remarks>
        public static bool ConfigureNonInteractiveSilentHost(this IPSClient ips)
        {
            bool success = false;
            if (ips?.HostUI != null)
            {
                ips.HostUI.PromptCallback = (caption, message, descriptions) =>
                {
                    throw new NotSupportedException
                    (
                        "PowerShell is prompting for user input! " + Environment.NewLine
                        + JsonConvert.SerializeObject
                        (
                            new
                            {
                                Caption = caption,
                                Message = message,
                                Descriptions = descriptions
                            },
                            Formatting.Indented
                        )
                    );
                };

                ips.HostUI.PromptForChoiceCallback = (caption, message, choices, defaultChoice) =>
                {
                    return defaultChoice;
                };

                ips.HostUI.PromptForCredentialsCallback = (caption, message, userName, targetName, allowedCredentialTypes, options) =>
                {
                    throw new NotSupportedException
                    (
                        "PowerShell Prompting for Credentials!" + Environment.NewLine
                        + JsonConvert.SerializeObject
                        (
                            new
                            {
                                Caption = caption,
                                Message = message,
                                UserName = userName,
                                TargetName = targetName,
                                AllowedCredentialTypes = allowedCredentialTypes,
                                Options = options
                            },
                            Formatting.Indented
                        )
                    );
                };

                ips.HostUI.ReadLineCallback = () =>
                {
                    throw new NotSupportedException("PowerShell trying to ReadLine!");
                };

                ips.HostUI.WriteCallback = (level, foregroundColor, backgroundColor, message) =>
                { /* do nothing */ };

                ips.HostUI.WriteProgressCallback = (sourceId, record) =>
                { /* do nothing */ };

                success = true;
            }

            return success;
        }

        private static void WithConsoleLock(Action action)
        {
            bool locked = false;
            try
            {
                locked = Monitor.TryEnter(Console.Out, TimeSpan.FromSeconds(10));
                action?.Invoke();
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(Console.Out);
                }
            }
        }

        /// <summary>
        /// Converts the object into a single <see cref="Exception" />.
        /// </summary>
        /// <param name="input">The input object.</param>
        /// <returns>An exception representatitve of the passed in object.</returns>
        public static Exception GetSingleException(object input)
        {
            if (input is Exception ex) { return ex?.GetBaseException() ?? ex; }
            if (input is IEnumerable<Exception> exs)
            {
                return new AggregateException(exs.Select(x => x?.GetBaseException() ?? x));
            }

            var msg = GetString(input);
            if (string.IsNullOrEmpty(msg) || msg == "[null]") { msg = "Unspecified Error"; }

            return new Exception(msg);
        }

        /// <summary>
        /// Converts the object into an array of <see cref="Exception" /> s; flattening any <see
        /// cref="AggregateException" /> s.
        /// </summary>
        /// <param name="input">The input object.</param>
        /// <returns>An exception representatitve of the passed in object.</returns>
        public static IEnumerable<Exception> GetAllExceptions(object input)
        {
            if (input is null) { yield break; }

            if (input is IEnumerable ie && !(input is string))
            {
                foreach (var child in ie)
                {
                    foreach (var exc in GetAllExceptions(child))
                    {
                        yield return exc;
                    }
                }
                yield break;
            }

            if (!(input is AggregateException) && input is Exception ex)
            {
                input = ex.GetBaseException() ?? ex;
            }

            if (input is AggregateException aex)
            {
                if (aex.InnerExceptions.Any())
                {
                    foreach (var exc in aex.InnerExceptions)
                    {
                        foreach (var exc2 in GetAllExceptions(exc))
                        {
                            yield return exc2;
                        }
                    }
                }
                else
                {
                    yield return aex?.GetBaseException() ?? aex;
                }
            }
            else if (input is Exception ex2)
            {
                yield return ex2;
            }
            else
            {
                foreach (var exx in GetAllExceptions(GetSingleException(input)))
                {
                    yield return exx;
                }
            }
        }

        internal static string GetString(object o)
        {
            if (o is string s) { return s; }

            if (o is IEnumerable en)
            {
                var sb = new StringBuilder();
                foreach (object c in en)
                {
                    sb.AppendLine(GetString(c));
                }
                return sb.ToString().Trim();
            }

            return o?.ToString() ?? "[null]";
        }
    }
}