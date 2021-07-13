// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Modules;
using System;
using System.Collections.Generic;

namespace Bicep.Core.Registry
{
    public interface IModuleRegistry
    {
        ModuleReference? TryParseModuleReference(string reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);

        bool IsModuleRestoreRequired(ModuleReference reference);

        Uri? TryGetLocalModuleEntryPointPath(Uri parentModuleUri, ModuleReference reference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);

        IDictionary<ModuleReference, DiagnosticBuilder.ErrorBuilderDelegate> RestoreModules(IEnumerable<ModuleReference> references);
    }
}
