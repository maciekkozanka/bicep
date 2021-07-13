// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Modules;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.Modules
{
    class LocalModuleReferenceTests
    {
        [DataRow("./test.bicep")]
        [DataRow("foo/bar/test.bicep")]
        [DataRow("../bar/test.bicep")]
        [DataTestMethod]
        public void TryParseModuleReference_ValidLocalReference_ShouldParse(string value)
        {
            var reference = Parse(value);
            reference.Path.Should().Be(value);
        }

        private static LocalModuleReference Parse(string package)
        {
            var parsed = LocalModuleReference.TryParse(package, out var failureBuilder);
            parsed.Should().NotBeNull();
            failureBuilder.Should().BeNull();
            return parsed!;
        }

        // TODO: Add equality tests
        private static (LocalModuleReference, LocalModuleReference) ParsePair(string first, string second) => (Parse(first), Parse(second));
    }
}
