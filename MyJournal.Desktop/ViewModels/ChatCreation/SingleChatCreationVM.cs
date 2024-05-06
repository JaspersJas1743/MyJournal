using System.Reactive;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Models.ChatCreation;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.ChatCreation;

public sealed class SingleChatCreationVM(SingleChatCreationModel model) : BaseVM(model: model)
{
	public ObservableCollectionExtended<ExtendedInterlocutor> Interlocutors
	{
		get => model.Interlocutors;
		set => model.Interlocutors = value;
	}

	public ExtendedInterlocutor Interlocutor
	{
		get => model.Interlocutor;
		set => model.Interlocutor = value;
	}

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public ReactiveCommand<Unit, Unit> LoadInterlocutors => model.LoadInterlocutors;
	public ReactiveCommand<Unit, Unit> SelectIntendedInterlocutor => model.SelectIntendedInterlocutor;
	public ReactiveCommand<Unit, Unit> CreateMultiChat => model.CreateMultiChat;
	public ReactiveCommand<Unit, Unit> OnAttachedToVisualTree => model.OnAttachedToVisualTree;
}