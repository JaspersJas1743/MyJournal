using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class ClassCollection(Core.Collections.ClassCollection classCollection)
{
	public async Task<List<Class>> ToListAsync()
		=> await classCollection.Select(selector: @class => new Class(@class: @class)).ToListAsync();
}