namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class FileControllerMethods
{
	public const string DownloadFile = "file/download";
	public const string DeleteFile = "file/delete";

	public static string UploadFile(string bucket) => $"file/{bucket}/upload";
}