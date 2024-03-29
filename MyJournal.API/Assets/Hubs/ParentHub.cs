using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MyJournal.API.Assets.DatabaseModels;
using Task = System.Threading.Tasks.Task;

namespace MyJournal.API.Assets.Hubs;

[Authorize(Policy = nameof(UserRoles.Parent))]
public sealed class ParentHub : Hub<IParentHub>
{
	public async Task WardCompletedTask(int taskId)
		=> await Clients.Caller.WardCompletedTask(taskId: taskId);

	public async Task WardUncompletedTask(int taskId)
		=> await Clients.Caller.WardUncompletedTask(taskId: taskId);

	public async Task CreatedTaskToWard(int taskId, int subjectId)
		=> await Clients.Caller.CreatedTaskToWard(taskId: taskId, subjectId: subjectId);

	public async Task CreatedAssessmentToWard(int assessmentId)
		=> await Clients.Caller.CreatedAssessmentToWard(assessmentId: assessmentId);

	public async Task ChangedAssessmentToWard(int assessmentId)
		=> await Clients.Caller.ChangedAssessmentToWard(assessmentId: assessmentId);

	public async Task DeletedAssessmentToWard(int assessmentId)
		=> await Clients.Caller.DeletedAssessmentToWard(assessmentId: assessmentId);
}