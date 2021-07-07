// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Core.Extensions;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Bicep.Core.Registry
{
    public class ModuleRegistryDispatcher : IModuleRegistryDispatcher
    {
        private readonly ImmutableDictionary<string, IModuleRegistry> schemes;
        private readonly ImmutableDictionary<Type, IModuleRegistry> referenceTypes;

        public ModuleRegistryDispatcher(IFileResolver fileResolver)
        {
            (this.schemes, this.referenceTypes) = Initialize(fileResolver);
            this.AvailableSchemes = this.schemes.Keys.OrderBy(s => s).ToImmutableArray();
        }

        public IEnumerable<string> AvailableSchemes { get; }

        public ModuleReference? TryParseModuleReference(string reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var parts = reference.Split(':', 2, System.StringSplitOptions.None);
            switch (parts.Length)
            {
                case 1:
                    // local path reference
                    return schemes[string.Empty].TryParseModuleReference(parts[0], out failureBuilder);

                case 2:
                    var scheme = parts[0];

                    if (schemes.TryGetValue(scheme, out var registry))
                    {
                        // the scheme is recognized
                        var rawValue = parts[1];
                        return registry.TryParseModuleReference(rawValue, out failureBuilder);
                    }

                    // unknown scheme
                    failureBuilder = x => x.UnknownModuleReferenceScheme(scheme, this.AvailableSchemes);
                    return null;

                default:
                    // empty string
                    failureBuilder = x => x.ModulePathHasNotBeenSpecified();
                    return null;
            }
        }

        public bool IsModuleRestoreRequired(ModuleReference reference)
        {
            Type refType = reference.GetType();
            if (this.referenceTypes.TryGetValue(refType, out var registry))
            {
                return registry.IsModuleRestoreRequired(reference);
            }

            throw new NotImplementedException($"Unexpected module reference type '{refType.Name}'");
        }

        public Uri? TryGetLocalModuleEntryPointPath(Uri parentModuleUri, ModuleReference reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            Type refType = reference.GetType();
            if (this.referenceTypes.TryGetValue(refType, out var registry))
            {
                return registry.TryGetLocalModuleEntryPointPath(parentModuleUri, reference, out failureBuilder);
            }

            throw new NotImplementedException($"Unexpected module reference type '{refType.Name}'");
        }

        public void RestoreModules(IEnumerable<ModuleReference> references, ModuleInitErrorDelegate onErrorAction)
        {
            var lookup = references.ToLookup(@ref => @ref.GetType());

            foreach (var referenceType in this.referenceTypes.Keys.Where(refType => lookup.Contains(refType)))
            {
                this.referenceTypes[referenceType].RestoreModules(lookup[referenceType], onErrorAction);
            }
        }

        public IDictionary<ModuleDeclarationSyntax, DiagnosticBuilder.ErrorBuilderDelegate> RestoreModules(IEnumerable<ModuleDeclarationSyntax> modules)
        {
            var referenceLookup = modules.ToImmutableDictionaryExcludingNull(module => this.TryGetModuleReference(module, out _), EqualityComparer<ModuleReference>.Default);
            var failures = new Dictionary<ModuleDeclarationSyntax, DiagnosticBuilder.ErrorBuilderDelegate>();

            RestoreModules(referenceLookup.Keys, (reference, errorMessage) => failures.Add(referenceLookup[reference], x => x.ModuleRestoreFailedWithMessage(reference.FullyQualifiedReference, errorMessage)));

            return failures;
        }

        // TODO: Once we have some sort of dependency injection in the CLI, this could be simplified
        private static (ImmutableDictionary<string, IModuleRegistry>, ImmutableDictionary<Type, IModuleRegistry>) Initialize(IFileResolver fileResolver)
        {
            var mapByString = ImmutableDictionary.CreateBuilder<string, IModuleRegistry>();
            var mapByType = ImmutableDictionary.CreateBuilder<Type, IModuleRegistry>();

            void AddRegistry(string scheme, Type moduleRefType, IModuleRegistry instance)
            {
                mapByString.Add(scheme, instance);
                mapByType.Add(moduleRefType, instance);
            }

            AddRegistry(string.Empty, typeof(LocalModuleReference), new LocalModuleRegistry(fileResolver));
            AddRegistry(OciArtifactModuleReference.Scheme, typeof(OciArtifactModuleReference), new OciModuleRegistry(fileResolver));

            return (mapByString.ToImmutable(), mapByType.ToImmutable());
        }
    }
}
