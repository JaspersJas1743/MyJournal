using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Input;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Models;
using ReactiveUI;
using Attachment = MyJournal.Desktop.Assets.Utilities.ChatUtilities.Attachment;

namespace MyJournal.Desktop.ViewModels;

public sealed class MessagesVM(MessagesModel model) : MenuItemVM(model: model)
{
	public ObservableCollectionExtended<ObservableChat> Chats
	{
		get => model.Chats;
		set => model.Chats = value;
	}

	public ObservableCollectionExtended<Attachment> Attachments
	{
		get => model.Attachments;
		set => model.Attachments = value;
	}

	public ObservableCollectionExtended<Message> Messages
	{
		get => model.Messages;
		set => model.Messages = value;
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

	public string Message
	{
		get => model.Message;
		set => model.Message = value;
	}

	public bool ChatsAreLoaded
	{
		get => model.ChatsAreLoaded;
		set => model.ChatsAreLoaded = value;
	}

	public Message? SelectedMessage
	{
		get => model.SelectedMessage;
		set => model.SelectedMessage = value;
	}

	public ReactiveCommand<KeyEventArgs, Unit> OnKeyDown => model.OnKeyDown;
	public ReactiveCommand<Unit, Unit> OnSelectionChanged => model.OnSelectionChanged;
	public ReactiveCommand<Unit, Unit> OnChatsLoaded => model.OnChatsLoaded;
	public ReactiveCommand<Unit, Unit> AppendAttachment => model.AppendAttachment;
	public ReactiveCommand<Unit, Unit> SendMessage => model.SendMessage;

	public override async Task SetUser(User user)
	{
		await model.SetUser(user: user);
	}
}