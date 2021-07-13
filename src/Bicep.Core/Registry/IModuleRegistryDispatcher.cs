// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Syntax;
using System;
using System.Collections.Generic;

namespace Bicep.Core.Registry
{
    public interface IModuleRegistryDispatcher
    {
        IEnumerable<string> AvailableSchemes { get; }

        bool ValidateModuleReference(ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);

        bool IsModuleAvailable(ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? errorDetailBuilder);

        Uri? TryGetLocalModuleEntryPointPath(Uri parentModuleUri, ModuleDeclarationSyntax module, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);

        void RestoreModules(IEnumerable<ModuleDeclarationSyntax> modules);
    }
}
