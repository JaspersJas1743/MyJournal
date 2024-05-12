using System.ComponentModel;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public enum CreatedTaskCompletionStatus
{
	[Description(description: "Все задачи")]
	All,
	[Description(description: "Завершенные")]
	Expired,
	[Description(description: "Открытые")]
	NotExpired
}