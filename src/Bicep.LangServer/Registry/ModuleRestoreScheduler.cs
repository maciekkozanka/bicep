// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Core.Extensions;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Bicep.LanguageServer.Registry
{
    public sealed class ModuleRestoreScheduler
    {
        private readonly IModuleRegistryDispatcher dispatcher;

        private readonly Queue<ImmutableArray<ModuleDeclarationSyntax>> queue = new();

        private readonly CancellationTokenSource cancellationTokenSource = new();

        // block on initial wait until signaled
        private readonly ManualResetEventSlim manualResetEvent = new(false);

        private Task? consumerTask;

        public ModuleRestoreScheduler(IModuleRegistryDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Requests that the specified modules be restored to the local file system.
        /// Does not wait for the operation to complete and returns immediately.
        /// </summary>
        /// <param name="references">The module references</param>
        public void RequestModuleRestore(IEnumerable<ModuleDeclarationSyntax> references)
        {
            var immutable = references.ToImmutableArray();
            lock (this.queue)
            {
                this.queue.Enqueue(immutable);

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

                var items = new List<ModuleDeclarationSyntax>();
                lock (this.queue)
                {
                    this.UnsafeCollectModuleReferences(items);
                    Debug.Assert(this.queue.Count == 0, "this.queue.Count == 0");

                    // queue has been consumed - next iteration should block until more items have been added
                    this.manualResetEvent.Reset();
                }

                // TODO: What to do with the results?
                var failures = this.dispatcher.RestoreModules(items);
            }
        }

        private void UnsafeCollectModuleReferences(List<ModuleDeclarationSyntax> items)
        {
            while(this.queue.TryDequeue(out var partial))
            {
                items.AddRange(partial);
            }
        }
    }
}
