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

	public async Task CreatedTask(int taskId, IEnumerable<string> studentIds)
		=> await Clients.Users(userIds: studentIds).CreatedTask(taskId: taskId);
}