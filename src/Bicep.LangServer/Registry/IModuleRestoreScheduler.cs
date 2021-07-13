// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Collections.Generic;

namespace Bicep.LanguageServer.Registry
{
    public interface IModuleRestoreScheduler
    {
        void Start();

        void RequestModuleRestore(DocumentUri documentUri, IEnumerable<ModuleDeclarationSyntax> references);
    }
}
