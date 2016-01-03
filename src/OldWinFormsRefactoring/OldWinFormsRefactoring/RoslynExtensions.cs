using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OldWinFormsRefactoring
{
	public static class RoslynExtensions
	{
		public static Func<MemberDeclarationSyntax, bool> MoveTypeToFileFunction;

		public static async Task<Solution> MoveTypeToFileAsync(this Document document, ClassDeclarationSyntax typeDecl, CancellationToken cancellationToken = default(CancellationToken))
		{
			CompilationUnitSyntax compilationUnitSyntax = (CompilationUnitSyntax)(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
			IEnumerable<MemberDeclarationSyntax> arg_B5_0 = compilationUnitSyntax.Members;
			Func<MemberDeclarationSyntax, bool> arg_B5_1;
			if ((arg_B5_1 = MoveTypeToFileFunction) == null)
			{
				arg_B5_1 = (MoveTypeToFileFunction = new Func<MemberDeclarationSyntax, bool>(m => Microsoft.CodeAnalysis.CSharpExtensions.IsKind(m, SyntaxKind.NamespaceDeclaration)));
			}

			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			MethodDeclarationSyntax initializeComponentMethod = typeDecl.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "InitializeComponent" && m.ParameterList.Parameters.Count == 0);

			var localVars = typeDecl.DescendantNodes().OfType<FieldDeclarationSyntax>().ToDictionary(fds => fds.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Identifier.Text);

			var simpleAssigns = initializeComponentMethod.Body.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(mea => mea.Kind() == SyntaxKind.SimpleMemberAccessExpression && mea.ChildNodes().OfType<ThisExpressionSyntax>().Any()).SelectMany(id => id.DescendantNodes().OfType<IdentifierNameSyntax>()).Select(i => i.Identifier.Text).Distinct().ToList();

			NamespaceDeclarationSyntax namespaceDeclarationSyntax = (NamespaceDeclarationSyntax)arg_B5_0.Single(arg_B5_1);
			Solution solution = document.Project.Solution;

			DocumentId documentId = DocumentId.CreateNewId(document.Project.Id, null);

			List<SyntaxNode> toBeDeleted = new List<SyntaxNode>();
			toBeDeleted.Add(initializeComponentMethod);

			foreach (var item in localVars)
			{
				if (simpleAssigns.Contains(item.Key))
				{
					toBeDeleted.Add(item.Value);
				}
			}

			var oldType = SyntaxNodeExtensions.RemoveNodes(typeDecl, toBeDeleted, SyntaxRemoveOptions.KeepDirectives).AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

			solution = document.ReplaceFromDocumentAsync(typeDecl, oldType, cancellationToken).Result.Project.Solution;

			var modifiers = SyntaxFactory.TokenList(new SyntaxToken[] { SyntaxFactory.Token(SyntaxKind.PartialKeyword) });
			var newType = SyntaxFactory.ClassDeclaration(typeDecl.Identifier).WithModifiers(modifiers);
			var method1 = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "InitializeComponent");
			method1 = method1.WithBody(initializeComponentMethod.Body);
			newType = newType.AddMembers(method1);

			foreach (var item in localVars)
			{
				if (simpleAssigns.Contains(item.Key))
				{
					var varMod = SyntaxFactory.TokenList(item.Value.Modifiers.Select(t => SyntaxFactory.Token(t.Kind())).ToArray());
					newType = newType.AddMembers(SyntaxFactory.FieldDeclaration(item.Value.AttributeLists, varMod, item.Value.Declaration));
				}
			}

			solution = solution.AddDocument(documentId, Path.GetFileNameWithoutExtension(document.Name) + ".designer.cs", newType.GetText(), document.Folders, null, true);
			CompilationUnitSyntax compilationUnitSyntax2 = SyntaxFactory.CompilationUnit().AddUsings(compilationUnitSyntax.Usings.ToArray()).AddMembers(new MemberDeclarationSyntax[]
			{
				SyntaxNodeExtensions.WithLeadingTrivia(SyntaxNodeExtensions.NormalizeWhitespace(SyntaxFactory.NamespaceDeclaration(namespaceDeclarationSyntax.Name), "    ", false), new SyntaxTrivia[]
				{
					SyntaxFactory.Whitespace("\r\n")
				}).AddMembers(new MemberDeclarationSyntax[] { newType })
			});
			return solution.WithDocumentSyntaxRoot(documentId, compilationUnitSyntax2, 0);
		}

		public static async Task<Document> RemoveFromDocumentAsync(this Document document, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			SyntaxNode syntaxNode = SyntaxNodeExtensions.RemoveNode(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false), node, SyntaxRemoveOptions.KeepDirectives);
			return document.WithSyntaxRoot(syntaxNode);
		}

		public static async Task<Document> ReplaceFromDocumentAsync(this Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken cancellationToken = default(CancellationToken))
		{
			SyntaxNode syntaxNode = SyntaxNodeExtensions.ReplaceNode(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false), oldNode, newNode);
			return document.WithSyntaxRoot(syntaxNode);
		}
	}
}