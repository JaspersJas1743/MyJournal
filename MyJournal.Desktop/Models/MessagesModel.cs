using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MessagesModel : ModelBase
{
	private bool _isLoaded = false;
	private ChatCollection _chatCollection;
	private ObservableCollectionExtended<Chat> _chats = new ObservableCollectionExtended<Chat>();
	private Chat? _selectedChat;
	private string? _subheader = String.Empty;
	private string _filter = String.Empty;

	public MessagesModel()
	{
		OnKeyDown = ReactiveCommand.Create<KeyEventArgs>(execute: KeyDownHandler);
		OnSelectionChanged = ReactiveCommand.CreateFromTask(execute: SelectionChangedHandler);
		OnFilterChanged = ReactiveCommand.CreateFromTask<string>(execute: FilterChangedHandler);
		OnChatsLoaded = ReactiveCommand.CreateFromTask(execute: ChatsLoadedHandler);

		this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.5)).InvokeCommand(command: OnFilterChanged);
	}

	public ReactiveCommand<KeyEventArgs, Unit> OnKeyDown { get; }
	public ReactiveCommand<Unit, Unit> OnSelectionChanged { get; }
	public ReactiveCommand<string, Unit> OnFilterChanged { get; }
	public ReactiveCommand<Unit, Unit> OnChatsLoaded { get; }

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

	public string Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	public async Task SetUser(User user)
	{
		_chatCollection = await user.GetChats();
		Chats.Load(items: await _chatCollection.ToListAsync());
		_isLoaded = !_isLoaded;
	}

	private void KeyDownHandler(KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
			SelectedChat = null;
	}

	private string? GetTimeOfOnlineOfInterlocutor()
	{
		if (SelectedChat!.InterlocutorOnlineAt is null)
			return "в сети";

		return "был(-а) в сети " + SelectedChat!.InterlocutorOnlineAt.Humanize(culture: CultureInfo.CurrentUICulture);
	}

	private string GetCountOfParticipants()
		=> WordFormulator.GetForm(count: SelectedChat!.CountOfParticipants, forms: new string[] { "участников", "участник", "участника" });

	private async Task FilterChangedHandler(string filter)
	{
		if (!_isLoaded)
			return;

		if (filter == _chatCollection!.Filter)
			return;

		await _chatCollection.SetFilter(filter: filter)!;
		Chats.Load(items: await _chatCollection.ToListAsync());
	}

	private async Task SelectionChangedHandler()
	{
		Subheader = SelectedChat?.IsSingleChat switch
		{
			true => GetTimeOfOnlineOfInterlocutor(),
			false => GetCountOfParticipants(),
			_ => String.Empty
		};

		if (SelectedChat?.LastMessage?.IsRead == false)
		{
			await SelectedChat?.Read()!;
			SelectedChat.LastMessage.IsRead = true;
		}
	}

	private async Task ChatsLoadedHandler()
	{
		if (_chatCollection.AllItemsAreUploaded)
			return;

		int currentLength = _chatCollection.Length;
		await _chatCollection.LoadNext();
		Chats.Add(items: await _chatCollection.GetByRange(start: currentLength, end: _chatCollection.Length));
	}
}