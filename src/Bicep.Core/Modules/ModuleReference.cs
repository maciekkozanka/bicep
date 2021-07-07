// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Bicep.Core.Modules
{
    public abstract class ModuleReference
    {
        public abstract string FullyQualifiedReference { get; }

        public abstract string UnqualifiedReference { get; }

        protected string FormatFullyQualifiedReference(string scheme) => $"{scheme}:{this.UnqualifiedReference}";
    }
}
