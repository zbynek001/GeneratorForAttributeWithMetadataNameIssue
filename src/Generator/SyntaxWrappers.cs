using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Generator
{
    internal interface ISyntaxWrapper
    {
        void WriteBegin(StringBuilder codeGenerator);

        void WriteEnd(StringBuilder codeGenerator);
    }

    internal interface ISyntaxWrapperSimple
    {
        void Write(StringBuilder codeGenerator);
    }

    internal static class SyntaxWrappers
    {
        public static ImmutableList<ISyntaxWrapper> LoadSyntaxAncestors(TypeDeclarationSyntax syntax)
        {
            var builder = ImmutableList.CreateBuilder<ISyntaxWrapper>();

            foreach (var ancestor in syntax.Ancestors())
            {
                switch (ancestor)
                {
                    case CompilationUnitSyntax compilationUnit:
                        builder.Add(new CompilationUnitSyntaxWrapper(compilationUnit));
                        break;

                    case NamespaceDeclarationSyntax ancestorNamespace:
                        builder.Add(new NamespaceDeclarationSyntaxWrapper(ancestorNamespace));
                        break;

                    case ClassDeclarationSyntax nestingClass:
                        builder.Add(new ClassDeclarationSyntaxWrapper(nestingClass));
                        break;
                    case StructDeclarationSyntax nestingStruct:
                        builder.Add(new StructDeclarationSyntaxWrapper(nestingStruct));
                        break;
                    case RecordDeclarationSyntax nestingRecord:
                        builder.Add(new RecordDeclarationSyntaxWrapper(nestingRecord));
                        break;
                }
            }

            return builder.ToImmutable();
        }
    }

    internal sealed class CompilationUnitSyntaxWrapper : ISyntaxWrapper
    {
        public CompilationUnitSyntaxWrapper(CompilationUnitSyntax compilationUnit)
        {
            if (compilationUnit.Externs.Count > 0)
                Externs = compilationUnit.Externs.Select(i => new ExternAliasDirectiveSyntaxWrapper(i)).ToList();
            if (compilationUnit.Usings.Count > 0)
                Usings = compilationUnit.Usings.Select(i => new UsingDirectiveSyntaxWrapper(i)).ToList();
        }

        public IReadOnlyList<ISyntaxWrapperSimple>? Externs { get; }
        public IReadOnlyList<ISyntaxWrapperSimple>? Usings { get; }

        public void WriteBegin(StringBuilder codeGenerator)
        {
            if (Externs is not null)
            {
                foreach (var i in Externs)
                    i.Write(codeGenerator);
                codeGenerator.AppendLine();
            }
            if (Usings is not null)
            {
                foreach (var i in Usings)
                    i.Write(codeGenerator);
                codeGenerator.AppendLine();
            }
        }

        public void WriteEnd(StringBuilder codeGenerator)
        {
            // nothing to write
        }
    }

    internal sealed class ExternAliasDirectiveSyntaxWrapper : ISyntaxWrapperSimple
    {
        public ExternAliasDirectiveSyntaxWrapper(ExternAliasDirectiveSyntax externAliasDirective)
        {
            Directive = externAliasDirective.WithoutTrivia().ToString();
        }

        public string Directive { get; }

        public void Write(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine(Directive);
        }
    }

    internal sealed class UsingDirectiveSyntaxWrapper : ISyntaxWrapperSimple
    {
        public UsingDirectiveSyntaxWrapper(UsingDirectiveSyntax usingDirective)
        {
            Directive = usingDirective.WithoutTrivia().ToString();
        }

        public string Directive { get; }

        public void Write(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine(Directive);
        }
    }

    internal sealed class NamespaceDeclarationSyntaxWrapper : ISyntaxWrapper
    {
        public NamespaceDeclarationSyntaxWrapper(NamespaceDeclarationSyntax ancestorNamespace)
        {
            Name = ancestorNamespace.Name.ToString();
            if (ancestorNamespace.Externs.Count > 0)
                Externs = ancestorNamespace.Externs.Select(i => new ExternAliasDirectiveSyntaxWrapper(i)).ToList();
            if (ancestorNamespace.Usings.Count > 0)
                Usings = ancestorNamespace.Usings.Select(i => new UsingDirectiveSyntaxWrapper(i)).ToList();
        }

        public string Name { get; }
        public IReadOnlyList<ISyntaxWrapperSimple>? Externs { get; }
        public IReadOnlyList<ISyntaxWrapperSimple>? Usings { get; }

        public void WriteBegin(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine($"namespace {Name}");
            codeGenerator.AppendLine("{");

            if (Externs is not null)
            {
                foreach (var i in Externs)
                    i.Write(codeGenerator);
                codeGenerator.AppendLine();
            }
            if (Usings is not null)
            {
                foreach (var i in Usings)
                    i.Write(codeGenerator);
                codeGenerator.AppendLine();
            }
        }

        public void WriteEnd(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine("}");
        }
    }

    internal abstract class TypeDeclarationSyntaxWrapper : ISyntaxWrapper
    {
        public static TypeDeclarationSyntaxWrapper Create(TypeDeclarationSyntax nestingClass)
        {
            return nestingClass switch
            {
                ClassDeclarationSyntax cd => new ClassDeclarationSyntaxWrapper(cd),
                StructDeclarationSyntax sd => new StructDeclarationSyntaxWrapper(sd),
                RecordDeclarationSyntax rd => new RecordDeclarationSyntaxWrapper(rd),
                _ => throw new InvalidOperationException()
            };
        }

        public TypeDeclarationSyntaxWrapper(TypeDeclarationSyntax nestingClass)
        {
            Identifier = nestingClass.Identifier.ToString();
            TypeParameterList = nestingClass.TypeParameterList?.ToString() ?? "";
        }

        public string Identifier { get; }
        public string TypeParameterList { get; }

        public abstract void WriteBegin(StringBuilder codeGenerator);

        public void WriteEnd(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine("}");
        }
    }

    internal sealed class ClassDeclarationSyntaxWrapper : TypeDeclarationSyntaxWrapper
    {
        public ClassDeclarationSyntaxWrapper(ClassDeclarationSyntax nestingClass)
            : base(nestingClass)
        {
        }

        public override void WriteBegin(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine($"partial class {Identifier}{TypeParameterList}");
            codeGenerator.AppendLine("{");
        }
    }

    internal sealed class StructDeclarationSyntaxWrapper : TypeDeclarationSyntaxWrapper
    {
        public StructDeclarationSyntaxWrapper(StructDeclarationSyntax nestingStruct)
            : base(nestingStruct)
        {
        }

        public override void WriteBegin(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine($"partial struct {Identifier}{TypeParameterList}");
            codeGenerator.AppendLine("{");
        }
    }

    internal sealed class RecordDeclarationSyntaxWrapper : TypeDeclarationSyntaxWrapper
    {
        public RecordDeclarationSyntaxWrapper(RecordDeclarationSyntax nestingRecord)
            : base(nestingRecord)
        {
            ClassOrStructKeyword = nestingRecord.ClassOrStructKeyword.Text;
        }

        public string ClassOrStructKeyword { get; }

        public override void WriteBegin(StringBuilder codeGenerator)
        {
            codeGenerator.AppendLine($"partial record {ClassOrStructKeyword} {Identifier}{TypeParameterList}");
            codeGenerator.AppendLine("{");
        }
    }
}
