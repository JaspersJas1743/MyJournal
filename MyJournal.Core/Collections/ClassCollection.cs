using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class ClassCollection : IEnumerable<Class>
{
	private readonly Lazy<List<Class>> _classes;

	private ClassCollection(
		IEnumerable<Class> classes
	)
	{
		_classes = new Lazy<List<Class>>(value: new List<Class>(collection: classes));
	}

	public Class this[int index]
		=> _classes.Value.ElementAtOrDefault(index: index)
		?? throw new ArgumentOutOfRangeException(message: $"Элемент с индексом {index} отсутствует.", paramName: nameof(index));

	private sealed record GetClassesResponse(int Id, string Name);

	internal static async Task<ClassCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetClassesResponse> classes = await client.GetAsync<IEnumerable<GetClassesResponse>>(
			apiMethod: ClassControllerMethods.GetClasses,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new ClassCollection(
			classes: classes.Select(c => Class.Create(
				client: client,
				classId: c.Id,
				name: c.Name,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult())
		);
	}

	public IEnumerator<Class> GetEnumerator()
		=> _classes.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
}