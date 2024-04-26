using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public sealed class MessagesVM(MessagesModel model) : MenuItemVM(model: model)
{
	public override async Task SetUser(User user)
	{
	}
}