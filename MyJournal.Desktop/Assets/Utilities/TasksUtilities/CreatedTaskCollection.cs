using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyJournal.Core.Collections;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class CreatedTaskCollection
{
	private readonly MyJournal.Core.Collections.CreatedTaskCollection? _createdTaskCollection;
	private readonly TaskAssignedToClassCollection? _taskAssignedToClassCollection;

	public CreatedTaskCollection(MyJournal.Core.Collections.CreatedTaskCollection createdTaskCollection)
		=> _createdTaskCollection = createdTaskCollection;

	public CreatedTaskCollection(TaskAssignedToClassCollection taskAssignedToClassCollection)
		=> _taskAssignedToClassCollection = taskAssignedToClassCollection;

	public int Length => _createdTaskCollection?.Length ?? _taskAssignedToClassCollection!.Length;

	public bool AllItemsAreUploaded => _createdTaskCollection?.AllItemsAreUploaded ?? _taskAssignedToClassCollection!.AllItemsAreUploaded;

	public async Task<int> GetIndex(CreatedTask task)
	{
		return _createdTaskCollection is not null
			? await _createdTaskCollection.GetIndexById(id: task.Id)
			: await _taskAssignedToClassCollection!.GetIndexById(id: task.Id);
	}

	public async Task<CreatedTask> GetByIndex(int index)
	{
		return _createdTaskCollection is not null
			? new CreatedTask(createdTask: await _createdTaskCollection.GetByIndex(index: index))
			: new CreatedTask(taskAssignedToClass: await _taskAssignedToClassCollection!.GetByIndex(index: index));
	}

	public async Task<CreatedTask> GetByIndex(Index index)
	{
		return _createdTaskCollection is not null
			? new CreatedTask(createdTask: await _createdTaskCollection.GetByIndex(index: index))
			: new CreatedTask(taskAssignedToClass: await _taskAssignedToClassCollection!.GetByIndex(index: index));
	}

	public async Task<IEnumerable<CreatedTask>> GetByRange(Range range)
	{
		return _createdTaskCollection is not null
			? (await _createdTaskCollection.GetByRange(range: range)).Select(selector: assignedTask => new CreatedTask(createdTask: assignedTask))
			: (await _taskAssignedToClassCollection!.GetByRange(range: range)).Select(selector: taskAssignedToWard => new CreatedTask(taskAssignedToClass: taskAssignedToWard));
	}

	public async Task<IEnumerable<CreatedTask>> GetByRange(int start, int end)
	{
		return _createdTaskCollection is not null
			? (await _createdTaskCollection.GetByRange(start: start, end: end)).Select(selector: assignedTask => new CreatedTask(createdTask: assignedTask))
			: (await _taskAssignedToClassCollection!.GetByRange(start: start, end: end)).Select(selector: taskAssignedToWard => new CreatedTask(taskAssignedToClass: taskAssignedToWard));
	}

	public async Task<IEnumerable<CreatedTask>> GetByRange(Index start, Index end)
	{
		return _createdTaskCollection is not null
			? (await _createdTaskCollection.GetByRange(start: start, end: end)).Select(selector: assignedTask => new CreatedTask(createdTask: assignedTask))
			: (await _taskAssignedToClassCollection!.GetByRange(start: start, end: end)).Select(selector: taskAssignedToWard => new CreatedTask(taskAssignedToClass: taskAssignedToWard));
	}

	public async Task<CreatedTask?> FindById(int? id)
	{
		return _createdTaskCollection is not null
			? new CreatedTask(createdTask: await _createdTaskCollection.FindById(id: id))
			: new CreatedTask(taskAssignedToClass: await _taskAssignedToClassCollection!.FindById(id: id));
	}

	public async Task SetCompletionStatus(CreatedTaskCompletionStatus status)
	{
		if (_createdTaskCollection is not null)
		{
			await _createdTaskCollection.SetCompletionStatus(
				status: Enum.Parse<MyJournal.Core.Collections.CreatedTaskCollection.TaskCompletionStatus>(value: status.ToString())
			);
			return;
		}

		await _taskAssignedToClassCollection!.SetCompletionStatus(
			status: Enum.Parse<TaskAssignedToClassCollection.TaskCompletionStatus>(value: status.ToString())
		);
	}
	public async Task LoadNext(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (AllItemsAreUploaded)
			return;

		if (_createdTaskCollection is not null)
		{
			await _createdTaskCollection.LoadNext(cancellationToken: cancellationToken);
			return;
		}

		await _taskAssignedToClassCollection!.LoadNext(cancellationToken: cancellationToken);
	}

	public async Task<List<CreatedTask>> ToListAsync()
	{
		if (_createdTaskCollection is not null)
			return await _createdTaskCollection.Select(selector: assignedTask => new CreatedTask(createdTask: assignedTask)).ToListAsync();

		return await _taskAssignedToClassCollection!.Select(selector: taskAssignedToWard => new CreatedTask(taskAssignedToClass: taskAssignedToWard)).ToListAsync();
	}
}