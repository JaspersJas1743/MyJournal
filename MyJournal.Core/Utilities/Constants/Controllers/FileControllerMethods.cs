namespace MyJournal.Core.Utilities.Constants.Controllers;

internal static class FileControllerMethods
{
	internal const string DownloadFile = "file/download";
	internal const string DeleteFile = "file/delete";

	internal static string UploadFile(string bucket) => $"file/{bucket}/upload";
}