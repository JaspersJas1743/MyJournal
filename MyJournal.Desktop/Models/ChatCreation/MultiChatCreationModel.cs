using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls.Selection;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.MessageBusEvents;
using MyJournal.Desktop.Assets.Utilities.ChatCreationService;
using MyJournal.Desktop.Assets.Utilities.ChatUtilities;
using MyJournal.Desktop.Assets.Utilities.FileService;
using MyJournal.Desktop.ViewModels.ChatCreation;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.ChatCreation;

public class MultiChatCreationModel : ValidatableModel
{
	private readonly IFileStorageService _fileStorageService;
	private readonly ExtendedInterlocutorEqualityComparer _comparer = new ExtendedInterlocutorEqualityComparer();

	private bool _loadingPhoto;
	private readonly ObservableCollectionExtended<ExtendedInterlocutor> _selectedItems = new ObservableCollectionExtended<ExtendedInterlocutor>();
	private ObservableCollectionExtended<ExtendedInterlocutor> _interlocutors = new ObservableCollectionExtended<ExtendedInterlocutor>();
	private IntendedInterlocutorCollection _interlocutorCollection;
	private ExtendedInterlocutor _interlocutor;
	private ChatCollection _chatCollection;
	private bool _programSelection = false;
	private string _chatName = String.Empty;
	private string? _photo = String.Empty;
	private string? _filter = String.Empty;

	public MultiChatCreationModel(IFileStorageService fileStorageService)
	{
		_fileStorageService = fileStorageService;

		LoadPhoto = ReactiveCommand.CreateFromTask(execute: Load);
		DeletePhoto = ReactiveCommand.CreateFromTask(execute: Delete);
		OnAttachedToVisualTree = ReactiveCommand.CreateFromTask(execute: AttachedToVisualTreeHandler);
		OnFilterChanged = ReactiveCommand.CreateFromTask<string>(execute: FilterChangedHandler);
		CreateSingleChat = ReactiveCommand.Create(execute: ToCreatingSingleChat);
		LoadInterlocutors = ReactiveCommand.CreateFromTask(execute: LoadMoreInterlocutors);
		CreateMultiChat = ReactiveCommand.CreateFromTask(execute: CreateChat, canExecute: ValidationContext.Valid);

		Selection.SingleSelect = false;
		Selection.SelectionChanged += OnInterlocutorsSelectionChanged;

		this.WhenAnyValue(property1: model => model.Filter).WhereNotNull()
			.Throttle(dueTime: TimeSpan.FromSeconds(value: 0.5)).InvokeCommand(command: OnFilterChanged);
	}

	public SelectionModel<ExtendedInterlocutor> Selection { get; } = new SelectionModel<ExtendedInterlocutor>();

	public bool LoadingPhoto
	{
		get => _loadingPhoto;
		set => this.RaiseAndSetIfChanged(backingField: ref _loadingPhoto, newValue: value);
	}

	public ObservableCollectionExtended<ExtendedInterlocutor> Interlocutors
	{
		get => _interlocutors;
		set => this.RaiseAndSetIfChanged(backingField: ref _interlocutors, newValue: value);
	}

	public string? Filter
	{
		get => _filter;
		set => this.RaiseAndSetIfChanged(backingField: ref _filter, newValue: value);
	}

	public string? ChatName
	{
		get => _chatName;
		set => this.RaiseAndSetIfChanged(backingField: ref _chatName, newValue: value);
	}

	public string? Photo
	{
		get => _photo;
		set => this.RaiseAndSetIfChanged(backingField: ref _photo, newValue: value);
	}

	public ReactiveCommand<Unit, Unit> LoadInterlocutors { get; }
	public ReactiveCommand<Unit, Unit> CreateMultiChat { get; }
	public ReactiveCommand<Unit, Unit> CreateSingleChat { get; }
	public ReactiveCommand<Unit, Unit> LoadPhoto { get; }
	public ReactiveCommand<Unit, Unit> DeletePhoto { get; }
	public ReactiveCommand<Unit, Unit> OnAttachedToVisualTree { get; }
	public ReactiveCommand<string, Unit> OnFilterChanged { get; }

