This is a sample repo to reproduce **NUnit.ENgine 3.12.0** assemblies load issues on .NET Core.

### Run tests
In order to run the test using NUnit.Engine 3.12.0, you will need to use [NUnit.ConsoleRunner.NetCore](https://www.nuget.org/packages/NUnit.ConsoleRunner.NetCore/) NuGet package. For that add this package as a dependency to one of your projects and perform a `dotnet restore` (or you could try with [Get-PackageProvider](https://docs.microsoft.com/en-us/powershell/module/packagemanagement/get-packageprovider?view=powershell-7.1)).
Once the package is restores (mine was at `C:\Users\brekhadd020117\.nuget` ), locate it and run:

```C:\Users\brekhadd020117\.nuget\packages\nunit.consolerunner.netcore\3.12.0-beta1\tools\nunit3-console.exe <path_to_clone>\nunit-sample\.build\bin\Debug\netcoreapp3.1\UnitTests.dll```

### Excpected behavior
>Test Run Summary
  Overall result: Passed
  Test Count: 1, Passed: 1, Failed: 0, Warnings: 0, Inconclusive: 0, Skipped: 0

### Actual behavior

>System.Reflection.TargetInvocationException : Exception has been thrown by the target of an invocation.
  ----> System.IO.FileNotFoundException : Could not load file or assembly 'LibraryProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor, Boolean wrapExceptions)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at System.Reflection.MethodBase.Invoke(Object obj, Object[] parameters)
   at UnitTests.EmitDemo.CalculateMax(Double a, Double b) in D:\ARD\nunit-sample\UnitTests\EmiDemo.cs:line 82
   at UnitTests.EmitDemo.Test() in D:\ARD\nunit-sample\UnitTests\EmiDemo.cs:line 92
--FileNotFoundException
   at RoslynCore.Helper.GetMax(Double d1, Double d2)

### Attempt of fix

Since NUnit is using [a custom AssemblyLoadContext](https://github.com/nunit/nunit-console/pull/781), I've tried to force my assmeblies load in this custom context, and it worked!

Code snippet added:
```
var nunitCustomAssemblyLoadContext = AssemblyLoadContext.All.Single(c => c.ToString().Contains("NUnit.Engine.Internal.CustomAssemblyLoadContext"));
var asm = nunitCustomAssemblyLoadContext.LoadFromAssemblyPath(path);
```