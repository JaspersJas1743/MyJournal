using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels;

public abstract class MenuItemVM(ModelBase model) : BaseVM(model: model)
{
	public abstract Task SetUser(User user);
}