using System;
using System.Reactive;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ChatCreationService;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.ChatCreation;
using ReactiveUI;

namespace MyJournal.Desktop.Models.ChatCreation;

public class ChatCreationWindowModel : ModelBase
{
	private bool _haveLeftDirection;
	private bool _haveRightDirection;
	private bool _haveCrossFade;
	private BaseVM _content;

	public ChatCreationWindowModel(SingleChatCreationVM singleChatCreationVM)
	{
		MessageBus.Current.Listen<ChangeChatCreationVMContentEventArgs>().Subscribe(onNext: args =>
		{
			Content = args.NewVM;
			HaveCrossFade = args.AnimationType == AnimationType.CrossFade;
			HaveRightDirection = args.AnimationType == AnimationType.DirectionToLeft;
			HaveLeftDirection = args.AnimationType == AnimationType.DirectionToRight;
		});

		Content = singleChatCreationVM;
		Close = ReactiveCommand.Create(execute: OnWindowClosed);
	}

	public ReactiveCommand<Unit, Unit> Close { get; }

	public BaseVM Content
	{
		get => _content;
		set => this.RaiseAndSetIfChanged(backingField: ref _content, newValue: value);
	}

	public bool HaveCrossFade
	{
		get => _haveCrossFade;
		set => this.RaiseAndSetIfChanged(backingField: ref _haveCrossFade, newValue: value);
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

	private void OnWindowClosed()
		=> IChatCreationService.Instance?.Close(dialogResult: false);
}