using System.Reactive;
using MyJournal.Desktop.Models.ChatCreation;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.ChatCreation;

public sealed class ChatCreationWindowVM(ChatCreationWindowModel model) : BaseVM(model: model)
{
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

	public bool HaveCrossFade
	{
		get => model.HaveCrossFade;
		set => model.HaveCrossFade = value;
	}

	public bool HaveRightDirection
	{
		get => model.HaveRightDirection;
		set => model.HaveRightDirection = value;
	}
}