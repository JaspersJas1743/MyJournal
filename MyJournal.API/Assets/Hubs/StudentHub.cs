using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

public sealed class StudentHub : Hub<IStudentHub>
{
	public async Task CompletedTask(int taskId)
		=> await Clients.Caller.CompletedTask(taskId: taskId);

	public async Task UncompletedTask(int taskId)
		=> await Clients.Caller.UncompletedTask(taskId: taskId);
}