using System.Threading.Tasks;
using MyJournal.Core;

namespace MyJournal.Desktop.Models.Tasks;

public abstract class TasksModel : ModelBase
{
	public abstract Task SetUser(User user);
}