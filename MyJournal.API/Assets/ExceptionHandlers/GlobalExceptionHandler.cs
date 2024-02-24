using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyJournal.API.Assets.Utilities;

namespace MyJournal.API.Assets.ExceptionHandlers;

public record ErrorResponse(string Message);

public sealed class GlobalExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext context,
		Exception exception,
		CancellationToken cancellationToken
	)
	{
		ProblemDetails problemDetails = new ProblemDetails()
		{
			Title = "BadRequest",
			Status = StatusCodes.Status400BadRequest,
			Detail = exception.Message
		};

		if (exception is HttpResponseException ex)
		{
			problemDetails.Status = ex.StatusCode;
			problemDetails.Title = ((HttpStatusCode)ex.StatusCode).ToString();
		}

		context.Response.StatusCode = problemDetails.Status.Value;
		await context.Response.WriteAsJsonAsync(
			value: new ErrorResponse(Message: problemDetails.Detail),
			options: new JsonSerializerOptions()
			{
				WriteIndented = true,
				PropertyNamingPolicy = null
			},
			cancellationToken: cancellationToken
		);
		return true;
	}
}