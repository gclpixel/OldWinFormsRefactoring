using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OldWinFormsRefactoring
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp,LanguageNames.VisualBasic, Name = nameof(OldWinFormsRefactoringCodeRefactoringProvider)), Shared]
	internal class OldWinFormsRefactoringCodeRefactoringProvider : CodeRefactoringProviderPlugin
	{
		public const string RefactoringId = "MoveTypeToFile";

		private ClassDeclarationSyntax typeDecl;

		public override async Task<bool> CanRefactorAsync(CodeRefactoringContext context)
		{
			SyntaxNode syntaxNode = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false)).FindNode(context.Span, false, false);
			this.typeDecl = (syntaxNode as ClassDeclarationSyntax);
			return this.typeDecl != null && this.typeDecl.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.ValueText == "InitializeComponent" && m.ParameterList.Parameters.Count == 0);
		}

		protected override async Task GetRefactoringsInternalAsync(CodeRefactoringContext context)
		{
			CodeAction codeAction = CodeAction.Create("Move Designer Code to Partial File", (CancellationToken c) => context.Document.MoveTypeToFileAsync(this.typeDecl, c), null);
			context.RegisterRefactoring(codeAction);
		}
	}
}