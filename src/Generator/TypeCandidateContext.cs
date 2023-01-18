using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace Generator
{
    internal sealed class TypeCandidateContext
    {
        public static bool CanCreate(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax)
            {
                return true;
            }
            return false;
        }

        public static TypeCandidateContext Create(in GeneratorAttributeSyntaxContext context)
        {
            var node = (ClassDeclarationSyntax)context.TargetNode;
            return new TypeCandidateContext(node, context);
        }

        public TypeCandidateContext(ClassDeclarationSyntax syntax, in GeneratorAttributeSyntaxContext context)
        {
            Syntax = syntax;
        }

        public ClassDeclarationSyntax Syntax { get; }

        public void Generate(in SourceProductionContext ctx)
        {
            StringBuilder code = new StringBuilder();
            code.AppendLine($"// generated code for {Syntax.Identifier.Text}").AppendLine();

            var wrappers = SyntaxWrappers.LoadSyntaxAncestors(Syntax);

            foreach (var ancestor in wrappers.Reverse())
                ancestor.WriteBegin(code);

            code.AppendLine($"partial class {Syntax.Identifier.Text} {{");
            code.AppendLine($"}}");

            foreach (var ancestor in wrappers)
                ancestor.WriteEnd(code);


            string hintName = $"{Syntax.Identifier.Text}.{(Syntax.Arity > 0 ? $"`{Syntax.Arity}" : "")}.{Guid.NewGuid():N}.g"; // random name to prevent duplicates due to this issue
            ctx.AddSource(hintName, SourceText.From(code.ToString(), Encoding.UTF8));
        }
    }
}
