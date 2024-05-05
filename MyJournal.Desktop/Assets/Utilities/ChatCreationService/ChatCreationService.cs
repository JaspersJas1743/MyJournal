using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Desktop.Models.ChatCreation;
using MyJournal.Desktop.ViewModels.ChatCreation;
using MyJournal.Desktop.Views;
using MyJournal.Desktop.Views.ChatCreation;

namespace MyJournal.Desktop.Assets.Utilities.ChatCreationService;

public class ChatCreationService(MainWindowView mainWindow) : IChatCreationService
{
	public async Task<bool> Create(User user)
	{
		SingleChatCreationModel model = new SingleChatCreationModel();
		await model.SetUser(user: user);
		IChatCreationService.Instance = new ChatCreationWindow()
		{
			DataContext = new ChatCreationWindowVM(model: new ChatCreationWindowModel(
				singleChatCreationVM: new SingleChatCreationVM(model: model)
			))
		};
		bool result = await IChatCreationService.Instance.ShowDialog<bool>(owner: mainWindow);
		IChatCreationService.Instance = null;
		return result;
	}
}

public static class ChatCreationServiceExtensions
{
	public static IServiceCollection AddChatCreationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddSingleton<IChatCreationService, ChatCreationService>();

	public static IServiceCollection AddKeyedChatCreationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedSingleton<IChatCreationService, ChatCreationService>(serviceKey: key);
}