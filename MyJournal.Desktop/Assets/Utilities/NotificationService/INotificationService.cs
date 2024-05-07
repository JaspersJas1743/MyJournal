using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;

namespace MyJournal.Desktop.Assets.Utilities.NotificationService;

public interface INotificationService
{
	Task Show(
		string? title,
		string? content,
		NotificationType type = NotificationType.Information,
		TimeSpan? expiration = null,
		Action? onClick = null,
		Action? onClose = null
	);
}