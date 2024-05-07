using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Desktop.Views;

namespace MyJournal.Desktop.Assets.Utilities.NotificationService;

public sealed class NotificationService : INotificationService
{
	private readonly INotificationManager _notificationManager;

	public NotificationService(MainWindowView topLevel)
		=> _notificationManager = new WindowNotificationManager(host: TopLevel.GetTopLevel(visual: topLevel));

	public async Task Show(
		string? title,
		string? message,
		NotificationType type = NotificationType.Information,
		TimeSpan? expiration = null,
		Action? onClick = null,
		Action? onClose = null
	)
	{
		await Dispatcher.UIThread.InvokeAsync(callback: () => _notificationManager.Show(notification: new Notification(
			title: title,
			message: message,
			type: type,
			expiration: expiration,
			onClick: onClick,
			onClose: onClose
		)));
	}
}

public static class NotificationServiceExtensions
{
	public static IServiceCollection AddNotificationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<INotificationService, NotificationService>();

	public static IServiceCollection AddKeyedNotificationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<INotificationService, NotificationService>(serviceKey: key);
}