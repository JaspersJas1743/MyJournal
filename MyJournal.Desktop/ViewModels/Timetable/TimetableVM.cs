using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Models.Timetable;

namespace MyJournal.Desktop.ViewModels.Timetable;

public sealed class TimetableVM(TimetableModel model) : BaseTimetableVM(model: model)
{
	public BaseTimetableVM Content
	{
		get => model.Content;
		set => model.Content = value;
	}

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}