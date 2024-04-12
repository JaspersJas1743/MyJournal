using System.Reactive;
using Avalonia.Controls;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.Views;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class MainWindowModel
{
	public MainWindowModel(MainViewModel model, MainWindow mainWindow)
	{
		MainViewModel = model;
		Minimize = ReactiveCommand.Create(execute: () => mainWindow.WindowState = WindowState.Minimized);
		Maximize = ReactiveCommand.Create(execute: () => mainWindow.WindowState = WindowState.Maximized);
		Restore = ReactiveCommand.Create(execute: () => mainWindow.WindowState = WindowState.Normal);
		Close = ReactiveCommand.Create(execute: mainWindow.Close);
	}

	public ReactiveCommand<Unit, WindowState> Minimize { get; }
	public ReactiveCommand<Unit, WindowState> Maximize { get; }
	public ReactiveCommand<Unit, WindowState> Restore { get; }
	public ReactiveCommand<Unit, Unit> Close { get; }
	public MainViewModel MainViewModel { get; }
}