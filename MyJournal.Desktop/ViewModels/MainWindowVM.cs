using System.Reactive;
using Avalonia.Controls;
using MyJournal.Core;
using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public sealed class MainWindowVM(MainWindowModel model) : BaseVM(model: model)
{
	public ReactiveCommand<Unit, WindowState> Minimize => model.Minimize;
	public ReactiveCommand<Unit, WindowState> Maximize => model.Maximize;
	public ReactiveCommand<Unit, WindowState> Restore => model.Restore;
	public ReactiveCommand<Unit, Unit> Close => model.Close;

	public BaseVM Content
	{
		get => model.Content;
		set => model.Content = value;
	}

	public bool HaveLeftDirection
	{
		get => model.HaveLeftDirection;
		set => model.HaveLeftDirection = value;
	}

	public bool HaveRightDirection
	{
		get => model.HaveRightDirection;
		set => model.HaveRightDirection = value;
	}

	public void SetUser(User user)
		=> model.SetUser(user: user);
}