using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MyJournal.API.Assets.DatabaseModels;
using MyJournal.API.Assets.ExceptionHandlers;
using MyJournal.API.Assets.S3;
using MyJournal.API.Assets.Validation;
using MyJournal.API.Assets.Validation.Validators;

namespace MyJournal.API.Assets.Controllers;

[Authorize]
[ApiController]
[Route(template: "api/file")]
public sealed class FileController(
	MyJournalContext context,
	IFileStorageService fileStorageService
) : MyJournalBaseController(context: context)
{
	private readonly MyJournalContext _context = context;

	#region Records
	[Validator<FileRequestValidator>]
	public sealed record FileRequest(IFormFile File);

	[Validator<FileLinkValidator>]
	public sealed record FileLink(string Link);
	#endregion

	#region Methods
	#region GET
	/// <summary>
	/// Сохранить файл по ссылке
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	GET api/file/download?Link=https%3A%2F%2Fmyjournal_assets.hb.ru-msk.vkcs.cloud%2Fabd3618c-2099-4738-bc9b-8bbc215090b9.jpg
	///
	/// Параметры:
	///
	///	Link - ссылка на файл для сохранения
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Чат отмечен как прочитанный</response>
	/// <response code="400">Некорректные входные данные</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpGet(template: "download")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<FileLink>> DownloadFile(
		[FromQuery] FileLink request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		FileInfo fileInfo = new FileInfo(fileName: request.Link);
		string fileKey = $"{fileInfo.Directory?.Name}/{fileInfo.Name}";
		Stream file = await fileStorageService.GetFileAsync(
			key: fileKey,
			cancellationToken: cancellationToken
		);
		string fileExtension = fileInfo.Extension.Trim(trimChar: '.');

		if (!new FileExtensionContentTypeProvider().TryGetContentType(subpath: fileInfo.Name, contentType: out string? contentType))
			contentType = "application/octet-stream";

		return File(fileStream: file, contentType: contentType, fileDownloadName: $"file_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{fileExtension}");
	}
	#endregion

	#region PUT
	/// <summary>
	/// Сохранить файл на сервере
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	PUT api/file/{bucket}/upload
	///
	/// Параметры:
	///
	///	bucket - имя папки, в которую будет сохранён файл
	///	File - файл для загрузки на сервер
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Ссылка на загруженный файл</response>
	/// <response code="400">Некорректные входные данные</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpPut(template: "{bucket}/upload")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(FileLink))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult<FileLink>> UploadFile(
		[FromRoute] string bucket,
		[FromForm] FileRequest request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string fileExtension = Path.GetExtension(path: request.File.FileName);
		string fileKey = $"{bucket}/{Guid.NewGuid()}{fileExtension}";
		string link = await fileStorageService.UploadFileAsync(
			key: fileKey,
			fileStream: request.File.OpenReadStream(),
			cancellationToken: cancellationToken
		);
		return Ok(value: new FileLink(Link: link));
	}
	#endregion

	#region DELETE
	/// <summary>
	/// Удалить файл с сервера
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Пример запроса к API:
	///
	///	DELETE api/file/delete
	///
	/// Параметры:
	///
	///	Link - ссылка на файл для удаления с сервера
	///
	/// ]]>
	/// </remarks>
	/// <response code="200">Файл удален успешно</response>
	/// <response code="400">Некорректные входные данные</response>
	/// <response code="401">Пользователь не авторизован или авторизационный токен неверный</response>
	[HttpDelete(template: "delete")]
	[Produces(contentType: MediaTypeNames.Application.Json)]
	[ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
	[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ErrorResponse))]
	[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ErrorResponse))]
	public async Task<ActionResult> DeleteFile(
		[FromQuery] FileLink request,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		FileInfo file = new FileInfo(fileName: request.Link);
		string fileKey = $"{file.Directory!.Name}/{file.Name}";
		await fileStorageService.DeleteFileAsync(
			key: fileKey,
			cancellationToken: cancellationToken
		);
		return Ok();
	}
	#endregion
	#endregion
}