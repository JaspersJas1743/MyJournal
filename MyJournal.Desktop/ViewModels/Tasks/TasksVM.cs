using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Tasks;

namespace MyJournal.Desktop.ViewModels.Tasks;

public class TasksVM(TasksModel model) : MenuItemVM(model: model)
{
	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}