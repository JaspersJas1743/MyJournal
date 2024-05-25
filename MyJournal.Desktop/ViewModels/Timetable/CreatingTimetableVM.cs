using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Timetable;

namespace MyJournal.Desktop.ViewModels.Timetable;

public sealed class CreatingTimetableVM(CreatingTimetableModel model) : BaseTimetableVM(model: model)
{
	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}