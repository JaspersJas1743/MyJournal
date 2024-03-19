using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MyJournal.API.Assets.DatabaseModels;
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
	[HttpGet(template: "download")]
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
	[HttpPut(template: "{bucket}/upload")]
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
	[HttpDelete(template: "delete")]
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