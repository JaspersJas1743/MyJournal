using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Models;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.Views;

namespace MyJournal.Desktop;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App()
	{
		_services = new ServiceCollection()
			.AddSingleton<MainWindowModel>()
			.AddSingleton<MainWindow>()
			.AddSingleton<MainView>()
			.AddSingleton<MainWindowView>()
			.AddSingleton<MainViewModel>()
			.AddSingleton<MainWindowViewModel>()
			.BuildServiceProvider();
	}

	public T GetService<T>()
	{
		return _services.GetService<T>() ??
			throw new ArgumentException(message: $"Сервис типа {typeof(T)} не найден.", paramName: nameof(T));
	}

	public object? GetService(Type serviceType)
	{
		return _services.GetService(serviceType: serviceType) ??
			throw new ArgumentException(message: $"Сервис типа {serviceType} не найден.", paramName: nameof(serviceType));
	}

	public override void Initialize()
		=> AvaloniaXamlLoader.Load(obj: this);

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = GetService<MainWindow>();
			desktop.MainWindow!.DataContext = GetService<MainWindowViewModel>();
		}

		base.OnFrameworkInitializationCompleted();
	}
}