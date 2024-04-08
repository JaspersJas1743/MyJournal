using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MyJournal.API.Assets.DatabaseModels;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Hubs;

[Authorize(Policy = nameof(UserRoles.Student))]
public sealed class StudentHub : Hub<IStudentHub>
{
	public async Task CompletedTask(int taskId)
		=> await Clients.Caller.CompletedTask(taskId: taskId);

	public async Task UncompletedTask(int taskId)
		=> await Clients.Caller.UncompletedTask(taskId: taskId);

	public async Task TeacherCreatedTask(int taskId, int subjectId)
		=> await Clients.Caller.TeacherCreatedTask(taskId: taskId, subjectId: subjectId);

	public async Task TeacherCreatedAssessment(int assessmentId, int studentId, int subjectId)
		=> await Clients.Caller.TeacherCreatedAssessment(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);

	public async Task TeacherChangedAssessment(int assessmentId, int studentId, int subjectId)
		=> await Clients.Caller.TeacherChangedAssessment(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);

	public async Task TeacherDeletedAssessment(int assessmentId, int studentId, int subjectId)
		=> await Clients.Caller.TeacherDeletedAssessment(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);

	public async Task ChangedTimetable()
		=> await Clients.Caller.ChangedTimetable();
}