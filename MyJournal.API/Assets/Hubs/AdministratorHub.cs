using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MyJournal.API.Assets.DatabaseModels;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Hubs;

[Authorize(Policy = nameof(UserRoles.Administrator))]
public sealed class AdministratorHub : Hub<IAdministratorHub>
{
	public async Task StudentCompletedTask(int taskId)
		=> await Clients.Caller.StudentCompletedTask(taskId: taskId);

	public async Task StudentUncompletedTask(int taskId)
		=> await Clients.Caller.StudentUncompletedTask(taskId: taskId);

	public async Task CreatedTaskToStudents(int taskId, int subjectId, int classId)
		=> await Clients.Caller.CreatedTaskToStudents(taskId: taskId, subjectId: subjectId, classId: classId);

	public async Task CreatedAssessmentToStudent(int assessmentId, int studentId)
		=> await Clients.Caller.CreatedAssessmentToStudent(assessmentId: assessmentId, studentId: studentId);

	public async Task ChangedAssessmentToStudent(int assessmentId, int studentId)
		=> await Clients.Caller.ChangedAssessmentToStudent(assessmentId: assessmentId, studentId: studentId);

	public async Task DeletedAssessmentToStudent(int assessmentId, int studentId)
		=> await Clients.Caller.DeletedAssessmentToStudent(assessmentId: assessmentId, studentId: studentId);
}