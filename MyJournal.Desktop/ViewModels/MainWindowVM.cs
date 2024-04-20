using System.Reactive;
using Avalonia.Controls;
using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public sealed class MainWindowVM(MainWindowModel windowModel) : BaseVM
{
	public ReactiveCommand<Unit, WindowState> Minimize => windowModel.Minimize;
	public ReactiveCommand<Unit, WindowState> Maximize => windowModel.Maximize;
	public ReactiveCommand<Unit, WindowState> Restore => windowModel.Restore;
	public ReactiveCommand<Unit, Unit> Close => windowModel.Close;

	public BaseVM MainVM
	{
		get => windowModel.MainVM;
		set => windowModel.MainVM = value;
	}
}