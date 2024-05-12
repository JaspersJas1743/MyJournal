using System.ComponentModel;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public enum ReceivedTaskCompletionStatus
{
	[Description(description: "Все задачи")]
	All,
	[Description(description: "Открытые")]
	Uncompleted,
	[Description(description: "Выполненные")]
	Completed,
	[Description(description: "Завершенные")]
	Expired
}