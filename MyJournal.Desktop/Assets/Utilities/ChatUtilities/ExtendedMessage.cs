using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Assets.Utilities.ConfigurationService;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.ChatUtilities;

public sealed record ExtendedAttachment(string? FileName, ReactiveCommand<Button, Unit> Download);

public sealed class ExtendedMessage : ReactiveObject
{
	private readonly Message _message;
	private IEnumerable<ExtendedAttachment>? _attachments;
	private readonly bool _isSingleChat;

	public ExtendedMessage(
		Message message,
		IEnumerable<ExtendedAttachment>? attachments,
		bool isSingleChat
	)
	{
		Message = message;
		Attachments = attachments;
		IsSingleChat = isSingleChat;

		message.ReadMessage += _ => this.RaisePropertyChanged(propertyName: nameof(MessageIsRead));
	}

	public Message Message
	{
		get => _message;
		private init => this.RaiseAndSetIfChanged(backingField: ref _message, newValue: value);
	}

	public bool MessageIsRead => Message.IsRead;

	public IEnumerable<ExtendedAttachment>? Attachments
	{
		get => _attachments;
		set => this.RaiseAndSetIfChanged(backingField: ref _attachments, newValue: value);
	}

	public bool IsSingleChat
	{
		get => _isSingleChat;
		private init => this.RaiseAndSetIfChanged(backingField: ref _isSingleChat, newValue: value);
	}
}

public static class ExtendedMessageExtensions
{
	public static ExtendedMessage ToExtended(this Message message, bool isSingleChat, IConfigurationService configurationService)
	{
		return new ExtendedMessage(message: message, attachments: message.Attachments?.Select(
			selector: a => a.ToExtended(configurationService: configurationService)
		), isSingleChat: isSingleChat);
	}
}

public static class ExtendedAttachmentsExtensions
{
	public static ExtendedAttachment ToExtended(this Core.SubEntities.Attachment attachment, IConfigurationService configurationService)
	{
		return new ExtendedAttachment(
			FileName: Path.GetFileName(path: attachment.LinkToFile),
			Download: ReactiveCommand.CreateFromTask(execute: async (Button button) =>
			{
				string path = configurationService.Get(key: ConfigurationKeys.StorageFolder)!;
				// if ()
				await attachment.Download(pathToSave: path);
				button.Flyout?.ShowAt(placementTarget: button);
			})
		);
	}
}
