# Incremental generator ForAttributeWithMetadataName issues

## System.ArgumentException: SyntaxTree is not part of the compilation

This project contains very simple incremental source code generator `TestGenerator`, which takes `ClassDeclarationSyntax`s having `[Generate]` attribute and generates a partial class for it. This selection is done via `ForAttributeWithMetadataName`.

### Expected outcome
Generator has no error

### Actual outcome
Generator crashes with:
```
Warning	CS8785	Generator 'TestGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'ArgumentException' with message 'SyntaxTree is not part of the compilation
Parameter name: syntaxTree'	Generator.Test	...\src\Generator.Test	1	Active	Generator threw the following exception:
'System.ArgumentException: SyntaxTree is not part of the compilation
Parameter name: syntaxTree
   at Microsoft.CodeAnalysis.CSharp.CSharpCompilation.GetSemanticModel(SyntaxTree syntaxTree, Boolean ignoreAccessibility)
   at Microsoft.CodeAnalysis.SyntaxValueProvider.<>c__DisplayClass11_0`1.<ForAttributeWithMetadataName>b__0(ValueTuple`2 tuple, CancellationToken cancellationToken)
   at Microsoft.CodeAnalysis.UserFunctionExtensions.<>c__DisplayClass0_0`2.<WrapUserFunction>b__0(TInput input, CancellationToken token)'.
```

### Steps to reproduce:

1. Open and build the solution
2. Re-Open the visual studio to load the generator
    * Don't open any file from "Generator.Test" project yet

3. Open file `Generator.Test/DClass.cs`
    * Remove `[Generate]` attribute on the class `DClass`

4. Open file `Generator.Test/CClass.cs`
    * This should now trigger the error. If not, do a whitespace chage in this file

These steps rely on the order of `compilation.CommonSyntaxTrees`. In my case, the order is following:

```
...\Generator.Test\DClass.cs
...\Generator.Test\CClass.cs
...\Generator.Test\AClass.cs
...\Generator.Test\obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs
...\Generator.Test\obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs
```

## Duplicate hintName
The same steps as above could also lead to duplicate `hintName` issue, because one SyntaxNode will end up in the `NodeStateTable` twice. Once from previous compilation and once from current compilation. In this case it'll be `Generator.Test\CClass.cs
`

## Diagnostic data

Here is how the `NodeStateTable`s looks like inside `TransformNode` during the reproduction steps.
As you can see, the final result will end with duplicate `SyntaxNode`s in the `newTable`, and one of them is from previous compilation.

<table>
<thead>
  <tr>
    <th colspan="6">Iteration 1 - Remove `[Generate]` attribute on the class `DClass` (Step 3.)</th>
  </tr>
  <tr>
    <th></th>
    <th colspan="2">.Where((info, _) => info.Info.HasFlag(ContainsAttributeList))</th>
    <th colspan="3">.Where(tuple => tuple.Item2.Length > 0)</th>
  </tr>
  <tr>
    <th>Index</th>
    <th>PreviousTable</th>
    <th>NewTable</th>
    <th>SourceTable</th>
    <th>PreviousTable</th>
    <th>NewTable</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td>0</td>
    <td>DClass.cs (Cached)</td>
    <td>DClass.cs (Modified)</td>
    <td>DClass.cs (Modified)</td>
    <td>DClass.cs (Cached)</td>
    <td>DClass.cs (Removed)</td>
  </tr>
  <tr>
    <td>1</td>
    <td>CClass.cs (Cached)</td>
    <td>CClass.cs (Cached)</td>
    <td>CClass.cs (Cached)</td>
    <td>CClass.cs (Cached)</td>
    <td>CClass.cs (Cached)</td>
  </tr>
  <tr>
    <td>2</td>
    <td>AClass.cs (Cached)</td>
    <td>AClass.cs (Cached)</td>
    <td>AClass.cs (Cached)</td>
    <td>AClass.cs (Cached)</td>
    <td>AClass.cs (Cached)</td>
  </tr>
  <tr>
    <td>3</td>
    <td>obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs   (Cached)</td>
    <td>obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs   (Cached)</td>
    <td>obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs   (Cached)</td>
    <td> </td>
    <td> </td>
  </tr>
  <tr>
    <td>4</td>
    <td>obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs   (Cached)</td>
    <td>obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs   (Cached)</td>
    <td>obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs   (Cached)</td>
    <td> </td>
    <td> </td>
  </tr>
<tbody>
<thead>
  <tr>
    <th colspan="6">Iteration 2 - Open file `Generator.Test/CClass.cs` (Step 4.)</th>
  </tr>
  <tr>
    <th></th>
    <th colspan="2">.Where((info, _) => info.Info.HasFlag(ContainsAttributeList))</th>
    <th colspan="3">.Where(tuple => tuple.Item2.Length > 0)</th>
  </tr>
  <tr>
    <th>Index</th>
    <th>PreviousTable</th>
    <th>NewTable</th>
    <th>SourceTable</th>
    <th>PreviousTable</th>
    <th>NewTable</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td>0</td>
    <td>DClass.cs (Cached)</td>
    <td>DClass.cs (Cached)</td>
    <td>DClass.cs (Cached)</td>
    <td>CClass.cs (Cached)</td>
    <td>CClass.cs (Cached)</td>
  </tr>
  <tr>
    <td>1</td>
    <td>CClass.cs (Cached)</td>
    <td>CClass.cs (Modified)</td>
    <td>CClass.cs (Modified)</td>
    <td>AClass.cs (Cached)</td>
    <td>CClass.cs (Modified)</td>
  </tr>
  <tr>
    <td>2</td>
    <td>AClass.cs (Cached)</td>
    <td>AClass.cs (Cached)</td>
    <td>AClass.cs (Cached)</td>
    <td> </td>
    <td> </td>
  </tr>
  <tr>
    <td>3</td>
    <td>obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs&nbsp;&nbsp;&nbsp;(Cached)</td>
    <td>obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs&nbsp;&nbsp;&nbsp;(Cached)</td>
    <td>obj\Debug\net6.0\.NETCoreApp,Version=v6.0.AssemblyAttributes.cs&nbsp;&nbsp;&nbsp;(Cached)</td>
    <td> </td>
    <td> </td>
  </tr>
  <tr>
    <td>4</td>
    <td>obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs&nbsp;&nbsp;&nbsp;(Cached)</td>
    <td>obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs&nbsp;&nbsp;&nbsp;(Cached)</td>
    <td>obj\Debug\net6.0\Generator.Test.AssemblyInfo.cs&nbsp;&nbsp;&nbsp;(Cached)</td>
    <td>-</td>
    <td> </td>
  </tr>
</tbody>
</table>
