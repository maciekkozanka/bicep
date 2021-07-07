// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Bicep.Core.Modules
{
    public class OrasClient
    {
        private readonly string artifactCachePath;

        public OrasClient(string artifactCachePath)
        {
            this.artifactCachePath = artifactCachePath;
        }

        public bool Pull(OciArtifactModuleReference reference, [NotNullWhen(false)] out string? errorMessage)
        {
            string localArtifactPath = GetLocalPackageDirectory(reference);

            // ensure that the directory exists
            Directory.CreateDirectory(localArtifactPath);

            using var process = new Process
            {
                // TODO: What about spaces? Do we need escaping?
                StartInfo = new("oras", $"pull {reference.ArtifactId} -a")
                {
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    LoadUserProfile = true,

                    // LSP communicates over StdIn/StdOut, so we must not let the streams leak back to the parent process
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,

                    // ORAS uses the CWD to download artifacts
                    WorkingDirectory = localArtifactPath
                }
            };

            // drop StdOut from ORAS
            process.OutputDataReceived += (sender, e) => { };

            var error = new StringBuilder();
            process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);

            try
            {
                process.Start();
            }
            catch (Exception exception)
            {
                errorMessage = $"Unable to invoke \"oras pull\". {exception.Message}";
                return false;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            if(process.ExitCode == 0)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = error.ToString().Trim();
            return false;
        }

        public string GetLocalPackageDirectory(OciArtifactModuleReference reference)
        {
            var baseDirectories = new[]
            {
                this.artifactCachePath,
                reference.Registry
            };

            // TODO: Directory convention problematic. /foo/bar:baz and /foo:bar will share directories
            var directories = baseDirectories
                .Concat(reference.Repository.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Append(reference.Tag)
                .ToArray();

            return Path.Combine(directories);
        }

        public string GetLocalPackageEntryPointPath(OciArtifactModuleReference reference) => Path.Combine(this.GetLocalPackageDirectory(reference), "main.bicep");
    }
}
