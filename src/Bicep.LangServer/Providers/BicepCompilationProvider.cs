// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.CompilationManager;
using Bicep.LanguageServer.Registry;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LanguageServer.Providers
{
    /// <summary>
    /// Creates compilation contexts.
    /// </summary>
    /// <remarks>This class exists only so we can mock fatal exceptions in tests.</remarks>
    public class BicepCompilationProvider: ICompilationProvider
    {
        private readonly IResourceTypeProvider resourceTypeProvider;
        private readonly IFileResolver fileResolver;
        private readonly IModuleRegistryDispatcher dispatcher;

        public BicepCompilationProvider(IResourceTypeProvider resourceTypeProvider, IFileResolver fileResolver, IModuleRegistryDispatcher dispatcher)
        {
            this.resourceTypeProvider = resourceTypeProvider;
            this.fileResolver = fileResolver;
            this.dispatcher = dispatcher;
        }

        public CompilationContext Create(IReadOnlyWorkspace workspace, DocumentUri documentUri)
        {
            var syntaxTreeGrouping = SyntaxTreeGroupingBuilder.Build(fileResolver, dispatcher, workspace, documentUri.ToUri());
            return this.CreateContext(syntaxTreeGrouping);
        }

        public CompilationContext Update(IReadOnlyWorkspace workspace, CompilationContext current)
        {
            var syntaxTreeGrouping = SyntaxTreeGroupingBuilder.Rebuild(dispatcher, workspace, current.Compilation.SyntaxTreeGrouping);
            return this.CreateContext(syntaxTreeGrouping);
        }

        private CompilationContext CreateContext(SyntaxTreeGrouping syntaxTreeGrouping)
        {
            var compilation = new Compilation(resourceTypeProvider, syntaxTreeGrouping);
            return new CompilationContext(compilation);
        }
    }
}
