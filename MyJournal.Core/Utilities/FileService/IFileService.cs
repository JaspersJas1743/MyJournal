using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.Utilities.FileService;

public interface IFileService
{
	ApiClient ApiClient { get; set; }

	Task Download(
		string? link,
		string pathToSave,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task<string?> Upload(
		string folderToSave,
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task Delete(
		string? link,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}