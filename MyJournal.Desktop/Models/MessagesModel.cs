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
using Avalonia.Input;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using MsBox.Avalonia.Enums;
using MyJournal.Core;
using MyJournal.Core.Builders.MessageBuilder;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Desktop.Assets.Utilities;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.FileService;
using MyJournal.Desktop.Assets.Utilities.MessagesService;
using ReactiveUI;
using Attachment = MyJournal.Desktop.Assets.Utilities.ChatUtilities.Attachment;

namespace MyJournal.Desktop.Models;

public sealed class MessagesModel : ModelBase
{
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromSeconds(value: 30));
	private readonly IFileStorageService _fileStorageService;
	private readonly IMessageService _messageService;

	private int _previousChatId = -1;
	private readonly Dictionary<int, string> _allMessages = new Dictionary<int, string>();
	private readonly Dictionary<int, ObservableCollectionExtended<Attachment>> _allAttachments = new Dictionary<int, ObservableCollectionExtended<Attachment>>();
	private readonly Dictionary<int, IMessageBuilder> _allMessageBuilders = new Dictionary<int, IMessageBuilder>();

	private User? _user;
	private ChatCollection _chatCollection;
	private MessageCollection _messageFromSelectedChat;
	private IMessageBuilder _messageBuilder;
	private ObservableCollectionExtended<ObservableChat> _chats = new ObservableCollectionExtended<ObservableChat>();
	private ObservableCollectionExtended<Attachment> _attachments = new ObservableCollectionExtended<Attachment>();
	private ObservableCollectionExtended<Message> _messages = new ObservableCollectionExtended<Message>();
	private ObservableChat? _selectedChat;
	private bool _isLoaded = false;
	private bool _chatsAreLoaded = false;
	private string? _subheader = String.Empty;
	private string? _message = String.Empty;
	private string _filter = String.Empty;
	private Message? _selectedMessage;

	public MessagesModel(IFileStorageService fileStorageService, IMessageService messageService)
	{
		_fileStorageService = fileStorageService;
		_messageService = messageService;

		OnKeyDown = ReactiveCommand.Create<KeyEventArgs>(execute: KeyDownHandler);
		OnSelectionChanged = ReactiveCommand.CreateFromTask(execute: SelectionChangedHandler);
		OnFilterChanged = ReactiveCommand.CreateFromTask<string>(execute: FilterChangedHandler);
		OnChatsLoaded = ReactiveCommand.CreateFromTask(execute: ChatsLoadedHandler);
		AppendAttachment = ReactiveCommand.CreateFromTask(execute: AddAttachment);
		SendMessage = ReactiveCommand.CreateFromTask(execute: Send);

		this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.5)).InvokeCommand(command: OnFilterChanged);

		this.WhenAnyValue(property1: model => model.Message).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.2))
			.Subscribe(onNext: message => _messageBuilder?.SetText(text: message));

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

	public ObservableCollectionExtended<ObservableChat> Chats
	{
		get => _chats;
		set => this.RaiseAndSetIfChanged(backingField: ref _chats, newValue: value);
	}

	public ObservableCollectionExtended<Attachment> Attachments
	{
		get => _attachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _attachments, newValue: value);
	}

	public ObservableCollectionExtended<Message> Messages
	{
		get => _messages;
		set => this.RaiseAndSetIfChanged(backingField: ref _messages, newValue: value);
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

	public string Message
	{
		get => _message;
		set => this.RaiseAndSetIfChanged(backingField: ref _message, newValue: value);
	}

	public bool ChatsAreLoaded
	{
		get => _chatsAreLoaded;
		set => this.RaiseAndSetIfChanged(backingField: ref _chatsAreLoaded, newValue: value);
	}

	public Message? SelectedMessage
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

		return (DateTime.Now - SelectedChat!.OnlineAt).Value.Days switch
		{
			> 1 => $"был(-а) в сети {SelectedChat!.OnlineAt:dd MMMM yyyy} в {SelectedChat!.OnlineAt:HH:mm}",
			1 => $"был(-а) в сети {SelectedChat!.OnlineAt.Humanize(culture: CultureInfo.CurrentUICulture)} в {SelectedChat!.OnlineAt:HH:mm}",
			_ => "был(-а) в сети " + SelectedChat!.OnlineAt.Humanize(culture: CultureInfo.CurrentUICulture)
		};
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

		ChatsAreLoaded = true;
		Chats.Clear();
		await _chatCollection.SetFilter(filter: filter);
		List<Chat> chats = await _chatCollection.ToListAsync();
		Chats.Load(items: chats.Select(selector: chat => chat.ToObservable()));
		ChatsAreLoaded = false;
	}

	private async Task SelectionChangedHandler()
	{
		Subheader = SelectedChat!.IsSingleChat ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

		if (SelectedChat.IsRead == false)
			await SelectedChat?.Read()!;

		if (_previousChatId > 0)
		{
			_allAttachments[key: _previousChatId] = Attachments;
			_allMessages[key: _previousChatId] = Message;
			_allMessageBuilders[key: _previousChatId] = _messageBuilder;
		}

		Attachments = _allAttachments.TryGetValue(key: SelectedChat.Observable.Id, out ObservableCollectionExtended<Attachment>? attachments) ? attachments : new ObservableCollectionExtended<Attachment>();
		Message = _allMessages.TryGetValue(key: SelectedChat.Observable.Id, out string? message) ? message : String.Empty;

		_messageFromSelectedChat = await SelectedChat.Observable.GetMessages();
		Messages = new ObservableCollectionExtended<Message>(list: await _messageFromSelectedChat.ToListAsync());
		SelectedMessage = Messages.FirstOrDefault(predicate: m => m is { IsRead: false, FromMe: false }) ?? Messages.LastOrDefault() ?? null;

		_messageBuilder = _allMessageBuilders.TryGetValue(key: SelectedChat.Observable.Id, out IMessageBuilder? messageBuilder)
			? messageBuilder
			: _messageFromSelectedChat.CreateMessage();

		_previousChatId = SelectedChat.Observable.Id;
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

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
		=> Subheader = SelectedChat?.IsSingleChat == true ? GetTimeOfOnlineOfInterlocutor() : GetCountOfParticipants();

	private async Task Send()
	{
		await _messageBuilder.Send();
		Message = String.Empty;
		Attachments.Clear();
	}

	private async Task AddAttachment()
	{
		IStorageFile? pickedFile = await _fileStorageService.OpenFile();
		if (pickedFile is null)
			return;

		StorageItemProperties basicProperties = await pickedFile.GetBasicPropertiesAsync();
		if (basicProperties.Size / (1024f * 1024f) >= 30)
		{
			await _messageService.ShowPopup(
				text: "Максимальный размер файла: 30Мбайт.",
				title: String.Empty,
				buttons: ButtonEnum.Ok,
				image: Icon.Warning
			);
			return;
		}

		string pathToFile = HttpUtility.UrlDecode(pickedFile.Path.AbsolutePath);
		Attachment attachment = new Attachment()
		{
			FileName = Path.GetFileName(path: pathToFile)
		};
		attachment.Remove = ReactiveCommand.CreateFromTask(execute: async () =>
		{
			await _messageBuilder.RemoveAttachment(pathToFile: pathToFile);
			Attachments.Remove(item: attachment);
		});
		Attachments.Add(item: attachment);

		await _messageBuilder.AddAttachment(pathToFile: pathToFile);
	}
}