// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bicep.Core.Registry
{
    public class ModuleRegistryDispatcher : IModuleRegistryDispatcher
    {
        private readonly ImmutableDictionary<string, IModuleRegistry> schemes;
        private readonly ImmutableDictionary<Type, IModuleRegistry> referenceTypes;

        private readonly ConditionalWeakTable<ModuleDeclarationSyntax, DiagnosticBuilder.ErrorBuilderDelegate> restoreStatuses;

        public ModuleRegistryDispatcher(IFileResolver fileResolver)
        {
            (this.schemes, this.referenceTypes) = Initialize(fileResolver);
            this.AvailableSchemes = this.schemes.Keys.OrderBy(s => s).ToImmutableArray();
            this.restoreStatuses = new ConditionalWeakTable<ModuleDeclarationSyntax, DiagnosticBuilder.ErrorBuilderDelegate>();
        }

        public IEnumerable<string> AvailableSchemes { get; }

        public bool ValidateModuleReference(ModuleDeclarationSyntax module, [NotNullWhen(false)] out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder) =>
            this.TryGetModuleReference(module, out failureBuilder) is not null;

        public bool IsModuleAvailable(ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var reference = GetModuleReference(module);
            Type refType = reference.GetType();
            if (!this.referenceTypes.TryGetValue(refType, out var registry))
            {
                throw new NotImplementedException($"Unexpected module reference type '{refType.Name}'");
            }

            // have we already failed to restore this module?
            // TODO: This needs to reset after some time
            if(this.HasRestoreFailed(module, out var restoreFailureBuilder))
            {
                failureBuilder = restoreFailureBuilder;
                return false;
            }
                        
            if(registry.IsModuleRestoreRequired(reference))
            {
                // module is not present on the local file system
                // TODO: This error needs to have different text in CLI vs the language server
                failureBuilder = x => x.ModuleRequiresRestore(reference.FullyQualifiedReference);
                return false;
            }

            failureBuilder = null;
            return true;
        }

        public Uri? TryGetLocalModuleEntryPointPath(Uri parentModuleUri, ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            // has restore already failed for this module?
            if(this.HasRestoreFailed(module, out var restoreFailureBuilder))
            {
                failureBuilder = restoreFailureBuilder;
                return null;
            }

            var reference = GetModuleReference(module);
            Type refType = reference.GetType();
            if (this.referenceTypes.TryGetValue(refType, out var registry))
            {
                return registry.TryGetLocalModuleEntryPointPath(parentModuleUri, reference, out failureBuilder);
            }

            throw new NotImplementedException($"Unexpected module reference type '{refType.Name}'");
        }

        public void RestoreModules(IEnumerable<ModuleDeclarationSyntax> modules)
        {
            // WARNING: The various operations on ModuleReference objects here rely on the custom Equals() implementation and NOT on object identity

            // one module ref can be associated with multiple module declarations
            var referenceToModules = modules.ToLookup(module => GetModuleReference(module));

            var references = modules.Select(module => GetModuleReference(module)).Distinct();

            // split module refs by reference type
            var referencesByRefType = references.ToLookup(@ref => @ref.GetType());

            // send each set of refs to its own registry
            foreach (var referenceType in this.referenceTypes.Keys.Where(refType => referencesByRefType.Contains(refType)))
            {
                var restoreStatuses = this.referenceTypes[referenceType].RestoreModules(referencesByRefType[referenceType]);

                // update restore status for each failed module restore
                foreach(var (failedReference, failureBuilder) in restoreStatuses)
                {
                    foreach(var failedModule in referenceToModules[failedReference])
                    {
                        this.SetRestoreFailure(failedModule, failureBuilder);
                    }
                }
            }
        }

        private ModuleReference GetModuleReference(ModuleDeclarationSyntax module) =>
            TryGetModuleReference(module, out _) ?? throw new InvalidOperationException("The specified module reference is not valid. Ensure it is validated before calling.");

        private ModuleReference? TryGetModuleReference(ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var moduleReferenceString = SyntaxHelper.TryGetModulePath(module, out var getModulePathFailureBuilder);
            if (moduleReferenceString is null)
            {
                failureBuilder = getModulePathFailureBuilder ?? throw new InvalidOperationException($"Expected {nameof(SyntaxHelper.TryGetModulePath)} to provide failure diagnostics.");
                return null;
            }

            return this.TryParseModuleReference(moduleReferenceString, out failureBuilder);
        }

        private ModuleReference? TryParseModuleReference(string reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
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

        private bool HasRestoreFailed(ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            // TODO: In cases the user publishes a module after authoring an invalid reference to it, the current logic will permanently block it
            // until the language server is restarted. We need to reset the failure status after some time or create an alternative mechanism.
            return this.restoreStatuses.TryGetValue(module, out failureBuilder);
        }

        private void SetRestoreFailure(ModuleDeclarationSyntax module, DiagnosticBuilder.ErrorBuilderDelegate failureBuilder)
        {
            this.restoreStatuses.AddOrUpdate(module, failureBuilder);
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
