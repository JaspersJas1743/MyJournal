using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyJournal.Core.Collections;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class ReceivedTaskCollection
{
	private readonly AssignedTaskCollection? _assignedTaskCollection;
	private readonly TaskAssignedToWardCollection? _taskAssignedToWardCollection;

	public ReceivedTaskCollection(AssignedTaskCollection assignedTaskCollection)
		=> _assignedTaskCollection = assignedTaskCollection;

	public ReceivedTaskCollection(TaskAssignedToWardCollection taskAssignedToWardCollection)
		=> _taskAssignedToWardCollection = taskAssignedToWardCollection;

	public int Length => _assignedTaskCollection?.Length ?? _taskAssignedToWardCollection!.Length;

	public bool AllItemsAreUploaded => _assignedTaskCollection?.AllItemsAreUploaded ?? _taskAssignedToWardCollection!.AllItemsAreUploaded;

	public async Task<int> GetIndex(ReceivedTask task)
	{
		return _assignedTaskCollection is not null
			? await _assignedTaskCollection.GetIndexById(id: task.Id)
			: await _taskAssignedToWardCollection!.GetIndexById(id: task.Id);
	}

	public async Task<ReceivedTask> GetByIndex(int index)
	{
		return _assignedTaskCollection is not null
			? new ReceivedTask(assignedTask: await _assignedTaskCollection.GetByIndex(index: index))
			: new ReceivedTask(taskAssignedToWard: await _taskAssignedToWardCollection!.GetByIndex(index: index));
	}

	public async Task<ReceivedTask> GetByIndex(Index index)
	{
		return _assignedTaskCollection is not null
			? new ReceivedTask(assignedTask: await _assignedTaskCollection.GetByIndex(index: index))
			: new ReceivedTask(taskAssignedToWard: await _taskAssignedToWardCollection!.GetByIndex(index: index));
	}

	public async Task<IEnumerable<ReceivedTask>> GetByRange(Range range)
	{
		return _assignedTaskCollection is not null
			? (await _assignedTaskCollection.GetByRange(range: range)).Select(selector: assignedTask => new ReceivedTask(assignedTask: assignedTask))
			: (await _taskAssignedToWardCollection!.GetByRange(range: range)).Select(selector: taskAssignedToWard => new ReceivedTask(taskAssignedToWard: taskAssignedToWard));
	}

	public async Task<IEnumerable<ReceivedTask>> GetByRange(int start, int end)
	{
		return _assignedTaskCollection is not null
			? (await _assignedTaskCollection.GetByRange(start: start, end: end)).Select(selector: assignedTask => new ReceivedTask(assignedTask: assignedTask))
			: (await _taskAssignedToWardCollection!.GetByRange(start: start, end: end)).Select(selector: taskAssignedToWard => new ReceivedTask(taskAssignedToWard: taskAssignedToWard));
	}

	public async Task<IEnumerable<ReceivedTask>> GetByRange(Index start, Index end)
	{
		return _assignedTaskCollection is not null
			? (await _assignedTaskCollection.GetByRange(start: start, end: end)).Select(selector: assignedTask => new ReceivedTask(assignedTask: assignedTask))
			: (await _taskAssignedToWardCollection!.GetByRange(start: start, end: end)).Select(selector: taskAssignedToWard => new ReceivedTask(taskAssignedToWard: taskAssignedToWard));
	}

	public async Task<ReceivedTask?> FindById(int? id)
	{
		return _assignedTaskCollection is not null
			? new ReceivedTask(assignedTask: await _assignedTaskCollection.FindById(id: id))
			: new ReceivedTask(taskAssignedToWard: await _taskAssignedToWardCollection!.FindById(id: id));
	}

	public async Task LoadNext(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (AllItemsAreUploaded)
			return;

		if (_assignedTaskCollection is not null)
		{
			await _assignedTaskCollection.LoadNext(cancellationToken: cancellationToken);
			return;
		}

		await _taskAssignedToWardCollection!.LoadNext(cancellationToken: cancellationToken);
	}

	public async Task SetCompletionStatus(TaskCompletionStatus status)
	{
		if (_assignedTaskCollection is not null)
		{
			await _assignedTaskCollection.SetCompletionStatus(
				status: Enum.Parse<AssignedTaskCollection.AssignedTaskCompletionStatus>(value: status.ToString())
			);
			return;
		}

		await _taskAssignedToWardCollection!.SetCompletionStatus(
			status: Enum.Parse<TaskAssignedToWardCollection.AssignedTaskCompletionStatus>(value: status.ToString())
		);
	}

	public async Task<List<ReceivedTask>> ToListAsync()
	{
		if (_assignedTaskCollection is not null)
			return await _assignedTaskCollection.Select(selector: assignedTask => new ReceivedTask(assignedTask: assignedTask)).ToListAsync();

		return await _taskAssignedToWardCollection!.Select(selector: taskAssignedToWard => new ReceivedTask(taskAssignedToWard: taskAssignedToWard)).ToListAsync();
	}
}