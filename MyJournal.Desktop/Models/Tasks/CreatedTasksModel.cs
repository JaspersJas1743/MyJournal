using System.Threading.Tasks;
using MyJournal.Core;

namespace MyJournal.Desktop.Models.Tasks;

public sealed class CreatedTasksModel : TasksModel
{
	public override Task SetUser(User user) =>
		throw new System.NotImplementedException();
}