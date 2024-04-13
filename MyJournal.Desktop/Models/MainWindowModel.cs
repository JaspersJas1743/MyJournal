using System.Reactive;
using Avalonia.Controls;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.Views;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class MainWindowModel(WelcomeVM model, MainWindowView mainWindowView)
{
	public ReactiveCommand<Unit, WindowState> Minimize { get; } = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Minimized);
	public ReactiveCommand<Unit, WindowState> Maximize { get; } = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Maximized);
	public ReactiveCommand<Unit, WindowState> Restore { get; } = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Normal);
	public ReactiveCommand<Unit, Unit> Close { get; } = ReactiveCommand.Create(execute: mainWindowView.Close);
	public WelcomeVM WelcomeVM { get; } = model;
}