using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Input;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MessagesModel : ModelBase
{
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 30));

	private User? _user;
	private ChatCollection _chatCollection;
	private ObservableCollectionExtended<ObservableChat> _chats = new ObservableCollectionExtended<ObservableChat>();
	private ObservableCollectionExtended<string> _attachments = new ObservableCollectionExtended<string>();
	private ObservableChat? _selectedChat;
	private bool _isLoaded = false;
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

		Chats.CollectionChanged += OnChatsChanged;
		_timer.Elapsed += OnTimerElapsed;
		_timer.Start();
	}

	~MessagesModel()
		=> _timer.Stop();

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
		=> Subheader = SelectedChat?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

	public ReactiveCommand<KeyEventArgs, Unit> OnKeyDown { get; }
	public ReactiveCommand<Unit, Unit> OnSelectionChanged { get; }
	public ReactiveCommand<string, Unit> OnFilterChanged { get; }
	public ReactiveCommand<Unit, Unit> OnChatsLoaded { get; }

	public ObservableCollectionExtended<ObservableChat> Chats
	{
		get => _chats;
		set => this.RaiseAndSetIfChanged(backingField: ref _chats, newValue: value);
	}

	public ObservableChat? SelectedChat
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
		_user = user;
		Task<InterlocutorCollection> task = _user.GetInterlocutors();
		_chatCollection = await user.GetChats();
		List<Chat> chats = await _chatCollection.ToListAsync();
		Chats.Load(items: chats.Select(selector: chat => chat.ToObservable()));
		_isLoaded = !_isLoaded;
		InterlocutorCollection interlocutors = await task;

		_user.JoinedInChat += OnUserJoinedInChat;
		interlocutors.InterlocutorAppearedOnline += CurrentInterlocutorOnAppearedOnline;
		interlocutors.InterlocutorAppearedOffline += CurrentInterlocutorOnAppearedOffline;
	}

	private async void OnUserJoinedInChat(JoinedInChatEventArgs e)
		=> Chats.Insert(index: 0, item: (await _chatCollection.FindById(id: e.ChatId))!.ToObservable());

	private void KeyDownHandler(KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
			SelectedChat = null;
	}

	private string? GetTimeOfOnlineOfInterlocutor()
	{
		if (SelectedChat!.OnlineAt is null)
			return "в сети";

		if ((DateTime.Now - SelectedChat!.OnlineAt).Value.Minutes < 1)
			return "был(-а) в сети только что";

		return "был(-а) в сети " + SelectedChat!.OnlineAt.Humanize(culture: CultureInfo.CurrentUICulture);
	}

	private string GetCountOfParticipants()
	{
		if (SelectedChat is null)
			return String.Empty;

		return WordFormulator.GetForm(count: SelectedChat!.CountOfParticipants, forms: new string[] { "участников", "участник", "участника" });
	}

	private async Task FilterChangedHandler(string filter)
	{
		if (!_isLoaded || filter == _chatCollection.Filter)
			return;

		await _chatCollection.SetFilter(filter: filter)!;
		List<Chat> chats = await _chatCollection.ToListAsync();
		Chats.Load(items: chats.Select(selector: chat => chat.ToObservable()));
	}

	private async Task SelectionChangedHandler()
	{
		Subheader = String.Empty;

		Subheader = SelectedChat.IsSingleChat ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

		if (SelectedChat.IsRead == false)
		{
			await SelectedChat?.Read()!;
			SelectedChat.IsRead = true;
		}
	}

	private async Task ChatsLoadedHandler()
	{
		if (_chatCollection.AllItemsAreUploaded)
			return;

		int currentLength = _chatCollection.Length;
		await _chatCollection.LoadNext();
		IEnumerable<Chat> chats = await _chatCollection.GetByRange(start: currentLength, end: _chatCollection.Length);
		Chats.Add(items: chats.Select(selector: chat => chat.ToObservable()));
	}

	private async void OnChatsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action != NotifyCollectionChangedAction.Add)
			return;

		ObservableChat addedItem = e.NewItems!.OfType<ObservableChat>().Single();
		await addedItem.LoadInterlocutor(user: _user);
	}

	private void CurrentInterlocutorOnAppearedOffline(InterlocutorAppearedOfflineEventArgs e)
		=> Subheader = SelectedChat?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

	private void CurrentInterlocutorOnAppearedOnline(InterlocutorAppearedOnlineEventArgs e)
		=> Subheader = SelectedChat?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();
}