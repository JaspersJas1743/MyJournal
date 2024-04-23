using System;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Resources.Transitions;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Authorization;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class WelcomeModel : ModelBase
{
	private bool _haveLeftDirection;
	private bool _haveRightDirection;
	private BaseVM _content;

	public WelcomeModel(AuthorizationVM authorizationVM)
	{
		Content = authorizationVM;
		MessageBus.Current.Listen<ChangeWelcomeVMContentEventArgs>().Subscribe(onNext: args =>
		{
			Content = args.NewVM;
			HaveRightDirection = args.DirectionOfTransitionAnimation == PageTransition.Direction.Right;
			HaveLeftDirection = !HaveRightDirection;
		});
	}

	public BaseVM Content
	{
		get => _content;
		private set => this.RaiseAndSetIfChanged(backingField: ref _content, newValue: value);
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
}