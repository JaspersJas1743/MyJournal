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

public sealed record ExtendedMessage(Message Message, IEnumerable<ExtendedAttachment>? Attachments, bool IsSingleChat);

public static class ExtendedMessageExtensions
{
	public static ExtendedMessage ToExtended(this Message message, bool isSingleChat, IConfigurationService configurationService)
	{
		return new ExtendedMessage(Message: message, Attachments: message.Attachments?.Select(
			selector: a => a.ToExtended(configurationService: configurationService)
		), IsSingleChat: isSingleChat);
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
				await attachment.Download(pathToSave: path);
				button.Flyout?.ShowAt(placementTarget: button);
			})
		);
	}
}
