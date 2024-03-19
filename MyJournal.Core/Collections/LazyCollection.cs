using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.Collections;

public abstract class LazyCollection<T> : IEnumerable<T>
	where T: ISubEntity
{
	#region Fields
	protected readonly ApiClient _client;
	protected readonly List<T> _collection = new List<T>();
	protected readonly int _count;

	protected int _offset;
	#endregion

	#region Constructor
	protected LazyCollection(
		ApiClient client,
		IEnumerable<T> collection,
		int count
	)
	{
		_client = client;
		_collection.AddRange(collection: collection);
		_offset = _collection.Count;
		_count = count;
	}
	#endregion

	#region Properties
	public int Length => _collection.Count;

	public T this[int id]
		=> _collection.Find(match: i => i.Id.Equals(id))
		   ?? throw new ArgumentOutOfRangeException(message: $"Объект с идентификатором {id} отсутствует или не загружен.", paramName: nameof(id));
	#endregion

	#region Methods
	#region Abstract
	public abstract Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	);

	public abstract Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	);

	public abstract Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	#endregion

	#region IEnumerable<T>
	public IEnumerator<T> GetEnumerator()
		=> _collection.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
	#endregion

	#region Overriden
	public override bool Equals(object? obj)
	{
		if (obj is LazyCollection<T> collection)
			return _collection.SequenceEqual(second: collection);
		return false;
	}
	#endregion
	#endregion
}