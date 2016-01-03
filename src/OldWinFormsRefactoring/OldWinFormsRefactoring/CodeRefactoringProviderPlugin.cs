using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace OldWinFormsRefactoring
{
	public abstract class CodeRefactoringProviderPlugin : CodeRefactoringProvider
	{
		public abstract Task<bool> CanRefactorAsync(CodeRefactoringContext context);

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			if (await this.CanRefactorAsync(context))
			{
				await this.GetRefactoringsInternalAsync(context);
			}
		}

		protected abstract Task GetRefactoringsInternalAsync(CodeRefactoringContext context);
	}
}