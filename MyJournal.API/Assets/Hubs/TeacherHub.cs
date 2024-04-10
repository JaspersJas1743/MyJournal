using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MyJournal.API.Assets.DatabaseModels;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Hubs;

[Authorize(Policy = nameof(UserRoles.Teacher))]
public sealed class TeacherHub : Hub<ITeacherHub>
{
	public async Task StudentCompletedTask(int taskId, IEnumerable<string> studentIds)
		=> await Clients.Users(userIds: studentIds).StudentCompletedTask(taskId: taskId);

	public async Task StudentUncompletedTask(int taskId, IEnumerable<string> studentIds)
		=> await Clients.Users(userIds: studentIds).StudentUncompletedTask(taskId: taskId);

	public async Task CreatedTask(int taskId, int subjectId, IEnumerable<string> studentIds)
		=> await Clients.Users(userIds: studentIds).CreatedTask(taskId: taskId, subjectId: subjectId);

	public async Task CreatedAssessment(int assessmentId, int studentId, int subjectId)
		=> await Clients.Caller.CreatedAssessment(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);

	public async Task ChangedAssessment(int assessmentId, int studentId, int subjectId)
		=> await Clients.Caller.ChangedAssessment(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);

	public async Task DeletedAssessment(int assessmentId, int studentId, int subjectId)
		=> await Clients.Caller.DeletedAssessment(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);

	public async Task ChangedTimetable(int classId, IEnumerable<int> subjectIds)
		=> await Clients.Caller.ChangedTimetable(classId: classId, subjectIds: subjectIds);
}