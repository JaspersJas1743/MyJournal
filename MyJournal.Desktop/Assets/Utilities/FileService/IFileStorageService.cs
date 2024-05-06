using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace MyJournal.Desktop.Assets.Utilities.FileService;

public interface IFileStorageService
{
	public Task<IStorageFile?> OpenFile(IReadOnlyList<FilePickerFileType> fileTypes);
	public Task<IStorageFolder?> OpenFolder();
}