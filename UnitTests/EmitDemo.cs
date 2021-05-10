using LibraryProject;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace UnitTests
{
    public sealed class EmitDemo
    {
        private static IEnumerable<string> GetReferencedAssembliesRecursively(Assembly assembly)
        {
            var assemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var referencedAssembly = Assembly.Load(reference.FullName);
                assemblies.Add(referencedAssembly.Location);
                assemblies.UnionWith(GetReferencedAssembliesRecursively(referencedAssembly));
            }
            return assemblies;
        }

        private static object CalculateSum(double a, double b)
        {
            const string code = @"Option strict off
Imports System
Imports Microsoft.VisualBasic
Imports LibraryProject

Namespace DummyNamespace

    Public Class Helper
             Inherits CustomType

Function Sum(d1 As Double, d2 As Double) As Double
    Return d1+d2
End Function

    End Class
End Namespace";
            var tree = VisualBasicSyntaxTree.ParseText(code, VisualBasicParseOptions.Default);
            var binDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            var vbAssembly = typeof(Conversions).Assembly;
            var vb = MetadataReference.CreateFromFile(vbAssembly.Location);
            var fileName = "myLib";
            var compilation = VisualBasicCompilation.Create(fileName)
                .WithOptions(new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(typeof(CustomType).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(vb)
                .AddReferences(GetReferencedAssembliesRecursively(vbAssembly).Select(a => MetadataReference.CreateFromFile(a)))
                .AddSyntaxTrees(tree);
            var newpath = Path.Combine(Path.GetTempPath(), $"MyLibrary_{DateTime.Now:yyMMdd}");
            Directory.CreateDirectory(newpath);
            var path = Path.Combine(newpath, "MyLibrary.dll");
            var compilationResult = compilation.Emit(path);
            if (compilationResult.Success)
            {
                // below code works since our reference to LibraryProject.dll is already loaded in thi ALC
                /* var nunitCustomAssemblyLoadContext = AssemblyLoadContext.All.Single(c => c.ToString().Contains("NUnit.Engine.Internal.CustomAssemblyLoadContext"));
                var asm = nunitCustomAssemblyLoadContext.LoadFromAssemblyPath(path);*/

                //This code works as well using NUnit.Console 3.13.0: https://github.com/nunit/nunit-console/pull/942
                var asm = (AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default).LoadFromAssemblyPath(path);

                // this doesn't work
                //var asm = Assembly.LoadFrom(path);
                var classFullName = "DummyNamespace.Helper";
                var type = asm.GetType(classFullName);
                if (type == null)
                    throw new Exception($"Type '{classFullName}' could not be found");
                var instance = asm.CreateInstance(classFullName);
                if(instance == null)
                    throw new Exception($"'{classFullName}' deosn't have a valid default constructor");
                var methodInfo = type.GetMethod("Sum");
                if (methodInfo == null)
                    throw new Exception($"Method 'Sum' could not be found in type {type.FullName}");
                return methodInfo.Invoke(instance, new object[] { a, b });
            }
            foreach (var codeIssue in compilationResult.Diagnostics)
                Console.WriteLine($"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()}, Location: {codeIssue.Location.GetLineSpan()}, Severity: {codeIssue.Severity}");
            return null;
        }

        [Test]
        public void Test_EmiCompilation()
        {
            var result = CalculateSum(1, 2);
            Assert.IsNotNull(result);
            Assert.AreEqual(3, (double)result);
        }
    }
}