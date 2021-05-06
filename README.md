This is a sample repo to reproduce **NUnit.ENgine 3.12.0** assemblies load issues on .NET Core (cf. [nunit.console-#942](https://github.com/nunit/nunit-console/pull/942)).

### Run tests
In order to run the test using NUnit.Engine 3.12.0 (assuming you already run `dotnet build`), you will need to use [NUnit.ConsoleRunner.NetCore](https://www.nuget.org/packages/NUnit.ConsoleRunner.NetCore/) 3.12.0-beta*(beta1 or beta2) NuGet package. For that add this package as a dependency to one of your projects and perform a `dotnet restore` (or you could try with [Get-PackageProvider](https://docs.microsoft.com/en-us/powershell/module/packagemanagement/get-packageprovider?view=powershell-7.1)).
Once the package is restored (mine was at `C:\Users\brekhadd020117\.nuget` ), locate it and run:

```C:\Users\brekhadd020117\.nuget\packages\nunit.consolerunner.netcore\3.12.0-beta1\tools\nunit3-console.exe <path_to_clone>\nunit-sample\.build\bin\Debug\netcoreapp3.1\UnitTests.dll```

### Excpected behavior
>Test Run Summary
  Overall result: Passed
  Test Count: 1, Passed: 1, Failed: 0, Warnings: 0, Inconclusive: 0, Skipped: 0

### Actual behavior

>Error : UnitTests.EmitDemo.Test_EmiCompilation
System.Exception : Type 'DummyNamespace.Helper' could not be found
   at UnitTests.EmitDemo.CalculateSum(Double a, Double b) in D:\git\nunit-sample\UnitTests\EmitDemo.cs:line 83
   at UnitTests.EmitDemo.Test_EmiCompilation() in D:\git\nunit-sample\UnitTests\EmitDemo.cs:line 99

### Attempt of fix

Since NUnit is using [a custom AssemblyLoadContext](https://github.com/nunit/nunit-console/pull/781), I've tried to force my assmeblies load in this custom context, and it worked!

Code snippet added:
```
var nunitCustomAssemblyLoadContext = AssemblyLoadContext.All.Single(c => c.ToString().Contains("NUnit.Engine.Internal.CustomAssemblyLoadContext"));
var asm = nunitCustomAssemblyLoadContext.LoadFromAssemblyPath(path);
```