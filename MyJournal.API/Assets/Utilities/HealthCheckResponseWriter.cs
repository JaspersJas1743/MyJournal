using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MyJournal.API.Assets.Utilities;

public static class HealthCheckResponseWriter
{
	public static async Task WriteResponse(HttpContext context, HealthReport healthReport)
	{
		context.Response.ContentType = "application/json; charset=utf-8";
		JsonWriterOptions options = new JsonWriterOptions { Indented = true };
		using MemoryStream memoryStream = new MemoryStream();
		await using Utf8JsonWriter jsonWriter = new Utf8JsonWriter(utf8Json: memoryStream, options: options);
		jsonWriter.WriteStartObject();
		jsonWriter.WriteString(propertyName: "Status", value: healthReport.Status.ToString());
		jsonWriter.WriteString(propertyName: "TotalDuration", value: healthReport.TotalDuration.ToString());
		jsonWriter.WriteStartObject(propertyName: "Entries");

		foreach (KeyValuePair<string, HealthReportEntry> healthReportEntry in healthReport.Entries)
		{
			jsonWriter.WriteStartObject(propertyName: healthReportEntry.Key);

			jsonWriter.WriteStartObject(propertyName: "Data");
			foreach (KeyValuePair<string, object> item in healthReportEntry.Value.Data)
			{
				jsonWriter.WritePropertyName(propertyName: item.Key);
				JsonSerializer.Serialize(writer: jsonWriter, value: item.Value, inputType: item.Value.GetType());
			}
			jsonWriter.WriteEndObject();

			jsonWriter.WriteString(propertyName: "Duration", value: healthReportEntry.Value.Duration.ToString());
			jsonWriter.WriteString(propertyName: "Status", value: healthReportEntry.Value.Status.ToString());
			jsonWriter.WriteString(propertyName: "Description", value: healthReportEntry.Value.Description);

			jsonWriter.WriteStartArray(propertyName: "Tags");
			foreach (string tag in healthReportEntry.Value.Tags)
				jsonWriter.WriteStringValue(tag);
			jsonWriter.WriteEndArray();

			jsonWriter.WriteEndObject();
		}

		jsonWriter.WriteEndObject();
		jsonWriter.WriteEndObject();
		await jsonWriter.DisposeAsync();

		await context.Response.WriteAsync(text: Encoding.UTF8.GetString(bytes: memoryStream.ToArray()));
	}
}