// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Modules;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.Modules
{
    [TestClass]
    public class NugetModuleReferenceTests
    {
        [DataRow("Test.Test2@1.2.3", "Test.Test2@1.2.3")]
        [DataRow("Hello.World@1.0","hello.world@1.0")]
        [DataRow("Hello.World@1.0-PREVIEW", "hello.world@1.0-preview")]
        [DataTestMethod]
        public void PackagesWithIdOrVersionCasingDifferencesShouldBeEqual(string package1, string package2)
        {
            var (first, second) = ParsePair(package1, package2);

            first.Equals(second).Should().BeTrue();
            first.GetHashCode().Should().Be(second.GetHashCode());
        }

        [DataRow("Hello@22", "World@22")]
        [DataRow("Hello@1", "Hello@2")]
        [DataTestMethod]
        public void MismatchedPackagesShouldNotBeEqual(string package1, string package2)
        {
            var (first, second) = ParsePair(package1, package2);
            first.Equals(second).Should().BeFalse();
            first.GetHashCode().Should().NotBe(second.GetHashCode());
        }

        private static NugetModuleReference Parse(string package)
{
            var parsed = NugetModuleReference.TryParse(package, out _);
            parsed.Should().NotBeNull();
            return parsed!;
        }

        private static (NugetModuleReference, NugetModuleReference) ParsePair(string first, string second) => (Parse(first), Parse(second));
    }
}
