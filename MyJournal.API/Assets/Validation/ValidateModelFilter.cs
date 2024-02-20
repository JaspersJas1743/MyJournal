using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyJournal.API.Assets.Validation;

public sealed class ValidateModelFilter: IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context)
	{
		if (context.ModelState.IsValid)
			return;

		context.Result = new ValidationFailedResult(modelState: context.ModelState);
	}

	public void OnActionExecuted(ActionExecutedContext context) { }
}

public static class ValidateModelFilterExtension
{
	public static MvcOptions AddValidateModeFilter(this MvcOptions options)
	{
		options.Filters.Add<ValidateModelFilter>();
		return options;
	}
}