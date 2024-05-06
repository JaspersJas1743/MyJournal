using System.Reactive;
using Avalonia.Controls.Selection;
using DynamicData.Binding;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Models.ChatCreation;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.ChatCreation;

public sealed class MultiChatCreationVM(MultiChatCreationModel model) : BaseVM(model: model)
{
	public SelectionModel<ExtendedInterlocutor> Selection => model.Selection;

	public bool LoadingPhoto
	{
		get => model.LoadingPhoto;
		set => model.LoadingPhoto = value;
	}

	public ObservableCollectionExtended<ExtendedInterlocutor> Interlocutors
	{
		get => model.Interlocutors;
		set => model.Interlocutors = value;
	}

	public string? Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public string? ChatName
	{
		get => model.ChatName;
		set => model.ChatName = value;
	}

	public string? Photo
	{
		get => model.Photo;
		set => model.Photo = value;
	}

	public ReactiveCommand<Unit, Unit> LoadInterlocutors => model.LoadInterlocutors;
	public ReactiveCommand<Unit, Unit> CreateMultiChat => model.CreateMultiChat;
	public ReactiveCommand<Unit, Unit> CreateSingleChat => model.CreateSingleChat;
	public ReactiveCommand<Unit, Unit> LoadPhoto => model.LoadPhoto;
	public ReactiveCommand<Unit, Unit> DeletePhoto => model.DeletePhoto;
	public ReactiveCommand<Unit, Unit> OnAttachedToVisualTree => model.OnAttachedToVisualTree;
}