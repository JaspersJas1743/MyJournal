using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels.Tasks;

public class TasksVM(ModelBase model) : MenuItemVM(model: model)
{
	public override async Task SetUser(User user)
	{
	}
}