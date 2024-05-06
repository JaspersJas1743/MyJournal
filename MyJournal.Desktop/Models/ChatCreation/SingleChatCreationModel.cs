using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ChatCreationService;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.ViewModels.ChatCreation;
using ReactiveUI;

namespace MyJournal.Desktop.Models.ChatCreation;

public class SingleChatCreationModel : ModelBase
{
	private readonly MultiChatCreationVM _multiChatCreationVM;

	private ObservableCollectionExtended<ExtendedInterlocutor> _interlocutors = new ObservableCollectionExtended<ExtendedInterlocutor>();
	private IntendedInterlocutorCollection _interlocutorCollection;
	private ExtendedInterlocutor _interlocutor;
	private ChatCollection _chatCollection;
	private string? _filter = String.Empty;

	public SingleChatCreationModel(MultiChatCreationVM multiChatCreationVM)
	{
		_multiChatCreationVM = multiChatCreationVM;

		LoadInterlocutors = ReactiveCommand.CreateFromTask(execute: LoadMoreInterlocutors);
		SelectIntendedInterlocutor = ReactiveCommand.CreateFromTask(execute: CreateSingleChat);
		OnFilterChanged = ReactiveCommand.CreateFromTask<string>(execute: FilterChangedHandler);
		CreateMultiChat = ReactiveCommand.CreateFromTask(execute: ToCreatingMultiChat);
		OnAttachedToVisualTree = ReactiveCommand.CreateFromTask(execute: AttachedToVisualTreeHandler);

		this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.5)).InvokeCommand(command: OnFilterChanged);
	}

	private async Task ToCreatingMultiChat()
	{
		MessageBus.Current.SendMessage(message: new ChangeChatCreationVMContentEventArgs(
			newVM: _multiChatCreationVM, animationType: AnimationType.DirectionToRight
		));
	}

	public ObservableCollectionExtended<ExtendedInterlocutor> Interlocutors
	{
		get => _interlocutors;
		set => this.RaiseAndSetIfChanged(backingField: ref _interlocutors, newValue: value);
	}

	public ExtendedInterlocutor Interlocutor
	{
		get => _interlocutor;
		set => this.RaiseAndSetIfChanged(backingField: ref _interlocutor, newValue: value);
	}

	public string? Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> LoadInterlocutors { get; }
	public ReactiveCommand<Unit, Unit> SelectIntendedInterlocutor { get; }
	public ReactiveCommand<string, Unit> OnFilterChanged { get; }
	public ReactiveCommand<Unit, Unit> OnAttachedToVisualTree { get; }
	public ReactiveCommand<Unit, Unit> CreateMultiChat { get; }

	private async Task FilterChangedHandler(string filter)
	{
		if (filter == _interlocutorCollection.Filter)
			return;

		await _interlocutorCollection.SetFilter(filter: filter);
		List<IntendedInterlocutor> interlocutors = await _interlocutorCollection.ToListAsync();
		await Dispatcher.UIThread.InvokeAsync(callback: async () => Interlocutors.Load(items: await Task.WhenAll(
			tasks: interlocutors.Distinct().Select(selector: async i => await i.ToExtended())
		)));
	}

	private async Task CreateSingleChat()
	{
		await _chatCollection.AddSingleChat(interlocutorId: Interlocutor.UserId);
		IChatCreationService.Instance?.Close(dialogResult: true);
	}

	private async Task LoadMoreInterlocutors()
	{
		if (_interlocutorCollection.AllItemsAreUploaded)
			return;

		int currentLength = _interlocutorCollection.Length;
		await _interlocutorCollection.LoadNext();
		IEnumerable<IntendedInterlocutor> interlocutors = await _interlocutorCollection.GetByRange(
			start: currentLength,
			end: _interlocutorCollection.Length
		);
		Interlocutors.Add(items: await Task.WhenAll(tasks: interlocutors.Select(
			selector: async chat => await chat.ToExtended()
		)));
	}

	public async Task SetUser(User user)
	{
		_chatCollection = await user.GetChats();
		_interlocutorCollection = await user.GetIntendedInterlocutors();
	}

	private async Task AttachedToVisualTreeHandler()
	{
		await _interlocutorCollection.SetIncludeExistedInterlocutors(includeExistedInterlocutors: false);
		List<IntendedInterlocutor> interlocutors = await _interlocutorCollection.ToListAsync();
		Interlocutors.Load(items: await Task.WhenAll(tasks: interlocutors.Select(selector: async i => await i.ToExtended())));
	}
}