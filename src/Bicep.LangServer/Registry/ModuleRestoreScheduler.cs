// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Registry;
using Bicep.Core.Syntax;
using Bicep.LanguageServer.CompilationManager;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Bicep.LanguageServer.Registry
{
    public sealed class ModuleRestoreScheduler : IModuleRestoreScheduler
    {
        private record QueueItem(DocumentUri Uri, ImmutableArray<ModuleDeclarationSyntax> References);

        private readonly IModuleRegistryDispatcher dispatcher;

        private readonly ICompilationManager compilationManager;

        private readonly Queue<QueueItem> queue = new();

        private readonly CancellationTokenSource cancellationTokenSource = new();

        // block on initial wait until signaled
        private readonly ManualResetEventSlim manualResetEvent = new(false);

        private Task? consumerTask;

        public ModuleRestoreScheduler(IModuleRegistryDispatcher dispatcher, ICompilationManager compilationManager)
        {
            this.dispatcher = dispatcher;
            this.compilationManager = compilationManager;
        }

        /// <summary>
        /// Requests that the specified modules be restored to the local file system.
        /// Does not wait for the operation to complete and returns immediately.
        /// </summary>
        /// <param name="references">The module references</param>
        public void RequestModuleRestore(DocumentUri documentUri, IEnumerable<ModuleDeclarationSyntax> references)
        {
            var item = new QueueItem(documentUri, references.ToImmutableArray());
            lock (this.queue)
            {
                this.queue.Enqueue(item);

                // notify consumer about new items
                this.manualResetEvent.Set();
            }
        }

        public void Start()
        {
            this.consumerTask = Task.Factory.StartNew(this.ProcessQueueItems, this.cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void ProcessQueueItems()
        {
            var token = this.cancellationTokenSource.Token;
            while (true)
            {
                this.manualResetEvent.Wait(token);

                var uris = new List<DocumentUri>();
                var references = new List<ModuleDeclarationSyntax>();
                lock (this.queue)
                {
                    this.UnsafeCollectModuleReferences(uris, references);
                    Debug.Assert(this.queue.Count == 0, "this.queue.Count == 0");

                    // queue has been consumed - next iteration should block until more items have been added
                    this.manualResetEvent.Reset();
                }

                // this blocks until restore is completed
                // the dispatcher stores the results internally and manages their lifecycle
                this.dispatcher.RestoreModules(references);

                // notify compilation manager that restore is completed
                // to recompile the affected modules
                foreach (var uri in uris)
                {
                    this.compilationManager.RefreshCompilation(uri);
                }
            }
        }

        private void UnsafeCollectModuleReferences(List<DocumentUri> documentUris, List<ModuleDeclarationSyntax> references)
        {
            while (this.queue.TryDequeue(out var item))
            {
                documentUris.Add(item.Uri);
                references.AddRange(item.References);
            }
        }
    }
}
