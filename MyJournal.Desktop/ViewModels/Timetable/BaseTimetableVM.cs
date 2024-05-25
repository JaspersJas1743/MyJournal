using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Timetable;

namespace MyJournal.Desktop.ViewModels.Timetable;

public class BaseTimetableVM(BaseTimetableModel model) : MenuItemVM(model: model)
{
	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}