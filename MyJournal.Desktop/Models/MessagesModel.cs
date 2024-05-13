using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using MyJournal.Core;
using MyJournal.Core.Builders.MessageBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ChatCreationService;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using MyJournal.Desktop.Assets.Utilities.FileService;
using MyJournal.Desktop.Assets.Utilities.NotificationService;
using ReactiveUI;
using Attachment = MyJournal.Desktop.Assets.Utilities.ChatUtilities.Attachment;

namespace MyJournal.Desktop.Models;

public sealed class MessagesModel : ModelBase
{
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 30));
	private readonly IFileStorageService _fileStorageService;
	private readonly IConfigurationService _configurationService;
	private readonly IChatCreationService _chatCreationService;
	private readonly INotificationService _notificationService;

	private int _previousChatId = -1;
	private readonly Dictionary<int, string?> _allMessages = new Dictionary<int, string?>();
	private readonly Dictionary<int, ObservableCollectionExtended<Attachment>?> _allAttachments = new Dictionary<int, ObservableCollectionExtended<Attachment>?>();
	private readonly Dictionary<int, IMessageBuilder?> _allMessageBuilders = new Dictionary<int, IMessageBuilder?>();

	private User? _user;
	private ChatCollection _chatCollection;
	private MessageCollection? _messageFromSelectedChat;
	private IMessageBuilder? _messageBuilder;
	private ObservableCollectionExtended<ObservableChat> _chats = new ObservableCollectionExtended<ObservableChat>();
	private ObservableCollectionExtended<Attachment>? _attachments = new ObservableCollectionExtended<Attachment>();
	private ObservableCollectionExtended<ExtendedMessage> _messages = new ObservableCollectionExtended<ExtendedMessage>();
	private bool _chatsUpdate = false;
	private bool _isLoaded = false;
	private bool _chatsAreLoaded = true;
	private bool _messagesAreLoaded = true;
	private bool _lastMessageIsSelected = true;
	private bool _created = false;
	private string? _subheader = String.Empty;
	private string? _message = String.Empty;
	private string _filter = String.Empty;
	private ExtendedMessage? _selectedMessage;

	public MessagesModel(
		IFileStorageService fileStorageService,
		IConfigurationService configurationService,
		IChatCreationService chatCreationService,
		INotificationService notificationService
	)
	{
		_fileStorageService = fileStorageService;
		_configurationService = configurationService;
		_chatCreationService = chatCreationService;
		_notificationService = notificationService;

		OnKeyDown = ReactiveCommand.Create<KeyEventArgs>(execute: KeyDownHandler);
		OnSelectionChanged = ReactiveCommand.CreateFromTask(execute: SelectionChangedHandler);
		OnFilterChanged = ReactiveCommand.CreateFromTask<string>(execute: FilterChangedHandler);
		OnChatsLoaded = ReactiveCommand.CreateFromTask(execute: ChatsLoadedHandler);
		OnMessagesLoaded = ReactiveCommand.CreateFromTask(execute: MessageLoadedHandler);
		AppendAttachment = ReactiveCommand.CreateFromTask(execute: AddAttachment);
		SendMessage = ReactiveCommand.CreateFromTask(execute: Send);
		OnChatCreation = ReactiveCommand.CreateFromTask(execute: CreateChat);

		this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.5)).InvokeCommand(command: OnFilterChanged);

		Selection.LostSelection += OnLostSelection;
		Chats.CollectionChanged += OnChatsChanged;
		_timer.Elapsed += OnTimerElapsed;
		_timer.Start();
	}

	~MessagesModel()
		=> _timer.Stop();

	public ReactiveCommand<Unit, Unit> AppendAttachment { get; }
	public ReactiveCommand<Unit, Unit> SendMessage { get; }
	public ReactiveCommand<KeyEventArgs, Unit> OnKeyDown { get; }
	public ReactiveCommand<Unit, Unit> OnSelectionChanged { get; }
	public ReactiveCommand<string, Unit> OnFilterChanged { get; }
	public ReactiveCommand<Unit, Unit> OnChatsLoaded { get; }
	public ReactiveCommand<Unit, Unit> OnMessagesLoaded { get; }
	public ReactiveCommand<Unit, Unit> OnChatCreation { get; }

	public SelectionModel<ObservableChat> Selection { get; } = new SelectionModel<ObservableChat>();

	public ObservableCollectionExtended<ObservableChat> Chats
	{
		get => _chats;
		set => this.RaiseAndSetIfChanged(backingField: ref _chats, newValue: value);
	}

	public ObservableCollectionExtended<Attachment>? Attachments
	{
		get => _attachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _attachments, newValue: value);
	}

	public ObservableCollectionExtended<ExtendedMessage> Messages
	{
		get => _messages;
		set => this.RaiseAndSetIfChanged(backingField: ref _messages, newValue: value);
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

	public string? Message
	{
		get => _message;
		set => this.RaiseAndSetIfChanged(backingField: ref _message, newValue: value);
	}

	public bool ChatsAreLoaded
	{
		get => _chatsAreLoaded;
		set => this.RaiseAndSetIfChanged(backingField: ref _chatsAreLoaded, newValue: value);
	}

	public bool MessagesAreLoaded
	{
		get => _messagesAreLoaded;
		set => this.RaiseAndSetIfChanged(backingField: ref _messagesAreLoaded, newValue: value);
	}

	public ExtendedMessage? SelectedMessage
	{
		get => _selectedMessage;
		set => this.RaiseAndSetIfChanged(backingField: ref _selectedMessage, newValue: value);
	}

	public async Task SetUser(User user)
	{
		_user = user;
		Task<InterlocutorCollection> task = _user.GetInterlocutors();
		_chatCollection = await user.GetChats();
		List<Chat> chats = await _chatCollection.ToListAsync();
		Chats.Load(items: chats.Select(selector: chat => chat.ToObservable(notificationService: _notificationService)));
		_isLoaded = !_isLoaded;
		InterlocutorCollection interlocutors = await task;

		_user.JoinedInChat += OnUserJoinedInChat;
		_chatCollection.ReceivedMessageInChat += OnReceivedMessageInChat;
		interlocutors.InterlocutorAppearedOnline += CurrentInterlocutorOnAppearedOnline;
		interlocutors.InterlocutorAppearedOffline += CurrentInterlocutorOnAppearedOffline;
	}

	private async void OnUserJoinedInChat(JoinedInChatEventArgs e)
	{
		Chat? chat = await _chatCollection.FindById(id: e.ChatId);
		ObservableChat? observableChat = chat?.ToObservable(notificationService: _notificationService);
		await observableChat?.LoadInterlocutor(user: _user!)!;
		await Dispatcher.UIThread.InvokeAsync(callback: () => Chats.Insert(index: 0, item: observableChat));
		if (!_created)
			return;

		await Dispatcher.UIThread.InvokeAsync(callback: () => Selection.SelectedItem = Chats[index: 0]);
		_created = false;
	}

	private void KeyDownHandler(KeyEventArgs e)
	{
		if (e.Key != Key.Escape)
			return;

		Selection.SelectedItem = null;
		Messages.Clear();
	}

	private string GetTimeOfOnlineOfInterlocutor()
	{
		if (Selection.SelectedItem!.OnlineAt is null)
			return "в сети";

		TimeSpan dateDifference = (DateTime.Now - Selection.SelectedItem!.OnlineAt).Value;
		if (dateDifference.TotalMinutes < 1)
			return "был(-а) в сети только что";

		if (dateDifference.Days < 1)
			return "был(-а) в сети " + Selection.SelectedItem!.OnlineAt.Humanize(culture: CultureInfo.CurrentUICulture);

		if (dateDifference.Days >= 1 || dateDifference.Hours >= 12)
			return $"был(-а) в сети {Selection.SelectedItem!.OnlineAt:d MMMM} в {Selection.SelectedItem!.OnlineAt:HH:mm}";

		return $"был(-а) в сети {Selection.SelectedItem!.OnlineAt.Humanize(culture: CultureInfo.CurrentUICulture)} в {Selection.SelectedItem!.OnlineAt:HH:mm}";
	}

	private string GetCountOfParticipants()
	{
		if (Selection.SelectedItem is null)
			return String.Empty;

		return WordFormulator.GetForm(count: Selection.SelectedItem!.CountOfParticipants, forms: new string[] { "участников", "участник", "участника" });
	}

	private async Task FilterChangedHandler(string filter)
	{
		if (!_isLoaded || filter == _chatCollection.Filter)
			return;

		ChatsAreLoaded = false;
		Chats.Clear();
		await _chatCollection.SetFilter(filter: filter);
		List<Chat> chats = await _chatCollection.ToListAsync();
		await Dispatcher.UIThread.InvokeAsync(callback: () =>
			Chats.Load(items: chats.Select(selector: chat => chat.ToObservable(notificationService: _notificationService)))
		);
		ChatsAreLoaded = true;
	}

	private async Task SelectionChangedHandler()
	{
		Subheader = Selection.SelectedItem?.IsSingleChat switch
		{
			true => GetTimeOfOnlineOfInterlocutor(),
			false => GetCountOfParticipants(),
			_ => String.Empty
		};

		if (Selection.SelectedItem is { IsRead: false, NotFromMe: true })
			await Selection.SelectedItem?.Read()!;

		if (_previousChatId > 0)
			SaveDraft();

		if (Selection.SelectedItem is null)
		{
			_previousChatId = -1;
			return;
		}

		await UpdateMessages();
		GetGraft();
		_previousChatId = Selection.SelectedItem.Observable.Id;
	}

	private void SaveDraft()
	{
		_allMessages[key: _previousChatId] = Message?.Trim();
		_allAttachments[key: _previousChatId] = Attachments;
		_allMessageBuilders[key: _previousChatId] = _messageBuilder;
		ObservableChat previousChat = Chats.First(predicate: c => c.Observable.Id == _previousChatId);
		previousChat.Draft = Message;
		if (Attachments?.Any() == true)
			previousChat.Draft = $"[{String.Join(", ", Attachments.Select(selector: a => a.FileName))}]";
	}

	private async Task UpdateMessages()
	{
		MessagesAreLoaded = false;
		Messages.Clear();
		_messageFromSelectedChat = await Selection.SelectedItem?.Observable.GetMessages()!;
		List<Message> messages = await _messageFromSelectedChat.ToListAsync();
		MessagesAreLoaded = true;
		Messages = new ObservableCollectionExtended<ExtendedMessage>(collection: messages.Select(selector: m => m.ToExtended(
			isSingleChat: Selection.SelectedItem.Observable.IsSingleChat
		)));

		_lastMessageIsSelected = false;
		SelectedMessage = Messages.LastOrDefault() ?? null;
		_lastMessageIsSelected = true;
	}

	private void GetGraft()
	{
		if (Selection.SelectedItem is null)
			return;

		Attachments = _allAttachments.TryGetValue(key: Selection.SelectedItem.Observable.Id, out ObservableCollectionExtended<Attachment>? attachments)
			? attachments
			: new ObservableCollectionExtended<Attachment>();

		Message = _allMessages.TryGetValue(key: Selection.SelectedItem.Observable.Id, out string? message)
			? message
			: String.Empty;

		_messageBuilder = _allMessageBuilders.TryGetValue(key: Selection.SelectedItem.Observable.Id, out IMessageBuilder? messageBuilder)
			? messageBuilder
			: _messageFromSelectedChat?.CreateMessage();
	}

	private async Task ChatsLoadedHandler()
	{
		if (_chatCollection.AllItemsAreUploaded)
			return;

		int currentLength = _chatCollection.Length;
		await _chatCollection.LoadNext();
		IEnumerable<Chat> chats = await _chatCollection.GetByRange(start: currentLength, end: _chatCollection.Length);
		Chats.Add(items: chats.Select(selector: chat => chat.ToObservable(notificationService: _notificationService)));
	}

	private async void OnChatsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Move)
			_chatsUpdate = true;

		if (e.Action != NotifyCollectionChangedAction.Add)
			return;

		ObservableChat addedItem = e.NewItems!.OfType<ObservableChat>().Single();
		await addedItem.LoadInterlocutor(user: _user!);
	}

	private void CurrentInterlocutorOnAppearedOffline(InterlocutorAppearedOfflineEventArgs e)
		=> Subheader = Selection.SelectedItem?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

	private void CurrentInterlocutorOnAppearedOnline(InterlocutorAppearedOnlineEventArgs e)
		=> Subheader = Selection.SelectedItem?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
		=> Subheader = Selection.SelectedItem?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

	private async Task Send()
	{
		_messageBuilder?.SetText(text: Message?.Trim()!);
		await _messageBuilder?.Send()!;
		_messageBuilder = _messageFromSelectedChat?.CreateMessage();
		Message = String.Empty;
		Attachments?.Clear();
	}

	private async Task AddAttachment()
	{
		IStorageFile? file = await _fileStorageService.OpenFile(fileTypes: new FilePickerFileType[] { FilePickerFileTypes.All });
		if (file is null)
			return;

		StorageItemProperties basicProperties = await file.GetBasicPropertiesAsync();
		if (basicProperties.Size / (1024f * 1024f) >= 30)
		{
			await _notificationService.Show(
				title: "Слишком большой файл",
				content: "Максимальный размер файла - 30Мбайт.",
				type: NotificationType.Warning
			);
			return;
		}

		string pathToFile = HttpUtility.UrlDecode(file.Path.AbsolutePath);
		Attachment attachment = new Attachment()
		{
			FileName = Path.GetFileName(path: pathToFile),
			IsLoaded = false
		};
		attachment.Remove = ReactiveCommand.CreateFromTask(execute: async () =>
		{
			await _messageBuilder?.RemoveAttachment(pathToFile: pathToFile)!;
			Attachments?.Remove(item: attachment);
		});
		Attachments?.Add(item: attachment);
		try
		{
			await _messageBuilder?.AddAttachment(pathToFile: pathToFile)!;
			attachment.IsLoaded = true;
		}
		catch (ArgumentException e)
		{
			await _notificationService.Show(
				title: "Непредвиденная ошибка",
				content: e.Message,
				type: NotificationType.Error
			);
			Attachments?.Remove(item: attachment);
		}
	}

	private async void OnReceivedMessageInChat(ReceivedMessageEventArgs e)
	{
		ObservableChat? chat = Chats.FirstOrDefault(predicate: chat => chat.Observable.Id == e.ChatId);
		bool selected = false;
		if (chat is not null)
		{
			selected = true;
			int indexOfChat = Chats.IndexOf(item: chat);
			if (indexOfChat != 0)
				Chats.Move(oldIndex: indexOfChat, newIndex: 0);
		}
		else
		{
			chat = (await _chatCollection.FindById(id: e.ChatId))!.ToObservable(notificationService: _notificationService);
			Chats.Insert(index: 0, item: chat);
		}

		if (_messageFromSelectedChat is null)
			return;

		ExtendedMessage? receivedMessage = (await _messageFromSelectedChat?.FindById(id: e.MessageId)!)?.ToExtended(isSingleChat: chat.IsSingleChat);

		if (receivedMessage is null)
			return;

		if (!receivedMessage.Message.FromMe && selected)
			await chat.Read();

		Messages.Add(item: receivedMessage);
		SelectedMessage = receivedMessage;
	}

	private void OnLostSelection(object? sender, EventArgs args)
	{
		if (_chatsUpdate)
			Selection.SelectedItem = Chats[index: 0];
		_chatsUpdate = false;
	}

	private async Task MessageLoadedHandler()
	{
		if (_messageFromSelectedChat!.AllItemsAreUploaded && !_lastMessageIsSelected)
			return;

		int currentLength = _messageFromSelectedChat.Length;
		await _messageFromSelectedChat.LoadNext();
		IEnumerable<Message> messages = await _messageFromSelectedChat.GetByRange(start: 0, end: _messageFromSelectedChat.Length - currentLength);
		Messages.InsertRange(collection: messages.Select(selector: m => m.ToExtended(
			isSingleChat: Selection.SelectedItem?.IsSingleChat == true
		)), index: 0);
		_lastMessageIsSelected = false;
		SelectedMessage = Messages[_messageFromSelectedChat.Length - currentLength];
		_lastMessageIsSelected = true;
	}

	private async Task CreateChat()
		=> _created = await _chatCreationService.Create(user: _user!);
}