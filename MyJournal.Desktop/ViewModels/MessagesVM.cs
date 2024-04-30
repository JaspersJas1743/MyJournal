using System.Threading.Tasks;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public sealed class MessagesVM(MessagesModel model) : MenuItemVM(model: model)
{
	public ObservableCollectionExtended<Chat> Chats
	{
		get => model.Chats;
		set => model.Chats = value;
	}

	public override async Task SetUser(User user)
	{
		await model.SetUser(user: user);
	}
}