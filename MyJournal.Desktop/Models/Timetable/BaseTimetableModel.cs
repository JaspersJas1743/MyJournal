using System.Threading.Tasks;
using MyJournal.Core;

namespace MyJournal.Desktop.Models.Timetable;

public abstract class BaseTimetableModel : ModelBase
{
	public abstract Task SetUser(User user);
}