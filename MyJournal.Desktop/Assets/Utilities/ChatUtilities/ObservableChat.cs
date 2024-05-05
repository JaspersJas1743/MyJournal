using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.SubEntities;
using ReactiveUI;
using Activity = MyJournal.Core.UserData.Activity;

namespace MyJournal.Desktop.Assets.Utilities.ChatUtilities;

public class ObservableChat : ReactiveObject
{
	private readonly Chat _chatToObservable;
	private string? _draft = String.Empty;

	public ObservableChat(Chat chatToObservable)
	{
		_chatToObservable = chatToObservable;

		_chatToObservable.ReceivedMessage += _ =>
		{
			foreach (PropertyInfo propertyInfo in typeof(LastMessage).GetProperties())
				this.RaisePropertyChanged(propertyName: propertyInfo.Name);
			this.RaisePropertyChanged(propertyName: nameof(NotFromMe));
		};
	}

	public Chat Observable => _chatToObservable;
	public string? Name => _chatToObservable.Name;
	public string? Photo => _chatToObservable.Photo;
	public string? Content => _chatToObservable.LastMessage?.Content;
	public bool? IsFile => _chatToObservable.LastMessage?.IsFile;
	public DateTime? CreatedAt => _chatToObservable.LastMessage?.CreatedAt;
	public bool FromMe => _chatToObservable.LastMessage?.FromMe ?? false;
	public bool NotFromMe => !_chatToObservable.LastMessage?.FromMe ?? false;
	public bool IsRead
	{
		get => _chatToObservable.LastMessage?.IsRead ?? false;
		set
		{
			_chatToObservable.LastMessage!.IsRead = value;
			this.RaisePropertyChanged();
		}
	}

	public string? Draft
	{
		get => _draft;
		set => this.RaiseAndSetIfChanged(backingField: ref _draft, newValue: value);
	}

	public bool IsSingleChat => _chatToObservable.IsSingleChat;
	public int CountOfParticipants => _chatToObservable.CountOfParticipants;
	public Activity.Statuses? Activity => _chatToObservable.CurrentInterlocutor?.Activity;
	public DateTime? OnlineAt => _chatToObservable.CurrentInterlocutor?.OnlineAt;

	public async Task Read()
	{
		await _chatToObservable.Read();
		if (_chatToObservable.LastMessage is not null)
			IsRead = true;
	}

	public async Task LoadInterlocutor(User user)
	{
		await _chatToObservable.LoadInterlocutor(user: user);

		if (_chatToObservable.CurrentInterlocutor is null)
			return;

		_chatToObservable.CurrentInterlocutor!.AppearedOnline += _ => this.RaisePropertyChanged(propertyName: nameof(Activity));
		_chatToObservable.CurrentInterlocutor!.AppearedOffline += _ => this.RaisePropertyChanged(propertyName: nameof(Activity));
	}
}

public static class ChatExtensions
{
	public static ObservableChat ToObservable(this Chat chat)
		=> new ObservableChat(chatToObservable: chat);
}