using System;
using System.Reactive;
using Avalonia.Controls;
using MyJournal.Core;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.Views;
using ReactiveUI;
using Activity = MyJournal.Core.UserData.Activity;

namespace MyJournal.Desktop.Models;

public class MainWindowModel : ModelBase
{
	private User _user;
	private BaseVM _content;
	private bool _haveLeftDirection;
	private bool _haveRightDirection;

	public MainWindowModel(WelcomeVM startedContent, MainWindowView mainWindowView)
	{
		MessageBus.Current.Listen<ChangeMainWindowVMEventArgs>().Subscribe(onNext: args =>
		{
			Content = args.NewVM;
			HaveRightDirection = args.AnimationType == AnimationType.DirectionToLeft;
			HaveLeftDirection = args.AnimationType == AnimationType.DirectionToRight;
		});

		Minimize = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Minimized);
		Maximize = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Maximized);
		Restore = ReactiveCommand.Create(execute: () => mainWindowView.WindowState = WindowState.Normal);
		Close = ReactiveCommand.CreateFromTask(execute: async () =>
		{
			if (_user is not null)
			{
				Activity activity = await _user.GetActivity();
				await activity.SetOffline();
			}
			mainWindowView.Close();
		});
		Content = startedContent;
	}

	public ReactiveCommand<Unit, WindowState> Minimize { get; }
	public ReactiveCommand<Unit, WindowState> Maximize { get; }
	public ReactiveCommand<Unit, WindowState> Restore { get; }
	public ReactiveCommand<Unit, Unit> Close { get; }

	public BaseVM Content
	{
		get => _content;
		set => this.RaiseAndSetIfChanged(backingField: ref _content, newValue: value);
	}

	public bool HaveLeftDirection
	{
		get => _haveLeftDirection;
		set => this.RaiseAndSetIfChanged(backingField: ref _haveLeftDirection, newValue: value);
	}

	public bool HaveRightDirection
	{
		get => _haveRightDirection;
		set => this.RaiseAndSetIfChanged(backingField: ref _haveRightDirection, newValue: value);
	}

	public void SetUser(User user)
		=> _user = user;
}