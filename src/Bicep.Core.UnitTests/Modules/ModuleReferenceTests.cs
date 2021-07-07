// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Modules;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Bicep.Core.UnitTests.Modules
{
    [TestClass]
    public class ModuleReferenceTests
    {
        [DataTestMethod]
        [DynamicData(nameof(GetModuleRefSubClasses), DynamicDataSourceType.Method, DynamicDataDisplayNameDeclaringType = typeof(DataSet))]
        public void ModuleRefSubClassesShouldOverrideEqualsAndHashCode(Type type)
        {
            static MethodInfo? GetDeclaredMethod(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly);

            var equals = GetDeclaredMethod(type, nameof(object.Equals));
            equals.Should().NotBeNull();
            equals!.ReturnType.Should().Be(typeof(bool));
            equals.GetParameters().Should().SatisfyRespectively(x => x.ParameterType.Should().Be(typeof(object)));

            var getHashCode = GetDeclaredMethod(type, nameof(object.GetHashCode));
            getHashCode.Should().NotBeNull();
            getHashCode!.ReturnType.Should().Be(typeof(int));
            getHashCode.GetParameters().Should().BeEmpty();

            var toString = GetDeclaredMethod(type, nameof(object.ToString));
            toString.Should().NotBeNull();
            toString!.ReturnType.Should().Be(typeof(string));
            toString.GetParameters().Should().BeEmpty();
        }

        private static IEnumerable<object[]> GetModuleRefSubClasses() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && type.IsSubclassOf(typeof(ModuleReference)))
            .Select(type => new[] { type });
    }
}
