using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using MyJournal.Desktop.Views;

namespace MyJournal.Desktop.Assets.Utilities.FileService;

public sealed class FileStorageService : IFileStorageService
{
	private readonly Window _target;
	private readonly IFileService _fileService;
	private readonly IConfigurationService _configurationService;

	public FileStorageService(Window target, IFileService fileService, IConfigurationService configurationService)
	{
		_target = target;
		_fileService = fileService;
		_configurationService = configurationService;
	}

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

	public async Task SaveFile(string url)
	{
		string folder = _configurationService.Get(key: ConfigurationKeys.StorageFolder)!;
		if (!Path.Exists(path: folder))
			Directory.CreateDirectory(path: folder);

		await _fileService.Download(link: url, pathToSave: folder);
	}
}

public static class FilesServiceExtensions
{
	public static IServiceCollection AddFileStorageService(this IServiceCollection serviceCollection)
	{
		return serviceCollection.AddTransient<IFileStorageService, FileStorageService>(
			implementationFactory: provider => new FileStorageService(
				target: provider.GetService<MainWindowView>()!,
				fileService: provider.GetService<IFileService>()!,
				configurationService: provider.GetService<IConfigurationService>()!
			)
		);
	}

	public static IServiceCollection AddKeyedFileStorageService(this IServiceCollection serviceCollection, string key)
	{
		return serviceCollection.AddKeyedTransient<IFileStorageService, FileStorageService>(serviceKey: key,
			implementationFactory: (provider, o) => new FileStorageService(
				target: provider.GetService<MainWindowView>()!,
				fileService: provider.GetService<IFileService>()!,
				configurationService: provider.GetService<IConfigurationService>()!
			)
		);
	}
}