using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace MyJournal.API.Assets.Utilities.DisabledTokenFilter;

public record DisabledTokenModel(string Message);

public sealed class DisabledTokenResult : ObjectResult
{
	public DisabledTokenResult(string message)
		: base(value: new DisabledTokenModel(Message: message))
	{
		StatusCode = StatusCodes.Status401Unauthorized;
		ContentTypes.Add(item: MediaTypeNames.Application.Json);
	}
}