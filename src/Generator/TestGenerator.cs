using Microsoft.CodeAnalysis;

namespace Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed partial class TestGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TypeCandidateContext> typeCandidateContexts = context.SyntaxProvider.ForAttributeWithMetadataName("Generator.GenerateAttribute",
                static (node, _) => TypeCandidateContext.CanCreate(node),
                static (context, _) => TypeCandidateContext.Create(in context)
                );

            context.RegisterSourceOutput(typeCandidateContexts, (ctx, node) =>
            {
                node.Generate(ctx);
            });
        }
    }
}
