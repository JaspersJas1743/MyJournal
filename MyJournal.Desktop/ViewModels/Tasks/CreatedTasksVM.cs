using MyJournal.Desktop.Models.Tasks;

namespace MyJournal.Desktop.ViewModels.Tasks;

public sealed class CreatedTasksVM(CreatedTasksModel model) : TasksVM(model: model)
{

}