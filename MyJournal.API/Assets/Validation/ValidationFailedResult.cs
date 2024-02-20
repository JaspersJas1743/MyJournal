using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyJournal.API.Assets.Validation;

public sealed class ValidationResultModel(ModelStateDictionary modelState)
{
	public string Message { get; } = modelState.Values.First().Errors.First().ErrorMessage;
}

public sealed class ValidationFailedResult : ObjectResult
{
	public ValidationFailedResult(ModelStateDictionary modelState)
		: base(value: new ValidationResultModel(modelState: modelState))
	{
		StatusCode = StatusCodes.Status400BadRequest;
	}
}