	public SingleChatCreationVM? SingleChatCreationVM { get; set; }

	public async Task SetUser(User user)
	{
		_chatCollection = await user.GetChats();
		_interlocutorCollection = await user.GetIntendedInterlocutors();
	}

	private async Task AttachedToVisualTreeHandler()
	{
		await _interlocutorCollection.SetIncludeExistedInterlocutors(includeExistedInterlocutors: true);
		List<IntendedInterlocutor> interlocutors = await _interlocutorCollection.ToListAsync();
		Interlocutors.Load(items: await Task.WhenAll(tasks: interlocutors.Select(selector: async i => await i.ToExtended())));
	}

	private async Task Load()
	{
		IStorageFile? file = await _fileStorageService.OpenFile(fileTypes: new FilePickerFileType[] { FilePickerFileTypes.ImageAll });
		if (file is null)
			return;

		Photo = String.Empty;
		LoadingPhoto = true;
		Photo = await _chatCollection.LoadChatPhoto(pathToPhoto: HttpUtility.UrlDecode(str: file.Path.AbsolutePath));
		LoadingPhoto = false;
	}

	private async Task Delete()
	{
		await _chatCollection.RemoveChatPhoto(linkToPhoto: Photo);
		Photo = String.Empty;
	}

	private async Task FilterChangedHandler(string filter)
	{
		if (filter == _interlocutorCollection.Filter)
			return;

		await _interlocutorCollection.SetFilter(filter: filter);
		List<IntendedInterlocutor> interlocutors = await _interlocutorCollection.ToListAsync();
		await Dispatcher.UIThread.InvokeAsync(callback: async () =>
		{
			Interlocutors.Load(items: await Task.WhenAll(
				tasks: interlocutors.Select(selector: async i => await i.ToExtended())
			));
		});
		_programSelection = true;
		foreach (ExtendedInterlocutor selectedInterlocutor in _selectedItems.ToArray())
		{
			await Dispatcher.UIThread.InvokeAsync(callback: () => Selection.Select(
				index: Interlocutors.IndexOf(item: selectedInterlocutor, equalityComparer: _comparer)
			));
		}
		_programSelection = false;
	}

	private void ToCreatingSingleChat()
	{
		MessageBus.Current.SendMessage(message: new ChangeChatCreationVMContentEventArgs(
			newVM: SingleChatCreationVM!, animationType: AnimationType.DirectionToLeft
		));
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

	private async Task CreateChat()
	{
		await _chatCollection.AddMultiChat(
			interlocutorIds: _selectedItems.Select(selector: i => i.UserId),
			chatName: String.IsNullOrWhiteSpace(value: ChatName) ? "Чат без названия" : ChatName,
			linkToPhoto: String.IsNullOrWhiteSpace(value: Photo) ? null : Photo
		);
		IChatCreationService.Instance?.Close(dialogResult: true);
	}

	private void OnInterlocutorsSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs<ExtendedInterlocutor> e)
	{
		if (_programSelection)
			return;

		if (e.SelectedItems.Count > 0)
		{
			_selectedItems.AddRange(collection: e.SelectedItems.OfType<ExtendedInterlocutor>());
			return;
		}

		if (e.DeselectedItems.Count <= 0)
			return;

		foreach (ExtendedInterlocutor extendedInterlocutor in e.DeselectedItems.OfType<ExtendedInterlocutor>())
			_selectedItems.RemoveMany(itemsToRemove: _selectedItems.Where(predicate: i => i.UserId == extendedInterlocutor.UserId));
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model._selectedItems.Count,
			isPropertyValid: count => count > 0,
			message: "Собеседники не выбраны."
		);
	}
}