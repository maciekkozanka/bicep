// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Modules;
using Bicep.Core.Syntax;

namespace Bicep.Core.Registry
{
    public static class ModuleRegistryDispatcherExtensions
    {
        public static ModuleReference? TryGetModuleReference(this IModuleRegistryDispatcher dispatcher, ModuleDeclarationSyntax syntax, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var moduleReferenceString = SyntaxHelper.TryGetModulePath(syntax, out var getModulePathFailureBuilder);
            if (moduleReferenceString is null)
            {
                failureBuilder = getModulePathFailureBuilder;
                return null;
            }

            return dispatcher.TryParseModuleReference(moduleReferenceString, out failureBuilder);
        }
    }
}
