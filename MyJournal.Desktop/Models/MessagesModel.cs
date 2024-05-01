using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Input;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MessagesModel : ModelBase
{
	private User _user;
	private ObservableCollectionExtended<Chat> _chats = new ObservableCollectionExtended<Chat>();
	private Chat? _selectedChat;
	private string? _subheader = "123";

	public MessagesModel()
	{
		OnKeyDown = ReactiveCommand.Create<KeyEventArgs>(execute: KeyDownHandler);
		OnSelectionChanged = ReactiveCommand.CreateFromTask(execute: SelectionChangedHandler);
	}

	private async Task SelectionChangedHandler()
	{
		Subheader = SelectedChat?.IsSingleChat switch
		{
			true => GetTimeOfOnlineOfInterlocutor(),
			false => GetCountOfParticipants(),
			_ => String.Empty
		};
	}

	public ReactiveCommand<KeyEventArgs, Unit> OnKeyDown { get; }
	public ReactiveCommand<Unit, Unit> OnSelectionChanged { get; }

	public ObservableCollectionExtended<Chat> Chats
	{
		get => _chats;
		set => this.RaiseAndSetIfChanged(backingField: ref _chats, newValue: value);
	}

	public Chat? SelectedChat
	{
		get => _selectedChat;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedChat, newValue: value);
	}

	public string? Subheader
	{
		get => _subheader;
		set => this.RaiseAndSetIfChanged(backingField: ref _subheader, newValue: value);
	}

	public async Task SetUser(User user)
	{
		_user = user;
		ChatCollection chats = await _user.GetChats();
		Chats.Load(items: await chats.ToListAsync());
	}

	private void KeyDownHandler(KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
			SelectedChat = null;
	}

	private string? GetTimeOfOnlineOfInterlocutor()
	{

		return SelectedChat?.InterlocutorOnlineAt!.Value.ToString("F");
	}

	private string GetCountOfParticipants()
	{
		return SelectedChat?.CountOfParticipants + " участников";
	}
}