using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Views;

namespace MyJournal.Desktop.Assets.Utilities.FileService;

public sealed class FileStorageService : IFileStorageService
{
	private readonly Window _target;

	public FileStorageService(Window target)
		=> _target = target;

	public async Task<IStorageFile?> OpenFile()
	{
		IReadOnlyList<IStorageFile> files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
		{
			Title = "Открыть...",
			AllowMultiple = false
		});

		return files.Count >= 1 ? files[0] : null;
	}

	public async Task<IStorageFolder?> OpenFolder()
	{
		IReadOnlyList<IStorageFolder> folders = await _target.StorageProvider.OpenFolderPickerAsync(options: new FolderPickerOpenOptions()
		{
			Title = "Открыть...",
			AllowMultiple = false
		});

		return folders.Count >= 1 ? folders[0] : null;
	}
}

public static class FilesServiceExtensions
{
	public static IServiceCollection AddFileStorageService(this IServiceCollection serviceCollection)
	{
		return serviceCollection.AddTransient<IFileStorageService, FileStorageService>(
			implementationFactory: provider => new FileStorageService(target: provider.GetService<MainWindowView>()!)
		);
	}

	public static IServiceCollection AddKeyedFileStorageService(this IServiceCollection serviceCollection, string key)
	{
		return serviceCollection.AddKeyedTransient<IFileStorageService, FileStorageService>(serviceKey: key,
			implementationFactory: (provider, o) => new FileStorageService(target: provider.GetService<MainWindowView>()!)
		);
	}
}