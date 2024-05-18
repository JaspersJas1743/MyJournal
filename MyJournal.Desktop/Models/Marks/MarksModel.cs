using System.Threading.Tasks;
using MyJournal.Core;

namespace MyJournal.Desktop.Models.Marks;

public abstract class MarksModel : ModelBase
{
	public abstract Task SetUser(User user);
}