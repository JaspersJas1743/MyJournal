using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Input;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Models;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels;

public sealed class MessagesVM(MessagesModel model) : MenuItemVM(model: model)
{
	public ObservableCollectionExtended<ObservableChat> Chats
	{
		get => model.Chats;
		set => model.Chats = value;
	}

	public ObservableChat? SelectedChat
	{
		get => model.SelectedChat;
		set => model.SelectedChat = value;
	}

	public string? Subheader
	{
		get => model.Subheader;
		set => model.Subheader = value;
	}

	public string Filter
	{
		get => model.Filter;
		set => model.Filter = value;
	}

	public ReactiveCommand<KeyEventArgs, Unit> OnKeyDown => model.OnKeyDown;
	public ReactiveCommand<Unit, Unit> OnSelectionChanged => model.OnSelectionChanged;
	public ReactiveCommand<Unit, Unit> OnChatsLoaded => model.OnChatsLoaded;

	public override async Task SetUser(User user)
	{
		await model.SetUser(user: user);
	}
}