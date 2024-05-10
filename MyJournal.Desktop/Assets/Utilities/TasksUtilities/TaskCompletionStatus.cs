using System.ComponentModel;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public enum TaskCompletionStatus
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