// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Modules;
using Bicep.Core.Syntax;
using System.Collections.Generic;

namespace Bicep.Core.Registry
{
    public interface IModuleRegistryDispatcher : IModuleRegistry
    {
        IEnumerable<string> AvailableSchemes { get; }

        IDictionary<ModuleDeclarationSyntax, DiagnosticBuilder.ErrorBuilderDelegate> RestoreModules(IEnumerable<ModuleDeclarationSyntax> modules);
    }
}
