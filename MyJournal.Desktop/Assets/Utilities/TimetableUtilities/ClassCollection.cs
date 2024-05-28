using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyJournal.Desktop.Assets.Utilities.NotificationService;

namespace MyJournal.Desktop.Assets.Utilities.TimetableUtilities;

public sealed class ClassCollection(Core.Collections.ClassCollection classCollection)
{
	public async Task<List<Class>> ToListAsync(INotificationService notificationService)
		=> await classCollection.Select(selector: @class => new Class(@class: @class, notificationService: notificationService)).ToListAsync();
}