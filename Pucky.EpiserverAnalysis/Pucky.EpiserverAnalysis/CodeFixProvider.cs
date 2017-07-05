using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Pucky.EpiserverAnalysis
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PuckyEpiserverAnalysisCodeFixProvider)), Shared]
    public class PuckyEpiserverAnalysisCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add [CultureSpecific]";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PuckyEpiserverAnalysisAnalyzer.DiagnosticId); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var ancestors = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf();
            var declaration = ancestors.OfType<PropertyDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => UsingCultureSpecificAttributeAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        async Task<Solution> UsingCultureSpecificAttributeAsync(Document document, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            root = AddCultureSpecificAttribute(root, propertyDeclaration);

            root = AddUsingStatementIfNecessary(root);

            return document.WithSyntaxRoot(root).Project.Solution;
            
        }

        SyntaxNode AddCultureSpecificAttribute(SyntaxNode root, PropertyDeclarationSyntax propertyDeclaration)
        {
            var identifierToken = propertyDeclaration.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();
            var attributes = propertyDeclaration.AttributeLists.Add(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("CultureSpecific"))
                )));

            return root.ReplaceNode(
                    propertyDeclaration,
                    propertyDeclaration.WithAttributeLists(attributes));
        }

        SyntaxNode AddUsingStatementIfNecessary(SyntaxNode root)
        {
            var compilation =
            root as CompilationUnitSyntax;

            var EpiserverAnnotationsUsingName =
            SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName("EPiServer"),
                SyntaxFactory.IdentifierName("DataAnnotations"));

            if (!AnnotationUsingStatementExists(compilation))
            {
                return root.InsertNodesAfter(compilation.Usings.Last(), new[]{
                    SyntaxFactory.UsingDirective(
                            EpiserverAnnotationsUsingName)
                });
            }

            return root;
        }

        bool AnnotationUsingStatementExists(CompilationUnitSyntax compilation)
            => compilation.Usings.Any(u => u.Name.GetText().ToString() == "EPiServer.DataAnnotations");
    }
}