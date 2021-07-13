// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace Bicep.Core.Modules
{
    public class OciArtifactModuleReference : ModuleReference
    {
        public const string Scheme = "oci";

        // these exist to keep equals and hashcode implementations in sync
        private static readonly IEqualityComparer<string> RegistryComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly IEqualityComparer<string> RepositoryComparer = StringComparer.Ordinal;
        private static readonly IEqualityComparer<string> TagComparer = StringComparer.Ordinal;

        public OciArtifactModuleReference(string registry, string repository, string tag)
        {
            this.Registry = registry;
            this.Repository = repository;
            this.Tag = tag;
        }

        public string Registry { get; }

        public string Repository { get; }

        public string Tag { get; }

        public string ArtifactId => $"{this.Registry}{this.Repository}:{this.Tag}";

        public override string UnqualifiedReference => this.ArtifactId;

        public override string FullyQualifiedReference => this.FormatFullyQualifiedReference(Scheme);

        public override bool Equals(object obj)
        {
            if(obj is not OciArtifactModuleReference other)
            {
                return false;
            }

            return
                // TODO: Are all of these case-sensitive?
                RegistryComparer.Equals(this.Registry, other.Registry) &&
                RepositoryComparer.Equals(this.Repository, other.Repository) &&
                TagComparer.Equals(this.Tag, other.Tag);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(this.Registry, RegistryComparer);
            hash.Add(this.Repository, RepositoryComparer);
            hash.Add(this.Tag, TagComparer);

            return hash.ToHashCode();
        }

        public static OciArtifactModuleReference? TryParse(string rawValue, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            static DiagnosticBuilder.ErrorBuilderDelegate CreateErrorFunc(string rawValue) => x => x.InvalidOciArtifactReference($"oci:{rawValue}");

            // split tag from the uri
            var lastColonIndex = rawValue.LastIndexOf(':');
            if(lastColonIndex < 0)
            {
                failureBuilder = CreateErrorFunc(rawValue);
                return null;
            }

            var artifactStr = rawValue.Substring(0, lastColonIndex);
            var tag = rawValue.Substring(lastColonIndex + 1);

            // docker image refs (incl. OCI artifact refs) are not URIs
            // it appears the scheme part is missing, so we will fake it

            if(!Uri.TryCreate("oci://" + artifactStr, UriKind.Absolute, out var artifactUri) || string.IsNullOrWhiteSpace(tag))
            {
                failureBuilder = CreateErrorFunc(rawValue);
                return null;
            }

            // TODO: Do we need more validation?
            failureBuilder = null;
            var registry = artifactUri.Port < 0
                ? artifactUri.Host
                : $"{artifactUri.Host}:{artifactUri.Port}";
            var repo = artifactUri.PathAndQuery;
            return new OciArtifactModuleReference(registry, repo, tag);
        }
    }
}
