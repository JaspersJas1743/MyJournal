using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

[Authorize]
public sealed class TeacherHub : Hub<ITeacherHub>
{
	public async Task CreatedTask(int taskId, IEnumerable<string> studentIds)
		=> await Clients.Users(userIds: studentIds).CreatedTask(taskId: taskId);
}