// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Bicep.Core.Configuration;
using Bicep.Core.Extensions;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Samples;
using Bicep.Core.Syntax;
using Bicep.Core.UnitTests.Configuration;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.Providers;
using Bicep.LanguageServer.Registry;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LangServer.UnitTests
{
    [TestClass]
    public class BicepCompilationProviderTests
    {
        private static MockRepository Repository = new(MockBehavior.Strict);

        private static IFileResolver CreateEmptyFileResolver()
            => new InMemoryFileResolver(new Dictionary<Uri, string>());

        [TestMethod]
        public void Create_ShouldReturnValidCompilation()
        {
            IFileResolver fileResolver = CreateEmptyFileResolver();
            var mockScheduler = Repository.Create<IModuleRestoreScheduler>();
            mockScheduler.Setup(m => m.RequestModuleRestore(It.IsAny<DocumentUri>(), It.IsAny<IEnumerable<ModuleDeclarationSyntax>>()));

            var provider = new BicepCompilationProvider(TestTypeHelper.CreateEmptyProvider(), fileResolver, new ModuleRegistryDispatcher(fileResolver), mockScheduler.Object);

            var fileUri = DocumentUri.Parse($"/{DataSets.Parameters_LF.Name}.bicep");
            var syntaxTree = SyntaxTree.Create(fileUri.ToUri(), DataSets.Parameters_LF.Bicep);
            var workspace = new Workspace();
            workspace.UpsertSyntaxTrees(syntaxTree.AsEnumerable());
            var context = provider.Create(workspace, fileUri);

            context.Compilation.Should().NotBeNull();
            // TOOD: remove Where when the support of modifiers is dropped.
            context.Compilation.GetEntrypointSemanticModel()
                   .GetAllDiagnostics(new ConfigHelper().GetDisabledLinterConfig())
                   .Where(d => d.Code != "BCP161").Should().BeEmpty();
            context.LineStarts.Should().NotBeEmpty();
            context.LineStarts[0].Should().Be(0);
        }
    }
}

