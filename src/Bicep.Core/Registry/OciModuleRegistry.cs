// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bicep.Core.Registry
{
    public class OciModuleRegistry : IModuleRegistry
    {
        private readonly IFileResolver fileResolver;

        private readonly OrasClient orasClient;

        public OciModuleRegistry(IFileResolver fileResolver)
        {
            this.fileResolver = fileResolver;
            this.orasClient = new OrasClient(GetArtifactCachePath());
        }

        public ModuleReference? TryParseModuleReference(string reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder) => OciArtifactModuleReference.TryParse(reference, out failureBuilder);

        public bool IsModuleRestoreRequired(ModuleReference reference)
        {
            // TODO: This may need to be updated to account for concurrent processes updating the local cache
            var typed = ConvertReference(reference);

            // if module is missing, it requires init
            return !this.fileResolver.Exists(GetEntryPointUri(typed));
        }

        public Uri? TryGetLocalModuleEntryPointPath(Uri parentModuleUri, ModuleReference reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var typed = ConvertReference(reference);
            failureBuilder = null;
            return GetEntryPointUri(typed);
        }

        public IDictionary<ModuleReference, DiagnosticBuilder.ErrorBuilderDelegate> RestoreModules(IEnumerable<ModuleReference> references)
        {
            var statuses = new Dictionary<ModuleReference, DiagnosticBuilder.ErrorBuilderDelegate>();
            foreach(var reference in references.OfType<OciArtifactModuleReference>())
            {
                if(!this.orasClient.Pull(reference, out var errorMessage))
                {
                    statuses.Add(reference, x => x.ModuleRestoreFailedWithMessage(reference.FullyQualifiedReference, errorMessage));
                }
            }

            return statuses;
        }
        
        private static string GetArtifactCachePath()
        {
            // TODO: Will NOT work if user profile is not loaded on Windows! (Az functions load exes like that)
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(basePath, ".bicep", "artifacts");
        }

        private Uri GetEntryPointUri(OciArtifactModuleReference reference)
        {
            string localArtifactPath = this.orasClient.GetLocalPackageEntryPointPath(reference);
            if (Uri.TryCreate(localArtifactPath, UriKind.Absolute, out var uri))
            {
                return uri;
            }

            throw new NotImplementedException($"Local OCI artifact path is malformed: \"{localArtifactPath}\"");
        }

        private static OciArtifactModuleReference ConvertReference(ModuleReference reference)
        {
            if(reference is OciArtifactModuleReference typed)
            {
                return typed;
            }

            throw new ArgumentException($"Reference type '{reference.GetType().Name}' is not supported.");
        }
    }
}
