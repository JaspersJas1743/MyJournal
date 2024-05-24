using System.Threading.Tasks;
using MyJournal.Core;

namespace MyJournal.Desktop.Models.Timetable;

public abstract class TimetableModel : ModelBase
{
	public abstract Task SetUser(User user);
}