using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MyJournal.Desktop.Views;

namespace MyJournal.Desktop.Assets.Utilities.MessagesService;

public sealed class MessageService : IMessageService
{
	private readonly MainWindowView _mainWindow;

	public MessageService(MainWindowView mainWindow)
		=> _mainWindow = mainWindow;

	private static IMsBox<ButtonResult> GetMessageBox(string text, string title, ButtonEnum buttons, Icon image)
	{
		return MessageBoxManager.GetMessageBoxStandard(@params: new MessageBoxStandardParams()
		{
			ButtonDefinitions = buttons,
			CanResize = false,
			ContentMessage = text,
			Icon = image,
			SystemDecorations = SystemDecorations.None,
			EnterDefaultButton = ClickEnum.Ok,
			EscDefaultButton = ClickEnum.No,
			ShowInCenter = true,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			ContentTitle = title
		});
	}

	public async Task<ButtonResult> ShowWindow(string text, string title, ButtonEnum buttons, Icon image)
		=> await GetMessageBox(text: text, title: title, buttons: buttons, image: image).ShowWindowAsync();

	public async Task<ButtonResult> ShowErrorWindow(string text)
		=> await ShowWindow(text: text, title: "Ошибка", buttons: ButtonEnum.OkCancel, image: Icon.Error);

	public async Task<ButtonResult> ShowWarningWindow(string text)
		=> await ShowWindow(text: text, title: "Предупреждение", buttons: ButtonEnum.OkCancel, image: Icon.Warning);

	public async Task<ButtonResult> ShowInformationWindow(string text)
		=> await ShowWindow(text: text, title: "Сообщение", buttons: ButtonEnum.OkCancel, image: Icon.Info);

	public async Task<ButtonResult> ShowMessageWindow(string text)
		=> await ShowWindow(text: text, title: String.Empty, buttons: ButtonEnum.OkCancel, image: Icon.None);

	public async Task<ButtonResult> ShowPopup(string text, string title, ButtonEnum buttons, Icon image)
		=> await GetMessageBox(text: text, title: title, buttons: buttons, image: image).ShowAsPopupAsync(owner: _mainWindow);

	public async Task<ButtonResult> ShowErrorAsPopup(string text)
		=> await ShowPopup(text: text, title: "Ошибка", buttons: ButtonEnum.OkCancel, image: Icon.Error);

	public async Task<ButtonResult> ShowWarningAsPopup(string text)
		=> await ShowPopup(text: text, title: "Предупреждение", buttons: ButtonEnum.OkCancel, image: Icon.Warning);

	public async Task<ButtonResult> ShowInformationAsPopup(string text)
		=> await ShowPopup(text: text, title: "Сообщение", buttons: ButtonEnum.OkCancel, image: Icon.Info);

	public async Task<ButtonResult> ShowMessageAsPopup(string text)
		=> await ShowPopup(text: text, title: String.Empty, buttons: ButtonEnum.OkCancel, image: Icon.None);

	public async Task<ButtonResult> ShowDialog(string text, string title, ButtonEnum buttons, Icon image)
		=> await GetMessageBox(text: text, title: title, buttons: buttons, image: image).ShowWindowDialogAsync(owner: _mainWindow);
}

public static class MessageServiceExtensions
{
	public static IServiceCollection AddMessageService(this IServiceCollection serviceCollection)
	{
		return serviceCollection.AddTransient<IMessageService, MessageService>(
			implementationFactory: provider => new MessageService(mainWindow: provider.GetService<MainWindowView>()!)
		);
	}

	public static IServiceCollection AddKeyedMessageService(this IServiceCollection serviceCollection, string key)
	{
		return serviceCollection.AddKeyedTransient<IMessageService, MessageService>(serviceKey: key,
			implementationFactory: (provider, o) => new MessageService(mainWindow: provider.GetService<MainWindowView>()!)
		);
	}
}