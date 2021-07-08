// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Modules;
using Bicep.Core.Registry;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Bicep.LanguageServer.Registry
{
    public class ModuleRestoreScheduler
    {
        private readonly IModuleRegistryDispatcher dispatcher;

        private readonly Queue<ImmutableArray<ModuleReference>> queue = new();

        public ModuleRestoreScheduler(IModuleRegistryDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Requests that the specified modules be restored to the local file system.
        /// Does not wait for the operation to complete and returns immediately.
        /// </summary>
        /// <param name="references">The module references</param>
        public void RequestModuleRestore(IEnumerable<ModuleReference> references)
        {
            var immutable = references.ToImmutableArray();
            lock (this.queue)
            {
                this.queue.Enqueue(immutable);
            }
        }

        public void Start()
        {
            //var task = new Task()
        }
    }
}
