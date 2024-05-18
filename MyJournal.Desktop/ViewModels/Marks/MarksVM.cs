using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Marks;

namespace MyJournal.Desktop.ViewModels.Marks;

public class MarksVM(MarksModel model) : MenuItemVM(model: model)
{
	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}