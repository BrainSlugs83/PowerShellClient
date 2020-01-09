using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PowerShellClient
{
    /// <summary>
    /// Utilities for accessing the File System on a Remote Machine via PowerShell.
    /// </summary>
    public class PSFileSystem
    {
        /// <summary>
        /// Gets the PowerShell Client instance.
        /// </summary>
        public IPSClient PSClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFileSystem" /> class.
        /// </summary>
        /// <param name="psClient">
        /// The PowerShell Client instance to perform the commands against.
        /// </param>
        public PSFileSystem(PSClient psClient)
        {
            this.PSClient = psClient
                ?? throw new ArgumentNullException(nameof(psClient));
        }

        /// <summary>
        /// Determines if the specified path exists on the remote machine.
        /// </summary>
        /// <param name="path">The path to check for.</param>
        /// <param name="checkFiles">
        /// <c>true</c> if you want to check for files that exist.
        /// </param>
        /// <param name="checkFolders">
        /// <c>true</c> if you want to check for folders that exist.
        /// </param>
        public bool PathExists(string path, bool checkFiles = true, bool checkFolders = true)
        {
            if (!checkFiles && !checkFolders) { return false; } // how could that exist?

            string testPathType = "Any";
            if (!checkFiles) { testPathType = "Container"; }
            if (!checkFolders) { testPathType = "Leaf"; }

            var cmd = $"Test-Path -LiteralPath {PSUtils.EscapeString(path)} -PathType {testPathType}";
            var result = PSClient.InvokeScript<bool>(cmd).Single();
            return result;
        }

        /// <summary>
        /// Determines if the specified path exists on the remote machine asynchronously.
        /// </summary>
        /// <param name="path">The path to check for.</param>
        /// <param name="checkFiles">
        /// <c>true</c> if you want to check for files that exist.
        /// </param>
        /// <param name="checkFolders">
        /// <c>true</c> if you want to check for folders that exist.
        /// </param>
        public async Task<bool> PathExistsAsync(string path, bool checkFiles = true, bool checkFolders = true)
        {
            return await Task.Run(() => PathExists(path, checkFiles, checkFolders));
        }

        /// <summary>
        /// Ensures that the given directory exists on the remote machine.
        /// </summary>
        /// <param name="directory">The directory to check for.</param>
        public void EnsureDirectory(string directory)
        {
            if (!PathExists(directory, false, true))
            {
                // Specify return type to prevent console output.
                PSClient.InvokeCommand<PSObject>("md", new { Path = directory });
            }
        }

        /// <summary>
        /// Ensures that the given directory exists on the remote machine asynchronously.
        /// </summary>
        /// <param name="directory">The directory to check for.</param>
        public async Task EnsureDirectoryAsync(string directory)
        {
            await Task.Run(() => EnsureDirectory(directory));
        }

        /// <summary>
        /// Gets raw bytes of a file on the remote file system.
        /// </summary>
        /// <param name="remoteFilePath">The path to the file.</param>
        public byte[] GetFileBytes(string remoteFilePath)
        {
            byte[] results = null;

            if (PathExists(remoteFilePath, true, false))
            {
                remoteFilePath = PSUtils.EscapeString(remoteFilePath);
                var script = $@"[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes({remoteFilePath}))";

                var data = PSClient.InvokeScript<string>(script).Single();
                results = Convert.FromBase64String(data);
            }

            return results;
        }

        /// <summary>
        /// Gets raw bytes of a file on the remote file system asynchronously.
        /// </summary>
        /// <param name="remoteFilePath">The path to the file.</param>
        public async Task<byte[]> GetFileBytesAsync(string remoteFilePath)
        {
            return await Task.Run(() => GetFileBytes(remoteFilePath));
        }

        /// <summary>
        /// Gets text from a file on the remote file system.
        /// </summary>
        /// <param name="remoteFilePath">The path to the file.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times",
            Justification = "Okay to dispose nested streams this way.")]
        public string GetFileText(string remoteFilePath)
        {
            string result = null;
            var bytes = GetFileBytes(remoteFilePath);
            if (bytes != null)
            {
                using (var ms = new MemoryStream(bytes))
                using (var sr = new StreamReader(ms, true))
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    result = sr.ReadToEnd();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets text from a file on the remote file system asynchronously.
        /// </summary>
        /// <param name="remoteFilePath">The path to the file.</param>
        public async Task<string> GetFileTextAsync(string remoteFilePath)
        {
            return await Task.Run(() => GetFileText(remoteFilePath));
        }

        /// <summary>
        /// Gets the size of a remote file.
        /// </summary>
        /// <param name="remoteFilePath">The remote file path.</param>
        public long GetFileSize(string remoteFilePath)
        {
            var fileResult = PSClient.InvokeCommand<PSObject>
            (
                "Get-ChildItem",
                new
                {
                    PSPath = remoteFilePath
                }
            );

            return (long)fileResult.First().Properties["Length"].Value;
        }

        /// <summary>
        /// Gets the size of a remote file asynchronously.
        /// </summary>
        /// <param name="remoteFilePath">The remote file path.</param>
        public async Task<long> GetFileSizeAsync(string remoteFilePath)
        {
            return await Task.Run(() => GetFileSize(remoteFilePath));
        }

        /// <summary>
        /// Gets the hash of a remote file.
        /// </summary>
        /// <param name="remoteFilePath">The remote file path.</param>
        /// <param name="algorithm">The hashing algorithm to use (default is MD5).</param>
        public string GetFileHash(string remoteFilePath, string algorithm = null)
        {
            algorithm = (algorithm ?? string.Empty).Trim();
            bool appendFileLength = false;
            if (algorithm.EndsWith("+LENGTH", StringComparison.InvariantCultureIgnoreCase))
            {
                appendFileLength = true;
                algorithm = algorithm.Substring(0, algorithm.Length - 7).Trim();
            }

            if (string.IsNullOrWhiteSpace(algorithm))
            {
                algorithm = "MD5";
            }

            var hashResult = PSClient.InvokeScript<PSObject>
            (
                $"Get-FileHash -Path {PSUtils.EscapeString(remoteFilePath, 4)} -Algorithm MD5"
            );

            var row = hashResult.FirstOrDefault();
            string result = null;
            if (row != null)
            {
                result = row.Properties.Where
                (
                    x => x.Name.StartsWith("Hash", StringComparison.InvariantCultureIgnoreCase)
                )
                .Select
                (
                    x => x?.Value as string
                )
                .Where
                (
                    x => !string.IsNullOrWhiteSpace(x)
                )
                .FirstOrDefault();
            }

            if (appendFileLength)
            {
                var length = GetFileSize(remoteFilePath);
                result = (result ?? string.Empty) + "::" + length;
            }

            return result;
        }

        /// <summary>
        /// Gets the hash of a remote file asynchronously.
        /// </summary>
        /// <param name="remoteFilePath">The remote file path.</param>
        /// <param name="algorithm">The hashing algorithm to use (default is MD5).</param>
        public async Task<string> GetFileHashAsync(string remoteFilePath, string algorithm = null)
        {
            return await Task.Run(() => GetFileHash(remoteFilePath, algorithm));
        }

        private bool VerifyFileContents(string path, byte[] expectedContents)
        {
            var result = false;

            // Does the file already exist?
            if (PathExists(path, true, false))
            {
                // Is it the same size?
                long measuredSize = GetFileSize(path);
                if (measuredSize == expectedContents.LongLength)
                {
                    // Is it the same MD5 hash?
                    using (var md5 = MD5.Create())
                    {
                        var localHash = BitConverter.ToString(md5.ComputeHash(expectedContents))
                            .Replace("-", string.Empty).ToUpperInvariant();

                        var remoteHash = GetFileHash(path);

                        if (localHash == remoteHash)
                        {
                            // File already exists, and contains correct contents!
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Puts a file on the remote machine.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="contents">The raw binary contents of the file.</param>
        /// <param name="unblock">
        /// <c>true</c> to unblock the file after it's created; otherwise <c>false</c>.
        /// </param>
        public void PutFile(string path, byte[] contents, bool unblock = true)
        {
            var escapedPath = PSUtils.EscapeString(path);

            if (!VerifyFileContents(path, contents))
            {
                if (!PathExists(path, true, false))
                {
                    EnsureDirectory(Path.GetDirectoryName(path));
                }

                const long maxBufferSize = 1024 * 1024;
                if (contents.LongLength < maxBufferSize)
                {
                    var rawData = "\"" + Convert.ToBase64String(contents) + "\"";

                    PSClient.InvokeScript
                    (
                        @"[System.IO.File]::WriteAllBytes" +
                        "(" +
                            escapedPath + ", " +
                            "[System.Convert]::FromBase64String(" + rawData + ")" +
                        ")"
                    );
                }
                else
                {
                    var files = new List<string>();
                    var buffer = new byte[maxBufferSize];
                    int fileNumber = 0;

                    using (var ms = new MemoryStream(contents))
                    {
                        while (ms.Position < ms.Length)
                        {
                            var oldPos = ms.Position;
                            int size = ms.Read(buffer, 0, buffer.Length);
                            if (size <= 0) { break; }
                            if (size < buffer.Length)
                            {
                                // do over with resized buffer.
                                buffer = new byte[size];
                                ms.Position = oldPos;
                                continue;
                            }

                            var rawData = "\"" + Convert.ToBase64String(buffer) + "\"";
                            var rawPath = path + "_" + fileNumber;
                            var outputFile = PSUtils.EscapeString(rawPath);
                            PSClient.InvokeScript
                            (
                                @"[System.IO.File]::WriteAllBytes" +
                                "(" +
                                     outputFile + ", " +
                                    "[System.Convert]::FromBase64String(" + rawData + ")" +
                                ")"
                            );
                            files.Add(rawPath);
                            fileNumber++;

                            if (buffer.Length < maxBufferSize)
                            {
                                buffer = new byte[maxBufferSize];
                            }
                        }
                    }

                    var sb = new StringBuilder();
                    sb.Append("gc -LiteralPath ");
                    sb.Append(string.Join(",", files.Select(x => PSUtils.EscapeString(x, 0))));
                    sb.Append(" -Encoding Byte -Read 512 | sc -LiteralPath ");
                    sb.Append(escapedPath);
                    sb.Append(" -Encoding Byte");
                    var cmd = sb.ToString();
                    PSClient.InvokeScript(cmd);

                    foreach (var file in files)
                    {
                        DeleteFile(file);
                    }

                    if (!VerifyFileContents(path, contents))
                    {
                        throw new InvalidOperationException("PUT FILE FAILED.");
                    }
                }
            }

            if (unblock)
            {
                PSClient.InvokeCommand("Unblock-File", new { Path = path });
            }
        }

        /// <summary>
        /// Puts a file on the remote machine asynchronously.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="contents">The raw binary contents of the file.</param>
        /// <param name="unblock">
        /// <c>true</c> to unblock the file after it's created; otherwise <c>false</c>.
        /// </param>
        public async Task PutFileAsync(string path, byte[] contents, bool unblock = true)
        {
            await Task.Run(() => PutFile(path, contents, unblock));
        }

        /// <summary>
        /// Unzips the supplied zip file to the specified remote file system path.
        /// </summary>
        /// <param name="zipFile">The zip file.</param>
        /// <param name="outputPath">
        /// The path on the remote file system where the zip file should be unzipped.
        /// </param>
        /// <param name="token">A cancellation token to abort the unzipping operation.</param>
        public void UnzipTo(ZipArchive zipFile, string outputPath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Normalize the output Path to use correct slashes, and always contain a
            // trailing slash.
            outputPath = outputPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            int id = Math.Abs(Guid.NewGuid().GetHashCode());

            var startedAt = DateTime.UtcNow;

            double extractedBytes = 0;
            double totalBytes = zipFile.Entries.Select(x => Math.Max(x.Length, 1)).Sum();

            foreach (var entry in zipFile.Entries)
            {
                token.ThrowIfCancellationRequested();

                double percentComplete = (extractedBytes * 100d) / totalBytes;
                double elapsedSeconds = (DateTime.UtcNow - startedAt).TotalSeconds;
                double secondsRemaining = (elapsedSeconds / (double)percentComplete)
                    * (100d - (double)percentComplete);

                var outputFileName = entry.FullName.Replace
                (
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar
                )
                .TrimStart(Path.DirectorySeparatorChar);

                outputFileName = Path.Combine(outputPath, outputFileName);

                PSClient.InvokeCommand
                (
                    "Write-Progress",
                    new
                    {
                        Id = id,
                        Activity = $"Unzipping to '{outputPath}' on '{PSClient.ConnectionInfo}'.",
                        Status = entry.FullName,
                        PercentComplete = (int)Math.Round(percentComplete),
                        SecondsRemaining = (int)Math.Round(secondsRemaining),
                    }
                );

                if (outputFileName.EndsWith(Path.DirectorySeparatorChar.ToString()) && entry.Length == 0)
                {
                    EnsureDirectory(outputFileName);
                    extractedBytes++;
                }
                else
                {
                    using (var ms = new MemoryStream())
                    using (var entryStream = entry.Open())
                    {
                        entryStream.CopyTo(ms);
                        ms.Flush();

                        PutFile(outputFileName, ms.ToArray(), true);
                    }

                    extractedBytes += Math.Max(entry.Length, 1);
                }
            }

            PSClient.InvokeCommand
            (
                "Write-Progress",
                new
                {
                    Id = id,
                    Activity = $"Unzipping to '{outputPath}' on '{PSClient.ConnectionInfo}'.",
                    PercentComplete = 100,
                    SecondsRemaining = 0,
                },
                "Completed"
            );
        }

        /// <summary>
        /// Unzips the supplied zip file to the specified remote file system path
        /// asynchronously.
        /// </summary>
        /// <param name="zipFile">The zip file.</param>
        /// <param name="outputPath">
        /// The path on the remote file system where the zip file should be unzipped.
        /// </param>
        /// <param name="token">A cancellation token to abort the unzipping operation.</param>
        public async Task UnzipToAsync(ZipArchive zipFile, string outputPath, CancellationToken token)
        {
            await Task.Run(() => UnzipTo(zipFile, outputPath, token));
        }

        /// <summary>
        /// Unzips the supplied zip file to the specified remote file system path.
        /// </summary>
        /// <param name="zipFile">The zip file.</param>
        /// <param name="outputPath">
        /// The path on the remote file system where the zip file should be unzipped.
        /// </param>
        public void UnzipTo(ZipArchive zipFile, string outputPath)
        {
            UnzipTo(zipFile, outputPath, CancellationToken.None);
        }

        /// <summary>
        /// Unzips the supplied zip file to the specified remote file system path
        /// asynchronously.
        /// </summary>
        /// <param name="zipFile">The zip file.</param>
        /// <param name="outputPath">
        /// The path on the remote file system where the zip file should be unzipped.
        /// </param>
        public async Task UnzipToAsync(ZipArchive zipFile, string outputPath)
        {
            await UnzipToAsync(zipFile, outputPath, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a file from the connected machine.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public void DeleteFile(string path)
        {
            if (PathExists(path, true, false))
            {
                var escapedPath = PSUtils.EscapeString(path, 4);
                PSClient.InvokeScript($"del -Path {escapedPath} -Force");
            }
        }

        /// <summary>
        /// Deletes a file from the connected machine asynchronously.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public async Task DeleteFileAsync(string path)
        {
            await Task.Run(() => DeleteFile(path));
        }

        /// <summary>
        /// Deletes a folder (recursively) from the connected machine.
        /// </summary>
        /// <param name="path">The path to the folder.</param>
        public void DeleteFolderRecursively(string path)
        {
            if (PathExists(path, false, true))
            {
                var escapedPath = PSUtils.EscapeString(path);
                PSClient.InvokeScript($"rm {escapedPath} -Recurse -Force");
            }
        }

        /// <summary>
        /// Deletes a folder (recursively) from the connected machine asynchronously.
        /// </summary>
        /// <param name="path">The path to the folder.</param>
        public async Task DeleteFolderRecursivelyAsync(string path)
        {
            await Task.Run(() => DeleteFolderRecursively(path));
        }
    }
}
