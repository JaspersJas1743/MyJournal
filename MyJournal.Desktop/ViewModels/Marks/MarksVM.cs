using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models;

namespace MyJournal.Desktop.ViewModels.Marks;

public class MarksVM(ModelBase model) : MenuItemVM(model: model)
{
	public override async Task SetUser(User user)
	{
	}
}