using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyJournal.Desktop.Assets.Utilities.TasksUtilities;

public sealed class Class(int id, string name, Func<Task<List<Subject>>> subjectsFactory)
{
	private List<Subject>? _subjects = null;

	public int Id { get; } = id;
	public string Name { get; } = name;

	public async Task<List<Subject>> GetSubjects()
		=> _subjects ??= await subjectsFactory();
}
